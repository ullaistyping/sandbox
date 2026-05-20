using System;
using System.Collections.Generic;
using Godot;

public partial class Simulation : RefCounted
{
	public const int SimW = 320;
	public const int SimH = 180;

	public enum Cell : byte
	{
		Air = 0, Sand = 1, Water = 2, Stone = 3, Lava = 4,
		Gas = 5, Food = 6, Copper = 7, Steam = 8, Battery = 9, Wood = 10, Mirror = 11,
		Dirt = 12, Grass = 13, GrassSeed = 14, TreeSeed = 15,
		Bark = 16, Leaves = 17, Fire = 18, Smoke = 19,
		LiquidNitrogen = 20, NitrogenGas = 21, Ice = 22
	}

	public byte[]  Grid;
	public byte[]  Flow;     // water: flow dir; copper: heat 0-255
	public byte[]  Electric; // 1 if copper cell is electrified this tick
	public byte[]  Pinned;   // 1 = cell is pinned (never moves)
	public byte[]  Settled;  // 1 = water cell can skip horizontal spread this tick
	public float[] VelX;
	public float[] VelY;
	// Bumped whenever pixel-visible state may have changed. Main reads this to
	// skip the full grid render on frames where nothing changed.
	public bool RenderDirty = true;
	public readonly List<(int cx, int cy, int radius)> PendingExplosions = new();
	private byte[] _visited;
	private bool   _flip;
	private readonly Random     _rng           = new Random();
	private readonly Stack<int> _electricStack = new Stack<int>();
	private readonly Queue<int> _hotQueue      = new Queue<int>(1024);
	private readonly Queue<int> _coldQueue     = new Queue<int>(1024);
	private byte[]   _hotDist;   // copper hops from nearest lava (255 = unreached)
	private byte[]   _coldDist;  // copper hops from nearest LN2

	// Velocity-cell physics — tunable at runtime
	public float Gravity   = 0.45f;
	public float Friction  = 0.99f;
	public float DampCol   = 0.30f;
	public float StopThr   = 0.30f;

	public int   CopperBoilThreshold = 200; // 128=room, 255=lava-hot
	public int   CopperGasThreshold  = 230;
	public int   IceCopperThreshold  = 64;  // copper below this freezes steam
	public int   HeatRange           = 32;  // max copper hops heat travels
	public int   HeatFireDist        = 16;  // fire-adjacent copper acts like this many hops from lava
	public int   HeatSmoothing       = 2;   // higher = smoother ramp; 1 = instant
	public float FireIgniteChance    = 0.12f;
	public int   FireBaseTicks       = 30;
	public float GrassSeedRate       = 0.003f;
	public float TreeSeedRate        = 0.001f;
	public int   WaterSpreadDist     = 4;   // max cells water can travel horizontally per tick

	public Simulation()
	{
		int size = SimW * SimH;
		Grid     = new byte[size];
		Flow     = new byte[size];
		Electric = new byte[size];
		Pinned   = new byte[size];
		Settled  = new byte[size];
		VelX     = new float[size];
		VelY     = new float[size];
		_visited  = new byte[size];
		_hotDist  = new byte[size];
		_coldDist = new byte[size];
	}

	public bool InBounds(int x, int y) => x >= 0 && x < SimW && y >= 0 && y < SimH;

	public byte GetCell(int x, int y)
	{
		if (x < 0 || x >= SimW || y < 0 || y >= SimH) return (byte)Cell.Sand;
		return Grid[y * SimW + x];
	}

	public void SetCell(int x, int y, int type)
	{
		if (!InBounds(x, y)) return;
		int i = y * SimW + x;
		Grid[i] = (byte)type;
		if (type == (int)Cell.Water || type == (int)Cell.Copper)
			Flow[i] = 128; // room temperature on the 0–255 scale
		else
			Flow[i] = 0;
		VelX[i] = 0; VelY[i] = 0;
		WakeNeighbors(x, y);
		RenderDirty = true;
	}

	// Clears the Settled flag on (x,y) and its 4 neighbors so settled water re-checks
	// its surroundings next tick. Called from every mutation path that can change
	// what a settled cell "sees" — Swap, SetCell, explosions, reactions, etc.
	private void WakeNeighbors(int x, int y)
	{
		if (InBounds(x, y))     Settled[y       * SimW + x      ] = 0;
		if (InBounds(x - 1, y)) Settled[y       * SimW + (x - 1)] = 0;
		if (InBounds(x + 1, y)) Settled[y       * SimW + (x + 1)] = 0;
		if (InBounds(x, y - 1)) Settled[(y - 1) * SimW + x      ] = 0;
		if (InBounds(x, y + 1)) Settled[(y + 1) * SimW + x      ] = 0;
	}

	public void SetPinned(int x, int y, bool pin)
	{
		if (InBounds(x, y)) Pinned[y * SimW + x] = pin ? (byte)1 : (byte)0;
	}

	// ── Main update ───────────────────────────────────────────────────────────

	public void Update()
	{
		Array.Clear(_visited, 0, _visited.Length);
		_flip = !_flip;
		UpdateVelocityCells();
		for (int y = SimH - 1; y >= 0; y--)
		{
			if (_flip) for (int x = SimW - 1; x >= 0; x--) UpdateCell(x, y);
			else        for (int x = 0; x < SimW; x++) UpdateCell(x, y);
		}
		PropagateElectricity();
		PropagateHeat();
		RenderDirty = true; // a tick can change anything — always re-render after
	}

	// ── Electricity ───────────────────────────────────────────────────────────

	private void PropagateElectricity()
	{
		Array.Clear(Electric, 0, Electric.Length);
		_electricStack.Clear();
		int n = Grid.Length;
		for (int i = 0; i < n; i++)
		{
			if (Grid[i] != (byte)Cell.Battery) continue;
			int bx = i % SimW, by = i / SimW;
			TryElectrify(bx - 1, by); TryElectrify(bx + 1, by);
			TryElectrify(bx, by - 1); TryElectrify(bx, by + 1);
		}
		while (_electricStack.Count > 0)
		{
			int idx = _electricStack.Pop();
			int cx = idx % SimW, cy = idx / SimW;
			TryElectrify(cx - 1, cy); TryElectrify(cx + 1, cy);
			TryElectrify(cx, cy - 1); TryElectrify(cx, cy + 1);
		}
	}

	private void TryElectrify(int x, int y)
	{
		if (!InBounds(x, y)) return;
		int i = y * SimW + x;
		if (Grid[i] == (byte)Cell.Copper && Electric[i] == 0)
		{ Electric[i] = 1; _electricStack.Push(i); }
	}

	// ── Copper heat ──────────────────────────────────────────────────────────
	// Two BFS distance-field passes through connected copper:
	//   _hotDist[i]  = copper hops from nearest Lava (Fire seeds at HeatFireDist)
	//   _coldDist[i] = copper hops from nearest LiquidNitrogen
	// Heat is computed deterministically from those distances and smoothed
	// toward Flow[i].  Independent of scan order → lava and LN2 behave
	// symmetrically, and copper touching both settles cleanly at room temp.

	private void PropagateHeat()
	{
		int n     = Grid.Length;
		int range = Math.Clamp(HeatRange, 1, 254);
		int fireD = Math.Clamp(HeatFireDist, 0, range);
		Array.Fill(_hotDist,  (byte)255);
		Array.Fill(_coldDist, (byte)255);
		_hotQueue.Clear();
		_coldQueue.Clear();

		ReadOnlySpan<int> dx = stackalloc[] { 0, 0, -1, 1 };
		ReadOnlySpan<int> dy = stackalloc[] { -1, 1, 0, 0 };

		// Seed: each copper cell scans neighbours for thermal sources.
		for (int i = 0; i < n; i++)
		{
			if (Grid[i] != (byte)Cell.Copper) continue;
			int x = i % SimW, y = i / SimW;
			for (int k = 0; k < 4; k++)
			{
				int nx = x + dx[k], ny = y + dy[k];
				if (!InBounds(nx, ny)) continue;
				byte nc = Grid[ny * SimW + nx];
				if (nc == (byte)Cell.Lava)
				{
					if (_hotDist[i] > 0) { _hotDist[i] = 0; _hotQueue.Enqueue(i); }
				}
				else if (nc == (byte)Cell.Fire)
				{
					if (_hotDist[i] > fireD) { _hotDist[i] = (byte)fireD; _hotQueue.Enqueue(i); }
				}
				else if (nc == (byte)Cell.LiquidNitrogen)
				{
					if (_coldDist[i] > 0) { _coldDist[i] = 0; _coldQueue.Enqueue(i); }
				}
			}
		}

		HeatBfs(_hotQueue,  _hotDist,  range, dx, dy);
		HeatBfs(_coldQueue, _coldDist, range, dx, dy);

		// Compute target heat from distances and smooth Flow toward it.
		int smooth = Math.Max(1, HeatSmoothing);
		for (int i = 0; i < n; i++)
		{
			if (Grid[i] != (byte)Cell.Copper) continue;
			int hd = _hotDist[i], cd = _coldDist[i];
			int hotPull  = hd < range ? 127 * (range - hd) / range : 0;
			int coldPull = cd < range ? 128 * (range - cd) / range : 0;
			int target   = Math.Clamp(128 + hotPull - coldPull, 0, 255);

			int current = Flow[i];
			int diff    = target - current;
			int step    = diff / smooth;
			if (step == 0 && diff != 0) step = diff > 0 ? 1 : -1;
			Flow[i] = (byte)Math.Clamp(current + step, 0, 255);
		}

		// Side effects: hot copper boils water / ignites gas, cold copper freezes steam.
		for (int i = 0; i < n; i++)
		{
			if (Grid[i] != (byte)Cell.Copper) continue;
			int heat = Flow[i];
			int x = i % SimW, y = i / SimW;

			if (heat >= CopperBoilThreshold)
				for (int k = 0; k < 4; k++)
				{
					int nx = x + dx[k], ny = y + dy[k];
					if (!InBounds(nx, ny)) continue;
					int ni = ny * SimW + nx;
					if (Grid[ni] == (byte)Cell.Water) { Grid[ni] = (byte)Cell.Steam; Flow[ni] = 0; }
				}

			if (heat >= CopperGasThreshold)
				for (int k = 0; k < 4; k++)
				{
					int nx = x + dx[k], ny = y + dy[k];
					if (!InBounds(nx, ny)) continue;
					if (Grid[ny * SimW + nx] == (byte)Cell.Gas) { ExplodeGasPocket(nx, ny); break; }
				}

			if (heat <= IceCopperThreshold)
				for (int k = 0; k < 4; k++)
				{
					int nx = x + dx[k], ny = y + dy[k];
					if (!InBounds(nx, ny)) continue;
					int ni = ny * SimW + nx;
					if (Grid[ni] == (byte)Cell.Steam) { Grid[ni] = (byte)Cell.Ice; Flow[ni] = 0; }
				}
		}
	}

	private void HeatBfs(Queue<int> q, byte[] dist, int range,
						 ReadOnlySpan<int> dx, ReadOnlySpan<int> dy)
	{
		while (q.Count > 0)
		{
			int i = q.Dequeue();
			byte d = dist[i];
			if (d >= range) continue;
			byte nd = (byte)(d + 1);
			int x = i % SimW, y = i / SimW;
			for (int k = 0; k < 4; k++)
			{
				int nx = x + dx[k], ny = y + dy[k];
				if (!InBounds(nx, ny)) continue;
				int ni = ny * SimW + nx;
				if (Grid[ni] != (byte)Cell.Copper) continue;
				if (dist[ni] > nd) { dist[ni] = nd; q.Enqueue(ni); }
			}
		}
	}

	// ── Velocity cells ────────────────────────────────────────────────────────

	private void UpdateVelocityCells()
	{
		float gravity  = Gravity;
		float friction = Friction;
		float dampCol  = DampCol;
		float stopThr  = StopThr;
		const int maxSteps = 8;

		int n = VelX.Length;
		for (int i = 0; i < n; i++)
		{
			if (_visited[i] != 0) continue;
			byte g = Grid[i];
			if (g == (byte)Cell.Air)     { VelX[i] = 0; VelY[i] = 0; continue; }
			if (g == (byte)Cell.Copper)  { VelX[i] = 0; VelY[i] = 0; continue; }
			if (g == (byte)Cell.Battery) { VelX[i] = 0; VelY[i] = 0; continue; }
			if (g == (byte)Cell.Wood)    { VelX[i] = 0; VelY[i] = 0; continue; }
			if (g == (byte)Cell.Mirror)  { VelX[i] = 0; VelY[i] = 0; continue; }
			if (g == (byte)Cell.Grass)   { VelX[i] = 0; VelY[i] = 0; continue; }
			if (g == (byte)Cell.Bark)    { VelX[i] = 0; VelY[i] = 0; continue; }
			if (g == (byte)Cell.Fire)         { VelX[i] = 0; VelY[i] = 0; continue; }
			if (g == (byte)Cell.Smoke)        { VelX[i] = 0; VelY[i] = 0; continue; }
			if (g == (byte)Cell.NitrogenGas)  { VelX[i] = 0; VelY[i] = 0; continue; }
			if (g == (byte)Cell.Ice)          { VelX[i] = 0; VelY[i] = 0; continue; }
			if (Pinned[i] != 0)          { VelX[i] = 0; VelY[i] = 0; continue; }

			float vx = VelX[i], vy = VelY[i];
			if (MathF.Abs(vx) < stopThr && MathF.Abs(vy) < stopThr) { VelX[i] = 0; VelY[i] = 0; continue; }

			vy += gravity; vx *= friction; vy *= friction;

			int absX = (int)MathF.Ceiling(MathF.Abs(vx));
			int absY = (int)MathF.Ceiling(MathF.Abs(vy));
			int steps = Math.Min(maxSteps, Math.Max(absX, absY));
			if (steps == 0) { VelX[i] = vx; VelY[i] = vy; continue; }

			float stepX = vx / steps, stepY = vy / steps;
			int x = i % SimW, y = i / SimW;
			float curX = x + 0.5f, curY = y + 0.5f;
			int prevX = x, prevY = y;
			bool collided = false;

			for (int s = 0; s < steps; s++)
			{
				curX += stepX; curY += stepY;
				int nx = (int)curX, ny = (int)curY;
				if (nx == prevX && ny == prevY) continue;
				if (!InBounds(nx, ny)) { collided = true; break; }
				int newI = ny * SimW + nx;
				if (Grid[newI] != (byte)Cell.Air || Pinned[newI] != 0) { collided = true; break; }

				int prevI = prevY * SimW + prevX;
				Grid[newI] = Grid[prevI]; Flow[newI] = Flow[prevI];
				VelX[newI] = vx; VelY[newI] = vy;
				Grid[prevI] = (byte)Cell.Air; Flow[prevI] = 0;
				VelX[prevI] = 0; VelY[prevI] = 0;
				_visited[newI] = 1;
				prevX = nx; prevY = ny;
			}

			int finalI = prevY * SimW + prevX;
			if (collided) { VelX[finalI] = vx * -dampCol; VelY[finalI] = vy * -dampCol; }
			else          { VelX[finalI] = vx;            VelY[finalI] = vy; }
			if (MathF.Abs(VelX[finalI]) < stopThr) VelX[finalI] = 0;
			if (MathF.Abs(VelY[finalI]) < stopThr) VelY[finalI] = 0;
		}
	}

	// ── Cell dispatch ─────────────────────────────────────────────────────────

	private void UpdateCell(int x, int y)
	{
		int i = y * SimW + x;
		if (_visited[i] != 0) return;
		if (Pinned[i]   != 0) return; // pinned — never moves
		switch ((Cell)Grid[i])
		{
			case Cell.Sand:   UpdateSand(x, y);   break;
			case Cell.Water:  UpdateWater(x, y);  break;
			case Cell.Lava:   UpdateLava(x, y);   break;
			case Cell.Gas:    UpdateGas(x, y);    break;
			case Cell.Food:   UpdateFood(x, y);   break;
			case Cell.Steam:     UpdateSteam(x, y);     break;
			case Cell.Dirt:      UpdateDirt(x, y);      break;
			case Cell.GrassSeed: UpdateGrassSeed(x, y); break;
			case Cell.TreeSeed:  UpdateTreeSeed(x, y);  break;
			case Cell.Grass:     UpdateGrass(x, y);     break;
			case Cell.Fire:            UpdateFire(x, y);            break;
			case Cell.Smoke:           UpdateSmoke(x, y);           break;
			case Cell.LiquidNitrogen:  UpdateLiquidNitrogen(x, y);  break;
			case Cell.NitrogenGas:     UpdateNitrogenGas(x, y);     break;
			case Cell.Ice:             UpdateIce(x, y);             break;
			// Stone, Battery, Wood, Bark, Leaves, Mirror: static
		}
	}

	private void Swap(int ax, int ay, int bx, int by)
	{
		int ai = ay * SimW + ax, bi = by * SimW + bx;
		if (Pinned[ai] != 0 || Pinned[bi] != 0) return; // respect pins
		(Grid[bi], Grid[ai]) = (Grid[ai], Grid[bi]);
		(Flow[bi], Flow[ai]) = (Flow[ai], Flow[bi]);
		(VelX[bi], VelX[ai]) = (VelX[ai], VelX[bi]);
		(VelY[bi], VelY[ai]) = (VelY[ai], VelY[bi]);
		_visited[bi] = 1;
		// A swap always disturbs the local neighborhood — wake settled water nearby
		// so it re-checks fall/spread, and clear the swapped cells themselves.
		Settled[ai] = 0;
		Settled[bi] = 0;
		WakeNeighbors(ax, ay);
		WakeNeighbors(bx, by);
	}

	// ── Element rules ─────────────────────────────────────────────────────────

	private static bool SandCanFallInto(byte c) =>
		c == (byte)Cell.Air || c == (byte)Cell.Water || c == (byte)Cell.Gas || c == (byte)Cell.Steam;

	private void UpdateSand(int x, int y)
	{
		if (y + 1 >= SimH) return;
		if (SandCanFallInto(GetCell(x, y + 1))) { Swap(x, y, x, y + 1); return; }
		bool lo = x > 0        && SandCanFallInto(GetCell(x - 1, y + 1));
		bool ro = x < SimW - 1 && SandCanFallInto(GetCell(x + 1, y + 1));
		if      (lo && ro) { if (_rng.NextSingle() < 0.5f) Swap(x, y, x - 1, y + 1); else Swap(x, y, x + 1, y + 1); }
		else if (lo) Swap(x, y, x - 1, y + 1);
		else if (ro) Swap(x, y, x + 1, y + 1);
	}

	private bool ReactLavaWithWater(int x, int y)
	{
		int i = y * SimW + x;
		ReadOnlySpan<int> dx = stackalloc[] { 0, 0, -1, 1 };
		ReadOnlySpan<int> dy = stackalloc[] { -1, 1, 0, 0 };
		for (int k = 0; k < 4; k++)
		{
			int nx = x + dx[k], ny = y + dy[k];
			if (!InBounds(nx, ny)) continue;
			int ni = ny * SimW + nx;
			if (Grid[ni] == (byte)Cell.Water)
			{
				Grid[i] = (byte)Cell.Stone; Flow[i] = 0;
				Grid[ni] = (byte)Cell.Stone; Flow[ni] = 0;
				_visited[i] = 1; _visited[ni] = 1;
				return true;
			}
		}
		return false;
	}

	private void UpdateLava(int x, int y)
	{
		if (LavaTouchesGas(x, y)) return;
		if (ReactLavaWithWater(x, y)) return;
		if (ReactLavaWithLN2(x, y)) return;
		LavaIgnitesFlammables(x, y);
		if (_rng.NextSingle() < 0.5f) return;
		if (y + 1 < SimH)
		{
			byte below = GetCell(x, y + 1);
			if (below == (byte)Cell.Air || below == (byte)Cell.Gas) { Swap(x, y, x, y + 1); return; }
			bool dl = x > 0        && (GetCell(x-1,y+1)==(byte)Cell.Air || GetCell(x-1,y+1)==(byte)Cell.Gas);
			bool dr = x < SimW - 1 && (GetCell(x+1,y+1)==(byte)Cell.Air || GetCell(x+1,y+1)==(byte)Cell.Gas);
			if (dl && dr) { if (_rng.NextSingle()<0.5f) Swap(x,y,x-1,y+1); else Swap(x,y,x+1,y+1); return; }
			if (dl) { Swap(x, y, x-1, y+1); return; }
			if (dr) { Swap(x, y, x+1, y+1); return; }
		}
		int dir = _rng.NextSingle() < 0.5f ? -1 : 1;
		int nx2 = x + dir;
		if (nx2 >= 0 && nx2 < SimW && GetCell(nx2, y) == (byte)Cell.Air) Swap(x, y, nx2, y);
	}

	private bool GasMeetsLava(int x, int y, out int lx, out int ly)
	{
		ReadOnlySpan<int> dx = stackalloc[] { 0, 0, -1, 1 };
		ReadOnlySpan<int> dy = stackalloc[] { -1, 1, 0, 0 };
		for (int k = 0; k < 4; k++)
		{
			int nx = x + dx[k], ny = y + dy[k];
			if (InBounds(nx, ny) && Grid[ny*SimW+nx] == (byte)Cell.Lava) { lx = nx; ly = ny; return true; }
		}
		lx = ly = 0; return false;
	}

	private void UpdateGas(int x, int y)
	{
		if (GasMeetsLava(x, y, out _, out _)) { ExplodeGasPocket(x, y); return; }
		if (y == 0) { int i = y*SimW+x; Grid[i]=(byte)Cell.Air; Flow[i]=0; return; }
		if (GetCell(x, y-1) == (byte)Cell.Air) { Swap(x, y, x, y-1); return; }
		bool ul = x > 0        && GetCell(x-1, y-1) == (byte)Cell.Air;
		bool ur = x < SimW - 1 && GetCell(x+1, y-1) == (byte)Cell.Air;
		if (ul && ur) { if (_rng.NextSingle()<0.5f) Swap(x,y,x-1,y-1); else Swap(x,y,x+1,y-1); return; }
		if (ul) { Swap(x, y, x-1, y-1); return; }
		if (ur) { Swap(x, y, x+1, y-1); return; }
		int dir = _rng.NextSingle() < 0.5f ? -1 : 1;
		int sx = x + dir;
		if (sx >= 0 && sx < SimW && GetCell(sx, y) == (byte)Cell.Air) Swap(x, y, sx, y);
	}

	private bool LavaTouchesGas(int x, int y)
	{
		ReadOnlySpan<int> ddx = stackalloc[] { 0, 0, -1, 1 };
		ReadOnlySpan<int> ddy = stackalloc[] { -1, 1, 0, 0 };
		for (int k = 0; k < 4; k++)
		{
			int nx = x + ddx[k], ny = y + ddy[k];
			if (InBounds(nx, ny) && Grid[ny*SimW+nx] == (byte)Cell.Gas) { ExplodeGasPocket(nx, ny); return true; }
		}
		return false;
	}

	private void ExplodeGasPocket(int startX, int startY)
	{
		var stack    = new Stack<int>();
		var seen     = new HashSet<int>();
		var gasCells = new List<(int x, int y)>();
		int s0 = startY * SimW + startX;
		stack.Push(s0); seen.Add(s0);
		while (stack.Count > 0)
		{
			int idx = stack.Pop();
			if (Grid[idx] != (byte)Cell.Gas) continue;
			int gx = idx % SimW, gy = idx / SimW;
			gasCells.Add((gx, gy));
			void Try(int ni) { if (!seen.Contains(ni)) { seen.Add(ni); stack.Push(ni); } }
			if (gx > 0)      Try(idx - 1);
			if (gx < SimW-1) Try(idx + 1);
			if (gy > 0)      Try(idx - SimW);
			if (gy < SimH-1) Try(idx + SimW);
		}
		if (gasCells.Count == 0) return;
		foreach (var (gx, gy) in gasCells) { Grid[gy*SimW+gx]=(byte)Cell.Air; Flow[gy*SimW+gx]=0; WakeNeighbors(gx, gy); }
		long sx = 0, sy = 0;
		foreach (var (gx, gy) in gasCells) { sx += gx; sy += gy; }
		int cx = (int)(sx / gasCells.Count), cy = (int)(sy / gasCells.Count);
		int radius = Math.Clamp((int)(10 + MathF.Sqrt(gasCells.Count) * 2.2f), 10, 36);
		Explode(cx, cy, radius);
		// Scatter lava sparks for visual impact
		int sparks = Math.Clamp(gasCells.Count / 4, 5, 18);
		for (int k = 0; k < sparks; k++)
		{
			float angle = k * MathF.PI * 2f / sparks + _rng.NextSingle() * 0.6f;
			float r     = (0.25f + _rng.NextSingle() * 0.55f) * radius;
			int   fx    = cx + (int)(MathF.Cos(angle) * r);
			int   fy    = cy + (int)(MathF.Sin(angle) * r);
			if (!InBounds(fx, fy)) continue;
			int fi = fy * SimW + fx;
			if (Grid[fi] != (byte)Cell.Air) continue;
			Grid[fi]  = (byte)Cell.Lava;
			float spd = 3f + _rng.NextSingle() * 5f;
			VelX[fi]  = MathF.Cos(angle) * spd;
			VelY[fi]  = MathF.Sin(angle) * spd - 2.5f;
		}
	}

	public void Explode(int cx, int cy, int radius)
	{
		int r2 = radius * radius, innerR2 = (radius * 3 / 8) * (radius * 3 / 8);
		for (int y = Math.Max(cy-radius,0); y < Math.Min(cy+radius+1,SimH); y++)
		for (int x = Math.Max(cx-radius,0); x < Math.Min(cx+radius+1,SimW); x++)
		{
			int dx = x-cx, dy = y-cy, distSq = dx*dx + dy*dy;
			if (distSq > r2) continue;
			int i = y * SimW + x;
			if (Grid[i] == (byte)Cell.Air) continue;
			if (Pinned[i] != 0) continue;
			if (distSq <= innerR2)
			{ Grid[i]=(byte)Cell.Air; Flow[i]=0; VelX[i]=0; VelY[i]=0; WakeNeighbors(x, y); }
			else
			{
				float dist = MathF.Sqrt(distSq), falloff = 1f - dist / radius;
				float speed = 4f + 9f * falloff;
				VelX[i] = dx / dist * speed; VelY[i] = dy / dist * speed;
				WakeNeighbors(x, y);
			}
		}
		PendingExplosions.Add((cx, cy, radius));
	}

	// Pass A — productive drop search. Walks up to WaterSpreadDist cells in `dir`,
	// dropping into the first air gap found directly below the walk path. Returns
	// true if the cell moved. Used by surface water for fast basin-filling.
	private bool TryDropSearch(int x, int y, int dir)
	{
		int maxDist = Math.Max(1, WaterSpreadDist);
		for (int step = 1; step <= maxDist; step++)
		{
			int nx = x + dir * step;
			if (nx < 0 || nx >= SimW) break;
			if (GetCell(nx, y) != (byte)Cell.Air) break;
			if (y + 1 < SimH && GetCell(nx, y + 1) == (byte)Cell.Air)
			{
				Swap(x, y, nx, y + 1);
				return true;
			}
		}
		return false;
	}

	// Pass B — deterministic lateral spread for surface water with a clear
	// gradient. The cell walks up to WaterSpreadDist cells toward the side
	// whose immediate neighbor is air (the other side blocked by water/stone).
	// Skips entirely when both sides are air or both are blocked:
	//   both blocked → caller settles
	//   both air     → no clear gradient; moving would just oscillate (parity
	//                  tiebreak picks left this tick, right the next), so we
	//                  also settle. Pass A handles flattening of peaks if a
	//                  drop exists within WaterSpreadDist.
	private bool TrySpreadLateral(int x, int y)
	{
		bool leftAir  = x > 0          && GetCell(x - 1, y) == (byte)Cell.Air;
		bool rightAir = x < SimW - 1   && GetCell(x + 1, y) == (byte)Cell.Air;
		if (leftAir == rightAir) return false; // both air or both blocked

		int dir = leftAir ? -1 : 1;
		int maxDist = Math.Max(1, WaterSpreadDist);
		int prevX = x;
		for (int step = 1; step <= maxDist; step++)
		{
			int nx = x + dir * step;
			if (nx < 0 || nx >= SimW) break;
			if (GetCell(nx, y) != (byte)Cell.Air) break;
			prevX = nx;
		}
		if (prevX != x)
		{
			Swap(x, y, prevX, y);
			return true;
		}
		return false;
	}

	private void UpdateWater(int x, int y)
	{
		int i = y * SimW + x;
		ReadOnlySpan<int> ddx4 = stackalloc[] { 0, 0, -1, 1 };
		ReadOnlySpan<int> ddy4 = stackalloc[] { -1, 1, 0, 0 };

		// LN2 contact: violent boil → ice + nitrogen gas
		for (int k = 0; k < 4; k++)
		{
			int nx = x + ddx4[k], ny = y + ddy4[k];
			if (!InBounds(nx, ny)) continue;
			int ni = ny * SimW + nx;
			if (Grid[ni] == (byte)Cell.LiquidNitrogen && _rng.NextSingle() < 0.5f)
			{
				Grid[i] = (byte)Cell.Ice;          Flow[i]  = 0;
				Grid[ni] = (byte)Cell.NitrogenGas; Flow[ni] = 0;
				_visited[i] = 1; _visited[ni] = 1;
				return;
			}
		}

		// Fall and spread (no direction memory — temp lives in Flow now). These
		// checks are cheap and always run, even for settled cells — so removing a
		// stone wall under a settled pool correctly drains it next tick.
		if (y+1 < SimH && GetCell(x, y+1) == (byte)Cell.Air) { Swap(x,y,x,y+1); return; }
		bool dl = x>0       && y+1<SimH && GetCell(x-1,y+1)==(byte)Cell.Air;
		bool dr = x<SimW-1  && y+1<SimH && GetCell(x+1,y+1)==(byte)Cell.Air;
		if      (dl && dr) { if (_rng.NextSingle()<0.5f) Swap(x,y,x-1,y+1); else Swap(x,y,x+1,y+1); return; }
		else if (dl) { Swap(x,y,x-1,y+1); return; }
		else if (dr) { Swap(x,y,x+1,y+1); return; }

		// Surface vs interior split.
		// Interior cells (water directly above) never drive horizontal flow; they
		// settle immediately and only wake on neighbor changes. Surface cells
		// (air above) drive leveling via the two passes below.
		bool isSurface = y == 0 || GetCell(x, y - 1) != (byte)Cell.Water;

		if (!isSurface)
		{
			Settled[i] = 1;
		}
		else if (Settled[i] == 0)
		{
			// Pass A — fast deep-drop search. Walk in both directions looking for
			// an air gap to drop into. Tick parity picks which side to try first
			// so symmetric drops on either side don't always favor the same one.
			int firstDir = _flip ? -1 : 1;
			if (TryDropSearch(x, y, firstDir))  return;
			if (TryDropSearch(x, y, -firstDir)) return;

			// Pass B — deterministic single-cell lateral spread.
			if (TrySpreadLateral(x, y)) return;

			// Nothing productive available — settle. Surface cells re-wake when
			// any neighbor changes (Swap/SetCell/Explode call WakeNeighbors).
			Settled[i] = 1;
		}

		// Stationary — conduct temperature
		int temp = Flow[i];
		int delta = 0;
		for (int k = 0; k < 4; k++)
		{
			int nx = x + ddx4[k], ny = y + ddy4[k];
			if (!InBounds(nx, ny)) continue;
			int ni = ny * SimW + nx;
			byte nc = Grid[ni]; int nf = Flow[ni];
			if      (nc == (byte)Cell.Water)  delta += (nf - temp) / 6;
			else if (nc == (byte)Cell.Copper) delta += (nf - temp) / 3;
			else if (nc == (byte)Cell.Ice)    delta -= 12;
			else if (nc == (byte)Cell.Fire)   delta += 25;
			else if (nc == (byte)Cell.Lava)   delta += 35;
		}
		delta += (128 - temp) / 20; // drift back toward room temp
		temp = Math.Clamp(temp + delta, 0, 255);
		Flow[i] = (byte)temp;

		if (temp <= 8  && _rng.NextSingle() < 0.10f) { Grid[i]=(byte)Cell.Ice;   Flow[i]=0; _visited[i]=1; }
		else if (temp >= 220 && _rng.NextSingle() < 0.04f) { Grid[i]=(byte)Cell.Steam; Flow[i]=0; _visited[i]=1; }
	}

	private void UpdateFood(int x, int y)
	{
		if (y+1 < SimH && Grid[(y+1)*SimW+x]==(byte)Cell.Air) Swap(x,y,x,y+1);
	}

	private void UpdateSteam(int x, int y)
	{
		if (_rng.NextSingle() < 0.0025f) { int i=y*SimW+x; Grid[i]=(byte)Cell.Air; Flow[i]=0; return; }
		if (y == 0) { int i=y*SimW+x; Grid[i]=(byte)Cell.Air; Flow[i]=0; return; }
		byte above = GetCell(x, y-1);
		if (above==(byte)Cell.Air||above==(byte)Cell.Water) { Swap(x,y,x,y-1); return; }
		bool ul = x>0       && (GetCell(x-1,y-1)==(byte)Cell.Air||GetCell(x-1,y-1)==(byte)Cell.Water);
		bool ur = x<SimW-1  && (GetCell(x+1,y-1)==(byte)Cell.Air||GetCell(x+1,y-1)==(byte)Cell.Water);
		if (ul&&ur) { if(_rng.NextSingle()<0.5f) Swap(x,y,x-1,y-1); else Swap(x,y,x+1,y-1); return; }
		if (ul) { Swap(x,y,x-1,y-1); return; }
		if (ur) { Swap(x,y,x+1,y-1); return; }
		int dir = _rng.NextSingle()<0.5f?-1:1;
		int sx = x+dir;
		if (sx>=0&&sx<SimW&&(GetCell(sx,y)==(byte)Cell.Air||GetCell(sx,y)==(byte)Cell.Water))
			Swap(x,y,sx,y);
	}

	// ── Flammability helpers ──────────────────────────────────────────────────

	private bool IsFlammable(byte cell) =>
		cell == (byte)Cell.Wood  ||
		cell == (byte)Cell.Bark  ||
		cell == (byte)Cell.Leaves ||
		cell == (byte)Cell.Grass;

	private void IgniteCell(int x, int y)
	{
		if (!InBounds(x, y)) return;
		int i = y * SimW + x;
		if (Pinned[i] != 0) return;
		byte was = Grid[i];
		Grid[i] = (byte)Cell.Fire;
		// Bark and wood burn longer so fire persists on the structure
		Flow[i] = was == (byte)Cell.Bark || was == (byte)Cell.Wood
			? (byte)Math.Min(255, FireBaseTicks * 3 + _rng.Next(FireBaseTicks * 2))
			: (byte)(FireBaseTicks + _rng.Next(FireBaseTicks));
		VelX[i] = 0; VelY[i] = 0;
		_visited[i] = 1;
	}

	public void SetFire(int x, int y)
	{
		if (!InBounds(x, y)) return;
		int i = y * SimW + x;
		Grid[i] = (byte)Cell.Fire;
		Flow[i] = (byte)(FireBaseTicks + _rng.Next(FireBaseTicks));
		VelX[i] = 0; VelY[i] = 0;
		RenderDirty = true;
	}

	private void LavaIgnitesFlammables(int x, int y)
	{
		ReadOnlySpan<int> ddx = stackalloc[] { 0, 0, -1, 1 };
		ReadOnlySpan<int> ddy = stackalloc[] { -1, 1, 0, 0 };
		for (int k = 0; k < 4; k++)
		{
			int nx = x + ddx[k], ny = y + ddy[k];
			if (!InBounds(nx, ny)) continue;
			if (IsFlammable(Grid[ny * SimW + nx]) && _rng.NextSingle() < 0.03f)
				IgniteCell(nx, ny);
		}
	}

	// ── Dirt ──────────────────────────────────────────────────────────────────

	private void UpdateDirt(int x, int y)
	{
		if (y + 1 >= SimH) return;
		if (SandCanFallInto(GetCell(x, y + 1))) { Swap(x, y, x, y + 1); return; }
		if (_rng.NextSingle() > 0.30f) return; // clumpy: rarely slides diagonally
		bool lo = x > 0        && SandCanFallInto(GetCell(x - 1, y + 1));
		bool ro = x < SimW - 1 && SandCanFallInto(GetCell(x + 1, y + 1));
		if      (lo && ro) { if (_rng.NextSingle() < 0.5f) Swap(x, y, x - 1, y + 1); else Swap(x, y, x + 1, y + 1); }
		else if (lo) Swap(x, y, x - 1, y + 1);
		else if (ro) Swap(x, y, x + 1, y + 1);
	}

	// ── Seeds ─────────────────────────────────────────────────────────────────

	private void UpdateGrassSeed(int x, int y)
	{
		if (y + 1 >= SimH) return;
		byte below = GetCell(x, y + 1);
		if (SandCanFallInto(below)) { Swap(x, y, x, y + 1); return; }
		bool lo = x > 0        && SandCanFallInto(GetCell(x - 1, y + 1));
		bool ro = x < SimW - 1 && SandCanFallInto(GetCell(x + 1, y + 1));
		if      (lo && ro) { if (_rng.NextSingle() < 0.5f) Swap(x, y, x - 1, y + 1); else Swap(x, y, x + 1, y + 1); return; }
		else if (lo)       { Swap(x, y, x - 1, y + 1); return; }
		else if (ro)       { Swap(x, y, x + 1, y + 1); return; }
		// Stationary on dirt → sprout; anywhere else → wither slowly
		if (below == (byte)Cell.Dirt && _rng.NextSingle() < GrassSeedRate)
			SetCell(x, y, (int)Cell.Grass);
		else if (below != (byte)Cell.Dirt && _rng.NextSingle() < 0.005f)
			SetCell(x, y, (int)Cell.Air);
	}

	private void UpdateTreeSeed(int x, int y)
	{
		if (y + 1 >= SimH) return;
		byte below = GetCell(x, y + 1);
		if (SandCanFallInto(below)) { Swap(x, y, x, y + 1); return; }
		bool lo = x > 0        && SandCanFallInto(GetCell(x - 1, y + 1));
		bool ro = x < SimW - 1 && SandCanFallInto(GetCell(x + 1, y + 1));
		if      (lo && ro) { if (_rng.NextSingle() < 0.5f) Swap(x, y, x - 1, y + 1); else Swap(x, y, x + 1, y + 1); return; }
		else if (lo)       { Swap(x, y, x - 1, y + 1); return; }
		else if (ro)       { Swap(x, y, x + 1, y + 1); return; }
		// Stationary on dirt/grass → grow; anywhere else → wither
		bool onSoil = below == (byte)Cell.Dirt || below == (byte)Cell.Grass;
		if (onSoil && _rng.NextSingle() < TreeSeedRate)
			GrowTree(x, y);
		else if (!onSoil && _rng.NextSingle() < 0.005f)
			SetCell(x, y, (int)Cell.Air);
	}

	// ── Grass ─────────────────────────────────────────────────────────────────

	private void UpdateGrass(int x, int y)
	{
		if (y + 1 >= SimH) return;
		byte below  = GetCell(x, y + 1);
		byte below2 = y + 2 < SimH ? GetCell(x, y + 2) : (byte)Cell.Stone;
		bool isSurface = below == (byte)Cell.Dirt;
		bool isBlade1  = below == (byte)Cell.Grass && below2 == (byte)Cell.Dirt;
		if (!isSurface && !isBlade1) return;

		if (isSurface)
		{
			for (int dx = -1; dx <= 1; dx += 2)
			{
				int nx = x + dx;
				if (!InBounds(nx, y) || y <= 0) continue;
				if (GetCell(nx, y) == (byte)Cell.Dirt && GetCell(nx, y - 1) == (byte)Cell.Air
					&& _rng.NextSingle() < 0.0008f)
				{
					int ni = y * SimW + nx;
					Grid[ni] = (byte)Cell.Grass;
					_visited[ni] = 1;
				}
			}
		}
		if (y > 0 && GetCell(x, y - 1) == (byte)Cell.Air && _rng.NextSingle() < 0.0006f)
		{
			int ni = (y - 1) * SimW + x;
			Grid[ni] = (byte)Cell.Grass;
			_visited[ni] = 1;
		}
	}

	// ── Tree growth ───────────────────────────────────────────────────────────

	private void GrowTree(int sx, int sy)
	{
		int height  = 8 + _rng.Next(8);
		int nBranch = 2 + _rng.Next(3);

		for (int h = 0; h <= height; h++)
		{
			int ty = sy - h;
			if (!InBounds(sx, ty)) break;
			int ti = ty * SimW + sx;
			if (Grid[ti] == (byte)Cell.Air || Grid[ti] == (byte)Cell.Grass ||
				Grid[ti] == (byte)Cell.GrassSeed || Grid[ti] == (byte)Cell.TreeSeed)
			{ Grid[ti] = (byte)Cell.Bark; Pinned[ti] = 1; Flow[ti] = 0; }
		}

		var usedH = new HashSet<int>();
		for (int b = 0; b < nBranch; b++)
		{
			int bh = 2 + _rng.Next(height - 3);
			for (int tries = 0; tries < 8 && !usedH.Add(bh); tries++)
				bh = 2 + _rng.Next(height - 3);
			int by  = sy - bh;
			int dir = _rng.NextSingle() < 0.5f ? -1 : 1;
			int len = 3 + _rng.Next(4);
			for (int l = 1; l <= len; l++)
			{
				int bx = sx + dir * l;
				if (!InBounds(bx, by)) break;
				int bi = by * SimW + bx;
				if (Grid[bi] == (byte)Cell.Air || Grid[bi] == (byte)Cell.Grass)
				{ Grid[bi] = (byte)Cell.Bark; Pinned[bi] = 1; Flow[bi] = 0; }
			}
			PlaceLeafBlob(sx + dir * (len + 1), by, 3 + _rng.Next(2));
		}
		PlaceLeafBlob(sx, sy - height, 4 + _rng.Next(3));
		SetCell(sx, sy, (int)Cell.Bark);
		Pinned[sy * SimW + sx] = 1;
	}

	private void PlaceLeafBlob(int cx, int cy, int r)
	{
		for (int dy = -r; dy <= r; dy++)
		for (int dx = -r; dx <= r; dx++)
		{
			if (dx * dx + dy * dy > r * r + r) continue;
			int lx = cx + dx, ly = cy + dy;
			if (!InBounds(lx, ly)) continue;
			int li = ly * SimW + lx;
			if (Grid[li] == (byte)Cell.Air || Grid[li] == (byte)Cell.Grass)
			{ Grid[li] = (byte)Cell.Leaves; Flow[li] = 0; }
		}
	}

	// ── Fire ──────────────────────────────────────────────────────────────────

	private void UpdateFire(int x, int y)
	{
		int i = y * SimW + x;
		byte life = Flow[i];

		// Decrement lifetime first so Swap carries the updated value
		if (life == 0) { Grid[i] = (byte)Cell.Air; Flow[i] = 0; _visited[i] = 1; return; }
		Flow[i] = (byte)(life - 1);

		ReadOnlySpan<int> ddx = stackalloc[] { 0, 0, -1, 1 };
		ReadOnlySpan<int> ddy = stackalloc[] { -1, 1, 0, 0 };

		// Water/LN2 extinguishes fire
		for (int k = 0; k < 4; k++)
		{
			int nx = x + ddx[k], ny = y + ddy[k];
			if (!InBounds(nx, ny)) continue;
			int ni = ny * SimW + nx;
			if (Grid[ni] == (byte)Cell.Water)
			{
				Grid[i] = (byte)Cell.Steam; Flow[i] = 0;
				Grid[ni] = (byte)Cell.Air;  Flow[ni] = 0;
				_visited[i] = 1; return;
			}
			if (Grid[ni] == (byte)Cell.LiquidNitrogen)
			{
				Grid[i]  = (byte)Cell.Air;         Flow[i]  = 0;
				Grid[ni] = (byte)Cell.NitrogenGas; Flow[ni] = 0;
				_visited[i] = 1; return;
			}
		}

		// Ignite neighbours — 8-directional for better spread across dense structures
		ReadOnlySpan<int> ddx8 = stackalloc[] { 0, 0, -1, 1, -1, 1, -1, 1 };
		ReadOnlySpan<int> ddy8 = stackalloc[] { -1, 1, 0, 0, -1, -1,  1, 1 };
		for (int k = 0; k < 8; k++)
		{
			int nx = x + ddx8[k], ny = y + ddy8[k];
			if (!InBounds(nx, ny)) continue;
			if (IsFlammable(Grid[ny * SimW + nx]) && _rng.NextSingle() < FireIgniteChance)
				IgniteCell(nx, ny);
		}

		// Emit smoke upward
		if (y > 0 && GetCell(x, y - 1) == (byte)Cell.Air && _rng.NextSingle() < 0.15f)
		{
			int si = (y - 1) * SimW + x;
			Grid[si] = (byte)Cell.Smoke;
			Flow[si]  = (byte)(30 + _rng.Next(35));
			_visited[si] = 1;
		}

		// Cling to fuel — drift much slower when adjacent to something burning
		bool nearFuel = false;
		for (int k = 0; k < 4; k++)
		{
			int nx = x + ddx[k], ny = y + ddy[k];
			if (InBounds(nx, ny) && IsFlammable(Grid[ny * SimW + nx]))
				{ nearFuel = true; break; }
		}
		float driftChance = nearFuel ? 0.18f : 0.70f;

		// Drift upward
		if (y == 0) { Grid[i] = (byte)Cell.Air; Flow[i] = 0; return; }
		if (GetCell(x, y - 1) == (byte)Cell.Air && _rng.NextSingle() < driftChance)
			{ Swap(x, y, x, y - 1); return; }
		bool ul = x > 0        && GetCell(x - 1, y - 1) == (byte)Cell.Air;
		bool ur = x < SimW - 1 && GetCell(x + 1, y - 1) == (byte)Cell.Air;
		if      (ul && ur) { if (_rng.NextSingle() < 0.5f) Swap(x, y, x-1, y-1); else Swap(x, y, x+1, y-1); }
		else if (ul)       Swap(x, y, x-1, y-1);
		else if (ur)       Swap(x, y, x+1, y-1);
	}

	// ── Smoke ─────────────────────────────────────────────────────────────────

	private void UpdateSmoke(int x, int y)
	{
		int i = y * SimW + x;
		byte life = Flow[i];
		if (life == 0) { Grid[i] = (byte)Cell.Air; Flow[i] = 0; _visited[i] = 1; return; }
		Flow[i] = (byte)(life - 1);
		if (y == 0) { Grid[i] = (byte)Cell.Air; Flow[i] = 0; return; }
		if (GetCell(x, y - 1) == (byte)Cell.Air) { Swap(x, y, x, y - 1); return; }
		bool ul = x > 0        && GetCell(x - 1, y - 1) == (byte)Cell.Air;
		bool ur = x < SimW - 1 && GetCell(x + 1, y - 1) == (byte)Cell.Air;
		if      (ul && ur) { if (_rng.NextSingle() < 0.5f) Swap(x, y, x-1, y-1); else Swap(x, y, x+1, y-1); return; }
		else if (ul)       { Swap(x, y, x-1, y-1); return; }
		else if (ur)       { Swap(x, y, x+1, y-1); return; }
		int dir = _rng.NextSingle() < 0.5f ? -1 : 1;
		int sx2 = x + dir;
		if (sx2 >= 0 && sx2 < SimW && GetCell(sx2, y) == (byte)Cell.Air) Swap(x, y, sx2, y);
	}

	// ── Lava + LN2 ───────────────────────────────────────────────────────────

	private bool ReactLavaWithLN2(int x, int y)
	{
		int i = y * SimW + x;
		ReadOnlySpan<int> dx = stackalloc[] { 0, 0, -1, 1 };
		ReadOnlySpan<int> dy = stackalloc[] { -1, 1, 0, 0 };
		for (int k = 0; k < 4; k++)
		{
			int nx = x + dx[k], ny = y + dy[k];
			if (!InBounds(nx, ny)) continue;
			int ni = ny * SimW + nx;
			if (Grid[ni] == (byte)Cell.LiquidNitrogen)
			{
				Grid[i]  = (byte)Cell.Stone;       Flow[i]  = 0;
				Grid[ni] = (byte)Cell.NitrogenGas; Flow[ni] = 0;
				_visited[i] = 1; _visited[ni] = 1;
				return true;
			}
		}
		return false;
	}

	// ── Liquid Nitrogen ───────────────────────────────────────────────────────

	private static bool LN2CanFallInto(byte c) =>
		c == (byte)Cell.Air || c == (byte)Cell.Gas ||
		c == (byte)Cell.NitrogenGas || c == (byte)Cell.Steam;

	private void UpdateLiquidNitrogen(int x, int y)
	{
		int i = y * SimW + x;
		ReadOnlySpan<int> ddx4 = stackalloc[] { 0, 0, -1, 1 };
		ReadOnlySpan<int> ddy4 = stackalloc[] { -1, 1, 0, 0 };

		// React with neighbours
		for (int k = 0; k < 4; k++)
		{
			int nx = x + ddx4[k], ny = y + ddy4[k];
			if (!InBounds(nx, ny)) continue;
			int ni = ny * SimW + nx; byte nc = Grid[ni];
			// Fire: extinguish, LN2 boils to N2
			if (nc == (byte)Cell.Fire)
			{ Grid[i]=(byte)Cell.NitrogenGas; Flow[i]=0; Grid[ni]=(byte)Cell.Air; Flow[ni]=0; _visited[i]=1; return; }
			// Steam: freeze steam to ice, LN2 boils
			if (nc == (byte)Cell.Steam && _rng.NextSingle() < 0.4f)
			{ Grid[i]=(byte)Cell.NitrogenGas; Flow[i]=0; Grid[ni]=(byte)Cell.Ice; Flow[ni]=0; _visited[i]=1; return; }
		}

		// Evaporate at surface (air above)
		if (y > 0 && GetCell(x, y-1) == (byte)Cell.Air && _rng.NextSingle() < 0.003f)
		{ Grid[i]=(byte)Cell.NitrogenGas; Flow[i]=0; _visited[i]=1; return; }

		// Flow like water but only into LN2CanFallInto
		byte below = y+1 < SimH ? GetCell(x, y+1) : (byte)Cell.Stone;
		if (LN2CanFallInto(below)) { Swap(x, y, x, y+1); return; }
		bool dl = x>0       && y+1<SimH && LN2CanFallInto(GetCell(x-1,y+1));
		bool dr = x<SimW-1  && y+1<SimH && LN2CanFallInto(GetCell(x+1,y+1));
		if      (dl && dr) { if (_rng.NextSingle()<0.5f) Swap(x,y,x-1,y+1); else Swap(x,y,x+1,y+1); return; }
		else if (dl) { Swap(x,y,x-1,y+1); return; }
		else if (dr) { Swap(x,y,x+1,y+1); return; }
		int dir1 = _rng.NextSingle()<0.5f?-1:1;
		if (x+dir1>=0 && x+dir1<SimW && LN2CanFallInto(GetCell(x+dir1,y))) { Swap(x,y,x+dir1,y); return; }
		if (x-dir1>=0 && x-dir1<SimW && LN2CanFallInto(GetCell(x-dir1,y))) { Swap(x,y,x-dir1,y); }
	}

	// ── Nitrogen Gas ──────────────────────────────────────────────────────────

	private void UpdateNitrogenGas(int x, int y)
	{
		int i = y * SimW + x;
		if (y == 0) { Grid[i]=(byte)Cell.Air; Flow[i]=0; return; }
		byte above = GetCell(x, y-1);
		if (above == (byte)Cell.Air) { Swap(x,y,x,y-1); return; }
		bool ul = x>0       && GetCell(x-1,y-1)==(byte)Cell.Air;
		bool ur = x<SimW-1  && GetCell(x+1,y-1)==(byte)Cell.Air;
		if      (ul&&ur) { if(_rng.NextSingle()<0.5f) Swap(x,y,x-1,y-1); else Swap(x,y,x+1,y-1); return; }
		else if (ul)     { Swap(x,y,x-1,y-1); return; }
		else if (ur)     { Swap(x,y,x+1,y-1); return; }
		int ddir = _rng.NextSingle()<0.5f?-1:1;
		int sx3 = x+ddir;
		if (sx3>=0&&sx3<SimW&&GetCell(sx3,y)==(byte)Cell.Air) Swap(x,y,sx3,y);
	}

	// ── Ice ───────────────────────────────────────────────────────────────────

	private void UpdateIce(int x, int y)
	{
		int i = y * SimW + x;
		ReadOnlySpan<int> ddx4 = stackalloc[] { 0, 0, -1, 1 };
		ReadOnlySpan<int> ddy4 = stackalloc[] { -1, 1, 0, 0 };
		float meltChance = 0.0003f;

		for (int k = 0; k < 4; k++)
		{
			int nx = x+ddx4[k], ny = y+ddy4[k];
			if (!InBounds(nx,ny)) continue;
			int ni = ny*SimW+nx; byte nc = Grid[ni];
			if (nc==(byte)Cell.Fire)   meltChance = MathF.Max(meltChance, 0.15f);
			if (nc==(byte)Cell.Lava)   meltChance = MathF.Max(meltChance, 0.20f);
			if (nc==(byte)Cell.Copper && Flow[ni] > 128)
				meltChance = MathF.Max(meltChance, (Flow[ni]-128)/127f * 0.10f);
			// Spread freeze to adjacent cold water
			if (nc==(byte)Cell.Water && Flow[ni] <= 64 && _rng.NextSingle() < 0.08f)
			{ Grid[ni]=(byte)Cell.Ice; Flow[ni]=0; _visited[ni]=1; }
		}
		if (_rng.NextSingle() < meltChance)
		{ Grid[i]=(byte)Cell.Water; Flow[i]=60; _visited[i]=1; } // melt as cold water
	}

	public void ApplyForce(int cx, int cy, int radius, int strength)
	{
		int r2 = radius * radius;
		for (int y = Math.Max(cy-radius,0); y < Math.Min(cy+radius+1,SimH); y++)
		for (int x = Math.Max(cx-radius,0); x < Math.Min(cx+radius+1,SimW); x++)
		{
			int dx = x-cx, dy = y-cy, distSq = dx*dx+dy*dy;
			if (distSq > r2) continue;
			int i = y*SimW+x;
			byte g = Grid[i];
			if (g==(byte)Cell.Air||g==(byte)Cell.Copper||g==(byte)Cell.Battery||g==(byte)Cell.Wood) continue;
			if (Pinned[i] != 0) continue;
			float dist = MathF.Sqrt(MathF.Max(distSq,0.25f));
			float falloff = 1f - dist/radius;
			float speed = (0.5f + 0.6f*strength) * MathF.Max(falloff, 0.1f);
			VelX[i] += dx/dist*speed; VelY[i] += dy/dist*speed - falloff*1.2f;
		}
	}
}

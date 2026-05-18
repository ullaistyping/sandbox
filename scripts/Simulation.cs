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
		Gas = 5, Food = 6, Copper = 7, Steam = 8, Battery = 9, Wood = 10, Mirror = 11
	}

	public byte[]  Grid;
	public byte[]  Flow;     // water: flow dir; copper: heat 0-255
	public byte[]  Electric; // 1 if copper cell is electrified this tick
	public byte[]  Pinned;   // 1 = cell is pinned (never moves)
	public float[] VelX;
	public float[] VelY;
	private byte[] _visited;
	private bool   _flip;
	private readonly Random     _rng           = new Random();
	private readonly Stack<int> _electricStack = new Stack<int>();

	public int CopperBoilThreshold = 100;
	public int CopperGasThreshold  = 200;

	public Simulation()
	{
		int size = SimW * SimH;
		Grid     = new byte[size];
		Flow     = new byte[size];
		Electric = new byte[size];
		Pinned   = new byte[size];
		VelX     = new float[size];
		VelY     = new float[size];
		_visited = new byte[size];
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
		if (type != (int)Cell.Water) Flow[i] = 0;
		VelX[i] = 0; VelY[i] = 0;
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

	// ── Velocity cells ────────────────────────────────────────────────────────

	private void UpdateVelocityCells()
	{
		const float gravity  = 0.30f;
		const float friction = 0.99f;
		const float dampCol  = 0.30f;
		const float stopThr  = 0.30f;
		const int   maxSteps = 8;

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
			case Cell.Copper: UpdateCopper(x, y); break;
			case Cell.Steam:  UpdateSteam(x, y);  break;
			// Stone, Battery, Wood: handled externally or static
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
		foreach (var (gx, gy) in gasCells) { Grid[gy*SimW+gx]=(byte)Cell.Air; Flow[gy*SimW+gx]=0; }
		long sx = 0, sy = 0;
		foreach (var (gx, gy) in gasCells) { sx += gx; sy += gy; }
		int cx = (int)(sx / gasCells.Count), cy = (int)(sy / gasCells.Count);
		int radius = Math.Clamp((int)(10 + MathF.Sqrt(gasCells.Count) * 2.2f), 10, 36);
		Explode(cx, cy, radius);
	}

	public void Explode(int cx, int cy, int radius)
	{
		int r2 = radius * radius, innerR2 = (radius / 2) * (radius / 2);
		for (int y = Math.Max(cy-radius,0); y < Math.Min(cy+radius+1,SimH); y++)
		for (int x = Math.Max(cx-radius,0); x < Math.Min(cx+radius+1,SimW); x++)
		{
			int dx = x-cx, dy = y-cy, distSq = dx*dx + dy*dy;
			if (distSq > r2) continue;
			int i = y * SimW + x;
			if (Grid[i] == (byte)Cell.Air) continue;
			if (Pinned[i] != 0) continue; // pinned cells survive explosions
			if (distSq <= innerR2)
			{ Grid[i]=(byte)Cell.Air; Flow[i]=0; VelX[i]=0; VelY[i]=0; }
			else
			{
				float dist = MathF.Sqrt(distSq), falloff = 1f - dist / radius;
				float speed = 1.5f + 5f * falloff;
				VelX[i] = dx / dist * speed; VelY[i] = dy / dist * speed;
			}
		}
	}

	private void UpdateWater(int x, int y)
	{
		int i = y * SimW + x; byte fd = Flow[i];
		if (y+1 < SimH && GetCell(x,y+1)==(byte)Cell.Air) { Swap(x,y,x,y+1); return; }
		if (y + 1 < SimH)
		{
			bool dl = x>0       && GetCell(x-1,y+1)==(byte)Cell.Air;
			bool dr = x<SimW-1  && GetCell(x+1,y+1)==(byte)Cell.Air;
			if (dl && dr)
			{
				if (fd==2||(fd==0&&_rng.NextSingle()<0.5f))
				{ Swap(x,y,x-1,y+1); Flow[(y+1)*SimW+x-1]=2; }
				else { Swap(x,y,x+1,y+1); Flow[(y+1)*SimW+x+1]=1; }
				return;
			}
			if (dl) { Swap(x,y,x-1,y+1); Flow[(y+1)*SimW+x-1]=2; return; }
			if (dr) { Swap(x,y,x+1,y+1); Flow[(y+1)*SimW+x+1]=1; return; }
		}
		int dir = fd==1?1:fd==2?-1:(_rng.NextSingle()<0.5f?1:-1);
		if (fd==0) Flow[i] = dir==1?(byte)1:(byte)2;
		int nx = x + dir;
		if (nx<0||nx>=SimW) { Flow[i]=dir==1?(byte)2:(byte)1; return; }
		byte nc = GetCell(nx, y);
		if (nc==(byte)Cell.Air) { Swap(x,y,nx,y); Flow[y*SimW+nx]=dir==1?(byte)1:(byte)2; }
		else if (nc!=(byte)Cell.Water) { Flow[i]=dir==1?(byte)2:(byte)1; }
		else
		{
			int onx = x - dir;
			if (onx>=0&&onx<SimW&&GetCell(onx,y)==(byte)Cell.Air)
			{ Swap(x,y,onx,y); Flow[y*SimW+onx]=dir==1?(byte)2:(byte)1; }
		}
	}

	private void UpdateFood(int x, int y)
	{
		if (y+1 < SimH && Grid[(y+1)*SimW+x]==(byte)Cell.Air) Swap(x,y,x,y+1);
	}

	private void UpdateCopper(int x, int y)
	{
		int i = y * SimW + x; int heat = Flow[i];
		ReadOnlySpan<int> ddx = stackalloc[] { 0, 0, -1, 1 };
		ReadOnlySpan<int> ddy = stackalloc[] { -1, 1, 0, 0 };
		bool nearLava = false; int maxNeighbor = 0;
		for (int k = 0; k < 4; k++)
		{
			int nx = x+ddx[k], ny = y+ddy[k];
			if (!InBounds(nx,ny)) continue;
			int ni = ny*SimW+nx; byte nc = Grid[ni];
			if (nc==(byte)Cell.Lava) nearLava = true;
			else if (nc==(byte)Cell.Copper && Flow[ni]>maxNeighbor) maxNeighbor=Flow[ni];
		}
		if (nearLava) heat = 255;
		else
		{
			if (maxNeighbor > heat) heat = Math.Min(255, heat + Math.Min(16, maxNeighbor-heat));
			heat = Math.Max(0, heat - 1);
		}
		if (heat >= CopperBoilThreshold)
			for (int k = 0; k < 4; k++)
			{
				int nx = x+ddx[k], ny = y+ddy[k];
				if (!InBounds(nx,ny)) continue;
				int ni = ny*SimW+nx;
				if (Grid[ni]==(byte)Cell.Water)
				{ Grid[ni]=(byte)Cell.Steam; Flow[ni]=0; _visited[ni]=1; heat=Math.Max(0,heat-8); }
			}
		if (heat >= CopperGasThreshold)
			for (int k = 0; k < 4; k++)
			{
				int nx = x+ddx[k], ny = y+ddy[k];
				if (!InBounds(nx,ny)) continue;
				if (Grid[ny*SimW+nx]==(byte)Cell.Gas)
				{ ExplodeGasPocket(nx,ny); heat=Math.Max(0,heat-50); break; }
			}
		Flow[i] = (byte)heat;
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
			float speed = (0.4f + 0.3f*strength) * MathF.Max(falloff, 0.1f);
			VelX[i] += dx/dist*speed; VelY[i] += dy/dist*speed - falloff*0.6f;
		}
	}
}

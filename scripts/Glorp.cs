using Godot;
using System;
using System.Collections.Generic;

public partial class Glorp : Node2D
{
	private const int   BodyPx       = 48;   // display diameter in screen pixels
	private const int   TexPx        = 12;   // texture resolution (upscaled 4x for pixel art look)
	public  const float SimR         = 6f;   // collision radius in sim units (public for hit-test in Main)
	private const float SenseRange      = 38f;
	private const float EatRange        = 4f;
	private const float TalkRange       = 22f;
	private const int   Scale           = 4;

	// Physics
	private const float Gravity         = 130f;   // sim units / s²
	private const float MaxFallSpeed    = 95f;    // sim units / s
	private const float MaxHorizSpeed   = 32f;    // sim units / s
	private const float HorizAccel      = 110f;   // sim units / s²
	private const float GroundFriction  = 4f;     // exponential decay constant
	private const float BounceRestitution = 0.18f;
	private const float SquishDecay     = 3.5f;   // squish units lost per second

	private const float HungerRate   = 5f;
	private const float ThirstRate   = 7f;
	private const float LonelyRate   = 4f;
	private const float HungerFill   = 55f;
	private const float ThirstFill   = 45f;
	private const float SocialGain   = 25f;
	private const float BubbleLife   = 3.5f;

	public Vector2 SimPos;
	public float Hunger   = 0f;
	public float Thirst   = 0f;
	public float Social   = 80f;
	public bool  Selected = false;

	private Vector2 _vel;          // AI intent direction (normalised)
	private float _physVelX;       // actual horizontal physics velocity
	private float _physVelY;       // actual vertical physics velocity (gravity)
	private bool  _isGrounded;
	private float _squishAmount;   // 0 = round, 1 = max squish on landing
	private float _rotation;       // rolling angle in radians
	private float _wanderTimer;
	private float _bubbleTimer;
	private string _bubbleText = "";

	private Simulation _sim;
	private List<Glorp> _allGlorps;
	private static Texture2D _bodyTex;

	// ── Init ─────────────────────────────────────────────────────────────────

	public void Init(Simulation sim, List<Glorp> allGlorps, Vector2 simPos)
	{
		_sim      = sim;
		_allGlorps = allGlorps;
		SimPos    = simPos;
		Position  = simPos * Scale;
		float ang = (float)GD.RandRange(0.0, Math.PI * 2.0);
		_vel = new Vector2(MathF.Cos(ang), MathF.Sin(ang));
		TextureFilter = TextureFilterEnum.Nearest; // keep pixels sharp when upscaled
		BuildTexture();
	}

	private static void BuildTexture()
	{
		if (_bodyTex != null) return;
		// Small texture upscaled 4x — each texel becomes a visible pixel block
		var img = Image.Create(TexPx, TexPx, false, Image.Format.Rgba8);
		float r = TexPx / 2f;
		for (int py = 0; py < TexPx; py++)
			for (int px = 0; px < TexPx; px++)
			{
				float dx = px - r + 0.5f, dy = py - r + 0.5f;
				float d  = MathF.Sqrt(dx * dx + dy * dy);
				if (d > r) { img.SetPixel(px, py, Colors.Transparent); continue; }
				float t = d / r; // 0 = centre, 1 = edge
				img.SetPixel(px, py, new Color(0.30f - 0.30f * t, 0.95f - 0.70f * t, 0.10f - 0.10f * t));
			}
		_bodyTex = ImageTexture.CreateFromImage(img);
	}

	// ── Update ───────────────────────────────────────────────────────────────

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		Hunger = MathF.Min(100, Hunger + HungerRate * dt);
		Thirst = MathF.Min(100, Thirst + ThirstRate * dt);

		// Social interaction with nearby Glorps
		bool nearFriend = false;
		foreach (var other in _allGlorps)
		{
			if (other == this) continue;
			if ((other.SimPos - SimPos).Length() < TalkRange)
			{
				nearFriend = true;
				Social = MathF.Min(100, Social + SocialGain * dt);
				if (_bubbleTimer <= 0 && other._bubbleTimer <= 0)
				{
					TriggerBubble();
					other.TriggerBubble();
				}
				break;
			}
		}
		if (!nearFriend)
			Social = MathF.Max(0, Social - LonelyRate * dt);

		// Pick movement target
		Vector2? target = null;
		bool seekFood  = Hunger > Thirst && Hunger > 45;
		bool seekWater = !seekFood && Thirst > 45;
		bool seekGlorp = !seekFood && !seekWater && Social < 35;

		if (seekFood)
			target = FindNearestCell((byte)Simulation.Cell.Food);
		if (seekWater && !target.HasValue)
			target = FindNearestCell((byte)Simulation.Cell.Water);
		if (seekGlorp && !target.HasValue)
			target = FindNearestGlorp();

		if (target.HasValue)
		{
			var diff = target.Value - SimPos;
			if (diff.LengthSquared() > 0.01f)
				_vel = diff.Normalized();

			// Eat / drink when close enough
			if (diff.Length() < EatRange)
			{
				int tx = (int)target.Value.X, ty = (int)target.Value.Y;
				byte tc = _sim.GetCell(tx, ty);
				if (tc == (byte)Simulation.Cell.Food)
				{
					_sim.SetCell(tx, ty, (int)Simulation.Cell.Air);
					Hunger = MathF.Max(0, Hunger - HungerFill);
				}
				else if (tc == (byte)Simulation.Cell.Water)
				{
					Thirst = MathF.Max(0, Thirst - ThirstFill);
				}
			}
		}
		else
		{
			_wanderTimer -= dt;
			if (_wanderTimer <= 0)
			{
				float ang = (float)GD.RandRange(-Math.PI, Math.PI);
				_vel = new Vector2(MathF.Cos(ang), MathF.Sin(ang));
				_wanderTimer = (float)GD.RandRange(0.8, 2.2);
			}
		}

		// ── Physics ──────────────────────────────────────────────────────────
		bool wasGrounded = _isGrounded;
		_isGrounded = CheckGrounded();

		// Gravity
		if (!_isGrounded)
			_physVelY = Math.Min(_physVelY + Gravity * dt, MaxFallSpeed);

		// Landing: squish proportional to impact speed, small bounce
		if (!wasGrounded && _isGrounded && _physVelY > 15f)
		{
			_squishAmount = Math.Clamp(_physVelY / 90f, 0.15f, 0.65f);
			_physVelY = -_physVelY * BounceRestitution;
		}
		else if (_isGrounded && _physVelY > 0)
		{
			_physVelY = 0;
		}

		// Drive horizontal velocity toward AI intent
		float intendedX = _vel.X * MaxHorizSpeed;
		float hDiff     = intendedX - _physVelX;
		_physVelX += Math.Sign(hDiff) * Math.Min(MathF.Abs(hDiff), HorizAccel * dt);

		// Ground friction (exponential decay)
		if (_isGrounded)
			_physVelX *= MathF.Exp(-GroundFriction * dt);

		// Integrate position
		SimPos = new Vector2(SimPos.X + _physVelX * dt, SimPos.Y + _physVelY * dt);

		// Horizontal collision — only hard solids (stone/lava) block; sand is soft so Glorps wade through it.
		// Narrower radius (75 %) and step-up (up to 3 px) so small bumps are climbed, not a wall.
		float hr = SimR * 0.75f;
		int   cy = (int)SimPos.Y;
		int   lx = (int)(SimPos.X - hr);
		int   rx = (int)(SimPos.X + hr);

		if (lx < 0 || IsHardSolid(_sim.GetCell(lx, cy)))
		{
			bool stepped = false;
			for (int s = 1; s <= 3; s++)
				if (!IsHardSolid(_sim.GetCell(lx, cy - s))) { SimPos = new Vector2(SimPos.X, SimPos.Y - s); stepped = true; break; }
			if (!stepped) { SimPos = new Vector2(Math.Max(hr, SimPos.X), SimPos.Y); _physVelX = MathF.Abs(_physVelX) * 0.25f; }
		}
		if (rx >= Simulation.SimW || IsHardSolid(_sim.GetCell(rx, cy)))
		{
			bool stepped = false;
			for (int s = 1; s <= 3; s++)
				if (!IsHardSolid(_sim.GetCell(rx, cy - s))) { SimPos = new Vector2(SimPos.X, SimPos.Y - s); stepped = true; break; }
			if (!stepped) { SimPos = new Vector2(Math.Min(Simulation.SimW - hr, SimPos.X), SimPos.Y); _physVelX = -MathF.Abs(_physVelX) * 0.25f; }
		}

		// Push the bottom edge of the Glorp above any walkable ground (sand included).
		// Iterates from the bottom pixel upward until the foot is clear.
		for (int push = 0; push < (int)(SimR + 3); push++)
		{
			int footY = (int)(SimPos.Y + SimR);
			if (footY >= Simulation.SimH || !IsWalkable(_sim.GetCell((int)SimPos.X, footY))) break;
			SimPos    = new Vector2(SimPos.X, SimPos.Y - 1f);
			_physVelY = Math.Min(_physVelY, 0f);
		}

		// Secondary: push centre out of hard solids (handles edge cases like explosions)
		for (int push = 0; push < (int)(SimR * 2 + 1); push++)
		{
			if (!IsHardSolid(_sim.GetCell((int)SimPos.X, (int)SimPos.Y))) break;
			SimPos    = new Vector2(SimPos.X, SimPos.Y - 1f);
			_physVelY = Math.Min(_physVelY, 0f);
		}

		// Hard boundary clamp
		SimPos = new Vector2(
			Math.Clamp(SimPos.X, SimR, Simulation.SimW - SimR),
			Math.Clamp(SimPos.Y, SimR, Simulation.SimH - SimR));

		// Rolling rotation: ω = v / r  (r in screen px = BodyPx/2)
		_rotation += _physVelX * Scale / (BodyPx / 2f) * dt;

		// Squish decay
		_squishAmount = MathF.Max(0, _squishAmount - SquishDecay * dt);

		Position = SimPos * Scale;
		if (_bubbleTimer > 0) _bubbleTimer -= dt;
		QueueRedraw();
	}

	// ── Helpers ──────────────────────────────────────────────────────────────

	private void TriggerBubble()
	{
		_bubbleText  = GetMoodText();
		_bubbleTimer = BubbleLife;
	}

	private string GetMoodText()
	{
		if (Hunger > 85 || Thirst > 85)               return ">:(";
		if (Hunger < 30 && Thirst < 30 && Social > 65) return ":)";
		if (Hunger > 65 || Thirst > 65 || Social < 20) return ":(";
		return ":/";
	}

	private Vector2? FindNearestCell(byte type)
	{
		int gx = (int)SimPos.X, gy = (int)SimPos.Y;
		int range = (int)SenseRange;
		float best = float.MaxValue;
		Vector2? result = null;
		for (int dy = -range; dy <= range; dy++)
			for (int dx = -range; dx <= range; dx++)
			{
				if (_sim.GetCell(gx + dx, gy + dy) != type) continue;
				float d = dx * dx + dy * dy;
				if (d < best) { best = d; result = new Vector2(gx + dx, gy + dy); }
			}
		return result;
	}

	private Vector2? FindNearestGlorp()
	{
		float best = SenseRange * SenseRange;
		Vector2? result = null;
		foreach (var g in _allGlorps)
		{
			if (g == this) continue;
			float d = (g.SimPos - SimPos).LengthSquared();
			if (d < best) { best = d; result = g.SimPos; }
		}
		return result;
	}

	private bool CheckGrounded()
	{
		int gy = (int)(SimPos.Y + SimR);
		if (gy >= Simulation.SimH) return true;
		for (int dx = -3; dx <= 3; dx++)
			if (IsWalkable(_sim.GetCell((int)SimPos.X + dx, gy)))
				return true;
		return false;
	}

	// Hard solid: stops horizontal movement and triggers push-out (stone, lava only)
	private static bool IsHardSolid(byte c) =>
		c == (byte)Simulation.Cell.Stone  ||
		c == (byte)Simulation.Cell.Lava   ||
		c == (byte)Simulation.Cell.Copper;

	// Walkable: Glorps can stand on it — includes soft sand for ground detection
	private static bool IsWalkable(byte c) =>
		IsHardSolid(c) || c == (byte)Simulation.Cell.Sand;

	// ── Drawing ──────────────────────────────────────────────────────────────

	public override void _Draw()
	{
		// Squish: horizontal expansion + vertical compression on landing
		float sx = 1f + _squishAmount * 0.38f;
		float sy = 1f - _squishAmount * 0.32f;

		// Apply rotation and squish for the body, reset for UI elements
		DrawSetTransform(Vector2.Zero, _rotation, new Vector2(sx, sy));
		DrawTextureRect(_bodyTex, new Rect2(-BodyPx / 2, -BodyPx / 2, BodyPx, BodyPx), false);
		DrawSetTransformMatrix(Transform2D.Identity);

		if (_bubbleTimer > 0)
			DrawBubble();

		if (Selected)
			DrawSelectionFrame();
	}

	private void DrawSelectionFrame()
	{
		const float fr  = 30f;  // half-size of the frame square
		const float arm = 10f;  // length of each L-corner stroke
		const float lw  = 2f;   // line width

		// Top-left  ⌐
		DrawLine(new Vector2(-fr, -fr), new Vector2(-fr + arm, -fr), Colors.White, lw);
		DrawLine(new Vector2(-fr, -fr), new Vector2(-fr, -fr + arm), Colors.White, lw);
		// Top-right  ¬
		DrawLine(new Vector2( fr, -fr), new Vector2( fr - arm, -fr), Colors.White, lw);
		DrawLine(new Vector2( fr, -fr), new Vector2( fr, -fr + arm), Colors.White, lw);
		// Bottom-left  L
		DrawLine(new Vector2(-fr,  fr), new Vector2(-fr + arm,  fr), Colors.White, lw);
		DrawLine(new Vector2(-fr,  fr), new Vector2(-fr,  fr - arm), Colors.White, lw);
		// Bottom-right  J
		DrawLine(new Vector2( fr,  fr), new Vector2( fr - arm,  fr), Colors.White, lw);
		DrawLine(new Vector2( fr,  fr), new Vector2( fr,  fr - arm), Colors.White, lw);

		// Stats: remapped so 100 = fully satisfied, 0 = desperate
		int h = Math.Clamp((int)(100 - Hunger), 0, 100);
		int t = Math.Clamp((int)(100 - Thirst), 0, 100);
		int s = Math.Clamp((int)Social,          0, 100);

		// Dark backing + three labelled values below the frame
		float textX  = -fr;
		float textY  = fr + 6f;
		float textW  = fr * 2;
		DrawRect(new Rect2(textX, textY, textW, 52), new Color(0, 0, 0, 0.65f), true);
		DrawString(ThemeDB.FallbackFont, new Vector2(textX + 4, textY + 15), $"H: {h}", HorizontalAlignment.Left, (int)textW, 13, Colors.White);
		DrawString(ThemeDB.FallbackFont, new Vector2(textX + 4, textY + 32), $"T: {t}", HorizontalAlignment.Left, (int)textW, 13, Colors.White);
		DrawString(ThemeDB.FallbackFont, new Vector2(textX + 4, textY + 49), $"S: {s}", HorizontalAlignment.Left, (int)textW, 13, Colors.White);
	}

	private void DrawBubble()
	{
		const float bw = 70f, bh = 40f;
		float bBottom = -(BodyPx / 2 + 14);
		float bTop    = bBottom - bh;
		float bLeft   = -bw / 2;

		// Background + border
		DrawRect(new Rect2(bLeft, bTop, bw, bh), new Color(1, 1, 1, 0.96f), true);
		DrawRect(new Rect2(bLeft, bTop, bw, bh), new Color(0.35f, 0.35f, 0.35f), false, 1.5f);

		// Tail triangle pointing down at the Glorp
		float tipY = -(BodyPx / 2 + 4);
		var pts = new Vector2[] { new Vector2(-7, bBottom), new Vector2(7, bBottom), new Vector2(0, tipY) };
		DrawPolygon(pts, new[] { new Color(1, 1, 1, 0.96f) });
		DrawLine(pts[0], pts[2], new Color(0.35f, 0.35f, 0.35f), 1.5f);
		DrawLine(pts[1], pts[2], new Color(0.35f, 0.35f, 0.35f), 1.5f);
		DrawLine(pts[0], pts[1], new Color(1, 1, 1, 0.96f), 2.5f); // cover gap at base

		// Emoji text — origin must be the LEFT edge of the rect for Center alignment to work
		DrawString(ThemeDB.FallbackFont, new Vector2(bLeft, bTop + bh * 0.68f),
			_bubbleText, HorizontalAlignment.Center, (int)bw, 20, Colors.Black);
	}
}

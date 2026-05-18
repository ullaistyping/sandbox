using System;
using System.Collections.Generic;
using Godot;

public partial class Main : Control
{
	// ── Constants ─────────────────────────────────────────────────────────────

	private const int SimW  = Simulation.SimW;
	private const int SimH  = Simulation.SimH;
	private const int Scale = 4;

	private const int BrushSand     = (int)Simulation.Cell.Sand;
	private const int BrushWater    = (int)Simulation.Cell.Water;
	private const int BrushStone    = (int)Simulation.Cell.Stone;
	private const int BrushLava     = (int)Simulation.Cell.Lava;
	private const int BrushGas      = (int)Simulation.Cell.Gas;
	private const int BrushFood     = (int)Simulation.Cell.Food;
	private const int BrushCopper   = (int)Simulation.Cell.Copper;
	private const int BrushBattery  = (int)Simulation.Cell.Battery;
	private const int BrushWood     = (int)Simulation.Cell.Wood;
	private const int BrushErase    = -1;
	private const int BrushForce    = -2;
	private const int BrushGlorp    = -3;
	private const int BrushPin      = -4;
	private const int BrushHeatView = -5;

	// ── Colours ───────────────────────────────────────────────────────────────

	private static readonly byte AirR        = (byte)(0.10f * 255);
	private static readonly byte AirG        = (byte)(0.10f * 255);
	private static readonly byte AirB        = (byte)(0.18f * 255);
	private static readonly byte SandR       = (byte)(0.76f * 255);
	private static readonly byte SandG       = (byte)(0.66f * 255);
	private static readonly byte SandB       = (byte)(0.42f * 255);
	private static readonly byte WaterR      = (byte)(0.23f * 255);
	private static readonly byte WaterG      = (byte)(0.48f * 255);
	private static readonly byte WaterB      = (byte)(0.84f * 255);
	private static readonly byte StoneR      = (byte)(0.45f * 255);
	private static readonly byte StoneG      = (byte)(0.45f * 255);
	private static readonly byte StoneB      = (byte)(0.50f * 255);
	private static readonly byte LavaR       = (byte)(0.95f * 255);
	private static readonly byte LavaG       = (byte)(0.32f * 255);
	private static readonly byte LavaB       = (byte)(0.10f * 255);
	private static readonly byte GasR        = (byte)(0.65f * 255);
	private static readonly byte GasG        = (byte)(0.90f * 255);
	private static readonly byte GasB        = (byte)(0.35f * 255);
	private static readonly byte FoodR       = (byte)(0.85f * 255);
	private static readonly byte FoodG       = (byte)(0.60f * 255);
	private static readonly byte FoodB       = (byte)(0.15f * 255);
	private static readonly byte CopperColdR = (byte)(0.65f * 255);
	private static readonly byte CopperColdG = (byte)(0.40f * 255);
	private static readonly byte CopperColdB = (byte)(0.20f * 255);
	private static readonly byte CopperHotR  = (byte)(0.95f * 255);
	private static readonly byte CopperHotG  = (byte)(0.70f * 255);
	private static readonly byte CopperHotB  = (byte)(0.25f * 255);
	private static readonly byte BatteryR    = (byte)(0.50f * 255);
	private static readonly byte BatteryG    = (byte)(0.52f * 255);
	private static readonly byte BatteryB    = (byte)(0.58f * 255);
	private static readonly byte WoodR       = (byte)(0.55f * 255);
	private static readonly byte WoodG       = (byte)(0.35f * 255);
	private static readonly byte WoodB       = (byte)(0.15f * 255);
	private static readonly byte SteamR      = (byte)(0.85f * 255);
	private static readonly byte SteamG      = (byte)(0.85f * 255);
	private static readonly byte SteamB      = (byte)(0.88f * 255);

	// ── Fields ────────────────────────────────────────────────────────────────

	private Simulation    _sim;
	private Image         _image;
	private ImageTexture  _texture;
	private byte[]        _raw;
	private byte[]        _sandOffsets;
	private int           _brush          = BrushSand;
	private int           _brushSize      = 5;
	private int           _ticksPerSecond = 30;
	private double        _tickAccum      = 0.0;
	private readonly Random _rng = new Random();

	// UI nodes
	private TextureRect _textureRect;
	private Button _btnSand, _btnWater, _btnStone, _btnLava, _btnGas;
	private Button _btnFood, _btnGlorp, _btnCopper, _btnBattery, _btnWood, _btnErase, _btnForce;
	private Button _btnTabMaterials, _btnTabSettings, _btnTabAnalysis, _detachBtn;
	private Button _btnHeatView, _btnPin;
	private Control _materialsPage, _settingsPage, _analysisPage;
	private Label   _heatResultLabel;
	private HSlider _slider, _speedSlider;
	private Panel   _panel, _tab;
	private Control _toolBox;
	private Tween         _activeTween;
	private bool          _toolBoxExpanded;
	private OverlayCanvas _overlay;

	// Detach / drag
	private bool    _detached, _dragging;
	private Vector2 _dragOffset;
	private const float ToolBoxW = 400f;
	private const float ToolBoxH = 270f;

	// Panel tween
	private const float  PanelHiddenY   = -240f;
	private const float  PanelShownY    = 30f;
	private const double PanelTweenTime = 0.22;

	// Console
	private Panel         _consolePanel;
	private RichTextLabel _consoleOutput;
	private LineEdit      _consoleInput;
	private bool          _consoleOpen;

	// Glorps
	private readonly List<Glorp> _glorps = new();
	private Glorp _selectedGlorp;
	private bool  _suppressRightErase;

	// Heat viewer
	private Vector2I _heatStart, _heatEnd;
	private bool     _selectingHeat;
	private bool     _hasHeatResult;

	// Pin tool
	private readonly HashSet<int> _pinnedSet = new(); // indices into Grid
	private bool _pinSetMode; // true = pin, false = unpin on this drag

	// Rigid bodies
	private readonly List<RigidBody>                  _bodies     = new();
	private readonly Dictionary<(int,int), RigidBody> _cellToBody = new();
	private readonly HashSet<(int x, int y)>          _woodPreview = new();
	private bool _placingWood;

	private static readonly (int dx, int dy)[] FaceDirections = { (0,-1),(0,1),(-1,0),(1,0) };

	// ── Ready ─────────────────────────────────────────────────────────────────

	public override void _Ready()
	{
		_sim = new Simulation();

		_sandOffsets = new byte[SimW * SimH];
		for (int i = 0; i < _sandOffsets.Length; i++)
			_sandOffsets[i] = (byte)_rng.Next(0, 41);

		_raw     = new byte[SimW * SimH * 4];
		_image   = Image.Create(SimW, SimH, false, Image.Format.Rgba8);
		_texture = ImageTexture.CreateFromImage(_image);

		_textureRect         = GetNode<TextureRect>("TextureRect");
		_textureRect.Texture = _texture;

		// Overlay sits after TextureRect in the child list so it renders on top of it
		_overlay = new OverlayCanvas();
		_overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_overlay.MouseFilter = Control.MouseFilterEnum.Ignore;
		_overlay.OnDraw = DrawOverlay;
		AddChild(_overlay);

		_toolBox = GetNode<Control>("UI/ToolBox");
		_tab     = GetNode<Panel>("UI/ToolBox/Tab");
		_panel   = GetNode<Panel>("UI/ToolBox/Panel");

		const string g = "UI/ToolBox/Panel/VBoxContainer/MaterialsPage/ButtonGrid/";
		_btnSand    = GetNode<Button>(g + "BtnSand");
		_btnWater   = GetNode<Button>(g + "BtnWater");
		_btnStone   = GetNode<Button>(g + "BtnStone");
		_btnLava    = GetNode<Button>(g + "BtnLava");
		_btnGas     = GetNode<Button>(g + "BtnGas");
		_btnFood    = GetNode<Button>(g + "BtnFood");
		_btnGlorp   = GetNode<Button>(g + "BtnGlorp");
		_btnCopper  = GetNode<Button>(g + "BtnCopper");
		_btnBattery = GetNode<Button>(g + "BtnBattery");
		_btnWood    = GetNode<Button>(g + "BtnWood");
		_btnErase   = GetNode<Button>(g + "BtnErase");
		_btnForce   = GetNode<Button>(g + "BtnForce");

		_slider      = GetNode<HSlider>("UI/ToolBox/Panel/VBoxContainer/MaterialsPage/SizeSlider");
		_speedSlider = GetNode<HSlider>("UI/ToolBox/Panel/VBoxContainer/SettingsPage/SpeedSlider");

		_btnTabMaterials = GetNode<Button>("UI/ToolBox/Panel/VBoxContainer/TabBar/BtnTabMaterials");
		_btnTabSettings  = GetNode<Button>("UI/ToolBox/Panel/VBoxContainer/TabBar/BtnTabSettings");
		_btnTabAnalysis  = GetNode<Button>("UI/ToolBox/Panel/VBoxContainer/TabBar/BtnTabAnalysis");
		_detachBtn       = GetNode<Button>("UI/ToolBox/Tab/DetachBtn");

		_materialsPage = GetNode<Control>("UI/ToolBox/Panel/VBoxContainer/MaterialsPage");
		_settingsPage  = GetNode<Control>("UI/ToolBox/Panel/VBoxContainer/SettingsPage");
		_analysisPage  = GetNode<Control>("UI/ToolBox/Panel/VBoxContainer/AnalysisPage");

		const string ap = "UI/ToolBox/Panel/VBoxContainer/AnalysisPage/";
		_btnHeatView     = GetNode<Button>(ap + "AnalysisRow/BtnHeatView");
		_btnPin          = GetNode<Button>(ap + "AnalysisRow/BtnPin");
		_heatResultLabel = GetNode<Label>(ap + "HeatResultLabel");

		_btnSand.Pressed    += () => SetBrush(BrushSand);
		_btnWater.Pressed   += () => SetBrush(BrushWater);
		_btnStone.Pressed   += () => SetBrush(BrushStone);
		_btnLava.Pressed    += () => SetBrush(BrushLava);
		_btnGas.Pressed     += () => SetBrush(BrushGas);
		_btnFood.Pressed    += () => SetBrush(BrushFood);
		_btnGlorp.Pressed   += () => SetBrush(BrushGlorp);
		_btnCopper.Pressed  += () => SetBrush(BrushCopper);
		_btnBattery.Pressed += () => SetBrush(BrushBattery);
		_btnWood.Pressed    += () => SetBrush(BrushWood);
		_btnErase.Pressed   += () => SetBrush(BrushErase);
		_btnForce.Pressed   += () => SetBrush(BrushForce);
		_slider.ValueChanged      += v => _brushSize      = (int)v;
		_speedSlider.ValueChanged += v => _ticksPerSecond = (int)v;

		_btnTabMaterials.Pressed += () => SetActiveTab(0);
		_btnTabSettings.Pressed  += () => SetActiveTab(1);
		_btnTabAnalysis.Pressed  += () => SetActiveTab(2);
		_detachBtn.Pressed       += ToggleDetach;
		_btnHeatView.Pressed     += () => SetBrush(BrushHeatView);
		_btnPin.Pressed          += () => SetBrush(BrushPin);

		_consolePanel  = GetNode<Panel>("UI/ConsolePanel");
		_consoleOutput = GetNode<RichTextLabel>("UI/ConsolePanel/ConsoleVBox/ConsoleOutput");
		_consoleInput  = GetNode<LineEdit>("UI/ConsolePanel/ConsoleVBox/ConsoleInput");
		_consoleInput.TextSubmitted += ExecuteCommand;

		SetActiveTab(0);
		SetBrush(BrushSand);
	}

	// ── Process ───────────────────────────────────────────────────────────────

	public override void _Process(double delta)
	{
		_tickAccum += delta;
		double interval = 1.0 / _ticksPerSecond;
		while (_tickAccum >= interval)
		{
			_sim.Update();
			UpdateRigidBodies();
			_tickAccum -= interval;
		}
		Render();
		_overlay.QueueRedraw();

		Vector2 mouse = GetViewport().GetMousePosition();

		if (!_detached)
		{
			bool inToolBox = _toolBox.GetGlobalRect().HasPoint(mouse);
			if (inToolBox != _toolBoxExpanded)
			{
				if (inToolBox) ShowPanel(); else HidePanel();
			}
		}

		// Block brush from firing over UI
		if (_tab.GetGlobalRect().HasPoint(mouse)) return;
		if (_detached  && _toolBox.GetGlobalRect().HasPoint(mouse)) return;
		if (!_detached && _toolBoxExpanded && _panel.GetGlobalRect().HasPoint(mouse)) return;

		bool lmbHeld = Input.IsMouseButtonPressed(MouseButton.Left);
		bool rmbHeld = Input.IsMouseButtonPressed(MouseButton.Right);

		if (lmbHeld)
		{
			var simPos = ScreenToSim(mouse);
			switch (_brush)
			{
				case BrushWood when _placingWood:
					StampWoodPreview(ScreenToSim(mouse));
					break;
				case BrushHeatView when _selectingHeat:
					_heatEnd = simPos;
					ComputeHeatResult();
					break;
				case BrushPin:
					ApplyPin(simPos, _pinSetMode);
					break;
				default:
					ApplyBrush(mouse);
					break;
			}
		}

		if (rmbHeld && !_suppressRightErase)
		{
			var simPos = ScreenToSim(mouse);
			if (_brush == BrushPin)
				ApplyPin(simPos, false); // RMB always unpins
			else
				StampCircle(simPos.X, simPos.Y, (int)Simulation.Cell.Air);
		}
	}

	// ── Input ─────────────────────────────────────────────────────────────────

	public override void _Input(InputEvent @event)
	{
		// Console toggle
		if (@event is InputEventKey k && k.Pressed && !k.Echo && k.Keycode == Key.Quoteleft)
		{
			_consoleOpen = !_consoleOpen;
			_consolePanel.Visible = _consoleOpen;
			if (_consoleOpen) _consoleInput.GrabFocus();
			else              _consoleInput.ReleaseFocus();
			GetViewport().SetInputAsHandled();
			return;
		}

		// Drag floating toolbox by its tab
		if (_detached && @event is InputEventMouseButton dmb && dmb.ButtonIndex == MouseButton.Left)
		{
			if (dmb.Pressed
				&& _tab.GetGlobalRect().HasPoint(dmb.Position)
				&& !_detachBtn.GetGlobalRect().HasPoint(dmb.Position))
			{
				_dragging   = true;
				_dragOffset = dmb.Position - new Vector2(_toolBox.OffsetLeft, _toolBox.OffsetTop);
				GetViewport().SetInputAsHandled();
				return;
			}
			if (!dmb.Pressed) _dragging = false;
		}
		if (_detached && _dragging && @event is InputEventMouseMotion mot)
		{
			var np = mot.Position - _dragOffset;
			_toolBox.OffsetLeft   = np.X;
			_toolBox.OffsetRight  = np.X + ToolBoxW;
			_toolBox.OffsetTop    = np.Y;
			_toolBox.OffsetBottom = np.Y + ToolBoxH;
			return;
		}

		if (@event is InputEventMouseButton mb)
		{
			var mouse = mb.Position;
			bool overUI = _tab.GetGlobalRect().HasPoint(mouse)
						|| (_toolBoxExpanded && _panel.GetGlobalRect().HasPoint(mouse))
						|| (_detached        && _toolBox.GetGlobalRect().HasPoint(mouse));

			if (mb.Pressed)
			{
				if (mb.ButtonIndex == MouseButton.Left)
				{
					if (_brush == BrushWood && !overUI)
					{
						_placingWood = true;
						StampWoodPreview(ScreenToSim(mouse));
					}
					else if (_brush == BrushHeatView && !overUI)
					{
						_selectingHeat  = true;
						_hasHeatResult  = false;
						_heatStart = _heatEnd = ScreenToSim(mouse);
						_heatResultLabel.Text = "Drag to select region…";
					}
					else if (_brush == BrushPin && !overUI)
					{
						var sp = ScreenToSim(mouse);
						int idx = sp.Y * SimW + sp.X;
						_pinSetMode = _sim.InBounds(sp.X, sp.Y) && _sim.Pinned[idx] == 0;
						ApplyPin(sp, _pinSetMode);
					}
					else if (_brush == BrushGlorp && !overUI)
					{
						SpawnGlorp(ScreenToSim(mouse).X, ScreenToSim(mouse).Y);
					}
				}
				else if (mb.ButtonIndex == MouseButton.Right)
				{
					var rMouse = mb.Position / Scale;
					var hit = FindGlorpAt(rMouse);
					if (hit != null) { SelectGlorp(hit); _suppressRightErase = true; }
					else             { _suppressRightErase = false; }
				}
				else if (mb.ButtonIndex == MouseButton.WheelUp)
				{ _brushSize = Math.Min(_brushSize + 1, 20); _slider.Value = _brushSize; }
				else if (mb.ButtonIndex == MouseButton.WheelDown)
				{ _brushSize = Math.Max(_brushSize - 1, 1); _slider.Value = _brushSize; }
			}
			else // released
			{
				if (mb.ButtonIndex == MouseButton.Left)
				{
					if (_brush == BrushWood && _placingWood)
					{
						_placingWood = false;
						PlaceWood();
					}
					else if (_brush == BrushHeatView && _selectingHeat)
					{
						_selectingHeat = false;
						_hasHeatResult = true;
					}
				}
			}
		}
	}

	// ── Rigid body physics ────────────────────────────────────────────────────

	private void UpdateRigidBodies()
	{
		const float Gravity     = 0.35f;
		const float MaxFall     = 8f;
		const float MaxRise     = 3f;
		const float MaxAngVel   = 0.06f;
		const float Restitution = 0.2f;
		const float FricVelX    = 0.85f;
		const float FricAngVel  = 0.88f;
		const int   SleepFrames = 10;
		const float SleepVel    = 0.05f;
		const float SleepAng    = 0.001f;

		for (int bi = _bodies.Count - 1; bi >= 0; bi--)
		{
			var body = _bodies[bi];

			// Remove if all wood cells have been overwritten (e.g. by explosion)
			bool alive = false;
			foreach (var (cx, cy) in body.GridCells)
				if (_sim.InBounds(cx, cy) && _sim.Grid[cy * SimW + cx] == (byte)Simulation.Cell.Wood)
				{ alive = true; break; }
			if (!alive)
			{
				foreach (var c in body.GridCells) _cellToBody.Remove(c);
				_bodies.RemoveAt(bi);
				continue;
			}

			// Freeze if any cell is pinned
			bool pinned = false;
			foreach (var (cx, cy) in body.GridCells)
				if (_sim.InBounds(cx, cy) && _sim.Pinned[cy * SimW + cx] != 0) { pinned = true; break; }
			if (pinned)
			{
				body.VelX = body.VelY = body.AngVel = body.SubX = body.SubY = 0;
				body.Sleeping = false; body.SleepTimer = 0;
				continue;
			}

			// Skip sleeping bodies unless something wakes them
			if (body.Sleeping)
			{
				if (!ShouldWake(body)) continue;
				body.Sleeping = false;
				body.SleepTimer = 0;
			}

			// ─ Gravity (suppressed when resting on a surface) ─
			bool groundedPre = IsGrounded(body);
			if (!groundedPre || body.VelY < 0f)
				body.VelY = Math.Clamp(body.VelY + Gravity, -MaxRise, MaxFall);
			else
				body.VelY = 0f;

			// ─ Fluid forces (steam, water, gas acting on exposed faces) ─
			ApplyFluidForces(body);

			// ─ X translation (sub-pixel stepping) ─
			body.SubX += body.VelX;
			int stepsX = (int)MathF.Abs(body.SubX);
			int  dirX  = Math.Sign(body.SubX);
			for (int s = 0; s < stepsX; s++)
			{
				float nx   = body.Position.X + dirX;
				var   proj = ProjectCells(body.LocalCells, nx, body.Position.Y, body.Angle);
				DisplaceWater(body.GridCells, proj);
				var blocked = GetBlockedCells(body.GridCells, proj, out var otherX);
				if (blocked.Count == 0)
				{
					StampGrid(body, proj);
					body.Position = new Vector2(nx, body.Position.Y);
					body.SubX -= dirX;
				}
				else
				{
					ResolveCollision(body, otherX, blocked, new Vector2(-dirX, 0), Restitution);
					body.SubX = 0f;
					break;
				}
			}
			if (MathF.Abs(body.SubX) < 1f && MathF.Abs(body.VelX) < 0.001f) body.SubX = 0f;

			// ─ Y translation (sub-pixel stepping) ─
			body.SubY += body.VelY;
			int stepsY = (int)MathF.Abs(body.SubY);
			int  dirY  = Math.Sign(body.SubY);
			for (int s = 0; s < stepsY; s++)
			{
				float ny   = body.Position.Y + dirY;
				var   proj = ProjectCells(body.LocalCells, body.Position.X, ny, body.Angle);
				DisplaceWater(body.GridCells, proj);
				var blocked = GetBlockedCells(body.GridCells, proj, out var otherY);
				if (blocked.Count == 0)
				{
					StampGrid(body, proj);
					body.Position = new Vector2(body.Position.X, ny);
					body.SubY -= dirY;
				}
				else
				{
					ResolveCollision(body, otherY, blocked, new Vector2(0, -dirY), Restitution);
					body.SubY = 0f;
					break;
				}
			}

			// ─ Rotation (one delta per tick, capped) ─
			body.AngVel = Math.Clamp(body.AngVel, -MaxAngVel, MaxAngVel);
			if (MathF.Abs(body.AngVel) > 0.0005f)
			{
				float newAngle = body.Angle + body.AngVel;
				var   proj     = ProjectCells(body.LocalCells, body.Position.X, body.Position.Y, newAngle);
				DisplaceWater(body.GridCells, proj);
				var blocked = GetBlockedCells(body.GridCells, proj, out var otherR);
				if (blocked.Count == 0)
				{
					StampGrid(body, proj);
					body.Angle = newAngle;
				}
				else
				{
					var n = ComputeContactNormal(body.Position, blocked);
					ResolveCollision(body, otherR, blocked, n, Restitution * 0.5f);
				}
			}

			// ─ Friction when in contact with a surface ─
			bool groundedPost = IsGrounded(body);
			if (groundedPost)
			{
				body.VelX   *= FricVelX;
				body.AngVel *= FricAngVel;
				if (MathF.Abs(body.VelX) < 0.001f) body.VelX = 0f;
			}

			// ─ Sleep ─
			if (groundedPost &&
				MathF.Abs(body.VelX)   < SleepVel &&
				MathF.Abs(body.VelY)   < SleepVel &&
				MathF.Abs(body.AngVel) < SleepAng)
				body.SleepTimer++;
			else
				body.SleepTimer = 0;

			if (body.SleepTimer >= SleepFrames)
			{
				body.Sleeping = true;
				body.VelX = body.VelY = body.AngVel = body.SubX = body.SubY = 0f;
			}
		}
	}

	private static HashSet<(int, int)> ProjectCells(List<Vector2> local, float px, float py, float angle)
	{
		var   result = new HashSet<(int, int)>();
		float cos    = MathF.Cos(angle), sin = MathF.Sin(angle);
		foreach (var l in local)
		{
			int wx = (int)MathF.Round(px + l.X * cos - l.Y * sin);
			int wy = (int)MathF.Round(py + l.X * sin + l.Y * cos);
			result.Add((wx, wy));
		}
		return result;
	}

	// Returns cells in `next` that are not passable (blocked). Out param is the owning RigidBody if it's another body.
	private List<(int, int)> GetBlockedCells(
		HashSet<(int, int)> current,
		HashSet<(int, int)> next,
		out RigidBody otherBody)
	{
		var blocked = new List<(int, int)>();
		otherBody = null;
		foreach (var (x, y) in next)
		{
			if (current.Contains((x, y))) continue;
			if (!_sim.InBounds(x, y)) { blocked.Add((x, y)); continue; }
			if (_sim.Pinned[y * SimW + x] != 0) { blocked.Add((x, y)); continue; }
			byte c = _sim.Grid[y * SimW + x];
			if (c == (byte)Simulation.Cell.Air   ||
				c == (byte)Simulation.Cell.Steam  ||
				c == (byte)Simulation.Cell.Gas    ||
				c == (byte)Simulation.Cell.Water) continue;
			if (c == (byte)Simulation.Cell.Wood &&
				_cellToBody.TryGetValue((x, y), out var owner) && owner != null)
			{
				blocked.Add((x, y));
				if (otherBody == null) otherBody = owner;
				continue;
			}
			blocked.Add((x, y));
		}
		return blocked;
	}

	// Push water cells that `next` would enter into adjacent air cells.
	private void DisplaceWater(HashSet<(int, int)> current, HashSet<(int, int)> next)
	{
		foreach (var (x, y) in next)
		{
			if (current.Contains((x, y))) continue;
			if (!_sim.InBounds(x, y)) continue;
			if (_sim.Grid[y * SimW + x] == (byte)Simulation.Cell.Water)
				TryDisplaceWater(x, y);
		}
	}

	private bool TryDisplaceWater(int wx, int wy)
	{
		ReadOnlySpan<int> dxs = stackalloc int[] { 0, 0, -1, 1 };
		ReadOnlySpan<int> dys = stackalloc int[] { -1, 1, 0, 0 };
		for (int k = 0; k < 4; k++)
		{
			int nx = wx + dxs[k], ny = wy + dys[k];
			if (!_sim.InBounds(nx, ny)) continue;
			if (_sim.Grid[ny * SimW + nx] != (byte)Simulation.Cell.Air) continue;
			int si = wy * SimW + wx, di = ny * SimW + nx;
			_sim.Grid[di] = (byte)Simulation.Cell.Water; _sim.Flow[di] = _sim.Flow[si];
			_sim.Grid[si] = (byte)Simulation.Cell.Air;   _sim.Flow[si] = 0;
			return true;
		}
		return false;
	}

	private void StampGrid(RigidBody body, HashSet<(int, int)> next)
	{
		foreach (var (x, y) in body.GridCells)
		{
			if (next.Contains((x, y))) continue;
			_sim.Grid[y * SimW + x] = (byte)Simulation.Cell.Air;
			_cellToBody.Remove((x, y));
		}
		foreach (var (x, y) in next)
		{
			_sim.Grid[y * SimW + x] = (byte)Simulation.Cell.Wood;
			_cellToBody[(x, y)] = body;
		}
		body.GridCells = next;
	}

	private static void ResolveCollision(
		RigidBody bodyA,
		RigidBody bodyB,             // null = static surface
		List<(int, int)> contactCells,
		Vector2 n,
		float e)
	{
		// Mean contact point
		float cx = 0f, cy = 0f;
		foreach (var (x, y) in contactCells) { cx += x; cy += y; }
		cx /= contactCells.Count; cy /= contactCells.Count;

		// Offset from CoM to contact point
		var rA = new Vector2(cx - bodyA.Position.X, cy - bodyA.Position.Y);
		// Contact velocity on A: v + ω × r  (2D: ω × r = (-ω*r.y, ω*r.x))
		var velA = new Vector2(bodyA.VelX - bodyA.AngVel * rA.Y,
							   bodyA.VelY + bodyA.AngVel * rA.X);

		float rACrossN = rA.X * n.Y - rA.Y * n.X;
		float denom    = 1f / bodyA.Mass + rACrossN * rACrossN / bodyA.Inertia;

		Vector2 velRel;
		Vector2 rB = default;
		if (bodyB != null)
		{
			rB = new Vector2(cx - bodyB.Position.X, cy - bodyB.Position.Y);
			var velB = new Vector2(bodyB.VelX - bodyB.AngVel * rB.Y,
								   bodyB.VelY + bodyB.AngVel * rB.X);
			velRel = velA - velB;
			float rBCrossN = rB.X * n.Y - rB.Y * n.X;
			denom += 1f / bodyB.Mass + rBCrossN * rBCrossN / bodyB.Inertia;
		}
		else
		{
			velRel = velA;
		}

		float Vn = velRel.X * n.X + velRel.Y * n.Y;
		if (Vn >= 0f) return; // already separating

		float j = -(1f + e) * Vn / MathF.Max(denom, 0.0001f);

		bodyA.VelX   += j * n.X / bodyA.Mass;
		bodyA.VelY   += j * n.Y / bodyA.Mass;
		bodyA.AngVel += (rA.X * (j * n.Y) - rA.Y * (j * n.X)) / bodyA.Inertia;

		if (bodyB != null)
		{
			bodyB.VelX   -= j * n.X / bodyB.Mass;
			bodyB.VelY   -= j * n.Y / bodyB.Mass;
			bodyB.AngVel -= (rB.X * (j * n.Y) - rB.Y * (j * n.X)) / bodyB.Inertia;
			if (bodyB.Sleeping) { bodyB.Sleeping = false; bodyB.SleepTimer = 0; }
		}
	}

	// Per exposed face, apply force from adjacent fluid cells (enables steam windmill).
	// Force direction is AWAY from the fluid cell (inward push on the body).
	private void ApplyFluidForces(RigidBody body)
	{
		foreach (var (cx, cy) in body.GridCells)
		foreach (var (dx, dy) in FaceDirections)
		{
			int nx = cx + dx, ny = cy + dy;
			if (!_sim.InBounds(nx, ny)) continue;
			if (body.GridCells.Contains((nx, ny))) continue;
			float force = FluidForce(_sim.Grid[ny * SimW + nx]);
			if (force == 0f) continue;

			// Contact point at the face centre; force pushes in -face direction
			float rx = cx + dx * 0.5f - body.Position.X;
			float ry = cy + dy * 0.5f - body.Position.Y;
			float fx = force * -dx, fy = force * -dy;

			body.VelX   += fx / body.Mass;
			body.VelY   += fy / body.Mass;
			body.AngVel += (rx * fy - ry * fx) / body.Inertia;
		}
	}

	private static float FluidForce(byte cell)
	{
		if (cell == (byte)Simulation.Cell.Steam) return 0.08f;
		if (cell == (byte)Simulation.Cell.Water) return 0.04f;
		if (cell == (byte)Simulation.Cell.Gas)   return 0.02f;
		return 0f;
	}

	// Contact normal pointing from blocked cells toward body CoM.
	private static Vector2 ComputeContactNormal(Vector2 pos, List<(int, int)> blocked)
	{
		float nx = 0f, ny = 0f;
		foreach (var (x, y) in blocked) { nx += pos.X - x; ny += pos.Y - y; }
		float len = MathF.Sqrt(nx * nx + ny * ny);
		return len > 0.001f ? new Vector2(nx / len, ny / len) : new Vector2(0f, -1f);
	}

	private bool IsGrounded(RigidBody body)
	{
		var proj    = ProjectCells(body.LocalCells, body.Position.X, body.Position.Y + 1f, body.Angle);
		var blocked = GetBlockedCells(body.GridCells, proj, out _);
		return blocked.Count > 0;
	}

	private bool ShouldWake(RigidBody body)
	{
		// Wake if no longer supported from below
		if (!IsGrounded(body)) return true;
		// Wake if a fluid force cell touches an exposed face
		foreach (var (cx, cy) in body.GridCells)
		foreach (var (dx, dy) in FaceDirections)
		{
			int nx = cx + dx, ny = cy + dy;
			if (!_sim.InBounds(nx, ny)) continue;
			if (body.GridCells.Contains((nx, ny))) continue;
			byte c = _sim.Grid[ny * SimW + nx];
			if (FluidForce(c) > 0f) return true;
			if (c == (byte)Simulation.Cell.Wood &&
				_cellToBody.TryGetValue((nx, ny), out var other) &&
				other != null && !other.Sleeping) return true;
		}
		return false;
	}

	private void StampWoodPreview(Vector2I simPos)
	{
		int r = _brushSize;
		for (int dy = -r; dy <= r; dy++)
		for (int dx = -r; dx <= r; dx++)
		{
			if (dx * dx + dy * dy > r * r) continue;
			int wx = simPos.X + dx, wy = simPos.Y + dy;
			if (_sim.InBounds(wx, wy)) _woodPreview.Add((wx, wy));
		}
	}

	private void PlaceWood()
	{
		if (_woodPreview.Count == 0) return;
		var cells = new List<(int, int)>(_woodPreview.Count);
		foreach (var (wx, wy) in _woodPreview)
		{
			byte c = _sim.Grid[wy * SimW + wx];
			if (c == (byte)Simulation.Cell.Air || c == (byte)Simulation.Cell.Steam)
				cells.Add((wx, wy));
		}
		_woodPreview.Clear();
		if (cells.Count == 0) return;

		var body = new RigidBody(cells);
		_bodies.Add(body);
		foreach (var (wx, wy) in cells)
		{
			_sim.Grid[wy * SimW + wx] = (byte)Simulation.Cell.Wood;
			_cellToBody[(wx, wy)] = body;
		}
	}

	// ── Heat viewer ───────────────────────────────────────────────────────────

	private void ComputeHeatResult()
	{
		int x0 = Math.Min(_heatStart.X, _heatEnd.X), x1 = Math.Max(_heatStart.X, _heatEnd.X);
		int y0 = Math.Min(_heatStart.Y, _heatEnd.Y), y1 = Math.Max(_heatStart.Y, _heatEnd.Y);

		long total = 0; int count = 0;
		for (int y = y0; y <= y1; y++)
		for (int x = x0; x <= x1; x++)
		{
			if (!_sim.InBounds(x, y)) continue;
			int i = y * SimW + x;
			if (_sim.Grid[i] == (byte)Simulation.Cell.Copper) { total += _sim.Flow[i]; count++; }
		}

		if (count == 0) { _heatResultLabel.Text = "No copper in selection"; return; }
		float avg  = (float)total / count;
		string desc = avg < 60 ? "Cold" : avg < 150 ? "Warm" : avg < 220 ? "Hot" : "Critical";
		_heatResultLabel.Text = $"Avg heat: {avg:F0} / 255\n{count} copper cell{(count == 1 ? "" : "s")} — {desc}";
	}

	// ── Pin tool ──────────────────────────────────────────────────────────────

	private void ApplyPin(Vector2I simPos, bool pin)
	{
		int r = Math.Max(1, _brushSize / 2); // pin uses half brush radius
		for (int dy = -r; dy <= r; dy++)
		for (int dx = -r; dx <= r; dx++)
		{
			if (dx * dx + dy * dy > r * r) continue;
			int px = simPos.X + dx, py = simPos.Y + dy;
			if (!_sim.InBounds(px, py)) continue;
			int idx = py * SimW + px;
			_sim.Pinned[idx] = pin ? (byte)1 : (byte)0;
			if (pin) _pinnedSet.Add(idx);
			else     _pinnedSet.Remove(idx);
		}
	}

	// ── Rendering ─────────────────────────────────────────────────────────────

	private void CellColor(byte cell, byte flow, out byte r, out byte g, out byte b)
	{
		if (cell == (byte)Simulation.Cell.Sand)
		{
			int off = _sandOffsets[Math.Clamp((int)cell, 0, _sandOffsets.Length - 1)] - 20;
			r = (byte)Math.Clamp(SandR + off, 0, 255);
			g = (byte)Math.Clamp(SandG + off, 0, 255);
			b = (byte)Math.Clamp(SandB + off / 2, 0, 255);
		}
		else if (cell == (byte)Simulation.Cell.Water)
		{
			int t = flow == 1 ? 10 : flow == 2 ? -10 : 0;
			r = (byte)Math.Clamp(WaterR - t/2, 0, 255);
			g = (byte)Math.Clamp(WaterG + t/2, 0, 255);
			b = (byte)Math.Clamp(WaterB + t,   0, 255);
		}
		else if (cell == (byte)Simulation.Cell.Stone)
		{
			int off = (flow >> 2) - 5;
			r = (byte)Math.Clamp(StoneR + off, 0, 255);
			g = (byte)Math.Clamp(StoneG + off, 0, 255);
			b = (byte)Math.Clamp(StoneB + off, 0, 255);
		}
		else if (cell == (byte)Simulation.Cell.Lava)
		{
			int off = (flow >> 1) - 10;
			r = (byte)Math.Clamp(LavaR + off,     0, 255);
			g = (byte)Math.Clamp(LavaG + off,     0, 255);
			b = (byte)Math.Clamp(LavaB + off / 4, 0, 255);
		}
		else if (cell == (byte)Simulation.Cell.Gas)     { r = GasR;     g = GasG;     b = GasB;     }
		else if (cell == (byte)Simulation.Cell.Food)    { r = FoodR;    g = FoodG;    b = FoodB;    }
		else if (cell == (byte)Simulation.Cell.Copper)
		{
			float t = flow / 255f;
			r = (byte)(CopperColdR + (CopperHotR - CopperColdR) * t);
			g = (byte)(CopperColdG + (CopperHotG - CopperColdG) * t);
			b = (byte)(CopperColdB + (CopperHotB - CopperColdB) * t);
		}
		else if (cell == (byte)Simulation.Cell.Battery) { r = BatteryR; g = BatteryG; b = BatteryB; }
		else if (cell == (byte)Simulation.Cell.Wood)
		{
			// subtle grain variation per-cell using position hash
			int off = ((flow * 17) & 0x1F) - 12;
			r = (byte)Math.Clamp(WoodR + off,     0, 255);
			g = (byte)Math.Clamp(WoodG + off / 2, 0, 255);
			b = WoodB;
		}
		else if (cell == (byte)Simulation.Cell.Steam)   { r = SteamR;   g = SteamG;   b = SteamB;   }
		else                                            { r = AirR;     g = AirG;     b = AirB;     }
	}

	private void Render()
	{
		int pixelI = 0;
		for (int y = 0; y < SimH; y++)
		for (int x = 0; x < SimW; x++)
		{
			int i = y * SimW + x;
			byte cell = _sim.Grid[i];
			byte r, g, b;

			if (cell == (byte)Simulation.Cell.Gas)
			{
				const float ga = 0.52f;
				r = (byte)(GasR * ga + AirR * (1 - ga));
				g = (byte)(GasG * ga + AirG * (1 - ga));
				b = (byte)(GasB * ga + AirB * (1 - ga));
			}
			else if (cell == (byte)Simulation.Cell.Steam)
			{
				const float sa = 0.55f;
				r = (byte)(SteamR * sa + AirR * (1 - sa));
				g = (byte)(SteamG * sa + AirG * (1 - sa));
				b = (byte)(SteamB * sa + AirB * (1 - sa));
			}
			else if (cell == (byte)Simulation.Cell.Copper && _sim.Electric[i] != 0 && _rng.NextSingle() > 0.45f)
			{
				r = (byte)Math.Clamp(225 + _rng.Next(-25, 25), 0, 255);
				g = (byte)Math.Clamp(205 + _rng.Next(-25, 25), 0, 255);
				b = (byte)_rng.Next(0, 45);
			}
			else
			{
				CellColor(cell, _sim.Flow[i], out r, out g, out b);
			}

			// Pinned cells: lighten slightly so they're identifiable in-world
			if (_sim.Pinned[i] != 0 && cell != (byte)Simulation.Cell.Air)
			{
				r = (byte)Math.Min(255, r + 40);
				g = (byte)Math.Min(255, g + 40);
				b = (byte)Math.Min(255, b + 40);
			}

			_raw[pixelI]     = r;
			_raw[pixelI + 1] = g;
			_raw[pixelI + 2] = b;
			_raw[pixelI + 3] = 255;
			pixelI += 4;
		}
		_image.SetData(SimW, SimH, false, Image.Format.Rgba8, _raw);
		_texture.Update(_image);
	}

	// Drawn by OverlayCanvas (sits above the TextureRect in the scene tree)
	private void DrawOverlay(OverlayCanvas c)
	{
		if (_woodPreview.Count > 0)
		{
			var fill = new Color(WoodR / 255f, WoodG / 255f, WoodB / 255f, 0.70f);
			foreach (var (wx, wy) in _woodPreview)
				c.DrawRect(new Rect2(wx * Scale, wy * Scale, Scale, Scale), fill);
		}

		// Heat selection rectangle — white fill + solid white outline
		if (_selectingHeat || _hasHeatResult)
		{
			int x0 = Math.Min(_heatStart.X, _heatEnd.X), x1 = Math.Max(_heatStart.X, _heatEnd.X);
			int y0 = Math.Min(_heatStart.Y, _heatEnd.Y), y1 = Math.Max(_heatStart.Y, _heatEnd.Y);
			var selRect = new Rect2(x0 * Scale, y0 * Scale, (x1 - x0 + 1) * Scale, (y1 - y0 + 1) * Scale);
			c.DrawRect(selRect, new Color(1, 1, 1, 0.12f));           // subtle white fill
			c.DrawRect(selRect, new Color(1, 1, 1, 0.95f), false, 2f); // bright white outline
		}
	}

	// ── UI state ──────────────────────────────────────────────────────────────

	private void SetBrush(int b)
	{
		_brush = b;
		if (b != BrushHeatView) { _selectingHeat = false; _hasHeatResult = false; }
		if (b != BrushWood && _placingWood) { _placingWood = false; _woodPreview.Clear(); }

		_btnSand.Modulate    = b == BrushSand    ? Colors.Yellow : Colors.White;
		_btnWater.Modulate   = b == BrushWater   ? Colors.Yellow : Colors.White;
		_btnStone.Modulate   = b == BrushStone   ? Colors.Yellow : Colors.White;
		_btnLava.Modulate    = b == BrushLava    ? Colors.Yellow : Colors.White;
		_btnGas.Modulate     = b == BrushGas     ? Colors.Yellow : Colors.White;
		_btnFood.Modulate    = b == BrushFood    ? Colors.Yellow : Colors.White;
		_btnGlorp.Modulate   = b == BrushGlorp   ? Colors.Yellow : Colors.White;
		_btnCopper.Modulate  = b == BrushCopper  ? Colors.Yellow : Colors.White;
		_btnBattery.Modulate = b == BrushBattery ? Colors.Yellow : Colors.White;
		_btnWood.Modulate    = b == BrushWood    ? Colors.Yellow : Colors.White;
		_btnErase.Modulate   = b == BrushErase   ? Colors.Yellow : Colors.White;
		_btnForce.Modulate   = b == BrushForce   ? Colors.Yellow : Colors.White;
		_btnHeatView.Modulate = b == BrushHeatView ? Colors.Yellow : Colors.White;
		_btnPin.Modulate      = b == BrushPin      ? Colors.Yellow : Colors.White;
	}

	private void SetActiveTab(int tab)
	{
		_materialsPage.Visible = (tab == 0);
		_settingsPage.Visible  = (tab == 1);
		_analysisPage.Visible  = (tab == 2);
		_btnTabMaterials.Modulate = tab == 0 ? Colors.Yellow : Colors.White;
		_btnTabSettings.Modulate  = tab == 1 ? Colors.Yellow : Colors.White;
		_btnTabAnalysis.Modulate  = tab == 2 ? Colors.Yellow : Colors.White;
	}

	private void ToggleDetach()
	{
		_detached = !_detached;
		if (_detached)
		{
			var pos = _toolBox.GetGlobalRect().Position;
			_toolBox.AnchorLeft   = 0f; _toolBox.AnchorRight  = 0f;
			_toolBox.AnchorTop    = 0f; _toolBox.AnchorBottom = 0f;
			_toolBox.OffsetLeft   = pos.X;         _toolBox.OffsetRight  = pos.X + ToolBoxW;
			_toolBox.OffsetTop    = pos.Y;         _toolBox.OffsetBottom = pos.Y + ToolBoxH;
			ShowPanel();
			_detachBtn.Text = "⊡";
		}
		else
		{
			_toolBox.AnchorLeft   = 1f; _toolBox.AnchorRight  = 1f;
			_toolBox.AnchorTop    = 0f; _toolBox.AnchorBottom = 0f;
			_toolBox.OffsetLeft   = -ToolBoxW; _toolBox.OffsetRight  = 0f;
			_toolBox.OffsetTop    = 0f;        _toolBox.OffsetBottom = ToolBoxH;
			_toolBoxExpanded = true; HidePanel();
			_detachBtn.Text = "⊞";
		}
	}

	private void ShowPanel()
	{
		if (_toolBoxExpanded) return;
		_toolBoxExpanded = true;
		_activeTween?.Kill();
		_activeTween = CreateTween();
		_activeTween.TweenProperty(_panel, "position:y", PanelShownY, PanelTweenTime)
			.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
	}

	private void HidePanel()
	{
		if (!_toolBoxExpanded) return;
		_toolBoxExpanded = false;
		_activeTween?.Kill();
		_activeTween = CreateTween();
		_activeTween.TweenProperty(_panel, "position:y", PanelHiddenY, PanelTweenTime)
			.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
	}

	// ── Helpers ───────────────────────────────────────────────────────────────

	private static Vector2I ScreenToSim(Vector2 screen) =>
		new Vector2I(Math.Clamp((int)(screen.X / Scale), 0, SimW - 1),
					 Math.Clamp((int)(screen.Y / Scale), 0, SimH - 1));

	private void ApplyBrush(Vector2 screenPos)
	{
		var sp = ScreenToSim(screenPos);
		switch (_brush)
		{
			case BrushSand:    StampCircle(sp.X, sp.Y, (int)Simulation.Cell.Sand);    break;
			case BrushWater:   StampCircle(sp.X, sp.Y, (int)Simulation.Cell.Water);   break;
			case BrushStone:   StampCircle(sp.X, sp.Y, (int)Simulation.Cell.Stone);   break;
			case BrushLava:    StampCircle(sp.X, sp.Y, (int)Simulation.Cell.Lava);    break;
			case BrushGas:     StampCircle(sp.X, sp.Y, (int)Simulation.Cell.Gas);     break;
			case BrushFood:    StampCircle(sp.X, sp.Y, (int)Simulation.Cell.Food);    break;
			case BrushCopper:  StampCircle(sp.X, sp.Y, (int)Simulation.Cell.Copper);  break;
			case BrushBattery: StampCircle(sp.X, sp.Y, (int)Simulation.Cell.Battery); break;
			case BrushErase:   StampCircle(sp.X, sp.Y, (int)Simulation.Cell.Air);     break;
			case BrushForce:   _sim.ApplyForce(sp.X, sp.Y, _brushSize * 3, 6);        break;
		}
	}

	private void StampCircle(int cx, int cy, int cell)
	{
		int r = _brushSize;
		for (int dy = -r; dy <= r; dy++)
		for (int dx = -r; dx <= r; dx++)
			if (dx * dx + dy * dy <= r * r)
				_sim.SetCell(cx + dx, cy + dy, cell);
	}

	// ── Glorps ────────────────────────────────────────────────────────────────

	private Glorp FindGlorpAt(Vector2 simPos)
	{
		foreach (var g in _glorps)
			if ((g.SimPos - simPos).Length() < Glorp.SimR) return g;
		return null;
	}

	private void SelectGlorp(Glorp hit)
	{
		if (_selectedGlorp != null) _selectedGlorp.Selected = false;
		_selectedGlorp = (_selectedGlorp == hit) ? null : hit;
		if (_selectedGlorp != null) _selectedGlorp.Selected = true;
	}

	private void SpawnGlorp(int gx, int gy)
	{
		var g = new Glorp(); AddChild(g);
		g.Init(_sim, _glorps, new Vector2(gx, gy));
		_glorps.Add(g);
	}

	// ── Console ───────────────────────────────────────────────────────────────

	private void ExecuteCommand(string raw)
	{
		_consoleInput.Clear();
		string[] parts = raw.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length == 0) return;

		switch (parts[0].ToLower())
		{
			case "help":
				ConsoleLog("[color=yellow]Commands:[/color]");
				ConsoleLog("  [color=white]tps <n>[/color]          simulation ticks per second (1–120)");
				ConsoleLog("  [color=white]brush <n>[/color]        brush size (1–20)");
				ConsoleLog("  [color=white]clear[/color]            wipe the grid, pins, and wood");
				ConsoleLog("  [color=white]boil <n>[/color]         copper boil threshold (0–255)");
				ConsoleLog("  [color=white]gasthresh <n>[/color]    copper gas-ignite threshold (0–255)");
				break;

			case "tps" when parts.Length > 1 && int.TryParse(parts[1], out int tps):
				_ticksPerSecond = Math.Clamp(tps, 1, 120);
				_speedSlider.Value = _ticksPerSecond;
				ConsoleLog($"[color=cyan]TPS → {_ticksPerSecond}[/color]");
				break;

			case "brush" when parts.Length > 1 && int.TryParse(parts[1], out int bs):
				_brushSize = Math.Clamp(bs, 1, 20);
				_slider.Value = _brushSize;
				ConsoleLog($"[color=cyan]Brush size → {_brushSize}[/color]");
				break;

			case "clear":
				Array.Clear(_sim.Grid,   0, _sim.Grid.Length);
				Array.Clear(_sim.Flow,   0, _sim.Flow.Length);
				Array.Clear(_sim.VelX,   0, _sim.VelX.Length);
				Array.Clear(_sim.VelY,   0, _sim.VelY.Length);
				Array.Clear(_sim.Pinned, 0, _sim.Pinned.Length);
				_pinnedSet.Clear();
				_bodies.Clear();
				_cellToBody.Clear();
				_woodPreview.Clear();
				_placingWood = false;
				foreach (var gl in _glorps) gl.QueueFree();
				_glorps.Clear(); _selectedGlorp = null;
				ConsoleLog("[color=cyan]Grid cleared.[/color]");
				break;

			case "boil" when parts.Length > 1 && int.TryParse(parts[1], out int bt):
				_sim.CopperBoilThreshold = Math.Clamp(bt, 0, 255);
				ConsoleLog($"[color=cyan]Copper boil threshold → {_sim.CopperBoilThreshold}[/color]");
				break;

			case "gasthresh" when parts.Length > 1 && int.TryParse(parts[1], out int gt):
				_sim.CopperGasThreshold = Math.Clamp(gt, 0, 255);
				ConsoleLog($"[color=cyan]Gas ignition threshold → {_sim.CopperGasThreshold}[/color]");
				break;

			default:
				ConsoleLog($"[color=red]Unknown command '{parts[0]}'. Type 'help'.[/color]");
				break;
		}
	}

	private void ConsoleLog(string bbcode) => _consoleOutput.AppendText("\n" + bbcode);
}

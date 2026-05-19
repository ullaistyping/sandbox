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
	private const int BrushTurret    = -6;
	private const int BrushMirror    = -7;
	private const int BrushDirt      = (int)Simulation.Cell.Dirt;
	private const int BrushGrassSeed = (int)Simulation.Cell.GrassSeed;
	private const int BrushTreeSeed  = (int)Simulation.Cell.TreeSeed;
	private const int BrushFire           = -9;
	private const int BrushLiquidNitrogen = (int)Simulation.Cell.LiquidNitrogen;

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
	private static readonly byte MirrorR     = (byte)(0.82f * 255);
	private static readonly byte MirrorG     = (byte)(0.88f * 255);
	private static readonly byte MirrorB     = (byte)(0.96f * 255);
	private static readonly byte DirtR       = (byte)(0.50f * 255);
	private static readonly byte DirtG       = (byte)(0.32f * 255);
	private static readonly byte DirtB       = (byte)(0.15f * 255);
	private static readonly byte GrassR      = (byte)(0.25f * 255);
	private static readonly byte GrassG      = (byte)(0.72f * 255);
	private static readonly byte GrassB      = (byte)(0.15f * 255);
	private static readonly byte GrassSeedR  = (byte)(0.62f * 255);
	private static readonly byte GrassSeedG  = (byte)(0.45f * 255);
	private static readonly byte GrassSeedB  = (byte)(0.18f * 255);
	private static readonly byte TreeSeedR   = (byte)(0.45f * 255);
	private static readonly byte TreeSeedG   = (byte)(0.28f * 255);
	private static readonly byte TreeSeedB   = (byte)(0.10f * 255);
	private static readonly byte BarkR       = (byte)(0.42f * 255);
	private static readonly byte BarkG       = (byte)(0.26f * 255);
	private static readonly byte BarkB       = (byte)(0.10f * 255);
	private static readonly byte LeavesR     = (byte)(0.15f * 255);
	private static readonly byte LeavesG     = (byte)(0.55f * 255);
	private static readonly byte LeavesB     = (byte)(0.12f * 255);
	private static readonly byte LN2R        = (byte)(0.55f * 255);
	private static readonly byte LN2G        = (byte)(0.85f * 255);
	private static readonly byte LN2B        = (byte)(0.95f * 255);
	private static readonly byte IceR        = (byte)(0.78f * 255);
	private static readonly byte IceG        = (byte)(0.88f * 255);
	private static readonly byte IceB        = (byte)(0.97f * 255);
	// Copper ice-cold colour (remapped scale: 0=cold, 128=room, 255=hot)
	private static readonly byte CopperIceR  = (byte)(0.40f * 255);
	private static readonly byte CopperIceG  = (byte)(0.65f * 255);
	private static readonly byte CopperIceB  = (byte)(0.88f * 255);

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
	private Button _btnHeatView, _btnPin, _btnTurret, _btnMirror;
	private Button _btnDirt, _btnGrassSeed, _btnTreeSeed, _btnFire, _btnLiquidNitrogen;
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

	// Turrets
	private readonly List<LaserTurret> _turrets = new();
	private float _laserFalloff     = 0.4f; // power multiplier applied per bounce (0=instant, 1=no decay)
	private int   _laserMaxBounces  = 12;   // hard cap on mirror reflections per beam
	private Vector2I _mouseSim;

	// Shockwaves
	private struct ShockwaveEffect { public Vector2 Center; public float Radius, MaxRadius, Life; }
	private readonly List<ShockwaveEffect> _shockwaves = new();

	// Glorps
	private readonly List<Glorp> _glorps = new();
	private Glorp _selectedGlorp;
	private bool  _suppressRightErase;

	// Bezier mirrors
	private readonly List<BezierMirror> _mirrors = new();
	private BezierMirror _mirrorInProgress;

	// Heat viewer
	private Vector2I _heatStart, _heatEnd;
	private bool     _selectingHeat;
	private bool     _hasHeatResult;

	// Pin tool
	private readonly HashSet<int> _pinnedSet = new(); // indices into Grid
	private bool _pinSetMode; // true = pin, false = unpin on this drag

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
		_btnTurret    = GetNode<Button>(g + "BtnTurret");
		_btnMirror    = GetNode<Button>(g + "BtnMirror");
		_btnDirt      = GetNode<Button>(g + "BtnDirt");
		_btnGrassSeed = GetNode<Button>(g + "BtnGrassSeed");
		_btnTreeSeed  = GetNode<Button>(g + "BtnTreeSeed");
		_btnFire            = GetNode<Button>(g + "BtnFire");
		_btnLiquidNitrogen  = GetNode<Button>(g + "BtnLiquidNitrogen");

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
		_btnTurret.Pressed      += () => SetBrush(BrushTurret);
		_btnMirror.Pressed    += () => SetBrush(BrushMirror);
		_btnDirt.Pressed      += () => SetBrush(BrushDirt);
		_btnGrassSeed.Pressed += () => SetBrush(BrushGrassSeed);
		_btnTreeSeed.Pressed  += () => SetBrush(BrushTreeSeed);
		_btnFire.Pressed           += () => SetBrush(BrushFire);
		_btnLiquidNitrogen.Pressed += () => SetBrush(BrushLiquidNitrogen);
		_slider.ValueChanged      += v => _brushSize      = (int)v;
		_speedSlider.ValueChanged += v => _ticksPerSecond = (int)v;

		_btnTabMaterials.Pressed += () => SetActiveTab(0);
		_btnTabSettings.Pressed  += () => SetActiveTab(1);
		_btnTabAnalysis.Pressed  += () => SetActiveTab(2);
		_detachBtn.Pressed       += ToggleDetach;
		_btnHeatView.Pressed     += () => SetBrush(BrushHeatView);
		_btnPin.Pressed          += () => SetBrush(BrushPin);

		SetActiveTab(0);
		SetBrush(BrushSand);
		LoadConfig();
	}

	// ── Process ───────────────────────────────────────────────────────────────

	public override void _Process(double delta)
	{
		_tickAccum += delta;
		double interval = 1.0 / _ticksPerSecond;
		while (_tickAccum >= interval)
		{
			_sim.Update();
			_tickAccum -= interval;
		}
		// Spawn shockwave visuals for any explosions that fired this frame
		foreach (var (ecx, ecy, erad) in _sim.PendingExplosions)
			_shockwaves.Add(new ShockwaveEffect {
				Center    = new Vector2((ecx + 0.5f) * Scale, (ecy + 0.5f) * Scale),
				Radius    = erad * Scale * 0.15f,
				MaxRadius = erad * Scale * 1.8f,
				Life      = 1.0f
			});
		_sim.PendingExplosions.Clear();
		for (int si = _shockwaves.Count - 1; si >= 0; si--)
		{
			var sw = _shockwaves[si];
			sw.Radius += (sw.MaxRadius - sw.Radius) * 0.18f + 1.5f;
			sw.Life   -= 0.07f;
			_shockwaves[si] = sw;
			if (sw.Life <= 0f) _shockwaves.RemoveAt(si);
		}
		if (_sim.RenderDirty)
		{
			Render();
			_sim.RenderDirty = false;
		}
		_overlay.QueueRedraw();

		Vector2 mouse = GetViewport().GetMousePosition();
		_mouseSim = ScreenToSim(mouse);
		UpdateTurrets();

		// ImGui must be submitted every frame — do this before any early returns
		DrawDebugGui();

		if (!_detached)
		{
			bool inToolBox = _toolBox.GetGlobalRect().HasPoint(mouse);
			if (inToolBox != _toolBoxExpanded)
			{
				if (inToolBox) ShowPanel(); else HidePanel();
			}
		}

		// Block game input when any UI owns the mouse
		if (ImGuiNET.ImGui.GetIO().WantCaptureMouse) return;
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
				case BrushHeatView when _selectingHeat:
					_heatEnd = simPos;
					ComputeHeatResult();
					break;
				case BrushPin:
					ApplyPin(simPos, _pinSetMode);
					break;
				case BrushMirror:
					_mirrorInProgress?.AddSample(ScreenToSimF(mouse));
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
			{
				StampCircle(simPos.X, simPos.Y, (int)Simulation.Cell.Air);
				EraseTurretsInRadius(simPos.X, simPos.Y, _brushSize);
				EraseMirrorsInRadius(simPos.X, simPos.Y, _brushSize);
			}
		}
	}

	// ── Input ─────────────────────────────────────────────────────────────────

	public override void _Input(InputEvent @event)
	{
		// Debug panel toggle
		if (@event is InputEventKey k && k.Pressed && !k.Echo)
		{
			if (k.Keycode == Key.Quoteleft)
			{
				_showDebugGui = !_showDebugGui;
				_debugWinGeomLoaded = false; // re-apply saved position on next open
				GetViewport().SetInputAsHandled();
				return;
			}
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
			bool overUI = ImGuiNET.ImGui.GetIO().WantCaptureMouse
						|| _tab.GetGlobalRect().HasPoint(mouse)
						|| (_toolBoxExpanded && _panel.GetGlobalRect().HasPoint(mouse))
						|| (_detached        && _toolBox.GetGlobalRect().HasPoint(mouse));

			if (mb.Pressed)
			{
				if (mb.ButtonIndex == MouseButton.Left)
				{
					if (_brush == BrushHeatView && !overUI)
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
					else if (_brush == BrushTurret && !overUI)
					{
						PlaceTurret(ScreenToSim(mouse));
					}
					else if (_brush == BrushMirror && !overUI)
					{
						_mirrorInProgress = new BezierMirror();
						_mirrorInProgress.AddSample(ScreenToSimF(mouse));
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
					if (_brush == BrushHeatView && _selectingHeat)
					{
						_selectingHeat = false;
						_hasHeatResult = true;
					}
					else if (_mirrorInProgress != null)
					{
						if (_mirrorInProgress.SamplePoints.Count >= 2)
							_mirrors.Add(_mirrorInProgress);
						_mirrorInProgress = null;
					}
				}
			}
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
		_sim.RenderDirty = true; // pinned cells are tinted in the render
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
			// Flow is now temperature: 0=frozen, 128=room, 255=hot
			float tw = (flow - 128) / 127f; // -1=cold, 0=room, +1=hot
			int tint = (int)(tw * 14);
			r = (byte)Math.Clamp(WaterR + tint * 2, 0, 255);
			g = (byte)Math.Clamp(WaterG - tint,     0, 255);
			b = (byte)Math.Clamp(WaterB - tint * 3, 0, 255);
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
			// 3-way gradient: 0=icy blue, 128=room copper, 255=lava hot
			if (flow <= 128)
			{
				float tc = flow / 128f;
				r = (byte)(CopperIceR  + (CopperColdR - CopperIceR)  * tc);
				g = (byte)(CopperIceG  + (CopperColdG - CopperIceG)  * tc);
				b = (byte)(CopperIceB  + (CopperColdB - CopperIceB)  * tc);
			}
			else
			{
				float th = (flow - 128) / 127f;
				r = (byte)(CopperColdR + (CopperHotR - CopperColdR) * th);
				g = (byte)(CopperColdG + (CopperHotG - CopperColdG) * th);
				b = (byte)(CopperColdB + (CopperHotB - CopperColdB) * th);
			}
		}
		else if (cell == (byte)Simulation.Cell.Battery)  { r = BatteryR;  g = BatteryG;  b = BatteryB;  }
		else if (cell == (byte)Simulation.Cell.Mirror)   { r = MirrorR;   g = MirrorG;   b = MirrorB;   }
		else if (cell == (byte)Simulation.Cell.Dirt)
		{
			int off = (flow >> 2) - 5;
			r = (byte)Math.Clamp(DirtR + off, 0, 255);
			g = (byte)Math.Clamp(DirtG + off, 0, 255);
			b = (byte)Math.Clamp(DirtB + off, 0, 255);
		}
		else if (cell == (byte)Simulation.Cell.Grass)   { r = GrassR;    g = GrassG;    b = GrassB;    }
		else if (cell == (byte)Simulation.Cell.GrassSeed){ r = GrassSeedR;g = GrassSeedG;b = GrassSeedB;}
		else if (cell == (byte)Simulation.Cell.TreeSeed) { r = TreeSeedR; g = TreeSeedG; b = TreeSeedB; }
		else if (cell == (byte)Simulation.Cell.Bark)
		{
			int off = ((flow * 17) & 0x1F) - 12;
			r = (byte)Math.Clamp(BarkR + off,     0, 255);
			g = (byte)Math.Clamp(BarkG + off / 2, 0, 255);
			b = BarkB;
		}
		else if (cell == (byte)Simulation.Cell.Leaves)        { r = LeavesR; g = LeavesG; b = LeavesB; }
		else if (cell == (byte)Simulation.Cell.LiquidNitrogen){ r = LN2R;    g = LN2G;    b = LN2B;    }
		else if (cell == (byte)Simulation.Cell.Ice)           { r = IceR;    g = IceG;    b = IceB;    }
		else if (cell == (byte)Simulation.Cell.Fire)
		{
			float t2 = Math.Clamp(flow / 40f, 0f, 1f);
			r = (byte)Math.Clamp(200 + (int)(55 * t2), 180, 255);
			g = (byte)Math.Clamp(40  + (int)(100 * t2), 20, 160);
			b = 0;
		}
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
			else if (cell == (byte)Simulation.Cell.Smoke)
			{
				float age = 1f - Math.Clamp(_sim.Flow[i] / 65f, 0f, 1f);
				float sm  = 0.70f * (1f - age * 0.55f);
				r = (byte)(25 * sm + AirR * (1 - sm));
				g = (byte)(25 * sm + AirG * (1 - sm));
				b = (byte)(30 * sm + AirB * (1 - sm));
			}
			else if (cell == (byte)Simulation.Cell.NitrogenGas)
			{
				const float na = 0.48f;
				r = (byte)(LN2R * na + AirR * (1 - na));
				g = (byte)(LN2G * na + AirG * (1 - na));
				b = (byte)(LN2B * na + AirB * (1 - na));
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
		// Heat selection rectangle — white fill + solid white outline
		if (_selectingHeat || _hasHeatResult)
		{
			int x0 = Math.Min(_heatStart.X, _heatEnd.X), x1 = Math.Max(_heatStart.X, _heatEnd.X);
			int y0 = Math.Min(_heatStart.Y, _heatEnd.Y), y1 = Math.Max(_heatStart.Y, _heatEnd.Y);
			var selRect = new Rect2(x0 * Scale, y0 * Scale, (x1 - x0 + 1) * Scale, (y1 - y0 + 1) * Scale);
			c.DrawRect(selRect, new Color(1, 1, 1, 0.12f));           // subtle white fill
			c.DrawRect(selRect, new Color(1, 1, 1, 0.95f), false, 2f); // bright white outline
		}
		// Bezier mirrors — draw committed curves then in-progress stroke (dimmer)
		var mirrorCol   = new Color(0.82f, 0.92f, 1.00f, 0.95f);
		var mirrorColWip = new Color(0.82f, 0.92f, 1.00f, 0.45f);
		foreach (var m in _mirrors)
			m.Draw(c, mirrorCol);
		_mirrorInProgress?.Draw(c, mirrorColWip);
		DrawTurrets(c);
		// Shockwave rings
		foreach (var sw in _shockwaves)
		{
			float a = sw.Life;
			c.DrawArc(sw.Center, sw.Radius, 0, MathF.PI * 2f, 48, new Color(1f, 0.55f, 0.05f, a * 0.30f), 10f);
			c.DrawArc(sw.Center, sw.Radius, 0, MathF.PI * 2f, 48, new Color(1f, 0.85f, 0.35f, a * 0.85f),  2f);
			c.DrawArc(sw.Center, sw.Radius * 0.25f, 0, MathF.PI * 2f, 24, new Color(1f, 0.95f, 0.8f, a * a * 0.6f), 5f);
		}
		DrawBrushCursor(c);
	}

	private void DrawBrushCursor(OverlayCanvas c)
	{
		if (ImGuiNET.ImGui.GetIO().WantCaptureMouse) return;

		var  col    = new Color(1f, 1f, 1f, 0.80f);
		const float lw = 1.5f;
		var  center = new Vector2((_mouseSim.X + 0.5f) * Scale, (_mouseSim.Y + 0.5f) * Scale);

		switch (_brush)
		{
			case BrushTurret:
			{
				// Outline of the 5×3 main block plus the single-cell copper terminals
				// that flank the middle row on both sides.
				float s  = Scale;
				float ox = _mouseSim.X * s;
				float oy = _mouseSim.Y * s;
				var pts = new Vector2[]
				{
					new(ox - 2*s, oy      ),
					new(ox + 3*s, oy      ),
					new(ox + 3*s, oy +   s),
					new(ox + 4*s, oy +   s),
					new(ox + 4*s, oy + 2*s),
					new(ox + 3*s, oy + 2*s),
					new(ox + 3*s, oy + 3*s),
					new(ox - 2*s, oy + 3*s),
					new(ox - 2*s, oy + 2*s),
					new(ox - 3*s, oy + 2*s),
					new(ox - 3*s, oy +   s),
					new(ox - 2*s, oy +   s),
					new(ox - 2*s, oy      ),  // close
				};
				c.DrawPolyline(pts, col, lw);
				break;
			}

			case BrushGlorp:
				c.DrawArc(center, Glorp.SimR * Scale, 0, MathF.PI * 2f, 48, col, lw);
				break;

			case BrushForce:
				// Force radius is brushSize * 3 (matches ApplyForce call)
				c.DrawArc(center, _brushSize * 3 * Scale, 0, MathF.PI * 2f, 64, col, lw);
				break;

			case BrushPin:
			{
				float r = Math.Max(1, _brushSize / 2) * Scale;
				c.DrawArc(center, r, 0, MathF.PI * 2f, 48, col, lw);
				break;
			}

			case BrushHeatView:
			case BrushMirror:
				// HeatView shows its own selection rect; Mirror draws the live curve preview
				break;

			default:
				// All cell-stamping brushes: Sand, Water, Stone, Lava, Gas, Food,
				// Copper, Battery, Wood, Erase, Dirt, GrassSeed, TreeSeed, Fire, LN2
				c.DrawArc(center, (_brushSize + 0.5f) * Scale, 0, MathF.PI * 2f, 64, col, lw);
				break;
		}
	}

	// ── UI state ──────────────────────────────────────────────────────────────

	private void SetBrush(int b)
	{
		if (b != BrushMirror && _mirrorInProgress != null)
		{
			if (_mirrorInProgress.SamplePoints.Count >= 2) _mirrors.Add(_mirrorInProgress);
			_mirrorInProgress = null;
		}
		_brush = b;
		// Clear any heat selection when switching away
		if (b != BrushHeatView) { _selectingHeat = false; _hasHeatResult = false; }

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
		_btnTurret.Modulate    = b == BrushTurret    ? Colors.Yellow : Colors.White;
		_btnMirror.Modulate    = b == BrushMirror    ? Colors.Yellow : Colors.White;
		_btnDirt.Modulate      = b == BrushDirt      ? Colors.Yellow : Colors.White;
		_btnGrassSeed.Modulate = b == BrushGrassSeed ? Colors.Yellow : Colors.White;
		_btnTreeSeed.Modulate  = b == BrushTreeSeed  ? Colors.Yellow : Colors.White;
		_btnFire.Modulate           = b == BrushFire           ? Colors.Yellow : Colors.White;
		_btnLiquidNitrogen.Modulate = b == BrushLiquidNitrogen ? Colors.Yellow : Colors.White;
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

	private static Vector2 ScreenToSimF(Vector2 screen) =>
		new Vector2(screen.X / Scale, screen.Y / Scale);

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
			case BrushWood:    StampCircle(sp.X, sp.Y, (int)Simulation.Cell.Wood);    break;
			case BrushErase:
				StampCircle(sp.X, sp.Y, (int)Simulation.Cell.Air);
				EraseTurretsInRadius(sp.X, sp.Y, _brushSize);
				EraseMirrorsInRadius(sp.X, sp.Y, _brushSize);
				break;
			case BrushForce:     _sim.ApplyForce(sp.X, sp.Y, _brushSize * 3, 6); break;
			case BrushDirt:      StampCircle(sp.X, sp.Y, (int)Simulation.Cell.Dirt);      break;
			case BrushGrassSeed: StampCircle(sp.X, sp.Y, (int)Simulation.Cell.GrassSeed); break;
			case BrushTreeSeed:  StampCircle(sp.X, sp.Y, (int)Simulation.Cell.TreeSeed);  break;
			case BrushFire:           StampFireCircle(sp.X, sp.Y); break;
			case BrushLiquidNitrogen: StampCircle(sp.X, sp.Y, (int)Simulation.Cell.LiquidNitrogen); break;
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

	private void StampFireCircle(int cx, int cy)
	{
		int r = _brushSize;
		for (int dy = -r; dy <= r; dy++)
		for (int dx = -r; dx <= r; dx++)
			if (dx * dx + dy * dy <= r * r)
				_sim.SetFire(cx + dx, cy + dy);
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
		string[] parts = raw.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length == 0) return;

		switch (parts[0].ToLower())
		{
			case "help":
				ConsoleLog("[color=yellow]Commands:[/color]");
				ConsoleLog("  [color=white]tps <n>[/color]           simulation ticks per second (1–120)");
				ConsoleLog("  [color=white]brush <n>[/color]         brush size (1–20)");
				ConsoleLog("  [color=white]clear[/color]             wipe the grid, pins, and wood");
				ConsoleLog("  [color=white]boil <n>[/color]          copper boil threshold (0–255)");
				ConsoleLog("  [color=white]gasthresh <n>[/color]     copper gas-ignite threshold (0–255)");
				ConsoleLog("  [color=white]firerate <0-1>[/color]    fire ignite chance per tick (default 0.12)");
				ConsoleLog("  [color=white]fireticks <n>[/color]     base fire lifetime in ticks (default 30)");
				ConsoleLog("  [color=white]seedrate <0-1>[/color]    grass seed sprout chance per tick (default 0.003)");
				ConsoleLog("  [color=white]treerate <0-1>[/color]    tree seed grow chance per tick (default 0.001)");
				ConsoleLog("  [color=white]icethresh <n>[/color]     copper temp below which it freezes water (default 64)");
				ConsoleLog("  [color=white]laserfalloff <0-1>[/color] laser power multiplier per bounce (0=instant, 1=no decay, default 0.4)");
				ConsoleLog("  [color=white]lasermax <n>[/color]      max mirror bounces per beam (default 12)");
				ConsoleLog("  [color=white]mirrordist <f>[/color]    mirror raw sample chord length in sim units (default 1.0)");
				ConsoleLog("  [color=white]mirrorepsilon <f>[/color] mirror RDP simplification threshold in sim units (default 1.5)");
				ConsoleLog("  [color=gray]Tab to autocomplete[/color]");
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
				foreach (var gl in _glorps) gl.QueueFree();
				_glorps.Clear(); _selectedGlorp = null;
				_turrets.Clear();
				_mirrors.Clear(); _mirrorInProgress = null;
				_sim.RenderDirty = true;
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

			case "firerate" when parts.Length > 1 && float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float fr):
				_sim.FireIgniteChance = Math.Clamp(fr, 0f, 1f);
				ConsoleLog($"[color=cyan]Fire ignite chance → {_sim.FireIgniteChance:F3}[/color]");
				break;

			case "fireticks" when parts.Length > 1 && int.TryParse(parts[1], out int ft):
				_sim.FireBaseTicks = Math.Clamp(ft, 1, 200);
				ConsoleLog($"[color=cyan]Fire base ticks → {_sim.FireBaseTicks}[/color]");
				break;

			case "seedrate" when parts.Length > 1 && float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float sr):
				_sim.GrassSeedRate = Math.Clamp(sr, 0f, 1f);
				ConsoleLog($"[color=cyan]Grass seed rate → {_sim.GrassSeedRate:F4}[/color]");
				break;

			case "treerate" when parts.Length > 1 && float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float tr):
				_sim.TreeSeedRate = Math.Clamp(tr, 0f, 1f);
				ConsoleLog($"[color=cyan]Tree seed rate → {_sim.TreeSeedRate:F4}[/color]");
				break;

			case "icethresh" when parts.Length > 1 && int.TryParse(parts[1], out int it):
				_sim.IceCopperThreshold = Math.Clamp(it, 0, 127);
				ConsoleLog($"[color=cyan]Ice copper threshold → {_sim.IceCopperThreshold}[/color]");
				break;

			case "laserfalloff" when parts.Length > 1 && float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float lf):
				_laserFalloff = Math.Clamp(lf, 0f, 1f);
				ConsoleLog($"[color=cyan]Laser falloff → {_laserFalloff:F2}[/color]");
				break;

			case "lasermax" when parts.Length > 1 && int.TryParse(parts[1], out int lm):
				_laserMaxBounces = Math.Clamp(lm, 0, 64);
				ConsoleLog($"[color=cyan]Laser max bounces → {_laserMaxBounces}[/color]");
				break;

			case "mirrordist" when parts.Length > 1 && float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float md):
				BezierMirror.RawMinDist = Math.Clamp(md, 0.2f, 20f);
				ConsoleLog($"[color=cyan]Mirror sample dist → {BezierMirror.RawMinDist:F2}[/color]");
				break;

			case "mirrorepsilon" when parts.Length > 1 && float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float me):
				BezierMirror.RdpEpsilon = Math.Clamp(me, 0f, 30f);
				ConsoleLog($"[color=cyan]Mirror RDP epsilon → {BezierMirror.RdpEpsilon:F2}[/color]");
				break;

			default:
				ConsoleLog($"[color=red]Unknown command '{parts[0]}'. Type 'help'.[/color]");
				break;
		}
	}

	// ConsoleLog is defined in Main.DebugGui.cs — routes to ImGui console tab

	// ── Laser Turrets ──────────────────────────────────────────────────────────

	private void PlaceTurret(Vector2I origin)
	{
		for (int dy = 0; dy < LaserTurret.BaseH; dy++)
		for (int dx = -LaserTurret.BaseHalfW; dx <= LaserTurret.BaseHalfW; dx++)
			if (!_sim.InBounds(origin.X + dx, origin.Y + dy)) return;
		_turrets.Add(LaserTurret.Place(_sim, origin));
	}

	private void UpdateTurrets()
	{
		foreach (var t in _turrets)
		{
			t.Powered = t.CheckPowered(_sim);
			t.UpdateAngle(_mouseSim);
		}
	}

	private void EraseMirrorsInRadius(int cx, int cy, int radius)
	{
		float r = radius + 1f;
		for (int i = _mirrors.Count - 1; i >= 0; i--)
			if (_mirrors[i].IsNearCurve(cx, cy, r))
				_mirrors.RemoveAt(i);
	}

	private void EraseTurretsInRadius(int cx, int cy, int radius)
	{
		for (int i = _turrets.Count - 1; i >= 0; i--)
		{
			var t = _turrets[i];
			bool hit = false;
			for (int dy = -radius; dy <= radius && !hit; dy++)
			for (int dx = -radius; dx <= radius && !hit; dx++)
			{
				if (dx * dx + dy * dy > radius * radius) continue;
				if (t.ContainsIndex((cy + dy) * SimW + (cx + dx))) hit = true;
			}
			if (!hit) continue;
			t.Remove(_sim);
			_turrets.RemoveAt(i);
		}
	}

	private void DrawTurrets(OverlayCanvas c)
	{
		const float barrelPx = 4 * Scale;
		var barrelCol = new Color(0.12f, 0.12f, 0.12f);

		foreach (var t in _turrets)
		{
			var pivotScr = new Vector2(t.Origin.X * Scale + Scale * 0.5f,
									   t.Origin.Y * Scale + Scale * 0.5f);
			var dir     = new Vector2(MathF.Cos(t.Angle), MathF.Sin(t.Angle));
			var tipScr  = pivotScr + dir * barrelPx;
			c.DrawLine(pivotScr, tipScr, barrelCol, 3f);

			if (!t.Powered) continue;

			var startSim  = new Vector2(t.Origin.X + dir.X * 4.5f,
										t.Origin.Y + dir.Y * 4.5f);
			var waypoints = CastLaserRay(startSim, dir, 300f, new Vector2(t.Origin.X, t.Origin.Y));
			Vector2 prevScr  = tipScr;
			float   segPower = 1.0f;
			for (int w = 1; w < waypoints.Count; w++)
			{
				var segEnd = new Vector2(waypoints[w].X * Scale, waypoints[w].Y * Scale);
				int steps  = Math.Max(1, (int)((segEnd - prevScr).Length() / 8f));
				for (int s = 0; s < steps; s++)
				{
					var p0 = prevScr.Lerp(segEnd, (float)s       / steps);
					var p1 = prevScr.Lerp(segEnd, (float)(s + 1) / steps);
					c.DrawLine(p0, p1, new Color(1f, 0.2f,  0f,    0.20f * segPower), 6f);
					c.DrawLine(p0, p1, new Color(1f, 0.45f, 0.1f,  0.85f * segPower), 2.5f);
					c.DrawLine(p0, p1, new Color(1f, 0.9f,  0.85f, 0.95f * segPower), 1f);
				}
				segPower *= _laserFalloff;
				prevScr   = segEnd;
			}
		}
	}

	private List<Vector2> CastLaserRay(Vector2 startSim, Vector2 dir, float segRange, Vector2 turretOrigin)
	{
		var waypoints = new List<Vector2> { startSim };
		Vector2 pos    = startSim;
		Vector2 curDir = dir;

		for (int bounce = 0; bounce <= _laserMaxBounces; bounce++)
		{
			// Each segment gets a fresh range budget — exhaustion of one segment
			// never prevents detection on the next.
			float remaining = segRange;

			// Find the closest Bezier mirror hit within this segment's budget
			float   bestMirrorDist = float.MaxValue;
			Vector2 bestMirrorNorm = default;
			foreach (var m in _mirrors)
			{
				if (m.Intersect(pos, curDir, remaining, out float md, out Vector2 mn) && md < bestMirrorDist)
				{ bestMirrorDist = md; bestMirrorNorm = mn; }
			}
			if (_mirrorInProgress is { } wip && wip.SamplePoints.Count >= 2)
			{
				if (wip.Intersect(pos, curDir, remaining, out float md, out Vector2 mn) && md < bestMirrorDist)
				{ bestMirrorDist = md; bestMirrorNorm = mn; }
			}

			// March through the cell grid — stop before the mirror if one was found
			float marchLimit = MathF.Min(bestMirrorDist - 0.3f, remaining);
			for (float dist = 0.5f; dist <= marchLimit; dist += 0.5f)
			{
				Vector2 p  = pos + curDir * dist;
				int gx = (int)p.X, gy = (int)p.Y;
				if (!_sim.InBounds(gx, gy)) { waypoints.Add(p); return waypoints; }
				byte cell = _sim.Grid[gy * SimW + gx];
				if (cell == (byte)Simulation.Cell.Air   ||
					cell == (byte)Simulation.Cell.Gas   ||
					cell == (byte)Simulation.Cell.Steam) continue;
				if (cell == (byte)Simulation.Cell.Sand  ||
					cell == (byte)Simulation.Cell.Water ||
					cell == (byte)Simulation.Cell.Food  ||
					cell == (byte)Simulation.Cell.Lava)
				{
					if (_rng.NextSingle() < 0.25f)
						_sim.SetCell(gx, gy, (int)Simulation.Cell.Air);
				}
				waypoints.Add(p);
				return waypoints;
			}

			if (bestMirrorDist <= remaining)
			{
				var hitPos = pos + curDir * bestMirrorDist;
				waypoints.Add(hitPos);
				pos    = hitPos;
				curDir = (curDir - 2f * curDir.Dot(bestMirrorNorm) * bestMirrorNorm).Normalized();
			}
			else
			{
				waypoints.Add(pos + curDir * remaining);
				return waypoints;
			}
		}
		waypoints.Add(pos + curDir * segRange);
		return waypoints;
	}

	// ── LaserTurret ────────────────────────────────────────────────────────────

	private sealed class LaserTurret
	{
		public const int BaseHalfW = 2;
		public const int BaseH     = 3;

		public Vector2I           Origin;
		public float              Angle;
		public bool               Powered;
		public readonly List<int> OccupiedIndices = new();

		public static LaserTurret Place(Simulation sim, Vector2I origin)
		{
			var t = new LaserTurret { Origin = origin };
			for (int dy = 0; dy < BaseH; dy++)
			for (int dx = -BaseHalfW; dx <= BaseHalfW; dx++)
			{
				int gx = origin.X + dx, gy = origin.Y + dy;
				if (!sim.InBounds(gx, gy)) continue;
				int idx = gy * Simulation.SimW + gx;
				sim.Grid[idx]   = (byte)(dy == 0 && dx == 0
					? Simulation.Cell.Battery
					: Simulation.Cell.Stone);
				sim.Flow[idx]   = 0;
				sim.Pinned[idx] = 1;
				t.OccupiedIndices.Add(idx);
			}
			// Copper terminals on both sides of the middle row — power input ports
			int termRow = origin.Y + 1;
			foreach (int termX in new[] { origin.X - BaseHalfW - 1, origin.X + BaseHalfW + 1 })
			{
				if (!sim.InBounds(termX, termRow)) continue;
				int idx = termRow * Simulation.SimW + termX;
				sim.Grid[idx]   = (byte)Simulation.Cell.Copper;
				sim.Flow[idx]   = 0;
				sim.Pinned[idx] = 1;
				t.OccupiedIndices.Add(idx);
			}
			sim.RenderDirty = true;
			return t;
		}

		public bool CheckPowered(Simulation sim)
		{
			int row    = Origin.Y + 1;
			int leftX  = Origin.X - BaseHalfW - 1;
			int rightX = Origin.X + BaseHalfW + 1;
			return (sim.InBounds(leftX,  row) && sim.Electric[row * Simulation.SimW + leftX]  != 0)
				|| (sim.InBounds(rightX, row) && sim.Electric[row * Simulation.SimW + rightX] != 0);
		}

		public void UpdateAngle(Vector2I mouseSimPos)
		{
			float dx = mouseSimPos.X - Origin.X;
			float dy = mouseSimPos.Y - Origin.Y;
			if (dx * dx + dy * dy > 0.01f)
				Angle = MathF.Atan2(dy, dx);
		}

		public void Remove(Simulation sim)
		{
			foreach (int idx in OccupiedIndices)
			{
				sim.Grid[idx]   = (byte)Simulation.Cell.Air;
				sim.Pinned[idx] = 0;
				sim.Flow[idx]   = 0;
			}
			sim.RenderDirty = true;
			OccupiedIndices.Clear();
		}

		public bool ContainsIndex(int idx) => OccupiedIndices.Contains(idx);
	}

	// ── BezierMirror ───────────────────────────────────────────────────────────
	// A Catmull-Rom spline fitted live through the user's brush samples.
	// Purely geometric — no grid cells. Intersection uses analytical Bezier
	// tangent for normals; turret origin picks the facing side via dot product.

	private sealed class BezierMirror
	{
		// Tunable via console — shared by all mirrors drawn after the change
		public static float RawMinDist  = 1.0f;  // chord length between raw samples (sim units)
		public static float RdpEpsilon  = 1.5f;  // RDP perpendicular-deviation threshold (sim units)

		private const int IntersectSteps = 24;
		private const int DrawSteps      = 16;

		// Raw dense samples collected during the stroke
		private readonly List<Vector2> _raw = new();

		// RDP-simplified control points — spline is built from these
		public readonly List<Vector2> SamplePoints = new();

		private struct Seg { public Vector2 P0, P1, P2, P3; }
		private readonly List<Seg> _segs = new();

		public void AddSample(Vector2 p)
		{
			if (_raw.Count > 0 &&
				(p - _raw[^1]).LengthSquared() < RawMinDist * RawMinDist)
				return;
			_raw.Add(p);

			// Rebuild RDP-simplified control points (always includes first and last raw point)
			SamplePoints.Clear();
			SamplePoints.Add(_raw[0]);
			if (_raw.Count > 1)
				RdpRecurse(_raw, 0, _raw.Count - 1, RdpEpsilon * RdpEpsilon, SamplePoints);

			if (SamplePoints.Count >= 2) Rebuild();
		}

		// Ramer–Douglas–Peucker — `lo` is already in `result`.
		// This call adds indices lo+1 … hi with no duplicates.
		private static void RdpRecurse(List<Vector2> pts, int lo, int hi,
									   float epsSq, List<Vector2> result)
		{
			if (hi <= lo + 1) { result.Add(pts[hi]); return; }

			var a = pts[lo]; var b = pts[hi];
			var ab = b - a; float abLen2 = ab.LengthSquared();

			float maxSq = 0f; int maxIdx = lo + 1;
			for (int i = lo + 1; i < hi; i++)
			{
				float sq = abLen2 > 1e-12f
					? PerpendicularDistSq(pts[i], a, ab, abLen2)
					: (pts[i] - a).LengthSquared();
				if (sq > maxSq) { maxSq = sq; maxIdx = i; }
			}

			if (maxSq > epsSq)
			{
				RdpRecurse(pts, lo,     maxIdx, epsSq, result); // adds lo+1 … maxIdx
				RdpRecurse(pts, maxIdx, hi,     epsSq, result); // adds maxIdx+1 … hi
			}
			else
			{
				result.Add(pts[hi]); // interior collinear — skip it, keep endpoint
			}
		}

		private static float PerpendicularDistSq(Vector2 p, Vector2 a, Vector2 ab, float abLen2)
		{
			float t = Math.Clamp(((p.X - a.X) * ab.X + (p.Y - a.Y) * ab.Y) / abLen2, 0f, 1f);
			float ex = a.X + t * ab.X - p.X;
			float ey = a.Y + t * ab.Y - p.Y;
			return ex * ex + ey * ey;
		}

		// Chordal Catmull-Rom (α=1): tangents scale with local chord length.
		// A short segment between two long ones can't produce an overshooting
		// handle, which is what caused the self-intersection loops.
		private void Rebuild()
		{
			_segs.Clear();
			int n = SamplePoints.Count;
			for (int i = 0; i < n - 1; i++)
			{
				// Reflection phantoms at chain ends → natural tangent (handle = chord/3)
				var p0 = i > 0     ? SamplePoints[i - 1] : 2f * SamplePoints[i] - SamplePoints[i + 1];
				var p1 = SamplePoints[i];
				var p2 = SamplePoints[i + 1];
				var p3 = i + 2 < n ? SamplePoints[i + 2] : 2f * SamplePoints[i + 1] - SamplePoints[i];

				float d01 = (p1 - p0).Length();
				float d12 = (p2 - p1).Length();
				float d23 = (p3 - p2).Length();

				if (d12 < 1e-6f) continue;

				Vector2 b1 = d01 < 1e-6f
					? p1 + (p2 - p1) / 3f
					: p1 + ((p2 - p1) / d12 - (p2 - p0) / (d01 + d12) + (p1 - p0) / d01) * (d12 / 3f);

				Vector2 b2 = d23 < 1e-6f
					? p2 - (p2 - p1) / 3f
					: p2 - ((p3 - p2) / d23 - (p3 - p1) / (d12 + d23) + (p2 - p1) / d12) * (d12 / 3f);

				_segs.Add(new Seg { P0 = p1, P1 = b1, P2 = b2, P3 = p2 });
			}
		}

		private static Vector2 Eval(in Seg s, float t)
		{
			float u  = 1f - t;
			return u*u*u*s.P0 + 3f*u*u*t*s.P1 + 3f*u*t*t*s.P2 + t*t*t*s.P3;
		}

		// Analytical first derivative — gives the tangent direction at parameter t
		private static Vector2 Tangent(in Seg s, float t)
		{
			float u = 1f - t;
			return 3f * (u*u*(s.P1 - s.P0) + 2f*u*t*(s.P2 - s.P1) + t*t*(s.P3 - s.P2));
		}

		// Ray–Bezier intersection. Returns true with the ray distance and the
		// surface normal pointing toward `origin` (the firing turret).
		public bool Intersect(Vector2 origin, Vector2 dir, float maxDist,
							  out float hitDist, out Vector2 normal)
		{
			hitDist = float.MaxValue;
			normal  = default;

			foreach (var seg in _segs)
			{
				Vector2 prev = Eval(seg, 0f);
				for (int k = 1; k <= IntersectSteps; k++)
				{
					float   t   = k / (float)IntersectSteps;
					Vector2 cur = Eval(seg, t);
					var e = cur - prev;

					// 2D ray–segment: origin + d*dir = prev + s*e
					// denom = dir × e  (2D cross)
					float denom = dir.X * e.Y - dir.Y * e.X;
					if (MathF.Abs(denom) < 1e-6f) { prev = cur; continue; }

					var   f = prev - origin;
					float d = (f.X * e.Y - f.Y * e.X) / denom;  // distance along ray
					float s = -(dir.X * f.Y - dir.Y * f.X) / denom; // fraction along edge

					if (s < 0f || s > 1f || d < 0.3f || d >= maxDist) { prev = cur; continue; }

					if (d < hitDist)
					{
						hitDist = d;
						float hitT = Math.Clamp(((k - 1) + s) / IntersectSteps, 0.001f, 0.999f);

						var tangent = Tangent(seg, hitT);
						if (tangent.LengthSquared() < 1e-6f) tangent = e;

						// Two candidate normals — pick the one facing the turret
						var n1 = new Vector2(-tangent.Y,  tangent.X).Normalized();
						var n2 = new Vector2( tangent.Y, -tangent.X).Normalized();
						var toOrigin = origin - (prev + s * e);
						normal = n1.Dot(toOrigin) >= 0f ? n1 : n2;
					}
					prev = cur;
				}
			}
			return hitDist < maxDist;
		}

		public void Draw(OverlayCanvas c, Color col)
		{
			foreach (var seg in _segs)
			{
				Vector2 prev = Eval(seg, 0f);
				for (int k = 1; k <= DrawSteps; k++)
				{
					var cur = Eval(seg, k / (float)DrawSteps);
					c.DrawLine(prev * Scale, cur * Scale, col, 2f);
					prev = cur;
				}
			}
		}

		// Proximity check against the fitted curve polyline (used for erase)
		public bool IsNearCurve(float cx, float cy, float radius)
		{
			float r2 = radius * radius;
			foreach (var seg in _segs)
			{
				Vector2 prev = Eval(seg, 0f);
				for (int k = 1; k <= DrawSteps; k++)
				{
					var cur = Eval(seg, k / (float)DrawSteps);
					// Closest point on line segment prev→cur to (cx,cy)
					var  ab  = cur - prev;
					float len2 = ab.LengthSquared();
					float tx  = cx - prev.X, ty = cy - prev.Y;
					float proj = len2 > 0f ? Math.Clamp((tx*ab.X + ty*ab.Y) / len2, 0f, 1f) : 0f;
					float ex = prev.X + proj*ab.X - cx;
					float ey = prev.Y + proj*ab.Y - cy;
					if (ex*ex + ey*ey <= r2) return true;
					prev = cur;
				}
			}
			return false;
		}
	}
}

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
	private const int BrushArm            = -10;
	private const int BrushTrack          = -12;

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
	private Button _btnTabMaterials, _btnTabSettings, _btnTabAnalysis, _btnTabQuick, _detachBtn;
	private Button _btnHeatView, _btnPin, _btnTurret, _btnMirror;
	private Button _btnDirt, _btnGrassSeed, _btnTreeSeed, _btnFire, _btnLiquidNitrogen, _btnArm;
	private Control _materialsPage, _settingsPage, _analysisPage, _quickSelectPage;
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
	private const float  PanelHiddenY   = -380f;  // must equal the panel's height in main.tscn
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

	// Rail tracks
	private readonly List<RailTrack> _tracks = new();
	private RailTrack _trackInProgress;
	private Button    _btnTrack;
	private object    _trackDragMachine; // LaserTurret or RoboArm being dragged along a track

	// Wire overlay system
	private readonly List<WireNode> _wireNodes     = new();
	private int     _wirePendingIdx = -1;    // first-click node awaiting second click; -1 = none
	private bool    _wireModeActive = false;
	private float   _wireSnapRadius = 8f;    // sim-cell radius for snapping to batteries/terminals/nodes
	private Vector2 _mouseSimF;              // float-precision mouse in sim space (for wire ghost)

	// Context-sensitive Space — closest machine within this radius is the Space target each frame
	private const float MachineSpaceRadius = 6f;
	private LaserTurret _spaceTargetTurret;
	private RoboArm     _spaceTargetArm;

	// Quick select radial
	private bool    _radialOpen         = false;
	private Vector2 _radialOriginScreen;
	private int     _radialHoveredSlot  = -1;   // -1 = dead zone; 0-5 = slice index
	private double  _radialInputCooldown = 0.0; // seconds before brush input resumes after close

	// Robotic arms
	private readonly List<RoboArm> _arms = new();
	private RoboArm _activeArm;                   // most recently dragged — receives Space toggle
	private (RoboArm arm, int joint)? _armDrag;   // joint: 0 = elbow, 1 = claw
	private int _pincerHalfWidth = 1;             // 0=1cell, 1=3cell, 2=5cell, …
	private int _pincerDepth    = 1;             // rows grabbed along the forearm (1=single row)
	// Default whitelist: every cell type that can be picked up/moved in the sim
	private readonly HashSet<byte> _clawWhitelist = new()
	{
		(byte)Simulation.Cell.Sand,    (byte)Simulation.Cell.Water,   (byte)Simulation.Cell.Lava,
		(byte)Simulation.Cell.Gas,     (byte)Simulation.Cell.Food,    (byte)Simulation.Cell.Dirt,
		(byte)Simulation.Cell.Fire,    (byte)Simulation.Cell.Smoke,   (byte)Simulation.Cell.Steam,
		(byte)Simulation.Cell.LiquidNitrogen, (byte)Simulation.Cell.NitrogenGas,
		(byte)Simulation.Cell.GrassSeed, (byte)Simulation.Cell.TreeSeed,
	};

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
		_btnArm             = GetNode<Button>(g + "BtnArm");
		_btnTrack           = GetNode<Button>(g + "BtnTrack");

		_slider      = GetNode<HSlider>("UI/ToolBox/Panel/VBoxContainer/MaterialsPage/SizeSlider");
		_speedSlider = GetNode<HSlider>("UI/ToolBox/Panel/VBoxContainer/SettingsPage/SpeedSlider");

		_btnTabMaterials = GetNode<Button>("UI/ToolBox/Panel/VBoxContainer/TabBar/BtnTabMaterials");
		_btnTabSettings  = GetNode<Button>("UI/ToolBox/Panel/VBoxContainer/TabBar/BtnTabSettings");
		_btnTabAnalysis  = GetNode<Button>("UI/ToolBox/Panel/VBoxContainer/TabBar/BtnTabAnalysis");
		_btnTabQuick     = GetNode<Button>("UI/ToolBox/Panel/VBoxContainer/TabBar/BtnTabQuick");
		_detachBtn       = GetNode<Button>("UI/ToolBox/Tab/DetachBtn");

		_materialsPage    = GetNode<Control>("UI/ToolBox/Panel/VBoxContainer/MaterialsPage");
		_settingsPage     = GetNode<Control>("UI/ToolBox/Panel/VBoxContainer/SettingsPage");
		_analysisPage     = GetNode<Control>("UI/ToolBox/Panel/VBoxContainer/AnalysisPage");
		_quickSelectPage  = GetNode<Control>("UI/ToolBox/Panel/VBoxContainer/QuickSelectPage");

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
		_btnArm.Pressed            += () => SetBrush(BrushArm);
		_btnTrack.Pressed          += () => SetBrush(BrushTrack);
		_slider.ValueChanged      += v => _brushSize      = (int)v;
		_speedSlider.ValueChanged += v => _ticksPerSecond = (int)v;

		_btnTabMaterials.Pressed += () => SetActiveTab(0);
		_btnTabSettings.Pressed  += () => SetActiveTab(1);
		_btnTabAnalysis.Pressed  += () => SetActiveTab(2);
		_btnTabQuick.Pressed     += () => SetActiveTab(3);
		_detachBtn.Pressed       += ToggleDetach;

		InitQuickSelect();
		ApplyMaterialButtonColors();
		InitScripts();
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
		_scriptTicksThisFrame = 0;
		while (_tickAccum >= interval)
		{
			_sim.Update();
			_tickAccum -= interval;
			_scriptTicksThisFrame++;
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
		_mouseSim  = ScreenToSim(mouse);
		_mouseSimF = ScreenToSimF(mouse);
		// Wire power runs after the last sim tick so it writes into Electric[] after
		// PropagateElectricity has built the copper network for this frame.
		PropagateWirePower();
		UpdateTracks();
		UpdateMachineSpaceTarget();
		UpdateTurrets();
		UpdateArms();
		TickPreview();

		// Update radial hover every frame (before UI early-returns so it's always live)
		if (_radialOpen) UpdateRadialHover(mouse);

		// ImGui must be submitted every frame — do this before any early returns
		DrawDebugGui();

		if (!_detached)
		{
			// Trigger on the small tab strip only; keep open while mouse is over the panel.
			bool inToolBox = _tab.GetGlobalRect().HasPoint(mouse)
				|| (_toolBoxExpanded && _panel.GetGlobalRect().HasPoint(mouse));
			if (inToolBox != _toolBoxExpanded)
			{
				if (inToolBox) ShowPanel(); else HidePanel();
			}
		}

		// Block game input when any UI owns the mouse
		if (_scriptEditorOpen) return;
		if (ImGuiNET.ImGui.GetIO().WantCaptureMouse) return;
		if (_tab.GetGlobalRect().HasPoint(mouse)) return;
		if (_detached  && _toolBox.GetGlobalRect().HasPoint(mouse)) return;
		if (!_detached && _toolBoxExpanded && _panel.GetGlobalRect().HasPoint(mouse)) return;

		bool lmbHeld = Input.IsMouseButtonPressed(MouseButton.Left);
		bool rmbHeld = Input.IsMouseButtonPressed(MouseButton.Right);

		// Track machine drag overrides all brush behavior while held
		if (lmbHeld && _trackDragMachine != null)
		{
			UpdateTrackMachineDrag(ScreenToSimF(mouse));
			return;
		}

		// Arm joint drag overrides all brush behavior while held
		if (lmbHeld && _armDrag != null)
		{
			UpdateArmDrag(ScreenToSimF(mouse));
			return;
		}

		// Wire mode and radial: all interaction handled in _Input
		if (_wireModeActive) return;
		if (_radialOpen)     return;

		// Post-radial cooldown — suppress brush input briefly so the releasing button
		// doesn't immediately paint the cell under the cursor.
		if (_radialInputCooldown > 0) { _radialInputCooldown -= delta; return; }

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
				case BrushTrack:
					_trackInProgress?.AddSample(ScreenToSimF(mouse));
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
			else if (_brush == BrushScript)
				DetachScriptAt(simPos);
			else
			{
				StampCircle(simPos.X, simPos.Y, (int)Simulation.Cell.Air);
				EraseTurretsInRadius(simPos.X, simPos.Y, _brushSize);
				EraseMirrorsInRadius(simPos.X, simPos.Y, _brushSize);
				EraseArmsInRadius(simPos.X, simPos.Y, _brushSize);
			}
		}
		// Track detach/delete is handled as a one-shot on RMB press (see _Input)
	}

	// ── Input ─────────────────────────────────────────────────────────────────

	public override void _Input(InputEvent @event)
	{
		// Debug panel toggle + arm claw toggle
		if (@event is InputEventKey k && k.Pressed && !k.Echo)
		{
			if (k.Keycode == Key.Quoteleft)
			{
				_showDebugGui = !_showDebugGui;
				_debugWinGeomLoaded = false; // re-apply saved position on next open
				GetViewport().SetInputAsHandled();
				return;
			}
			if (k.Keycode == Key.Z && !ImGuiNET.ImGui.GetIO().WantCaptureKeyboard)
			{
				_wireModeActive = !_wireModeActive;
				if (!_wireModeActive) _wirePendingIdx = -1; // cancel pending wire on exit
				GetViewport().SetInputAsHandled();
				return;
			}
			if (k.Keycode == Key.S && !ImGuiNET.ImGui.GetIO().WantCaptureKeyboard)
			{
				ToggleScriptEditor();
				GetViewport().SetInputAsHandled();
				return;
			}
			if (k.Keycode == Key.Escape && _wireModeActive && !ImGuiNET.ImGui.GetIO().WantCaptureKeyboard)
			{
				if (_wirePendingIdx >= 0) _wirePendingIdx = -1; // cancel pending first, then exit on next Esc
				else _wireModeActive = false;
				GetViewport().SetInputAsHandled();
				return;
			}
			// Yield Space to ImGui when a text input is focused (e.g. console tab).
		if (k.Keycode == Key.Space && !ImGuiNET.ImGui.GetIO().WantCaptureKeyboard)
			{
				// Context-sensitive: near turret → toggle freeze; near arm → toggle claw.
				// Falls back to the most-recently-dragged arm if nothing else is near.
				if (_spaceTargetTurret != null)
				{
					_spaceTargetTurret.Frozen = !_spaceTargetTurret.Frozen;
					_sim.RenderDirty = true;
				}
				else if (_spaceTargetArm != null)
				{
					ToggleArmClaw(_spaceTargetArm);
				}
				else
				{
					ToggleActiveArmClaw();
				}
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
			bool overUI = _scriptEditorOpen
						|| ImGuiNET.ImGui.GetIO().WantCaptureMouse
						|| _tab.GetGlobalRect().HasPoint(mouse)
						|| (_toolBoxExpanded && _panel.GetGlobalRect().HasPoint(mouse))
						|| (_detached        && _toolBox.GetGlobalRect().HasPoint(mouse));

			// Hard guard for PRESS events: when any UI owns the mouse, ignore the
			// press so nothing in Main reacts (wheel scrolls, RMB Glorp select, etc.)
			// Release events still flow through so drag-state cleanup can run.
			if (mb.Pressed && overUI) return;

			if (mb.Pressed)
			{
				// Both buttons held simultaneously opens the radial quick-select menu
				if (!_radialOpen && !overUI &&
					((mb.ButtonIndex == MouseButton.Left  && Input.IsMouseButtonPressed(MouseButton.Right)) ||
					 (mb.ButtonIndex == MouseButton.Right && Input.IsMouseButtonPressed(MouseButton.Left))))
				{
					OpenRadial(mb.Position);
					_suppressRightErase = true;
					GetViewport().SetInputAsHandled();
					return;
				}

				// Wire mode intercepts LMB and RMB before all other brush logic
				if (_wireModeActive && !overUI)
				{
					if (mb.ButtonIndex == MouseButton.Left)
					{
						PlaceWireClick(ScreenToSimF(mouse));
						GetViewport().SetInputAsHandled();
						return;
					}
					if (mb.ButtonIndex == MouseButton.Right)
					{
						TryDeleteWireNode(ScreenToSimF(mouse));
						GetViewport().SetInputAsHandled();
						return;
					}
				}

				if (mb.ButtonIndex == MouseButton.Left)
				{
					// Track machine drag — skip when BrushScript is active so script-attach clicks reach their handler
					if (!overUI && _brush != BrushScript)
					{
						var trackMach = FindNearestTrackMachine(ScreenToSimF(mouse));
						if (trackMach != null)
						{
							_trackDragMachine = trackMach;
							GetViewport().SetInputAsHandled();
							return;
						}
					}

					// Arm-joint drag takes priority over every brush — clicking near
					// any elbow or claw grabs that joint regardless of current tool.
					if (!overUI)
					{
						var hit = FindClosestJoint(ScreenToSimF(mouse));
						if (hit != null)
						{
							_armDrag   = hit;
							_activeArm = hit.Value.arm;
							GetViewport().SetInputAsHandled();
							return;
						}
					}

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
						var simF = ScreenToSimF(mouse);
						var snap = FindNearestTrackSnap(simF);
						if (snap.HasValue) PlaceTrackTurret(snap.Value.track, snap.Value.t);
						else              PlaceTurret(ScreenToSim(mouse));
					}
					else if (_brush == BrushArm && !overUI)
					{
						var simF = ScreenToSimF(mouse);
						var snap = FindNearestTrackSnap(simF);
						if (snap.HasValue) PlaceTrackArm(snap.Value.track, snap.Value.t);
						else              PlaceArm(ScreenToSim(mouse));
					}
					else if (_brush == BrushTrack && !overUI)
					{
						_trackInProgress = new RailTrack();
						_trackInProgress.AddSample(ScreenToSimF(mouse));
					}
					else if (_brush == BrushScript && !overUI)
					{
						AttachActiveScriptAt(ScreenToSim(mouse));
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
					if (hit != null)
					{
						SelectGlorp(hit);
						_suppressRightErase = true;
					}
					else if (!overUI && _brush != BrushScript && TryTrackRmb(ScreenToSimF(mb.Position)))
					{
						_suppressRightErase = true;
					}
					else
					{
						_suppressRightErase = false;
					}
				}
				else if (mb.ButtonIndex == MouseButton.WheelUp)
				{ _brushSize = Math.Min(_brushSize + 1, 20); _slider.Value = _brushSize; }
				else if (mb.ButtonIndex == MouseButton.WheelDown)
				{ _brushSize = Math.Max(_brushSize - 1, 1); _slider.Value = _brushSize; }
			}
			else // released
			{
				// Either button released while radial is open → close and apply selection
				if (_radialOpen && (mb.ButtonIndex == MouseButton.Left || mb.ButtonIndex == MouseButton.Right))
				{
					CloseRadial();
					GetViewport().SetInputAsHandled();
					return;
				}

				if (mb.ButtonIndex == MouseButton.Left)
				{
					if (_trackDragMachine != null)
					{
						_trackDragMachine = null;
					}
					else if (_armDrag != null)
					{
						_armDrag = null;
					}
					else if (_brush == BrushHeatView && _selectingHeat)
					{
						_selectingHeat = false;
						_hasHeatResult = true;
					}
					else if (_trackInProgress != null)
					{
						if (_trackInProgress.SamplePoints.Count >= 2)
							_tracks.Add(_trackInProgress);
						_trackInProgress = null;
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
		DrawTracks(c);
		DrawTurrets(c);
		DrawArms(c);
		// Wire overlay — only visible while wire mode is active (Z to toggle)
		if (_wireModeActive) DrawWires(c);
		// Shockwave rings
		foreach (var sw in _shockwaves)
		{
			float a = sw.Life;
			c.DrawArc(sw.Center, sw.Radius, 0, MathF.PI * 2f, 48, new Color(1f, 0.55f, 0.05f, a * 0.30f), 10f);
			c.DrawArc(sw.Center, sw.Radius, 0, MathF.PI * 2f, 48, new Color(1f, 0.85f, 0.35f, a * 0.85f),  2f);
			c.DrawArc(sw.Center, sw.Radius * 0.25f, 0, MathF.PI * 2f, 24, new Color(1f, 0.95f, 0.8f, a * a * 0.6f), 5f);
		}
		if (_radialOpen) DrawRadial(c);
		else DrawBrushCursor(c);
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

			case BrushArm:
			{
				// Outline of the 3×3 base + 2 single-cell terminals on the middle row
				float s  = Scale;
				float ox = _mouseSim.X * s;
				float oy = _mouseSim.Y * s;
				var pts = new Vector2[]
				{
					new(ox - 1*s, oy - 1*s),
					new(ox + 2*s, oy - 1*s),
					new(ox + 2*s, oy        ),
					new(ox + 3*s, oy        ),
					new(ox + 3*s, oy +   s),
					new(ox + 2*s, oy +   s),
					new(ox + 2*s, oy + 2*s),
					new(ox - 1*s, oy + 2*s),
					new(ox - 1*s, oy +   s),
					new(ox - 2*s, oy +   s),
					new(ox - 2*s, oy        ),
					new(ox - 1*s, oy        ),
					new(ox - 1*s, oy - 1*s),  // close
				};
				c.DrawPolyline(pts, col, lw);
				break;
			}

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
		if (b != BrushTrack && _trackInProgress != null)
		{
			if (_trackInProgress.SamplePoints.Count >= 2) _tracks.Add(_trackInProgress);
			_trackInProgress = null;
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
		_btnArm.Modulate            = b == BrushArm            ? Colors.Yellow : Colors.White;
		_btnTrack.Modulate          = b == BrushTrack          ? Colors.Yellow : Colors.White;
	}

	private void SetActiveTab(int tab)
	{
		_materialsPage.Visible   = (tab == 0);
		_settingsPage.Visible    = (tab == 1);
		_analysisPage.Visible    = (tab == 2);
		_quickSelectPage.Visible = (tab == 3);
		_scriptsPage.Visible     = (tab == 4);
		_btnTabMaterials.Modulate = tab == 0 ? Colors.Yellow : Colors.White;
		_btnTabSettings.Modulate  = tab == 1 ? Colors.Yellow : Colors.White;
		_btnTabAnalysis.Modulate  = tab == 2 ? Colors.Yellow : Colors.White;
		_btnTabQuick.Modulate     = tab == 3 ? Colors.Yellow : Colors.White;
		_btnTabScripts.Modulate   = tab == 4 ? Colors.Yellow : Colors.White;
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
				EraseArmsInRadius(sp.X, sp.Y, _brushSize);
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
				ConsoleLog("  [color=white]waterspread <n>[/color]   max cells water travels horizontally per tick (1–32, default 4)");
				ConsoleLog("  [color=white]laserfalloff <0-1>[/color] laser power multiplier per bounce (0=instant, 1=no decay, default 0.4)");
				ConsoleLog("  [color=white]lasermax <n>[/color]      max mirror bounces per beam (default 12)");
				ConsoleLog("  [color=white]mirrordist <f>[/color]    mirror raw sample chord length in sim units (default 1.0)");
				ConsoleLog("  [color=white]mirrorepsilon <f>[/color] mirror RDP simplification threshold in sim units (default 1.5)");
				ConsoleLog("  [color=white]clawadd <type>[/color]    add cell type to claw whitelist");
				ConsoleLog("  [color=white]clawremove <type>[/color] remove cell type from claw whitelist");
				ConsoleLog("  [color=white]clawlist[/color]          print current claw whitelist");
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
				_tracks.Clear();  _trackInProgress  = null; _trackDragMachine = null;
				_arms.Clear(); _activeArm = null; _armDrag = null;
				_sim.RenderDirty = true;
				ConsoleLog("[color=cyan]Grid cleared.[/color]");
				break;

			case "boil" when parts.Length > 1 && int.TryParse(parts[1], out int bt):
				_sim.CopperBoilThreshold = Math.Clamp(bt, 0, 255);
				ConsoleLog($"[color=cyan]Copper boil threshold → {_sim.CopperBoilThreshold}[/color]");
				break;

			case "waterspread" when parts.Length > 1 && int.TryParse(parts[1], out int ws):
				_sim.WaterSpreadDist = Math.Clamp(ws, 1, 32);
				ConsoleLog($"[color=cyan]Water spread distance → {_sim.WaterSpreadDist} cells/tick[/color]");
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

			case "clawadd" when parts.Length > 1 && Enum.TryParse<Simulation.Cell>(parts[1], true, out var addType):
				_clawWhitelist.Add((byte)addType);
				ConsoleLog($"[color=cyan]Claw can now pick up {addType}[/color]");
				break;

			case "clawremove" when parts.Length > 1 && Enum.TryParse<Simulation.Cell>(parts[1], true, out var remType):
				_clawWhitelist.Remove((byte)remType);
				ConsoleLog($"[color=cyan]Claw can no longer pick up {remType}[/color]");
				break;

			case "clawlist":
				var names = new List<string>();
				foreach (byte b in _clawWhitelist) names.Add(((Simulation.Cell)b).ToString());
				names.Sort();
				ConsoleLog($"[color=cyan]Claw whitelist:[/color] {string.Join(", ", names)}");
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

	// Picks the closest turret or arm to the mouse cursor within MachineSpaceRadius.
	// The result drives both the Space-key dispatch and the "near" visual highlight.
	// Only one machine is returned — if a turret and arm are both in range, the closer wins.
	private void UpdateMachineSpaceTarget()
	{
		_spaceTargetTurret = null;
		_spaceTargetArm    = null;
		if (ImGuiNET.ImGui.GetIO().WantCaptureMouse) return;

		float bestDist = MachineSpaceRadius;
		foreach (var t in _turrets)
		{
			// Turret pivot is at the visual centre of the base block
			var center = new Vector2(t.Origin.X + 0.5f, t.Origin.Y + 1.5f);
			float d = _mouseSimF.DistanceTo(center);
			if (d < bestDist) { bestDist = d; _spaceTargetTurret = t; _spaceTargetArm = null; }
		}
		foreach (var a in _arms)
		{
			var center = new Vector2(a.Origin.X + 0.5f, a.Origin.Y + 0.5f);
			float d = _mouseSimF.DistanceTo(center);
			if (d < bestDist) { bestDist = d; _spaceTargetArm = a; _spaceTargetTurret = null; }
		}
	}

	private void UpdateTurrets()
	{
		float maxDelta = Mathf.DegToRad(ScriptSmoothingSpeed * _scriptTicksThisFrame);
		foreach (var t in _turrets)
		{
			t.Powered = t.Track != null ? t.Track.Powered : t.CheckPowered(_sim);

			// Slide along track
			if (t.Track != null)
			{
				if (t.ScriptRT != null)
					t.TrackT = MoveAngleToward(t.TrackT, t.TargetTrackT, TrackSmoothingSpeed * _scriptTicksThisFrame);
				else
					t.TrackT = t.TargetTrackT; // manual drag: instant
				t.TrackT = Math.Clamp(t.TrackT, 0f, 1f);
				var pos = t.Track.GetPointAtT(t.TrackT);
				t.Origin = new Vector2I((int)pos.X, (int)pos.Y);
				_sim.RenderDirty = true;
			}
			if (t.ScriptRT != null)
			{
				// Scripted: advance runtime; manual aim is disabled while a script is attached
				if (t.Powered)
					TickRuntime(t.ScriptRT, _scriptTicksThisFrame, b => ExecuteTurretBlock(t, b));
				// Smooth toward target every frame, even when unpowered (so the machine
				// settles to its last commanded position rather than stalling mid-swing)
				t.Angle = MoveAngleToward(t.Angle, t.TargetAngle, maxDelta);
				_sim.RenderDirty = true;
			}
			else if (!t.Frozen)
			{
				// Manual aim — disabled when frozen (Space toggle) so the turret holds its last angle
				t.UpdateAngle(_mouseSim);
			}
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

	// ── Robotic arm ────────────────────────────────────────────────────────────

	private void PlaceArm(Vector2I origin)
	{
		// Check the 3×3 base + 2 terminal cells are all in-bounds
		for (int dy = -RoboArm.BaseHalfW; dy <= RoboArm.BaseHalfW; dy++)
		for (int dx = -RoboArm.BaseHalfW; dx <= RoboArm.BaseHalfW; dx++)
			if (!_sim.InBounds(origin.X + dx, origin.Y + dy)) return;

		var arm = RoboArm.Place(_sim, origin);
		_arms.Add(arm);
		_activeArm = arm; // newly placed arm becomes active
	}

	private void EraseArmsInRadius(int cx, int cy, int radius)
	{
		for (int i = _arms.Count - 1; i >= 0; i--)
		{
			var a = _arms[i];
			bool hit = false;
			for (int dy = -radius; dy <= radius && !hit; dy++)
			for (int dx = -radius; dx <= radius && !hit; dx++)
			{
				if (dx * dx + dy * dy > radius * radius) continue;
				if (a.ContainsIndex((cy + dy) * SimW + (cx + dx))) hit = true;
			}
			if (!hit) continue;
			a.Remove(_sim);
			if (_activeArm == a) _activeArm = null;
			_arms.RemoveAt(i);
		}
	}

	// Returns the closest grabbable joint within JointGrabRadius of `simPos`.
	// joint: 0 = elbow, 1 = claw. Returns null if nothing in range.
	private (RoboArm arm, int joint)? FindClosestJoint(Vector2 simPos)
	{
		float bestDistSq = RoboArm.JointGrabRadius * RoboArm.JointGrabRadius;
		(RoboArm arm, int joint)? best = null;
		foreach (var a in _arms)
		{
			float d0 = (a.Elbow - simPos).LengthSquared();
			if (d0 < bestDistSq) { bestDistSq = d0; best = (a, 0); }
			float d1 = (a.Claw - simPos).LengthSquared();
			if (d1 < bestDistSq) { bestDistSq = d1; best = (a, 1); }
		}
		return best;
	}

	// Drive the currently-dragged joint toward the mouse, respecting collision.
	private void UpdateArmDrag(Vector2 mouseSimF)
	{
		if (_armDrag is not { } d) return;
		var arm = d.arm;
		if (!arm.Powered) return; // unpowered = frozen

		if (d.joint == 0)
		{
			// Dragging elbow → set shoulder angle so the upper arm points at the mouse.
			// Both segments must clear blockers (forearm comes along for the ride).
			float target = MathF.Atan2(mouseSimF.Y - arm.Shoulder.Y, mouseSimF.X - arm.Shoulder.X);
			float prev   = arm.ShoulderAngle;
			arm.ShoulderAngle = target;
			if (ArmSegmentBlocked(arm.Shoulder, arm.Elbow, RoboArm.ShoulderSkip) ||
				ArmSegmentBlocked(arm.Elbow,    arm.Claw,  0f))
				arm.ShoulderAngle = prev;
			else
				_sim.RenderDirty = true;
		}
		else
		{
			// Dragging claw → set forearm angle so the forearm points at the mouse.
			float target = MathF.Atan2(mouseSimF.Y - arm.Elbow.Y, mouseSimF.X - arm.Elbow.X);
			float prev   = arm.ElbowAngle;
			arm.ElbowAngle = target;
			if (ArmSegmentBlocked(arm.Elbow, arm.Claw, 0f))
				arm.ElbowAngle = prev;
			else
				_sim.RenderDirty = true;
		}
	}

	private void UpdateArms()
	{
		float maxDelta = Mathf.DegToRad(ScriptSmoothingSpeed * _scriptTicksThisFrame);
		foreach (var a in _arms)
		{
			a.Powered = a.Track != null ? a.Track.Powered : a.CheckPowered(_sim);

			// Slide along track
			if (a.Track != null)
			{
				if (a.ScriptRT != null)
					a.TrackT = MoveAngleToward(a.TrackT, a.TargetTrackT, TrackSmoothingSpeed * _scriptTicksThisFrame);
				else
					a.TrackT = a.TargetTrackT;
				a.TrackT = Math.Clamp(a.TrackT, 0f, 1f);
				var pos = a.Track.GetPointAtT(a.TrackT);
				a.Origin = new Vector2I((int)pos.X, (int)pos.Y);
				_sim.RenderDirty = true;
			}
			if (a.ScriptRT != null)
			{
				if (a.Powered)
					TickRuntime(a.ScriptRT, _scriptTicksThisFrame, b => ExecuteArmBlock(a, b));
				// Smooth toward target joint angles
				a.ShoulderAngle = MoveAngleToward(a.ShoulderAngle, a.TargetShoulderAngle, maxDelta);
				a.ElbowAngle    = MoveAngleToward(a.ElbowAngle,    a.TargetElbowAngle,    maxDelta);
				_sim.RenderDirty = true;
			}
		}
	}

	// Toggle the active arm's claw. Closing grabs whitelisted cells in the
	// pincer area into the arm's Held list. Opening releases them as ballistic
	// cells (downward velocity) — the velocity-cell physics handles the rest.
	// Legacy fallback: toggle the most-recently-dragged arm. Used when Space is
	// pressed with no arm/turret in proximity (e.g. after dragging a joint, before
	// moving the mouse away). Delegates to the shared ToggleArmClaw helper.
	private void ToggleActiveArmClaw()
	{
		if (_activeArm == null) return;
		ToggleArmClaw(_activeArm);
	}

	private void DrawArms(OverlayCanvas c)
	{
		var trackArmBaseCol = new Color(0.45f, 0.45f, 0.50f, 0.85f);
		Span<Vector2I> pincer = stackalloc Vector2I[RoboArm.PincerCellCount(RoboArm.MaxPincerHalfWidth, RoboArm.MaxPincerDepth)];
		foreach (var a in _arms)
		{
			// Draw overlay base for track-mounted arms (no grid cells)
			if (a.Track != null)
			{
				float bx = (a.Origin.X - RoboArm.BaseHalfW) * Scale;
				float by = (a.Origin.Y - RoboArm.BaseHalfW) * Scale;
				int   bw = (RoboArm.BaseHalfW * 2 + 1) * Scale;
				c.DrawRect(new Rect2(bx, by, bw, bw), trackArmBaseCol, filled: true);
			}

			float alpha = a.Powered ? 1.0f : 0.5f;
			var bodyCol  = new Color(0.62f, 0.66f, 0.72f, alpha);
			var jointCol = new Color(0.80f, 0.85f, 0.90f, alpha);
			var activeCol = new Color(1.00f, 0.90f, 0.40f, alpha);

			Vector2 sh = a.Shoulder * Scale;
			Vector2 el = a.Elbow    * Scale;
			Vector2 cl = a.Claw     * Scale;

			c.DrawLine(sh, el, bodyCol, 3.0f);
			c.DrawLine(el, cl, bodyCol, 3.0f);

			// Joints — active arm gets a slight highlight on its joints
			var pivotCol = a == _activeArm ? activeCol : jointCol;
			c.DrawCircle(sh, 3.5f, pivotCol);
			c.DrawCircle(el, 3.0f, pivotCol);

			// "Near" highlight — ring around the shoulder when Space would target this arm
			if (a == _spaceTargetArm)
				c.DrawArc(sh, 7f, 0f, MathF.PI * 2f, 24, new Color(0.4f, 0.85f, 1.0f, 0.65f), 1.5f);

			// Pincer indicator: closed = thin tip line; open = rectangle showing grab zone.
			Vector2 perpScr = a.PincerPerp * Scale;
			Vector2 fwdScr  = new Vector2(MathF.Cos(a.ElbowAngle), MathF.Sin(a.ElbowAngle)) * Scale;
			if (a.ClawClosed)
			{
				c.DrawLine(cl + perpScr * 0.4f, cl - perpScr * 0.4f, pivotCol, 2.5f);
			}
			else
			{
				float hw  = _pincerHalfWidth + 0.5f;
				float dep = _pincerDepth - 0.5f;
				Vector2 tipL  = cl + perpScr * hw;
				Vector2 tipR  = cl - perpScr * hw;
				Vector2 bakL  = cl + perpScr * hw + fwdScr * dep;
				Vector2 bakR  = cl - perpScr * hw + fwdScr * dep;
				c.DrawLine(tipL, tipR, pivotCol, 2.0f);
				c.DrawLine(bakL, bakR, pivotCol, 2.0f);
				c.DrawLine(tipL, bakL, pivotCol, 2.0f);
				c.DrawLine(tipR, bakR, pivotCol, 2.0f);
			}

			// Held cells render at their grabbed positions
			if (a.ClawClosed && a.Held.Count > 0)
			{
				int n     = a.Held.Count;
				int hw    = a.HeldHalfWidth;
				int depth = a.HeldDepth;
				a.GetPincerCells(hw, depth, pincer[..n]);
				for (int i = 0; i < n; i++)
				{
					var (cell, flow) = a.Held[i];
					if (cell == 0) continue;
					CellColor(cell, flow, out byte r, out byte g, out byte b);
					var col = new Color(r / 255f, g / 255f, b / 255f, alpha);
					var p = pincer[i];
					c.DrawRect(new Rect2(p.X * Scale, p.Y * Scale, Scale, Scale), col, true);
				}
			}
		}
	}

	private void DrawTurrets(OverlayCanvas c)
	{
		const float barrelPx = 4 * Scale;
		var barrelCol = new Color(0.12f, 0.12f, 0.12f);

		var trackBaseCol = new Color(0.45f, 0.45f, 0.50f, 0.85f); // stone gray for track-mounted base
		foreach (var t in _turrets)
		{
			// Draw overlay base for track-mounted turrets (no grid cells)
			if (t.Track != null)
			{
				float bx = (t.Origin.X - LaserTurret.BaseHalfW) * Scale;
				float by =  t.Origin.Y                           * Scale;
				c.DrawRect(new Rect2(bx, by, (LaserTurret.BaseHalfW * 2 + 1) * Scale, LaserTurret.BaseH * Scale),
					trackBaseCol, filled: true);
				// Battery dot at center-top
				c.DrawRect(new Rect2(t.Origin.X * Scale, t.Origin.Y * Scale, Scale, Scale),
					new Color(0.50f, 0.52f, 0.58f, 1f), filled: true);
			}

			var pivotScr = new Vector2(t.Origin.X * Scale + Scale * 0.5f,
									   t.Origin.Y * Scale + Scale * 0.5f);
			var dir     = new Vector2(MathF.Cos(t.Angle), MathF.Sin(t.Angle));
			var tipScr  = pivotScr + dir * barrelPx;
			c.DrawLine(pivotScr, tipScr, barrelCol, 3f);

			// "Near" highlight — pulsing ring around the pivot when Space would target this turret
			if (t == _spaceTargetTurret)
				c.DrawArc(pivotScr, Scale * 3.5f, 0f, MathF.PI * 2f, 32, new Color(0.4f, 0.85f, 1.0f, 0.65f), 1.5f);

			// Frozen indicator — small filled square at the pivot so the user can see at a
			// glance which turrets they've intentionally taken off mouse-aim
			if (t.Frozen)
				c.DrawRect(new Rect2(pivotScr.X - 3f, pivotScr.Y - 3f, 6f, 6f), new Color(0.9f, 0.9f, 1.0f, 0.85f), true);

			// Beam only fires when powered AND the script (or default) wants the laser on
			if (!t.Powered || !t.LaserOn) continue;

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

	// ── Wire system ────────────────────────────────────────────────────────────
	// Wires exist on a separate overlay plane — no grid cells, invisible outside
	// wire mode (Z key). They form an undirected graph; power floods from nodes
	// anchored to Battery cells outward through the graph, then writes Electric[]
	// at machine terminals so turrets and arms receive power normally.

	private sealed class WireNode
	{
		public Vector2            Pos;                         // sim-space float position
		public readonly List<int> Connections = new();        // indices into _wireNodes (undirected)
		public int                AnchorIdx   = -1;           // grid cell if snapped to battery/terminal; -1 if free or track terminal
		public bool               Powered;                    // set by PropagateWirePower each frame
		public RailTrack          TrackRef;                   // non-null if this node is anchored to a track endpoint
		public bool               TrackIsStart;               // true = t=0 endpoint, false = t=1 endpoint
	}

	// Returns the snapped sim position for a cursor. Preference order by distance:
	// battery cells → machine terminals → track endpoints → existing wire nodes → cursor unchanged.
	private Vector2 FindWireSnap(Vector2 simPos, out int anchorIdx, out int existingNodeIdx,
		out RailTrack trackRef, out bool trackIsStart)
	{
		anchorIdx      = -1;
		existingNodeIdx = -1;
		trackRef       = null;
		trackIsStart   = false;
		float   best    = _wireSnapRadius;
		Vector2 snapPos = simPos;

		// Battery cells — walk a bounding box around the cursor
		int cx = (int)simPos.X, cy = (int)simPos.Y;
		int r  = (int)MathF.Ceiling(_wireSnapRadius);
		for (int dy = -r; dy <= r; dy++)
		for (int dx = -r; dx <= r; dx++)
		{
			int gx = cx + dx, gy = cy + dy;
			if (!_sim.InBounds(gx, gy)) continue;
			int idx = gy * SimW + gx;
			if (_sim.Grid[idx] != (byte)Simulation.Cell.Battery) continue;
			float dist = simPos.DistanceTo(new Vector2(gx + 0.5f, gy + 0.5f));
			if (dist < best)
			{
				best = dist;
				snapPos        = new Vector2(gx + 0.5f, gy + 0.5f);
				anchorIdx      = idx;
				existingNodeIdx = -1;
			}
		}

		// Turret copper terminals
		foreach (var t in _turrets)
		{
			int row = t.Origin.Y + 1;
			foreach (int tx in new[] { t.Origin.X - LaserTurret.BaseHalfW - 1, t.Origin.X + LaserTurret.BaseHalfW + 1 })
			{
				if (!_sim.InBounds(tx, row)) continue;
				float dist = simPos.DistanceTo(new Vector2(tx + 0.5f, row + 0.5f));
				if (dist < best)
				{
					best = dist;
					snapPos        = new Vector2(tx + 0.5f, row + 0.5f);
					anchorIdx      = row * SimW + tx;
					existingNodeIdx = -1;
				}
			}
		}

		// Arm copper terminals
		foreach (var a in _arms)
		{
			int termY = a.Origin.Y;
			foreach (int tx in new[] { a.Origin.X - RoboArm.BaseHalfW - 1, a.Origin.X + RoboArm.BaseHalfW + 1 })
			{
				if (!_sim.InBounds(tx, termY)) continue;
				float dist = simPos.DistanceTo(new Vector2(tx + 0.5f, termY + 0.5f));
				if (dist < best)
				{
					best = dist;
					snapPos        = new Vector2(tx + 0.5f, termY + 0.5f);
					anchorIdx      = termY * SimW + tx;
					existingNodeIdx = -1;
				}
			}
		}

		// Track endpoints
		foreach (var tr in _tracks)
		{
			foreach (bool isStart in new[] { true, false })
			{
				var ep   = isStart ? tr.StartPoint : tr.EndPoint;
				float dist = simPos.DistanceTo(ep);
				if (dist < best)
				{
					best           = dist;
					snapPos        = ep;
					anchorIdx      = -1;
					existingNodeIdx = -1;
					trackRef       = tr;
					trackIsStart   = isStart;
				}
			}
		}

		// Existing wire nodes (don't snap a pending node to itself)
		for (int i = 0; i < _wireNodes.Count; i++)
		{
			if (i == _wirePendingIdx) continue;
			float dist = simPos.DistanceTo(_wireNodes[i].Pos);
			if (dist < best)
			{
				best           = dist;
				snapPos        = _wireNodes[i].Pos;
				anchorIdx      = _wireNodes[i].AnchorIdx;
				existingNodeIdx = i;
				trackRef       = _wireNodes[i].TrackRef;
				trackIsStart   = _wireNodes[i].TrackIsStart;
			}
		}

		return snapPos;
	}

	// LMB click in wire mode: place or reuse a node and connect it to the pending node.
	private void PlaceWireClick(Vector2 simPos)
	{
		var snapPos = FindWireSnap(simPos, out int anchorIdx, out int existingIdx, out RailTrack snapTrackRef, out bool snapTrackIsStart);

		int targetIdx;
		if (existingIdx >= 0)
		{
			// Clicked near an existing node — connect to it rather than spawning a new one
			targetIdx = existingIdx;
		}
		else
		{
			_wireNodes.Add(new WireNode { Pos = snapPos, AnchorIdx = anchorIdx, TrackRef = snapTrackRef, TrackIsStart = snapTrackIsStart });
			targetIdx = _wireNodes.Count - 1;
		}

		if (_wirePendingIdx >= 0 && _wirePendingIdx != targetIdx
			&& !_wireNodes[_wirePendingIdx].Connections.Contains(targetIdx))
		{
			_wireNodes[_wirePendingIdx].Connections.Add(targetIdx);
			_wireNodes[targetIdx].Connections.Add(_wirePendingIdx);
		}

		_wirePendingIdx = targetIdx;
		_sim.RenderDirty = true;
	}

	// RMB click in wire mode: cancel pending first; if none, delete the nearest node
	// and all its edges.
	private void TryDeleteWireNode(Vector2 simPos)
	{
		if (_wirePendingIdx >= 0) { _wirePendingIdx = -1; return; }

		float best   = _wireSnapRadius;
		int   hitIdx = -1;
		for (int i = 0; i < _wireNodes.Count; i++)
		{
			float d = simPos.DistanceTo(_wireNodes[i].Pos);
			if (d < best) { best = d; hitIdx = i; }
		}
		if (hitIdx < 0) return;

		// Remove edges from every neighbour that pointed to this node
		foreach (int ci in _wireNodes[hitIdx].Connections)
			_wireNodes[ci].Connections.Remove(hitIdx);

		_wireNodes.RemoveAt(hitIdx);

		// Fix up all indices that shifted after the RemoveAt
		for (int i = 0; i < _wireNodes.Count; i++)
			for (int j = 0; j < _wireNodes[i].Connections.Count; j++)
				if (_wireNodes[i].Connections[j] > hitIdx)
					_wireNodes[i].Connections[j]--;

		if      (_wirePendingIdx == hitIdx) _wirePendingIdx = -1;
		else if (_wirePendingIdx  > hitIdx) _wirePendingIdx--;

		_sim.RenderDirty = true;
	}

	// Called every frame after sim ticks. BFS from battery-anchored nodes,
	// then writes Electric[] at powered terminal nodes so machines see power.
	private void PropagateWirePower()
	{
		if (_wireNodes.Count == 0) return;

		foreach (var n in _wireNodes) n.Powered = false;
		foreach (var tr in _tracks)  tr.Powered = false;

		var queue = new Queue<int>();
		for (int i = 0; i < _wireNodes.Count; i++)
		{
			var n = _wireNodes[i];
			if (n.AnchorIdx >= 0 && _sim.Grid[n.AnchorIdx] == (byte)Simulation.Cell.Battery)
			{ n.Powered = true; queue.Enqueue(i); }
		}
		while (queue.Count > 0)
		{
			int ci = queue.Dequeue();
			foreach (int ni in _wireNodes[ci].Connections)
			{
				if (!_wireNodes[ni].Powered)
				{ _wireNodes[ni].Powered = true; queue.Enqueue(ni); }
			}
		}

		// Deliver power to machine terminals via Electric[] channel, and to tracks via TrackRef
		foreach (var n in _wireNodes)
		{
			if (!n.Powered) continue;
			if (n.AnchorIdx >= 0 && _sim.Grid[n.AnchorIdx] != (byte)Simulation.Cell.Battery)
				_sim.Electric[n.AnchorIdx] = 1;
			if (n.TrackRef != null)
				n.TrackRef.Powered = true;
		}
	}

	// Draws the wire graph overlay. Only called when _wireModeActive = true.
	private void DrawWires(OverlayCanvas c)
	{
		var poweredEdge   = new Color(1.00f, 0.85f, 0.15f, 0.90f); // warm yellow
		var unpoweredEdge = new Color(0.40f, 0.40f, 0.45f, 0.70f); // dim grey
		var poweredNode   = new Color(1.00f, 0.95f, 0.40f, 1.00f);
		var unpoweredNode = new Color(0.50f, 0.50f, 0.55f, 1.00f);
		var pendingCol    = new Color(0.40f, 0.85f, 1.00f, 1.00f); // cyan = awaiting second click
		var anchorRingCol = new Color(1.00f, 1.00f, 1.00f, 0.55f); // white halo for anchored nodes
		var ghostCol      = new Color(0.40f, 0.85f, 1.00f, 0.45f);

		// Edges — draw each undirected edge once (j > i guard)
		for (int i = 0; i < _wireNodes.Count; i++)
		{
			var  a  = _wireNodes[i];
			var  pa = a.Pos * Scale;
			foreach (int j in a.Connections)
			{
				if (j <= i) continue;
				var b       = _wireNodes[j];
				bool powered = a.Powered && b.Powered;
				c.DrawLine(pa, b.Pos * Scale, powered ? poweredEdge : unpoweredEdge, 2.0f);
			}
		}

		// Nodes
		for (int i = 0; i < _wireNodes.Count; i++)
		{
			var     n   = _wireNodes[i];
			Vector2 p   = n.Pos * Scale;
			bool    isPending = i == _wirePendingIdx;
			var     col = isPending ? pendingCol : (n.Powered ? poweredNode : unpoweredNode);
			float   rad = n.AnchorIdx >= 0 ? 4.0f : 2.8f; // anchored nodes are slightly larger
			// White halo distinguishes anchored (battery/terminal) nodes from free junctions
			if (n.AnchorIdx >= 0)
				c.DrawCircle(p, rad + 2.0f, anchorRingCol);
			c.DrawCircle(p, rad, col);
		}

		// Ghost wire + snap indicator when a pending node is waiting for second click
		if (_wirePendingIdx >= 0 && _wirePendingIdx < _wireNodes.Count)
		{
			var snapPos = FindWireSnap(_mouseSimF, out _, out _, out _, out _);
			Vector2 from = _wireNodes[_wirePendingIdx].Pos * Scale;
			Vector2 to   = snapPos * Scale;
			c.DrawLine(from, to, ghostCol, 1.5f);
			c.DrawCircle(to, 2.8f, ghostCol);
		}
		else
		{
			// No pending node: show a subtle snap indicator at cursor position
			var snapPos = FindWireSnap(_mouseSimF, out int snapAnchor, out _, out RailTrack snapTr, out _);
			bool snapped = snapAnchor >= 0 || snapTr != null || snapPos.DistanceTo(_mouseSimF) > 0.1f;
			if (snapped)
			{
				var snapIndicatorCol = new Color(0.40f, 0.85f, 1.00f, 0.60f);
				c.DrawCircle(snapPos * Scale, 3.5f, snapIndicatorCol);
			}
		}

		// Wire mode indicator label — drawn via debug output, not overlay
		// (visible because the overlay has no text-drawing API)
	}

	// ── LaserTurret ────────────────────────────────────────────────────────────

	private sealed class LaserTurret
	{
		public const int BaseHalfW = 2;
		public const int BaseH     = 3;

		public Vector2I           Origin;
		public float              Angle;
		public float              TargetAngle;                    // scripted target — Angle smoothly approaches this each tick
		public bool               Powered;
		public bool               LaserOn = true;                 // scripts can toggle this; default fires whenever powered
		public bool               Frozen;                         // Space-toggled: stops mouse-aim, keeps firing. Scripted turrets ignore Frozen.
		public ScriptRuntime      ScriptRT;                       // null = manual (mouse-aim); non-null = scripted
		public readonly List<int> OccupiedIndices = new();
		public RailTrack          Track;                          // null = free-standing; non-null = riding a track
		public float              TrackT;                         // current position on track (0=start, 1=end)
		public float              TargetTrackT;                   // scripted/drag target position

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

	// ── RoboArm ────────────────────────────────────────────────────────────────
	// Two-segment robotic arm. Base is a 3×3 pinned Stone block with two copper
	// terminals one cell outside the middle row (same wiring pattern as turret).
	// Segments are overlay-only — no cells stamped while the arm swings.
	// Joint angles are absolute world-space radians so future scripting just sets
	// floats. The claw is a 3-cell pincer perpendicular to the forearm.

	private sealed class RoboArm
	{
		public const int   BaseHalfW        = 1;     // 3×3 base
		public const float UpperArmLen      = 12f;
		public const float ForearmLen       = 12f;
		public const float JointGrabRadius  = 3.0f;  // sim units — how close to click to grab a joint
		public const float ShoulderSkip     = 2.0f;  // skip the first 2 units of upper arm (inside our own base)

		public Vector2I Origin;
		public float    ShoulderAngle      = -MathF.PI / 2f;
		public float    ElbowAngle         = -MathF.PI / 2f;
		public float    TargetShoulderAngle = -MathF.PI / 2f;       // scripted target — actual angles smoothly approach
		public float    TargetElbowAngle    = -MathF.PI / 2f;
		public bool     ClawClosed;
		public bool     Powered;
		public ScriptRuntime ScriptRT;                              // null = manual (joint drag); non-null = scripted
		public readonly List<(byte cell, byte flow)> Held = new();
		public readonly List<int> OccupiedIndices = new();
		public RailTrack Track;
		public float     TrackT;
		public float     TargetTrackT;

		public Vector2 Shoulder => new(Origin.X + 0.5f, Origin.Y + 0.5f);
		public Vector2 Elbow    => Shoulder + new Vector2(MathF.Cos(ShoulderAngle), MathF.Sin(ShoulderAngle)) * UpperArmLen;
		public Vector2 Claw     => Elbow    + new Vector2(MathF.Cos(ElbowAngle),    MathF.Sin(ElbowAngle))    * ForearmLen;

		// Perpendicular to forearm direction, used to compute pincer cell positions
		public Vector2 PincerPerp => new(-MathF.Sin(ElbowAngle), MathF.Cos(ElbowAngle));

		// The cells the pincer occupies: 2*halfWidth+1 cells along the line perpendicular
		// to the forearm, centered at the claw tip. halfWidth=0 → 1 cell at the tip only.
		// halfWidth=1 → 3 cells (one on each side of the tip). Etc.
		public const int MaxPincerHalfWidth = 6;
		public const int MaxPincerDepth    = 8;
		public static int PincerCellCount(int halfWidth, int depth) => (2 * halfWidth + 1) * depth;

		// hw × depth stored at grab time so release can reconstruct positions correctly
		public int HeldHalfWidth;
		public int HeldDepth = 1;

		// Fills outCells row-major: d=0 is at claw tip, d=1..depth-1 extend forward past the tip.
		public void GetPincerCells(int halfWidth, int depth, Span<Vector2I> outCells)
		{
			var c    = Claw;
			var perp = PincerPerp;
			float fwdX = MathF.Cos(ElbowAngle);
			float fwdY = MathF.Sin(ElbowAngle);
			int width  = 2 * halfWidth + 1;
			for (int d = 0; d < depth; d++)
			{
				float rx = c.X + fwdX * d;
				float ry = c.Y + fwdY * d;
				for (int w = -halfWidth; w <= halfWidth; w++)
				{
					outCells[d * width + (w + halfWidth)] = new Vector2I(
						(int)MathF.Floor(rx + perp.X * w),
						(int)MathF.Floor(ry + perp.Y * w));
				}
			}
		}

		public static RoboArm Place(Simulation sim, Vector2I origin)
		{
			var a = new RoboArm { Origin = origin };
			for (int dy = -BaseHalfW; dy <= BaseHalfW; dy++)
			for (int dx = -BaseHalfW; dx <= BaseHalfW; dx++)
			{
				int gx = origin.X + dx, gy = origin.Y + dy;
				if (!sim.InBounds(gx, gy)) continue;
				int idx = gy * Simulation.SimW + gx;
				sim.Grid[idx]   = (byte)Simulation.Cell.Stone;
				sim.Flow[idx]   = 0;
				sim.Pinned[idx] = 1;
				a.OccupiedIndices.Add(idx);
			}
			// Copper terminals at middle row, 1 cell outside the base
			int termY = origin.Y;
			foreach (int termX in new[] { origin.X - BaseHalfW - 1, origin.X + BaseHalfW + 1 })
			{
				if (!sim.InBounds(termX, termY)) continue;
				int idx = termY * Simulation.SimW + termX;
				sim.Grid[idx]   = (byte)Simulation.Cell.Copper;
				sim.Flow[idx]   = 0;
				sim.Pinned[idx] = 1;
				a.OccupiedIndices.Add(idx);
			}
			sim.RenderDirty = true;
			return a;
		}

		public bool CheckPowered(Simulation sim)
		{
			int termY  = Origin.Y;
			int leftX  = Origin.X - BaseHalfW - 1;
			int rightX = Origin.X + BaseHalfW + 1;
			return (sim.InBounds(leftX,  termY) && sim.Electric[termY * Simulation.SimW + leftX]  != 0)
				|| (sim.InBounds(rightX, termY) && sim.Electric[termY * Simulation.SimW + rightX] != 0);
		}

		public void Remove(Simulation sim)
		{
			foreach (int idx in OccupiedIndices)
			{
				sim.Grid[idx]   = (byte)Simulation.Cell.Air;
				sim.Pinned[idx] = 0;
				sim.Flow[idx]   = 0;
			}
			OccupiedIndices.Clear();
			sim.RenderDirty = true;
		}

		public bool ContainsIndex(int idx) => OccupiedIndices.Contains(idx);
	}

	// Cells that block arm body movement. Pinned cells of any type also block.
	// Electrified copper and batteries pass through so wired circuits don't cage the arm.
	private static bool IsArmBlocker(Simulation sim, int gx, int gy)
	{
		int idx = gy * Simulation.SimW + gx;
		if (sim.Pinned[idx] != 0) return true;
		byte c = sim.Grid[idx];
		// Powered copper is intentionally passable — the arm is designed to operate inside circuits.
		bool electrifiedCopper = c == (byte)Simulation.Cell.Copper && sim.Electric[idx] != 0;
		return !electrifiedCopper && (
			   c == (byte)Simulation.Cell.Stone
			|| c == (byte)Simulation.Cell.Copper
			|| c == (byte)Simulation.Cell.Wood
			|| c == (byte)Simulation.Cell.Bark
			|| c == (byte)Simulation.Cell.Mirror
			|| c == (byte)Simulation.Cell.Ice
			|| c == (byte)Simulation.Cell.Leaves);
	}

	// Tests if the line segment from `a` to `b` (sim coords) passes through any
	// blocker cell. `skipStart` lets us ignore the first portion (the arm's own base).
	private bool ArmSegmentBlocked(Vector2 a, Vector2 b, float skipStart)
	{
		var dir = b - a;
		float len = dir.Length();
		if (len < 1e-6f) return false;
		float startT = MathF.Min(skipStart / len, 1f);
		int steps = Math.Max(1, (int)MathF.Ceiling((len - skipStart) * 2f));
		for (int i = 0; i <= steps; i++)
		{
			float t = startT + (1f - startT) * (i / (float)steps);
			var p = a + dir * t;
			int gx = (int)MathF.Floor(p.X), gy = (int)MathF.Floor(p.Y);
			if (gx < 0 || gx >= SimW || gy < 0 || gy >= SimH) return true;
			if (IsArmBlocker(_sim, gx, gy)) return true;
		}
		return false;
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

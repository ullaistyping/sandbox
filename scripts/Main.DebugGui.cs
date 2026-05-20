using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Godot;
using ImGuiNET;
using Vec2 = System.Numerics.Vector2;
using Vec4 = System.Numerics.Vector4;

public partial class Main
{
	// ── State ─────────────────────────────────────────────────────────────────

	private bool _showDebugGui;
	private readonly List<(string text, Vec4 color)> _debugLog = new();
	private readonly byte[] _consoleInputBytes = new byte[256];
	private bool _scrollConsoleToBottom;

	// Persisted window geometry — applied once on first open
	private Vec2 _debugWinPos  = new Vec2(20, 20);
	private Vec2 _debugWinSize = new Vec2(540, 620);
	private bool _debugWinGeomLoaded;

	// ── Entry point (called from Main._Process) ────────────────────────────

	private void DrawDebugGui()
	{
		if (!_showDebugGui) return;

		if (!_debugWinGeomLoaded)
		{
			ImGui.SetNextWindowPos(_debugWinPos,  ImGuiCond.Always);
			ImGui.SetNextWindowSize(_debugWinSize, ImGuiCond.Always);
			_debugWinGeomLoaded = true;
		}

		ImGui.Begin("Sandbox Debug");

		if (ImGui.BeginTabBar("##tabs"))
		{
			DrawSimulationTab();
			DrawHeatTab();
			DrawFirePlantsTab();
			DrawLaserTab();
			DrawMirrorsTab();
			DrawWireTab();
			DrawArmTab();
			DrawScriptsTab();
			DrawGlorpTab();
			DrawConsoleTab();
			ImGui.EndTabBar();
		}

		ImGui.Separator();
		if (ImGui.Button("Save Settings")) SaveConfig();
		ImGui.SetItemTooltip("Save all values + window position to user://tuning.cfg");

		ImGui.End();
	}

	// ── Tabs ──────────────────────────────────────────────────────────────────

	private void DrawSimulationTab()
	{
		if (!ImGui.BeginTabItem("Simulation")) return;

		int tps = _ticksPerSecond;
		if (ImGui.SliderInt("TPS", ref tps, 1, 120)) _ticksPerSecond = tps;
		ImGui.SetItemTooltip("Simulation ticks per second — lower = slow motion, higher = fast-forward");

		int brush = _brushSize;
		if (ImGui.SliderInt("Brush Size", ref brush, 1, 20)) _brushSize = brush;
		ImGui.SetItemTooltip("Radius of the paint brush in sim cells");

		ImGui.SeparatorText("Ballistic Physics");

		float g = _sim.Gravity;
		if (ImGui.SliderFloat("Gravity", ref g, 0f, 2f, "%.3f")) _sim.Gravity = g;
		ImGui.SetItemTooltip("Downward acceleration added to VelY each tick for flying cells");

		float fr = _sim.Friction;
		if (ImGui.SliderFloat("Friction", ref fr, 0.90f, 1.00f, "%.4f")) _sim.Friction = fr;
		ImGui.SetItemTooltip("Velocity multiplier per tick (1=no air drag, 0.99=light drag)");

		float dc = _sim.DampCol;
		if (ImGui.SliderFloat("Collision Damping", ref dc, 0f, 1f, "%.2f")) _sim.DampCol = dc;
		ImGui.SetItemTooltip("Fraction of speed retained after hitting a solid — 0=dead stop, 1=perfect bounce");

		float st = _sim.StopThr;
		if (ImGui.SliderFloat("Stop Threshold", ref st, 0f, 1f, "%.2f")) _sim.StopThr = st;
		ImGui.SetItemTooltip("Velocity below which a moving cell is frozen in place");

		ImGui.EndTabItem();
	}

	private void DrawHeatTab()
	{
		if (!ImGui.BeginTabItem("Heat")) return;

		ImGui.SeparatorText("Copper Reactions");

		int boil = _sim.CopperBoilThreshold;
		if (ImGui.SliderInt("Boil Threshold", ref boil, 0, 255)) _sim.CopperBoilThreshold = boil;
		ImGui.SetItemTooltip("Copper heat level at which adjacent water turns to steam (128=room temp, 255=lava)");

		int gas = _sim.CopperGasThreshold;
		if (ImGui.SliderInt("Gas Ignite Threshold", ref gas, 0, 255)) _sim.CopperGasThreshold = gas;
		ImGui.SetItemTooltip("Copper heat level at which adjacent gas pockets explode");

		int ice = _sim.IceCopperThreshold;
		if (ImGui.SliderInt("Ice Threshold", ref ice, 0, 127)) _sim.IceCopperThreshold = ice;
		ImGui.SetItemTooltip("Copper heat level below which adjacent steam freezes to ice");

		ImGui.SeparatorText("Propagation");

		int hr = _sim.HeatRange;
		if (ImGui.SliderInt("Heat Range", ref hr, 1, 64)) _sim.HeatRange = hr;
		ImGui.SetItemTooltip("Maximum copper hops heat travels from a lava/fire source");

		int hfd = _sim.HeatFireDist;
		if (ImGui.SliderInt("Fire Heat Dist", ref hfd, 0, 32)) _sim.HeatFireDist = hfd;
		ImGui.SetItemTooltip("Fire seeds copper as if it were this many hops from lava (fire < lava as heat source)");

		int hs = _sim.HeatSmoothing;
		if (ImGui.SliderInt("Heat Smoothing", ref hs, 1, 10)) _sim.HeatSmoothing = hs;
		ImGui.SetItemTooltip("Divisor for heat ramp speed — 1=instant snap, higher=slow thermal inertia");

		ImGui.EndTabItem();
	}

	private void DrawFirePlantsTab()
	{
		if (!ImGui.BeginTabItem("Fire / Plants")) return;

		ImGui.SeparatorText("Fire");

		float fic = _sim.FireIgniteChance;
		if (ImGui.SliderFloat("Ignite Chance", ref fic, 0f, 1f, "%.3f")) _sim.FireIgniteChance = fic;
		ImGui.SetItemTooltip("Probability per tick that fire spreads to a neighbouring flammable cell");

		int fbt = _sim.FireBaseTicks;
		if (ImGui.SliderInt("Base Lifetime", ref fbt, 1, 200)) _sim.FireBaseTicks = fbt;
		ImGui.SetItemTooltip("Base number of ticks a fire cell burns before dying (bark/wood burn 3x longer)");

		ImGui.SeparatorText("Plants");

		float gsr = _sim.GrassSeedRate;
		if (ImGui.SliderFloat("Grass Seed Rate", ref gsr, 0f, 0.02f, "%.4f")) _sim.GrassSeedRate = gsr;
		ImGui.SetItemTooltip("Chance per tick that a grass seed on dirt sprouts into grass");

		float tsr = _sim.TreeSeedRate;
		if (ImGui.SliderFloat("Tree Seed Rate", ref tsr, 0f, 0.005f, "%.5f")) _sim.TreeSeedRate = tsr;
		ImGui.SetItemTooltip("Chance per tick that a tree seed on soil grows a full tree");

		ImGui.EndTabItem();
	}

	private void DrawLaserTab()
	{
		if (!ImGui.BeginTabItem("Laser")) return;

		float lf = _laserFalloff;
		if (ImGui.SliderFloat("Falloff", ref lf, 0f, 1f, "%.2f")) _laserFalloff = lf;
		ImGui.SetItemTooltip("Power multiplier applied after each mirror bounce (0=beam dies instantly, 1=no decay)");

		int lm = _laserMaxBounces;
		if (ImGui.SliderInt("Max Bounces", ref lm, 0, 64)) _laserMaxBounces = lm;
		ImGui.SetItemTooltip("Hard cap on mirror reflections per beam segment");

		ImGui.EndTabItem();
	}

	private void DrawMirrorsTab()
	{
		if (!ImGui.BeginTabItem("Mirrors")) return;

		float ep = BezierMirror.RdpEpsilon;
		if (ImGui.SliderFloat("RDP Epsilon", ref ep, 0f, 20f, "%.2f")) BezierMirror.RdpEpsilon = ep;
		ImGui.SetItemTooltip("Max perpendicular deviation before a raw sample point is kept (higher = fewer, smoother control points)");

		float md = BezierMirror.RawMinDist;
		if (ImGui.SliderFloat("Sample Dist", ref md, 0.2f, 10f, "%.2f")) BezierMirror.RawMinDist = md;
		ImGui.SetItemTooltip("Minimum chord distance between raw mouse samples (sim units) — sets base resolution of the drawn mirror");

		ImGui.EndTabItem();
	}

	private void DrawWireTab()
	{
		if (!ImGui.BeginTabItem("Wire")) return;

		ImGui.SeparatorText("Controls");
		ImGui.TextDisabled("Z — toggle wire mode   LMB — place node   RMB — cancel / delete node   Esc — cancel pending");

		ImGui.SeparatorText("Settings");
		float sr = _wireSnapRadius;
		if (ImGui.SliderFloat("Snap Radius", ref sr, 2f, 20f, "%.1f"))
			_wireSnapRadius = sr;
		ImGui.SetItemTooltip(
			"How close (sim cells) the cursor must be to a battery, machine terminal,\n" +
			"or existing wire node to snap to it instead of placing a free junction.");

		ImGui.SeparatorText("State");
		ImGui.Text($"Mode active: {(_wireModeActive ? "YES (Z to exit)" : "no (Z to enter)")}");
		ImGui.Text($"Nodes: {_wireNodes.Count}");
		int edgeCount = 0;
		foreach (var n in _wireNodes) edgeCount += n.Connections.Count;
		ImGui.Text($"Edges: {edgeCount / 2}");
		int powered = 0;
		foreach (var n in _wireNodes) if (n.Powered) powered++;
		ImGui.Text($"Powered nodes: {powered} / {_wireNodes.Count}");
		if (_wirePendingIdx >= 0)
			ImGui.TextColored(new System.Numerics.Vector4(0.4f, 0.85f, 1f, 1f),
				$"Pending node #{_wirePendingIdx} — click to connect");
		else
			ImGui.TextDisabled("No pending node");

		if (ImGui.Button("Clear All Wires"))
		{
			_wireNodes.Clear();
			_wirePendingIdx = -1;
		}
		ImGui.SetItemTooltip("Remove every wire node and edge from the world.");

		ImGui.EndTabItem();
	}

	private void DrawScriptsTab()
	{
		if (!ImGui.BeginTabItem("Scripts")) return;

		ImGui.SeparatorText("Machine Motion");

		float speed = ScriptSmoothingSpeed;
		if (ImGui.SliderFloat("Smoothing Speed", ref speed, 0.1f, 30f, "%.2f deg/tick"))
			ScriptSmoothingSpeed = speed;
		ImGui.SetItemTooltip(
			"How fast scripted machines rotate toward their target angle, in degrees per simulation tick.\n" +
			"Lower = slow, deliberate motion. Higher = snappier, near-instant.\n" +
			"Default 3 deg/tick = 90 deg/sec at 30 TPS.\n" +
			"Set Angle / Add Angle blocks set the target; this slider controls how fast the machine actually moves.");

		ImGui.Text($"= {speed * _ticksPerSecond:F1} deg/sec at current TPS");

		ImGui.SeparatorText("State");
		int activeTurrets = 0; foreach (var t in _turrets) if (t.ScriptRT != null) activeTurrets++;
		int activeArms    = 0; foreach (var a in _arms)    if (a.ScriptRT != null) activeArms++;
		ImGui.Text($"Saved scripts:     {_scripts.Count}");
		ImGui.Text($"Scripted turrets:  {activeTurrets} / {_turrets.Count}");
		ImGui.Text($"Scripted arms:     {activeArms} / {_arms.Count}");
		ImGui.Text($"Editor open:       {(_scriptEditorOpen ? "YES" : "no")}");

		ImGui.EndTabItem();
	}

	private void DrawArmTab()
	{
		if (!ImGui.BeginTabItem("Arm")) return;

		ImGui.SeparatorText("Claw");

		int hw = _pincerHalfWidth;
		if (ImGui.SliderInt("Pincer Half-Width", ref hw, 0, RoboArm.MaxPincerHalfWidth))
			_pincerHalfWidth = hw;
		ImGui.SetItemTooltip(
			$"Cells grabbed perpendicular to the forearm: {2 * _pincerHalfWidth + 1}.\n" +
			"0 = single cell. 1 = 3-wide. 2 = 5-wide. Etc.\n" +
			"Applies to the next grab — held cells keep their size until released.");

		int dep = _pincerDepth;
		if (ImGui.SliderInt("Pincer Depth", ref dep, 1, RoboArm.MaxPincerDepth))
			_pincerDepth = dep;
		ImGui.SetItemTooltip(
			$"Rows grabbed along the forearm: {_pincerDepth}.\n" +
			"1 = single row at claw tip. Higher values grab a rectangular block\n" +
			"extending back toward the elbow.");

		ImGui.Text($"Total cells per grab: {(2 * _pincerHalfWidth + 1) * _pincerDepth}  ({2 * _pincerHalfWidth + 1} wide × {_pincerDepth} deep)");

		ImGui.SeparatorText("State");
		ImGui.Text($"Arms in world: {_arms.Count}");
		if (_activeArm != null)
		{
			ImGui.Text($"Active arm — powered: {_activeArm.Powered}, claw: {(_activeArm.ClawClosed ? "closed" : "open")}, holding: {_activeArm.Held.Count} cells");
		}
		else
		{
			ImGui.TextDisabled("No active arm (drag a joint to activate one).");
		}

		ImGui.EndTabItem();
	}

	private void DrawGlorpTab()
	{
		if (!ImGui.BeginTabItem("Glorp")) return;

		ImGui.SeparatorText("Physics");

		float gg = Glorp.Gravity;
		if (ImGui.SliderFloat("Gravity##g", ref gg, 0f, 300f, "%.1f")) Glorp.Gravity = gg;
		ImGui.SetItemTooltip("Downward acceleration (sim units / s²)");

		float mfs = Glorp.MaxFallSpeed;
		if (ImGui.SliderFloat("Max Fall Speed", ref mfs, 0f, 200f, "%.1f")) Glorp.MaxFallSpeed = mfs;
		ImGui.SetItemTooltip("Terminal vertical velocity (sim units / s)");

		float mhs = Glorp.MaxHorizSpeed;
		if (ImGui.SliderFloat("Max Horiz Speed", ref mhs, 0f, 100f, "%.1f")) Glorp.MaxHorizSpeed = mhs;
		ImGui.SetItemTooltip("Maximum horizontal speed the AI can drive (sim units / s)");

		float ha = Glorp.HorizAccel;
		if (ImGui.SliderFloat("Horiz Accel", ref ha, 0f, 300f, "%.1f")) Glorp.HorizAccel = ha;
		ImGui.SetItemTooltip("How quickly horizontal velocity reaches the target (sim units / s²)");

		float gf = Glorp.GroundFriction;
		if (ImGui.SliderFloat("Ground Friction", ref gf, 0f, 20f, "%.2f")) Glorp.GroundFriction = gf;
		ImGui.SetItemTooltip("Exponential decay constant for horizontal speed on ground (higher = more friction)");

		float br = Glorp.BounceRestitution;
		if (ImGui.SliderFloat("Bounce", ref br, 0f, 1f, "%.2f")) Glorp.BounceRestitution = br;
		ImGui.SetItemTooltip("Fraction of vertical speed retained on landing (0=no bounce, 1=full bounce)");

		ImGui.SeparatorText("Needs");

		float hr2 = Glorp.HungerRate;
		if (ImGui.SliderFloat("Hunger Rate", ref hr2, 0f, 20f, "%.1f")) Glorp.HungerRate = hr2;
		ImGui.SetItemTooltip("Hunger points gained per second");

		float tr2 = Glorp.ThirstRate;
		if (ImGui.SliderFloat("Thirst Rate", ref tr2, 0f, 20f, "%.1f")) Glorp.ThirstRate = tr2;
		ImGui.SetItemTooltip("Thirst points gained per second");

		float lr = Glorp.LonelyRate;
		if (ImGui.SliderFloat("Lonely Rate", ref lr, 0f, 20f, "%.1f")) Glorp.LonelyRate = lr;
		ImGui.SetItemTooltip("Social points lost per second when no other Glorp is nearby");

		float hf = Glorp.HungerFill;
		if (ImGui.SliderFloat("Hunger Fill", ref hf, 0f, 100f, "%.1f")) Glorp.HungerFill = hf;
		ImGui.SetItemTooltip("Hunger reduction when eating a Food cell");

		float tf = Glorp.ThirstFill;
		if (ImGui.SliderFloat("Thirst Fill", ref tf, 0f, 100f, "%.1f")) Glorp.ThirstFill = tf;
		ImGui.SetItemTooltip("Thirst reduction when drinking Water");

		float sg = Glorp.SocialGain;
		if (ImGui.SliderFloat("Social Gain", ref sg, 0f, 100f, "%.1f")) Glorp.SocialGain = sg;
		ImGui.SetItemTooltip("Social points gained per second when near another Glorp");

		ImGui.SeparatorText("Senses");

		float sr = Glorp.SenseRange;
		if (ImGui.SliderFloat("Sense Range", ref sr, 0f, 80f, "%.1f")) Glorp.SenseRange = sr;
		ImGui.SetItemTooltip("How far a Glorp scans for food, water, and other Glorps (sim cells)");

		float er = Glorp.EatRange;
		if (ImGui.SliderFloat("Eat Range", ref er, 0f, 20f, "%.1f")) Glorp.EatRange = er;
		ImGui.SetItemTooltip("Distance at which a Glorp actually eats or drinks from a cell");

		float tk = Glorp.TalkRange;
		if (ImGui.SliderFloat("Talk Range", ref tk, 0f, 80f, "%.1f")) Glorp.TalkRange = tk;
		ImGui.SetItemTooltip("Distance at which two Glorps socialise and show speech bubbles");

		ImGui.EndTabItem();
	}

	private void DrawConsoleTab()
	{
		if (!ImGui.BeginTabItem("Console")) return;

		float logH = ImGui.GetContentRegionAvail().Y - 32f;
		if (ImGui.BeginChild("##log", new Vec2(0, logH)))
		{
			foreach (var (text, color) in _debugLog)
				ImGui.TextColored(color, text);

			if (_scrollConsoleToBottom)
			{
				ImGui.SetScrollHereY(1.0f);
				_scrollConsoleToBottom = false;
			}
		}
		ImGui.EndChild();

		ImGui.SetNextItemWidth(-1);
		if (ImGui.InputText("##consoleinput", _consoleInputBytes, (uint)_consoleInputBytes.Length,
			ImGuiInputTextFlags.EnterReturnsTrue))
		{
			string cmd = System.Text.Encoding.UTF8.GetString(_consoleInputBytes).TrimEnd('\0');
			Array.Clear(_consoleInputBytes, 0, _consoleInputBytes.Length);
			if (!string.IsNullOrWhiteSpace(cmd))
				ExecuteCommand(cmd);
			ImGui.SetKeyboardFocusHere(-1);
		}

		ImGui.EndTabItem();
	}

	// ── Console log ───────────────────────────────────────────────────────────

	private static readonly Vec4 LogWhite  = new Vec4(1.00f, 1.00f, 1.00f, 1.00f);
	private static readonly Vec4 LogCyan   = new Vec4(0.20f, 0.90f, 0.90f, 1.00f);
	private static readonly Vec4 LogYellow = new Vec4(1.00f, 1.00f, 0.20f, 1.00f);
	private static readonly Vec4 LogRed    = new Vec4(1.00f, 0.35f, 0.35f, 1.00f);
	private static readonly Vec4 LogGray   = new Vec4(0.60f, 0.60f, 0.60f, 1.00f);

	private void ConsoleLog(string bbcode)
	{
		Vec4 color = LogWhite;
		if (bbcode.Contains("[color=cyan]"))   color = LogCyan;
		else if (bbcode.Contains("[color=yellow]")) color = LogYellow;
		else if (bbcode.Contains("[color=red]"))    color = LogRed;
		else if (bbcode.Contains("[color=gray]"))   color = LogGray;

		string text = Regex.Replace(bbcode, @"\[/?[^\]]*\]", "").Trim();
		if (!string.IsNullOrEmpty(text))
		{
			_debugLog.Add((text, color));
			_scrollConsoleToBottom = true;
		}
	}

	// ── Save / Load ───────────────────────────────────────────────────────────

	private const string CfgPath = "user://tuning.cfg";

	private void SaveConfig()
	{
		var cfg = new ConfigFile();

		// Window geometry (read from ImGui at save time)
		var wp = ImGui.GetWindowPos();
		var ws = ImGui.GetWindowSize();
		cfg.SetValue("Window", "x", wp.X); cfg.SetValue("Window", "y", wp.Y);
		cfg.SetValue("Window", "w", ws.X); cfg.SetValue("Window", "h", ws.Y);

		// Simulation
		cfg.SetValue("Simulation", "tps",      _ticksPerSecond);
		cfg.SetValue("Simulation", "brushSize", _brushSize);
		cfg.SetValue("Simulation", "gravity",   _sim.Gravity);
		cfg.SetValue("Simulation", "friction",  _sim.Friction);
		cfg.SetValue("Simulation", "dampCol",   _sim.DampCol);
		cfg.SetValue("Simulation", "stopThr",   _sim.StopThr);

		// Heat
		cfg.SetValue("Heat", "boilThreshold",  _sim.CopperBoilThreshold);
		cfg.SetValue("Heat", "gasThreshold",   _sim.CopperGasThreshold);
		cfg.SetValue("Heat", "iceThreshold",   _sim.IceCopperThreshold);
		cfg.SetValue("Heat", "heatRange",      _sim.HeatRange);
		cfg.SetValue("Heat", "heatFireDist",   _sim.HeatFireDist);
		cfg.SetValue("Heat", "heatSmoothing",  _sim.HeatSmoothing);

		// Fire / Plants
		cfg.SetValue("FirePlants", "fireIgniteChance", _sim.FireIgniteChance);
		cfg.SetValue("FirePlants", "fireBaseTicks",    _sim.FireBaseTicks);
		cfg.SetValue("FirePlants", "grassSeedRate",    _sim.GrassSeedRate);
		cfg.SetValue("FirePlants", "treeSeedRate",     _sim.TreeSeedRate);

		// Laser
		cfg.SetValue("Laser", "falloff",    _laserFalloff);
		cfg.SetValue("Laser", "maxBounces", _laserMaxBounces);

		// Mirrors
		cfg.SetValue("Mirrors", "rdpEpsilon",  BezierMirror.RdpEpsilon);
		cfg.SetValue("Mirrors", "rawMinDist",  BezierMirror.RawMinDist);

		// Wire
		cfg.SetValue("Wire", "snapRadius", _wireSnapRadius);

		// Arm
		cfg.SetValue("Arm", "pincerHalfWidth", _pincerHalfWidth);
		cfg.SetValue("Arm", "pincerDepth",     _pincerDepth);

		// Glorp
		cfg.SetValue("Glorp", "gravity",           Glorp.Gravity);
		cfg.SetValue("Glorp", "maxFallSpeed",       Glorp.MaxFallSpeed);
		cfg.SetValue("Glorp", "maxHorizSpeed",      Glorp.MaxHorizSpeed);
		cfg.SetValue("Glorp", "horizAccel",         Glorp.HorizAccel);
		cfg.SetValue("Glorp", "groundFriction",     Glorp.GroundFriction);
		cfg.SetValue("Glorp", "bounceRestitution",  Glorp.BounceRestitution);
		cfg.SetValue("Glorp", "hungerRate",         Glorp.HungerRate);
		cfg.SetValue("Glorp", "thirstRate",         Glorp.ThirstRate);
		cfg.SetValue("Glorp", "lonelyRate",         Glorp.LonelyRate);
		cfg.SetValue("Glorp", "hungerFill",         Glorp.HungerFill);
		cfg.SetValue("Glorp", "thirstFill",         Glorp.ThirstFill);
		cfg.SetValue("Glorp", "socialGain",         Glorp.SocialGain);
		cfg.SetValue("Glorp", "senseRange",         Glorp.SenseRange);
		cfg.SetValue("Glorp", "eatRange",           Glorp.EatRange);
		cfg.SetValue("Glorp", "talkRange",          Glorp.TalkRange);

		SaveQuickProfiles(cfg);
		SaveScripts(cfg);
		cfg.SetValue("Scripts", "smoothingSpeed", ScriptSmoothingSpeed);

		cfg.Save(CfgPath);
		ConsoleLog("[color=cyan]Settings saved.[/color]");
	}

	private void LoadConfig()
	{
		var cfg = new ConfigFile();
		if (cfg.Load(CfgPath) != Error.Ok) return;

		float G(string s, string k, float d) => (float)cfg.GetValue(s, k, Variant.From(d));
		int   I(string s, string k, int   d) => (int)  cfg.GetValue(s, k, Variant.From(d));

		// Window
		_debugWinPos  = new Vec2(G("Window","x",20), G("Window","y",20));
		_debugWinSize = new Vec2(G("Window","w",540), G("Window","h",620));
		_debugWinGeomLoaded = false; // triggers SetNextWindowPos/Size on next open

		// Simulation
		_ticksPerSecond = I("Simulation","tps",           _ticksPerSecond);
		_brushSize      = I("Simulation","brushSize",      _brushSize);
		_sim.Gravity    = G("Simulation","gravity",        _sim.Gravity);
		_sim.Friction   = G("Simulation","friction",       _sim.Friction);
		_sim.DampCol    = G("Simulation","dampCol",        _sim.DampCol);
		_sim.StopThr    = G("Simulation","stopThr",        _sim.StopThr);

		// Heat
		_sim.CopperBoilThreshold = I("Heat","boilThreshold", _sim.CopperBoilThreshold);
		_sim.CopperGasThreshold  = I("Heat","gasThreshold",  _sim.CopperGasThreshold);
		_sim.IceCopperThreshold  = I("Heat","iceThreshold",  _sim.IceCopperThreshold);
		_sim.HeatRange           = I("Heat","heatRange",     _sim.HeatRange);
		_sim.HeatFireDist        = I("Heat","heatFireDist",  _sim.HeatFireDist);
		_sim.HeatSmoothing       = I("Heat","heatSmoothing", _sim.HeatSmoothing);

		// Fire / Plants
		_sim.FireIgniteChance = G("FirePlants","fireIgniteChance", _sim.FireIgniteChance);
		_sim.FireBaseTicks    = I("FirePlants","fireBaseTicks",    _sim.FireBaseTicks);
		_sim.GrassSeedRate    = G("FirePlants","grassSeedRate",    _sim.GrassSeedRate);
		_sim.TreeSeedRate     = G("FirePlants","treeSeedRate",     _sim.TreeSeedRate);

		// Laser
		_laserFalloff    = G("Laser","falloff",    _laserFalloff);
		_laserMaxBounces = I("Laser","maxBounces", _laserMaxBounces);

		// Mirrors
		BezierMirror.RdpEpsilon = G("Mirrors","rdpEpsilon", BezierMirror.RdpEpsilon);
		BezierMirror.RawMinDist = G("Mirrors","rawMinDist", BezierMirror.RawMinDist);

		// Arm
		// Wire
		_wireSnapRadius = Math.Clamp(G("Wire","snapRadius", _wireSnapRadius), 2f, 20f);

		_pincerHalfWidth = Math.Clamp(I("Arm","pincerHalfWidth", _pincerHalfWidth), 0, RoboArm.MaxPincerHalfWidth);
		_pincerDepth     = Math.Clamp(I("Arm","pincerDepth",     _pincerDepth),     1, RoboArm.MaxPincerDepth);

		// Glorp
		Glorp.Gravity           = G("Glorp","gravity",          Glorp.Gravity);
		Glorp.MaxFallSpeed      = G("Glorp","maxFallSpeed",     Glorp.MaxFallSpeed);
		Glorp.MaxHorizSpeed     = G("Glorp","maxHorizSpeed",    Glorp.MaxHorizSpeed);
		Glorp.HorizAccel        = G("Glorp","horizAccel",       Glorp.HorizAccel);
		Glorp.GroundFriction    = G("Glorp","groundFriction",   Glorp.GroundFriction);
		Glorp.BounceRestitution = G("Glorp","bounceRestitution",Glorp.BounceRestitution);
		Glorp.HungerRate        = G("Glorp","hungerRate",       Glorp.HungerRate);
		Glorp.ThirstRate        = G("Glorp","thirstRate",       Glorp.ThirstRate);
		Glorp.LonelyRate        = G("Glorp","lonelyRate",       Glorp.LonelyRate);
		Glorp.HungerFill        = G("Glorp","hungerFill",       Glorp.HungerFill);
		Glorp.ThirstFill        = G("Glorp","thirstFill",       Glorp.ThirstFill);
		Glorp.SocialGain        = G("Glorp","socialGain",       Glorp.SocialGain);
		Glorp.SenseRange        = G("Glorp","senseRange",       Glorp.SenseRange);
		Glorp.EatRange          = G("Glorp","eatRange",         Glorp.EatRange);
		Glorp.TalkRange         = G("Glorp","talkRange",        Glorp.TalkRange);

		LoadQuickProfiles(cfg);
		LoadScripts(cfg);
		ScriptSmoothingSpeed = Math.Clamp(G("Scripts", "smoothingSpeed", ScriptSmoothingSpeed), 0.1f, 30f);
	}
}

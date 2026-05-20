using System;
using System.Collections.Generic;
using Godot;

// Machine scripting system.
//
// Player presses S to open a full-screen editor where blocks (set angle, wait,
// loop, etc.) are stacked vertically to choreograph a turret or robotic arm.
// Saved scripts appear in the Scripts toolbar tab; clicking one makes it the
// active brush, and clicking on a machine attaches the script. RMB detaches.
//
// Scripts target a specific machine type (turret OR arm). Each attached script
// runs in its own ScriptRuntime instance so two machines can share a script
// definition but keep independent execution state.
public partial class Main
{
	// ── Data model ─────────────────────────────────────────────────────────────

	public enum ScriptMachineType : byte { Turret = 0, Arm = 1 }

	public enum BlockType : byte
	{
		// Control flow (common to all machines)
		Wait,
		LoopN,
		LoopForever,
		// Turret actions
		TurretSetAngle,
		TurretAddAngle,
		TurretLaserOn,
		TurretLaserOff,
		// Arm actions
		ArmSetShoulder,
		ArmSetElbow,
		ArmAddShoulder,
		ArmAddElbow,
		ArmOpenClaw,
		ArmCloseClaw,
		// Track actions (available on any machine that can ride a track)
		TrackSetPosition,
		TrackAddPosition,
	}

	public sealed class ScriptBlock
	{
		public BlockType        Type;
		public float            ValueF;   // angle in degrees (converted to radians on execute)
		public int              ValueI;   // wait ticks or loop iterations
		public List<ScriptBlock> Body;    // non-null for LoopN / LoopForever

		public ScriptBlock Clone()
		{
			var c = new ScriptBlock { Type = Type, ValueF = ValueF, ValueI = ValueI };
			if (Body != null)
			{
				c.Body = new List<ScriptBlock>(Body.Count);
				foreach (var b in Body) c.Body.Add(b.Clone());
			}
			return c;
		}
	}

	public sealed class MachineScript
	{
		public string                  Name = "Untitled";
		public ScriptMachineType       MachineType;
		public int                     RateTicks = 30;   // ticks per action block
		public readonly List<ScriptBlock> Blocks = new();
	}

	// One frame on the execution stack — represents "we're at Index in this block
	// list, with LoopRemaining iterations left when we hit the end."
	public struct ScriptFrame
	{
		public List<ScriptBlock> Blocks;
		public int Index;
		public int LoopRemaining; // -1 = infinite; otherwise iterations left when end reached
	}

	public sealed class ScriptRuntime
	{
		public MachineScript     Script;
		public Stack<ScriptFrame> Stack = new();
		public int               WaitRemaining; // ticks left on the current Wait block
		public int               TickAccum;     // ticks accumulated toward next action
		public bool              Finished;

		public void Reset()
		{
			Stack.Clear();
			WaitRemaining = 0;
			TickAccum     = 0;
			Finished      = false;
			if (Script != null)
				Stack.Push(new ScriptFrame { Blocks = Script.Blocks, Index = 0, LoopRemaining = 1 });
		}
	}

	// ── State ──────────────────────────────────────────────────────────────────

	private const int BrushScript = -11;

	private readonly List<MachineScript> _scripts = new();
	private int  _activeScriptIdx = -1;   // index into _scripts; -1 = none

	// Editor
	private bool          _scriptEditorOpen;
	private MachineScript _editingScript;
	private ScriptBlock   _editorAddTarget;     // null = top level; otherwise the loop block whose Body is the current scope

	// Editor UI (created programmatically when first opened)
	private CanvasLayer   _editorLayer;
	private Control       _editorRoot;
	private LineEdit      _editorNameEdit;
	private SpinBox       _editorRateSpin;
	private OptionButton  _editorScriptPicker;
	private VBoxContainer _editorPaletteList;
	private VBoxContainer _editorCanvasList;
	private Label         _editorScopeLabel;

	// Scripts tab UI refs
	private Button         _btnTabScripts;
	private Control        _scriptsPage;
	private HBoxContainer  _scriptsToolbar;
	private VBoxContainer  _scriptsList;
	private OptionButton   _scriptsFilter;

	// Ticks accumulated this frame (set by _Process loop); scripts advance this much per frame
	private int _scriptTicksThisFrame;

	// Global smoothing speed for scripted machines (degrees per sim tick).
	// 3 °/tick = 90 °/sec at 30 TPS. Tunable in the ImGui Scripts tab.
	public static float ScriptSmoothingSpeed = 3f;

	// Moves `current` toward `target` by at most `maxDelta`, never overshooting.
	// Operates on the literal numeric value (no angle wrap) so Add Angle can drive
	// continuous rotation by accumulating beyond ±π.
	private static float MoveAngleToward(float current, float target, float maxDelta)
	{
		float diff = target - current;
		if (MathF.Abs(diff) <= maxDelta) return target;
		return current + MathF.Sign(diff) * maxDelta;
	}

	// How many sim ticks the smoothing needs to drive `current` to `target` at the
	// global smoothing speed. Used by Set/Add motion blocks to stall the runtime
	// until their motion is visually complete.
	private static int AngleMotionTicks(float current, float target)
	{
		float distance = MathF.Abs(target - current);
		float speed    = Mathf.DegToRad(ScriptSmoothingSpeed);
		if (speed <= 0f) return 0;
		return (int)MathF.Ceiling(distance / speed);
	}

	// ── Live preview (bottom-right of editor) ──────────────────────────────────
	// A bare machine that mirrors the script's motion in real time without needing
	// to attach the script to anything in the world. Always-running, auto-loops at
	// end, honours the script's RateTicks.
	private sealed class ScriptPreview
	{
		public float ShoulderAngle       = -MathF.PI / 2f; // turret angle OR arm shoulder
		public float ElbowAngle          = -MathF.PI / 2f; // arm only
		public float TargetShoulderAngle = -MathF.PI / 2f;
		public float TargetElbowAngle    = -MathF.PI / 2f;
		public bool  ClawClosed;
		public bool  LaserOn    = true;
		public float TrackT     = 0f;
		public float TargetTrackT = 0f;
		public ScriptRuntime Runtime;
	}
	private readonly ScriptPreview _preview = new();
	private OverlayCanvas _previewCanvas;

	// ── Init ───────────────────────────────────────────────────────────────────

	private void InitScripts()
	{
		const string sp = "UI/ToolBox/Panel/VBoxContainer/ScriptsPage/";
		_btnTabScripts  = GetNode<Button>("UI/ToolBox/Panel/VBoxContainer/TabBar/BtnTabScripts");
		_scriptsPage    = GetNode<Control>("UI/ToolBox/Panel/VBoxContainer/ScriptsPage");
		_scriptsToolbar = GetNode<HBoxContainer>(sp + "ScriptsToolbar");
		_scriptsList    = GetNode<VBoxContainer>(sp + "ScriptsScroll/ScriptsList");

		_btnTabScripts.Pressed += () => SetActiveTab(4);

		BuildScriptsToolbar();
		RefreshScriptsList();
	}

	private void BuildScriptsToolbar()
	{
		_scriptsFilter = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		_scriptsFilter.AddItem("All",     0);
		_scriptsFilter.AddItem("Turret",  1);
		_scriptsFilter.AddItem("Arm",     2);
		_scriptsFilter.ItemSelected += _ => RefreshScriptsList();

		var newTurret = new Button { Text = "+ Turret", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		newTurret.Pressed += () => CreateNewScript(ScriptMachineType.Turret);

		var newArm = new Button { Text = "+ Arm", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		newArm.Pressed += () => CreateNewScript(ScriptMachineType.Arm);

		_scriptsToolbar.AddChild(_scriptsFilter);
		_scriptsToolbar.AddChild(newTurret);
		_scriptsToolbar.AddChild(newArm);
	}

	private void CreateNewScript(ScriptMachineType type)
	{
		string baseName = type == ScriptMachineType.Turret ? "Turret Script" : "Arm Script";
		var script = new MachineScript
		{
			Name        = $"{baseName} {_scripts.Count + 1}",
			MachineType = type,
		};
		_scripts.Add(script);
		_activeScriptIdx = _scripts.Count - 1;
		RefreshScriptsList();
		AutoSaveScripts();
		OpenScriptEditor(script);
	}

	private void RefreshScriptsList()
	{
		foreach (Node child in _scriptsList.GetChildren())
			child.QueueFree();

		int filter = _scriptsFilter?.Selected ?? 0; // 0=all, 1=turret, 2=arm

		for (int i = 0; i < _scripts.Count; i++)
		{
			var s = _scripts[i];
			if (filter == 1 && s.MachineType != ScriptMachineType.Turret) continue;
			if (filter == 2 && s.MachineType != ScriptMachineType.Arm)    continue;

			int idx = i;
			var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
			string badge = s.MachineType == ScriptMachineType.Turret ? "[T]" : "[A]";
			var pick = new Button
			{
				Text                = $"{badge} {s.Name}",
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
				Modulate            = (idx == _activeScriptIdx && _brush == BrushScript) ? Colors.Yellow : Colors.White,
				TooltipText         = "Click to make this the active script brush (then click a machine to attach)",
			};
			pick.Pressed += () =>
			{
				_activeScriptIdx = idx;
				SetBrush(BrushScript);
				RefreshScriptsList();
			};

			var edit = new Button { Text = "Edit", TooltipText = "Open in editor" };
			edit.Pressed += () => OpenScriptEditor(_scripts[idx]);

			var del = new Button { Text = "✕", TooltipText = "Delete this script" };
			del.Pressed += () =>
			{
				_scripts.RemoveAt(idx);
				if (_activeScriptIdx == idx) _activeScriptIdx = -1;
				else if (_activeScriptIdx > idx) _activeScriptIdx--;
				DetachScriptInstancesOfDeleted(_scripts.Count > idx ? null : null); // no-op safety
				RefreshScriptsList();
				AutoSaveScripts();
			};

			row.AddChild(pick);
			row.AddChild(edit);
			row.AddChild(del);
			_scriptsList.AddChild(row);
		}
	}

	private void DetachScriptInstancesOfDeleted(MachineScript deleted)
	{
		if (deleted == null) return;
		foreach (var t in _turrets) if (t.ScriptRT?.Script == deleted) t.ScriptRT = null;
		foreach (var a in _arms)    if (a.ScriptRT?.Script == deleted) a.ScriptRT = null;
	}

	// ── Script-as-brush attach / detach ────────────────────────────────────────

	// Returns the LaserTurret or RoboArm at simPos, preferring the one whose 5×3 / 3×3
	// footprint contains the cell. Falls back to origin-based bounds for track-mounted
	// machines (which have no grid cells in OccupiedIndices).
	private object FindMachineAt(Vector2I simPos)
	{
		int idx = simPos.Y * SimW + simPos.X;
		foreach (var t in _turrets)
		{
			if (t.OccupiedIndices.Contains(idx)) return t;
			if (t.Track != null &&
				simPos.X >= t.Origin.X - LaserTurret.BaseHalfW &&
				simPos.X <= t.Origin.X + LaserTurret.BaseHalfW &&
				simPos.Y >= t.Origin.Y &&
				simPos.Y <  t.Origin.Y + LaserTurret.BaseH)
				return t;
		}
		foreach (var a in _arms)
		{
			if (a.OccupiedIndices.Contains(idx)) return a;
			if (a.Track != null &&
				simPos.X >= a.Origin.X - RoboArm.BaseHalfW &&
				simPos.X <= a.Origin.X + RoboArm.BaseHalfW &&
				simPos.Y >= a.Origin.Y - RoboArm.BaseHalfW &&
				simPos.Y <= a.Origin.Y + RoboArm.BaseHalfW)
				return a;
		}
		return null;
	}

	private void AttachActiveScriptAt(Vector2I simPos)
	{
		if (_activeScriptIdx < 0 || _activeScriptIdx >= _scripts.Count) return;
		var script  = _scripts[_activeScriptIdx];
		var machine = FindMachineAt(simPos);
		if (machine == null) return;

		if (machine is LaserTurret t && script.MachineType == ScriptMachineType.Turret)
		{
			t.ScriptRT     = new ScriptRuntime { Script = script };
			t.ScriptRT.Reset();
			t.TargetAngle  = t.Angle;        // start with no pending motion
		}
		else if (machine is RoboArm a && script.MachineType == ScriptMachineType.Arm)
		{
			a.ScriptRT            = new ScriptRuntime { Script = script };
			a.ScriptRT.Reset();
			a.TargetShoulderAngle = a.ShoulderAngle;
			a.TargetElbowAngle    = a.ElbowAngle;
		}
		// Silent no-op on type mismatch
	}

	private void DetachScriptAt(Vector2I simPos)
	{
		var machine = FindMachineAt(simPos);
		if      (machine is LaserTurret t) t.ScriptRT = null;
		else if (machine is RoboArm    a)  a.ScriptRT = null;
	}

	// ── Runtime execution ──────────────────────────────────────────────────────

	// Advances the runtime by `ticks` sim ticks. Action blocks consume `RateTicks`
	// ticks each; Wait consumes exactly its specified ticks; control blocks (loops)
	// consume 0 ticks. Dispatch is via callbacks so the same engine drives turrets
	// and arms.
	private void TickRuntime(ScriptRuntime rt, int ticks, Action<ScriptBlock> execAction)
	{
		if (rt == null || rt.Finished || rt.Script == null) return;

		while (ticks > 0)
		{
			if (rt.WaitRemaining > 0)
			{
				int consume = Math.Min(ticks, rt.WaitRemaining);
				rt.WaitRemaining -= consume;
				ticks            -= consume;
				continue;
			}
			if (rt.TickAccum < rt.Script.RateTicks)
			{
				int need    = rt.Script.RateTicks - rt.TickAccum;
				int consume = Math.Min(ticks, need);
				rt.TickAccum += consume;
				ticks        -= consume;
				if (rt.TickAccum < rt.Script.RateTicks) continue;
			}

			// TickAccum has reached RateTicks — fire the next instruction
			rt.TickAccum = 0;
			if (!StepRuntime(rt, execAction))
			{
				rt.Finished = true;
				return;
			}
		}
	}

	// Fires one instruction step. Returns false if the script has fully completed.
	private bool StepRuntime(ScriptRuntime rt, Action<ScriptBlock> execAction)
	{
		while (rt.Stack.Count > 0)
		{
			var frame = rt.Stack.Peek();
			if (frame.Index >= frame.Blocks.Count)
			{
				// Reached end of current block list — pop frame or loop back
				rt.Stack.Pop();
				if (frame.LoopRemaining < 0 || frame.LoopRemaining > 1)
				{
					int next = frame.LoopRemaining < 0 ? -1 : frame.LoopRemaining - 1;
					rt.Stack.Push(new ScriptFrame
					{
						Blocks        = frame.Blocks,
						Index         = 0,
						LoopRemaining = next,
					});
				}
				continue;
			}

			var block = frame.Blocks[frame.Index];
			// Advance index BEFORE executing — loop blocks push a new frame and we want
			// to resume after the loop block on the outer frame.
			var advanced = frame;
			advanced.Index++;
			rt.Stack.Pop();
			rt.Stack.Push(advanced);

			switch (block.Type)
			{
				case BlockType.Wait:
					rt.WaitRemaining = Math.Max(1, block.ValueI);
					return true;
				case BlockType.LoopN:
					if (block.Body != null && block.Body.Count > 0 && block.ValueI > 0)
					{
						rt.Stack.Push(new ScriptFrame
						{
							Blocks = block.Body, Index = 0, LoopRemaining = block.ValueI,
						});
					}
					continue; // loops don't consume the per-block rate budget
				case BlockType.LoopForever:
					if (block.Body != null && block.Body.Count > 0)
					{
						rt.Stack.Push(new ScriptFrame
						{
							Blocks = block.Body, Index = 0, LoopRemaining = -1,
						});
					}
					continue;
				default:
					execAction(block);
					return true;
			}
		}
		return false;
	}

	// Action blocks set the TARGET angle. The actual Angle field interpolates toward
	// the target every tick at ScriptSmoothingSpeed degrees per tick (see UpdateTurrets / UpdateArms).
	// Motion blocks also set WaitRemaining = motion duration, so the runtime waits for the
	// motion to fully complete before firing the next block. RateTicks then adds extra
	// dwell on top of that.
	private void ExecuteTurretBlock(LaserTurret t, ScriptBlock b)
	{
		switch (b.Type)
		{
			case BlockType.TurretSetAngle:
			{
				float newTarget = Mathf.DegToRad(b.ValueF);
				if (t.ScriptRT != null) t.ScriptRT.WaitRemaining = AngleMotionTicks(t.Angle, newTarget);
				t.TargetAngle = newTarget;
				break;
			}
			case BlockType.TurretAddAngle:
			{
				// Fire-and-forget: shift the target and let the next block run immediately.
				t.TargetAngle += Mathf.DegToRad(b.ValueF);
				break;
			}
			case BlockType.TurretLaserOn:  t.LaserOn = true;  break;
			case BlockType.TurretLaserOff: t.LaserOn = false; break;
			default: ExecuteTrackBlock(t.Track, ref t.TrackT, ref t.TargetTrackT, t.ScriptRT, b); break;
		}
	}

	private void ExecuteArmBlock(RoboArm a, ScriptBlock b)
	{
		switch (b.Type)
		{
			case BlockType.ArmSetShoulder:
			{
				float newTarget = Mathf.DegToRad(b.ValueF);
				if (a.ScriptRT != null) a.ScriptRT.WaitRemaining = AngleMotionTicks(a.ShoulderAngle, newTarget);
				a.TargetShoulderAngle = newTarget;
				break;
			}
			case BlockType.ArmSetElbow:
			{
				float newTarget = Mathf.DegToRad(b.ValueF);
				if (a.ScriptRT != null) a.ScriptRT.WaitRemaining = AngleMotionTicks(a.ElbowAngle, newTarget);
				a.TargetElbowAngle = newTarget;
				break;
			}
			case BlockType.ArmAddShoulder:
			{
				// Fire-and-forget: shift the target and continue immediately.
				a.TargetShoulderAngle += Mathf.DegToRad(b.ValueF);
				break;
			}
			case BlockType.ArmAddElbow:
			{
				a.TargetElbowAngle += Mathf.DegToRad(b.ValueF);
				break;
			}
			case BlockType.ArmOpenClaw:
				if (a.ClawClosed) ToggleArmClaw(a);
				break;
			case BlockType.ArmCloseClaw:
				if (!a.ClawClosed) ToggleArmClaw(a);
				break;
			default: ExecuteTrackBlock(a.Track, ref a.TrackT, ref a.TargetTrackT, a.ScriptRT, b); break;
		}
	}

	private static int TrackMotionTicks(float current, float target)
	{
		float dist = MathF.Abs(target - current);
		return TrackSmoothingSpeed <= 0f ? 0 : (int)MathF.Ceiling(dist / TrackSmoothingSpeed);
	}

	private static void ExecuteTrackBlock(RailTrack track, ref float trackT, ref float targetTrackT,
		ScriptRuntime rt, ScriptBlock b)
	{
		switch (b.Type)
		{
			case BlockType.TrackSetPosition:
			{
				float newTarget = Math.Clamp(b.ValueF, 0f, 1f);
				if (rt != null) rt.WaitRemaining = TrackMotionTicks(trackT, newTarget);
				targetTrackT = newTarget;
				break;
			}
			case BlockType.TrackAddPosition:
				targetTrackT = Math.Clamp(targetTrackT + b.ValueF, 0f, 1f);
				break;
		}
	}

	// Toggles a specific arm's claw without requiring it to be the "active" arm.
	// Shared by the script runtime, the Space key (proximity), and the active-arm fallback.
	private void ToggleArmClaw(RoboArm arm)
	{
		if (!arm.Powered) return;

		if (!arm.ClawClosed)
		{
			int hw    = _pincerHalfWidth;
			int depth = _pincerDepth;
			int n     = RoboArm.PincerCellCount(hw, depth);
			Span<Vector2I> pincer = stackalloc Vector2I[RoboArm.PincerCellCount(RoboArm.MaxPincerHalfWidth, RoboArm.MaxPincerDepth)];
			arm.GetPincerCells(hw, depth, pincer[..n]);
			arm.HeldHalfWidth = hw;
			arm.HeldDepth     = depth;
			arm.Held.Clear();
			for (int i = 0; i < n; i++)
			{
				var p = pincer[i];
				if (!_sim.InBounds(p.X, p.Y)) { arm.Held.Add((0, 0)); continue; }
				int idx = p.Y * SimW + p.X;
				byte c = _sim.Grid[idx];
				if (_clawWhitelist.Contains(c) && _sim.Pinned[idx] == 0)
				{
					arm.Held.Add((c, _sim.Flow[idx]));
					_sim.SetCell(p.X, p.Y, (int)Simulation.Cell.Air);
				}
				else
				{
					arm.Held.Add((0, 0));
				}
			}
			arm.ClawClosed = true;
		}
		else
		{
			int n     = arm.Held.Count;
			int hw    = arm.HeldHalfWidth;
			int depth = arm.HeldDepth;
			Span<Vector2I> pincer = stackalloc Vector2I[RoboArm.PincerCellCount(RoboArm.MaxPincerHalfWidth, RoboArm.MaxPincerDepth)];
			arm.GetPincerCells(hw, depth, pincer[..n]);
			for (int i = 0; i < n; i++)
			{
				var (cell, flow) = arm.Held[i];
				if (cell == 0) continue;
				var p = pincer[i];
				if (!_sim.InBounds(p.X, p.Y)) continue;
				int idx = p.Y * SimW + p.X;
				if (_sim.Grid[idx] != (byte)Simulation.Cell.Air) continue;
				_sim.SetCell(p.X, p.Y, cell);
				_sim.Flow[idx] = flow;
				_sim.VelY[idx] = 1.0f;
			}
			arm.Held.Clear();
			arm.ClawClosed = false;
		}
		_sim.RenderDirty = true;
	}

	// ── Editor ─────────────────────────────────────────────────────────────────

	private void ToggleScriptEditor()
	{
		if (_scriptEditorOpen) CloseScriptEditor();
		else
		{
			if (_scripts.Count == 0)
			{
				// Auto-create a first turret script so the editor has something to show
				CreateNewScript(ScriptMachineType.Turret);
				return; // CreateNewScript already opened the editor
			}
			OpenScriptEditor(_editingScript ?? _scripts[0]);
		}
	}

	private void OpenScriptEditor(MachineScript script)
	{
		_editingScript    = script;
		_editorAddTarget  = null;
		_scriptEditorOpen = true;

		if (_editorLayer == null) BuildEditorUI();
		_editorLayer.Visible = true;

		// Reset preview to default pose so the new script starts from a known state
		_preview.ShoulderAngle       = -MathF.PI / 2f;
		_preview.ElbowAngle          = -MathF.PI / 2f;
		_preview.TargetShoulderAngle = -MathF.PI / 2f;
		_preview.TargetElbowAngle    = -MathF.PI / 2f;
		_preview.ClawClosed          = false;
		_preview.LaserOn             = true;
		_preview.Runtime             = null;

		SyncEditorToScript();
	}

	private void CloseScriptEditor()
	{
		_scriptEditorOpen = false;
		if (_editorLayer != null) _editorLayer.Visible = false;
		AutoSaveScripts();
	}

	private void BuildEditorUI()
	{
		_editorLayer = new CanvasLayer { Layer = 50 };
		AddChild(_editorLayer);

		// Dimming background
		var bg = new ColorRect
		{
			Color           = new Color(0.04f, 0.04f, 0.06f, 0.92f),
			AnchorLeft = 0, AnchorTop = 0, AnchorRight = 1, AnchorBottom = 1,
			MouseFilter     = Control.MouseFilterEnum.Stop,
		};
		_editorLayer.AddChild(bg);

		// Centered editor panel
		var panel = new PanelContainer
		{
			AnchorLeft = 0.05f, AnchorTop = 0.05f,
			AnchorRight = 0.95f, AnchorBottom = 0.95f,
		};
		_editorLayer.AddChild(panel);

		var outerV = new VBoxContainer();
		panel.AddChild(outerV);

		// Header row
		var header = new HBoxContainer();
		outerV.AddChild(header);

		header.AddChild(new Label { Text = "Script:" });

		_editorScriptPicker = new OptionButton();
		_editorScriptPicker.ItemSelected += idx =>
		{
			if (idx >= 0 && idx < _scripts.Count) OpenScriptEditor(_scripts[(int)idx]);
		};
		header.AddChild(_editorScriptPicker);

		_editorNameEdit = new LineEdit
		{
			PlaceholderText     = "Name…",
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		_editorNameEdit.TextChanged += t =>
		{
			if (_editingScript != null) { _editingScript.Name = t; AutoSaveScripts(); }
		};
		header.AddChild(_editorNameEdit);

		header.AddChild(new Label { Text = "Rate (ticks/block):" });
		_editorRateSpin = new SpinBox { MinValue = 1, MaxValue = 240, Value = 30 };
		_editorRateSpin.ValueChanged += v =>
		{
			if (_editingScript != null) { _editingScript.RateTicks = (int)v; AutoSaveScripts(); ResetPreviewRuntime(); }
		};
		header.AddChild(_editorRateSpin);

		var closeBtn = new Button { Text = "✕ Close (S)" };
		closeBtn.Pressed += CloseScriptEditor;
		header.AddChild(closeBtn);

		outerV.AddChild(new HSeparator());

		// Body: palette | canvas
		var body = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
		outerV.AddChild(body);

		// Palette column (fixed width)
		var paletteScroll = new ScrollContainer
		{
			CustomMinimumSize   = new Vector2(220, 0),
			SizeFlagsVertical   = Control.SizeFlags.ExpandFill,
		};
		body.AddChild(paletteScroll);
		_editorPaletteList = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		paletteScroll.AddChild(_editorPaletteList);

		// Canvas column
		var canvasV = new VBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical   = Control.SizeFlags.ExpandFill,
		};
		body.AddChild(canvasV);

		_editorScopeLabel = new Label { Text = "Adding to: top level" };
		canvasV.AddChild(_editorScopeLabel);

		var topBtn = new Button { Text = "↑ Add to top level" };
		topBtn.Pressed += () => { _editorAddTarget = null; RefreshEditorCanvas(); };
		canvasV.AddChild(topBtn);

		var canvasScroll = new ScrollContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical   = Control.SizeFlags.ExpandFill,
		};
		canvasV.AddChild(canvasScroll);
		_editorCanvasList = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		canvasScroll.AddChild(_editorCanvasList);

		// Live preview pane (bottom of canvas column)
		canvasV.AddChild(new HSeparator());
		var previewHeader = new HBoxContainer();
		previewHeader.AddChild(new Label
		{
			Text                = "Live Preview  (runs the script on a bare machine)",
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		});
		var replayBtn = new Button { Text = "↻ Replay" };
		replayBtn.TooltipText = "Reset the preview machine and replay the script from the first block.";
		replayBtn.Pressed += ResetPreviewRuntime;
		previewHeader.AddChild(replayBtn);
		canvasV.AddChild(previewHeader);
		_previewCanvas = new OverlayCanvas
		{
			CustomMinimumSize   = new Vector2(0, 220),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		_previewCanvas.OnDraw = DrawPreview;
		canvasV.AddChild(_previewCanvas);
	}

	// Sync entire editor view to the current _editingScript
	private void SyncEditorToScript()
	{
		// Script picker
		_editorScriptPicker.Clear();
		int activeIdx = 0;
		for (int i = 0; i < _scripts.Count; i++)
		{
			var s     = _scripts[i];
			string b  = s.MachineType == ScriptMachineType.Turret ? "[T]" : "[A]";
			_editorScriptPicker.AddItem($"{b} {s.Name}", i);
			if (s == _editingScript) activeIdx = i;
		}
		if (_scripts.Count > 0) _editorScriptPicker.Selected = activeIdx;

		// Name + rate
		if (_editingScript != null)
		{
			_editorNameEdit.Text   = _editingScript.Name;
			_editorRateSpin.Value  = _editingScript.RateTicks;
		}

		RefreshEditorPalette();
		RefreshEditorCanvas();
	}

	// Returns the block types available for the current script's machine type,
	// plus the common control blocks (Wait/Loop).
	private static IEnumerable<BlockType> PaletteFor(ScriptMachineType type)
	{
		// Control blocks first
		yield return BlockType.Wait;
		yield return BlockType.LoopN;
		yield return BlockType.LoopForever;
		if (type == ScriptMachineType.Turret)
		{
			yield return BlockType.TurretSetAngle;
			yield return BlockType.TurretAddAngle;
			yield return BlockType.TurretLaserOn;
			yield return BlockType.TurretLaserOff;
		}
		else
		{
			yield return BlockType.ArmSetShoulder;
			yield return BlockType.ArmSetElbow;
			yield return BlockType.ArmAddShoulder;
			yield return BlockType.ArmAddElbow;
			yield return BlockType.ArmOpenClaw;
			yield return BlockType.ArmCloseClaw;
		}
		// Track blocks are available to all machine types
		yield return BlockType.TrackSetPosition;
		yield return BlockType.TrackAddPosition;
	}

	private static string BlockLabel(BlockType t) => t switch
	{
		BlockType.Wait            => "Wait (ticks)",
		BlockType.LoopN           => "Loop N times",
		BlockType.LoopForever     => "Loop forever",
		BlockType.TurretSetAngle  => "Set angle (°)",
		BlockType.TurretAddAngle  => "Add to angle (°)",
		BlockType.TurretLaserOn   => "Laser ON",
		BlockType.TurretLaserOff  => "Laser OFF",
		BlockType.ArmSetShoulder  => "Set shoulder (°)",
		BlockType.ArmSetElbow     => "Set elbow (°)",
		BlockType.ArmAddShoulder  => "Add to shoulder (°)",
		BlockType.ArmAddElbow     => "Add to elbow (°)",
		BlockType.ArmOpenClaw        => "Open claw",
		BlockType.ArmCloseClaw       => "Close claw",
		BlockType.TrackSetPosition   => "Track: set pos (0-1)",
		BlockType.TrackAddPosition   => "Track: add pos (0-1)",
		_                            => "?",
	};

	private void RefreshEditorPalette()
	{
		foreach (Node child in _editorPaletteList.GetChildren())
			child.QueueFree();

		if (_editingScript == null) return;

		_editorPaletteList.AddChild(new Label { Text = "PALETTE — click to add" });

		foreach (var t in PaletteFor(_editingScript.MachineType))
		{
			var btn = new Button { Text = BlockLabel(t), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
			var captured = t;
			btn.Pressed += () => AppendBlockToScope(captured);
			_editorPaletteList.AddChild(btn);
		}
	}

	private List<ScriptBlock> CurrentScope()
		=> _editorAddTarget != null ? (_editorAddTarget.Body ??= new List<ScriptBlock>())
		                            : _editingScript.Blocks;

	private void AppendBlockToScope(BlockType type)
	{
		var b = new ScriptBlock { Type = type };
		if (type == BlockType.LoopN)        { b.ValueI = 3;  b.Body = new(); }
		if (type == BlockType.LoopForever)  { b.Body   = new(); }
		if (type == BlockType.Wait)         { b.ValueI = 30; }
		// Angle defaults: 0
		CurrentScope().Add(b);
		AutoSaveScripts();
		ResetPreviewRuntime();
		RefreshEditorCanvas();
	}

	// Cancels any in-flight motion in the preview and rewinds the runtime to the
	// start of the script. Called after any edit that changes execution semantics
	// (block add/delete/reorder/param/rate change) so the user immediately sees the
	// effect of their change from a clean state.
	private void ResetPreviewRuntime()
	{
		_preview.ShoulderAngle       = -MathF.PI / 2f;
		_preview.ElbowAngle          = -MathF.PI / 2f;
		_preview.TargetShoulderAngle = -MathF.PI / 2f;
		_preview.TargetElbowAngle    = -MathF.PI / 2f;
		_preview.ClawClosed          = false;
		_preview.LaserOn             = true;
		_preview.TrackT              = 0f;
		_preview.TargetTrackT        = 0f;
		_preview.Runtime             = null;
		_previewCanvas?.QueueRedraw();
	}

	private void RefreshEditorCanvas()
	{
		foreach (Node child in _editorCanvasList.GetChildren())
			child.QueueFree();

		if (_editingScript == null) return;

		_editorScopeLabel.Text = _editorAddTarget == null
			? "Adding to: top level"
			: $"Adding to: inside {BlockLabel(_editorAddTarget.Type)}";

		RenderBlocks(_editingScript.Blocks, _editorCanvasList, depth: 0);
	}

	private void RenderBlocks(List<ScriptBlock> blocks, Container parent, int depth)
	{
		for (int i = 0; i < blocks.Count; i++)
		{
			var block      = blocks[i];
			int capturedI  = i;
			var capturedList = blocks;
			RenderOneBlockRow(block, parent, capturedList, capturedI, depth);

			if (block.Body != null)
			{
				var bodyRow = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
				parent.AddChild(bodyRow);
				// Indent spacer
				bodyRow.AddChild(new Control { CustomMinimumSize = new Vector2((depth + 1) * 18 + 6, 0) });
				var bodyCol = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
				bodyRow.AddChild(bodyCol);
				RenderBlocks(block.Body, bodyCol, depth + 1);
				// "End" footer for visual closure
				var endLabel = new Label { Text = "└ end", Modulate = new Color(0.6f, 0.6f, 0.7f) };
				bodyCol.AddChild(endLabel);
			}
		}
	}

	private void RenderOneBlockRow(ScriptBlock block, Container parent, List<ScriptBlock> ownerList, int idx, int depth)
	{
		var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		parent.AddChild(row);

		// Indent
		if (depth > 0) row.AddChild(new Control { CustomMinimumSize = new Vector2(depth * 18, 0) });

		bool isLoop = block.Type == BlockType.LoopN || block.Type == BlockType.LoopForever;
		bool isHighlighted = block == _editorAddTarget;

		var lbl = new Label
		{
			Text                = BlockLabel(block.Type),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			Modulate            = isHighlighted ? Colors.Yellow : Colors.White,
		};
		row.AddChild(lbl);

		// Param input
		if (block.Type == BlockType.Wait || block.Type == BlockType.LoopN)
		{
			var spin = new SpinBox { MinValue = 1, MaxValue = 9999, Value = block.ValueI };
			spin.ValueChanged += v => { block.ValueI = (int)v; AutoSaveScripts(); ResetPreviewRuntime(); };
			row.AddChild(spin);
		}
		else if (block.Type == BlockType.TurretSetAngle || block.Type == BlockType.TurretAddAngle ||
				 block.Type == BlockType.ArmSetShoulder || block.Type == BlockType.ArmAddShoulder ||
				 block.Type == BlockType.ArmSetElbow    || block.Type == BlockType.ArmAddElbow)
		{
			var spin = new SpinBox { MinValue = -3600, MaxValue = 3600, Step = 1, Value = block.ValueF };
			spin.ValueChanged += v => { block.ValueF = (float)v; AutoSaveScripts(); ResetPreviewRuntime(); };
			row.AddChild(spin);
		}
		else if (block.Type == BlockType.TrackSetPosition)
		{
			var spin = new SpinBox { MinValue = 0.0, MaxValue = 1.0, Step = 0.01, Value = block.ValueF };
			spin.ValueChanged += v => { block.ValueF = (float)v; AutoSaveScripts(); ResetPreviewRuntime(); };
			row.AddChild(spin);
		}
		else if (block.Type == BlockType.TrackAddPosition)
		{
			var spin = new SpinBox { MinValue = -1.0, MaxValue = 1.0, Step = 0.01, Value = block.ValueF };
			spin.ValueChanged += v => { block.ValueF = (float)v; AutoSaveScripts(); ResetPreviewRuntime(); };
			row.AddChild(spin);
		}

		// Loop-only: "enter scope" button
		if (isLoop)
		{
			var enterBtn = new Button { Text = isHighlighted ? "★ Inside" : "Add inside" };
			enterBtn.Pressed += () =>
			{
				_editorAddTarget = isHighlighted ? null : block;
				RefreshEditorCanvas();
			};
			row.AddChild(enterBtn);
		}

		// Reorder up/down
		var upBtn = new Button { Text = "↑" };
		upBtn.Disabled = idx == 0;
		upBtn.Pressed += () =>
		{
			if (idx > 0)
			{
				(ownerList[idx - 1], ownerList[idx]) = (ownerList[idx], ownerList[idx - 1]);
				AutoSaveScripts();
				ResetPreviewRuntime();
				RefreshEditorCanvas();
			}
		};
		row.AddChild(upBtn);

		var dnBtn = new Button { Text = "↓" };
		dnBtn.Disabled = idx >= ownerList.Count - 1;
		dnBtn.Pressed += () =>
		{
			if (idx < ownerList.Count - 1)
			{
				(ownerList[idx + 1], ownerList[idx]) = (ownerList[idx], ownerList[idx + 1]);
				AutoSaveScripts();
				ResetPreviewRuntime();
				RefreshEditorCanvas();
			}
		};
		row.AddChild(dnBtn);

		var delBtn = new Button { Text = "✕" };
		delBtn.Pressed += () =>
		{
			if (_editorAddTarget == block) _editorAddTarget = null;
			ownerList.RemoveAt(idx);
			AutoSaveScripts();
			ResetPreviewRuntime();
			RefreshEditorCanvas();
		};
		row.AddChild(delBtn);
	}

	// ── Live preview tick + draw ───────────────────────────────────────────────

	// Called every frame from _Process. Advances the preview runtime and triggers
	// a redraw. Auto-loops when the script reaches its end.
	private void TickPreview()
	{
		if (!_scriptEditorOpen || _editingScript == null) return;

		// (Re)init runtime when the editor switched to a different script. Otherwise
		// let it run to completion and stop — matching the real-machine behavior.
		// Edits trigger ResetPreviewRuntime which nulls Runtime, restarting the script.
		if (_preview.Runtime == null || _preview.Runtime.Script != _editingScript)
		{
			_preview.Runtime = new ScriptRuntime { Script = _editingScript };
			_preview.Runtime.Reset();
		}

		TickRuntime(_preview.Runtime, _scriptTicksThisFrame, ExecutePreviewBlock);

		float maxDelta = Mathf.DegToRad(ScriptSmoothingSpeed * _scriptTicksThisFrame);
		_preview.ShoulderAngle = MoveAngleToward(_preview.ShoulderAngle, _preview.TargetShoulderAngle, maxDelta);
		_preview.ElbowAngle    = MoveAngleToward(_preview.ElbowAngle,    _preview.TargetElbowAngle,    maxDelta);
		_preview.TrackT        = Math.Clamp(MoveAngleToward(_preview.TrackT, _preview.TargetTrackT,
		                             TrackSmoothingSpeed * _scriptTicksThisFrame), 0f, 1f);

		_previewCanvas?.QueueRedraw();
	}

	private void ExecutePreviewBlock(ScriptBlock b)
	{
		void SetShoulder(float newTarget)
		{
			if (_preview.Runtime != null)
				_preview.Runtime.WaitRemaining = AngleMotionTicks(_preview.ShoulderAngle, newTarget);
			_preview.TargetShoulderAngle = newTarget;
		}
		void SetElbow(float newTarget)
		{
			if (_preview.Runtime != null)
				_preview.Runtime.WaitRemaining = AngleMotionTicks(_preview.ElbowAngle, newTarget);
			_preview.TargetElbowAngle = newTarget;
		}

		switch (b.Type)
		{
			// Set: wait until motion completes (synchronizing destination block)
			case BlockType.TurretSetAngle:
			case BlockType.ArmSetShoulder:  SetShoulder(Mathf.DegToRad(b.ValueF)); break;
			case BlockType.ArmSetElbow:     SetElbow(Mathf.DegToRad(b.ValueF)); break;

			// Add: fire-and-forget delta (no wait, target accumulates while motion catches up)
			case BlockType.TurretAddAngle:
			case BlockType.ArmAddShoulder:  _preview.TargetShoulderAngle += Mathf.DegToRad(b.ValueF); break;
			case BlockType.ArmAddElbow:     _preview.TargetElbowAngle    += Mathf.DegToRad(b.ValueF); break;

			case BlockType.ArmOpenClaw:     _preview.ClawClosed = false; break;
			case BlockType.ArmCloseClaw:    _preview.ClawClosed = true;  break;
			case BlockType.TurretLaserOn:   _preview.LaserOn    = true;  break;
			case BlockType.TurretLaserOff:  _preview.LaserOn    = false; break;
			case BlockType.TrackSetPosition:
			{
				float newTarget = Math.Clamp(b.ValueF, 0f, 1f);
				if (_preview.Runtime != null)
					_preview.Runtime.WaitRemaining = TrackMotionTicks(_preview.TrackT, newTarget);
				_preview.TargetTrackT = newTarget;
				break;
			}
			case BlockType.TrackAddPosition:
				_preview.TargetTrackT = Math.Clamp(_preview.TargetTrackT + b.ValueF, 0f, 1f);
				break;
		}
	}

	private const float PreviewScale  = 3f;
	private const float TrackMarginPx = 20f; // screen-px inset from canvas edges for the preview track

	private static bool ScriptHasTrackBlocks(MachineScript script) =>
		BlockListHasTrack(script.Blocks);

	private static bool BlockListHasTrack(List<ScriptBlock> blocks)
	{
		foreach (var b in blocks)
		{
			if (b.Type == BlockType.TrackSetPosition || b.Type == BlockType.TrackAddPosition) return true;
			if (b.Body != null && BlockListHasTrack(b.Body)) return true;
		}
		return false;
	}

	private static void DrawPreviewTrack(OverlayCanvas c, float left, float right, float y)
	{
		var bedCol  = new Color(0.22f, 0.22f, 0.25f);
		var tieCol  = new Color(0.35f, 0.25f, 0.15f);
		var railCol = new Color(0.78f, 0.78f, 0.82f);

		c.DrawLine(new Vector2(left, y), new Vector2(right, y), bedCol, 7f);

		float tieInterval = 14f;
		for (float x = left; x <= right + 0.5f; x += tieInterval)
			c.DrawLine(new Vector2(x, y - 5f), new Vector2(x, y + 5f), tieCol, 4f);

		c.DrawLine(new Vector2(left, y - 4f), new Vector2(right, y - 4f), railCol, 2.5f);
		c.DrawLine(new Vector2(left, y + 4f), new Vector2(right, y + 4f), railCol, 2.5f);

		c.DrawCircle(new Vector2(left,  y), 4.5f, railCol);
		c.DrawCircle(new Vector2(right, y), 4.5f, railCol);
	}

	private void DrawPreview(OverlayCanvas c)
	{
		var size = c.Size;
		// Backdrop
		c.DrawRect(new Rect2(0, 0, size.X, size.Y), new Color(0.06f, 0.06f, 0.10f), true);
		c.DrawRect(new Rect2(0, 0, size.X, size.Y), new Color(0.30f, 0.30f, 0.40f), false, 1f);

		if (_editingScript == null) return;

		if (_editingScript.MachineType == ScriptMachineType.Turret) DrawPreviewTurret(c, size);
		else                                                        DrawPreviewArm(c, size);
	}

	private void DrawPreviewTurret(OverlayCanvas c, Vector2 canvasSize)
	{
		float   s      = PreviewScale;
		bool    onTrack = _editingScript != null && ScriptHasTrackBlocks(_editingScript);
		float   trackY = canvasSize.Y / 2f;
		float   trackL = TrackMarginPx, trackR = canvasSize.X - TrackMarginPx;

		if (onTrack) DrawPreviewTrack(c, trackL, trackR, trackY);

		Vector2 center = onTrack
			? new Vector2(Mathf.Lerp(trackL, trackR, _preview.TrackT), trackY)
			: new Vector2(canvasSize.X / 2f, canvasSize.Y / 2f);

		// 5×3 stone base centred on (Origin.X, Origin.Y+1) — turret pivot is at +1 row
		var stoneCol  = new Color(0.45f, 0.45f, 0.50f);
		var copperCol = new Color(0.86f, 0.48f, 0.18f);

		c.DrawRect(new Rect2(center.X - 2.5f * s, center.Y - 1.5f * s, 5 * s, 3 * s), stoneCol, true);
		c.DrawRect(new Rect2(center.X - 3.5f * s, center.Y - 0.5f * s,     s,     s), copperCol, true);
		c.DrawRect(new Rect2(center.X + 2.5f * s, center.Y - 0.5f * s,     s,     s), copperCol, true);

		// Barrel
		float   angle    = _preview.ShoulderAngle;
		Vector2 dir      = new(MathF.Cos(angle), MathF.Sin(angle));
		Vector2 barrelTo = center + dir * 4.5f * s;
		c.DrawLine(center, barrelTo, new Color(0.12f, 0.12f, 0.12f), 3f);
		c.DrawCircle(center, 2.5f, Colors.White);

		// Laser beam — preview machine is always "powered" so we render the beam
		// whenever the script wants the laser on. Scripts can toggle LaserOn/Off.
		if (_preview.LaserOn)
		{
			Vector2 beamEnd = barrelTo + dir * canvasSize.Length();
			c.DrawLine(barrelTo, beamEnd, new Color(1f, 0.20f, 0.0f,  0.20f), 6f);
			c.DrawLine(barrelTo, beamEnd, new Color(1f, 0.45f, 0.1f,  0.85f), 2.5f);
			c.DrawLine(barrelTo, beamEnd, new Color(1f, 0.90f, 0.85f, 0.95f), 1f);
		}
	}

	private void DrawPreviewArm(OverlayCanvas c, Vector2 canvasSize)
	{
		float   s       = PreviewScale;
		bool    onTrack = _editingScript != null && ScriptHasTrackBlocks(_editingScript);
		float   baseY   = canvasSize.Y - 30;
		float   trackL  = TrackMarginPx, trackR = canvasSize.X - TrackMarginPx;

		if (onTrack) DrawPreviewTrack(c, trackL, trackR, baseY);

		Vector2 baseCenter = onTrack
			? new Vector2(Mathf.Lerp(trackL, trackR, _preview.TrackT), baseY)
			: new Vector2(canvasSize.X / 2f, baseY);

		var stoneCol  = new Color(0.45f, 0.45f, 0.50f);
		var copperCol = new Color(0.86f, 0.48f, 0.18f);
		var bodyCol   = new Color(0.62f, 0.66f, 0.72f);
		var jointCol  = new Color(0.80f, 0.85f, 0.90f);

		c.DrawRect(new Rect2(baseCenter.X - 1.5f * s, baseCenter.Y - 1.5f * s, 3 * s, 3 * s), stoneCol, true);
		c.DrawRect(new Rect2(baseCenter.X - 2.5f * s, baseCenter.Y - 0.5f * s,     s,     s), copperCol, true);
		c.DrawRect(new Rect2(baseCenter.X + 1.5f * s, baseCenter.Y - 0.5f * s,     s,     s), copperCol, true);

		Vector2 shoulder = baseCenter;
		Vector2 elbow    = shoulder + new Vector2(MathF.Cos(_preview.ShoulderAngle), MathF.Sin(_preview.ShoulderAngle)) * 12f * s;
		Vector2 claw     = elbow    + new Vector2(MathF.Cos(_preview.ElbowAngle),    MathF.Sin(_preview.ElbowAngle))    * 12f * s;

		c.DrawLine(shoulder, elbow, bodyCol, 3f);
		c.DrawLine(elbow,    claw,  bodyCol, 3f);
		c.DrawCircle(shoulder, 3.5f, jointCol);
		c.DrawCircle(elbow,    3.0f, jointCol);

		// Pincer indicator — short bars perpendicular to the forearm
		Vector2 perp   = new Vector2(-MathF.Sin(_preview.ElbowAngle), MathF.Cos(_preview.ElbowAngle));
		float   spread = _preview.ClawClosed ? 0.5f : (_pincerHalfWidth + 0.5f);
		c.DrawLine(claw, claw + perp * spread * s, jointCol, 2.5f);
		c.DrawLine(claw, claw - perp * spread * s, jointCol, 2.5f);
		// When closed, draw a small filled square at the tip to show it's holding something
		if (_preview.ClawClosed)
			c.DrawRect(new Rect2(claw.X - 2f, claw.Y - 2f, 4f, 4f), Colors.White, true);
	}

	// ── Persistence ────────────────────────────────────────────────────────────

	private void AutoSaveScripts()
	{
		var cfg = new ConfigFile();
		cfg.Load(CfgPath);
		SaveScripts(cfg);
		cfg.Save(CfgPath);
	}

	public void SaveScripts(ConfigFile cfg)
	{
		cfg.SetValue("Scripts", "count", _scripts.Count);
		for (int i = 0; i < _scripts.Count; i++)
		{
			var section = $"Script_{i}";
			cfg.SetValue(section, "name",      _scripts[i].Name);
			cfg.SetValue(section, "type",      (int)_scripts[i].MachineType);
			cfg.SetValue(section, "rateTicks", _scripts[i].RateTicks);
			cfg.SetValue(section, "blocks",    SerializeBlocks(_scripts[i].Blocks));
		}
	}

	public void LoadScripts(ConfigFile cfg)
	{
		int count = (int)(long)cfg.GetValue("Scripts", "count", 0);
		_scripts.Clear();
		for (int i = 0; i < count; i++)
		{
			var section = $"Script_{i}";
			var s = new MachineScript
			{
				Name        = (string)cfg.GetValue(section, "name", $"Script {i + 1}"),
				MachineType = (ScriptMachineType)(int)(long)cfg.GetValue(section, "type", 0),
				RateTicks   = (int)(long)cfg.GetValue(section, "rateTicks", 30),
			};
			var blockData = (string)cfg.GetValue(section, "blocks", "");
			DeserializeBlocks(blockData, s.Blocks);
			_scripts.Add(s);
		}
		RefreshScriptsList();
	}

	// Compact block serialization: each block is "T|F|I|<body>" semicolon-separated.
	// Body uses '{' .. '}' as delimiters.
	private static string SerializeBlocks(List<ScriptBlock> blocks)
	{
		if (blocks == null || blocks.Count == 0) return "";
		var sb = new System.Text.StringBuilder();
		foreach (var b in blocks)
		{
			sb.Append((int)b.Type).Append('|').Append(b.ValueF.ToString("R")).Append('|').Append(b.ValueI);
			if (b.Body != null) { sb.Append('{').Append(SerializeBlocks(b.Body)).Append('}'); }
			sb.Append(';');
		}
		return sb.ToString();
	}

	private static void DeserializeBlocks(string data, List<ScriptBlock> outList)
	{
		if (string.IsNullOrEmpty(data)) return;
		int i = 0;
		while (i < data.Length)
		{
			// Read type
			int p = data.IndexOf('|', i);
			if (p < 0) break;
			int type = int.Parse(data.AsSpan(i, p - i));
			i = p + 1;
			// Read ValueF
			p = data.IndexOf('|', i);
			if (p < 0) break;
			float vf = float.Parse(data.AsSpan(i, p - i), System.Globalization.CultureInfo.InvariantCulture);
			i = p + 1;
			// Read ValueI (until '{' or ';')
			int term = i;
			while (term < data.Length && data[term] != '{' && data[term] != ';') term++;
			int vi = int.Parse(data.AsSpan(i, term - i));
			i = term;

			var b = new ScriptBlock { Type = (BlockType)type, ValueF = vf, ValueI = vi };
			if (i < data.Length && data[i] == '{')
			{
				// Find matching '}'
				int depth = 1, j = i + 1;
				while (j < data.Length && depth > 0)
				{
					if (data[j] == '{') depth++;
					else if (data[j] == '}') depth--;
					if (depth > 0) j++;
				}
				b.Body = new List<ScriptBlock>();
				DeserializeBlocks(data.Substring(i + 1, j - i - 1), b.Body);
				i = j + 1;
			}
			outList.Add(b);
			if (i < data.Length && data[i] == ';') i++;
		}
	}
}

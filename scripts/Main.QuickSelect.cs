using System;
using System.Collections.Generic;
using Godot;

// Quick-select radial menu and profile management.
// Z+drag opens a 6-slice radial; hold LMB+RMB and drag to select a slot.
// Profiles are configured in the toolbar's "Quick" tab.
public partial class Main
{
	// ── Constants ─────────────────────────────────────────────────────────────

	private const float RadialRadius   = 95f;   // screen-space radius of the full radial
	private const float RadialDeadZone = 26f;   // center dead-zone radius (no selection)
	private const int   MaxProfiles    = 6;
	private const int   SlotsPerProfile = 6;

	// All brush values available for assignment to a quick-select slot
	private static readonly int[] QuickSelectOptions = {
		BrushSand, BrushWater, BrushStone, BrushLava, BrushGas, BrushFood,
		BrushCopper, BrushBattery, BrushWood, BrushErase, BrushForce,
		BrushDirt, BrushGrassSeed, BrushTreeSeed, BrushFire, BrushLiquidNitrogen,
		BrushGlorp, BrushTurret, BrushMirror, BrushArm,
	};

	// ── Data ──────────────────────────────────────────────────────────────────

	private sealed class QuickSelectProfile
	{
		public string Name  = "Profile";
		public int[]  Slots = new int[SlotsPerProfile];

		public QuickSelectProfile Clone() => new() { Name = Name, Slots = (int[])Slots.Clone() };
	}

	private readonly List<QuickSelectProfile> _quickProfiles = new();
	private int  _activeProfileIdx = 0;
	private int  _editingSlotIdx   = -1;  // slot selected for material reassignment

	// Toolbar node refs (populated in InitQuickSelect)
	private HBoxContainer _qsProfileRow;
	private HBoxContainer _qsProfileActions;
	private HBoxContainer _qsSlotRow;
	private Label         _qsAssignHint;
	private GridContainer _qsMaterialPicker;

	// ── Init ──────────────────────────────────────────────────────────────────

	private void InitQuickSelect()
	{
		// Default profiles
		_quickProfiles.Add(new QuickSelectProfile
		{
			Name  = "General",
			Slots = new[] { BrushSand, BrushWater, BrushStone, BrushFire, BrushLiquidNitrogen, BrushErase }
		});
		_quickProfiles.Add(new QuickSelectProfile
		{
			Name  = "Electric",
			Slots = new[] { BrushCopper, BrushBattery, BrushTurret, BrushArm, BrushStone, BrushErase }
		});
		_quickProfiles.Add(new QuickSelectProfile
		{
			Name  = "Nature",
			Slots = new[] { BrushDirt, BrushGrassSeed, BrushTreeSeed, BrushWater, BrushFire, BrushErase }
		});

		const string qp = "UI/ToolBox/Panel/VBoxContainer/QuickSelectPage/";
		_qsProfileRow     = GetNode<HBoxContainer>(qp + "ProfileRow");
		_qsProfileActions = GetNode<HBoxContainer>(qp + "ProfileActions");
		_qsSlotRow        = GetNode<HBoxContainer>(qp + "SlotRow");
		_qsAssignHint     = GetNode<Label>(qp + "AssignHint");
		_qsMaterialPicker = GetNode<GridContainer>(qp + "MaterialPicker");

		BuildQSProfileActions();
		BuildQSMaterialPicker();
		RefreshQSProfiles();
		RefreshQSSlots();
	}

	// Creates the "New Profile" and "Delete" action buttons (built once)
	private void BuildQSProfileActions()
	{
		var newBtn = new Button { Text = "+ New", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		newBtn.Pressed += () =>
		{
			if (_quickProfiles.Count >= MaxProfiles) return;
			var clone = _quickProfiles[_activeProfileIdx].Clone();
			clone.Name = "Profile " + (_quickProfiles.Count + 1);
			_quickProfiles.Add(clone);
			_activeProfileIdx = _quickProfiles.Count - 1;
			RefreshQSProfiles();
			RefreshQSSlots();
			AutoSaveProfiles();
		};

		var delBtn = new Button { Text = "Delete", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		delBtn.Pressed += () =>
		{
			if (_quickProfiles.Count <= 1) return;
			_quickProfiles.RemoveAt(_activeProfileIdx);
			_activeProfileIdx = Math.Clamp(_activeProfileIdx, 0, _quickProfiles.Count - 1);
			RefreshQSProfiles();
			RefreshQSSlots();
			AutoSaveProfiles();
		};

		var nameEdit = new LineEdit { PlaceholderText = "Profile name…", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		nameEdit.TextChanged += text =>
		{
			if (_quickProfiles.Count > 0)
			{
				_qsEditingName = true;
				_quickProfiles[_activeProfileIdx].Name = text;
				RefreshQSProfiles();
				_qsEditingName = false;
				AutoSaveProfiles();
			}
		};
		// Store reference so RefreshQSProfiles can update it when active profile changes
		_qsNameEdit = nameEdit;

		_qsProfileActions.AddChild(newBtn);
		_qsProfileActions.AddChild(delBtn);
		_qsProfileActions.AddChild(nameEdit);
	}

	private LineEdit _qsNameEdit;
	private bool     _qsEditingName; // true while TextChanged is on the call stack

	// Rebuilds the profile button row to match current profile list
	private void RefreshQSProfiles()
	{
		foreach (Node child in _qsProfileRow.GetChildren())
			child.QueueFree();

		for (int i = 0; i < _quickProfiles.Count; i++)
		{
			int idx = i;
			var btn = new Button
			{
				Text                  = _quickProfiles[i].Name,
				SizeFlagsHorizontal   = Control.SizeFlags.ExpandFill,
				Modulate              = i == _activeProfileIdx ? Colors.Yellow : Colors.White,
			};
			btn.Pressed += () =>
			{
				_activeProfileIdx = idx;
				_editingSlotIdx   = -1;
				RefreshQSProfiles();
				RefreshQSSlots();
			};
			_qsProfileRow.AddChild(btn);
		}

		// Sync the name field only when switching profiles, not while the user is typing
		// (setting .Text resets the caret to 0, which would reverse typed characters)
		if (_qsNameEdit != null && _quickProfiles.Count > 0 && !_qsEditingName)
			_qsNameEdit.Text = _quickProfiles[_activeProfileIdx].Name;
	}

	// Rebuilds the 6 slot buttons to reflect the active profile's assignments
	private void RefreshQSSlots()
	{
		foreach (Node child in _qsSlotRow.GetChildren())
			child.QueueFree();

		var profile = _quickProfiles[_activeProfileIdx];
		for (int i = 0; i < SlotsPerProfile; i++)
		{
			int slotIdx = i;
			int brush   = profile.Slots[i];
			var col     = BrushDisplayColor(brush);
			var btn = new Button
			{
				Text                = BrushName(brush),
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
				// Tint slot button with the material's color;
				// selected slot gets a bright yellow outline via Modulate override
				Modulate            = slotIdx == _editingSlotIdx ? Colors.Yellow : Colors.White,
				TooltipText         = $"Slot {slotIdx + 1}: {BrushName(brush)}  — click to reassign",
			};
			// Background color hint via self-modulate on button
			btn.AddThemeColorOverride("font_color", col.Lightened(0.3f));
			btn.Pressed += () =>
			{
				_editingSlotIdx = (_editingSlotIdx == slotIdx) ? -1 : slotIdx;
				RefreshQSSlots();
				_qsAssignHint.Visible = _editingSlotIdx >= 0;
			};
			_qsSlotRow.AddChild(btn);
		}
		_qsAssignHint.Visible = _editingSlotIdx >= 0;
	}

	// Builds the material picker grid once; buttons assign to the selected slot
	private void BuildQSMaterialPicker()
	{
		foreach (int brush in QuickSelectOptions)
		{
			int b   = brush;
			var col = BrushDisplayColor(brush);
			var btn = new Button
			{
				Text                = BrushName(brush),
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			};
			btn.AddThemeColorOverride("font_color", col.Lightened(0.3f));
			btn.Pressed += () =>
			{
				if (_editingSlotIdx < 0) return;
				_quickProfiles[_activeProfileIdx].Slots[_editingSlotIdx] = b;
				_editingSlotIdx = -1;
				RefreshQSSlots();
				AutoSaveProfiles();
			};
			_qsMaterialPicker.AddChild(btn);
		}
	}

	// ── Radial open / close ───────────────────────────────────────────────────

	private void OpenRadial(Vector2 screenPos)
	{
		// Nudge away from screen edges so all slices remain visible
		float margin = RadialRadius + 12f;
		var   vpSize = GetViewport().GetVisibleRect().Size;
		_radialOriginScreen = new Vector2(
			Math.Clamp(screenPos.X, margin, vpSize.X - margin),
			Math.Clamp(screenPos.Y, margin, vpSize.Y - margin));
		_radialOpen        = true;
		_radialHoveredSlot = -1;
		_sim.RenderDirty   = true;
	}

	private void CloseRadial()
	{
		if (_radialHoveredSlot >= 0 && _activeProfileIdx < _quickProfiles.Count)
		{
			int brush = _quickProfiles[_activeProfileIdx].Slots[_radialHoveredSlot];
			SetBrush(brush);
		}
		_radialOpen          = false;
		_radialHoveredSlot   = -1;
		_radialInputCooldown = 0.18; // brief window so the releasing button doesn't immediately paint
		_sim.RenderDirty     = true;
	}

	private void UpdateRadialHover(Vector2 screenMouse)
	{
		float dist = _radialOriginScreen.DistanceTo(screenMouse);
		if (dist < RadialDeadZone)
		{
			_radialHoveredSlot = -1;
			return;
		}
		float angle    = MathF.Atan2(screenMouse.Y - _radialOriginScreen.Y,
		                              screenMouse.X - _radialOriginScreen.X);
		// Offset so slice 0 starts at the top (-90°)
		float adjusted = ((angle + MathF.PI / 2f) + MathF.PI * 2f) % (MathF.PI * 2f);
		_radialHoveredSlot = (int)(adjusted / (MathF.PI / 3f)) % SlotsPerProfile;
	}

	// ── Radial drawing ────────────────────────────────────────────────────────

	private void DrawRadial(OverlayCanvas c)
	{
		if (_activeProfileIdx >= _quickProfiles.Count) return;
		var     profile   = _quickProfiles[_activeProfileIdx];
		Vector2 center    = _radialOriginScreen;
		float   startOff  = -MathF.PI / 2f;   // slice 0 opens upward
		const int arcSteps = 6;               // low step count for pixelated arc edges

		// Dark background disc
		c.DrawCircle(center, RadialRadius + 8f, new Color(0f, 0f, 0f, 0.60f));

		// Filled wedge for each slot
		for (int i = 0; i < SlotsPerProfile; i++)
		{
			float a0      = startOff + i * MathF.PI / 3f;
			float a1      = a0 + MathF.PI / 3f;
			bool  hovered = i == _radialHoveredSlot;

			var baseCol = BrushDisplayColor(profile.Slots[i]);
			var fillCol = hovered ? baseCol.Lightened(0.4f) : baseCol.Darkened(0.30f);
			fillCol.A   = hovered ? 1.0f : 0.82f;

			// Fan polygon: center point + arc points snapped to 4px grid
			var pts  = new Vector2[arcSteps + 2];
			pts[0]   = center;
			for (int k = 0; k <= arcSteps; k++)
			{
				float a = Mathf.Lerp(a0, a1, k / (float)arcSteps);
				pts[k + 1] = new Vector2(
					MathF.Round((center.X + MathF.Cos(a) * RadialRadius) / Scale) * Scale,
					MathF.Round((center.Y + MathF.Sin(a) * RadialRadius) / Scale) * Scale);
			}
			c.DrawColoredPolygon(pts, fillCol);
		}

		// White pixelated spokes (endpoints snapped to 4px grid)
		for (int i = 0; i < SlotsPerProfile; i++)
		{
			float a = startOff + i * MathF.PI / 3f;
			var end = new Vector2(
				MathF.Round((center.X + MathF.Cos(a) * RadialRadius) / Scale) * Scale,
				MathF.Round((center.Y + MathF.Sin(a) * RadialRadius) / Scale) * Scale);
			c.DrawLine(center, end, Colors.White, 2f);
		}

		// Material name labels — drawn at the midpoint of each arc
		var font = ThemeDB.FallbackFont;
		for (int i = 0; i < SlotsPerProfile; i++)
		{
			float midAngle  = startOff + (i + 0.5f) * MathF.PI / 3f;
			float labelDist = RadialRadius * 0.62f;
			var   labelPos  = center + new Vector2(MathF.Cos(midAngle), MathF.Sin(midAngle)) * labelDist;
			string name     = BrushName(profile.Slots[i]);
			bool hovered    = i == _radialHoveredSlot;
			// Center text on the label position (DrawString pos is baseline-left)
			var textSize = font.GetStringSize(name, fontSize: 10);
			var drawPos  = labelPos - new Vector2(textSize.X * 0.5f, -textSize.Y * 0.25f);
			c.DrawString(font, drawPos, name, fontSize: 10,
				modulate: hovered ? Colors.White : new Color(0.9f, 0.9f, 0.9f, 0.85f));
		}

		// Center disc — dark fill with pixelated ring outline
		c.DrawCircle(center, RadialDeadZone, new Color(0.06f, 0.06f, 0.10f, 0.92f));
		c.DrawArc(center, RadialDeadZone, 0f, MathF.PI * 2f, 8, Colors.White, 2f);

		// Profile name in the very center
		if (_quickProfiles.Count > 0)
		{
			string pname    = _quickProfiles[_activeProfileIdx].Name;
			var    nameSize = font.GetStringSize(pname, fontSize: 9);
			c.DrawString(font, center - new Vector2(nameSize.X * 0.5f, -nameSize.Y * 0.25f),
				pname, fontSize: 9, modulate: new Color(0.75f, 0.75f, 0.75f, 1f));
		}
	}

	// ── Helpers ───────────────────────────────────────────────────────────────

	private Color BrushDisplayColor(int brush)
	{
		if (brush >= 0)
		{
			CellColor((byte)brush, 128, out byte r, out byte g, out byte b);
			return new Color(r / 255f, g / 255f, b / 255f);
		}
		return brush switch
		{
			BrushErase  => new Color(0.80f, 0.25f, 0.25f),
			BrushForce  => new Color(0.45f, 0.55f, 0.90f),
			BrushGlorp  => new Color(0.65f, 0.35f, 0.90f),
			BrushTurret => new Color(0.35f, 0.35f, 0.55f),
			BrushMirror => new Color(0.55f, 0.80f, 0.90f),
			BrushArm    => new Color(0.60f, 0.65f, 0.72f),
			_           => Colors.Gray,
		};
	}

	private static string BrushName(int brush) => brush switch
	{
		BrushSand           => "Sand",
		BrushWater          => "Water",
		BrushStone          => "Stone",
		BrushLava           => "Lava",
		BrushGas            => "Gas",
		BrushFood           => "Food",
		BrushGlorp          => "Glorp",
		BrushCopper         => "Copper",
		BrushBattery        => "Battery",
		BrushWood           => "Wood",
		BrushErase          => "Erase",
		BrushForce          => "Force",
		BrushTurret         => "Turret",
		BrushMirror         => "Mirror",
		BrushDirt           => "Dirt",
		BrushGrassSeed      => "Grass",
		BrushTreeSeed       => "Tree Seed",
		BrushFire           => "Fire",
		BrushLiquidNitrogen => "Liq. N2",
		BrushArm            => "Arm",
		_                   => "?",
	};

	// Applies material-matched font colors to every button in the Materials tab grid.
	// Called once from _Ready after all buttons are resolved.
	private void ApplyMaterialButtonColors()
	{
		void Tint(Button btn, int brush)
		{
			var c = BrushDisplayColor(brush).Lightened(0.25f);
			btn.AddThemeColorOverride("font_color",         c);
			btn.AddThemeColorOverride("font_color_hover",   c.Lightened(0.2f));
			btn.AddThemeColorOverride("font_color_pressed", Colors.White);
		}

		Tint(_btnSand,           BrushSand);
		Tint(_btnWater,          BrushWater);
		Tint(_btnStone,          BrushStone);
		Tint(_btnLava,           BrushLava);
		Tint(_btnGas,            BrushGas);
		Tint(_btnFood,           BrushFood);
		Tint(_btnGlorp,          BrushGlorp);
		Tint(_btnCopper,         BrushCopper);
		Tint(_btnBattery,        BrushBattery);
		Tint(_btnWood,           BrushWood);
		Tint(_btnErase,          BrushErase);
		Tint(_btnForce,          BrushForce);
		Tint(_btnTurret,         BrushTurret);
		Tint(_btnMirror,         BrushMirror);
		Tint(_btnDirt,           BrushDirt);
		Tint(_btnGrassSeed,      BrushGrassSeed);
		Tint(_btnTreeSeed,       BrushTreeSeed);
		Tint(_btnFire,           BrushFire);
		Tint(_btnLiquidNitrogen, BrushLiquidNitrogen);
		Tint(_btnArm,            BrushArm);
	}

	// ── Persistence ───────────────────────────────────────────────────────────

	// Writes only the quick-select sections to disk immediately.
	// Loads the existing cfg first so no other settings are clobbered.
	private void AutoSaveProfiles()
	{
		var cfg = new ConfigFile();
		cfg.Load(CfgPath); // ignore error — file may not exist yet
		SaveQuickProfiles(cfg);
		cfg.Save(CfgPath);
	}

	public void SaveQuickProfiles(ConfigFile cfg)
	{
		cfg.SetValue("QuickSelect", "profileCount",   _quickProfiles.Count);
		cfg.SetValue("QuickSelect", "activeProfile",  _activeProfileIdx);
		for (int i = 0; i < _quickProfiles.Count; i++)
		{
			var p = _quickProfiles[i];
			cfg.SetValue($"QSProfile{i}", "name",  p.Name);
			cfg.SetValue($"QSProfile{i}", "slots", p.Slots);
		}
	}

	public void LoadQuickProfiles(ConfigFile cfg)
	{
		int count = (int)(long)cfg.GetValue("QuickSelect", "profileCount", _quickProfiles.Count);
		if (count <= 0) return;

		_quickProfiles.Clear();
		for (int i = 0; i < Math.Min(count, MaxProfiles); i++)
		{
			string section = $"QSProfile{i}";
			var p = new QuickSelectProfile
			{
				Name  = (string)cfg.GetValue(section, "name", $"Profile {i + 1}"),
				Slots = Array.ConvertAll(
					(int[])cfg.GetValue(section, "slots",
						new[] { BrushSand, BrushWater, BrushStone, BrushFire, BrushLiquidNitrogen, BrushErase }),
					s => s),
			};
			_quickProfiles.Add(p);
		}
		_activeProfileIdx = Math.Clamp(
			(int)(long)cfg.GetValue("QuickSelect", "activeProfile", 0), 0, _quickProfiles.Count - 1);

		RefreshQSProfiles();
		RefreshQSSlots();
	}
}

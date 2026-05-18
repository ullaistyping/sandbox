# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A pixel-art falling-sand sandbox simulation built in **C# with Godot 4.6** (Forward Plus / D3D12). Despite the project using Jolt Physics for 3D, all simulation physics is hand-rolled in C#.

- **Language:** C# (not GDScript — ignore the GDScript conventions section below for this codebase)
- **Grid:** 320×180 cells, displayed at 4× scale (1280×720 screen)
- **Simulation rate:** configurable TPS (default 30), separate from render rate

## Running

```
godot --path U:/code/Godot-games/sandbox
```

Console (`` ` `` key): `tps <n>`, `brush <n>`, `clear`, `boil <n>`, `gasthresh <n>`

## Architecture

### Core files

| File | Role |
|---|---|
| `scripts/Simulation.cs` | Cell physics engine — all element update rules, velocity, electricity |
| `scripts/Main.cs` | Wood rigid bodies, rendering, input, UI, Glorp spawning |
| `scripts/Glorp.cs` | AI creature — needs, pathfinding, physics, rendering |
| `scripts/OverlayCanvas.cs` | Thin overlay node for debug/preview drawing on top of the sim texture |
| `scenes/main.tscn` | Single scene; root is Control → TextureRect + UI tree |

### Simulation.cs — cell grid

`Simulation` owns parallel flat arrays indexed `y * SimW + x`:

- `Grid[]` (byte) — cell type enum
- `Flow[]` (byte) — dual purpose: water flow direction OR copper heat (0–255)
- `Electric[]` (byte) — electricity propagation
- `VelX[], VelY[]` (float) — velocity for loose particles (sand, water, lava, gas, steam)
- `Pinned[]` (byte) — locked cells (pin tool)

Cell types (11): Air, Sand, Water, Stone, Lava, Gas, Food, Copper, Steam, Battery, Wood.

Wood cells are **not** updated by `Simulation.Update()` — they are owned and moved entirely by `Main.cs`.

### Main.cs — wood physics (`WoodPiece` class)

Wood is handled as **macro-level rigid bodies** outside the per-pixel simulation. Each `WoodPiece` stores:

- `LocalCells` — unrotated cell offsets from centre of mass (CoM)
- `GridCells` — current world positions (updated each tick)
- `Position` (float) — CoM in sim-space
- `Angle` — rotation in radians
- `VelY, SubY` — vertical velocity + sub-pixel accumulator
- `AngVel` — angular velocity (rad/tick)

**Rotation pipeline (per tick, `UpdateWoodPieces`):**
1. `ProjectCells()` rotates `LocalCells` through `(Position, Angle)` → world integer coords
2. `CanOccupy()` checks the projected footprint against the grid (air/steam only, respects pins)
3. `StampGrid()` clears old cells, stamps new positions as Wood
4. Landing torque: bottom-edge cells below a solid compute average X offset from CoM → `AngVel += offset * VelY * LandAngFactor`
5. Angular damping: `AngVel *= AngDamp` each tick

**Current known physics problems (Crayon Physics goal):**

The rotation system has been attempted but does not yet work robustly. Specific failure modes:
- Pieces placed flat rarely gain angular velocity from landing
- Rotation can become stuck or oscillate without settling
- No horizontal (X-axis) velocity — pieces only fall vertically; they cannot slide sideways after landing, which prevents natural tipping
- The `LandAngFactor` (0.0018) and the integer-step Y movement interact poorly — the `VelY` at the moment of collision is clamped before the torque impulse fires, weakening it significantly

**What a robust solution needs:**
- Separate `VelX` on `WoodPiece` (pieces must be able to slide sideways)
- Torque from rotational inertia (moment of inertia based on piece shape, not just contact offset × velocity)
- Possibly sub-pixel X movement (same accumulator pattern as Y)
- Collision response that also reflects horizontal momentum
- Resting detection that truly zeroes `AngVel` when settled on flat ground, rather than relying solely on damping

### Main.cs — rendering

`Render()` builds a raw RGBA8 byte array each frame and pushes it to an `ImageTexture`. The `Flow` byte drives color variation (water direction tint, stone/lava grain, copper heat gradient, wood grain hash). Gas and Steam are alpha-blended with the air color inline.

### Glorp.cs — AI creatures

Glorps live at `SimPos` (float sim-space coords), not grid cells. They have independent physics (gravity, friction, bounce, step-up) and a needs system (hunger, thirst, social) that drives goal-seeking AI. They render as 12×12 pixel textures drawn via `DrawTexture` on the `OverlayCanvas`.

## Key constants to tune (all in `UpdateWoodPieces`)

```csharp
const float GravPerTick   = 0.35f;
const float SteamLift     = 0.60f;   // per steam cell below bottom edge
const float MaxFall       = 8f;
const float MaxRise       = 3f;
const float AngDamp       = 0.88f;   // angular velocity decay per tick
const float MaxAngVel     = 0.055f;  // ~3° per tick cap
const float LandAngFactor = 0.0018f; // contact offset → angular impulse
```

## GDScript Conventions

- Use `class_name` declarations for reusable scripts
- Prefer `@export` variables over hard-coded values for tunable parameters
- Use `@onready` for node references rather than storing paths as strings
- Signal connections should be made in `_ready()` using `signal.connect()`

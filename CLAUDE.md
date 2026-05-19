# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## North star

**Interesting interactions between materials.** Everything in this sandbox is justified by the reactions it enables. When adding or tuning a material, the question is never "does it look right" — it's "what new behaviours does this unlock when it touches the materials that already exist?" A new cell type with no reactions to existing cells is dead weight.

The core loop the player is meant to discover:
- **Heat as a circulatory system.** Lava and LN2 are sources; copper is the wire. Heat flows through connected copper as a distance field, which lets the player build kettles, freezers, gas-detonators, and ice machines out of geometry alone.
- **Electricity as a parallel network.** Batteries push current through copper. The same wire that carries heat also powers laser turrets — wiring a heater also wires a weapon.
- **Fire as a state-spreading reaction.** Fire ignites flammables, makes smoke, boils water → steam, is killed by water/LN2 → steam/N2 gas. Burnable structures (trees, grown from seeds on dirt) make the world fuel-rich.
- **Phase changes everywhere.** Water ↔ Ice ↔ Steam, LN2 ↔ N2 gas, Gas → explosion when ignited by lava or hot copper. Phase boundaries are where the interesting interactions happen.
- **Mirrors + turrets** turn the laser into a puzzle element — turret needs power (copper + battery), beam reflects off mirrors, the player aims it like a thrown line.

When evaluating a proposed feature, ask: does it create at least one new edge in the material interaction graph? If not, push back.

## Running and building

This is a **Godot 4.6.2 + .NET 8** project. There is no separate build/test step — Godot drives the C# compile.

- **Run the game**: open `project.godot` in Godot 4.6, press F5. Main scene is `scenes/main.tscn`.
- **Build only**: `dotnet build Sandbox.sln` (or let the editor do it on save).
- **No test suite.** Behaviour is verified by playing the sim.

There are no Cursor rules, Copilot instructions, or README.

## Architecture

### Two coupled layers

1. **Per-cell simulation** (`scripts/Simulation.cs`) — a `320×180` grid of `Cell` enums plus parallel arrays (`Flow`, `Electric`, `Pinned`, `VelX`, `VelY`). One `Update()` runs every tick.
2. **Macro objects** that live on top of the grid: `Glorp` creatures (`scripts/Glorp.cs`), laser turrets (nested class in `Main.cs`), and the unfinished `WoodPiece` rigid body system.

`Main.cs` owns rendering, input, the brush system, the console, turret/glorp/shockwave lists, and the per-frame loop that calls `_sim.Update()` at `_ticksPerSecond`. `Simulation` is a plain `RefCounted` — it knows nothing about Godot rendering.

### The parallel-array convention

Every per-cell datum is stored as a flat `byte[]` or `float[]` of length `SimW * SimH`, indexed by `y * SimW + x`. `Flow` is overloaded by cell type — for water it's temperature (0=ice, 128=room, 255=boiling), for copper it's also temperature on the same scale, for fire/smoke it's lifetime ticks, for stone/dirt it's a colour-jitter seed. Always check `Grid[i]` before interpreting `Flow[i]`.

### Update order (one tick)

```
Simulation.Update()
  1. UpdateVelocityCells()   — ballistic cells (sand/water/lava with VelX,VelY != 0) move first
  2. Per-cell scan, bottom-to-top, alternating L→R / R→L each tick (the _flip)
        dispatched through UpdateCell → per-element rules (UpdateSand, UpdateWater, …)
        each rule uses Swap() which marks _visited so cells move at most once per tick
  3. PropagateElectricity()  — flood-fill from Battery cells through connected Copper
  4. PropagateHeat()         — two BFS distance-field passes (hot from Lava/Fire, cold from LN2)
                               then symmetric ramp toward target heat, then side-effects
                               (boil water, ignite gas, freeze steam)
```

The BFS heat field is the reason copper temperature behaves symmetrically and is scan-order-independent — earlier versions used a one-pass diffuse and were buggy.

### Cell vs static-structure cells

Some `Cell` values are **structural** — they never run an update rule and are treated as solid: `Stone`, `Wood`, `Bark`, `Leaves`, `Mirror`, `Battery`, `Ice`. Look at the switch in `UpdateCell` (~`Simulation.cs:326`) — anything not in the switch is static. `Pinned[i] != 0` further marks any cell as immovable regardless of type (used for tree trunks, turret bases, and the pin tool).

### Reactions live on both sides

A reaction between two cells (e.g. lava + water → stone, water + LN2 → ice + N2 gas) is usually triggered from one cell's update rule (`UpdateLava`, `UpdateWater`, etc.). Be careful: when adding a new pair, decide which side scans for the other, and make sure you `_visited[i] = 1` on both cells if both change, or one will get re-processed within the same tick. `Swap()` does this automatically.

### Velocity cells (`UpdateVelocityCells`)

Cells with non-zero `VelX`/`VelY` (set by explosions or `ApplyForce`) follow a ballistic trajectory until they collide, with sub-pixel stepping. This system is for *projectile* behaviour layered on top of normal element rules — most cells normally have zero velocity. Static types (Wood, Copper, Battery, Mirror, etc.) zero their velocity at the top of the function.

### Heat propagation specifics (`PropagateHeat`)

- `HeatRange` caps how many copper hops heat travels. Beyond that, copper stays at room temp.
- `HeatFireDist` seeds copper touching fire as if it were that many hops from lava — fire is weaker than lava as a heat source.
- `HeatSmoothing` is the divisor for moving `Flow[i]` toward the target each tick. 1 = instant, higher = slow ramp. **Smoothing is essential for stable thermal "circuits"** — without it, oscillation is easy.
- Side effects (boil/ignite/freeze) fire only when smoothed `Flow[i]` crosses a threshold, not when the BFS hits a source. This means cold copper *can* be next to lava if there's only a single-cell connection — the smoothing damps it.

### Macro entities on the grid

- **`Glorp`** is a `Node2D` with float position. It reads `_sim.GetCell` for ground/wall detection but writes only to eat (`SetCell(..., Air)`). It has its own gravity/squish/rolling physics independent of the cell sim. Multiple Glorps are stored in `_glorps` and passed into each Glorp's `Init` so they can sense each other.
- **`LaserTurret`** (private class in `Main.cs`) occupies a `5×3` block of Stone/Battery cells plus two copper terminals on the sides. `CheckPowered` reads `_sim.Electric` at the terminals — so wiring electricity to the turret simply means: route copper from a battery to either side. The beam itself is non-destructive geometry traced in `CastLaserRay`, not cells.
- **`WoodPiece` / rigid bodies** — see "Active in-progress work" below.

### Rendering pipeline

`Render()` (in `Main.cs`) walks the grid once per frame and writes RGBA into `_raw[]`, then uploads to a single `ImageTexture` shown by `TextureRect`. Per-cell colour is computed by `CellColor` (mostly a lookup) with special-cased translucency for gas/steam/smoke/N2 (blended over the air colour) and a heat-gradient for water and copper. `OverlayCanvas` is a sibling node drawn on top for laser beams, shockwaves, mirror X marks, pin highlights, and the heat-view rectangle.

## Wood is a static cell — the rigid-body experiment is abandoned

Wood is just another structural cell type, like Stone or Bark. It doesn't fall, rotate, or behave as a rigid body. Don't try to revive the Crayon-Physics-style rigid body system — it was investigated and abandoned as a failed experiment.

`docs/plans/rigid-body-physics.md` is kept as a historical record of *why* the approach didn't fit, not as a TODO. The short version: continuous float-space bodies stamping onto a per-cell grid created too many awkward boundary cases (collision response with falling sand, force transmission from fluid cells, sleep/wake corner cases) for the value it delivered, and the cell-grid sim is the part of this codebase that's actually interesting. If a player wants something to fall, they use sand or dirt. Wood is for structures.

If you find yourself wanting to add rotation, momentum, or "rigid" behaviour to a new material — stop and design a different interaction instead. The grid is the design space.

## Conventions

- **No physics engine on the 2D sim** — everything is hand-rolled. Avoid pulling in Godot's `RigidBody2D` for sandbox elements; it doesn't compose with the cell grid.
- **Tabs for indentation** in `.cs` files (existing files use tabs — match that, not spaces).
- **Reactions go where the change happens.** If lava becomes stone next to water, the rule lives in `UpdateLava`. Don't split a single reaction across two element rules unless the design genuinely calls for it (steam-from-fire-extinction is one such case).
- **`Pinned` is the universal "don't move" flag.** Respect it in any new cell mover, swap helper, or explosion routine. `Swap()` and `Explode()` already check it.
- **Console first for tuning.** Thresholds (boil, gasthresh, firerate, fireticks, seedrate, treerate, icethresh) are runtime-tweakable via the `~` console — prefer adding a console command over a recompile loop when iterating.

## Useful console commands at runtime

Backtick (`` ` ``) toggles the console. `help` lists everything; key tuning commands: `tps`, `brush`, `clear`, `boil`, `gasthresh`, `firerate`, `fireticks`, `seedrate`, `treerate`, `icethresh`. Tab autocompletes.

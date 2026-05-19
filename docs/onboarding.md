# Onboarding: Sandbox Codebase Guide

A pedagogical walkthrough of every system in this falling-sand sandbox. Read it once front-to-back, then keep it open while you make your first change. Source line numbers are quoted throughout so you can jump straight to the code.

---

## Table of Contents

1. [What the project is and where to start](#1-what-the-project-is-and-where-to-start)
   1. [The north star: interactions, not materials](#11-the-north-star-interactions-not-materials)
   2. [Building and running](#12-building-and-running)
   3. [The file layout in one screen](#13-the-file-layout-in-one-screen)
2. [Mental model: the cell grid](#2-mental-model-the-cell-grid)
   1. [Dimensions, scale, and indexing](#21-dimensions-scale-and-indexing)
   2. [The parallel-array convention](#22-the-parallel-array-convention)
   3. [Cell types and what they mean](#23-cell-types-and-what-they-mean)
   4. [The `Pinned` flag](#24-the-pinned-flag)
   5. [The `_visited` flag and once-per-tick processing](#25-the-_visited-flag-and-once-per-tick-processing)
3. [The simulation tick, end to end](#3-the-simulation-tick-end-to-end)
   1. [Pass 1: velocity cells (ballistic projectiles)](#31-pass-1-velocity-cells-ballistic-projectiles)
   2. [Pass 2: the cell scan and `_flip`](#32-pass-2-the-cell-scan-and-_flip)
   3. [Pass 3: electricity propagation](#33-pass-3-electricity-propagation)
   4. [Pass 4: heat propagation](#34-pass-4-heat-propagation)
4. [Element rules in detail](#4-element-rules-in-detail)
   1. [Sand and dirt](#41-sand-and-dirt)
   2. [Water and its phase changes](#42-water-and-its-phase-changes)
   3. [Lava and its reactions](#43-lava-and-its-reactions)
   4. [Gas, smoke, steam, nitrogen gas](#44-gas-smoke-steam-nitrogen-gas)
   5. [Fire and flammability](#45-fire-and-flammability)
   6. [Liquid nitrogen and ice](#46-liquid-nitrogen-and-ice)
   7. [Seeds, grass, and trees](#47-seeds-grass-and-trees)
   8. [Static structural cells](#48-static-structural-cells)
5. [Heat as a circulatory system](#5-heat-as-a-circulatory-system)
   1. [Why a BFS distance field](#51-why-a-bfs-distance-field)
   2. [How `_hotDist` and `_coldDist` combine](#52-how-_hotdist-and-_colddist-combine)
   3. [Smoothing and stable thermal circuits](#53-smoothing-and-stable-thermal-circuits)
   4. [Side effects: boil, ignite, freeze](#54-side-effects-boil-ignite-freeze)
6. [Electricity as a parallel network](#6-electricity-as-a-parallel-network)
7. [Explosions, shockwaves, and `ApplyForce`](#7-explosions-shockwaves-and-applyforce)
8. [Macro entities living on the grid](#8-macro-entities-living-on-the-grid)
   1. [Glorps](#81-glorps)
   2. [Laser turrets](#82-laser-turrets)
   3. [Bezier mirrors](#83-bezier-mirrors)
9. [Rendering pipeline](#9-rendering-pipeline)
   1. [The per-cell colour function](#91-the-per-cell-colour-function)
   2. [Translucent overlays for gases](#92-translucent-overlays-for-gases)
   3. [The OverlayCanvas](#93-the-overlaycanvas)
10. [The Godot scene tree and the UI](#10-the-godot-scene-tree-and-the-ui)
    1. [`scenes/main.tscn` layout](#101-scenesmaintscn-layout)
    2. [Brush system and the brush ID space](#102-brush-system-and-the-brush-id-space)
    3. [Detach / float tool box](#103-detach--float-tool-box)
    4. [Pin tool and heat-view tool](#104-pin-tool-and-heat-view-tool)
11. [Debug UI, console, and config](#11-debug-ui-console-and-config)
    1. [The ImGui debug window](#111-the-imgui-debug-window)
    2. [The text console](#112-the-text-console)
    3. [Persisted tuning (`user://tuning.cfg`)](#113-persisted-tuning-usertuningcfg)
12. [Why wood is just a static cell](#12-why-wood-is-just-a-static-cell)
13. [Conventions and gotchas](#13-conventions-and-gotchas)
14. [Your first change: a worked example](#14-your-first-change-a-worked-example)
15. [Where to look next](#15-where-to-look-next)

---

## 1. What the project is and where to start

### 1.1 The north star: interactions, not materials

This is a 2D falling-sand sandbox. The single design principle, restated in `CLAUDE.md`, is **interesting interactions between materials**. A new cell type that does not react with anything already in the world is dead weight. When you propose a feature, ask: *does it create at least one new edge in the material interaction graph?* If the answer is no, push back or redesign.

The intended player discovery loop is:

- **Heat as a circulatory system.** Lava is hot, liquid nitrogen is cold, copper carries both. The player builds kettles and freezers from geometry alone.
- **Electricity as a parallel network on the same wire.** Batteries push current through copper; the same wire that carries heat also powers laser turrets.
- **Fire as a state-spreading reaction.** Fire ignites flammables, boils water to steam, is killed by water and LN2.
- **Phase changes everywhere.** Water ↔ Ice ↔ Steam, LN2 ↔ N2 gas, Gas → explosion when touched by lava or hot copper.
- **Mirrors + turrets** make the laser into a puzzle element.

Wood is intentionally a static structural cell — the rigid-body experiment was abandoned. See [§12](#12-why-wood-is-just-a-static-cell).

### 1.2 Building and running

This is a **Godot 4.6.2 + .NET 8** project. `Sandbox.csproj:1` pins the SDK.

- **Run the game.** Open `project.godot` in Godot 4.6 and press F5. The main scene is `scenes/main.tscn`. There is no separate launcher.
- **Build only.** `dotnet build Sandbox.sln` from the repo root, or let Godot's editor build on save.
- **No test suite.** Behaviour is verified by playing the sim. There are no Cursor rules, no Copilot instructions, no README — this onboarding doc is your only formal guide.

Editor plugin: `imgui-godot` is autoloaded (`project.godot:18-20`) so ImGui is always available; the debug panel relies on it.

### 1.3 The file layout in one screen

| File                          | Lines | Role                                                                |
|-------------------------------|-------|---------------------------------------------------------------------|
| `scripts/Simulation.cs`       | ~1019 | Pure cell-grid sim. No Godot rendering, no input. `RefCounted`.     |
| `scripts/Main.cs`             | ~1424 | Godot `Control` node. Owns rendering, input, brushes, turrets, mirrors, shockwaves, glorps, console. |
| `scripts/Main.DebugGui.cs`    | ~438  | `partial class Main` extension — ImGui debug panel + config save/load. |
| `scripts/Glorp.cs`            | ~388  | Creature `Node2D` with float-space position, AI, physics, drawing.  |
| `scripts/OverlayCanvas.cs`    | 10    | Tiny `Control` that delegates `_Draw` to a callback in `Main`.      |
| `scenes/main.tscn`            | 269   | UI scene tree: TextureRect + ToolBox panel + tabs/buttons.          |
| `docs/plans/rigid-body-physics.md` | 198 | Historical record of the abandoned wood rigid-body experiment. |
| `CLAUDE.md`                   | 104   | Authoritative project conventions and design intent.                |

The split between `Simulation.cs` and `Main.cs` is the most important separation in the codebase: **`Simulation` knows nothing about Godot rendering**. Everything visual lives in `Main`.

---

## 2. Mental model: the cell grid

### 2.1 Dimensions, scale, and indexing

The sim is a fixed `320 × 180` grid (`Simulation.SimW` / `Simulation.SimH`, declared at `Simulation.cs:7-8`). Each cell renders as a `4×4` screen-pixel block (`Main.Scale = 4`, `Main.cs:11`), giving a 1280×720 window — matching `project.godot:24-25`.

Indexing is row-major. The single formula used everywhere is:

```
i = y * SimW + x
```

`InBounds(x, y)` (`Simulation.cs:66`) and `GetCell(x, y)` (`Simulation.cs:68`) wrap the index math. `GetCell` returns `Sand` for out-of-bounds reads — this is intentional so most rules treat the world boundary as solid without special casing.

### 2.2 The parallel-array convention

Per-cell data lives in flat arrays of length `SimW * SimH`, all allocated in the `Simulation` constructor (`Simulation.cs:52-64`):

| Array       | Type      | Purpose                                                                      |
|-------------|-----------|------------------------------------------------------------------------------|
| `Grid`      | `byte[]`  | Cell type (cast from `Cell` enum).                                           |
| `Flow`      | `byte[]`  | **Overloaded per cell type.** See below.                                     |
| `Electric`  | `byte[]`  | `1` if a copper cell is electrified *this tick*. Cleared every tick.         |
| `Pinned`    | `byte[]`  | `1` = never moves (used for trees, turret bases, the pin tool).              |
| `VelX/VelY` | `float[]` | Ballistic velocity for cells set by explosions or `ApplyForce`.              |
| `_visited`  | `byte[]`  | `1` if the cell has already been updated this tick. Cleared at tick start.   |
| `_hotDist`  | `byte[]`  | BFS hops from nearest lava through copper. `255` = unreached.                |
| `_coldDist` | `byte[]`  | BFS hops from nearest LN2 through copper.                                    |

The `Flow` byte is the trick that keeps memory low. Its meaning depends on what type the cell is:

- **Water:** temperature on a 0–255 scale (`0` = ice, `128` = room, `255` = boiling). See `UpdateWater` at `Simulation.cs:534`.
- **Copper:** also temperature on the 0–255 scale.
- **Fire / Smoke:** lifetime in ticks. Counts down; when it hits 0 the cell dies.
- **Stone / Dirt / Wood / Bark:** a random colour-jitter seed (set once at paint time) used by the renderer for visual variety.
- **Other cells:** typically `0`.

**Always check `Grid[i]` before interpreting `Flow[i]`.** This is the single most common bug class.

### 2.3 Cell types and what they mean

Enum at `Simulation.cs:10-17`:

```
Air, Sand, Water, Stone, Lava,
Gas, Food, Copper, Steam, Battery, Wood, Mirror,
Dirt, Grass, GrassSeed, TreeSeed,
Bark, Leaves, Fire, Smoke,
LiquidNitrogen, NitrogenGas, Ice
```

Three coarse buckets:

- **Movable elements** (have an `UpdateX` rule): `Sand`, `Water`, `Lava`, `Gas`, `Food`, `Steam`, `Dirt`, `GrassSeed`, `TreeSeed`, `Grass`, `Fire`, `Smoke`, `LiquidNitrogen`, `NitrogenGas`, `Ice`.
- **Structural / static** (no update rule, treated as solid by movers): `Stone`, `Wood`, `Bark`, `Leaves`, `Mirror`, `Battery`, `Copper`.
- **Special**: `Air` (the absence of anything), `Mirror` (geometric — see [§8.3](#83-bezier-mirrors), it is never actually painted as a cell; the `Cell.Mirror` enum exists but the on-screen mirrors are vector curves, not cells).

The single dispatch point is `UpdateCell` at `Simulation.cs:332-356`. Anything not in that switch is treated as static — no rule fires, no movement.

### 2.4 The `Pinned` flag

`Pinned[i] != 0` means the cell never moves, regardless of its type. It is checked in:

- `UpdateCell` (`Simulation.cs:336`) — pinned cells skip their update rule entirely.
- `UpdateVelocityCells` (`Simulation.cs:286`) — pinned cells have their velocity zeroed.
- `Swap` (`Simulation.cs:358-367`) — pinned cells refuse swaps on either side.
- `Explode` (`Simulation.cs:521`) — pinned cells are immune.
- `ApplyForce` (`Simulation.cs:1012`) — pinned cells get no kick.

Used by tree trunks (`GrowTree` sets `Pinned = 1`), turret bases (`LaserTurret.Place`), and the pin tool (`Main.ApplyPin`). The pin tool maintains a parallel `_pinnedSet` in `Main` so it can be cleared on `clear`.

### 2.5 The `_visited` flag and once-per-tick processing

Without `_visited`, a sand grain falling diagonally could be re-processed on the same tick at its new position and fall again, producing teleportation. The fix:

- `Update()` clears `_visited` at the top (`Simulation.cs:95`).
- `Swap()` writes `_visited[bi] = 1` on the destination cell (`Simulation.cs:366`).
- `UpdateCell()` skips cells where `_visited[i] != 0` (`Simulation.cs:335`).

When you add a new reaction that mutates two cells (e.g. water + LN2 → ice + N2), set `_visited[i] = 1` on **both** cells, otherwise one may be reprocessed within the same tick. See `ReactLavaWithWater` (`Simulation.cs:385-404`) for the canonical pattern.

---

## 3. The simulation tick, end to end

`Simulation.Update()` (`Simulation.cs:93-105`) runs four passes in fixed order:

```
1. Array.Clear(_visited)
2. _flip = !_flip                     ; alternates scan direction
3. UpdateVelocityCells()              ; ballistic projectiles move first
4. for y = SimH-1 .. 0:
     for x = (depending on _flip):    ; bottom-to-top, alternating L→R / R→L
         UpdateCell(x, y)
5. PropagateElectricity()
6. PropagateHeat()
```

Why this order matters:

- **Velocity cells first** because a sand grain in flight should not also try to fall via the normal `UpdateSand` rule on the same tick.
- **Bottom-to-top** because falling rules check `y+1` — scanning bottom-up means a tile and the tile below it both get the right view of the world.
- **Alternating L→R / R→L** (`_flip`) prevents directional bias in piles of water and sand. Without it, pools drift left or right systematically.
- **Electricity and heat last** because they propagate through whatever is on the grid *after* movement settled — wiring is recomputed each tick from scratch.

### 3.1 Pass 1: velocity cells (ballistic projectiles)

`UpdateVelocityCells` (`Simulation.cs:262-328`) is the projectile system layered on top of normal element rules. Most cells normally have zero velocity; the only routes into this system are:

- `ApplyForce(cx, cy, radius, strength)` (`Simulation.cs:1001`) — paints velocity onto a disc. Called by the Force brush (`Main.cs:826`).
- `Explode(cx, cy, radius)` (`Simulation.cs:511`) — see [§7](#7-explosions-shockwaves-and-applyforce).
- The gas-explosion spark scatter in `ExplodeGasPocket` (`Simulation.cs:494-508`).

Per tick, every cell with `(VelX, VelY) ≠ 0` and a non-static type:

1. Static-cell types (Air, Copper, Battery, Wood, Mirror, Grass, Bark, Fire, Smoke, NitrogenGas, Ice) zero their velocity and skip — they should never be ballistic. Pinned cells also skip.
2. Below `StopThr` (default 0.30), velocity is zeroed.
3. Apply gravity (`vy += Gravity`) and friction (`vx *= Friction; vy *= Friction`).
4. Sub-step the trajectory in up to 8 steps so fast cells do not tunnel through walls.
5. On each sub-step: if the destination is Air and not pinned, swap; otherwise mark `collided = true` and stop.
6. On collision, invert velocity and damp by `DampCol`.

Sub-pixel stepping is the only reason explosions look right.

### 3.2 Pass 2: the cell scan and `_flip`

The main scan iterates `y = SimH-1` to `0`, then for each row alternates direction based on `_flip`. `UpdateCell` dispatches by `Grid[i]` cast to `Cell`. Each rule typically:

- Reads neighbours via `GetCell`.
- Reacts (mutates `Grid` / `Flow` for both cells, marks both `_visited`).
- Moves itself (via `Swap`, which auto-sets `_visited` on the destination).

The static-cell types are not in the dispatch switch — they simply sit there and let movers swap around them.

### 3.3 Pass 3: electricity propagation

`PropagateElectricity` at `Simulation.cs:109-127`:

- Clears `Electric[]`.
- For every `Battery` cell, calls `TryElectrify` on its 4-neighbours.
- `TryElectrify` electrifies a `Copper` cell that is not yet electrified and pushes it onto a stack.
- Drains the stack, electrifying connected copper.

This is a flood fill, not a directional current. Any copper connected to *any* battery is `Electric[i] = 1` for the next tick. The result is consumed by:

- `LaserTurret.CheckPowered` (`Main.cs:1184-1191`) — reads the two terminal cells on the turret's sides.
- The renderer (`Main.cs:650-655`) — electrified copper sparks yellow/red about 55% of the time, which is what you see as "current flowing".

### 3.4 Pass 4: heat propagation

`PropagateHeat` at `Simulation.cs:146-237`. This is the most algorithmically interesting part of the codebase. See [§5](#5-heat-as-a-circulatory-system) for the design rationale.

---

## 4. Element rules in detail

Each rule lives in `Simulation.cs`. They are all small — read the source after this overview.

### 4.1 Sand and dirt

`UpdateSand` (`Simulation.cs:374-383`). The classic falling-sand rule:

- If the cell directly below is fall-into-able (Air / Water / Gas / Steam), swap down.
- Otherwise check diagonals. If both are open, pick one randomly; if only one, take it.

`SandCanFallInto` (`Simulation.cs:371-372`) defines what counts as "can fall through".

`UpdateDirt` (`Simulation.cs:658-668`) is identical except for an early-out: `if (_rng.NextSingle() > 0.30f) return;` before the diagonal step. The effect is *clumpy* — dirt slides only 30% as often as sand, so dirt piles steeper.

### 4.2 Water and its phase changes

`UpdateWater` (`Simulation.cs:534-587`) is the most reaction-heavy rule. In order:

1. **LN2 contact** (50% per tick): water → ice, LN2 → nitrogen gas. Mark both `_visited` and return.
2. **Movement**: fall down, then diagonally, then sideways. Sideways uses a small random-direction-and-symmetric-fallback pattern to spread evenly.
3. **Stationary**: if it could not move, conduct temperature with neighbours.
   - Other water exchanges at `(nf - temp) / 6`.
   - Copper exchanges at `(nf - temp) / 3` — copper is thermally conductive.
   - Ice subtracts 12.
   - Fire adds 25, lava adds 35.
   - All water drifts toward 128 (room temp) at `(128 - temp) / 20`.
4. **Freeze** at `temp ≤ 8` (10% per tick) → Ice.
5. **Boil** at `temp ≥ 220` (4% per tick) → Steam.

Water's `Flow` is its temperature. Renderer tints it red→blue accordingly (`Main.cs:527-535`).

### 4.3 Lava and its reactions

`UpdateLava` (`Simulation.cs:406-426`) chain:

1. `LavaTouchesGas` → explodes the entire connected gas pocket (`ExplodeGasPocket`).
2. `ReactLavaWithWater` → both cells become Stone.
3. `ReactLavaWithLN2` → lava becomes Stone, LN2 becomes Nitrogen Gas.
4. `LavaIgnitesFlammables` (`Simulation.cs:643-654`) → 3% chance per flammable neighbour per tick to ignite it.
5. Falling — 50% per tick (so lava moves at half-speed, looks viscous), preferring straight-down then diagonals, then sideways into air.

The "scan for the other side" convention matters here: gas / water / LN2 reactions are coded inside `UpdateLava`. If you add a new lava reaction, follow the same pattern and remember to `_visited[i] = 1` on both cells.

### 4.4 Gas, smoke, steam, nitrogen gas

All four are upward-drifters with small variations:

| Cell           | Update                     | Notes                                                  |
|----------------|----------------------------|--------------------------------------------------------|
| `Gas`          | `UpdateGas` (440-453)      | Dies at top row. Explodes on contact with lava (any neighbour). |
| `Steam`        | `UpdateSteam` (594-609)    | 0.25% chance per tick to dissipate (→ Air). Can rise through water. |
| `Smoke`        | `UpdateSmoke` (873-889)    | `Flow` is lifetime; dies when it reaches 0.            |
| `NitrogenGas`  | `UpdateNitrogenGas` (959-973) | Pure upward drift; cleared at top row.              |

The pattern is the same: try straight up, then up-diagonals, then sideways. Steam can displace water on the way up — used by the steam-from-extinguishment effect.

### 4.5 Fire and flammability

`IsFlammable(c)` at `Simulation.cs:613-617` is the canonical list: `Wood`, `Bark`, `Leaves`, `Grass`. Sand and dirt do not burn.

`UpdateFire` (`Simulation.cs:798-869`) per tick:

1. **Lifetime tick down.** `Flow` is the remaining ticks. At 0 the cell dies.
2. **Extinguishment**: any 4-neighbour water turns this cell into Steam and clears the water. Any LN2 neighbour turns it into Air and produces N2 from the LN2.
3. **8-directional ignition** of flammable neighbours at `FireIgniteChance` (default 0.12) per tick per neighbour. 8-directional is what makes fire actually consume dense tree structures instead of just crawling along surfaces.
4. **Smoke emission**: 15% chance per tick to place Smoke directly above into Air.
5. **Drift**: fires near fuel drift slowly (18%), fires in the open drift fast (70%). This is what makes fires *cling to logs* instead of immediately wafting off into the sky.

`IgniteCell` (`Simulation.cs:619-632`) is the placement helper. Bark and wood get triple base lifetime so structures actually burn through.

`SetFire(x, y)` (`Simulation.cs:634-641`) is the public entry point used by the Fire brush (`Main.StampFireCircle`). Unlike `IgniteCell`, `SetFire` does not check flammability — it just places a fire cell unconditionally.

### 4.6 Liquid nitrogen and ice

`UpdateLiquidNitrogen` (`Simulation.cs:920-955`):

- Reacts on contact: extinguishes Fire (LN2 boils to N2 gas), freezes Steam to Ice (40% per tick).
- Surface evaporation: if the cell above is Air, 0.3% per tick to vapour away as N2.
- Flows like water but only into `LN2CanFallInto` cells (`Air`, `Gas`, `NitrogenGas`, `Steam`).

`UpdateIce` (`Simulation.cs:977-999`) is a one-trick rule:

- Scans 4-neighbours and accumulates a `meltChance`: Fire = 0.15, Lava = 0.20, hot copper proportional to how hot.
- Spreads freeze: cold water (Flow ≤ 64) adjacent to ice flips to ice at 8% per tick.
- Melts at the accumulated rate, becoming cold water (Flow = 60).

These two cells are why an LN2 + copper rig can make an ice machine: cold copper freezes adjacent steam (the threshold is `IceCopperThreshold`, default 64, checked in `PropagateHeat` side-effects).

### 4.7 Seeds, grass, and trees

Three seed types feed into a tiny ecosystem:

- `GrassSeed` (`UpdateGrassSeed`, `Simulation.cs:672-687`): falls like sand, sprouts into `Grass` when sitting on `Dirt` at `GrassSeedRate` (default 0.003/tick). Withers slowly if it lands on anything else.
- `TreeSeed` (`UpdateTreeSeed`, `Simulation.cs:689-705`): same fall behaviour, but on `Dirt` or `Grass` calls `GrowTree` at `TreeSeedRate` (default 0.001/tick).
- `Grass` (`UpdateGrass`, `Simulation.cs:709-739`): spreads horizontally onto adjacent dirt and vertically (one blade tall) into the air cell above. Spread rates are tiny (0.0008 horizontal, 0.0006 vertical) so growth feels organic, not viral.

`GrowTree` (`Simulation.cs:743-780`) is procedural and worth reading:

- Pick a trunk height (8–15 cells) and 2–4 branch attachment heights.
- Trunk cells become `Bark` and are `Pinned = 1`.
- Each branch is a horizontal `Bark` stick capped by a leaf blob.
- Top of the trunk also gets a leaf blob.

`PlaceLeafBlob` (`Simulation.cs:782-794`) stamps a roughly circular disc of `Leaves` (no pinning — leaves can be knocked off by explosions).

This is the source of "fuel-rich worlds": dump tree seeds onto dirt and the map will fill with flammable structures over time.

### 4.8 Static structural cells

`Stone`, `Wood`, `Bark`, `Leaves`, `Mirror`, `Battery`, `Copper`, `Ice` have **no update rule**. They sit on the grid. Movers can swap into them only if a rule explicitly allows it (e.g. `SandCanFallInto` does not include any of them).

`Copper` and `Battery` are dynamic on the electrical/thermal axis (they are read by `PropagateElectricity` and `PropagateHeat`) but spatially static.

---

## 5. Heat as a circulatory system

This is the most carefully designed part of the simulation. Read `PropagateHeat` (`Simulation.cs:146-237`) with the explanation below open.

### 5.1 Why a BFS distance field

Earlier versions used a one-pass diffuse (each copper cell averages with its neighbours every tick). That is buggy in two ways:

- **Scan-order dependence**: copper at the end of a long wire would heat up faster from the left than the right, depending on which way the loop scanned that tick.
- **Asymmetry between hot and cold**: lava and LN2 propagated at different "speeds" because of the order rules fired in.

The fix: compute, every tick, the **graph distance through connected copper** from each copper cell to the nearest lava and the nearest LN2. Then derive temperature deterministically from those two distances. The scan order no longer matters.

### 5.2 How `_hotDist` and `_coldDist` combine

For each copper cell `i`:

```
hotPull  = hd < range ? 127 * (range - hd) / range : 0
coldPull = cd < range ? 128 * (range - cd) / range : 0
target   = clamp(128 + hotPull - coldPull, 0, 255)
```

- `hd` = `_hotDist[i]` (copper hops from nearest lava; fire seeds at `HeatFireDist`, default 16 — fire is a *weaker* heat source than lava).
- `cd` = `_coldDist[i]` (copper hops from nearest LN2).
- `range` = `HeatRange` (default 32) — beyond this, copper stays at room temp.

A cell next to lava with no LN2 in range gets target = 255. A cell next to LN2 with no lava nearby gets target = 0. A cell equidistant between both settles at 128 (room temp). The whole field is order-independent and bilaterally symmetric.

### 5.3 Smoothing and stable thermal circuits

`HeatSmoothing` (default 2) is the divisor controlling how fast `Flow[i]` ramps toward `target`:

```
step = (target - current) / smooth
```

Why this matters: without smoothing, a single tick of cooling could trip the boil threshold, then the next tick trip the freeze threshold, then back again — oscillation. Smoothing gives copper thermal inertia. **It also means cold copper can survive a single-cell touch of lava** — the smoothing damps the pull, so a thin solder joint doesn't immediately turn the whole circuit red.

### 5.4 Side effects: boil, ignite, freeze

After `Flow` has been smoothed, a second pass over all copper cells checks `Flow[i]` (the smoothed value, not the raw target) against thresholds:

- `Flow[i] ≥ CopperBoilThreshold` (default 200) → adjacent Water becomes Steam.
- `Flow[i] ≥ CopperGasThreshold` (default 230) → adjacent Gas pockets explode via `ExplodeGasPocket`.
- `Flow[i] ≤ IceCopperThreshold` (default 64) → adjacent Steam becomes Ice.

This is why the player's kettle works: heat copper enough → adjacent water boils to steam → steam rises → on cold copper somewhere else, the steam freezes back to ice. The whole loop is geometry plus thresholds — no scripting required.

All three thresholds are tunable at runtime in the console (`boil`, `gasthresh`, `icethresh`) and in the ImGui debug panel.

---

## 6. Electricity as a parallel network

Already covered in [§3.3](#33-pass-3-electricity-propagation). Two design points worth repeating:

- The wire is **the same copper that conducts heat**. Wiring a heater also wires a weapon — that is the whole point. There is no separate "wire" cell.
- `Electric[]` is recomputed from scratch every tick. There is no "stored charge" or capacitance.

If you want to add a new electrical consumer, the pattern is in `LaserTurret.CheckPowered`:

```csharp
return (sim.InBounds(termX, row) && sim.Electric[row * SimW + termX] != 0)
```

— just read `Electric[]` at the cells you care about.

---

## 7. Explosions, shockwaves, and `ApplyForce`

`Explode(cx, cy, radius)` (`Simulation.cs:511-532`) does three things in one pass:

1. Cells inside an inner radius (3/8 of the radius) are **deleted** to Air.
2. Cells in the outer ring get a velocity kick — speed proportional to `1 - dist/radius`, direction radially outward.
3. The explosion is appended to `PendingExplosions` so the renderer can draw a shockwave.

The visual shockwave is **not part of the sim** — it lives in `Main`. Each frame, `Main._Process` (`Main.cs:285-300`) drains `PendingExplosions` into a `_shockwaves` list of expanding-and-fading rings drawn by the overlay (`Main.cs:699-705`). Doing it this way keeps `Simulation` free of Godot dependencies.

`ApplyForce(cx, cy, radius, strength)` (`Simulation.cs:1001-1018`) is the user-facing brush. It paints velocity onto cells in a disc but does *not* delete anything — it is a non-destructive shove. The Force brush (`Main.cs:826`) uses it.

`ExplodeGasPocket` (`Simulation.cs:467-509`) is the cooler usage: when gas meets a heat source, it flood-fills the connected gas pocket, clears all gas cells, computes the centroid, picks a radius scaled by `sqrt(cellCount)`, calls `Explode`, then scatters lava sparks around the perimeter with outward velocity. The whole pocket detonates as one event.

---

## 8. Macro entities living on the grid

Three things sit *on top of* the cell grid as separate objects. They read the grid for collisions and reactions but mostly write back through their own logic.

### 8.1 Glorps

`Glorp.cs` is a `Node2D` with **float-space position** (`SimPos`), independent of the grid. Glorps are creatures with simple needs and they exist to make the world feel alive. They are added/removed via `Main.SpawnGlorp` and the Glorp brush.

**State** (`Glorp.cs:13-46`):

- Needs: `Hunger`, `Thirst`, `Social` (0–100; high hunger/thirst = bad, high social = good).
- Physics: `_physVelX`, `_physVelY`, `_isGrounded`, `_squishAmount`, `_rotation`.
- AI intent: `_vel` (normalised direction the AI wants to move).
- Talk bubble: `_bubbleTimer`, `_bubbleText`.

**Per-frame loop** (`Glorp._Process`, `Glorp.cs:86-244`):

1. Tick up `Hunger`, `Thirst` at their respective rates. Tick `Social` up when near another Glorp (within `TalkRange`, default 22 sim units), down when alone.
2. Pick a target: food if hungry, water if thirsty, another Glorp if lonely.
3. If close enough (`EatRange = 4`), eat (clear a Food cell, reduce Hunger) or drink (reduce Thirst).
4. Otherwise wander randomly with a 0.8–2.2s direction timer.
5. Apply gravity / friction / horizontal accel toward AI intent.
6. Move; handle horizontal collisions against `IsHardSolid` (Stone, Lava, Copper) with a 3-cell step-up; push the foot above any walkable cell so the Glorp sits on sand.
7. Compute rolling rotation from horizontal velocity (`ω = v / r`).
8. Decay squish.

**Drawing** (`Glorp._Draw`, `Glorp.cs:313-329`): a circular pixel-art texture rotated by `_rotation`, squished by `_squishAmount`. Selected Glorps get a corner-bracket frame with H/T/S stats. Speech bubbles use a polygon tail pointing down.

**Important touchpoints with the sim:**

- Reads `_sim.GetCell` for ground / wall detection.
- Writes to the sim only when eating: `_sim.SetCell(tx, ty, Cell.Air)`.
- Holds a reference to `_allGlorps` (passed in from `Main`) so it can sense its peers without iterating the scene tree.

### 8.2 Laser turrets

`LaserTurret` is a private nested class in `Main.cs:1144-1213`. It is **a chunk of grid cells plus a float angle**, not a standalone scene node.

Placement (`LaserTurret.Place`, `Main.cs:1154-1182`):

- A `5×3` block: outer cells are `Stone`, the centre is `Battery`.
- Two `Copper` terminals on the middle row, one cell to either side of the block.
- All occupied cells get `Pinned = 1`.

`CheckPowered` (`Main.cs:1184-1191`) returns `true` iff either terminal cell is electrified. So *wiring electricity to the turret means routing copper from a battery to a terminal*.

`UpdateAngle` (`Main.cs:1193-1199`) just points the turret at the current mouse sim position via `atan2`.

The beam itself is **not cells**. Each frame, `DrawTurrets` (`Main.cs:1038-1074`) calls `CastLaserRay` for each powered turret. `CastLaserRay` (`Main.cs:1076-1140`):

- Marches along the ray in 0.5-cell steps.
- Stops at the first non-`Air`/`Gas`/`Steam` cell; if that cell is Sand/Water/Food/Lava, 25% chance to vapourise it (this is the laser's only sim side effect).
- Before marching, scans every mirror for the closest ray–curve intersection within the segment budget. If a mirror is closer than the next solid cell, the beam reflects off the mirror's surface normal and continues with a new direction and a fresh range budget.
- Hard-capped at `_laserMaxBounces` (default 12) reflections; power drops by `_laserFalloff` (default 0.4) per bounce, used to fade the rendered beam alpha — not to limit destruction.

### 8.3 Bezier mirrors

`BezierMirror` is the other private nested class (`Main.cs:1220-1423`). Mirrors are **purely geometric** — they live in float-space and never touch the cell grid.

The pipeline from mouse-drag to a smooth curve:

1. **Sample** raw mouse positions as the user drags (`AddSample`, `Main.cs:1238-1252`). Only accept samples that are at least `RawMinDist` (default 1.0 sim units) away from the previous one.
2. **Simplify** via Ramer–Douglas–Peucker (`RdpRecurse`, `Main.cs:1256-1282`). Reduces the raw polyline to a smaller set of significant points using `RdpEpsilon` (default 1.5) as the deviation threshold. Both are tunable in the console (`mirrordist`, `mirrorepsilon`) and ImGui Mirrors tab.
3. **Build** chordal Catmull-Rom segments through the simplified points (`Rebuild`, `Main.cs:1295-1323`). Chordal (α=1) means tangent magnitudes scale with local chord length — preventing the self-intersecting loops you get with uniform Catmull-Rom when a short segment sits between two long ones.

Intersection (`Intersect`, `Main.cs:1340-1384`):

- Each Catmull-Rom segment is sampled into 24 line segments.
- For each line segment, 2D ray vs segment intersection. If a hit is found, the closer one wins.
- Surface normal is computed from the **analytical tangent** (`Tangent`, `Main.cs:1332-1336`) — exact derivative of the cubic Bézier — then flipped to face the firing turret (using a dot product against the vector from hit to origin).

`Draw` and `IsNearCurve` use the same sampling. Erase uses `IsNearCurve` against the brush radius.

This is the only macro entity that is genuinely vector-graphics; everything else either is a cell or stamps cells.

---

## 9. Rendering pipeline

`Render()` (`Main.cs:611-677`) walks the grid once per frame:

1. For each cell, compute `(r, g, b)` via `CellColor` or special-case the translucent gas/steam/smoke/N2 cells.
2. If a cell is electrified copper, randomly tint it yellow/red (about 55% of the time) so current is visible as flicker.
3. If a cell is pinned (and not Air), brighten by `+40` per channel so pins are visible.
4. Write the four RGBA bytes into a flat `_raw` byte array.
5. `_image.SetData(...)` then `_texture.Update(_image)` uploads to the single `ImageTexture` shown by `TextureRect`.

This is one upload per frame, regardless of how many cells changed.

### 9.1 The per-cell colour function

`CellColor` (`Main.cs:518-609`) is a big switch. Most cells return a constant RGB triple. Three cells are temperature-mapped:

- **Water**: tinted toward red when hot, deeper blue when cold (`Main.cs:530-534`).
- **Copper**: 3-way gradient — icy blue at `Flow=0`, dull copper at `Flow=128`, lava-hot at `Flow=255` (`Main.cs:552-568`). This is what makes a copper wire visibly indicate its temperature.
- **Lava, Stone, Dirt, Wood, Bark**: jitter their base colour by a deterministic function of `Flow` (which stores a random colour seed) so paint strokes don't look flat.

Fire is a special case: its colour depends on its remaining `Flow` lifetime — bright yellow at the start, dark red as it dies (`Main.cs:592-598`).

### 9.2 Translucent overlays for gases

Gas, Steam, Smoke, NitrogenGas are blended with the Air colour in the renderer (`Main.cs:621-649`) using a hard-coded alpha (e.g. Gas = 0.52). This is the simplest possible "translucency" without an actual alpha buffer: just lerp toward `(AirR, AirG, AirB)` before writing the opaque RGBA. Smoke also fades over its lifetime via `_sim.Flow[i]` (lifetime).

### 9.3 The OverlayCanvas

`OverlayCanvas` (`scripts/OverlayCanvas.cs`) is a 10-line `Control` that delegates `_Draw` to an `OnDraw` callback. `Main` creates one (`Main.cs:191-195`) as a child *after* the TextureRect so it renders above the simulation pixels, and points `OnDraw` at `Main.DrawOverlay`.

`DrawOverlay` (`Main.cs:680-706`) draws everything that isn't a cell:

- Heat-view selection rectangle (subtle white fill + bright outline).
- Committed Bezier mirrors (bright) and the in-progress stroke (dimmer).
- Turret barrels and laser beams (three stacked `DrawLine` calls per segment — wide soft glow, mid glow, hot core).
- Shockwave rings (`DrawArc` × 3 per ring).

`_overlay.QueueRedraw()` is called once per frame in `Main._Process` (`Main.cs:302`).

---

## 10. The Godot scene tree and the UI

### 10.1 `scenes/main.tscn` layout

The root is a `Control` named `Main` with `scripts/Main.cs` attached. Two children:

- `TextureRect` — fills the viewport, shows the cell-grid texture.
- `UI` (`CanvasLayer`) — holds the ToolBox panel.

The ToolBox structure:

```
UI/ToolBox (Control, anchored top-right)
  Tab (Panel, the always-visible header bar)
    TabLabel ("▼ Tools")
    DetachBtn ("⊞" / "⊡")
  Panel (the dropdown body, off-screen by default)
    VBoxContainer
      TabBar (Materials / Settings / Analysis buttons)
      MaterialsPage (4-column grid of brush buttons + brush-size slider)
      SettingsPage (sim-speed slider)
      AnalysisPage (Heat View + Pin buttons + result label + hint)
```

The `Panel` slides down on hover via a Godot `Tween` (`ShowPanel` / `HidePanel`, `Main.cs:778-796`). When detached, hover-toggling is disabled and the whole ToolBox can be dragged by its Tab (`Main._Input`, `Main.cs:380-402`).

`OverlayCanvas` is added programmatically in `_Ready`, not declared in the scene. The ImGui debug window is also pure code — the `imgui-godot` autoload provides the ImGui context.

### 10.2 Brush system and the brush ID space

Brush IDs are declared at the top of `Main.cs:13-33`. Two ID spaces share a single int:

- **Positive IDs** map directly to `Cell` enum values (so `BrushSand = (int)Cell.Sand`). The brush calls `_sim.SetCell(...)` with the cast value.
- **Negative IDs** are "special" brushes that do not paint a cell type: `BrushErase = -1`, `BrushForce = -2`, `BrushGlorp = -3`, `BrushPin = -4`, `BrushHeatView = -5`, `BrushTurret = -6`, `BrushMirror = -7`, `BrushFire = -9`.

`ApplyBrush` (`Main.cs:807-833`) switches on the current brush and either calls `StampCircle` (for cell-painting brushes), `StampFireCircle` (because fire needs `SetFire` per cell), `_sim.ApplyForce`, or returns and lets the per-tool branch in `_Process` handle it (Heat / Pin / Mirror / Turret / Glorp).

`StampCircle` (`Main.cs:835-842`) is a simple discrete disc: paint every cell where `dx² + dy² ≤ r²`.

The mouse wheel resizes the brush in `_Input` (`Main.cs:451-454`), clamping to 1–20.

### 10.3 Detach / float tool box

`ToggleDetach` (`Main.cs:754-776`) flips between two anchor configurations:

- **Anchored** (default): pinned to the top-right corner via `AnchorRight = 1`, drops down on hover, slides back up on hover-out.
- **Floating**: anchors removed, position becomes absolute. The Tab acts as a drag handle (`_Input` checks `_tab.GetGlobalRect().HasPoint(mouse)`, `Main.cs:383-393`).

UI mouse capture is checked in three places in `_Process` and `_Input`. The order is: ImGui first, then Tab, then Panel (only when expanded), then floating ToolBox.

### 10.4 Pin tool and heat-view tool

**Pin** (`ApplyPin`, `Main.cs:500-514`):

- LMB-drag pins; pin/unpin mode is decided on the first click — if the first clicked cell is unpinned, the drag pins; if already pinned, the drag unpins.
- RMB-drag always unpins.
- Uses half the brush radius so pinning feels precise.

**Heat view** (`Main.cs:478-496`):

- LMB-drag draws a selection rectangle.
- On release, `ComputeHeatResult` averages `Flow[i]` across all copper cells in the rectangle and prints a label like "Avg heat: 184 / 255  3 copper cells — Hot".
- The rectangle is drawn by the overlay until the brush is switched away.

These two are good examples of "tools that read or annotate the sim" without painting cells.

---

## 11. Debug UI, console, and config

### 11.1 The ImGui debug window

`Main.DebugGui.cs` is `partial class Main` — it shares fields with `Main.cs`. The window toggles on the **backtick key** (`Main.cs:371`) — set up in `_Input` to catch `Key.Quoteleft`.

`DrawDebugGui` (`Main.DebugGui.cs:25-55`) is called every frame from `Main._Process` (`Main.cs:309`). **Important**: it is called *before* the "block game input" early return because ImGui must be submitted every frame regardless of UI focus.

Tabs:

- **Simulation** — TPS, brush size, ballistic constants (`Gravity`, `Friction`, `DampCol`, `StopThr`).
- **Heat** — boil / gas / ice thresholds, `HeatRange`, `HeatFireDist`, `HeatSmoothing`.
- **Fire / Plants** — fire ignite chance, base lifetime, seed rates.
- **Laser** — beam falloff per bounce, max bounces.
- **Mirrors** — RDP epsilon, raw sample distance.
- **Glorp** — physics + needs + senses (every static field in `Glorp`).
- **Console** — scrollback + input line for the text console.

The Save Settings button writes the entire panel state to `user://tuning.cfg`.

### 11.2 The text console

Lives in the Console tab of the ImGui debug window. Submit with Enter. `ExecuteCommand` (`Main.cs:878-989`) parses commands:

| Command           | Effect                                                          |
|-------------------|-----------------------------------------------------------------|
| `help`            | List all commands.                                              |
| `tps <n>`         | Simulation speed (1–120).                                       |
| `brush <n>`       | Brush radius (1–20).                                            |
| `clear`           | Wipe grid, pins, glorps, turrets, mirrors.                      |
| `boil <n>`        | Copper boil threshold (0–255).                                  |
| `gasthresh <n>`   | Copper gas-ignite threshold (0–255).                            |
| `firerate <0-1>`  | Per-tick fire spread chance.                                    |
| `fireticks <n>`   | Base fire lifetime.                                             |
| `seedrate <0-1>`  | Grass seed sprout rate.                                         |
| `treerate <0-1>`  | Tree seed grow rate.                                            |
| `icethresh <n>`   | Copper threshold below which it freezes water (0–127).          |
| `laserfalloff <0-1>` | Beam power per bounce.                                       |
| `lasermax <n>`    | Max mirror bounces.                                             |
| `mirrordist <f>`  | Raw sample chord length.                                        |
| `mirrorepsilon <f>` | RDP simplification threshold.                                 |

`ConsoleLog` (`Main.DebugGui.cs:297-311`) strips BBCode tags from the message and maps the first colour tag to an ImGui colour. The reason it accepts BBCode is historical — the console used to render in a Godot `RichTextLabel`.

**Convention from `CLAUDE.md`**: when iterating on a tuning value, prefer adding a console command over a recompile loop.

### 11.3 Persisted tuning (`user://tuning.cfg`)

`SaveConfig` (`Main.DebugGui.cs:317-376`) and `LoadConfig` (`Main.DebugGui.cs:378-437`) round-trip every tunable value plus the debug window's position and size. `LoadConfig` is called once at the end of `_Ready` (`Main.cs:270`), so the previous session's tuning auto-restores on launch. Save is a button — there is no auto-save.

The file lives in Godot's standard `user://` location, which on Windows is `%APPDATA%\Godot\app_userdata\Sandbox\tuning.cfg`.

---

## 12. Why wood is just a static cell

`docs/plans/rigid-body-physics.md` is a 198-line design doc for a Crayon-Physics-style rigid-body system that was prototyped and abandoned. It is kept as a *historical record of why the approach didn't fit*, not as a TODO. The short version:

- Continuous float-space bodies stamping onto a per-cell grid created too many awkward boundary cases (collision response with falling sand, force transmission from fluid cells, sleep/wake corner cases).
- The cell-grid simulation is the part of this codebase that is genuinely interesting and composable; rigid bodies fought it instead of extending it.
- If a player wants something to fall, they use sand or dirt. Wood is for structures.

**Practical implications:**

- `Wood` is just a structural cell type, like Stone or Bark. It does not fall, rotate, or behave as a rigid body.
- Do not pull in Godot's `RigidBody2D` for sandbox elements; it does not compose with the cell grid.
- If a new feature wants rotation, momentum, or "rigid" behaviour, **stop and design a different interaction**. The grid is the design space.

---

## 13. Conventions and gotchas

From `CLAUDE.md` and observed code style:

- **Tabs for indentation** in `.cs` files. Match existing files.
- **No physics engine on the 2D sim** — every bit of motion is hand-rolled.
- **Reactions go where the change happens.** If lava becomes stone next to water, the rule lives in `UpdateLava`. Don't split a single reaction across two element rules unless the design genuinely calls for it (steam-from-fire-extinction is one such case).
- **`Pinned` is the universal "don't move" flag.** Respect it in any new cell mover, swap helper, or explosion routine. `Swap()` and `Explode()` already check it.
- **`Flow` is overloaded.** Always check `Grid[i]` before reading it. Setting a cell type via `SetCell` (`Simulation.cs:74-84`) auto-initialises `Flow` to 128 for Water/Copper, 0 for everything else.
- **`_visited` must be set on both cells in a two-cell reaction**, or one will be reprocessed within the same tick.
- **`Swap` and `Explode` and `ApplyForce` all check `Pinned`** — but a new bespoke mover you write does not. Add the check.
- **Console first for tuning.** Thresholds (`boil`, `gasthresh`, `firerate`, `fireticks`, `seedrate`, `treerate`, `icethresh`) are runtime-tweakable. Prefer adding a console command over recompiling to test a number.
- **`GetCell` returns `Sand` out of bounds.** This is deliberate — most rules treat the world edge as solid. Rely on it.
- **The renderer reads `_sim.Pinned[i]` and `_sim.Electric[i]` directly.** If you add a new per-cell visualisation flag, surface it as a public array on `Simulation`, the same way.
- **ImGui must be submitted every frame.** Do not early-return out of `_Process` before `DrawDebugGui()`.
- **Subsystems lifetime.** `_glorps`, `_turrets`, `_mirrors`, `_pinnedSet` are all cleared by the `clear` console command (`Main.cs:916-928`). If you add a new long-lived collection, clear it there too.

---

## 14. Your first change: a worked example

Let's say you want to add a new reaction: **dirt next to lava bakes into stone over time** (justification: it adds an edge to the interaction graph — dirt becomes a soft barrier that hardens under heat, which lets the player build heat-shielded structures).

**Where the rule goes.** Following the "reactions go where the change happens" convention, the change happens to dirt, not lava. So extend `UpdateDirt` (`Simulation.cs:658-668`).

**What to add.** At the top of `UpdateDirt`, before the falling logic:

```csharp
private void UpdateDirt(int x, int y)
{
    // Bake to stone if adjacent to lava
    ReadOnlySpan<int> ddx = stackalloc[] { 0, 0, -1, 1 };
    ReadOnlySpan<int> ddy = stackalloc[] { -1, 1, 0, 0 };
    for (int k = 0; k < 4; k++)
    {
        int nx = x + ddx[k], ny = y + ddy[k];
        if (!InBounds(nx, ny)) continue;
        if (Grid[ny * SimW + nx] == (byte)Cell.Lava && _rng.NextSingle() < 0.01f)
        {
            int i = y * SimW + x;
            Grid[i] = (byte)Cell.Stone; Flow[i] = 0;
            _visited[i] = 1;
            return;
        }
    }
    // ... original falling logic unchanged
}
```

**Why this is small.** No `Pinned` check (we are not moving anything). No `_visited` on the lava cell (we are not changing it). The probability constant is small (1% per neighbour per tick) so the bake is visible but not instant — make it tunable via a new console command if you iterate on the value.

**How to verify.** Run the game. Paint dirt with the Dirt brush, drop lava on it. Within a few seconds the contact line should be stone. Open the heat-view tool to confirm the original heat circuit on adjacent copper is unaffected.

**Where it might go wrong.**

- If you forget `_visited[i] = 1`, the bake-then-fall logic could fire on the same tick and move a stone cell as if it were dirt.
- If you reverse the check (look at *lava* for nearby dirt in `UpdateLava` instead), you'd need to think about both sides setting `_visited` and the symmetry with other lava reactions. Following the convention keeps the diff local.

That's roughly the size of contribution this codebase is designed for: small, local, justified by an interaction.

---

## 15. Where to look next

Read in this order:

1. `CLAUDE.md` — design philosophy, conventions, and the "what NOT to revive" list.
2. `Simulation.cs` end-to-end — start at `Update()` (`:93`) and follow the dispatch in `UpdateCell` (`:332`).
3. `PropagateHeat` (`Simulation.cs:146`) — the most algorithmically interesting code, with comments.
4. `Main.cs:_Ready` and `_Process` — see how the sim is wired into Godot's frame loop.
5. `CastLaserRay` and `BezierMirror.Intersect` (`Main.cs:1076, 1340`) — the prettiest geometry in the codebase.
6. `Glorp._Process` — the only place float-space physics live; a good contrast to the cell sim.
7. `docs/plans/rigid-body-physics.md` — read *only* to understand why this approach was rejected, not as a roadmap.

When you propose a change, pull this doc back open and search for the relevant section — the conventions are the load-bearing part of working in this codebase, not any individual rule.

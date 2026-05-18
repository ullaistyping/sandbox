# Rigid Body Physics System

Agreed design for a Crayon Physics-style rigid body simulation. Wood is the first consumer. All decisions below were locked in through design review.

---

## Core model

- Bodies live in **continuous float-space**: `Position` (Vector2), `Angle` (float), `VelX`, `VelY`, `AngVel` (all floats).
- The cell grid is a **read-only collision surface and render canvas**. Each tick a body's shape is *stamped* onto it; the grid does not own or drive body state.
- A reverse lookup `Dictionary<(int,int), RigidBody>` maps every occupied grid cell to its owning body. Updated on every stamp.

---

## WoodPiece (RigidBody) data

```
LocalCells   List<Vector2>          unrotated cell offsets from CoM, fixed at construction
GridCells    HashSet<(int,int)>     current world grid positions, updated each tick
Position     Vector2                float CoM in sim-space
Angle        float                  radians
VelX, VelY   float                  linear velocity (px/tick)
SubX, SubY   float                  sub-pixel accumulators
AngVel       float                  radians/tick
Mass         float                  = LocalCells.Count (1 unit/cell)
Inertia      float                  = Σ |r_i|²  (computed once at construction)
SleepTimer   int                    ticks below all sleep thresholds
Sleeping     bool
```

---

## Physics pipeline (per tick, per body)

```
1. Skip if Sleeping
2. Accumulate forces → update velocities
3. Step X (sub-pixel accumulator, 1 cell at a time)
4. Step Y (sub-pixel accumulator, 1 cell at a time)
5. Step Angle (one delta per tick, capped)
6. Apply friction if grounded
7. Check sleep condition
```

Each step tests the projected footprint via `ProjectCells()` before committing. If blocked, run collision response instead of moving.

---

## Collision detection

**Footprint sampling** — project all `LocalCells` through the rotation matrix to integer grid coords:

```
wx = round(px + lx*cos(θ) - ly*sin(θ))
wy = round(py + lx*sin(θ) + ly*cos(θ))
```

Per-axis stepping: if |SubX| or |SubY| > 1, step 1 cell at a time until blocked or accumulated sub-pixel is consumed. Rotation applies once per tick (no sub-stepping; angular velocity is capped low enough that tunnelling through 1-cell walls is not a concern).

---

## Cell interaction rules

| Cell type | Wood behaviour |
|---|---|
| Air | Pass-through |
| Steam, Gas | Pass-through (force applied separately by fluid system) |
| Water | Displace — push water cell to a random adjacent air cell |
| Stone, Copper, Battery, Lava, Sand | **Hard block** — triggers collision response |
| Other Wood (via reverse lookup) | **Hard block** — two-body collision response |
| Pinned cell | Hard block regardless of type |
| World boundary | Hard block |

---

## Collision response (impulse-based)

When a body's step is blocked, compute a contact normal `n` (mean direction from blocked cells to CoM, normalised), contact point `r` (mean position of blocked cells relative to CoM), then:

**One-body (static grid):**
```
Vn    = dot(Vel, n)                          // relative velocity along normal
rCrossN = cross(r, n)                        // scalar in 2D: r.x*n.y - r.y*n.x
j     = -(1 + e) * Vn / (1/M + rCrossN² / I)
VelX += j * n.x / M
VelY += j * n.y / M
AngVel += cross(r, j*n) / I
```

**Two-body (other wood piece B):**
```
Vrel  = (VelA + cross(AngVelA, rA)) - (VelB + cross(AngVelB, rB))
Vn    = dot(Vrel, n)
j     = -(1 + e) * Vn /
        (1/Ma + 1/Mb + (rA×n)²/Ia + (rB×n)²/Ib)
// Apply +j*n to A, -j*n to B (linear and angular)
```

**Restitution `e` = 0.2** (slightly bouncy, mostly inelastic like real wood).

After impulse, zero the velocity component that was driving into the surface (prevent sinking).

---

## Fluid / cell force system

Each tick, for every exposed face of a body (a cell edge bordering a non-wood cell), check the neighbour:

```
for each cell (cx, cy) in GridCells:
    for each of 4 face directions (dx, dy) in {up, down, left, right}:
        neighbour = Grid[(cy+dy)*W + (cx+dx)]
        force = ForceForCell(neighbour)     // float magnitude, 0 if no force
        if force == 0: continue
        n = normalised(dx, dy)              // outward face normal
        r = (cx + 0.5*dx, cy + 0.5*dy) - Position   // contact point from CoM
        VelX  += force * n.x / Mass
        VelY  += force * n.y / Mass
        AngVel += cross(r, force*n) / Inertia
```

**`ForceForCell` magnitudes (per tick, tunable):**
| Cell | Force |
|---|---|
| Steam | 0.08 |
| Water (flowing) | 0.04 |
| Gas | 0.02 |
| All others | 0.0 |

This enables the windmill case: steam hitting the side face of a paddle blade applies a sideways force at an offset from CoM, generating torque.

---

## Friction

Applied when the body is in contact with any hard-block surface (grounded or side-contact):

```
VelX   *= 0.85  (per tick)
AngVel *= 0.88  (per tick)
```

Not applied when fully airborne. Prevents infinite sliding on slopes.

---

## Sleep / wake

**Sleep condition** — body enters sleep after `N = 10` consecutive ticks where all of:
- `|VelX| < 0.05`
- `|VelY| < 0.05`
- `|AngVel| < 0.001`

**Wake condition** — any of:
- A hard-block neighbour cell changes type (cell adjacent to any GridCell changed last tick)
- Another body's footprint enters an adjacent cell
- A fluid force cell appears adjacent to an exposed face

Sleeping bodies are skipped in the physics loop entirely. The reverse lookup is still maintained for them (other bodies can still collide with sleeping pieces).

---

## Mass and inertia

Computed once in constructor, never recomputed (destruction out of scope for this implementation):

```csharp
Mass    = LocalCells.Count;
Inertia = LocalCells.Sum(l => l.LengthSquared());  // Σ |r_i|²
```

Minimum inertia clamp to avoid divide-by-zero on single-cell bodies: `Inertia = max(Inertia, 1.0)`.

---

## Implementation steps

1. **`RigidBody` class** — data fields, constructor (CoM, LocalCells, Mass, Inertia), `ProjectCells()`
2. **Reverse lookup** — `_cellToBody` dictionary, updated in `StampGrid()`
3. **`StampGrid` / `ClearGrid`** — clear old cells → air, write new cells → Wood, update lookup
4. **`CanOccupy` + contact info** — returns blocked cells, identifies whether blocker is static or another `RigidBody`
5. **Impulse solver** — `ResolveCollision(bodyA, bodyB_or_null, contactCells, normal)`, one-body and two-body paths
6. **Physics tick** — force accumulation, sub-pixel X/Y stepping with collision response, angle stepping
7. **Fluid force pass** — exposed-face scan, `ForceForCell`, apply linear + angular impulse
8. **Friction pass** — contact check, apply velocity damping
9. **Sleep / wake system** — sleep timer, neighbour-change detection for wake
10. **Placement** — brush paints cells, on release construct `RigidBody` from painted set, stamp to grid
11. **`clear` command** — wipe `_bodies` list and `_cellToBody` dictionary
12. **Tuning pass** — adjust force magnitudes, restitution, friction, sleep thresholds in-engine

---

## Out of scope (this implementation)

- Cell destruction / piece fragmentation from burning
- Rope / joint constraints between bodies
- Non-wood rigid body types (reuse the same system when needed)

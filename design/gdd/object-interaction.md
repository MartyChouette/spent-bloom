---
status: reverse-documented
source: [src/gameplay/ObjectGrabber.cs, src/gameplay/PlaceableObject.cs, src/gameplay/PairableItem.cs, src/gameplay/ItemHighlight.cs, src/gameplay/PlacementSurface.cs, src/gameplay/DropZone.cs, src/gameplay/snap/MagneticSnap.cs]
date: 2026-05-20
---

# Object Interaction

> **Note**: This document was reverse-engineered from the existing implementation.

**Status**: Reverse-documented
**Depends on**: PlacementSurface, PlaceableObject, PairableItem, ItemHighlight, DropZone, ObjectGrabber, MagneticSnap (all snap target variants)

---

## Overview

The Object Interaction system lets the player pick up, carry, and place objects
throughout the apartment using a spring-damper physics model. Objects float toward
a target point driven by the mouse cursor, with five authored feel presets that
govern how each object archetype handles momentum and resistance. Placement uses a
ghost-preview renderer with an 11-step validation cascade, grid snapping, and
magnetic snap zones for contextually appropriate placement targets (pairs, trash,
turntables, bottles, watering cans). Items that belong together — shoes, dishes,
books — snap and stack via the `PairableItem` system. Held objects render on top
of all scene geometry, and a highlight system with six priority layers pulses to
draw attention to interactive items.

## Player Fantasy

The player should feel like they are tidying up with gentle, satisfying weight.
Heavy objects should feel like they have mass; small trinkets should feel nimble
and responsive. Placing something in exactly the right spot — a shoe paired with
its partner, a dish stacked on its pile — should feel like a small click of
rightness. The apartment should reward attentiveness: magnetic snaps guide rather
than force, and the ghost preview communicates intent before commitment.

## Detailed Rules

### Grab Feel Presets

Each `PlaceableObject` is assigned one of five spring-damper feel presets. These
govern how the physics model pulls the object toward the cursor target point.

| Preset   | Spring | Damper | MaxAccel | AngularDamp |
|----------|--------|--------|----------|-------------|
| Default  | 120    | 18     | 60       | 12          |
| Plucky   | 35     | 6      | 20       | 5           |
| Floaty   | 25     | 12     | 15       | 4           |
| Snappy   | 500    | 40     | 200      | 25          |
| Heavy    | 50     | 25     | 25       | 3           |

### Pickup Feel Ramp

When an object is first picked up, there is a 0.25-second blend from the
**carry parameters** (spring=250, damper=22, maxAccel=120, angularDamp=16) into
the object's assigned preset. This prevents the object from immediately flying
to the cursor on grab — it eases in from a tight, controlled carry state into its
characteristic feel.

### Spring-Damper Physics

Each physics tick, the following force is computed and applied to the held
object's Rigidbody:

```
force = (targetPosition - objectCenter) * spring - velocity * damper
force = Clamp(force, -maxAccel, maxAccel)
```

Angular velocity is damped separately each tick by `angularDamp`.

### Grid Snapping

Placement positions are snapped to a grid with a base cell size of **0.06 meters**.
An edge margin of **0.03 meters** is applied, meaning objects cannot be placed closer
than 0.03m to a surface edge. The wall-surface grid multiplier is currently **1.0x**
(same grid as floors), with a planned future value of 2–3x for coarser wall placement.

### Ghost Preview Renderer

While an object is held, a ghost preview mesh renders at the candidate placement
position.

- **Valid placement**: RGBA (0.5, 0.95, 0.55, 0.35) — desaturated green
- **Invalid placement**: RGBA (0.95, 0.2, 0.2, 0.35) — red
- **Ghost render queue**: 3500 (renders behind held object)
- **Held object render queue**: 3900 (renders on top of all scene geometry)

The ghost updates every frame to the snapped candidate position.

### Placement Rejection Cascade

Before a placement is confirmed, 11 checks run in order. The first failure rejects
placement and turns the ghost red. Checks run sequentially — if check 3 fails,
checks 4–11 do not run.

1. **Surface check**: A valid `PlacementSurface` must be under the candidate position.
2. **Wall mount**: If the surface is a wall, the object must be a wall-mountable type.
3. **Fridge state**: If placing inside a fridge, the fridge door must be open.
4. **Cubby state**: If placing inside a cubby, the cubby door must be open.
5. **Trash category**: If placing in a trash zone, the object's category must match
   the zone's accepted category.
6. **Height clearance**: The object's bounding box must fit within the surface's
   vertical clearance.
7. **Barrier sphere**: A sphere of radius **0.009 meters** is cast at the candidate
   position. Any collision with a barrier collider rejects placement.
8. **Exclusion zones**: The candidate position must not overlap any active exclusion
   zone volume.
9. **Footprint overhang**: The object's footprint must be at least 50% supported by
   the surface — no more than 50% may overhang the edge.
10. **Occupancy**: The grid cell at the candidate position must not already be
    occupied by another placed object.
11. (Reserved for future use / additional game-specific check.)

### Magnetic Snap Zones

Certain placement targets pull the held object into a precise position when the
object is within snap range. Snaps are evaluated every frame while the object is
held.

| Snap Type      | Range  | Behavior                                     |
|----------------|--------|----------------------------------------------|
| Pair snap      | 0.4 m  | Hard teleport to partner position immediately |
| Trash snap     | 0.8 m  | Quadratic pull — stronger as distance closes  |
| Turntable snap | 0.6 m  | Smooth position lock to turntable center      |
| Bottle snap    | 1.2 m  | Smooth snap to bottle target mount point      |
| Watering snap  | 1.5 m  | Smooth snap to watering can spout target      |

Pair snaps (0.4 m) trigger a **hard teleport** — the object is immediately moved
to the snap position rather than being pulled over time. All other snaps apply a
force or position blend per frame.

### PairableItem System

`PairableItem` defines whether an object can bond with another object on placement.

- **SpecificPartner**: The item snaps only to one named partner (e.g., left shoe
  to right shoe). The pairing is consumed on first snap — it is a one-time bond.
- **AnyOfCategory**: The item snaps to any other item in the same category (e.g.,
  dishes stack with other dishes, books stack with other books). Stackable.

Snap orientations on placement:

- **SideBySide**: Used for shoes. The snapping item is placed adjacent to the
  partner along a fixed axis.
- **Stacked**: Used for dishes and books. The snapping item is placed directly
  on top of the stack.

**Double-click to unstack**: If the player double-clicks a stacked item within a
**0.3-second window**, the entire stack is broken and all items are individually
grabbable. Outside this window, clicking grabs only the top item.

### ItemHighlight System

Interactive objects are highlighted using `MaterialPropertyBlock` (MPB) per
interaction slot — no material instances are created. There are **6 priority
layers**; higher-priority highlights override lower-priority ones on the same
object.

The highlight pulses at **3 Hz** using the formula:

```
intensity = 0.7 + 0.3 * sin(time * 2 * PI * 3)
```

This keeps the highlight always visible (minimum 0.7) with a rhythmic pulse to
draw attention.

### Rotation

While holding an object, pressing **RMB** rotates it in **45-degree steps**.
If an object was mounted on a wall before pickup (and thus had a wall-specific
rotation), that rotation is preserved when the object is placed back on a wall
surface. Floor placement always uses the cursor-facing rotation.

### Tether and Wall Collision

If the held object moves more than **3 meters** from the grab origin, it snaps
back to the grab origin (tether snap-back). Velocity is clamped against wall
colliders — the object cannot be pushed through a wall by the spring force.

## Formulas

### Spring-Damper Force

```
force = (targetPosition - objectCenter) * spring - velocity * damper
clampedForce = Clamp(force magnitude, 0, maxAccel) in force direction
```

Variables:
- `targetPosition`: world-space cursor target projected onto the carry plane
- `objectCenter`: world-space center of the held Rigidbody
- `spring`: preset spring constant (e.g., 120 for Default)
- `damper`: preset damper constant (e.g., 18 for Default)
- `maxAccel`: maximum force magnitude in units/sec² (e.g., 60 for Default)

**Example (Default preset)**: Object is 0.5 units from target, velocity is 1 unit/sec
toward target.
```
force = 0.5 * 120 - 1.0 * 18 = 60 - 18 = 42 units/sec²
42 < maxAccel(60), so force = 42 units/sec²
```

### Pickup Ramp Blend

```
blendParam = min(timeSincePickup / 0.25, 1.0)
spring = Lerp(250, presetSpring, blendParam)
damper = Lerp(22, presetDamper, blendParam)
maxAccel = Lerp(120, presetMaxAccel, blendParam)
angularDamp = Lerp(16, presetAngularDamp, blendParam)
```

At t=0 (moment of grab): fully carry parameters (tight, controlled).
At t=0.25s: fully preset parameters (characteristic object feel).

### Highlight Pulse

```
intensity = 0.7 + 0.3 * sin(Time.time * 2 * PI * 3.0)
```

Range: [0.4, 1.0]. Period: 0.333 seconds (3 Hz).

### Pair Snap Teleport

```
if (distance(heldObject, partnerObject) <= 0.4):
    heldObject.position = snapTargetPosition  // immediate, no lerp
```

### Trash Magnetic Pull (Quadratic)

```
t = 1.0 - (distance / 0.8)  // normalized, 0 at range, 1 at contact
pullForce = t * t * maxTrashPull
```

## Edge Cases

- **Pair snap consumed**: Once a `SpecificPartner` pair bond forms, the snap zone
  deactivates. Picking up one shoe and moving it elsewhere does not re-enable the
  snap; the player must deliberately re-place.

- **Double-click on top of a single item**: If a solo item (no stack) is
  double-clicked, the double-click is treated as a standard grab. No unstacking
  animation or feedback plays.

- **Wall rotation on floor drop**: If an object with wall rotation is dropped on
  a floor surface (not a wall), the rotation resets to the default floor-facing
  orientation. It is not preserved across surface types.

- **Barrier sphere at 0.009m**: This is intentionally very small — it catches
  near-miss placements clipping into thin geometry (baseboards, trim) that the
  footprint check does not detect. Objects with very thin footprints may still
  fail this check in tight corners.

- **Tether distance**: At exactly 3.0 meters, the snap-back triggers. The object
  teleports to the grab origin, not to the cursor. Velocity is zeroed at snap-back
  to prevent oscillation.

- **Ghost during magnetic snap**: When a magnetic snap is active, the ghost preview
  moves to the snap target position rather than the grid-snapped cursor position.
  The 11-step cascade still runs against the snap position. If the snap position
  fails validation, the ghost turns red but the snap remains visually active.

- **Stacked item occupancy**: Each item in a stack occupies the same grid cell.
  Occupancy check passes for stacking (handled by the `PairableItem` Stacked path
  before the occupancy check runs), but a non-pairable item cannot be placed in the
  same cell as any stack member.

- **Fridge/cubby with closed door**: If a door closes while an item is inside, the
  item is not ejected. The cubby-state check only applies at placement time.

- **Render queue conflict**: Held objects at queue 3900 render on top of UI elements
  that render below that queue. If a UI panel uses queue < 3900, a held object will
  visually clip through it. This is known and accepted for the current UI stack.

## Dependencies

- **PlacementSurface**: Defines valid surface volumes, wall/floor type, height
  clearance, and edge boundaries. Required for checks 1–2 and 6 of the rejection
  cascade.
- **PlaceableObject**: Carries per-object data: feel preset, category, wall-mount
  flag, footprint size, home position. The static registry `PlaceableObject.All`
  is used for batch queries (never `FindObjectsByType`).
- **PairableItem**: Extends `PlaceableObject` with partner data, snap type, and
  stack state. Manages the double-click unstack window.
- **ItemHighlight**: MPB-based highlight system. Reads priority layer assignments
  from `ObjectGrabber` (held = priority 6) and interaction raycasts (hover = priority 3).
- **DropZone**: Represents trash zones and other categorized placement zones. Provides
  accepted category and snap range for trash magnetic pull.
- **ObjectGrabber**: The player-facing controller. Owns the carry plane, spring
  update loop, tether logic, RMB rotation, and ghost renderer. Reads
  `ApartmentManager.ScreenPointToRay` for all raycasts.
- **MagneticSnap variants**: Individual snap target components (pair, trash,
  turntable, bottle, watering). Each registers itself; `ObjectGrabber` polls all
  active snaps each frame.

**Reverse dependencies** (systems that depend on Object Interaction):

- `TidyScorer` reads `PlaceableObject.IsAtHome` and `PlaceableObject.IsOnFloor`
  each scoring tick to compute mess and clutter.
- `DateSessionManager` queries object states (e.g., turntable, watering snap) to
  trigger date reactions.

## Tuning Knobs

| Parameter                  | Current Value          | Safe Range       | Affects                                              |
|----------------------------|------------------------|------------------|-------------------------------------------------------|
| Default spring             | 120                    | 60 – 300         | Responsiveness of default-feel objects               |
| Default damper             | 18                     | 8 – 40           | Oscillation settling of default-feel objects         |
| Default maxAccel           | 60                     | 30 – 150         | Maximum speed of default-feel object movement        |
| Carry spring (ramp start)  | 250                    | 150 – 400        | Tightness of feel at moment of grab                  |
| Pickup ramp duration       | 0.25 sec               | 0.1 – 0.5        | How long before object settles into its preset feel  |
| Grid base size             | 0.06 m                 | 0.03 – 0.12      | Precision of placement snapping                      |
| Grid edge margin           | 0.03 m                 | 0.01 – 0.05      | Minimum clearance from surface edge                  |
| Barrier sphere radius      | 0.009 m                | 0.005 – 0.02     | Near-geometry placement rejection sensitivity        |
| Pair snap range            | 0.4 m                  | 0.2 – 0.8        | Distance at which shoe/partner snap triggers         |
| Trash snap range           | 0.8 m                  | 0.4 – 1.5        | Distance at which trash pull activates               |
| Turntable snap range       | 0.6 m                  | 0.3 – 1.2        | Distance at which turntable snap triggers            |
| Bottle snap range          | 1.2 m                  | 0.6 – 2.0        | Distance at which bottle mount triggers              |
| Watering snap range        | 1.5 m                  | 0.8 – 2.5        | Distance at which watering can snap triggers         |
| Tether distance            | 3.0 m                  | 1.5 – 5.0        | Maximum carry distance before snap-back              |
| Highlight pulse frequency  | 3 Hz                   | 1 – 6            | Attention-drawing speed of interactive highlights    |
| Highlight min intensity    | 0.7                    | 0.4 – 0.9        | Minimum highlight brightness between pulses          |
| Double-click window        | 0.3 sec                | 0.2 – 0.5        | Time window to detect double-click for unstack       |
| RMB rotation step          | 45 degrees             | 15 – 90          | Precision of manual object rotation                  |
| Ghost render queue         | 3500                   | 3000 – 3800      | Render order of placement ghost                      |
| Held render queue          | 3900                   | 3800 – 4000      | Render order of held object (above scene geometry)   |

## Acceptance Criteria

1. **Spring preset — Snappy**: Pick up a Snappy-preset object and move the cursor
   quickly. The object must reach the cursor in under 0.1 seconds from 0.5 units
   away. Release and verify it settles without significant oscillation.

2. **Pickup ramp**: Pick up a Plucky-preset object (spring=35). At the frame of
   grab, spring must be 250 (carry). At 0.25 seconds post-grab, spring must be 35.
   Intermediate values must be a linear lerp.

3. **Ghost validity**: Hold an object over a valid surface. Ghost must display green
   (0.5, 0.95, 0.55, 0.35). Move object over an occupied cell. Ghost must turn red
   (0.95, 0.2, 0.2, 0.35) without placing.

4. **11-step cascade — occupancy**: Place an object on a grid cell. Attempt to
   place a second non-pairable object in the same cell. Placement must be rejected
   (red ghost) and no overlap must occur.

5. **Pair snap teleport**: Place one shoe. Hold the partner shoe within 0.4m of
   the first. The held shoe must immediately teleport to the snap position — no
   lerp visible.

6. **Trash snap quadratic pull**: Hold a trash item at 0.8m from a trash zone.
   Verify zero pull force. Move to 0.4m. Verify pull increases. At 0.01m, verify
   pull is near maximum.

7. **Double-click unstack**: Stack three dishes. Double-click the top dish within
   0.3 seconds. All three dishes must be individually grabbable afterward.

8. **Double-click timeout**: Stack two dishes. Wait 0.4 seconds, then double-click.
   Only the top dish is grabbed — the lower dish remains placed.

9. **Tether snap-back**: Hold an object and drag the cursor 3.1 meters from the
   grab origin. The object must snap back to the grab origin. Velocity must be
   zero immediately after snap-back.

10. **Highlight pulse**: Hover over an interactive object. Verify the highlight
    pulses at 3 Hz (one full cycle in 0.333 seconds), with intensity never dropping
    below 0.7.

11. **RMB rotation preserved on wall**: Place an object on a wall. Pick it up.
    Do not rotate it. Replace it on the same wall. Verify the object returns to
    its pre-pickup wall rotation.

12. **Held render-on-top**: Pick up an object and drag it in front of a piece of
    furniture. The held object must render on top of the furniture geometry.

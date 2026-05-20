---
status: reverse-documented
source: [src/gameplay/ApartmentManager.cs, src/gameplay/ApartmentAreaDefinition.cs, src/camera/ApartmentCameraController.cs, src/camera/ParallaxController.cs, src/input/IrisInput.cs]
date: 2026-05-20
---

# Apartment Hub

> **Note**: This document was reverse-engineered from the existing implementation.

**Status**: Reverse-documented
**Depends on**: IrisInput, CinemachineBrain, DayPhaseManager, AccessibilitySettings, ScreenFade, MoodMachine

---

## Overview

The Apartment Hub is the persistent home base of Iris/Spent Bloom. The player browses
a small apartment across three distinct physical areas — Kitchen, Living Room, and
Entrance — using a Cinemachine spline-dolly camera mounted on a 4-knot closed loop.
The camera system supports panning, zooming, edge-scrolling, and a top-down overview
mode, with a parallax layer that adds depth to the 2.5D diorama presentation. During
date phases, the camera transitions to director-authored preset positions and restricts
free movement. All areas are always available with no gating.

## Player Fantasy

The player should feel like a quiet observer of their own life — drifting unhurriedly
through a small, lived-in space. Browsing the apartment should feel like panning a
camera across a handcrafted scene: deliberate, calm, and exploratory. No area should
feel inaccessible or locked away. On dates, the camera positions should feel curated,
as though the apartment is staging itself to be seen through someone else's eyes.

## Detailed Rules

### Area Layout

Five areas are configured via `ApartmentAreaDefinition` ScriptableObjects. Three are
currently active: Kitchen, Living Room, and Entrance. Each definition stores the
area's world-space center position and size extent for bounds calculations and
camera clamping.

Active area positions and sizes:

| Area        | Center (x, y, z)   | Size (x, y, z) |
|-------------|---------------------|----------------|
| Kitchen     | (-3.5, 1.5, 1)      | (5, 4, 11)     |
| Living Room | (2, 1.5, 1)         | (6, 4, 11)     |
| Entrance    | (0, 1.5, -6)        | (12, 4, 3)     |

### Area Cycling

Areas can be cycled using A/D keys. This UI is currently hidden and disabled. The
mechanic exists in code but is not exposed to players in the current build.

### Camera Rig

The browse camera runs on a `CinemachineSplineDolly` mounted on a 4-knot closed
spline loop that spans the apartment. The Cinemachine Brain handles blending between
the dolly camera and date preset virtual cameras.

Default projection is **orthographic**. Date-phase camera presets may switch to
**perspective** projection as authored.

### Parallax

A parallax controller applies mouse-position-driven offset and rotation to parallax
layers.

- **Max offset**: 0.05 world units
- **Max rotation**: 1.5 degrees
- **Smoothing**: 8 (lerp factor, applied per frame)
- **Disabled by**: `AccessibilitySettings.ReduceMotion`

When `ReduceMotion` is active, parallax offset and rotation are zeroed out and the
system becomes a no-op.

### Panning

The player can pan the camera within each area by three input methods:

1. **WASD keys**: 3 units/sec constant speed
2. **Middle mouse button drag**: 0.01 world-units-per-pixel multiplier applied to
   screen-space drag delta
3. **Edge-scrolling**: Activates when the cursor is within 60px of the screen edge,
   after a 0.1-second delay. Maximum speed is 4 units/sec, interpolated linearly
   from 0 at the 60px boundary to full speed at the edge.

Pan position is constrained by two selectable clamp modes:

- **Circular clamp**: 3-unit radius from area center (default)
- **Rectangular bounds**: clamped to the area's defined size extents

### Top-Down View

Pressing **Tab** toggles a top-down overview mode.

- **Camera height**: 12 units
- **Orthographic size**: 8
- **Transition speed**: lerp factor 6 (applied per frame)
- **Bounds in top-down mode**: rectangular, fixed at (6, 6) — ignores area-specific
  extents

### Zoom

Five discrete zoom steps are available, cycling with the scroll wheel. Zoom is
applied as orthographic size reduction:

| Step | Orthographic Size |
|------|-------------------|
| 1    | 100               |
| 2    | 80                |
| 3    | 60                |
| 4    | 45                |
| 5    | 30                |

Transitions between zoom levels use lerp at speed 5. `AccessibilitySettings.InvertScroll`
reverses the scroll direction for players who prefer it.

### Camera Transitions

When the camera moves between positions (e.g., area changes or returning from a
preset), it lerps at **speed 5** (configurable range: 1–15). The transition is
considered complete when the squared distance between the camera and its target
position is less than **0.0001**, at which point movement stops.

### Date Phase Overrides

During date phases, a director-authored preset virtual camera takes priority via the
Cinemachine Brain. Free pan speed is reduced to **0.35x** of the base pan speed
while a date preset is active. The parallax system continues running unless
`ReduceMotion` is set.

### Area Gating

All stations and areas are always active. There is no progression unlock or time-of-day
gating for any area.

## Formulas

### Parallax Offset

```
mouseNormalized = (mousePosition - screenCenter) / screenHalfSize
  // range [-1, 1] on each axis

targetOffset = mouseNormalized * maxOffset
  // maxOffset = 0.05

currentOffset = Lerp(currentOffset, targetOffset, smoothing * deltaTime)
  // smoothing = 8
```

### Parallax Rotation

```
targetRotation = mouseNormalized * maxRotation
  // maxRotation = 1.5 degrees

currentRotation = Lerp(currentRotation, targetRotation, smoothing * deltaTime)
```

### Pan Speed (Edge-Scroll)

```
edgeFraction = (60 - distanceFromEdge) / 60
  // edgeFraction in [0, 1], 0 at boundary, 1 at screen edge

panSpeed = edgeFraction * 4.0  // units/sec
```

Edge-scroll does not activate until the cursor has been within the 60px zone for
0.1 continuous seconds.

### Camera Transition Completion

```
done = (cameraPosition - targetPosition).sqrMagnitude < 0.0001
```

**Example**: A camera 0.009 units from its target has sqrMagnitude ~0.000081, which
is below threshold — transition is considered complete.

### Date Phase Pan Speed

```
effectivePanSpeed = basePanSpeed * 0.35
  // basePanSpeed = 3.0 units/sec (WASD)
  // effectivePanSpeed during date = 1.05 units/sec
```

## Edge Cases

- **ReduceMotion active**: Parallax offset and rotation are forced to zero each frame.
  The parallax component remains in the scene but produces no visual movement. Pan
  and zoom are unaffected.

- **InvertScroll active**: The scroll delta sign is flipped before zoom step selection.
  Scrolling up zooms out instead of in. No other input is affected.

- **Date preset active, player pans**: Panning is not fully locked — it is slowed to
  0.35x. If the player pans away from the preset framing, the pan offset accumulates.
  Returning to free-browse mode restores full pan speed.

- **Top-down mode with date preset**: If a date preset activates while top-down is
  engaged, the Cinemachine Brain blend takes priority and the top-down virtual camera
  loses priority. On date end, top-down state is not automatically restored — the
  player must re-press Tab.

- **Circular clamp at area boundary**: If the player pans to exactly the 3-unit
  radius, position is clamped to the circle's perimeter. No bounce or spring is
  applied — clamping is hard.

- **MMB drag while edge-scrolling**: Both inputs contribute simultaneously. The
  resulting velocity is the sum of the drag delta (scaled by 0.01) and the
  edge-scroll speed. This can cause faster-than-expected movement near corners.

- **Zoom at minimum or maximum step**: Scroll input beyond the bounds is ignored.
  No animation or feedback plays at the limit.

- **Area cycling (disabled)**: The A/D key handlers exist in code but the UI is
  hidden. If the UI is re-enabled, cycling wraps from the last area back to the first.

## Dependencies

- **IrisInput**: Provides WASD, Tab, scroll, RMB, MMB input events consumed by
  the camera controller.
- **CinemachineBrain**: Manages blending between the spline-dolly browse camera and
  date-phase preset virtual cameras. Priority-based; presets must outrank the dolly.
- **DayPhaseManager**: Signals when a date phase begins/ends, triggering the pan
  speed reduction and preset camera activation.
- **AccessibilitySettings**: Provides `ReduceMotion` and `InvertScroll` flags read
  by the parallax controller and zoom system respectively.
- **ScreenFade**: Used during camera transitions between areas when a hard cut is
  needed (fade-to-white, then fade back in at the new position).
- **MoodMachine**: Reads the current area to set ambient audio and visual mood
  parameters. Changes when the active browse area changes.

**Reverse dependencies** (systems that depend on Apartment Hub):

- `TidyScorer` reads area bounds from `ApartmentAreaDefinition` to assign misplaced
  objects to the correct area score.
- `DailyMessSpawner` uses area definitions to place morning mess within area bounds.
- `ObjectGrabber` uses `ApartmentManager.ScreenPointToRay` for all raycasts
  (never `Camera.main`).

## Tuning Knobs

| Parameter              | Current Value     | Safe Range   | Affects                                         |
|------------------------|-------------------|--------------|--------------------------------------------------|
| WASD pan speed         | 3.0 units/sec     | 1.5 – 6.0    | How quickly keyboard panning traverses an area  |
| MMB drag multiplier    | 0.01              | 0.005 – 0.02 | Sensitivity of middle-mouse drag panning         |
| Pan circular clamp     | 3.0 units radius  | 2.0 – 5.0    | How far the player can deviate from area center |
| Edge-scroll zone       | 60 px             | 30 – 100     | How wide the edge-scroll trigger region is       |
| Edge-scroll delay      | 0.1 sec           | 0.0 – 0.3    | How long before edge-scroll activates            |
| Edge-scroll max speed  | 4.0 units/sec     | 2.0 – 6.0    | Maximum edge-scroll pan speed                    |
| Parallax max offset    | 0.05 units        | 0.0 – 0.15   | Depth illusion intensity from mouse movement     |
| Parallax max rotation  | 1.5 degrees       | 0.0 – 4.0    | Tilt illusion intensity from mouse movement      |
| Parallax smoothing     | 8                 | 4 – 16       | How snappily parallax tracks the mouse           |
| Top-down height        | 12 units          | 8 – 20       | How high the overview camera sits               |
| Top-down ortho size    | 8                 | 5 – 12       | How much of the apartment fits in overview       |
| Top-down lerp speed    | 6                 | 3 – 12       | Speed of transition into/out of top-down mode   |
| Zoom lerp speed        | 5                 | 2 – 10       | Smoothness of zoom step transitions              |
| Camera transition speed| 5                 | 1 – 15       | Speed of position lerp between camera targets   |
| Date pan speed mult.   | 0.35x             | 0.1 – 1.0    | How restricted panning feels during dates        |

## Acceptance Criteria

1. **Pan bounds**: Place the camera at the maximum circular clamp radius (3 units)
   and verify it cannot be moved further outward by any pan method (WASD, MMB,
   edge-scroll). Position must not exceed 3.0001 units from area center.

2. **Zoom steps**: Starting at step 1 (ortho size 100), scroll down five times and
   verify ortho size reaches 30. Scroll once more and verify ortho size remains 30.

3. **InvertScroll**: Enable `InvertScroll` in accessibility settings. Scrolling up
   must zoom out (size increases). Scrolling down must zoom in (size decreases).

4. **ReduceMotion**: Enable `ReduceMotion`. Move the mouse to the screen corner.
   Verify no parallax offset or rotation is applied to any parallax layer.

5. **Edge-scroll activation delay**: Move cursor to within 60px of the screen edge.
   The camera must not move for the first 0.1 seconds. After 0.1 seconds it must
   begin panning toward the edge.

6. **Top-down toggle**: Press Tab. Verify camera lerps to height 12, ortho size 8.
   Press Tab again. Verify camera returns to the previous browse position.

7. **Date pan speed reduction**: Trigger a date phase with a preset camera active.
   Verify WASD panning is perceptibly slower than free-browse (measured speed must
   be at or below 1.05 units/sec, i.e., 3.0 * 0.35).

8. **Camera transition completion**: Initiate a camera position transition. Verify
   the camera stops updating position once sqrMagnitude to target is below 0.0001.

9. **Area gating**: Verify that at game start (no prior progression), Kitchen,
   Living Room, and Entrance are all accessible and interactable without any unlock
   condition.

10. **MMB drag**: Click and drag the middle mouse button 100 pixels. Verify the
    camera translates 1.0 world units (100px * 0.01 multiplier).

---
status: reverse-documented
source: [WateringManager, WaterablePlant, LivingFlowerPlant, PotCrossSectionUI, PlantDefinition, ObjectGrabber, AudioManager, ApartmentManager]
date: 2026-05-20
---

# Watering System

> **Note**: This document was reverse-engineered from the existing implementation.

**Status**: Reverse-documented
**Depends on**: ObjectGrabber, WaterablePlant, LivingFlowerPlant, PotCrossSectionUI,
PlantDefinition, AudioManager, ApartmentManager

---

## Overview

The Watering System lets the player use a physical watering can to tend their
potted plants. Picking up the can and moving it near a plant triggers a
magnetic snap; tilting to pour fills a 2D cross-section UI representing the
pot's interior. Water level is governed by pot geometry — different pot shapes
require different pour rates to rise at a consistent visual speed. The target
fill sits at 70% of pot height, with an 8-point tolerance window on either
side. Overflow spills onto the floor. Water level persists between sessions
and dries overnight at a rate scaled by the next day's weather. Chronic
underwatering causes leaf shedding; chronic overwatering is marked as a future
consequence (TODO).

## Player Fantasy

The player should feel the gentle satisfaction of a daily ritual done right:
lifting a familiar object, tilting it with deliberate care, watching the water
rise to exactly the right line. Getting it perfect should feel effortless once
learned but never automatic — the oscillating pour rate and varying pot shapes
mean the player must stay present. Forgetting to water should carry a quiet
cost, not a catastrophic penalty: leaves fall, the plant droops, a small mess
appears. The system rewards attentiveness and forgives distraction.

## Detailed Rules

### Interaction Flow

1. The player grabs the watering can via `ObjectGrabber`.
2. When the can's position comes within 1.5 meters of a plant's snap point,
   `ObjectGrabber` detects proximity and highlights the interaction.
3. Clicking while in snap range snaps the can to the pouring position and
   begins the `WateringManager` state transition: Idle → Pouring.
4. The player holds the interaction input to continue pouring; releasing
   transitions to Done (then back to Idle after a settle period).

### State Machine

| State | Behavior |
|---|---|
| Idle | Can is held but not snapped; no water flows |
| Pouring | Water level rises; foam accumulates; audio plays |
| Done | Pouring has stopped; foam settles; status message shown |

### Pot Geometry and Fill Shapes

The 2D cross-section UI renders the interior of the pot. Four pot shapes are
supported, each with a width function `w(h)` where `h` is normalized height
[0, 1]. The `shapeFillMultiplier` corrects the pour rate so that visually the
water rises at a consistent speed regardless of pot width at the current fill
level.

| Shape | Width Function `w(h)` | Fill Multiplier |
|---|---|---|
| TallCylinder | `1.0` (constant) | `1.0` |
| RoundBulb | `0.5 + 0.5 × sin(π × h)` | `1 / max(w(h), 0.3)` |
| TaperedCone | `lerp(1.4, 0.5, h)` | `1 / max(w(h), 0.3)` |
| Hourglass | `0.5 + 0.5 × cos(π × h)` | `1 / max(w(h), 0.35)` |

The Hourglass uses 0.35 as the clamp floor rather than 0.3 to prevent extreme
rate spikes at the waist.

### Water Level Visuals

- **Soil color**: linearly interpolates between dry `(R:0.55, G:0.40, B:0.25)`
  and wet `(R:0.30, G:0.22, B:0.12)` based on current water level.
- **Fill line marker**: fixed at `idealWaterLevel` (0.7); serves as a target
  guide for the player.
- **Water line marker**: moves dynamically to show the current water level.
- **Drain drip**: visible when `waterLevel > 0.5` to signal that the plant
  can drain if conditions allow.
- **Foam**: always rendered at least 0.03 above the water surface.

### Status Messages

Displayed after each pour session ends:

| Condition | Message |
|---|---|
| Water level within tolerance | "Looks perfect!" |
| Water level below target - tolerance | "Could use more..." |
| Water level above target + tolerance | "A bit much..." |

### Overflow

When `waterLevel` exceeds 1.0:
- A `SpillSurface` quad is spawned at ground level adjacent to the pot.
- `waterLevel` is clamped at 1.1 (hard ceiling).
- `foamLevel` is clamped at 1.2.

### Persistence

- Water level is stored on `WaterablePlant` and saved to disk.
- `SaveWaterLevel` is throttled to a maximum of 4 saves per second (4 Hz) to
  avoid write overhead during continuous pouring.
- On session load, the stored water level is restored before any overnight
  drying is applied.

### Camera Behavior

`ApartmentManager` suppresses the standard camera pan input while the player
is in an active watering interaction. Normal camera control resumes when the
can is released or snapped away from the plant.

### Overnight Drying

At the start of each new day, `LivingFlowerPlant` reduces each plant's water
level by a drying rate scaled by the weather for the coming day:

| Weather | Drying Multiplier |
|---|---|
| Clear | 1.5x |
| FallingLeaves | 1.2x |
| Overcast | 1.0x (baseline) |
| Rainy | 0.6x |
| Stormy | 0.4x |
| Snowy | 0.3x |

The base drying rate is defined per plant via `PlantDefinition`.

### Consequences of Poor Watering

- **Underwatering**: 50% chance per morning that the plant sheds a leaf,
  spawning a trash object near the plant's base.
- **Overwatering**: Visual water spill stain at pot base. Full consequence
  implementation is marked TODO in the current build.

## Formulas

### Pour Rate

```
pourRate = 0.1 × shapeFillMultiplier × oscillation

oscillation = 1 + 0.15 × sin(2π × 2 × t)
```

| Variable | Description |
|---|---|
| `0.1` | Base rate constant (water level units per second) |
| `shapeFillMultiplier` | Geometry correction factor for current pot shape and fill height |
| `oscillation` | Sinusoidal variance; frequency = 2 Hz, amplitude = ±15% of base |
| `t` | Time in seconds since pour began |

**Example — TallCylinder at any fill height:**
`shapeFillMultiplier = 1.0`, at `t = 0.25s` (quarter period, oscillation at peak):
```
oscillation = 1 + 0.15 × sin(π/2) = 1 + 0.15 = 1.15
pourRate = 0.1 × 1.0 × 1.15 = 0.115 units/second
```

**Example — RoundBulb at h = 0.5 (widest point):**
```
w(0.5) = 0.5 + 0.5 × sin(π × 0.5) = 0.5 + 0.5 = 1.0
shapeFillMultiplier = 1 / max(1.0, 0.3) = 1.0
pourRate = 0.1 × 1.0 × oscillation  (same as cylinder at widest)
```

**Example — RoundBulb at h = 0.0 (narrow bottom):**
```
w(0.0) = 0.5 + 0.5 × sin(0) = 0.5
shapeFillMultiplier = 1 / max(0.5, 0.3) = 2.0
pourRate = 0.1 × 2.0 × oscillation  (doubles at the base)
```

### Foam Dynamics

```
foamLevel = waterLevel + max(foamAccumulation, 0.03)

foamAccumulation rate (while pouring) = pourRate × 2.5
foamDecay rate (while not pouring)    = -0.2 × deltaTime
```

### Overnight Drying

```
newWaterLevel = waterLevel - (baseDryingRate × weatherMultiplier)
```

`baseDryingRate` is defined in `PlantDefinition` per plant species.

### Water Target and Tolerance

```
targetMin = idealWaterLevel - tolerance = 0.7 - 0.08 = 0.62
targetMax = idealWaterLevel + tolerance = 0.7 + 0.08 = 0.78

"Looks perfect!" when: targetMin <= waterLevel <= targetMax
```

## Edge Cases

**Watering an already-overflow pot**: If `waterLevel` is already at or above
1.0 when the player begins pouring, a `SpillSurface` spawns immediately and
`waterLevel` is clamped to 1.1. Continued pouring has no further effect on
level; the SpillSurface remains.

**Watering during a date**: The watering interaction is not explicitly gated
during a date, but `ApartmentManager`'s camera suppression is active regardless
of date phase. No design rule currently prevents watering during a date; this
is an unresolved edge case.

**Load with water level already in overflow range**: If a saved water level of
1.0+ is restored at session start, overnight drying runs first. If the result
is still >= 1.0, the SpillSurface spawns on day start.

**Zero base drying rate (PlantDefinition not set)**: If `baseDryingRate` is
undefined or zero, no overnight drying occurs. The plant behaves as if weather
is irrelevant. This is a data configuration issue, not a runtime fallback.

**Save throttle during rapid pour**: Because `SaveWaterLevel` is capped at
4 Hz, up to 0.25 seconds of water level change may be unsaved if the game is
force-closed mid-pour. On restore, the water level will reflect the last saved
value, not the moment of closure.

**Snap radius with multiple nearby plants**: If two plants are within 1.5m of
the can simultaneously, `ObjectGrabber` snaps to the nearest one. Only one
plant can be watered at a time.

**Leaf shedding on day 1**: Drying and the 50% leaf-shed check run at day
start. A plant restored from a previous session at an underwatered level will
roll the shed check on the first morning, potentially shedding a leaf before
the player has had a chance to water.

## Dependencies

| System | Role in This System | This System's Role for It |
|---|---|---|
| `ObjectGrabber` | Detects can proximity to plant; manages snap | Watering system activates on snap confirmation |
| `WaterablePlant` | Stores water level; exposes save/restore interface | Watering manager reads and writes water level here |
| `LivingFlowerPlant` | Applies overnight drying; communicates water stress | Reads `WaterablePlant`'s level to determine stress state |
| `PotCrossSectionUI` | Renders 2D pot interior; shows fill, foam, markers | Watering manager drives fill and foam values each frame |
| `PlantDefinition` | Defines pot shape, base drying rate, ideal species | Watering manager reads shape to select fill multiplier |
| `AudioManager` | Plays pour and splash audio cues | Watering manager signals pour start/stop/overflow |
| `ApartmentManager` | Suppresses camera pan during interaction | Watering manager registers active-interaction state |
| `WeatherSystem` | Provides next-day weather for drying multiplier | `LivingFlowerPlant` queries weather at day transition |
| `TidyScorer` | Registers SpillSurface as a mess | SpillSurface spawned by overflow is a tidiness event |

## Tuning Knobs

| Knob | Current Default | Safe Range | Gameplay Effect |
|---|---|---|---|
| Base pour rate | 0.1 units/s | 0.05–0.2 | Speed of fill; too fast makes precision impossible |
| Oscillation amplitude | 0.15 (±15%) | 0.05–0.30 | Tactile variance in pour; too high feels chaotic |
| Oscillation frequency | 2 Hz | 1–4 Hz | Rhythm of the pour; matches audio cue if synced |
| Snap radius | 1.5m | 0.8–2.5m | How close player must be; larger reduces navigation friction |
| Ideal water level | 0.7 | 0.5–0.85 | Target fill height; lower = less patient plant |
| Tolerance | 0.08 | 0.04–0.15 | Window for "perfect"; smaller requires more precision |
| Overflow water cap | 1.1 | 1.0–1.2 | How far past full the level is allowed to go |
| Foam rate multiplier | 2.5x | 1.5–4.0 | How quickly foam builds above water |
| Foam decay rate | 0.2/s | 0.1–0.5 | How quickly foam settles after pouring stops |
| Foam minimum offset | 0.03 | 0.01–0.05 | Ensures foam is always visually above waterline |
| Save throttle rate | 4 Hz | 2–10 Hz | Write frequency; lower risks data loss, higher is I/O heavy |
| Drying rate (Clear) | 1.5x | 1.0–2.0 | Evaporation on sunny days |
| Drying rate (Snowy) | 0.3x | 0.1–0.6 | Minimal evaporation in cold weather |
| Leaf shed chance | 50% | 20–80% | Probability of consequence per underwatered morning |
| Drain drip threshold | 0.5 | 0.3–0.7 | Water level above which drain drip visual activates |

## Acceptance Criteria

1. **Magnetic snap**: Moving the watering can within 1.5m of any plant triggers
   the snap highlight. Clicking begins pouring. QA: measure distance in scene
   at highlight activation.

2. **Pour rate oscillation**: Record `waterLevel` every 0.1 seconds during a
   30-second pour on a TallCylinder. Confirm level increases at a rate between
   0.085 and 0.115 units/second (base rate ± 15% amplitude). QA: unit test or
   scene-level recording.

3. **Target status messages**: Pour to exactly 0.62 (targetMin), 0.70
   (target), and 0.79 (just above targetMax) and stop. Confirm messages
   "Looks perfect!" (at 0.62 and 0.70), "A bit much..." (at 0.79). QA: manual
   test; pause at each level.

4. **Overflow spawns SpillSurface**: Pour until `waterLevel` exceeds 1.0.
   Confirm a `SpillSurface` quad appears at ground level and `waterLevel` does
   not exceed 1.1. QA: scene inspection after overflow.

5. **Overnight drying — Clear weather**: Plant at water level 0.7 on a Clear
   night. Confirm water level the next morning equals `0.7 - (baseDryingRate ×
   1.5)`. QA: set weather to Clear, advance day, check `WaterablePlant.waterLevel`.

6. **Overnight drying — Snowy weather**: Same plant, Snowy weather. Confirm
   level reduction is `baseDryingRate × 0.3`. QA: same method, weather = Snowy.

7. **Leaf shedding probability**: Over 100 simulated mornings with an
   underwatered plant, confirm leaf shed occurs between 35 and 65 times (50%
   ± statistical noise). QA: automated unit test iterating the morning event.

8. **Persistence across sessions**: Set water level to 0.55, save, restart
   session, confirm restored water level is 0.55 before any day-start drying
   runs. QA: log water level immediately on session load before day-start hook.

9. **Save throttle**: Pour continuously for 2 seconds and force a crash at the
   1-second mark. Confirm restored water level is no more than 0.25 seconds of
   pour behind the crash point. QA: debug-forced crash during pour.

10. **Camera suppression**: Begin a watering interaction; confirm camera pan
    input is non-responsive. Release the can; confirm pan resumes. QA: manual
    input test during and after interaction.

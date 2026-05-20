---
status: reverse-documented
source: [FlowerTrimmingBridge, LivingFlowerPlantManager, LivingFlowerPlant, WaterablePlant, ReactableTag, WeatherSystem, MoodMachine, PlantBreathingWobble, PlaceableObject, DateSessionManager]
date: 2026-05-20
---

# Living Plants

> **Note**: This document was reverse-engineered from the existing implementation.

**Status**: Reverse-documented
**Depends on**: FlowerTrimmingBridge, LivingFlowerPlantManager, MoodMachine,
WaterablePlant, ReactableTag, WeatherSystem, TidyScorer (smell),
DateSessionManager (Phase 3 evaluation)

---

## Overview

Living Plants are the persistent record of successful dates. Each trimmed
flower becomes a potted plant placed in the apartment, where it survives for a
number of days determined by the trimming score. Plants decay over time,
require daily watering to stay healthy, and affect the apartment's ambient mood
through two `MoodMachine` sources: overall plant health and air quality. A
healthy, well-watered plant impresses future dates during Phase 3 of the
dating loop. A neglected or dying plant does the opposite. When a plant dies,
it stops contributing to mood and can no longer be reacted to. Plants occupy
fixed slots for now, with slot cycling and movable placement planned for a
future iteration.

## Player Fantasy

The player should feel the gentle weight of responsibility. Each plant is a
souvenir of a specific date — a living memento that needs tending. Watching
several healthy flowers on the windowsill should feel like the apartment
coming alive, a space that reflects care and connection. The slow arc of a
plant's life — from the day it arrives to the day it quietly fades — gives the
apartment a rhythm that runs beneath the louder events of dates and trimming.
Neglect should register as a quiet loss, not a punishment screen.

## Detailed Rules

### Spawn Flow

1. `FlowerTrimmingBridge` captures a `TrimmedFlowerSnapshot` at session end,
   containing the flower geometry, trimming score, calculated days alive, and
   species data.
2. The snapshot is passed to `LivingFlowerPlantManager.SpawnPlant()`.
3. `SpawnPlant()` selects the next available slot from `_plantSlots[]`. If all
   slots are occupied, the first slot is overwritten (WIP — will become movable
   objects in a future iteration).
4. The flower geometry is scaled uniformly to 0.15 meters.
5. The following components are added to the spawned object:
   - `LivingFlowerPlant` — health, decay, death logic
   - `ReactableTag` — makes the plant visible to Phase 3 date evaluation
   - `WaterablePlant` — stores and saves water level
   - `PlantBreathingWobble` — idle animation (gentle oscillation)
   - `PlaceableObject` — registers the plant in the apartment's object registry

### Health System

- Health starts at 1.0 on spawn.
- Health decays linearly by `1.0 / totalDaysAlive` per day.
- `totalDaysAlive` is determined by the trimming score (range: 1–10 days; see
  `flower-trimming.md`).
- Health reaches 0.0 on the final day, at which point the plant is considered
  dead.

**Example**: A 10-day plant loses 0.10 health per day and dies on day 10.
A 4-day plant loses 0.25 health per day and dies on day 4.

### Visual Health States

| Condition | Visual |
|---|---|
| Health > 0.5 | Color interpolates from spawn color toward yellow |
| Health <= 0.5 | Color interpolates from yellow toward brown |
| Health = 0 | Plant appears fully brown (dead) |

- Scale: `lerp(0.8, 1.0, health)` — a dying plant visually shrinks to 80% of
  its trimmed size.
- Water stress overlays are applied on top of the health color:
  - Underwatered: brown tint overlay
  - Overwatered: dark green tint overlay

### Watering and Water State

Water state is determined by comparing `WaterablePlant.waterLevel` to fixed
thresholds:

| State | Condition |
|---|---|
| Underwatered | `waterLevel < waterTarget - tolerance` (< 0.62) |
| Perfect | `waterLevel` in range [0.62, 0.78] |
| Overwatered | `waterLevel > waterTarget + tolerance` (> 0.78) or overflowed |

`waterTarget = 0.7`, `tolerance = 0.08` (shared with the Watering System).

### Overnight Drying

At each day transition, `LivingFlowerPlant` applies overnight drying:

```
newWaterLevel = waterLevel - (dryingRate × weatherMultiplier)
```

| Weather | Multiplier |
|---|---|
| Clear | 1.5x |
| FallingLeaves | 1.2x |
| Overcast | 1.0x |
| Rainy | 0.6x |
| Stormy | 0.4x |
| Snowy | 0.3x |

### Leaf Shedding

If a plant's water state is not Perfect at the start of a morning:
- 50% chance the plant sheds a leaf.
- A trash object is spawned near the plant's base.
- `TidyScorer` registers the trash as a mess affecting the apartment's
  cleanliness score.

### Air Quality Contribution

Each plant contributes to air quality based on its current health and water
state:

```
if waterState == Perfect:
    airQualityContribution = health

if waterState == Underwatered or Overwatered:
    airQualityContribution = health × 0.3
```

`LivingFlowerPlantManager` aggregates all active plants' contributions and
reports the average to `MoodMachine` as the `"AirQuality"` source.

### MoodMachine Sources

`LivingFlowerPlantManager` drives two distinct `MoodMachine` sources:

| Source Name | Value |
|---|---|
| `"LivingPlants"` | Average health of all active (non-dead) plants |
| `"AirQuality"` | Average air quality contribution across all active plants |

Both sources are recomputed each time any plant's state changes (health decay,
watering, day transition).

### Date Evaluation (Phase 3)

When a date enters Phase 3 (Reveal) and investigates a plant via its
`ReactableTag`, the reaction is determined by:

| Condition | Base Reaction |
|---|---|
| `health >= 0.6` | Like |
| `health >= 0.3` | Neutral |
| `health < 0.3` | Dislike |

Water stress downgrades the result one tier:
- Like → Neutral
- Neutral → Dislike

A plant evaluated as Dislike when already Dislike remains Dislike; no further
downgrade applies.

### Plant Death

When health reaches 0.0:
- `ReactableTag` is deactivated (dates can no longer react to it).
- The plant's `"LivingPlants"` and `"AirQuality"` contributions are removed
  from `MoodMachine`.
- The plant `GameObject` is deactivated.
- The slot becomes available for the next spawned plant.

### Persistence

- Water level is saved via `WaterablePlant.ToRecord()` and restored via
  `RestoreWaterLevel()` on session load.
- Health is not saved directly; it is recalculated from the spawn day index
  and current day index on session load.
- If the current day exceeds the plant's `totalDaysAlive` since spawn, the
  plant is considered dead on load and is immediately deactivated.

## Formulas

### Daily Health Decay

```
healthPerDay = 1.0 / totalDaysAlive
health_n = 1.0 - (n × healthPerDay)
```

Where `n` = number of days since spawn.

**Example — 10-day plant on day 3:**
```
healthPerDay = 1.0 / 10 = 0.1
health_3 = 1.0 - (3 × 0.1) = 0.7
```

**Example — 4-day plant on day 2:**
```
healthPerDay = 1.0 / 4 = 0.25
health_2 = 1.0 - (2 × 0.25) = 0.5
```

### Scale

```
scale = lerp(0.8, 1.0, health)
```

At `health = 1.0`: `scale = 1.0`
At `health = 0.5`: `scale = 0.9`
At `health = 0.0`: `scale = 0.8`

### Air Quality Contribution

```
airQuality = health                    (if waterState == Perfect)
airQuality = health × 0.3             (if Underwatered or Overwatered)
```

**Example — two plants:**
- Plant A: `health = 0.8`, Perfect → `contribution = 0.8`
- Plant B: `health = 0.6`, Underwatered → `contribution = 0.6 × 0.3 = 0.18`

```
AirQuality MoodMachine source = (0.8 + 0.18) / 2 = 0.49
LivingPlants MoodMachine source = (0.8 + 0.6) / 2 = 0.7
```

### Overnight Drying

```
newWaterLevel = waterLevel - (dryingRate × weatherMultiplier)
newWaterLevel = clamp(newWaterLevel, 0.0, 1.0)
```

**Example — plant at 0.7, Clear night, `dryingRate = 0.12`:**
```
newWaterLevel = 0.7 - (0.12 × 1.5) = 0.7 - 0.18 = 0.52
```
Plant is now Underwatered (0.52 < 0.62); 50% chance of morning leaf shed.

## Edge Cases

**All slots full — slot overwrite**: When all `_plantSlots[]` are occupied,
the plant in slot 0 is replaced immediately without warning or animation.
Any `ReactableTag`, `MoodMachine` sources, and persistence data for the
evicted plant are removed silently. This is a known WIP behavior.

**Plant dead on session load**: If the current day index minus the spawn day
index exceeds `totalDaysAlive`, the plant is dead on load. Death triggers
immediately: ReactableTag deactivated, MoodMachine sources removed, GameObject
deactivated. No death animation plays on load.

**Zero totalDaysAlive (misconfigured)**: If `totalDaysAlive = 0`, `healthPerDay`
would be undefined (division by zero). This should not occur in practice as
the trimming system guarantees at least 1 day alive. No runtime guard is
documented; this is a data contract assumption.

**Water state exactly at boundary**: Water level of exactly 0.62 (targetMin)
returns Perfect state, not Underwatered. Water level of exactly 0.78 (targetMax)
returns Perfect, not Overwatered. Boundaries are inclusive.

**Dead plant investigated during a date**: If a plant dies between the time
the date arrives and Phase 3 begins (e.g., day transition during an edge case),
its `ReactableTag` is deactivated and the date does not react to it. No
affection event fires.

**Leaf shed on first morning after spawn**: Drying runs overnight before the
player has a chance to water a newly spawned plant. If the trimming session
ends late in the day and drying runs immediately at the next day transition,
the plant may be underwatered on its first morning, triggering a leaf shed.

**Multiple plants at the same water stress level**: Each plant's leaf shed
chance is independent (50% per plant). Three underwatered plants on the same
morning each roll separately, potentially spawning three pieces of trash.

**Weather not yet set for the new day**: If `WeatherSystem` has not resolved
the new day's weather at the moment overnight drying runs, the multiplier
defaults to 1.0 (Overcast baseline). This is an initialization order concern,
not a handled fallback.

## Dependencies

| System | Role in This System | This System's Role for It |
|---|---|---|
| `FlowerTrimmingBridge` | Delivers `TrimmedFlowerSnapshot` to `LivingFlowerPlantManager` | Receives the spawned plant reference (not currently used in bridge) |
| `LivingFlowerPlantManager` | Manages all active plant slots; computes MoodMachine aggregates | Drives `"LivingPlants"` and `"AirQuality"` sources |
| `MoodMachine` | Consumes `"LivingPlants"` and `"AirQuality"` sources | Receives updated values whenever plant state changes |
| `WaterablePlant` | Stores water level; exposes save/restore API | `LivingFlowerPlant` reads water state; Watering System writes it |
| `ReactableTag` | Makes plant visible to Phase 3 date evaluation | `LivingFlowerPlant` deactivates this on death |
| `WeatherSystem` | Provides multiplier for overnight drying | Queried once per day transition by `LivingFlowerPlant` |
| `TidyScorer` | Registers shed leaves as mess objects | Leaf shedding spawns trash objects that TidyScorer tracks |
| `DateSessionManager` | Evaluates plant health in Phase 3 | `ReactableTag` routes the investigation event to DateSessionManager |
| `PlantBreathingWobble` | Provides idle animation | Driven by the plant's health value (not currently documented as health-aware) |
| `PlaceableObject` | Registers plant in apartment object registry | Plant slot management goes through PlaceableObject on spawn |

## Tuning Knobs

| Knob | Current Default | Safe Range | Gameplay Effect |
|---|---|---|---|
| Spawn scale | 0.15m | 0.08–0.25m | Physical size of plant in apartment |
| Health decay per day | `1.0 / totalDaysAlive` | Derived | Determined by trimming score; adjust via minDays/maxDays in trimming |
| Scale at zero health | 0.8 | 0.5–0.95 | How much a dying plant visibly shrinks |
| Health → yellow threshold | 0.5 | 0.3–0.7 | When color shift toward yellow begins |
| Water target | 0.7 | 0.5–0.85 | Shared with Watering System |
| Water tolerance | 0.08 | 0.04–0.15 | Shared with Watering System |
| Air quality stress multiplier | 0.3x | 0.1–0.6 | How much stressed plants reduce air quality contribution |
| Like health threshold | 0.6 | 0.4–0.8 | Minimum health for a date to react positively |
| Neutral health threshold | 0.3 | 0.1–0.5 | Minimum health to avoid a negative reaction |
| Water stress downgrade | 1 tier | 0–1 | Whether bad water state worsens the date reaction |
| Leaf shed chance | 50% | 20–80% | Probability of trash spawn per stressed morning |
| Drying rate (Clear) | 1.5x | 1.0–2.0 | Shared with Watering System |
| Drying rate (Snowy) | 0.3x | 0.1–0.6 | Shared with Watering System |

## Acceptance Criteria

1. **Spawn at 0.15m**: After a trimming session, confirm the spawned plant's
   uniform scale is 0.15m. QA: inspect transform on spawned plant object.

2. **Health decay rate**: A 10-day plant at day 5 should have health = 0.5.
   QA: simulate 5 day transitions in unit test; check `LivingFlowerPlant.health`.

3. **Health at zero triggers death**: Set a plant's health to 0.0 (or advance
   days past `totalDaysAlive`). Confirm `ReactableTag` is deactivated,
   MoodMachine sources are removed, and GameObject is inactive. QA: unit test.

4. **Scale at health 0.5**: Confirm plant's transform scale = `lerp(0.8, 1.0,
   0.5) = 0.9`. QA: unit test or scene inspection at health 0.5.

5. **Water state boundaries**: Water level 0.62 → Perfect; 0.61 → Underwatered;
   0.78 → Perfect; 0.79 → Overwatered. QA: unit test `LivingFlowerPlant.
   GetWaterState()` at all four boundary values.

6. **Air quality — perfect water**: `health = 0.8`, `waterState = Perfect`.
   Confirm `airQualityContribution = 0.8`. QA: unit test.

7. **Air quality — stressed water**: `health = 0.8`, `waterState = Underwatered`.
   Confirm `airQualityContribution = 0.24` (0.8 × 0.3). QA: unit test.

8. **MoodMachine sources updated**: After a day transition changes a plant's
   health, confirm both `"LivingPlants"` and `"AirQuality"` values in
   `MoodMachine` reflect the new state. QA: read MoodMachine sources after
   simulated day transition.

9. **Phase 3 reaction — Like**: Plant with `health = 0.7`, Perfect water.
   Confirm date reacts with Like. QA: trigger Phase 3 evaluation on the plant
   via debug command.

10. **Phase 3 reaction — downgrade from water stress**: Plant with `health =
    0.7` (Like threshold), Underwatered. Confirm date reacts with Neutral
    (downgraded one tier). QA: trigger Phase 3 evaluation.

11. **Leaf shed spawns trash**: Advance morning with an Underwatered plant.
    Over 100 trials, confirm trash spawns between 35 and 65 times. QA:
    automated unit test iterating morning event.

12. **Persistence — water level**: Set plant water level to 0.55, save session,
    reload. Confirm water level is 0.55 before overnight drying. QA: log water
    level immediately on restore before day-start.

13. **Persistence — health recalculated from days**: Spawn a 10-day plant, save
    session, advance 3 days externally (modify day counter in save data), reload.
    Confirm health is 0.7. QA: save-data manipulation test.

14. **Dead on load**: Spawn a 4-day plant, manually set day counter to spawn
    day + 5 in save data, reload. Confirm plant is immediately deactivated on
    load. QA: save-data manipulation test.

15. **Slot overwrite**: Fill all slots with plants, then earn a new flower.
    Confirm the new plant spawns in slot 0, overwriting the previous occupant.
    QA: count active plants; confirm slot 0 has changed.

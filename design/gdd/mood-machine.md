---
status: reverse-documented
source: [src/gameplay/MoodMachine.cs, src/gameplay/MoodMachineProfile.cs, src/gameplay/AtmosphereController.cs]
date: 2026-05-20
---

# MoodMachine

> **Note**: This document was reverse-engineered from the existing implementation.

**Status**: Reverse-documented
**Depends on**: GameClock, WeatherSystem, LivingFlowerPlantManager, AudioManager,
AtmosphereController, PSXRenderController (indirect)

---

## Overview

The MoodMachine is the apartment's emotional nervous system. It continuously
aggregates environmental readings — time of day, weather, plant health, air
quality, and optional perfume — into a single normalized 0–1 mood value. That
value is evaluated against per-profile curves to drive every atmospheric output
in the scene: light color and intensity, fog density, rain particle emission,
audio mix levels, screen color filter, and post-process parameters. The system
is designed to feel organic: mood shifts gradually under most circumstances,
ensuring atmosphere reads as a living ambience rather than a switched state.

## Player Fantasy

The player should feel that the apartment is breathing. Watering a struggling
plant, watching rain begin outside, or spraying a particular perfume should each
register as a perceptible, cohesive shift in the world — not as isolated
one-off effects. The ideal experience is the player pausing and noticing "it
feels different in here now" without being able to name exactly what changed.
Mood is the silent collaborator in every scene.

## Detailed Rules

### Source Registration

Any system may register itself as a mood source by providing a string key and a
float value in [0, 1]. The MoodMachine maintains a dictionary of all active
sources. Sources may update their value at any time. Sources may be removed
(e.g., perfume wearing off, a plant dying). A source that is removed no longer
contributes to the average.

### Target Calculation

Each frame, the MoodMachine computes a target mood value as the simple
arithmetic mean of all currently registered source values:

```
target = sum(sourceValues) / count(activeSources)
```

If no sources are active, target defaults to 0.

### Mood Smoothing

The current mood value lerps toward the target each frame using `MoveTowards`,
which guarantees a linear rate of change rather than an asymptotic ease:

```
currentMood = MoveTowards(currentMood, target, 0.5 × deltaTime)
```

At maximum rate, traversing the full 0–1 range takes approximately 2 seconds.

### Source: TimeOfDay

GameClock provides a normalized time-of-day value. MoodMachine samples a
`TimeOfDayMoodCurve` (AnimationCurve) at that value. The authored curve encodes
the following control points:

| Time       | Hour (approx) | Mood Contribution |
|------------|---------------|-------------------|
| Midnight   | 0:00          | 0.9               |
| Morning    | 8:00          | 0.0               |
| Noon       | 12:00         | 0.05              |
| Golden     | 18:00         | 0.4               |
| Night      | 22:00         | 0.85              |

Intermediate values are determined by the curve's tangent authoring.

### Source: Weather

WeatherSystem reports a discrete weather state. MoodMachine maps each state to
a fixed float contribution:

| Weather State  | Mood Value |
|----------------|------------|
| Clear          | 0.1        |
| Overcast       | 0.3        |
| Rainy          | 0.6        |
| Stormy         | 0.9        |
| Snowy          | 0.4        |
| FallingLeaves  | 0.25       |

Weather changes register instantly as a new source value; smoothing is handled
by the target lerp, not by the weather source itself.

### Source: LivingPlants

LivingFlowerPlantManager reports the average health value across all currently
active (alive) plants, normalized to [0, 1]. This average is submitted as the
"LivingPlants" mood source. If no plants are active, this source contributes 0
(or is removed from the registry — behavior to confirm against implementation).

### Source: AirQuality

AirQuality is a derived source computed from plant stress states. Each plant
contributes differently based on its condition:

- Healthy plant: contributes its full health value
- Stressed plant: contributes `health × 0.3`

The AirQuality source value is the weighted average of all plant contributions
under this formula. A room full of stressed plants produces markedly worse air
quality than the LivingPlants source would suggest in isolation.

### Source: Perfume (Optional Override)

Perfume operates through a separate profile overlay system rather than a simple
registered source. See the Profile System section below.

### Profile System

The MoodMachine always has one **base profile** active. An **override profile**
may be blended in additively on top of the base. Each profile (MoodMachineProfile
ScriptableObject) defines curves mapping mood [0, 1] to output driver values.

**Perfume blending rules:**

- Each perfume spray increments `blendTarget` by 0.334 (allowing approximately
  three sprays to reach full blend at 1.002, clamped to 1.0).
- Multiple sprays of the same perfume stack additively up to the 1.0 cap.
- Applying a different perfume type resets `blendTarget` to 0 and begins
  blending the new profile from scratch.
- The actual blend amount (`blendCurrent`) moves toward `blendTarget` at a rate
  of 0.8 per second, meaning a single spray takes approximately 1.25 seconds
  to reach full influence.

### Atmosphere Outputs

The MoodMachine evaluates all output drivers each frame by sampling the active
profile's curves at `currentMood`. Outputs are applied to their respective
subsystems immediately:

**Lighting:**
- Directional light color: sampled from a gradient (warm tones at low mood,
  cool tones at high mood)
- Directional light intensity: 1.2 at mood 0 → 0.4 at mood 1
- Directional light pitch angle: 50° at mood 0 → 25° at mood 1

**Ambient Light:**
- Ambient color: sampled from `ambientColor` curve

**Fog:**
- Fog color: sampled from curve
- Fog density: 0.001 at mood 0 → 0.03 at mood 1

**Rain:**
- Particle emission rate: 0/sec at mood 0 → 200/sec at mood 1

**Audio:**
- Ambience volume: 0.6 at mood 0 → 0.3 at mood 1
- Weather volume: 0 until mood 0.4, then scales 0.3 → 0.8 between mood 0.4
  and mood 1.0

**Screen:**
- `_MoodColorFilter` shader property applied to screen filter material

**God Rays:**
- God ray rotation angle: sampled from curve

**AtmosphereController (post-process):**
- Bloom intensity: multiplied by 0.25 at high mood (bloom pulls back as mood
  darkens and becomes more atmospheric)
- Film grain intensity: multiplied by 2.5 at high mood
- Post-exposure: shifted by -0.4 at high mood (scene dims)
- Vignette intensity: shifted by +0.15 at high mood

## Formulas

**Target mood:**
```
target = (sum of all active source values) / (count of active sources)
```
- Variables: all source values are floats in [0, 1]
- If count = 0: target = 0
- Example: TimeOfDay=0.9, Weather=0.6, LivingPlants=0.2, AirQuality=0.18
  → target = (0.9 + 0.6 + 0.2 + 0.18) / 4 = 0.47

**Mood smoothing:**
```
currentMood = MoveTowards(currentMood, target, 0.5 × deltaTime)
```
- Maximum rate: 0.5 units/second
- Full traverse (0→1 or 1→0): ~2.0 seconds
- Example at 60fps: step per frame = 0.5 × 0.01667 ≈ 0.00833 units

**Perfume blend increment per spray:**
```
blendTarget = clamp(blendTarget + 0.334, 0, 1)
```
- Sprays to full blend: ceil(1.0 / 0.334) = 3 sprays
- Example: 2 sprays → blendTarget = 0.668 (not yet full)

**Perfume blend smoothing:**
```
blendCurrent = MoveTowards(blendCurrent, blendTarget, 0.8 × deltaTime)
```
- Rate: 0.8 units/second
- One spray to full blend: 0.334 / 0.8 ≈ 0.42 seconds
- Full blend: 1.0 / 0.8 = 1.25 seconds

**AirQuality source:**
```
AirQuality = (sum over all plants: (stressed ? health × 0.3 : health)) / plantCount
```
- Variables: health ∈ [0, 1], stressed is bool derived from plant stress state
- Example: 2 healthy plants (health=0.8), 1 stressed plant (health=0.5)
  → AirQuality = (0.8 + 0.8 + 0.5×0.3) / 3 = (0.8 + 0.8 + 0.15) / 3 = 0.583

## Edge Cases

**No active sources:** Target mood is 0. If this is undesirable at game start,
a default ambient source should be registered before play begins. The system
does not self-initialize a fallback.

**Single source:** Averaging a single value returns that value exactly. No
division anomaly.

**Plant count drops to zero mid-session:** LivingFlowerPlantManager should
either remove the "LivingPlants" source from the registry or submit 0. If the
source remains registered at 0, it still pulls mood down. If removed, it no
longer influences. Confirm which behavior is implemented.

**Perfume change mid-blend:** If the player switches perfume type while a
previous blend is in progress, `blendTarget` resets to 0 and begins climbing
again. The previous override profile fades out as `blendCurrent` descends and
the new profile fades in. There is no cross-fade between two override profiles
simultaneously.

**Three sprays overshoot to 1.002:** `blendTarget` is clamped to 1.0 before
smoothing. No visual artifact from the overshoot.

**Mood stuck near target:** `MoveTowards` will not overshoot. If `currentMood`
is within `0.5 × deltaTime` of target, it snaps exactly to target. This is
visually imperceptible.

**Weather change during stormy rain:** Weather mood jumps to 0.9 immediately
as a source value update. The 2-second smoothing on `currentMood` prevents the
visual outputs from snapping — rain emission and fog ramp up over ~2 seconds.

**Override profile with no base profile assigned:** Behavior is undefined in
the design; implementation should guard against null profile and log a warning.

## Dependencies

**MoodMachine reads from:**
- `GameClock` — normalized time-of-day for TimeOfDay source
- `WeatherSystem` — discrete weather state for Weather source
- `LivingFlowerPlantManager` — plant health averages for LivingPlants and
  AirQuality sources
- `MoodMachineProfile` (ScriptableObject) — curve definitions for all outputs

**MoodMachine writes to:**
- `AudioManager` — ambience volume, weather volume
- `AtmosphereController` — bloom, grain, exposure, vignette multipliers
- `RenderSettings` — fog color, fog density
- Directional light `Light` component — color, intensity, rotation
- Screen filter material — `_MoodColorFilter` property
- Rain particle system — emission rate
- God ray transform — rotation

**Systems that register sources into MoodMachine:**
- `GameClock` (TimeOfDay)
- `WeatherSystem` (Weather)
- `LivingFlowerPlantManager` (LivingPlants, AirQuality)
- `RecordSlot` / `ToneArm` (Music — see record-player.md)
- Perfume system (override profile via blend)

**PSXRenderController** reads AtmosphereController outputs indirectly. Changes
to post-process parameters flow through the volume system.

## Tuning Knobs

| Knob                        | Current Default | Safe Range     | Affects                                      |
|-----------------------------|-----------------|----------------|----------------------------------------------|
| Mood smoothing rate         | 0.5 /sec        | 0.1 – 2.0      | How quickly atmosphere responds to changes   |
| Perfume blend speed         | 0.8 /sec        | 0.2 – 2.0      | How quickly perfume takes hold               |
| Perfume blend increment     | 0.334           | 0.1 – 0.5      | Sprays required to reach full blend (3 now)  |
| Perfume blend cap           | 1.0             | fixed          | Maximum override profile influence           |
| TimeOfDay midnight value    | 0.9             | 0.5 – 1.0      | Night atmosphere intensity                   |
| TimeOfDay morning value     | 0.0             | 0.0 – 0.2      | Morning clarity / brightness                 |
| TimeOfDay golden value      | 0.4             | 0.2 – 0.6      | Golden hour warmth                           |
| Weather: Stormy             | 0.9             | 0.7 – 1.0      | Drama of storm atmosphere                   |
| Weather: Rainy              | 0.6             | 0.4 – 0.8      | Moodiness of rain                            |
| AirQuality stress penalty   | 0.3× health     | 0.1× – 0.6×    | How much stressed plants degrade air quality |
| Fog density max             | 0.03            | 0.01 – 0.1     | Atmospheric thickness at peak mood           |
| Rain emission max           | 200/sec         | 50 – 500       | Visual rain intensity at mood 1              |
| Bloom multiplier at peak    | 0.25×           | 0.1× – 0.5×    | Bloom pullback in dark/moody scenes          |
| Film grain multiplier       | 2.5×            | 1.0× – 4.0×    | Grain intensification at high mood           |
| Post-exposure shift         | -0.4            | -0.1 – -0.8    | Scene darkening at high mood                 |
| Vignette shift              | +0.15           | +0.05 – +0.3   | Edge darkening at high mood                  |
| Directional light intensity | 1.2 → 0.4       | 0.8→0.2 – 1.5→0.6 | Day brightness range                     |
| Light pitch range           | 50° → 25°       | 30°→10° – 70°→40° | Sun angle across mood range              |
| Audio ambience volume       | 0.6 → 0.3       | 0.4→0.1 – 1.0→0.5 | Ambience audibility range                |
| Weather audio threshold     | mood 0.4        | 0.2 – 0.6      | When weather sounds begin to emerge          |

## Acceptance Criteria

1. **Source averaging:** Register three sources with values 0.2, 0.6, 0.8. The
   computed target equals 0.533 (±0.001).

2. **Smoothing rate:** With currentMood=0 and target=1, after exactly 2 seconds
   of simulated deltaTime steps, currentMood equals 1.0.

3. **TimeOfDay mapping:** At game clock time 8:00, the TimeOfDay source
   registers 0.0 (±0.01). At 0:00, it registers 0.9 (±0.01).

4. **Weather mapping:** Setting WeatherSystem to Stormy causes the Weather
   source to read 0.9 within one frame.

5. **LivingPlants averaging:** With two plants at health 0.4 and 0.8, the
   LivingPlants source reads 0.6 (±0.001).

6. **AirQuality stress penalty:** One healthy plant (health=1.0) and one
   stressed plant (health=1.0) produce AirQuality = (1.0 + 0.3) / 2 = 0.65.

7. **Perfume single spray:** One perfume spray sets blendTarget to 0.334.
   After 1.25 seconds, blendCurrent reaches 0.334 (within 0.01).

8. **Perfume three sprays:** Three sprays set blendTarget to 1.0 (clamped).
   After 1.25 seconds, blendCurrent reaches 1.0 (within 0.01).

9. **Perfume type change resets blend:** Spray perfume A (blendCurrent > 0),
   then switch to perfume B. blendTarget resets to 0 and the override profile
   changes to B's profile.

10. **Fog output:** At currentMood=1.0, RenderSettings.fogDensity equals 0.03
    (±0.001). At currentMood=0.0, fogDensity equals 0.001 (±0.0001).

11. **Rain output:** At currentMood=1.0, rain particle emission rate is 200/sec.
    At currentMood=0.0, emission rate is 0.

12. **Weather audio threshold:** At currentMood=0.39, weather AudioMixer volume
    is 0 (or effectively silent). At currentMood=0.41, weather volume is
    non-zero.

13. **No sources registered:** With zero sources, target=0 and currentMood
    moves toward 0.

14. **Atmosphere at peak mood:** At currentMood=1.0, verify AtmosphereController
    receives bloom multiplier=0.25, grain multiplier=2.5, exposure shift=-0.4,
    vignette shift=+0.15.

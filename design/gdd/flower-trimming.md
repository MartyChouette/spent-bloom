---
status: reverse-documented
source: [FlowerTrimmingBridge, FlowerSessionController, FlowerGameBrain, FlowerStemRuntime, FlowerPartRuntime, MeshCutting, IdealFlowerDefinition, XYTetherJoint, FlowerAutoSetup, LivingFlowerPlantManager]
date: 2026-05-20
---

# Flower Trimming

> **Note**: This document was reverse-engineered from the existing implementation.

**Status**: Reverse-documented
**Depends on**: FlowerTrimmingBridge, FlowerSessionController, FlowerGameBrain,
FlowerStemRuntime, FlowerPartRuntime, MeshCutting, IdealFlowerDefinition,
XYTetherJoint, FlowerAutoSetup, LivingFlowerPlantManager

---

## Overview

Flower Trimming is a focused minigame unlocked at the end of a date when the
player earns sufficient affection. The game's main scene loads a dedicated
flower-trimming scene additively, offset 50 meters on the Y axis to avoid
spatial conflict. The player makes cuts using a plane-based mesh slicer, guided
by the ideal parameters defined in `IdealFlowerDefinition` for the active
flower species. Three scoring components — stem length, cut angle, and part
condition — combine into a normalized score that determines how many days the
resulting flower will live in the apartment. A perfect cut earns a flower that
lasts up to 10 days. Originally the system could trigger a hard game-over for
critical failures; this is now a soft-fail: bad scores produce an ugly flower
rather than ending the session.

## Player Fantasy

The player should feel like a florist with something to prove. The flower
arrives from the date with its own imperfections — withered leaves, bent stems
— and the player must decide what to keep, what to cut, and where to cut it.
The act of cutting should feel precise and slightly nerve-wracking: the blade
commits the moment it lands. A clean cut at the perfect angle and length should
feel like a small, private triumph. An imperfect flower that still survives for
a few days is a story: a reminder of how the date went, living on the
windowsill until it quietly fades.

## Detailed Rules

### Session Entry

- Flower Trimming is triggered by `DayPhaseManager` transitioning to the
  FlowerTrimming phase.
- The condition to enter: final date affection >= 30, OR the date character's
  `guaranteeFlower` flag is set.
- `FlowerTrimmingBridge` loads the trimming scene additively with a 50-meter
  Y offset from the main scene origin.
- `FlowerSessionController` orchestrates the session from scene load to
  score submission.

### Cutting Mechanics

Cuts are performed via `MeshCutting.Cut()`:
- A cutting plane is defined by the player's gesture or tool position.
- All mesh triangles are classified relative to the plane: above, below, or
  intersecting.
- Intersecting triangles are split; new vertices are interpolated along the
  plane intersection edge.
- Bone weights are preserved at interpolated vertices to maintain skinned mesh
  integrity.

### Tether Joints

Each flower part that can be cut away is connected to the stem or crown via
`XYTetherJoint`. The joint holds the part in place against physics and player
interaction. Cutting through the joint's region severs it, freeing the part.

A **cut grace window** of 0.15 seconds suppresses joint break detection
immediately after a cut completes, preventing false break events from the
physics impulse of the cut itself.

### Part Classification

`FlowerAutoSetup` assigns parts by name pattern during setup:

| Name Contains | Part Role |
|---|---|
| `stem` | Stem segment |
| `crown` | Crown (bloom head) |
| `leaf` | Leaf |
| `petal` | Petal |

For each part, `IdealFlowerDefinition` specifies: whether the part should be
present, the allowed condition (fresh/withered), whether missing is allowed,
and whether withered is allowed.

### Scoring Components

The session is evaluated by `FlowerGameBrain` after the player finishes all
cuts. Three components contribute to the final score.

#### Component 1 — Stem Length (default weight: 30%)

Measures how close the final stem length is to the character's ideal.

```
score = clamp01(1 - |currentLength - idealStemLength| / hardFailDelta)
```

| Parameter | Default | Wizard Default |
|---|---|---|
| `idealStemLength` | 1.0 | 0.5 |
| `hardFailDelta` | 0.5 | 0.3 |
| `perfectDelta` | 0.05 | 0.05 |

A stem within `perfectDelta` of ideal scores 1.0 (clamped). A stem exactly at
`hardFailDelta` distance from ideal scores 0.0.

#### Component 2 — Cut Angle (default weight: 20%)

Measures how close the cut angle is to the ideal diagonal cut.

```
rawAngle = measured cut angle - angleOffsetDeg (calibration)
delta    = |rawAngle - idealAngle|
score    = clamp01(1 - delta / hardFailDelta)
```

| Parameter | Default | Wizard Default |
|---|---|---|
| `idealAngle` | 45 degrees | 45 degrees |
| `hardFailDelta` | 20 degrees | 45 degrees |
| `angleOffsetDeg` | Calibrated per setup | — |

#### Component 3 — Part Condition (default weight: 50%, distributed)

Each part defined in `IdealFlowerDefinition` is evaluated individually. The
50% total weight is distributed proportionally across each part's
`scoreWeight`.

Per-part score values:

| Situation | Score |
|---|---|
| Condition matches ideal | 1.0 |
| Part is withered and `allowedWithered = true` | 0.5 |
| Part is missing and `allowedMissing = true` | 0.5 |
| Part has wrong condition | 0.2 |
| Part is missing and `allowedMissing = false` | 0.0 |

### Final Score Calculation

```
normalizedScore = sum(componentScore_i × weight_i) / sum(weight_i)
```

All weights sum to 1.0 by design (30% + 20% + 50%). If per-part weights
within Component 3 do not sum to the Component 3 weight, they are normalized
internally.

### Score to Days Alive

```
daysAlive = lerp(minDays, maxDays, normalizedScore)
```

| Parameter | Default |
|---|---|
| `minDays` | 1 |
| `maxDays` | 10 |

A perfect score of 1.0 gives 10 days. The minimum possible score (0.0) gives
1 day. The flower always survives at least one day regardless of how poorly it
is trimmed.

**Guarantee path**: If `normalizedScore >= 0.95` and the result would be `>=
7 days`, the guarantee threshold is considered met for `guaranteeFlower`
purposes.

### Soft-Fail Behavior

The following conditions previously triggered a hard game-over:
- Crown removed from stem.
- Stem too high above `hardFailDelta`.
- Stem too low below `hardFailDelta`.
- Cut angle too far from ideal beyond `hardFailDelta`.
- Required special part removed.

These conditions now produce a score of 0.0 on the affected component (or
0.0 overall), resulting in an "ugly flower" with minimum days alive. The
trimming session always completes; the player always receives a flower to place
in the apartment. `allowGameOver = false` is the controlling flag.

**Crown fail Y-failsafe**: If the crown is detected as removed but the score
state has not resolved within 3 seconds, the system forces a timeout and
scores the crown as missing.

### Result Handoff

`FlowerSessionController` passes the `TrimmedFlowerSnapshot` (geometry, score,
days alive, species) to `LivingFlowerPlantManager.SpawnPlant()`, which
instantiates the flower in the apartment.

## Formulas

### Stem Length Score

```
score_stem = clamp(1 - |currentLen - idealStemLength| / hardFailDelta, 0, 1)
```

**Example — slightly short stem:**
`currentLen = 0.85`, `idealStemLength = 1.0`, `hardFailDelta = 0.5`
```
|0.85 - 1.0| / 0.5 = 0.15 / 0.5 = 0.3
score_stem = clamp(1 - 0.3, 0, 1) = 0.7
```

**Example — stem at fail boundary:**
`currentLen = 0.5`, `idealStemLength = 1.0`, `hardFailDelta = 0.5`
```
|0.5 - 1.0| / 0.5 = 0.5 / 0.5 = 1.0
score_stem = clamp(1 - 1.0, 0, 1) = 0.0
```

### Cut Angle Score

```
score_angle = clamp(1 - |rawAngle - 45| / hardFailDelta, 0, 1)
```

**Example — 10-degree error:**
`rawAngle = 55`, `idealAngle = 45`, `hardFailDelta = 20`
```
|55 - 45| / 20 = 10 / 20 = 0.5
score_angle = clamp(1 - 0.5, 0, 1) = 0.5
```

### Combined Score

```
normalizedScore = (score_stem × 0.30) + (score_angle × 0.20) + (score_parts × 0.50)
```

**Example — good trim:**
`score_stem = 0.9`, `score_angle = 0.8`, `score_parts = 0.85`
```
normalizedScore = (0.9 × 0.30) + (0.8 × 0.20) + (0.85 × 0.50)
               = 0.27 + 0.16 + 0.425
               = 0.855
```

### Days Alive

```
daysAlive = round(lerp(1, 10, 0.855)) = round(8.695) = 9 days
```

## Edge Cases

**Crown removed (soft-fail)**: Crown removal previously ended the session.
Now the crown component scores 0.0 for its part-condition weight. If the crown
carries significant `scoreWeight`, the overall score drops substantially but
the session continues.

**Crown fail Y-failsafe**: If crown removal is detected but `FlowerGameBrain`
has not resolved the score state after 3 seconds, the system forces a timeout
and continues evaluation as if the crown is missing.

**Cut grace window collision with intentional re-cut**: If the player attempts
a second cut within 0.15 seconds of the first (unlikely but physically
possible), the second cut's joint-break events may be suppressed. Re-cuts
this close together are effectively treated as a single cut.

**All parts missing**: If every part is removed and none had `allowedMissing =
true`, all per-part scores are 0.0, Component 3 contributes 0.0, and the
resulting normalizedScore reflects only stem and angle components. The flower
always spawns with at least 1 day alive.

**Ideal stem length of 0 (misconfigured)**: If `idealStemLength = 0`, any
non-zero stem length diverges from ideal immediately. The score will be 0.0
for any remaining stem. This is a data configuration error; no runtime
fallback.

**Scene offset collision**: The 50-meter Y offset assumes the main scene uses
Y values well below 50m. If the main scene extends above Y = 50m (e.g., tall
building exteriors), visual bleed between scenes may occur. This is a level
design constraint, not handled in code.

**`allowGameOver = false` in production**: Re-enabling hard game-over requires
setting this flag per `IdealFlowerDefinition`. All current definitions ship
with `allowGameOver = false`. Any definition authored with `allowGameOver =
true` will reactivate hard fail for that flower species.

**Score below guarantee threshold with `guaranteeFlower` set**: If the date
set `guaranteeFlower`, the flower spawns regardless of score. The guarantee
does not override the score-to-days calculation; a guaranteed session with a
score of 0.3 still produces a short-lived flower.

## Dependencies

| System | Role in This System | This System's Role for It |
|---|---|---|
| `FlowerTrimmingBridge` | Loads trimming scene; passes snapshot to apartment | Receives session result for handoff to `LivingFlowerPlantManager` |
| `FlowerSessionController` | Orchestrates session lifecycle (load → cut → score → exit) | Invokes `FlowerGameBrain` at session end |
| `FlowerGameBrain` | Evaluates all three scoring components; returns `normalizedScore` | Called once per session by `FlowerSessionController` |
| `FlowerStemRuntime` | Tracks current stem length at runtime | Read by `FlowerGameBrain` for stem score |
| `FlowerPartRuntime` | Tracks each part's condition and presence at runtime | Read by `FlowerGameBrain` for part-condition score |
| `MeshCutting` | Performs plane-based mesh split; preserves bone weights | Called by player cut gesture handler |
| `IdealFlowerDefinition` | Defines per-species ideal parameters and part rules | `FlowerGameBrain` reads this as scoring rubric |
| `XYTetherJoint` | Holds parts in place; severs on cut | Joint break drives part-removed events in `FlowerPartRuntime` |
| `FlowerAutoSetup` | Wizard that auto-creates components and `IdealFlowerDefinition` from name patterns | Used in editor; not active at runtime |
| `LivingFlowerPlantManager` | Spawns the resulting flower in the apartment | Receives `TrimmedFlowerSnapshot` from `FlowerTrimmingBridge` |
| `DayPhaseManager` | Triggers trimming phase entry | Dating loop depends on affection check before signaling bridge |

## Tuning Knobs

| Knob | Current Default | Wizard Default | Safe Range | Gameplay Effect |
|---|---|---|---|---|
| Stem weight | 30% | — | 10–50% | Importance of stem length in final score |
| Angle weight | 20% | — | 10–40% | Importance of cut angle in final score |
| Part condition weight | 50% | — | 20–60% | Importance of what the player chooses to remove |
| `idealStemLength` | 1.0 | 0.5 | 0.3–1.5 | Target stem length in world units |
| Stem `hardFailDelta` | 0.5 | 0.3 | 0.2–0.8 | Tolerance before stem score reaches 0 |
| Stem `perfectDelta` | 0.05 | 0.05 | 0.02–0.1 | Distance from ideal for a perfect stem score |
| `idealAngle` | 45 degrees | 45 degrees | 30–60 degrees | Target cut angle |
| Angle `hardFailDelta` | 20 degrees | 45 degrees | 10–45 degrees | Angular tolerance before score reaches 0 |
| `minDays` | 1 | — | 1–3 | Minimum days alive for any trimmed flower |
| `maxDays` | 10 | — | 7–14 | Maximum days alive for a perfect trim |
| Cut grace window | 0.15s | — | 0.05–0.3s | Joint break suppression after cut impulse |
| Crown fail Y-failsafe | 3s | — | 1–5s | Timeout before forcing crown-removed resolution |
| Tether `breakForce` | 800 | — | 400–1500 | Force required to break joint without a cut |
| Tether `spring` | 1200 | — | 600–2000 | Stiffness keeping part aligned to stem |
| Tether `damper` | 60 | — | 20–120 | Oscillation damping on tether |
| Tether `maxDist` | 0.3 | — | 0.1–0.5 | Max displacement before tether hard-clamps |
| `allowGameOver` | false | — | true/false | Re-enables hard fail on critical cut errors |

## Acceptance Criteria

1. **Scene loads additively at Y+50**: Confirm trimming scene root is offset
   exactly 50m on Y from the main scene origin. QA: check scene root transform
   after additive load.

2. **Stem score at ideal**: Cut stem to exactly `idealStemLength`. Confirm
   `score_stem = 1.0`. QA: unit test in `FlowerGameBrain` with mocked
   `FlowerStemRuntime`.

3. **Stem score at hard fail**: Cut stem to `idealStemLength - hardFailDelta`.
   Confirm `score_stem = 0.0`. QA: unit test.

4. **Cut angle score at 45 degrees**: Make a cut at exactly 45 degrees (after
   calibration offset). Confirm `score_angle = 1.0`. QA: unit test.

5. **Part condition — matching ideal**: All parts present and fresh when ideal
   is fresh. Confirm per-part score = 1.0. QA: unit test with mocked part data.

6. **Part condition — missing disallowed**: Required part removed, `allowedMissing
   = false`. Confirm per-part score = 0.0. QA: unit test.

7. **Soft-fail produces flower, not game-over**: Remove the crown. Confirm
   session completes, a flower is spawned in the apartment, and no game-over
   screen appears. QA: manual playtest.

8. **Crown Y-failsafe triggers at 3s**: Remove crown; block score resolution
   artificially for 4 seconds. Confirm the failsafe fires at the 3-second mark
   and the session proceeds. QA: debug flag to suppress score resolution.

9. **Score-to-days mapping**: `normalizedScore = 1.0` → 10 days.
   `normalizedScore = 0.0` → 1 day. `normalizedScore = 0.5` → 5.5 days
   (5 or 6 depending on rounding). QA: unit test `FlowerGameBrain.ScoreToDays`.

10. **Combined score formula**: Given `score_stem = 0.9`, `score_angle = 0.8`,
    `score_parts = 0.85`, confirm `normalizedScore = 0.855`. QA: unit test.

11. **Cut grace window**: Make a cut and query joint break state at 0.05s
    after cut. Confirm no break event has been registered. Query again at 0.20s.
    Confirm break events are now processed. QA: unit test with time mock.

12. **Guarantee path**: Set `guaranteeFlower = true` on a date character. Give
    the player a deliberate score of 0.0. Confirm a flower still spawns in the
    apartment. QA: debug date with forced affection and guarantee flag.

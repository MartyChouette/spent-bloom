---
status: reverse-documented
source: [src/gameplay/DrinkPourManager.cs, src/gameplay/PourDragHelper.cs, src/gameplay/DrinkCutawayUI.cs, src/data/GlassDefinition.cs, src/data/DrinkRecipeDefinition.cs, src/data/DrinkIngredientDefinition.cs, src/data/DrinkGarnishDefinition.cs]
date: 2026-05-20
---

# Drink Making

> **Note**: This document was reverse-engineered from the existing implementation.

**Status**: Reverse-documented
**Depends on**: PourDragHelper, DrinkCutawayUI, ObjectGrabber, DateSessionManager, AudioManager, GlassDefinition, DrinkRecipeDefinition, DrinkIngredientDefinition, DrinkGarnishDefinition

> **Legacy system note**: `SimpleDrinkManager` is the legacy drink implementation
> and is no longer the active system. All mechanics in this document describe
> `DrinkPourManager`, which is the active, shipped system. Do not document or
> reference `SimpleDrinkManager` behavior as current.

---

## Overview

Drink Making is a physical pouring minigame that runs during date Phase 2. The
player picks up a bottle, tilts it over a glass using a drag gesture, and pours
ingredients in layers to build a drink. A cutaway UI panel shows the glass filling
in real time. The finished drink is scored across five dimensions — ingredient order,
layer accuracy, fill level, garnish, and overflow penalty — and the total score
influences the date outcome. Ingredients have physical properties (weight, fizziness,
viscosity) that govern how foam builds and how layers sort. The glass glows during
pouring to signal active state.

## Player Fantasy

The player should feel like a careful, unhurried bartender in their own kitchen.
Tilting a bottle should feel weighty and deliberate. Watching layers settle in the
cutaway view should feel satisfying — a well-made drink is visually legible as a
column of distinct strata. Overflow should feel like a mistake that could have been
avoided, not an arbitrary punishment. A perfectly poured drink should leave the
player with a quiet sense of craft.

## Detailed Rules

### State Machine

`DrinkPourManager` runs a linear state machine:

```
Idle → ChoosingGlass → Pouring → ChoosingServeGlass → Score → Idle
```

- **Idle**: No active drink session. Bottles are grabbable but do nothing special.
- **ChoosingGlass**: Player has picked up a bottle. The system waits for the player
  to position the bottle above a valid glass and confirm selection.
- **Pouring**: Bottle is tilted above the selected glass. Drag input controls tilt
  angle and pour rate. Ingredient liquid flows into the cutaway glass.
- **ChoosingServeGlass**: Pour is complete (player releases or glass is full).
  Player selects the final serving vessel if it differs from the mixing glass.
- **Score**: The completed drink is evaluated and a score is computed.
- **Idle**: Score is reported to `DateSessionManager` and the system resets.

### Pour Drag Input

The pour gesture is handled by `PourDragHelper`. The player clicks and drags
downward on the screen to tilt the bottle:

```
rate = (dragDistance / 200px)²
tiltAngle = rate * 90 degrees
```

- `dragDistance` is the pixel distance dragged from the initial click point.
- `rate` ranges from 0.0 (no drag) to 1.0 (200px or more of drag).
- `tiltAngle` ranges from 0 degrees (upright) to 90 degrees (fully inverted).
- The quadratic curve means small drags produce very little flow; the player must
  commit to pour meaningfully.

Pour rate (liquid volume per second transferred to the glass) is proportional to
`rate` and is further modified by the ingredient's `pourRate` property and the
glass's `fillRateMultiplier`.

### Glass Properties

Each glass type is defined by a `GlassDefinition` ScriptableObject:

| Property          | Description                                                   |
|-------------------|---------------------------------------------------------------|
| `capacity`        | Maximum liquid volume the glass can hold                      |
| `fillStart`       | Y-position in the cutaway where liquid begins to appear       |
| `fillLine`        | Normalized fill level considered "ideal" (default: 0.8)       |
| `foamHeadroom`    | Normalized space above the fill line reserved for foam (0.2)  |
| `fillRateMultiplier` | Scales how quickly this glass fills per unit of pour rate  |

The glass is considered **full** when `liquid + foam >= 1.0` (normalized volume).

### Ingredient Properties

Each ingredient is defined by a `DrinkIngredientDefinition` ScriptableObject:

| Property           | Description                                                        |
|--------------------|--------------------------------------------------------------------|
| `pourRate`         | Volume per second transferred at full tilt (rate = 1.0)           |
| `fizziness`        | 0–1 scalar; higher values generate more foam and accumulate rush penalty |
| `foamRateMultiplier` | Scales foam generation relative to the ingredient's `fizziness` |
| `foamSettleRate`   | How quickly this ingredient's foam collapses over time             |
| `viscosity`        | Affects visual flow speed (cosmetic only)                          |
| `weight`           | Determines layer sorting — heavier ingredients sink below lighter ones |

### Foam Mechanics

Foam volume is computed each frame during a pour:

```
foamDelta = pourRate * foamMultiplier * (1 + fizziness)
```

Where `foamMultiplier` is the ingredient's `foamRateMultiplier`. Foam accumulates
on top of the liquid column.

Between pours, foam settles (collapses) using a weighted average of the settle
rates of all ingredients currently in the glass:

```
settleRate = weightedAverage(foamSettleRate for all poured ingredients,
                             weighted by their volume fraction in the glass)
foam -= settleRate * deltaTime
foam = max(foam, 0)
```

### Rush Penalty

The rush penalty accumulates while a fizzy ingredient is being poured too quickly.
If the player pours a fizzy ingredient (fizziness > 0) at a high rate, the rush
penalty accumulates proportionally to `rate * fizziness`. The penalty feeds into
foam instability, causing extra foam generation. The rush penalty decays when the
player slows or stops pouring. The total accumulated rush penalty is tracked per
session and can contribute to overflow.

### Layer Sorting

When two or more ingredients are present in the glass, they sort by `weight`:
heavier ingredients sink to the bottom, lighter ingredients rise to the top. Layer
order in the cutaway UI reflects this physical sorting. The order a player pours
ingredients determines the initial stacking, but weight determines the final settled
arrangement.

### Garnish

Garnish items (defined by `DrinkGarnishDefinition`) can be added to the completed
drink after pouring. Each correct garnish adds **5 points**, capped at **10 points**
total (maximum 2 garnish points, regardless of recipe).

### Overflow

If the combined volume of liquid and foam exceeds 1.0 (the glass's normalized
capacity):

- The overflow flag is set.
- A penalty of **-15 points** is applied at scoring time.
- Excess liquid and foam are not tracked beyond the 1.0 ceiling — the glass does
  not physically spill in world space, only in the cutaway UI.

### Magnetic Snap During Date Phase 2

During date Phase 2, a bottle magnetic snap activates. When a held bottle is
within **1.2 meters** of a glass (matching the bottle snap range from the Object
Interaction system), it snaps to the pouring position above the glass. This is
the same snap system used by object interaction, not a separate drink-specific snap.

### Glass Glow Shader

The glass uses a shader with a reactive glow state:

- **Idle**: Cyan-biased glow
- **Active pour**: Glow shifts toward green; pulses at **2 Hz**
- The glow returns to cyan-biased idle when pouring stops

## Scoring

### Score Components

The final drink score is a sum of five components, with a maximum possible score
of 100 before overflow penalty:

| Component  | Max Points | Description                                              |
|------------|------------|----------------------------------------------------------|
| Order      | 10         | Correct ingredient pour sequence                        |
| Layer      | 50         | Accuracy of each ingredient's portion size              |
| Fill       | 30         | How close the final fill level is to the ideal (0.75)   |
| Garnish    | 10         | Correct garnish items added (5 pts each, cap 10)        |
| Overflow   | -15        | Applied if liquid + foam exceeds glass capacity         |

**Maximum possible score**: 100 (no overflow)
**Maximum with overflow**: 85

### Order Score

```
if (pouringSequence == recipeSequence):
    orderScore = 10
elif (sequenceIsOneOutOfOrder):
    orderScore = 5
else:
    orderScore = 0
```

"One out of order" means exactly one adjacent pair of ingredients was swapped
relative to the recipe sequence. Any other deviation scores 0.

### Layer Score

Each ingredient in the recipe has an expected portion — a fraction of the total
glass volume. The layer score measures accuracy per ingredient:

```
for each ingredient i:
    accuracy_i = 1.0 - min(abs(actualPortion_i - expectedPortion_i), tolerance) / tolerance
    // tolerance = 0.08 (8% of glass volume)

layerScore = (sum of accuracy_i / ingredientCount) * 50
```

An ingredient poured to within 0.08 of its expected portion scores full accuracy
(1.0) for that ingredient. Beyond 0.08, accuracy falls linearly to 0.

### Fill Score

The ideal normalized fill level is **0.75**. Tolerance is **0.10**.

```
fillError = abs(actualFillLevel - 0.75)
if fillError <= 0.10:
    accuracy = 1.0 - (fillError / 0.10)
else:
    accuracy = 0.0

fillScore = accuracy * 30
```

### Date Outcome Integration

The drink score is passed to `DateSessionManager` as a magnitude value:

```
magnitude = drinkScore / 100.0  // normalized to [0, 1]
```

Date outcome logic:

| Condition                                             | Result  |
|-------------------------------------------------------|---------|
| Liked recipe AND score >= 60                          | Like    |
| Score >= 80 (any recipe preference)                   | Like (override) |
| Disliked recipe                                       | Dislike |
| All other cases                                       | Neutral |

Recipe preference (liked/disliked) is a property of the date character's
`DrinkRecipeDefinition` association, set by `DateSessionManager`.

## Formulas

### Pour Rate

```
dragRate = (dragDistance / 200)²                  // [0, 1]
tiltAngle = dragRate * 90                          // degrees
liquidPerSec = ingredient.pourRate * dragRate * glass.fillRateMultiplier
```

### Foam Generation

```
foamDelta = liquidPerSec * ingredient.foamRateMultiplier * (1 + ingredient.fizziness)
foam += foamDelta * deltaTime
```

### Foam Settling

```
settleRate = sum(ingredient.foamSettleRate * volumeFraction_i for each ingredient i)
foam = max(0, foam - settleRate * deltaTime)
```

### Layer Score (Full)

```
accuracy_i = clamp(1.0 - abs(actual_i - expected_i) / 0.08, 0, 1)
layerScore = (sum(accuracy_i) / n) * 50
```

**Example** (2 ingredients, both within tolerance):
```
actual = [0.40, 0.35], expected = [0.40, 0.35], tolerance = 0.08
accuracy_0 = 1.0 - 0/0.08 = 1.0
accuracy_1 = 1.0 - 0/0.08 = 1.0
layerScore = (1.0 + 1.0) / 2 * 50 = 50
```

### Fill Score (Full)

**Example** (fill level = 0.82):
```
fillError = |0.82 - 0.75| = 0.07
accuracy = 1.0 - (0.07 / 0.10) = 0.30
fillScore = 0.30 * 30 = 9
```

### Total Score Example

Correct order, good layers, poor fill, one garnish, no overflow:
```
orderScore  = 10
layerScore  = 42
fillScore   = 9
garnishScore= 5
overflow    = 0
total       = 66  // magnitude = 0.66 → Like if liked recipe, Neutral otherwise
```

## Edge Cases

- **Pour rate at 200px drag**: `rate = (200/200)² = 1.0`. This is the maximum
  pour rate. Dragging beyond 200px does not increase the rate further — it is
  clamped at 1.0.

- **Overflow with foam only**: If foam alone pushes the glass above 1.0 while
  liquid is still below 1.0, the overflow flag still triggers. Foam counts against
  capacity.

- **Rush penalty and foam spiral**: A highly fizzy ingredient poured at full speed
  can generate enough foam to trigger overflow even at a low liquid fill level.
  Players who slow their pour allow foam to settle before continuing.

- **Weight-sorted layers out of pour order**: If a player pours a heavy ingredient
  after a light one, the heavy ingredient sinks below the light one in the final
  sorted display. This can cause the layer accuracy score to measure against the
  sorted position rather than the pour order. Confirm with implementation which
  ordering the accuracy check uses — pour order or sorted order.

- **Garnish on disliked recipe**: Garnish points are still awarded per correct
  garnish item even if the recipe is disliked. The disliked result is applied
  after score computation — a disliked recipe always yields Dislike regardless of
  total score.

- **Score >= 80 on disliked recipe**: The >= 80 Like override does NOT override
  the disliked recipe Dislike. Recipe preference takes priority. Only liked or
  neutral-preference recipes benefit from the >= 80 override.

- **ChoosingServeGlass with no alternate glass**: If the recipe does not require
  a separate serving vessel, the ChoosingServeGlass state is skipped and the state
  machine advances directly from Pouring to Score.

- **Bottle snapped but player does not tilt**: If the bottle snaps to the pouring
  position above a glass but the player makes no drag gesture (rate = 0), no liquid
  flows. The Pouring state remains active until the player drags or moves the bottle
  out of snap range.

- **Multiple glasses in scene**: The ChoosingGlass state listens for the player to
  position the held bottle above a valid glass. If multiple glasses are present, only
  the glass directly under the bottle at confirmation time is selected as the target.

- **Score below zero**: Overflow (-15) can only reduce the score to a minimum of
  85 - all other component scores. The score is not clamped — if all other
  components score 0 and overflow triggers, the total is -15. `DateSessionManager`
  must handle negative magnitude input gracefully.

## Dependencies

- **PourDragHelper**: Converts screen-space drag input into `rate` and `tiltAngle`
  values consumed by `DrinkPourManager` each frame.
- **DrinkCutawayUI**: Renders the real-time cross-section view of the glass showing
  liquid layers, foam, and fill level. Reads layer data from `DrinkPourManager`.
- **ObjectGrabber**: Manages the physical bottle grab and the magnetic snap to the
  glass position. `DrinkPourManager` listens for ObjectGrabber snap events to
  transition from ChoosingGlass to Pouring.
- **DateSessionManager**: Receives the final magnitude (score/100) and the
  like/dislike recipe flag. Applies the outcome to the date result. Also provides
  Phase 2 timing — drink making is only active during Phase 2.
- **AudioManager**: Plays pour sounds, fizz sounds, overflow sounds, and the scoring
  cue. Triggered by state transitions and pour rate changes.
- **GlassDefinition**: ScriptableObject defining glass capacity, fill levels, and
  foam headroom. One instance per glass type.
- **DrinkRecipeDefinition**: ScriptableObject defining ingredient sequence, expected
  portions, and garnish requirements. Owned by the date character's configuration.
- **DrinkIngredientDefinition**: ScriptableObject per ingredient with pour physics
  and foam properties.
- **DrinkGarnishDefinition**: ScriptableObject per garnish type, mapping garnish
  items to their point value and recipe compatibility.

**Reverse dependencies** (systems that depend on Drink Making):

- `DateSessionManager` reads the drink score magnitude to determine Phase 2 outcome.
- `ObjectGrabber` has a bottle-specific snap path activated during date Phase 2 by
  `DrinkPourManager`.

## Tuning Knobs

| Parameter              | Current Value | Safe Range    | Affects                                                      |
|------------------------|---------------|---------------|---------------------------------------------------------------|
| Drag sensitivity       | 200 px        | 100 – 400     | How far the player must drag to reach full pour rate          |
| Pour curve exponent    | 2 (quadratic) | 1 – 3         | Linearity of drag-to-pour-rate mapping                        |
| Layer tolerance        | 0.08          | 0.04 – 0.15   | How precisely each ingredient portion must match recipe        |
| Ideal fill level       | 0.75          | 0.60 – 0.90   | Target fill for maximum fill score                           |
| Fill tolerance         | 0.10          | 0.05 – 0.20   | How close to ideal fill must be for score                    |
| Max layer score        | 50            | 30 – 60       | Weight of portion accuracy in total score                    |
| Max fill score         | 30            | 20 – 40       | Weight of fill level accuracy in total score                 |
| Max order score        | 10            | 5 – 20        | Weight of pour sequence correctness                          |
| Max garnish score      | 10            | 5 – 15        | Weight of correct garnish (per-garnish: 5 pts)               |
| Overflow penalty       | -15           | -5 to -25     | Punishment for overflowing the glass                         |
| One-out-of-order score | 5             | 2 – 8         | Partial credit for near-correct pour sequence                |
| Like threshold (score) | 60 (liked)    | 40 – 80       | Minimum score for Like on a liked recipe                     |
| Like override (score)  | 80            | 60 – 90       | Score at which outcome is Like regardless of recipe pref.    |
| Glass glow pulse rate  | 2 Hz          | 1 – 4         | Pulse speed of glass glow during active pour                 |
| Bottle snap range      | 1.2 m         | 0.6 – 2.0     | Distance at which bottle snaps to glass pouring position     |
| Glass fill line        | 0.8           | 0.6 – 0.95    | Normalized fill at which "full" is indicated in cutaway UI   |
| Glass foam headroom    | 0.2           | 0.05 – 0.35   | Fraction of glass volume reserved for foam above fill line   |

## Acceptance Criteria

1. **Drag-to-rate quadratic**: Drag 100px. Verify `rate = (100/200)² = 0.25`. Drag
   200px. Verify `rate = 1.0`. Drag 300px. Verify `rate` is still clamped at 1.0.

2. **Tilt angle**: At `rate = 0.25`, verify `tiltAngle = 22.5 degrees`. At
   `rate = 1.0`, verify `tiltAngle = 90 degrees`.

3. **Order score — correct sequence**: Pour two ingredients in recipe order. Verify
   `orderScore = 10`.

4. **Order score — one out of order**: Pour two ingredients in reversed order.
   Verify `orderScore = 5`.

5. **Order score — multiple out of order**: Pour three ingredients in an order that
   differs from recipe by two positions. Verify `orderScore = 0`.

6. **Layer score — within tolerance**: Pour an ingredient to exactly its expected
   portion. Verify `accuracy = 1.0` and it contributes fully to `layerScore`.

7. **Layer score — beyond tolerance**: Pour an ingredient 0.09 beyond its expected
   portion (outside 0.08 tolerance). Verify `accuracy = 0.0` for that ingredient.

8. **Fill score — at ideal**: Pour to exactly 0.75 fill. Verify `fillScore = 30`.

9. **Fill score — at tolerance edge**: Pour to 0.85 fill (0.10 above ideal). Verify
   `fillScore = 0` (at tolerance boundary, accuracy = 0).

10. **Overflow penalty**: Fill the glass past 1.0 (liquid + foam). Verify `-15` is
    applied to the total score and the overflow flag is set.

11. **Garnish cap**: Apply 3 correct garnish items (15 pts at 5 pts each). Verify
    garnish score is capped at 10.

12. **Date outcome — Like override**: Configure a neutral-preference recipe. Score
    exactly 80. Verify the date outcome is Like.

13. **Date outcome — Dislike overrides score**: Configure a disliked recipe. Score
    95. Verify the date outcome is Dislike (not Like).

14. **Date outcome — liked recipe at 59**: Configure a liked recipe. Score 59.
    Verify the date outcome is not Like.

15. **State machine — full pass**: Start Idle, grab bottle (ChoosingGlass), snap
    to glass (Pouring), pour and release (ChoosingServeGlass if applicable, then
    Score), verify Idle is restored. Verify `DateSessionManager` receives the score.

16. **Foam settling**: Pour a fizzy ingredient, then stop pouring. Verify foam
    volume decreases over time at a rate consistent with the ingredient's
    `foamSettleRate`.

17. **Layer sorting by weight**: Pour a light ingredient, then a heavy ingredient.
    Verify the cutaway UI shows the heavy ingredient below the light ingredient
    after settling.

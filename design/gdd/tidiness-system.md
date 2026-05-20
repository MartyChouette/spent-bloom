---
status: reverse-documented
source: [src/gameplay/TidyScorer.cs, src/gameplay/CleaningManager.cs, src/gameplay/DailyMessSpawner.cs, src/gameplay/ApartmentStainSpawner.cs, src/gameplay/ReactableTag.cs, src/gameplay/PlaceableObject.cs]
date: 2026-05-20
---

# Tidiness System

> **Note**: This document was reverse-engineered from the existing implementation.

**Status**: Reverse-documented
**Depends on**: CleaningManager, PlaceableObject, ReactableTag, DailyMessSpawner, ApartmentStainSpawner, TidyScorer

---

## Overview

The Tidiness System scores how clean and organized the apartment is across three
spatial areas: Kitchen, Living Room, and Entrance. Each area receives a score
between 0.0 and 1.0 built from four weighted sub-scores: surface stains, misplaced
objects, ambient smell, and floor clutter. The overall apartment tidiness score is
the unweighted average of the three area scores. This score feeds into Phase 1 of
the date sequence, where it determines whether the date's first impression of the
apartment is positive, neutral, or negative. Each morning, a spawner randomizes mess
in the Entrance (shoes, coats, hats), and a separate spawner adds surface stains
throughout the apartment.

## Player Fantasy

The player should feel the satisfaction of incremental restoration — wiping away a
stain, returning a misplaced shoe to its home, clearing the smell of yesterday's
food. The apartment should feel like a living space that generates its own entropy.
No single act of cleaning should fix everything; the work is cumulative and quiet.
The scoring should reward players who notice overlooked details (a smell behind a
closed drawer) as much as those who address the obvious mess.

## Detailed Rules

### Scoring Areas

Three areas participate in tidiness scoring. Each is defined by a world-space
center and size extent (used for spatial assignment of objects and stains):

| Area        | Center (x, y, z)  | Size (x, y, z) |
|-------------|-------------------|----------------|
| Kitchen     | (-3.5, 1.5, 1)    | (5, 4, 11)     |
| Living Room | (2, 1.5, 1)       | (6, 4, 11)     |
| Entrance    | (0, 1.5, -6)      | (12, 4, 3)     |

Objects and stains are assigned to whichever area's bounds contain their world
position. If a position falls outside all areas, it is excluded from scoring.

### Overall Tidiness Formula

```
overallTidiness = (kitchenScore + livingRoomScore + entranceScore) / 3
```

Each area score is computed independently by the formula below.

### Per-Area Score Formula

All four signals are weighted equally:

```
areaScore = (stainScore * 0.25)
           + (objectScore * 0.25)
           + (smellScore * 0.25)
           + (clutterScore * 0.25)
```

Weights sum to 1.0. Equal weighting means no single signal dominates — a messy
apartment with clean floors is scored the same as a clean apartment with cluttered
floors.

### Stain Sub-Score

Stains are tracked per-surface by `CleaningManager`. Each surface has a normalized
cleanliness value in [0, 1].

- Cleanliness is reduced when a stain spawns on the surface.
- Cleanliness is restored when the player wipes the stain with the sponge.
- **Sponge wipe radius**: 0.06 UV units per wipe pass.
- **Fully clean threshold**: A surface is considered fully clean when its
  cleanliness value is >= **0.95** (not 1.0 — the last 5% is treated as background
  wear).
- `stainScore` for an area = average cleanliness across all surfaces in that area.

### Object Mess Sub-Score

Each misplaced object in an area contributes a **mess multiplier** to the area's
object mess total. Objects are misplaced when they are not at their designated home
position. The surface the object sits on determines its multiplier (range 1–5x).

```
rawMess = sum of multipliers for all misplaced objects in area
objectScore = 1.0 - min(rawMess / maxExpectedMess, 1.0)
  // maxExpectedMess = 3
```

An `objectScore` of 1.0 means no mess. 0.0 means mess at or beyond the expected
maximum.

### Smell Sub-Score

`ReactableTag` components throughout the apartment carry a `SmellAmount` value.
The smell sub-score for an area is computed from all `ReactableTag` instances whose
world position falls within the area's bounds.

```
totalSmell = sum of SmellAmount for all ReactableTags in area
smellScore = 1.0 - min(totalSmell / smellThreshold, 1.0)
  // smellThreshold = 1.5
```

**Important**: Smell passes through closed drawers and closed cubby doors. A smelly
object inside a closed drawer still contributes its full `SmellAmount` to the area
score. Only removing the object or neutralizing its smell tag removes the contribution.

### Floor Clutter Sub-Score

Floor clutter counts objects that are on the floor and not at their home position.
This is distinct from object mess — a misplaced object on a shelf does not count
as floor clutter.

```
rawClutter = count of objects on floor and not at home in area
clutterScore = 1.0 - min(rawClutter / maxExpectedClutter, 1.0)
  // maxExpectedClutter = 3
```

### Hidden Items and Scoring Interaction

Items placed inside **closed cubbies or drawers** have the following behavior:

- **Not counted** for object mess scoring (treated as put away)
- **Not counted** for floor clutter scoring
- **Still counted** for smell scoring (smell passes through doors)

### Daily Mess Spawner

Each morning, `DailyMessSpawner` shuffles a set of items in the Entrance area.
The affected items are: shoes, coat, and hat. These are randomly repositioned
within the Entrance bounds in ways that deviate from their home positions, ensuring
the Entrance score is degraded each day and must be restored by the player.

### Stain Spawner

`ApartmentStainSpawner` adds surface stains throughout the apartment each in-game
day.

- **Stains per day**: 4
- **Stain source**: randomly selected from `spillPool` (a configured list of stain
  prefabs or definitions)
- Stains are placed on valid stainable surfaces. Each stain reduces the cleanliness
  of its target surface.

### Date Integration

The overall tidiness score feeds into the Phase 1 date judgment with the following
thresholds:

| Score        | Result  |
|--------------|---------|
| >= 0.8       | Like    |
| >= 0.5       | Neutral |
| < 0.5        | Dislike |

This judgment is one component of the overall date outcome and is evaluated once
at the start of the date phase.

### Sponge Physics

The cleaning sponge is a physics object with simulated squish behavior:

- **Spring squish**: The sponge deforms along its contact axis when pressed against
  a surface, driven by a spring joint.
- **Velocity-based deformation**: Deformation magnitude is proportional to contact
  velocity — pressing harder squishes more.
- **Idle breathing wobble**: When the sponge is at rest, a low-amplitude oscillation
  animates it to feel alive.

Sponge physics are visual only and do not affect cleaning rate. The wipe radius
(0.06 UV) is fixed regardless of how hard the sponge is pressed.

## Formulas

### Per-Area Score (Full Expansion)

```
stainScore    = avg(surfaceCleanliness for all surfaces in area)
                  // surfaces with cleanliness >= 0.95 count as 1.0

objectScore   = 1.0 - min(sum(missplacedMultipliers) / 3.0, 1.0)

smellScore    = 1.0 - min(sum(SmellAmount for all ReactableTags in area) / 1.5, 1.0)

clutterScore  = 1.0 - min(count(onFloor AND notAtHome in area) / 3.0, 1.0)

areaScore     = stainScore * 0.25
               + objectScore * 0.25
               + smellScore * 0.25
               + clutterScore * 0.25
```

**Example calculation (Kitchen, moderate mess):**

```
stainScore   = 0.70  (surfaces 30% stained on average)
objectScore  = 0.67  (rawMess = 1.0, 1 - 1.0/3 = 0.67)
smellScore   = 0.50  (totalSmell = 0.75, 1 - 0.75/1.5 = 0.50)
clutterScore = 1.00  (no floor clutter)

areaScore = 0.70*0.25 + 0.67*0.25 + 0.50*0.25 + 1.00*0.25
          = 0.175 + 0.1675 + 0.125 + 0.25
          = 0.7175
  // Overall result: Neutral (>= 0.5, < 0.8)
```

### Overall Apartment Score

```
overallTidiness = (kitchenScore + livingRoomScore + entranceScore) / 3.0
```

**Example**: Kitchen=0.66, LivingRoom=0.85, Entrance=0.40
```
overallTidiness = (0.66 + 0.85 + 0.40) / 3 = 1.91 / 3 = 0.637
  // Date result: Neutral
```

## Edge Cases

- **All surfaces at exactly 0.95 cleanliness**: All surfaces are treated as fully
  clean (1.0). A surface at 0.949 is not yet clean — it contributes 0.949, not 1.0.

- **Smell through closed drawer**: A smelly item placed in a closed drawer is not
  visible to the player but still degrades the smell score for its area. Players
  who close drawers without removing smelly items may be confused by a persisting
  low smell score.

- **Object at area boundary**: If an object's world position sits exactly on the
  boundary of two areas, it is assigned to the area whose bounds check passes first
  (order: Kitchen, Living Room, Entrance). There is no split assignment.

- **Zero surfaces in area**: If an area has no stainable surfaces registered with
  `CleaningManager`, the stain sub-score defaults to 1.0 (perfectly clean) for
  that area. This prevents divide-by-zero.

- **rawMess exceeds maxExpectedMess**: `min(rawMess / 3, 1.0)` clamps the
  denominator — any mess beyond 3x expected multiplier still results in objectScore
  of 0.0. Additional mess does not push the score below zero.

- **DailyMessSpawner with no valid positions**: If the shuffler cannot find valid
  floor positions for all entrance items (e.g., due to large objects blocking the
  area), it places items at their last valid shuffled positions from the previous
  day. If no prior shuffled position exists, items are left at home positions.

- **Stain spawner pool exhausted**: If all stains in `spillPool` have already been
  spawned and are still present (none cleaned), the spawner skips spawning for
  that day rather than stacking duplicate stains on the same surface.

- **Date phase triggered with no scoring areas active**: If `TidyScorer` has no
  registered areas at the moment of Phase 1 evaluation, overall tidiness defaults
  to 0.5 (Neutral). This is a fallback and should not occur in shipped content.

## Dependencies

- **CleaningManager**: Owns per-surface cleanliness values. `TidyScorer` reads
  these values; `CleaningManager` is modified by the player's sponge interactions.
- **PlaceableObject**: Provides `IsAtHome`, `IsOnFloor`, `MissMultiplier`
  (surface multiplier), and the static registry `PlaceableObject.All` for batch
  iteration. `TidyScorer` queries this registry each scoring tick.
- **ReactableTag**: Components on scene objects that carry `SmellAmount`. The
  static registry `ReactableTag.All` is queried by `TidyScorer` for smell scoring.
- **DailyMessSpawner**: Runs once per morning to randomize entrance items. Writes
  to `PlaceableObject` positions — `TidyScorer` reads results on next scoring tick.
- **ApartmentStainSpawner**: Runs once per morning to add surface stains.
  Modifies `CleaningManager` surface values.
- **TidyScorer**: The scoring aggregator. Reads all other systems and computes
  the per-area and overall scores. Exposes the overall score to `DateSessionManager`.

**Reverse dependencies** (systems that depend on Tidiness System):

- `DateSessionManager` reads the overall tidiness score from `TidyScorer` at
  Phase 1 to determine Like/Neutral/Dislike.
- `ApartmentHubCamera` / `MoodMachine` may read tidiness score to adjust ambient
  mood parameters (if connected — confirm with implementation).
- `ApartmentAreaDefinition` provides bounds data to both the camera system and
  `TidyScorer` — shared dependency on the same data source.

## Tuning Knobs

| Parameter              | Current Value | Safe Range   | Affects                                                     | Note                        |
|------------------------|---------------|--------------|--------------------------------------------------------------|-----------------------------|
| Stain weight           | 0.25          | 0.10 – 0.40  | How much surface cleanliness affects the area score         |                             |
| Object mess weight     | 0.25          | 0.10 – 0.40  | How much misplaced items affect the area score              |                             |
| Smell weight           | 0.25          | 0.10 – 0.40  | How much ambient smell degrades the area score              |                             |
| Clutter weight         | 0.25          | 0.10 – 0.40  | How much floor items affect the area score                  |                             |
| maxExpectedMess        | 3             | 1 – 6        | Mess multiplier sum at which objectScore reaches 0.0        |                             |
| Smell threshold        | 1.5           | 0.5 – 3.0    | Total SmellAmount at which smellScore reaches 0.0           |                             |
| maxExpectedClutter     | 3             | 1 – 6        | Floor item count at which clutterScore reaches 0.0          |                             |
| Clean threshold        | 0.95          | 0.85 – 1.0   | Cleanliness value above which surface counts as clean       |                             |
| Sponge wipe radius     | 0.06 UV units | 0.03 – 0.15  | Area of surface cleaned per wipe pass                       |                             |
| Stains per day         | 4             | 1 – 8        | Daily entropy added by stain spawner                        |                             |
| Like threshold         | 0.8           | 0.7 – 0.95   | Minimum overall score for Phase 1 Like result               |                             |
| Neutral threshold      | 0.5           | 0.3 – 0.7    | Minimum overall score for Phase 1 Neutral result            |                             |

## Acceptance Criteria

1. **Stain sub-score**: Spawn one stain on a surface, reducing it to 0.60 cleanliness.
   Verify the stain sub-score for that area is 0.60 (assuming it is the only surface).
   Wipe until cleanliness reaches 0.95. Verify sub-score becomes 1.0.

2. **Object mess clamping**: Place objects with a combined mess multiplier of 6.0
   in one area. Verify objectScore for that area is 0.0 (clamped — does not go
   negative).

3. **Smell through closed drawer**: Place a smelly `ReactableTag` item inside a
   closed drawer. Verify the area's smell sub-score reflects the item's `SmellAmount`.
   Open the drawer, remove the item to a different area. Verify the original area's
   smell sub-score improves.

4. **Floor clutter vs. shelf mess independence**: Place a misplaced object on a
   shelf (not on the floor). Verify it contributes to objectScore but not
   clutterScore. Place a different misplaced object on the floor. Verify it
   contributes to both objectScore and clutterScore.

5. **Hidden item exemption**: Place a misplaced object in a closed cubby. Verify
   it does not contribute to objectScore or clutterScore. Verify it does contribute
   to smellScore if it has a SmellAmount.

6. **Three-area average**: Set Kitchen=0.9, LivingRoom=0.9, Entrance=0.1 via test
   fixture. Verify overallTidiness = (0.9 + 0.9 + 0.1) / 3 = 0.633.

7. **Phase 1 Like threshold**: Configure scores so overallTidiness = 0.81. Trigger
   Phase 1. Verify date reaction is Like.

8. **Phase 1 Dislike threshold**: Configure scores so overallTidiness = 0.49. Trigger
   Phase 1. Verify date reaction is Dislike.

9. **Daily mess spawner**: Start a new in-game day. Verify that shoes, coat, and
   hat in the Entrance area are in new positions that differ from their home positions
   and from their prior-day positions.

10. **Stain spawner daily limit**: Remove all stains (full cleanliness). Advance
    one in-game day. Verify exactly 4 new stains appear, each from `spillPool`.

11. **Area boundary assignment**: Place an object at the exact boundary between
    Kitchen and Living Room. Verify the object is assigned to exactly one area
    (no double-counting) and that the assignment is deterministic (same result on
    repeated evaluations).

---
status: design-spec
date: 2026-05-20
---

# Character Arcs

## Overview

Seven date characters — Paris, Livii, Clover, Lily, Sage, Psychic, and Sterling
— each have a personal arc that the player advances through repeated successful
dates. A successful date is one where final affection reaches at least 30 out of
100, earning the player a flower trimming session. Each trimmed flower becomes a
living plant in the apartment. Three living plants grown from the same character's
flower complete that character's arc. The arc system gives the 7-day calendar a
long horizon beyond any single date: the player is building relationships in
parallel, tending plants that are themselves evidence of those relationships, and
working toward arc-completion moments that mark the emotional high points of the
game.

## Player Fantasy

The player should feel the slow warmth of something becoming familiar. The first
date with a character is a careful, slightly anxious first impression — reading
the newspaper ad, inferring preferences, arranging the apartment for a stranger.
By the third date the player knows this person: their flower, their quirks, what
music makes them relax. Arc completion should feel like a small, private
milestone — the recognition that this stranger has become someone the apartment
was partly shaped around. The three plants on the windowsill are proof. They
were alive at the same time, briefly, before they faded.

## Detailed Rules

### The Seven Characters

| Character | Notes |
|---|---|
| Paris | — |
| Livii | — |
| Clover | — |
| Lily | — |
| Sage | — |
| Psychic | — |
| Sterling | — |

Each character is defined by a ScriptableObject (or equivalent data profile)
containing:

- **Liked/disliked item tags** — drives Phase 3 (Reveal) reactions.
- **Mood preferences** — `preferredMoodMin` and `preferredMoodMax`; used by the
  dormant mood-multiplier system and future mood-matching features.
- **Drink preferences** — a liked recipe and a disliked recipe; drives Phase 2
  (Kitchen) reactions.
- **Perfume preferences** — intensity threshold for Like/Neutral/Dislike
  evaluation in Phase 1.
- **Reaction strength multiplier** — a per-character scalar applied to all
  affection deltas, making some characters more or less emotionally responsive.
- **Flower species** — the specific flower they bring on a successful date, which
  becomes the living plant in the apartment.
- **`guaranteeFlower` flag** — when set, the flower trimming phase loads
  regardless of final affection score. The tutorial character uses this flag.

### What Counts as a Successful Date

A date is successful when the final affection score is **>= 30** (starting from
50 out of 100). A successful date unlocks the flower trimming session. Arc
progress is tied to the trimming session completing, not merely to the affection
threshold being crossed: the player must actually receive and trim the flower for
it to count toward the arc.

A date with `guaranteeFlower = true` always produces a flower and always counts
as a successful date, regardless of final affection.

### Arc Progression Model

Arc progress for each character is tracked in `DateHistory` as a count of
successful dates (trimming sessions completed) with that character.

```
arcProgress[character] = count of completed trimming sessions with that character
arcComplete[character] = arcProgress[character] >= 3
```

Each successful date produces one living plant in the apartment from that
character's flower species. The three plants needed for arc completion are the
same three plants that physically exist (or existed) in the apartment. Arc
completion is evaluated the moment the third trimming session for a character
ends and the plant is spawned.

### What Happens When an Arc Completes

[TO BE DESIGNED]

Options for consideration:

**Option A — Narrative epilogue.** At the Evening phase following the
third successful date, before the day closes, a short character-specific scene
plays: a postcard, a letter, or a brief piece of dialogue that closes their
story. No mechanical reward. The moment is the reward.

**Option B — Apartment change.** Arc completion permanently changes something
in the apartment tied to that character — a framed photo, an object they left
behind, a change to the ambient art. The apartment accumulates these changes
across all seven arcs, making a fully completed apartment a visible record of
every relationship.

**Option C — Combination.** A brief narrative scene at Evening plus a small
permanent apartment change. The scene plays once; the apartment change persists.

**Option D — Unlock only.** Arc completion is recorded in `DateHistory` with an
`arcComplete` flag but no immediate content plays. The flag is reserved for use
by a future epilogue or ending condition.

Recommended: **Option C**, with the narrative scene authored per character by
the narrative director and the apartment change implemented as a
`PlaceableObject` variant flagged as `arcReward`. This separates the writing
task (7 scenes) from the art task (7 objects) and gives both teams clear
deliverables.

The arc completion event fires at Evening phase, after the results screen but
before the day fully closes. See `dating-loop.md` — the edge case "Three-arc
completion on the same day" confirms this sequencing.

### What Happens When a Date Fails

A date fails when final affection is below 30 (and `guaranteeFlower` is not
set). No flower trimming session is loaded. No plant is spawned. No arc progress
is recorded for that date. The day proceeds directly from DateInProgress to
Evening. The results screen still shows the final grade (C, D, or the lower
bounds of the grading table).

A failed date is not punitive beyond the missed opportunity. The character may
call again; the player gets another chance.

### Retry Policy — Can You Repeat a Failed Date?

[TO BE DESIGNED]

The game's scheduling system (`DayManager`, newspaper ads) rotates characters so
no character repeats until all 7 have appeared at least once. After the full
rotation completes, characters may repeat in a second (or later) cycle.

**Questions to resolve:**

1. **Does a failed date count toward the rotation?** Under the current
   implementation (per `dating-loop.md`), `DateHistory` records no entry for
   days where the phone was not answered, but does record entries for dates that
   were played and failed. The scheduling pool logic needs to specify: does a
   failed appearance exhaust that character's slot in the current rotation, or
   can they reappear sooner?

2. **Newspaper ad repeat system.** The newspaper ad is the only mechanism by
   which the player knows who is calling. If a character can call back after a
   failed date before the rotation completes, the ad system must support
   inserting a retry appearance. This requires a design decision on whether
   retry appearances interrupt the normal rotation or queue after it.

Recommended approach pending design decision: **Failed dates exhaust the
character's slot in the current rotation.** The character rejoins the pool in
the next rotation cycle. This keeps the scheduling simple, preserves the "all 7
before repeats" guarantee, and frames failed dates as a real cost without
removing all agency — the player will see the character again, just not
immediately.

### Maximum Dates per 7-Day Cycle

The game runs on a 7-day calendar. Each day has one potential date (one phone
call, one answer opportunity). The maximum number of dates in a single 7-day
cycle is therefore **7** — one per day, assuming the player answers the phone
every morning.

Practical maximum per character in one cycle: **1** (since each character
appears at most once per rotation). Reaching 3 successful dates with a single
character requires at least 3 separate calendar cycles where that character
appears and the date succeeds.

**Minimum calendar length to complete one arc:** 3 rotations × 7 days = 21
in-game days, assuming the character appears each rotation and the date succeeds
each time.

[TO BE DESIGNED — Does the calendar loop infinitely, or does the game end after
7 days? If the game ends after one 7-day cycle, arc completion for most
characters is impossible. Clarify the intended run length with narrative
director.]

### How DateHistory Tracks Arc Progress

`DateHistory` is the persistence layer for all date outcomes. For character arc
tracking it must store, at minimum, per character:

| Field | Type | Description |
|---|---|---|
| `successfulDates` | int | Count of completed trimming sessions with this character |
| `totalDates` | int | Total dates played with this character (successful + failed) |
| `arcComplete` | bool | True when `successfulDates >= 3` |
| `lastDateDay` | int | Day index of the most recent date with this character |
| `dateLog` | List\<DateRecord\> | Ordered list of all date outcomes (grade, affection, flowerEarned, dayIndex) |

The existing `DateHistory` implementation (per `dating-loop.md`) records grade,
affection, flower flag, and day index per date. The `successfulDates` count
should be derived from the log rather than stored separately, to keep the data
consistent: `successfulDates = dateLog.Count(d => d.flowerEarned)`.

Arc completion check runs once per successful trimming session:

```
if (!arcComplete[character] && successfulDates[character] >= 3):
    arcComplete[character] = true
    QueueArcCompletionEvent(character)
```

The arc completion event is dequeued at the Evening phase transition.

### What Triggers "Stops Calling Back"

[TO BE DESIGNED]

The brief says some characters stop calling back as an implied consequence of
failed dates. This is not implemented in the current codebase (per
`dating-loop.md`, the scheduling pool simply rotates without dropout). Several
design options:

**Option A — Threshold dropout.** After N consecutive failed dates with a
character (e.g., 2 in a row), they stop appearing in the scheduling pool. They
do not call again. The arc becomes permanently incomplete for that run.

**Option B — Single dropout.** A single failed date removes the character from
the pool permanently. High-stakes, unforgiving.

**Option C — No dropout.** Characters never stop calling. Failed dates are
purely an opportunity cost. The arc always remains completable.

**Option D — Soft dropout with re-entry.** After a failed date, the character's
pool weight is reduced (they are less likely to appear). They can still call but
do so less frequently. After a successful date they return to full weight.

**Option E — Story-triggered dropout.** Dropout is authored per character as a
narrative beat rather than a mechanical threshold. Some characters are more
forgiving than others. The narrative director controls which characters drop out
and under what conditions.

Recommended: **Option E**, since the seven characters likely have distinct
personalities that should determine their tolerance for a bad date. A single
mechanical threshold applied to all seven would flatten that variety. Option E
also gives the narrative director direct control over emotional stakes per
character without requiring code changes — dropout conditions are data on the
character profile, not a global system rule.

Implementation note: If dropout is implemented, `DateHistory` needs a
`droppedOut` flag per character. The scheduling system checks this flag before
adding a character to the rotation pool.

## Formulas

### Arc Completion Condition

```
arcComplete(character) = (count of dates where flowerEarned == true) >= 3
```

### Successful Date Condition

```
successful = (finalAffection >= 30) OR (guaranteeFlower == true)
```

### Arc Completion Day (Earliest Possible)

Assuming a calendar cycle of exactly 7 days, one appearance per character per
cycle, and all dates succeeding:

```
earliestArcCompletionDay = 3 × cycleLength + dayOfFirstAppearance
```

Where `cycleLength = 7` for the standard game calendar. A character appearing on
day 2 of their first cycle could complete their arc on day 2 of cycle 3
(day 16), assuming no scheduling gaps.

**Example — Paris appears on day 3 of each cycle:**
```
Cycle 1, Day 3: successful date #1
Cycle 2, Day 3: successful date #2 (day 10 overall)
Cycle 3, Day 3: successful date #3 → arc complete (day 17 overall)
```

### Affection and Grading Reference

(Sourced from `dating-loop.md` for cross-reference.)

| Grade | Affection Threshold |
|---|---|
| S | >= 90 |
| A | >= 75 |
| B | >= 60 |
| C | >= 40 |
| D | < 40 |
| F | Mid-date fail (dormant) |

Flower threshold: affection >= 30 OR `guaranteeFlower = true`.

## Edge Cases

**Arc completion and same-day plant death.** If the third plant spawns on a day
where a previously spawned plant from the same character dies that same morning,
the arc still completes — arc completion is triggered by the third trimming
session completing, not by three plants being simultaneously alive.

**Arc already complete, character calls again.** If `arcComplete = true` for a
character and they appear in a later cycle, the date proceeds normally. There is
no lock preventing further dates with a completed character. Whether additional
successful dates have any effect is [TO BE DESIGNED] — options: they simply add
more plants with no arc significance, or they are suppressed from the scheduling
pool after arc completion.

**`guaranteeFlower` and arc progress.** A `guaranteeFlower` date always
produces a flower, and that flower's trimming session always counts toward the
arc. This means a tutorial-character-style character with `guaranteeFlower` set
can advance their arc even on dates where the player performs poorly. Whether
this is intended depends on which character carries the flag — confirm with
narrative director.

**Player earns flower but abandons trimming session.** If the trimming scene
loads but the player somehow exits before the session resolves, no snapshot is
submitted to `LivingFlowerPlantManager` and no plant is spawned. No arc progress
should be recorded. The `DateHistory` entry for that date should set
`flowerEarned = false`. [TO BE CONFIRMED — current implementation does not
document an abort path for the trimming scene.]

**All 7 arcs complete.** If all seven characters reach `arcComplete = true`, no
specific game state change is documented. This is [TO BE DESIGNED] — likely the
intended ending condition or the trigger for a credits/epilogue sequence.

**Character dropped out before arc completes.** If dropout is implemented
(Option A or B from the Detailed Rules above) and a character drops out after 1
or 2 successful dates, their arc is permanently incomplete for that run. The
living plants from prior successful dates remain in the apartment and continue
their normal lifecycle. No special visual treatment for an abandoned arc is
currently designed.

**Three successful dates in three consecutive days (speed run).** Scheduling
prevents this for most characters (no repeats before full rotation) but is
theoretically possible if the calendar loops and the same character appears in
days 7, 14, and 21. This is valid play and the arc should complete normally.

**Phone unanswered for an entire cycle.** If the player does not answer the
phone for all 7 days of a cycle, no `DateHistory` entries are created, no arc
progress is made, and the scheduling pool resets into the next cycle as if the
prior cycle completed normally. Characters do not accumulate "missed calls."

## Dependencies

| System | Role in Character Arcs | Character Arcs' Role for It |
|---|---|---|
| `DateHistory` | Persists arc progress, successful date counts, `arcComplete` flags per character | Primary consumer; writes and reads arc state |
| `DayManager` | Schedules which character appears per day via newspaper ad | Reads dropout flags and rotation state from DateHistory |
| `DayPhaseManager` | Sequences Evening phase where arc completion event fires | Must receive arc completion signal to sequence the arc content |
| `FlowerTrimmingBridge` | Determines whether trimming session loads (affection >= 30 or guaranteeFlower) | Trimming session completion is the trigger for arc progress increment |
| `LivingFlowerPlantManager` | Spawns the living plant from each trimming session | Arc completion check fires after SpawnPlant() confirms the plant is in the apartment |
| `PhoneController` | Delivers the date call; player answering commits the date | Scheduling pool (including dropout state) is upstream of the call |
| `GameClock` | Provides day index for scheduling rotation and DateHistory entries | Arc progress is day-indexed for chronological reconstruction |
| Character ScriptableObjects | Define liked/disliked tags, mood prefs, drink prefs, guaranteeFlower, flower species | Character arc system reads these profiles; does not write to them |
| Narrative Director (authoring) | Authors arc completion scenes and dropout conditions per character | Arc system fires the event; content is authored externally |

**Reverse dependencies** (systems that depend on Character Arcs):

- `LivingFlowerPlantManager` — the species of plant in each slot reflects which
  character's flower it came from; arc completion state may influence visual
  treatment of plants (not yet designed).
- Future ending/credits system — likely reads `arcComplete` flags from
  `DateHistory` to determine whether all arcs are done.

## Tuning Knobs

| Knob | Current Value | Safe Range | Gameplay Effect |
|---|---|---|---|
| Successful dates to complete arc | 3 | 2–5 | Total relationship depth required; lower = faster arcs, higher = longer investment |
| Flower affection threshold | 30 | 20–50 | How often a date produces a flower; lower = more forgiving arc progression |
| Calendar cycle length | 7 days | 5–10 | How often each character can appear; shorter cycles = faster arc completion |
| Dropout threshold (if implemented) | [TO BE DESIGNED] | 1–3 consecutive failures | How unforgiving the "stops calling back" mechanic is |
| guaranteeFlower characters | Tutorial character only (currently) | 0–7 | How many characters bypass the affection threshold entirely |
| Arc completion timing | Evening phase, after results screen | — | When arc content plays relative to date resolution |

## Acceptance Criteria

1. **Three successful dates complete arc.** Play three dates with any single
   character, each ending with affection >= 30 and a trimming session completed.
   After the third trimming session, confirm `DateHistory.arcComplete[character]
   = true` and the arc completion event fires at Evening. QA: step through with
   one character across three separate days.

2. **Failed date records no arc progress.** Play a date that ends with affection
   < 29 (below flower threshold) and `guaranteeFlower = false`. Confirm
   `DateHistory.successfulDates[character]` does not increment. Confirm no
   trimming scene loads. QA: force affection to 25 via debug menu; observe
   DateHistory state after Evening.

3. **`guaranteeFlower` bypasses threshold.** Set `guaranteeFlower = true` on a
   character. Play a date and deliberately keep affection below 30. Confirm
   trimming scene loads. Confirm `successfulDates` increments after the session.
   QA: debug date with forced low affection and guarantee flag active.

4. **Arc completion fires at Evening, not during trimming.** On the third
   successful date, confirm the arc completion event does not fire during the
   trimming scene or the flower spawn. Confirm it fires at the Evening phase
   transition. QA: add event log to arc completion trigger; verify timestamp
   against phase log.

5. **`successfulDates` count is consistent with `dateLog`.** At any point in the
   game, `successfulDates[character]` must equal the count of entries in
   `dateLog[character]` where `flowerEarned == true`. QA: automated consistency
   check run after each date resolves.

6. **No character repeats before full rotation.** Over 7 consecutive days with
   the phone answered each day, all 7 characters appear exactly once. Arc
   progress for any given character advances at most once per 7-day cycle. QA:
   record character names from newspaper ads over 14 days; confirm each appears
   twice total (once per 7-day cycle) with no early repeats.

7. **Arc completion at arc 3 = exactly 3, not 4.** If a player has 2 successful
   dates with a character and the third date is successful, arc completes. If the
   `arcComplete` flag is somehow already set, a fourth successful date does not
   re-trigger the arc completion event. QA: unit test arc completion logic with
   input counts of 2→3 (should fire) and 3→4 (should not fire again).

8. **Phone unanswered, no arc state change.** Let the morning phase expire
   without answering the phone. Confirm no `DateHistory` entry is created for
   that day and no character's `successfulDates` or `totalDates` increments. QA:
   observe DateHistory state before and after an unanswered morning.

9. **Plant spawned after each successful date.** After each successful trimming
   session with any character, confirm a living plant appears in the apartment
   from `LivingFlowerPlantManager`. Confirm the plant's species matches the
   character's defined flower. QA: complete a date, inspect spawned plant
   GameObject for species metadata.

10. **Arc completion event queued, not fired, during trimming.** Verify that if
    arc completion triggers while the trimming scene is still loaded, the
    completion event does not fire until the trimming scene unloads and Evening
    begins. QA: add log breakpoints to arc event queue and Evening phase start.

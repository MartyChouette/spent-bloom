---
status: design-proposal
author: game-designer
date: 2026-05-19
review-required: creative-director, narrative-director, lead-programmer
---

# Arc Completion Rewards

> [DESIGN PROPOSAL] — This document presents design options and a recommended
> approach for the creative director to review. Nothing here is a final spec
> until sections marked [OPTION — RECOMMENDED] are accepted and the status
> is changed to `design-spec`.

---

## Overview

When a player completes three successful dates with any of the seven characters
— Paris, Livii, Clover, Lily, Sage, Psychic, Sterling — a layered reward fires
at the Evening phase following the third trimming session. The reward has three
simultaneous outputs: a permanent physical change to the apartment (an arc
object), a narrative beat (a character-authored goodbye), and a minor mechanical
shift (a new perfume unlocked for purchase). When all seven arcs complete, a
final apartment transformation and ending sequence plays. This document defines
the rules for all four of those outputs and offers design options where the
creative direction is still open.

---

## Player Fantasy

Arc completion is the emotional peak of a character relationship. The player
has spent at minimum three in-game weeks thinking about this person — reading
their newspaper ad, arranging the apartment for their preferences, trimming
their flower. Completion should feel like a small, private ceremony: not
triumphant, but warm and slightly melancholy. The character is leaving — or
at least, the active chapter of knowing them is closing.

The MDA target aesthetic for arc completion is **Narrative** (the closure of
a story) layered over **Expression** (the apartment itself as a record of
who Nema has been). The player should look at the apartment after all seven
arcs and see a room that has been shaped by seven specific people. It should
feel full in a way that is slightly more than comfortable — approaching, but
not reaching, a sense of accumulation that is quietly unsettling.

This mechanic primarily serves **Achievers** (a clear milestone reached after
sustained effort) and **Explorers** (the arc object becomes a new interactable
with hidden depth that other characters can discover and remark on).

Self-Determination Theory alignment:
- **Competence**: completing an arc rewards the player's sustained skill at
  reading and hosting each character.
- **Autonomy**: the player chose when and in what order to pursue each arc.
- **Relatedness**: the arc object and narrative beat reinforce the felt
  reality of a relationship with a fictional person.

---

## Detailed Rules

### 1. Arc Completion Trigger

Arc completion fires immediately after `LivingFlowerPlantManager.SpawnPlant()`
confirms the third plant is placed in the apartment, exactly as specified in
`character-arcs.md`:

```
if (!arcComplete[character] && successfulDates[character] >= 3):
    arcComplete[character] = true
    QueueArcCompletionEvent(character)
```

The queued event is dequeued at the start of the Evening phase transition,
after the trimming scene unloads but before the results screen closes.
Sequencing:

```
[FlowerTrimming phase ends]
  → scene unloads
  → QueuedArcCompletionEvent is dequeued
  → NarrativeBeat plays (see Section 3 — Narrative Reward)
  → NarrativeBeat completes
  → ArcObject spawns in apartment (see Section 2 — Physical Reward)
  → ArcObject spawn animation completes
  → Evening results screen resumes
  → Day closes normally
```

This sequence ensures the player sees the narrative beat with fresh emotional
context (the trimming session just ended, the plant just appeared) before the
results screen collapses the moment into a grade.

If multiple arcs complete on the same day (e.g., a date completes one arc and
the last plant of a second character from a prior day finally spawned), events
are queued in order of which arc's third plant was spawned first that session.
Each arc completion plays sequentially before the results screen resumes.

### 2. Physical Apartment Reward — Arc Object

#### 2a. What the Arc Object Is

Each character leaves behind one permanent object that appears in the apartment
after their arc completes. The object is:

- Unique to that character — it reflects their identity, not a generic trophy.
- Always present and interactable from the moment of spawn (it is a
  `PlaceableObject` with `ReactableTag`).
- Visible to all future date characters during Phase 3, who may react to it
  (see Section 2d).
- Scaled and themed to feel like something the character might plausibly own
  or have made — not a fantasy reward item.

The object must remain in the apartment permanently. It cannot be placed in
the trash zone. It cannot be overwritten by the slot-cycling system (it is
not a living plant — it occupies a dedicated `arcObjectSlot[]` indexed by
character, separate from `_plantSlots[]`).

#### 2b. Recommended Object Concepts per Character

These are design proposals only. The exact visual and thematic form of each
object is a decision for the creative director and art team.

| Character | Proposed Arc Object | Design Intent |
|---|---|---|
| Paris | A small handwritten note, framed, hung on the wall | Paris is precise — the note contains a single line, possibly redacted or partially illegible. Suggests correspondence that almost said something. |
| Livii | A ceramic bowl, slightly misshapen | Livii made this. It holds nothing useful. It is positioned wherever the player last left a plant; its presence implies Livii watched Nema care for something. |
| Clover | A potted cutting — a small plant that is not a flower, does not die, and never changes | Exactly this: a living thing with no lifecycle. A plant outside the normal plant-death system. It should unsettle players who have internalized that plants fade. |
| Lily | A photograph, face-down | The frame is there. The photo is face-down. It can be picked up but cannot be turned over (no valid placement flips it). |
| Sage | A small glass bottle, sealed, with something inside | The contents are not documented. `ReactableTag` description: "you aren't sure what this is." |
| Psychic | A mirror, small, that reflects the wrong thing | A mirror-shaped object whose texture does not reflect the room correctly. The reflection is slightly off — a different time of day, slightly different objects in the background. An art/shader task. |
| Sterling | A business card, pinned to the wall | Sterling left a card. It has a name and a number on it. The number is wrong — one digit off from Nema's own phone number. |

These concepts are designed to accumulate correctly: each individual object
reads as a slightly odd keepsake, but the full set of seven in one apartment
produces a room that feels like it has a secret. The goal is "cozy with a
secret," not haunted. The objects should not read as threatening in isolation.

#### 2c. Object Placement

Arc objects spawn at authored fixed positions in the apartment rather than
using the standard `_plantSlots[]` cycling. Each character has one
pre-authored spawn position defined in a `CharacterArcObjectDefinition`
ScriptableObject. The positions are chosen so that:

- Objects from different characters do not visually crowd each other even
  when all seven are present.
- Each object is visible from at least one Phase 3 camera angle during a
  standard date walkthrough.
- The full set of seven objects, when present simultaneously, creates a
  visible change in the apartment's visual density without blocking navigation.

On spawn, the arc object plays a brief arrival animation (a simple fade-in
or settle-in, authored per object). Duration: 1.0–2.0 seconds. The animation
must complete before the Evening results screen resumes.

**[OPTION A — RECOMMENDED]**: Arc objects spawn at their authored fixed position
and stay there permanently. The player can pick them up and move them (they are
`PlaceableObject`s with standard interaction), but they cannot be discarded.
If moved from their authored position, they remain wherever the player put them.

**[OPTION B]**: Arc objects are static scene objects, not `PlaceableObject`s.
The player cannot interact with them — they are ambient decoration. This is
simpler to implement and removes the risk of the player accidentally deleting
their arc rewards, but it breaks the tactile consistency of the apartment where
everything can be touched.

**[OPTION C]**: Arc objects can be placed in a special "memory box" container
that stores completed arc objects out of view. This would give the player agency
over visual clutter as arcs accumulate, but it weakens the accumulation feeling
that is central to the "Quiet Accumulation" pillar.

Recommendation: **Option A**. The player should be able to hold and examine the
arc object. The inability to discard it is a design choice that enforces the
"Quiet Accumulation" pillar — these things stay. The apartment fills.

#### 2d. Other Characters React to Arc Objects

Each arc object has a `ReactableTag` that can be investigated during other
characters' Phase 3 walkthroughs. Reactions are:

- **Characters who have completed their own arc**: their reaction description
  references the object by name with a knowing note ("she picks it up and
  puts it back carefully").
- **Characters who have not completed their arc**: their reaction is neutral
  curiosity ("there's something on the shelf").
- **One character per object**: one character in the cast has a specifically
  authored reaction that implies they recognize the object or its significance.
  This is determined per-object by the narrative director.

The reaction uses the standard affection delta formula from `dating-loop.md`.
Arc objects have a fixed surface multiplier of 1x (they do not change the
affection math significantly — their value is narrative, not mechanical).

Default reaction valence for arc objects: **Neutral** (no affection change).
A character who has a specific authored reaction for that object may react
with Like or Dislike based on what the narrative director decides.

### 3. Narrative Reward — Goodbye Beat

#### 3a. Form of the Beat

At the Evening phase, before the results screen, a brief character-authored
narrative beat plays. The beat is a short moment authored per character by
the narrative director.

**[OPTION A — RECOMMENDED]**: A letter or postcard appears on-screen — not
a dialogue scene with the character present, but a piece of text the player
reads. The letter is authored to feel like a small ending: not melodramatic,
but specific. Something only this character would write. The letter fades
out after the player clicks (or after a minimum display duration of 4.0
seconds). The framing is an image of the letter/card in Nema's hands; the
character is not visible.

**[OPTION B]**: A brief phone call. The phone rings during Evening. Nema
answers. The character says a single thing. The call ends. No transcript is
shown. The player may or may not parse what was said depending on audio
clarity.

**[OPTION C]**: No dialogue or letter — instead, a newspaper ad change. The
next morning's newspaper contains a different ad from this character: not
asking for a date, but something else. A notice. A thank-you. A classified
ad for something they are selling. This format fits the "quiet, peripheral"
tone of the game but delays the emotional beat by one day.

**[OPTION D]**: A combination. A letter plays at Evening, and the newspaper
ad changes on the following morning. The letter closes the relationship; the
newspaper ad the next day is a small echo.

Recommendation: **Option A** for the primary beat, with **Option D** (adding
the newspaper echo) as an enhancement if narrative bandwidth allows. The letter
format respects the player's reading pace and fits the contemplative tone.
Phone call audio (Option B) is harder to author clearly and may feel
intrusive. The newspaper-only option (Option C) is good but the day-delay
weakens the emotional connection to the trimming session that just completed.

#### 3b. Nema's Behavior After Arc Completion

After the arc completion beat, Nema's behavior subtly shifts for that
character in future interactions:

- If the character calls again (arc is complete, scheduling continues per
  `character-arcs.md`), the newspaper ad for that character changes format:
  still readable as the same character, but slightly different. Something
  has shifted in how they are presenting themselves.
- The phone ring has a slight audio variation (a tuning knob, not a new
  audio asset — a pitch or reverb shift) on days that character calls.
  This is an ambient signal to the player that this character is different
  now without calling it out.

Whether the player continues dating completed-arc characters is their choice.
The mechanics are identical; the atmosphere is not.

### 4. Mechanical Reward — Perfume Unlock

#### 4a. What Unlocks

Each arc completion unlocks one new perfume available for purchase. The
perfume is thematically tied to that character — it is implied (not stated)
that this scent reminds Nema of them.

The seven arc perfumes have the following design constraints:
- Each has an unusual name — not "Rose" or "Cedar" — something slightly
  oblique that rewards players who make the connection.
- Each has a distinct effect on the date evaluation system (different
  `perfumeThreshold` and `likedIntensityRange` values) that makes them
  useful for specific future dates.
- None of them is strictly better than perfumes already available; they
  are lateral options that broaden the player's toolkit.

Perfume unlock is signaled by a brief line of text appearing in the next
morning's newspaper classifieds section (a fictional perfume ad). The player
visits the in-game shop (if one exists) or the perfume collection to find
it available. If no shop system exists, the perfume is simply added to
Nema's collection silently and appears on the shelf.

**[OPEN QUESTION]**: The current design does not document a shop or
acquisition system for perfumes. The creative director and lead programmer
need to confirm how the player acquires items before this reward is
specifiable in full. For now, the unlock is flagged in `DateHistory` as
`perfumeUnlocked[character] = true` and the implementation is deferred
until the acquisition system is designed.

#### 4b. Alternative: Music Unlock

If a perfume system cannot be implemented in scope, an alternative mechanical
reward is a new vinyl record that appears in the apartment. The record plays
music tied to that character. Records function fully within the existing
`RecordPlayer` system (see `record-player.md`). This is lower-implementation-
cost than a new perfume and connects naturally to the game's tactile systems.

The creative director should choose between perfume unlock (richer connection
to the dating system's chemistry) and record unlock (lower cost, directly
physical, integrates with existing systems without new infrastructure).

**[OPTION A]**: Perfume unlock (deferred until acquisition system designed).
**[OPTION B — RECOMMENDED if scope is constrained]**: Vinyl record unlock.
The record appears on a shelf, placed as a `PlaceableObject`. It can be
played on the turntable. Its MoodMachine mood value and character affection
multipliers are authored per-character by the game designer and narrative
director. It does not decay or die. It persists.

#### 4c. Post-Arc Character Behavior

After arc completion, the character does not stop calling. They may continue
to appear in the scheduling rotation. Future successful dates produce plants
with no arc significance, but the dates still earn flowers, still run the
normal loop, and still produce plants.

This is consistent with the "No Dropout After Arc" approach because:
1. The plants from post-arc dates continue to fill the apartment (Quiet
   Accumulation).
2. The arc object and completed arc state change the character's feel
   without removing them from the game.
3. Removing completed characters creates a hard game-length ceiling
   (once all 7 arcs complete, no more dates possible, forcing the ending).
   If the creative director wants a soft ending (player chooses when to
   stop), characters must remain available.

If the creative director wants a hard ending (all 7 arcs complete = game
ends), see Section 5.

### 5. All-Arcs-Complete Reward — The Ending

#### 5a. Trigger

The all-arcs-complete event fires the first time `arcComplete[character] = true`
for all seven characters simultaneously. It is checked after each individual
arc completion event:

```
allArcsComplete = arcComplete.All(a => a.Value == true)
if (allArcsComplete && !endingTriggered):
    endingTriggered = true
    QueueEndingEvent()
```

The ending event is queued immediately after the final arc's completion beat
plays. It fires at the start of the next morning (not the same Evening) to
give the player a night of diegetic time before the world changes.

#### 5b. Apartment Transformation

When all seven arcs are complete, the apartment undergoes a visible change
on the next morning's load.

**[OPTION A — RECOMMENDED]**: All seven arc objects begin subtly glowing or
emitting light — not violently, but enough that the apartment at full
completion has a different ambient luminosity. The MoodMachine registers a
new permanent source called `"ArcResonance"` with a value of 1.0. This shifts
the AtmosphereController outputs toward a specific authored state that feels
"completed" — not brighter or darker necessarily, but different. The PSX
post-processing parameters shift subtly: dither pattern changes, posterize
steps increase slightly, giving the room a flatter, more dreamlike quality.
The apartment looks like a memory of itself.

**[OPTION B]**: The apartment is physically transformed — new objects appear,
the layout shifts, something is wrong. This leans harder into "The Unease"
but risks feeling punitive after the player has invested in relationships.
The creative director should confirm whether the ending should feel like
reward or revelation.

**[OPTION C]**: Nothing changes visually. The ending is purely narrative —
a final letter, or a moment with Nema, or a change in the newspaper that is
the last thing the player reads. The apartment stays exactly as it was.
This is the most understated option and may be the truest to the game's tone.

Recommendation: **Option A** for the apartment change (it rewards visual
attention without requiring new geometry or assets) and **Option C** as the
narrative layer (a final text moment that does not explain what the accumulated
objects mean, but acknowledges them). Together: the room looks slightly
different and Nema reads something for the last time.

#### 5c. Ending Sequence

After the final arc completion beat and the apartment transformation:

1. The morning newspaper contains a final ad. It is not a date request.
   Its content is authored by the narrative director.
2. The phone does not ring. Or: it rings, and if answered, there is only
   static, then a click.
3. The player is given free time to walk through the apartment — no date
   scheduled, no day timer, no objective. The music that plays is the
   combination of whatever records the player has accumulated.
4. After some duration (timed or player-triggered), the screen fades to
   white (consistent with the existing ScreenFade system's white-fade
   convention per `technical-preferences.md`). Credits roll or appear.

The ending does not explain anything. It lets the apartment be the
explanation. The player has filled it themselves.

#### 5d. New Game Plus / Replay

Whether the player can start over from this state is [TO BE DECIDED by
creative director]. Options:
- Hard reset: everything clears, fresh save.
- Soft reset: arc objects remain but plants are gone; Nema keeps the perfumes.
- No reset: the game is one run, one ending. The save persists as a record.

---

## Formulas

### Arc Completion Check

```
arcComplete(character) = (successfulDates[character] >= 3)
successfulDates[character] = count(dateLog[character] where flowerEarned == true)
```

As defined in `character-arcs.md`. No new formula required for the completion
condition itself.

### All-Arcs-Complete Check

```
allArcsComplete = (count of characters where arcComplete == true) == 7
```

Evaluated after each individual `QueueArcCompletionEvent()` call.

### ArcResonance MoodMachine Source (Ending State)

```
arcResonanceValue = 1.0  (constant, permanent once set)
```

This source is registered once and never updated. It shifts the apartment's
average mood contribution permanently upward by `1.0 / (activeSources + 1)`
when no other sources are present. Under typical play conditions (plants,
weather, perfume all registered), its marginal effect is smaller but always
present.

**Example — ending apartment with 3 active sources before ArcResonance:**
```
priorMood = (0.6 + 0.4 + 0.7) / 3 = 0.567
postMood  = (0.6 + 0.4 + 0.7 + 1.0) / 4 = 0.675
```
The mood shifts by approximately 0.1 — noticeable in the AtmosphereController
outputs (a measurable light or color change) without being dramatic.

### Arc Object MoodMachine Contribution (Individual Arcs)

Arc objects do not register a MoodMachine source. They affect atmosphere only
through `ReactableTag` date reactions (affection system) and the aggregate
`ArcResonance` source at game end. This keeps individual arc completion from
noticeably changing the apartment's atmosphere until all arcs are complete.

---

## Edge Cases

**Arc object spawns when all plant slots are occupied.** Arc objects use a
dedicated `arcObjectSlot[]` array separate from `_plantSlots[]`. Slot overflow
in the plant system does not affect arc object placement. The two slot arrays
must never share indices or positions.

**Two arc completions queue on the same Evening.** If two characters' arcs
complete on the same day (possible if the second arc's third trimming session
occurs on the same day as another character's third trimming session — edge
case per `character-arcs.md`), both `QueueArcCompletionEvent()` calls fire,
and both arc beats play sequentially before the Evening results screen resumes.
Both arc objects spawn. The play order is determined by the order in which
the trimming sessions resolved.

**Final arc completion triggers all-arcs-complete.** When the seventh arc's
completion beat finishes, the all-arcs-complete check runs. The ending is
queued for the next morning. The Evening results screen plays normally before
the ending begins. There is no same-day ending.

**Player idles and never reaches all-arcs-complete.** If the player completes
six arcs and then stops answering the phone, the seventh character is never
reached, and the ending never fires. The game continues indefinitely in this
state. This is valid play — the game does not force an ending. The apartment
holds six arc objects and accumulates plants indefinitely.

**Arc object moved into corner or obscured by plants.** The player may
rearrange arc objects using standard `ObjectGrabber` interaction. The arc
object remains valid regardless of position. Future date Phase 3 evaluations
with `ReactableTag` use spatial proximity (distance from character's
investigation path), so an arc object pushed into a corner may not be
investigated. This is acceptable — the object still exists; it is simply
not seen on that visit.

**Arc object's authored spawn position is already occupied by a living plant.**
On spawn, if the arc object's authored position overlaps an existing plant
slot, the arc object spawns at the authored position and the plant is nudged
one grid cell (0.06m) in the nearest unoccupied direction. This prevents
visual overlap without requiring the player to manually move their plants.

**Perfume/record unlock but no acquisition system.** The `perfumeUnlocked`
flag is set in `DateHistory` regardless of whether the acquisition UI exists.
If the shop or acquisition system is added later, it reads this flag. Until
then, the unlock is flagged but has no in-game effect.

**All-arcs-complete during a date session.** The all-arcs-complete check
runs at Evening, never mid-date. If the seventh arc completes while a date
is in progress (not currently possible since arc completion fires post-trimming),
the check is deferred to the next Evening transition. No ending can interrupt
an active date.

**Clover's arc object (immortal plant) and the living-plant system.** The
immortal plant arc object for Clover must not register as a `WaterablePlant`
or `LivingFlowerPlant`. It must not decay, require watering, contribute to
air quality, or produce leaf shed. It is visually similar to a living plant
but is implemented as a static `PlaceableObject` with `ReactableTag` only.
The art team must ensure it does not share prefab components with the living
plant system.

**Lily's photograph and the face-down flip.** The photograph uses the standard
object rotation system (RMB in 45-degree steps per `object-interaction.md`).
To prevent the player from flipping it face-up, one of two approaches:
(a) The photo has no "face-up" valid placement state — the placement
    validator rejects wall-mount in the face-up orientation.
(b) The photo always renders its face-down texture regardless of rotation.
This is an implementation question for the lead programmer to resolve. The
design intent is that the face of the photograph is never revealed to the
player. [CONFIRM WITH CREATIVE DIRECTOR — is this a permanent rule or should
there be a condition under which it can eventually be turned over?]

**Psychic's mirror and PSX rendering.** The "wrong reflection" effect requires
a custom shader or a pre-authored texture. This is outside the scope of this
document and must be delegated to the unity-shader-specialist. The design
intent is documented here; the implementation path is not.

---

## Dependencies

| System | Role in This System | This System's Role for It |
|---|---|---|
| `DateHistory` | Stores `arcComplete[character]`, `perfumeUnlocked[character]`, `endingTriggered` | Primary write target for all arc completion state |
| `DayPhaseManager` | Sequences Evening phase; provides the hook where arc beats play | Arc completion event is dequeued at Evening phase start |
| `LivingFlowerPlantManager` | Spawns arc object via new `SpawnArcObject()` method; manages `arcObjectSlots[]` | Arc completion system calls `SpawnArcObject()` after the narrative beat |
| `MoodMachine` | Receives `"ArcResonance"` source at all-arcs-complete | Arc completion system registers source; `MoodMachine` uses it in mood calculation |
| `ReactableTag` | Makes arc objects visible to Phase 3 date evaluation | Arc objects carry `ReactableTag`; dating loop evaluates them normally |
| `PlaceableObject` | Arc objects registered in apartment object registry | Arc objects are valid `PlaceableObject`s; cannot be discarded |
| Character ScriptableObjects | Provides per-character arc object definition, narrative beat reference, perfume/record unlock reference | Arc completion system reads these; does not write to them |
| Narrative Director (authoring) | Authors 7 arc completion beats (letters/postcards), 1 final newspaper ad, optional phone moment | Arc system fires events; narrative content is externally authored |
| Art Team | Designs and builds 7 arc object prefabs | Arc system instantiates prefabs at authored positions |
| `PSXRenderController` | Receives parameter shift at all-arcs-complete (posterize steps, dither pattern) | Arc system sets a final parameter state on all-arcs-complete |
| `GameClock` | Provides day index for "next morning" ending queue | Arc system queues ending for `currentDay + 1` |
| `FlowerTrimmingBridge` | Arc completion check fires after `SpawnPlant()` confirms plant | This system is downstream of `FlowerTrimmingBridge`; does not call into it |

**New system required — `ArcCompletionManager`**: A new MonoBehaviour that:
- Subscribes to `LivingFlowerPlantManager.OnPlantSpawned`
- Runs the arc completion check after each spawn
- Manages the `QueueArcCompletionEvent()` queue
- Drives the Evening-phase sequencing of beats and spawns
- Registers `ArcResonance` with `MoodMachine` at all-arcs-complete
- Sets `PSXRenderController` ending parameters

This is a new system with no existing implementation. The lead programmer
must spec and build it. This document serves as the design contract for that
work.

---

## Tuning Knobs

| Knob | Recommended Value | Safe Range | Category | Effect |
|---|---|---|---|---|
| Arc beat minimum display duration | 4.0 seconds | 2.0–8.0 | Gate | How long the narrative beat is on screen before the player can dismiss it |
| Arc object spawn animation duration | 1.5 seconds | 0.5–3.0 | Feel | How long the arc object's arrival animation takes |
| Arc object surface multiplier (ReactableTag) | 1x | 1x–3x | Curve | How much arc objects can shift affection during dates (currently neutral) |
| ArcResonance MoodMachine value | 1.0 | 0.5–1.0 | Curve | How strongly the ending state shifts apartment mood; 1.0 is the maximum shift |
| Ending queue delay (next morning vs. same night) | 1 morning | 0–1 | Gate | Whether ending fires the same Evening or the following morning |
| PSX posterize step shift at ending | +1 step | 0–3 | Feel | How much flatter the PSX rendering becomes at all-arcs-complete |
| Arc object nudge distance (overlap resolution) | 1 grid cell (0.06m) | 0.06–0.3m | Feel | How far an arc object moves if its spawn position is occupied |
| Post-arc phone ring pitch shift | +2 semitones | 0–5 | Feel | Subtle audio change on days a completed-arc character calls |

All knob values live in `assets/data/arc-completion-config.asset`
(a ScriptableObject). None are hardcoded.

---

## Acceptance Criteria

**Functional criteria — what the system must do:**

1. **Arc object spawns after third trimming session.** Complete three
   successful dates with any single character. After the third trimming
   session ends and the plant spawns, confirm the character's arc object
   appears in the apartment at its authored position during the Evening
   phase. QA: step through three dates with one character using debug date
   forcing; inspect `arcObjectSlots[]` and apartment scene after Evening.

2. **Arc narrative beat plays before results screen.** On the third
   successful date, confirm the narrative beat (letter/postcard) appears
   after the trimming scene unloads but before the Evening results screen
   becomes interactive. QA: add phase-transition timestamps to the event
   log; verify beat fires between `TrimScene.Unload` and `ResultsScreen.Show`.

3. **`arcComplete` flag set correctly.** After three successful dates,
   `DateHistory.arcComplete[character] == true`. After two successful
   dates, `arcComplete == false`. QA: unit test arc completion check with
   counts of 2 and 3.

4. **Arc object uses dedicated slot, not plant slot.** After all `_plantSlots[]`
   are filled, earn a character's third arc flower. Confirm the arc object
   spawns without overwriting any plant. QA: fill plant slots, complete an
   arc, confirm all original plants and the arc object are present.

5. **Arc objects are indestructible.** Attempt to drag an arc object into
   a trash zone. Confirm placement is rejected (ghost turns red, drop fails).
   QA: manual interaction test with each arc object archetype.

6. **Dual arc completion on same Evening.** Engineer two characters whose
   third trimming session resolves on the same day. Confirm both narrative
   beats play sequentially and both arc objects spawn before the results
   screen. Confirm the results screen does not show until both sequences
   complete. QA: debug tool to force two arc completions on one day.

7. **All-arcs-complete fires on seventh arc.** Complete six arcs. Complete
   the seventh. Confirm `endingTriggered = true` in `DateHistory` and the
   ending sequence queues for the next morning. QA: complete all seven arcs
   in a debug session with forced outcomes.

8. **ArcResonance registered in MoodMachine at ending.** On the morning
   after all-arcs-complete, confirm `MoodMachine` has an active source
   called `"ArcResonance"` with value 1.0. Confirm apartment mood has
   shifted measurably (above pre-ending baseline). QA: log MoodMachine
   sources before and after ending morning.

9. **Post-arc character calls with audio variation.** Complete an arc.
   Let the character appear in the next scheduling rotation. Confirm the
   phone ring audio has the authored pitch shift (verify against
   `arc-completion-config.asset` value). QA: listen test with audio
   visualizer showing pitch; compare pre- and post-arc ring.

10. **Perfume/record unlock flagged.** Complete any arc. Confirm
    `DateHistory.perfumeUnlocked[character] == true` immediately after
    arc completion. QA: inspect `DateHistory` state in debugger after
    Evening phase.

**Experiential criteria — what a playtest must validate:**

11. **Narrative beat feels like a small, private goodbye.** Playtest with
    three new players who have not read the story content. After their
    first arc completion, ask: "What did that moment feel like?" Accept
    answers containing: closure, quietness, sadness, warmth, or specific
    character references. Reject: "I didn't notice," "It felt like a trophy,"
    or "It was too long." Target: 2 of 3 players describe an emotional
    register consistent with the Player Fantasy section.

12. **Arc objects accumulate without feeling cluttered (arcs 1–5).** In a
    playtested session where 5 arcs are complete, ask players to describe
    the apartment. Accept: "full," "lived-in," "like someone lives there,"
    "there's a lot of stuff." Reject: "cluttered," "messy," "too much."
    The shift from acceptable to uncomfortable should occur between arcs
    5 and 7, not before.

13. **Ending apartment reads as "slightly wrong."** After all 7 arcs
    complete and the ArcResonance atmosphere shift plays, ask players: "Does
    the apartment feel different?" Without prompting, at least 2 of 3 should
    confirm yes. Ask "different how?" — acceptable answers include any
    aesthetic description. The goal is that the shift is perceptible but
    cannot be easily named.

14. **Arc objects are noticed by subsequent date characters.** In a playtest
    session with at least 3 arc objects present, observe whether date
    characters' Phase 3 walkthroughs produce any `ReactableTag` events on
    arc objects. Confirm the reaction text registers appropriately (not "an
    object" but authored text). At least one arc object per date should be
    investigated in a standard walkthrough.

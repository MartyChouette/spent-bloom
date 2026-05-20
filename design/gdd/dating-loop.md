---
status: reverse-documented
source: [GameClock, DayManager, DayPhaseManager, PhoneController, DateSessionManager, ReactionEvaluator, DateCharacterController, DateEndScreen, FlowerTrimmingBridge, DateHistory]
date: 2026-05-20
---

# Dating Loop

> **Note**: This document was reverse-engineered from the existing implementation.

**Status**: Reverse-documented
**Depends on**: GameClock, DayPhaseManager, PhoneController, DateSessionManager,
ReactionEvaluator, DateCharacterController, DateEndScreen, TidyScorer, MoodMachine,
DrinkPourManager, ReactableTag, FlowerTrimmingBridge, DateHistory

---

## Overview

The Dating Loop is the primary progression spine of Spent Bloom. Over a 7-day
calendar the player receives phone calls from potential dates, arranges visits,
and hosts them across three sequential evaluation phases: Entrance (first
impressions), Kitchen (drink-making), and Reveal (apartment walkthrough). Each
phase feeds affection gains or losses into a running total. A passing grade
earns a trimmed flower. Three successful dates with a single character advances
their personal arc. Seven distinct characters cycle through the week, with the
tutorial date forced on Day 1 and each character withheld from repeat
appearance until all others have been seen.

## Player Fantasy

The player should feel like someone who genuinely wants to make a good
impression — cleaning the apartment not because a prompt says to, but because
a real person is coming over. Anticipation builds from the moment the phone
rings. Each date should feel like a small social puzzle: the player reads the
character's posted ad, infers their preferences, and tries to curate a space
that reflects them. Success should feel warm and earned; failure should feel
instructive rather than punitive, leaving the player thinking about what to
change before the next visitor arrives.

## Detailed Rules

### Calendar and Character Scheduling

- The game runs on a 7-day cycle managed by `GameClock`.
- Each morning, `DayManager` posts a newspaper personal ad for that day's
  available date character.
- Day 1 is always the tutorial date; the tutorial character is fixed and
  `_alwaysPositive = true` forces every evaluation in all phases to return
  Like, regardless of actual conditions.
- No character repeats until all 7 characters have appeared at least once.
- `DateHistory` records the outcome of every date (grade, affection score,
  flower awarded, day index).

### Phone and Scheduling Flow

1. `PhoneController` rings at a scripted or random point during the Morning
   phase.
2. The player must physically walk to the phone and answer it.
3. Answering commits the date; the character will arrive during the Exploration
   phase.
4. If the phone is not answered, no date occurs that day.

### Day Phases

`DayPhaseManager` sequences the day through five states in order:

| Phase | Name | What Happens |
|---|---|---|
| 1 | Morning | Newspaper ad appears; phone may ring |
| 2 | Exploration | Player preps apartment; date character arrives at phase end |
| 3 | DateInProgress | Three evaluation phases run sequentially |
| 4 | FlowerTrimming | Loads trimming scene if affection >= 30 or guaranteeFlower is set |
| 5 | Evening | Day closes; results screen shown |

### Phase 1 — Entrance

The date character enters and makes 4 simultaneous or near-simultaneous
judgments. Each judgment produces a reaction (Like, Neutral, or Dislike) that
feeds into the affection delta formula.

**Music** — evaluated against the character's surface multiplier for the
active music track. The result acts as a multiplier on subsequent judgments in
this phase, not a standalone affection event.

**Perfume** — evaluated against the character's liked-perfume intensity
threshold:
- Intensity in range [0.34, 0.99]: Like
- Intensity < 0.34: Neutral
- Intensity > 0.99: Dislike
- If the room's ambient smell exceeds 2x the character's perfume threshold,
  the result is downgraded one tier (Like → Neutral, Neutral → Dislike).

**Cleanliness** — evaluated against the tidiness score produced by
`TidyScorer`. The exact threshold mapping is defined per character.

**Outfit** — DISABLED (WIP). No affection events are generated from outfit
evaluation in the current build.

### Phase 2 — Kitchen

The player makes the character a drink using `DrinkPourManager`. The drink
receives a score in the range [0, 100].

- If the recipe is the character's liked recipe AND score >= 60: Like.
- If the recipe is the character's liked recipe AND score >= 80: Like
  (override; guaranteed regardless of other modifiers).
- If the recipe is the character's disliked recipe: Dislike, regardless of
  score.
- All other combinations: Neutral.

### Phase 3 — Reveal

The character walks the apartment and reacts to individual items tagged with
`ReactableTag`. This phase has the highest total affection potential because
it can fire many individual reactions.

**Cache building** — `BuildRevealCache` runs during the fade-in preceding
Phase 3, processing 10 items per frame. Items are sorted by surface multiplier
in descending order: 3x items evaluated first, then 2x, then 1x.

**Presentation** — Each item is surfaced to the player staggered 0.6 seconds
apart. The character animates toward the item and evaluates it at the 2.0-
second mark of their investigation.

**Player interaction** — The player may click any visible item to surface it
immediately, bypassing the stagger. Any items not manually surfaced or
auto-presented before the phase ends are swept automatically at phase
conclusion.

**Character excursions** — Between scripted item presentations,
`DateCharacterController` independently wanders:
- Excursion interval: every 4 seconds.
- Excursion chance: 90% per interval.
- Investigation duration: 3 seconds per item; evaluation fires at the 2.0-
  second mark.

**Character state machine**: Idle → Sitting → Investigating → Dismissed.

### Affection and Grading

Starting affection: 50 out of 100.

After all three phases, the final affection value is translated to a letter
grade:

| Grade | Threshold |
|---|---|
| S | >= 90 |
| A | >= 75 |
| B | >= 60 |
| C | >= 40 |
| D | < 40 |
| F | Mid-date fail (see Edge Cases) |

Flower trimming is unlocked when final affection >= 30, or when the
character's `guaranteeFlower` flag is set.

### Arc Progression

Three successful dates (any grade above F) with the same character completes
their personal arc, unlocking arc-specific narrative content. Arc completion
is stored in `DateHistory`.

### Dormant Systems

The following systems exist in code but are not active in the current build:

- **Ambient mood drift**: every 15 seconds, affection drifts +0.5 on a mood
  match, -0.25 on a miss. Disabled.
- **Mid-date fail thresholds**: affection floors that would trigger early
  date-end. Intentionally disabled ("old hat"). Outcome is always graded, never
  hard-failed mid-date.
- **moodMultiplier** in the reaction formula (1.5x match / 0.5x mismatch):
  the float-based system is being replaced by discrete per-phase evaluations
  and is currently dormant.

## Formulas

### Reaction Delta

Each individual reaction event changes affection by:

```
delta = baseValue × magnitude × moodMultiplier × characterReactionStrength
```

| Variable | Description | Current Values |
|---|---|---|
| `baseValue` | Valence of the reaction | Like: +5.0, Neutral: +0.5, Dislike: -4.0 |
| `magnitude` | Weight of the item or event | Surface multiplier 1–5x; drink score/100 |
| `moodMultiplier` | Mood match bonus/penalty | 1.5x match, 0.5x mismatch (DORMANT) |
| `characterReactionStrength` | Per-character sensitivity scalar | Defined per character profile |

**Example — Phase 3 item reaction:**
A character with `reactionStrength = 1.0` reacts with Like to a 2x surface
item while mood is matched (moodMultiplier dormant, treated as 1.0):
```
delta = 5.0 × 2.0 × 1.0 × 1.0 = +10.0 affection
```

**Example — Phase 2 drink reaction:**
Liked recipe, score = 72 (Like tier), `reactionStrength = 1.0`:
```
magnitude = 72 / 100 = 0.72
delta = 5.0 × 0.72 × 1.0 × 1.0 = +3.6 affection
```

### Perfume Tier Downgrade Condition

```
if roomSmell > perfumeThreshold × 2.0 → downgrade result one tier
```

## Edge Cases

**Tutorial date**: `_alwaysPositive = true` is set on the evaluator for Day 1
dates. All reactions return Like regardless of actual apartment state, perfume
intensity, drink score, or item condition. This cannot be toggled mid-date.

**No phone answer**: If the player does not answer the phone during the
Morning phase, the day proceeds to Evening with no date, no affection events,
and no flower. `DateHistory` records no entry for that day.

**All characters seen**: Once all 7 characters have appeared at least once,
the scheduling pool resets and characters may repeat. Repeat scheduling
preserves `DateHistory` across appearances.

**Outfit evaluation**: Because Outfit is disabled (WIP), characters never
react to clothing. If re-enabled, the outfit judgment fires alongside the
other three Entrance judgments and uses the standard reaction delta formula.

**Perfume with no active scent**: If no perfume has been applied, intensity is
0. This falls below the 0.34 threshold and returns Neutral. The room-smell
downgrade check still runs; if ambient smell exceeds 2x the threshold, result
degrades to Dislike.

**Flower threshold at exactly 30**: Affection equal to 30 (not strictly
greater than) qualifies for flower trimming. `guaranteeFlower` bypasses the
threshold check entirely.

**Mid-date fail**: Code paths for mid-date failure thresholds exist but are
disabled. If re-enabled, breaching the floor affection value mid-date would
terminate the date early and assign grade F, bypassing Phase 3 and FlowerTrimming.

**Three-arc completion on the same day**: If a third successful date completes
a character arc, the arc content triggers at the Evening phase after the
results screen, not during the date.

## Dependencies

| System | Role in This System | This System's Role for It |
|---|---|---|
| `GameClock` | Provides current day index | Reads day to enforce tutorial lock and character cycling |
| `DayPhaseManager` | Sequences phases, triggers transitions | Dating loop occupies phases 2–4 |
| `PhoneController` | Signals date arrival | Dating loop registers a listener to begin Exploration |
| `DateSessionManager` | Owns date state across all phases | Dating loop delegates per-phase logic to it |
| `ReactionEvaluator` | Computes reaction deltas | Feeds results back as affection events |
| `DateCharacterController` | Drives character animation and excursions | Receives phase signals (start/end Investigating/Dismissed) |
| `DateEndScreen` | Displays grade and results | Receives final affection value and grade at Evening |
| `TidyScorer` | Provides cleanliness score for Phase 1 | Polled once at Entrance evaluation time |
| `MoodMachine` | Provides mood state for moodMultiplier | Currently dormant; will resume when discrete mood ships |
| `DrinkPourManager` | Runs Phase 2 drink interaction | Returns score [0, 100] to ReactionEvaluator |
| `ReactableTag` | Marks items for Phase 3 evaluation | Registry queried by BuildRevealCache |
| `FlowerTrimmingBridge` | Loads trimming scene on phase transition | Dating loop signals bridge when affection qualifies |
| `DateHistory` | Persists outcomes | Dating loop writes grade, affection, flower flag after Evening |

## Tuning Knobs

| Knob | Current Default | Safe Range | Gameplay Effect |
|---|---|---|---|
| Starting affection | 50 | 30–70 | Sets the floor and ceiling pressure on each date |
| Like baseValue | +5.0 | +3.0–+8.0 | How much a single positive reaction is worth |
| Neutral baseValue | +0.5 | 0.0–+1.5 | Whether neutral reactions feel meaningless or slightly rewarding |
| Dislike baseValue | -4.0 | -2.0–-6.0 | Punishment severity for a bad impression |
| Flower affection threshold | 30 | 20–50 | How often the player earns the trimming reward |
| Phase 3 stagger interval | 0.6s | 0.3–1.2s | Pacing of reveal; shorter feels frenetic, longer feels deliberate |
| Excursion interval | 4s | 2–8s | Frequency of unsolicited character wandering |
| Excursion chance | 90% | 60–100% | How reliably the character explores between scripted events |
| Investigation duration | 3s | 1.5–5s | Time character lingers at each item |
| Evaluation mark | 2.0s | 1.0–2.5s | When within investigation the reaction fires |
| Perfume liked low threshold | 0.34 | 0.2–0.5 | How little perfume before result degrades to Neutral |
| Perfume liked high threshold | 0.99 | 0.8–1.0 | How much before result degrades to Dislike |
| Room smell downgrade multiplier | 2.0x | 1.5–3.0x | How strongly ambient smell interferes with perfume |

## Acceptance Criteria

1. **Day 1 tutorial lock**: All three phase evaluations return Like on Day 1
   regardless of apartment state, perfume level, or drink score. QA: set all
   conditions to worst case, confirm grade >= A.

2. **No character repeats before full rotation**: Over 7 consecutive days with
   phone answered each day, all 7 characters appear exactly once. QA: record
   character names from newspaper ads across 7 days; confirm no duplicate
   until day 8.

3. **Phase sequencing**: Phases fire in order Morning → Exploration →
   DateInProgress → FlowerTrimming (if qualified) → Evening. QA: add debug
   log to each phase transition; confirm order across 5 dates.

4. **Flower threshold**: A date ending at affection 30 loads the trimming
   scene. A date ending at affection 29 does not. QA: force affection to 30
   and 29 via debug menu; observe scene load behavior.

5. **Reaction delta correctness**: Provide a character with `reactionStrength
   = 1.0`, trigger a Like reaction on a 2x item with mood dormant; confirm
   affection increases by exactly 10.0. QA: unit test in `ReactionEvaluator`.

6. **Perfume downgrade**: Apply perfume at intensity 0.5 (Like tier) to a
   character whose threshold is 0.4. Set room smell to 0.9 (> 0.4 × 2 = 0.8).
   Confirm result degrades to Neutral. QA: automated test against
   `ReactionEvaluator.EvaluatePerfume`.

7. **Phase 3 cache sort**: Confirm that after `BuildRevealCache` completes,
   items appear to the player in descending surface-multiplier order (3x
   before 2x before 1x). QA: log item order at phase start.

8. **Arc completion**: After 3 successful dates with the same character,
   arc content triggers at Evening. QA: run 3 dates with one character,
   confirm arc flag set in `DateHistory` and arc scene/content plays.

9. **Grade boundaries**: Confirm grade assignment at affection values 39, 40,
   59, 60, 74, 75, 89, 90. QA: unit test against grading function with those
   exact inputs.

10. **No phone, no date**: If player does not approach or interact with the
    phone during Morning, day proceeds to Evening with no date content and no
    `DateHistory` entry for that day. QA: let morning timer expire; verify
    Evening phase loads and history is empty for that day.

---
status: analysis
date: 2026-05-19
affects: [drink-making.md, dating-loop.md]
---

# Drink Pour Magnitude Curve — Analysis and Alternatives

**Problem statement**: The current linear magnitude curve (`score / 100`) produces an
affection spread of only +2.0 points between a barely-passing drink (score 60) and a
perfect drink (score 100). Against grade bands that are 10–30 affection points wide,
this makes pour execution feel inconsequential and conflicts with the Deliberate
Physicality pillar.

---

## System Context

Phase 2 fires exactly one reaction event per date. With current defaults:

```
delta = baseValue × magnitude × moodMultiplier × characterReactionStrength
delta = 5.0 × magnitude × 1.0 × 1.0
```

- `moodMultiplier` is dormant (treated as 1.0)
- `reactionStrength = 1.0` for a baseline character
- The drink reaction gate (Like / Neutral / Dislike) is determined by the binary
  score thresholds in `DateSessionManager`, not by the magnitude curve
- Magnitude only controls *how much* affection moves within the verdict tier

Grade thresholds for reference (starting affection = 50):

| Grade | Affection threshold | Distance from start |
|-------|--------------------|--------------------|
| S     | >= 90              | +40 from start     |
| A     | >= 75              | +25 from start     |
| B     | >= 60              | +10 from start     |
| C     | >= 40              | -10 from start     |
| D     | < 40               | -11+ from start    |

A single drink reaction can contribute at most +5.0 under the current system — half a
grade band between C and B. The difference between sloppy (score 60) and perfect (score
100) is 2.0 affection points, well under the 10-point minimum grade band width.

---

## Two Separable Problems

**Problem 1 — Total ceiling too low**: Even a perfect drink only shifts affection by +5.0,
which is the same as a single Like on a 1x surface item in Phase 3. Phase 2 is meant to
be a centerpiece moment (see date-phase-scoring-redesign.md), but its peak contribution
is identical to the weakest possible Phase 3 Like reaction.

**Problem 2 — Spread too narrow**: Within the Like range (score 60–100), the affection
difference between passing and perfect is only 2.0 points. Players have no incentive to
pour carefully once they clear the binary Like gate at score 60.

The magnitude curve shape primarily addresses Problem 2. Problem 1 also requires widening
the effective range — either by changing `baseValue` for Phase 2 specifically (which
affects the reaction gate math) or by applying a scale multiplier to the drink magnitude
before it enters the delta formula.

---

## Comparison Table

All values computed with `baseValue = 5.0`, `reactionStrength = 1.0`, `moodMultiplier = 1.0`.

### Magnitude at Each Score Point

| Score | Linear (current) | Quadratic (A) | Power 1.5 (B) | Threshold Bonus (C) |
|-------|-----------------|---------------|----------------|---------------------|
| 0     | 0.000           | 0.000         | 0.000          | 0.000               |
| 30    | 0.300           | 0.090         | 0.164          | 0.300               |
| 60    | 0.600           | 0.360         | 0.465          | 0.600               |
| 80    | 0.800           | 0.640         | 0.716          | 0.800               |
| 90    | 0.900           | 0.810         | 0.854          | 1.025               |
| 100   | 1.000           | 1.000         | 1.000          | 1.500               |

### Affection Delta at Each Score Point (baseValue 5.0)

| Score | Linear (current) | Quadratic (A) | Power 1.5 (B) | Threshold Bonus (C) |
|-------|-----------------|---------------|----------------|---------------------|
| 0     | +0.00           | +0.00         | +0.00          | +0.00               |
| 30    | +1.50           | +0.45         | +0.82          | +1.50               |
| 60    | +3.00           | +1.80         | +2.33          | +3.00               |
| 80    | +4.00           | +3.20         | +3.58          | +4.00               |
| 90    | +4.50           | +4.05         | +4.27          | +5.13               |
| 100   | +5.00           | +5.00         | +5.00          | +7.50               |

### Spread (score 60 to score 100)

| Curve                | Delta at 60 | Delta at 100 | Spread 60→100 |
|---------------------|-------------|--------------|---------------|
| Linear (current)    | +3.00       | +5.00        | 2.0           |
| Quadratic (A)       | +1.80       | +5.00        | 3.2           |
| Power 1.5 (B)       | +2.33       | +5.00        | 2.7           |
| Threshold Bonus (C) | +3.00       | +7.50        | 4.5           |

---

## Option A — Quadratic

**Formula**:
```
magnitude = (score / 100)^2
```

**Variable definitions**:
- `score`: integer [0, 100], sum of order + layer + fill + garnish - overflow
- `magnitude`: float [0.0, 1.0], passed to delta formula

**Affection delta at key scores**: see table above.

**Curve description**: Concave upward (slow start, accelerating finish). Scores below 50
are heavily penalized relative to linear. Scores above 80 are only modestly rewarded.

**Analysis**:

The quadratic compresses low scores effectively but this compression does most of its
work in the score range that never reaches the Like verdict (below 60). Inside the Like
range (60–100), the spread is 3.2 points — marginally better than the 2.0 linear spread.
The more significant effect is that score 60 now only yields +1.80 affection, which
creates a perceptual mismatch: the reaction bubble reads "Like" but the affection delta
is lower than the current system. If the date-phase-scoring-redesign ships with the raw
score shown as flavor text ("Great mix! +1.8"), this will feel stingy for a passing
drink.

The quadratic also does not address Problem 1 at all — the ceiling remains at +5.0.

**Verdict**: Addresses the wrong end of the problem. Not recommended.

---

## Option B — Power 1.5

**Formula**:
```
magnitude = (score / 100)^1.5
```

**Variable definitions**: same as Option A.

**Affection delta at key scores**: see table above.

**Curve description**: Concave upward, but more gently than the quadratic. Intermediate
scores are compressed relative to linear; the ceiling is unchanged.

**Analysis**:

A conservative improvement. The 60→100 spread widens from 2.0 to 2.7. The score-60
delta drops slightly from +3.00 to +2.33, meaning a passing drink is slightly less
rewarding. The ceiling remains at +5.0.

This option is the safest change — it tightens the lower end without touching the
ceiling — but its impact on the felt problem is minimal. The player who pours sloppily
(score 60) loses +0.67 affection relative to current; the player who pours perfectly
gains nothing extra. The "why bother" question is only partially answered.

This is appropriate if the design intent is to make poor execution hurt slightly more
rather than to make excellent execution reward more. That framing is misaligned with
Deliberate Physicality, which rewards craft rather than punishing inattention.

**Verdict**: Marginal improvement, wrong direction of emphasis. Not recommended.

---

## Option C — Threshold Bonus (RECOMMENDED)

**Formula**:
```
aboveThreshold = max(0, (score / 100) - 0.8)
magnitude = (score / 100) + aboveThreshold × 2.5
```

Equivalently:
```
base     = score / 100                                  // [0.0, 1.0]
bonus    = max(0, (score / 100) - 0.8) × 2.5           // [0.0, 0.5]
magnitude = base + bonus                                // [0.0, 1.5]
```

**Variable definitions**:
- `score`: integer [-15, 100] (negative possible with overflow and zero other components)
- `base`: linear term, [0.0, 1.0]
- `bonus`: additional magnitude awarded above score 80, [0.0, 0.5]
- `magnitude`: total, [0.0, 1.5] — note: exceeds 1.0 for scores above 80
- Threshold: 0.8 (score 80) — matches the existing Like-override gate
- Bonus slope: 2.5 — tunable; see Tuning Knobs

**Affection delta at key scores**:

| Score | base   | bonus  | magnitude | delta  |
|-------|--------|--------|-----------|--------|
| 0     | 0.000  | 0.000  | 0.000     | +0.00  |
| 30    | 0.300  | 0.000  | 0.300     | +1.50  |
| 60    | 0.600  | 0.000  | 0.600     | +3.00  |
| 80    | 0.800  | 0.000  | 0.800     | +4.00  |
| 90    | 0.900  | 0.025  | 1.025     | +5.13  |
| 95    | 0.950  | 0.038  | 1.263     | +6.31  |
| 100   | 1.000  | 0.050  | 1.500     | +7.50  |

Wait — recalculating bonus at score 90: `(0.90 - 0.80) × 2.5 = 0.10 × 2.5 = 0.25`,
so magnitude = 0.90 + 0.25 = 1.15, delta = +5.75.

Corrected table:

| Score | base   | bonus  | magnitude | delta  |
|-------|--------|--------|-----------|--------|
| 0     | 0.000  | 0.000  | 0.000     | +0.00  |
| 30    | 0.300  | 0.000  | 0.300     | +1.50  |
| 60    | 0.600  | 0.000  | 0.600     | +3.00  |
| 80    | 0.800  | 0.000  | 0.800     | +4.00  |
| 90    | 0.900  | 0.250  | 1.150     | +5.75  |
| 95    | 0.950  | 0.375  | 1.325     | +6.63  |
| 100   | 1.000  | 0.500  | 1.500     | +7.50  |

**Corrected Comparison Table (summary)**:

| Score | Linear (current) | Quadratic (A) | Power 1.5 (B) | Threshold Bonus (C) |
|-------|-----------------|---------------|----------------|---------------------|
| 0     | +0.00           | +0.00         | +0.00          | +0.00               |
| 30    | +1.50           | +0.45         | +0.82          | +1.50               |
| 60    | +3.00           | +1.80         | +2.33          | +3.00               |
| 80    | +4.00           | +3.20         | +3.58          | +4.00               |
| 90    | +4.50           | +4.05         | +4.27          | +5.75               |
| 100   | +5.00           | +5.00         | +5.00          | +7.50               |

**Spread 60→100 corrected**: +3.00 to +7.50 = **4.50 points**.

**Curve description**: Linear below score 80 (identical to current). Above 80, the slope
increases continuously, reaching 1.5× the base rate at score 100. The curve is C1
continuous (smooth first derivative) at the threshold — there is no cliff or jump.

**Design alignment**:

The threshold at 0.8 (score 80) is not arbitrary. Score 80 is already the Like-override
gate: the binary reaction logic upgrades any drink to Like at score 80+ regardless of
recipe preference. Option C creates a *continuous analog* of that same gate. The player
learns one number — 80 — and it operates in two ways simultaneously: it guarantees the
Like verdict AND it unlocks escalating affection returns. This creates clear, reinforcing
feedback aligned with Pillar 4 (Judgment You Can Feel).

Below score 80, the curve is identical to the current linear. This means Option C
introduces zero regression for the majority of plays. The change is additive: it adds
new ceiling, not new floor.

The perfect drink (+7.50 affection) moves the needle materially — from 50 to 57.5 in a
vacuum, or nearly the full width of the C→B grade band. A sloppy-passing drink (+3.00)
moves it to 53, still in the C band unless Phase 3 adds more. This spread makes pour
execution a visible factor in grade outcomes, directly serving Deliberate Physicality.

**Magnitude > 1.0 note**: `DateSessionManager` receives magnitude as a float and passes
it to the delta formula without a documented clamp. Scores above 80 produce magnitude
> 1.0 under this curve. This needs to be verified against the implementation — if any
code path clamps magnitude to [0, 1], it must be removed or raised. The score display
in the scoring redesign ("Great mix! +7.5") will correctly show the higher value.

**Verdict**: Recommended. Widest spread (4.5 points), best pillar alignment, additive
change with no regression below score 80.

---

## Proposed Tuning Knob Additions (for drink-making.md)

These three parameters should be added to the Tuning Knobs table in
`design/gdd/drink-making.md`. They are currently implicit or absent.

| Parameter              | Recommended Value | Safe Range | Affects                                                    |
|------------------------|-------------------|------------|------------------------------------------------------------|
| `drinkBonusThreshold`  | 0.80              | 0.60–0.90  | Normalized score above which bonus slope activates         |
| `drinkBonusSlope`      | 2.5               | 1.0–4.0    | Rate of bonus magnitude accumulation above threshold       |
| `drinkMagnitudeClamp`  | unclamped (None)  | None–2.0   | If set, caps magnitude before it enters the delta formula  |

`drinkBonusSlope = 1.0` reproduces linear behavior above the threshold (no amplification,
just continuity). `drinkBonusSlope = 0.0` reproduces the current linear curve exactly
(no bonus). This means the feature can be disabled without a code change during tuning.

---

## Edge Cases

**Negative scores**: Score -15 (all components zero plus overflow) produces
`base = -0.15`, `bonus = 0` (threshold not reached), `magnitude = -0.15`, delta =
`5.0 × -0.15 = -0.75`. This is a small negative affection event for a catastrophically
bad pour. The Dislike reaction gate does not fire for this case unless the recipe was
disliked — so the character might have a neutral-to-negative reaction to a horribly
overflowed drink, which is narratively appropriate. `DateSessionManager` must handle
negative magnitude gracefully (existing edge case, already documented in drink-making.md).

**Score exactly 80**: `aboveThreshold = max(0, 0.80 - 0.80) = 0`. No bonus. Magnitude
= 0.80, delta = +4.00. This is correct — score 80 is the entry point for the Like
override but the bonus has not yet activated. This matches the intuition that score 80
is a floor, not a ceiling.

**Disliked recipe interaction**: The Dislike verdict fires regardless of score for a
disliked recipe. Under Option C, a score of 90 on a disliked recipe would yield
magnitude = 1.15, but the reaction gate passes a Dislike baseValue (-4.0):
`delta = -4.0 × 1.15 × 1.0 × 1.0 = -4.60`. This means a technically excellent pour
of a disliked drink is *slightly more punishing* than a poor pour (-1.80 at score 30
vs -4.60 at score 90). This is narratively coherent — getting a drink right that the
character hates signals you didn't listen to them. It also creates a memorable failure
mode that reinforces reading the character's preferences.

**Score 80 Like-override with disliked recipe**: The existing ruling is that Dislike
overrides the score-80 Like-override (documented in drink-making.md edge cases). Under
Option C this is unchanged — the bonus magnitude amplifies the delta in either
direction, but the verdict gate logic is not modified.

**`reactionStrength` scaling**: A character with `reactionStrength = 1.5` and a perfect
drink: `delta = 5.0 × 1.5 × 1.0 × 1.5 = +11.25`. At this strength, a perfect drink
alone could push the player from 50 to 61.25, crossing the B threshold. This is
intentional — high-sensitivity characters are designed to swing dramatically. The tuning
risk is that `drinkBonusSlope` interacts multiplicatively with `reactionStrength`, so
bonus slope should be validated against the full character roster.

---

## Recommendation Summary

Adopt **Option C (Threshold Bonus)** with `drinkBonusThreshold = 0.80` and
`drinkBonusSlope = 2.5`.

Key outcomes:
- Score 60 (sloppy passing): +3.00 affection — unchanged from current, no regression
- Score 80 (solid): +4.00 affection — unchanged from current
- Score 100 (perfect): +7.50 affection — vs +5.00 current, a 50% increase
- 60→100 spread: 4.50 points vs 2.00 current, a 125% increase
- The 80-threshold echoes the existing Like-override gate, teaching one number twice
- Formula is additive and can be disabled by setting `drinkBonusSlope = 0`
- No code-path regressions below score 80

Implementation requires: (1) verifying no magnitude clamp exists in `DateSessionManager`,
(2) adding `drinkBonusThreshold` and `drinkBonusSlope` as tunable parameters in the
drink scoring path, (3) updating the magnitude calculation in `DrinkPourManager` or
`ReactionEvaluator` (whichever normalizes the score before passing to `ApplyReaction`).

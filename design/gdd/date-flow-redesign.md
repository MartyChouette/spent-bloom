# Date Flow Redesign

## Overview

Redesign of the full date sequence to fix transition bugs, unify Phase 3
into one continuous stage, and make the sweep judgment a clean visual recap.

## Full Date Sequence (Beat by Beat)

### Day Start

1. Player wakes up
2. Mail arrives at door (newspaper + letter + packages)
3. Player clicks mail to pick up
4. Packages leave if empty, newspaper and letter stay
5. Newspaper opens: player's personal ad, available date ads, item order ads
6. Day 1: Paris is the only available date (no tutorial card needed)
7. First-time newspaper tutorial card explains how to read ads and call
8. Player calls a date
9. Free time: clean up, arrange items, set perfume, pick outfit, make drinks
10. Date arrives

### Phase 1 -- Impressions

1. Date enters, door greeting
2. Entrance judgment sequence: music, perfume, cleanliness, outfit, lighting
3. Each judgment: reaction cam, scoring, dialogue line
4. Date sits down on couch

### Phase 2 -- Drinks

1. Transition to kitchen area
2. Player makes drink (ingredients on drink cart or from fridge -- TBD)
3. Player serves drink (serve button + click which drink -- needs UX clarity)
4. Judgment cam: date reacts to drink, scoring
5. Drink stays as prop on table after serving
6. Drink becomes dirty dish next day (not happening currently -- bug)

### Phase 3 -- Reveal (Redesigned)

**One continuous stage. No "continue" button. No sub-stages.**

1. White fade out from Phase 2
2. Camera transitions to couch/living area framing while screen is white
3. Build reveal cache during fade (already implemented, spread across frames)
4. Fade in. Date settles on couch.
5. Date begins auto-excursions: walks to items, reacts with split-cam
6. Player can click items anytime to prompt reactions
7. If date is mid-reaction when player clicks, click queues and plays next
8. Both auto-reactions and player clicks use the same system:
   - Same split-cam
   - Same reaction bubble
   - Same scoring (ApplyReaction called once per item)
   - Same dialogue source (dialogue spreadsheet, not hardcoded)
   - Same interaction component (ReactableTag + DateInspectSystem)
9. Each reaction marks the item as scored in _scoredTags
10. Soft timer ticking in background
11. Phase ends when:
    - All reactable non-Neutral items have been seen/clicked, OR
    - Soft timer expires
12. Date says closing line
13. Transition to sweep

### Sweep Judgment (Separate Visual Sequence)

**All data is pre-computed. No processing at sweep start. Pure presentation.**

1. Camera pulls back to show full room
2. Good items shown first:
   - Camera points at each good item
   - Highlight + score popup + multiplier
   - Items already scored in Phase 3: show popup but DO NOT add points again
   - Unseen items: show popup AND apply points (first-time scoring)
3. Bad items shown second:
   - Same treatment: highlight, popup, multiplier
   - Already scored: display only, no double points
   - Unseen: apply points
4. Cleanliness evaluation
5. Grade reveal

### Flower Gift

1. If successful (affection >= threshold or guaranteeFlower):
   - Flower gift presentation screen
   - Flower rotation facing the viewer (not away -- bug fix)
   - Screen waits for player click (does not auto-advance -- bug fix)
2. Flower trimming scene loads
3. Baked/trimmed flower returns to apartment next day (not happening -- bug fix)

### Day End

1. Results screen with grade
2. Evening phase
3. Sleep transition to next day

---

## Known Bugs (Status)

1. ~~**Day 2 newspaper missing**~~ FIXED -- demo mode was skipping newspaper on day 2+
2. ~~**Kitchen flash**~~ FIXED -- kitchen model hidden immediately on phase transition
3. **Drink not persisting as prop** -- _dirtyGlassPrefab may not be assigned in Inspector. Code is correct, needs scene wiring check.
4. ~~**Flower rotation wrong**~~ FIXED -- added 180 Y rotation
5. ~~**Flower gift auto-advances**~~ FIXED -- now waits for click/space/enter
6. ~~**Baked flower not returning**~~ FIXED -- wired save/restore in AutoSaveController
7. ~~**Sweep hitch**~~ FIXED -- SweepAllItems only iterates non-neutral items
8. ~~**Mail not clickable**~~ FIXED -- ensures collider on spawned mail objects
9. **Hardcoded dialogue** -- DialogueDatabase built and CSV expanded, but not yet wired into DateSessionManager/DateReactionUI to replace hardcoded arrays

## Architectural Changes

### Phase 3 Unification

- Remove the Continue button between Phase 3 stages
- Remove the two-stage split (explore then sweep)
- Date auto-excursions and player clicks interleave naturally
- Both trigger split-cam via ReactionSplitScreen
- Queue system: if reaction in progress, next one waits

### Sweep Pre-computation

- Reveal cache already built during Phase 2-3 fade (keep this)
- Sweep presentation only touches non-Neutral items
- Already-scored items: display popup, skip ApplyReaction
- Unseen items: display popup, call ApplyReaction
- No iteration over neutral items at sweep time

### Dialogue Centralization

- All reaction text must come from dialogue-master.csv or DatePreferences
- Remove hardcoded s_likeTexts / s_neutralTexts / s_dislikeTexts arrays
- Per-character generic fallback lines already exist in CSV (P1-GEN-LIKE1, etc.)
- Core fallback lines exist in CSV (CORE-REACT-LIKE1, etc.)

---

## Tuning Knobs

| Knob | Suggested Value | Effect |
|------|----------------|--------|
| Phase 3 soft timer | 60-90 seconds | How long before phase auto-ends |
| Excursion interval | 4 seconds | How often date auto-walks to an item |
| Reaction queue max | 1 | How many clicks can queue while date is reacting |
| Sweep item stagger | 0.8 seconds | Time between each item shown in sweep |
| Sweep good-to-bad pause | 1.5 seconds | Pause between good items section and bad items section |

# Game Pillars — Iris / Spent Bloom

Iris / Spent Bloom is a contemplative apartment life simulation about hosting
strangers, tending what they leave behind, and reckoning with the quiet labor
of making a space feel welcoming. Every design decision — every mechanic, every
piece of feedback, every camera cut — should be traceable back to one or more
of these four pillars. When two choices are in tension, the pillars arbitrate.
When a new feature is proposed, the first question is: which pillar does this
serve?

---

## Pillar 1 — Deliberate Physicality

**The apartment is a real space you inhabit with your hands. Every action is
tactile, grounded, and slightly effortful.**

### Description

Nothing in Iris is a menu click. You carry objects through the apartment. You
pour drinks by tilting bottles. You wipe stains by scrubbing. You trim stems
with a cutting plane you drag across mesh geometry. The verbs are physical first
and functional second. The spring-damper object grab, the magnetic surface
snaps, the grid-aligned placement, the drink-pour physics — these mechanics
exist not as puzzle systems but as substitutes for the weight and texture of
actually doing things.

Physicality also means cost. Moving an object from the shelf to the coffee
table takes time. Getting the perfume spray intensity just right takes
attention. These small friction points are intentional: they make the space
feel like something you are present inside, not something you are clicking
through.

### How It Manifests in Gameplay

- Objects are picked up, carried, and set down using the spring-damper grab
  system. They respond to movement with appropriate lag and oscillation.
- Drink-making requires physically holding bottles over the glass and managing
  pour angle and duration — the score is a consequence of physical behavior,
  not a menu selection.
- Cleaning stains is a direct scrubbing interaction, not a "clean room" prompt.
- Flower trimming is a mesh-cutting minigame where the angle and position of
  the cut determine the score — the geometry of the action matters.
- Camera movement is smooth and continuous, not snap-cut between areas,
  reinforcing the sense of a continuous physical space.

### Systems That Serve This Pillar

| System | Contribution |
|---|---|
| Object Interaction | Core grab, carry, place, and snap mechanics |
| Drink Making | Pour physics, glass filling, recipe assembly |
| Tidiness System | Stain scrubbing, mess cleanup, displaced object detection |
| Flower Trimming | Mesh-cut minigame with scored geometry |
| Apartment Hub | Continuous spline-dolly camera; no hard transitions between areas |
| Watering System | Physical watering-can pour over plant targets |

---

## Pillar 2 — Preparation as Expression

**The date never sees what you chose not to do. Preparation is the game.**

### Description

The date's judgment is not a reaction to who you are — it is a reaction to
what you chose to spend your preparation time on. Cleaning one area means
leaving another untouched. Choosing music means choosing what the apartment
smells like and sounds like when your guest arrives. Watering plants takes time
away from making the kitchen spotless. Every choice about how to spend the
Exploration phase is an expressive act, and the date's response in all three
evaluation phases is the readout of those choices.

This pillar draws the distinction between a game about performance and a game
about care. The player is not trying to find the optimal sequence — they are
deciding what kind of host they want to be for this particular person. The
personal ad in the morning newspaper is the character sheet; how the player
reads and responds to it is the expression.

### How It Manifests in Gameplay

- The Exploration phase has genuine time pressure but no countdown clock — the
  player chooses when to answer the phone and commit to the date, implicitly
  deciding how much preparation time they want.
- Music selection, perfume choice, tidiness score, and object placement all
  feed directly into Phase 1 (Entrance) evaluation, making the pre-date
  apartment state a scored artifact of the player's choices.
- Phase 3 (Reveal) evaluates individual placed objects, rewarding or penalizing
  the specific things the player chose to put on prominent surfaces.
- Surface multipliers (1x, 2x, 3x, up to 5x) mean location decisions are
  expressive — putting a character's liked item on the highest-multiplier
  surface is a statement of care.
- No system tells the player what to do during Exploration. The newspaper
  provides character preference hints; the player interprets them.

### Systems That Serve This Pillar

| System | Contribution |
|---|---|
| Dating Loop | Sequences preparation time (Exploration phase) before judgment (DateInProgress) |
| Date Phase Scoring | Translates preparation choices into affection outcomes |
| Record Player | Music selection feeds Phase 1 music judgment |
| Tidiness System | Cleaning effort feeds Phase 1 cleanliness judgment |
| Object Interaction | Object placement feeds Phase 3 item reactions and surface multipliers |
| MoodMachine | Perfume and ambient state feed Phase 1 entrance evaluation |

---

## Pillar 3 — Quiet Accumulation

**Nothing explodes. Effects compound slowly across the 7-day calendar.
Pressure is gentle and cumulative, not urgent.**

### Description

Iris operates at the speed of a houseplant. A plant you neglected yesterday is
not dead — it is slightly worse. A successful date does not transform the
apartment — it adds one flower. Seven days of missed watering produces a wilted
plant; seven days of good dates produces a room full of living flowers. The
calendar creates a container for this slow build without imposing urgency. There
is no fail state, no timer, no game-over. The pressure is the knowledge that
what you do today will be visible tomorrow.

This pillar resists the impulse toward spectacle. The game's emotional register
is quiet. A flower trimmed badly lasts fewer days. Plants that die stop
contributing to mood. A room full of healthy plants from good dates creates a
materially different atmosphere than a sparse apartment from ignored ones. The
accumulation is the story.

### How It Manifests in Gameplay

- The 7-day calendar paces the game without time-of-day urgency. One date per
  day maximum. No punishment for skipping a date — only the opportunity lost.
- Living plants are the persistent physical record of past dates. Their health
  degrades daily without watering. Their presence (or absence) feeds the
  MoodMachine's LivingPlants and AirQuality sources.
- Flowers trimmed poorly live fewer days; flowers trimmed well persist longer,
  accumulating into a denser ambient presence.
- Plant health directly influences future date outcomes via Phase 3 reactable
  evaluation (healthy plants impress; dead or dying ones disappoint).
- MoodMachine changes are smoothed over ~2 seconds, meaning the apartment's
  atmosphere drifts rather than snaps — every environmental input accumulates
  gradually into the ambient state.
- Arc completion (3 successful dates with the same character) is the macro-loop
  milestone — a slow burn across potentially multiple calendar cycles.

### Systems That Serve This Pillar

| System | Contribution |
|---|---|
| Living Plants | Daily decay, watering dependency, multi-day lifespan scoring |
| Flower Trimming | Cut quality determines plant lifespan — each score compounds forward |
| Watering System | Daily maintenance action with cumulative consequence |
| MoodMachine | Aggregates plant health, weather, time-of-day into slow ambient drift |
| Dating Loop | 7-day calendar, arc completion requiring 3 successful dates |
| Date Phase Scoring | Plant health feeds Phase 3 reactions, linking past care to future outcomes |

---

## Pillar 4 — Judgment You Can Feel

**The date's reactions are specific, physical, and tied to things the player
actually did. Feedback is never vague.**

### Description

When the date dislikes something, they dislike a specific thing you placed on
a specific surface. When they react to your music, they react to the track you
chose. When the perfume is wrong, it is wrong because you applied too much or
too little of it — the intensity is a number you could have changed. Every
affection outcome has a named cause, and the Phase 3 Reveal makes that causal
chain visible and felt: item by item, surface multiplier surfaced in a popup,
the flower growing or wilting with each reveal.

This pillar serves player agency. Vague feedback ("the date didn't enjoy
themselves") produces helplessness. Specific feedback ("your music was wrong,
your kitchen was clean, but you placed a disliked object on the 3x surface in
the living room") produces understanding — and motivation to adjust. The game
is difficult in a way that rewards attention, not in a way that punishes
ignorance.

Judgment also has physical texture. The date character walks to items. They
animate a reaction. The flower on the UI responds. The affection number shifts.
Each beat has a distinct visual and audio signature so the player always knows
what is being evaluated and what the verdict is.

### How It Manifests in Gameplay

- Phase 1 (Entrance) evaluates music, perfume, cleanliness, and outfit
  sequentially with one judgment visible at a time — players watch each verdict
  land rather than receiving a combined score.
- Phase 3 (Reveal) sorts items by surface multiplier and presents them to the
  player in a deliberate sequence, with world-space multiplier popups making
  the stakes of placement visible.
- In the redesigned scoring flow, the player actively clicks items during Phase
  3 to show them to the date — they are a curator, choosing what to highlight
  and in what order.
- Affection changes are shown as live flower growth/wilt on the UI, giving the
  player a continuous emotional barometer through all three phases.
- Grade and per-phase breakdown are surfaced at the Evening results screen,
  giving the player a retrospective on what worked.
- Per-character `reactionStrength` scalars mean that some characters respond
  more dramatically to the same conditions, teaching the player that characters
  are distinct people with distinct emotional registers.

### Systems That Serve This Pillar

| System | Contribution |
|---|---|
| Date Phase Scoring | Item-by-item reveal with multiplier popups; flower as live affection barometer |
| Dating Loop | Sequential three-phase structure; grade and results at Evening |
| Object Interaction | Surface multiplier assignment to placement spots |
| Record Player | Music track selection maps to per-character judgments |
| Tidiness System | Cleanliness score feeds Phase 1 with a precise threshold |
| MoodMachine | Perfume intensity — a tunable parameter — produces a specific judgment outcome |

---

## Anti-Pillars

These are things Iris / Spent Bloom explicitly is not. When a proposed feature
conflicts with an anti-pillar, that conflict is a design signal, not a
negotiation.

| Anti-Pillar | Rationale |
|---|---|
| **Not a puzzle with a single correct solution.** | Preparation as Expression requires that multiple approaches produce meaningfully different (not wrong) outcomes. Optimizing for a single correct apartment configuration destroys the expressive dimension. |
| **Not a game about urgency.** | Quiet Accumulation requires that pressure feels gentle. Countdown timers, fail states, and time-gated content undermine the contemplative register of the game. |
| **Not a simulation of social approval.** | The date's judgment should feel like a consequence of the player's choices, not an evaluation of the player's taste or worth. Arbitrary or unexplained reactions break the causal chain that Judgment You Can Feel depends on. |
| **Not a game with spectacle.** | The emotional scale is small — one person, one apartment, one evening. Features that introduce visual or mechanical spectacle (combat, dramatic setpieces, explosive feedback) conflict with the quiet, domestic register of all four pillars. |

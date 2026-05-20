# Systems Index — Iris / Spent Bloom

This index is the canonical map of all designed game systems. It tracks
documentation status, primary inter-system dependencies, and implementation
state. Update this file whenever a GDD is created, revised, or reaches a new
milestone.

---

## System Registry

| # | System | GDD | Primary Dependencies | Status |
|---|--------|-----|----------------------|--------|
| 1 | Apartment Hub | [apartment-hub.md](gdd/apartment-hub.md) | MoodMachine, AccessibilitySettings, IrisInput, CinemachineBrain, DayPhaseManager | Reverse-documented |
| 2 | Object Interaction | [object-interaction.md](gdd/object-interaction.md) | Apartment Hub (ApartmentManager.ScreenPointToRay), PlacementSurface, IrisInput | Reverse-documented |
| 3 | Tidiness System | [tidiness-system.md](gdd/tidiness-system.md) | Object Interaction (displaced object detection), Apartment Hub (area bounds), DailyMessSpawner | Reverse-documented |
| 4 | Drink Making | [drink-making.md](gdd/drink-making.md) | Object Interaction (bottle grab/pour), Dating Loop (Phase 2 trigger), DrinkPourManager | Reverse-documented |
| 5 | Watering System | [watering-system.md](gdd/watering-system.md) | Object Interaction (watering can grab), Living Plants (WaterablePlant targets) | Reverse-documented |
| 6 | Dating Loop | [dating-loop.md](gdd/dating-loop.md) | Apartment Hub, Object Interaction, Tidiness System, Drink Making, Watering System, Flower Trimming, Living Plants, MoodMachine, Record Player, Date Phase Scoring | Reverse-documented |
| 7 | Flower Trimming | [flower-trimming.md](gdd/flower-trimming.md) | Dating Loop (FlowerTrimmingBridge trigger, affection >= 30), Living Plants (LivingFlowerPlantManager handoff) | Reverse-documented |
| 8 | Living Plants | [living-plants.md](gdd/living-plants.md) | Flower Trimming (trimming score → lifespan), Watering System (daily health maintenance), MoodMachine (LivingPlants + AirQuality sources), Dating Loop (Phase 3 ReactableTag evaluation) | Reverse-documented |
| 9 | MoodMachine | [mood-machine.md](gdd/mood-machine.md) | Living Plants (health averages), WeatherSystem (discrete state), GameClock (time-of-day), Record Player (music source), Perfume (override profile) | Reverse-documented |
| 10 | Record Player | [record-player.md](gdd/record-player.md) | Object Interaction (record handling), MoodMachine (music mood source registration), Dating Loop (Phase 1 music judgment) | Reverse-documented |
| 11 | PSX Rendering | [psx-rendering.md](gdd/psx-rendering.md) | AccessibilitySettings (PSX toggle, dither intensity), URP Renderer Feature pipeline, AtmosphereController (post-process volume) | Reverse-documented |
| 12 | Accessibility | [accessibility.md](gdd/accessibility.md) | (standalone — all other systems read from it) | Reverse-documented |
| 13 | Date Phase Scoring | [date-phase-scoring-redesign.md](gdd/date-phase-scoring-redesign.md) | Dating Loop (DateSessionManager orchestration), ReactionEvaluator, ReactableTag, AffectionBar, EntranceJudgmentSequence, DateInspectSystem | Draft |

---

## Dependency Graph

This section maps inter-system dependencies as directed relationships: `A → B`
means system A depends on system B (A reads from, calls into, or requires B to
function).

### Standalone Systems (no upstream dependencies)

```
Accessibility
  (all other systems read from it; it depends on nothing)
```

### Infrastructure Layer

```
PSX Rendering → Accessibility
MoodMachine → Living Plants
MoodMachine → GameClock (external)
MoodMachine → WeatherSystem (external)
MoodMachine → Record Player (music source registration)
Apartment Hub → MoodMachine
Apartment Hub → Accessibility
```

### Interaction Layer

```
Object Interaction → Apartment Hub
Tidiness System → Object Interaction
Tidiness System → Apartment Hub
Watering System → Object Interaction
Watering System → Living Plants
Drink Making → Object Interaction
Drink Making → Dating Loop
Record Player → Object Interaction
Record Player → MoodMachine
```

### Plant Lifecycle Loop

```
Flower Trimming → Dating Loop
Flower Trimming → Living Plants
Living Plants → Flower Trimming
Living Plants → Watering System
Living Plants → MoodMachine
```

Note: The Living Plants / Flower Trimming relationship is bidirectional.
Flower Trimming produces new plant instances (via LivingFlowerPlantManager)
that Living Plants then manages. Living Plants does not call back into Flower
Trimming after handoff.

### Dating Loop (Orchestrator)

The Dating Loop is the top-level orchestrator. It reads from or signals into
every other system at some point in the day cycle:

```
Dating Loop → Apartment Hub (phase transitions, camera presets)
Dating Loop → Object Interaction (phase-locked grab state)
Dating Loop → Tidiness System (TidyScorer poll at Phase 1)
Dating Loop → Drink Making (Phase 2 trigger/receive)
Dating Loop → MoodMachine (perfume/music state queries)
Dating Loop → Record Player (music judgment query)
Dating Loop → Living Plants (Phase 3 ReactableTag evaluation)
Dating Loop → Flower Trimming (FlowerTrimmingBridge signal on affection >= 30)
Dating Loop → Date Phase Scoring (affection delta application, phase sequencing)
```

### Date Phase Scoring

```
Date Phase Scoring → Dating Loop (DateSessionManager — reverse: scoring is a
  subsystem of the loop, not independent of it)
Date Phase Scoring → Object Interaction (ReactableTag surface multipliers)
Date Phase Scoring → Accessibility (reduce-motion guards on reveal animations)
```

---

## Dependency Heat Map

Systems ranked by how many other systems depend on them (highest = most
critical path):

| Rank | System | Depended on by (count) | Risk if broken |
|------|--------|------------------------|----------------|
| 1 | Object Interaction | 5 (Tidiness, Drink Making, Watering, Record Player, Date Phase Scoring) | Critical — most player-facing actions route through grab/place |
| 2 | Dating Loop | 5 (Drink Making, Flower Trimming, Date Phase Scoring, and all systems it orchestrates) | Critical — day cycle collapses without it |
| 3 | MoodMachine | 4 (Apartment Hub, Living Plants, Record Player, Dating Loop) | High — atmosphere and date judgment both degrade |
| 4 | Living Plants | 3 (Watering System, Flower Trimming, MoodMachine) | High — persistent consequence system |
| 5 | Accessibility | 3 (Apartment Hub, PSX Rendering, Date Phase Scoring) | Medium — graceful degradation possible |
| 6 | Apartment Hub | 2 (Object Interaction, Tidiness System) | Medium — camera and area bounds |
| 7 | Flower Trimming | 2 (Dating Loop trigger, Living Plants handoff) | Medium — blocked dates still complete; no trimming scene loads |
| 8–13 | All others | 0–1 | Lower — isolated or leaf-node systems |

---

## Status Definitions

| Status | Meaning |
|--------|---------|
| **Reverse-documented** | GDD was written by reading existing implementation. Reflects current code behavior. Pending design review for completeness and correctness. |
| **Draft** | GDD written prospectively. Not yet implemented. Pending approval before implementation begins. |
| **Approved** | GDD passed design-review. Implementation may proceed. |
| **Implemented** | System is implemented and matches the approved GDD. Acceptance criteria verified. |
| **In Review** | GDD submitted for design-review and awaiting verdict. |

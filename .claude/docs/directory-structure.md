# Directory Structure

```text
/
├── CLAUDE.md                    # Master configuration
├── .claude/                     # Agent definitions, skills, hooks, rules, docs
├── Assets/                      # Unity project root
│   ├── Scripts/                 # Game source code
│   │   ├── Apartment/           # Hub, grabber, mood, tidy scoring, placement
│   │   ├── Bookcase/            # Books, perfume, drawers
│   │   ├── Camera/              # Camera presets, test controller
│   │   ├── Cleaning/            # Stain/mess cleaning systems
│   │   ├── Dating/              # Date session, characters, reactions, phases
│   │   ├── DynamicMeshCutter/   # Mesh cutting engine + plane behaviors
│   │   ├── Fluids/              # Sap particles, decal pooling
│   │   ├── Framework/           # TimeScale, AudioManager, GameClock, Accessibility
│   │   ├── GameLogic/           # FlowerGameBrain, scoring, session control
│   │   ├── InteractionAndFeel/  # Spring joints, squish, grab pull
│   │   ├── Mechanics/           # Drinks, watering, record player, cleaning
│   │   ├── Rendering/           # PSX render, volumetric light, atmosphere
│   │   ├── Tags/                # Tag system
│   │   ├── Testing/             # Runtime test helpers
│   │   └── UI/                  # HUD, grading, settings, captions, themes
│   ├── Editor/                  # Editor tools and wizards
│   │   └── Tests/               # NUnit tests (8 suites — Unity Test Runner)
│   ├── Scenes/                  # Game scenes
│   ├── Shaders/                 # PSXLit, PSXPost, RimLight, NatureBox sky
│   ├── ScriptableObjects/       # Gameplay data (flowers, dates, drinks, etc.)
│   └── Resources/               # Auto-loaded assets (IrisTextTheme, etc.)
├── design/                      # Game design documents
│   └── gdd/                     # Per-system GDD files
├── docs/                        # Technical documentation (gitignored)
├── tools/                       # Build and pipeline tools
├── prototypes/                  # Throwaway prototypes (isolated from Assets/)
└── production/                  # Production management
    ├── session-state/           # Ephemeral session state (active.md — gitignored)
    └── session-logs/            # Session audit trail (gitignored)
```

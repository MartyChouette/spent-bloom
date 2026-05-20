---
status: reverse-documented
source: [src/systems/AccessibilitySettings.cs, src/ui/SettingsPanel.cs,
         src/systems/IrisQualityManager.cs]
date: 2026-05-20
---

# Accessibility

> **Note**: This document was reverse-engineered from the existing implementation.

**Status**: Reverse-documented
**Depends on**: AudioManager, PSXRenderController, IrisQualityManager,
ScalableBufferManager, SettingsPanel, DayPhaseManager, all visual systems
that respond to ReduceMotion

---

## Overview

The accessibility system exposes 16 settings across 6 categories — Visual,
Audio, Motion, Timing, Controls, and Performance — through a static utility
class. No MonoBehaviour is required: `AccessibilitySettings` is a static class
that reads and writes `PlayerPrefs` with an "Iris_" prefix, fires a batched
`OnSettingsChanged` event when values change, and is domain-reset-safe. A
SettingsPanel UI provides six tabbed pages corresponding to the six categories,
with `_suppressCallbacks` guards to prevent feedback loops when the UI
populates its own controls. Every setting has a documented default, a defined
safe range, and a clear system contract.

## Player Fantasy

Every player deserves to experience the apartment on their terms. The player
who is motion-sensitive should not have to endure swimming geometry to enjoy
the story. The player with two hours should not be timed out of a date. The
player with hearing loss should not miss narrative information conveyed only
through audio. Good accessibility design should feel like the game was always
meant to work this way — not like a concession bolted on afterward.

## Detailed Rules

### Architecture

`AccessibilitySettings` is a static class (no MonoBehaviour, no scene object
required). All state lives in `PlayerPrefs` under keys with the prefix `"Iris_"`.
The class exposes:
- Static properties with get/set for each setting
- `OnSettingsChanged`: a static C# event fired when any setting changes
- `BeginChanges()` / `EndChanges()`: batch wrapper that defers the event until
  `EndChanges()` is called, firing once regardless of how many settings changed
- `ResetAll()`: wipes all "Iris_" PlayerPrefs keys, restores defaults, and
  calls `ScalableBufferManager.ResizeBuffers(1, 1)`
- A `[RuntimeInitializeOnLoadMethod]` that clears all static event subscribers
  on domain reload, preventing stale subscriptions across play sessions

All systems that respond to settings changes subscribe to `OnSettingsChanged`
and read the relevant property. They do not receive the specific setting that
changed — they are responsible for reading what they care about.

### Setting: ColorblindMode (Visual)

Enum: Normal, Deuteranopia, Protanopia, Tritanopia. Default: Normal.

Remaps "happy" and "sad" semantic colors used in UI feedback and visual signals:

| Mode         | Happy Color | Sad Color      |
|--------------|-------------|----------------|
| Normal       | Green       | Red            |
| Deuteranopia | Blue        | Orange         |
| Protanopia   | Blue        | Yellow-Orange  |
| Tritanopia   | Magenta     | Cyan           |

All systems that display semantic color (mood indicators, feedback icons, plant
health indicators) must read ColorblindMode when rendering. Raw sprite/material
colors are not modified — the remapping is applied at the UI palette level.

### Setting: HighContrast (Visual)

Bool. Default: false.

When true, UI elements increase their contrast ratio — typically by darkening
backgrounds, thickening outlines, or increasing text/background separation.
Specific implementation per UI component is delegated to each component's
HighContrast handler. The accessibility system fires `OnSettingsChanged`;
components handle their own visual update.

### Setting: TextScale (Visual)

Float in [0.8, 1.5]. Default: 1.0.

Multiplies the base font size of all TextMeshPro elements that subscribe to
TextScale changes. A value of 1.0 is the authored size. Values below 1.0 are
allowed for players who prefer denser text. UI layout containers must be able
to accommodate up to 1.5× without text clipping.

### Setting: MasterVolume (Audio)

Float in [0, 1]. Default: 1.0.

Applied as a master gain to the AudioManager's master AudioMixer group. All
other volume settings are applied on top of this as sub-group gains.

### Setting: MusicVolume (Audio)

Float in [0, 1]. Default: 1.0.

Applied to the music AudioMixer sub-group. Affects record player music and any
scene-triggered music tracks.

### Setting: SFXVolume (Audio)

Float in [0, 1]. Default: 1.0.

Applied to the SFX AudioMixer sub-group. Affects interaction sounds, UI clicks,
object pick-up, needle drop, etc.

### Setting: AmbienceVolume (Audio)

Float in [0, 1]. Default: 1.0.

Applied to the ambience AudioMixer sub-group. Note: MoodMachine also adjusts
ambience volume dynamically (0.6→0.3 range based on mood). The accessibility
AmbienceVolume is applied on top of MoodMachine's dynamic adjustment — it scales
the entire dynamic range, not just the ceiling.

### Setting: UIVolume (Audio)

Float in [0, 1]. Default: 1.0.

Applied to the UI AudioMixer sub-group. Affects button clicks, tab transitions,
setting confirmation sounds, and any HUD feedback audio.

### Setting: CaptionsEnabled (Audio)

Bool. Default: false.

When true, dialogue and important audio events display a text caption in a
dedicated UI overlay region. Caption text is authored separately from dialogue
(it may be condensed or formatted differently for readability). The captions
system reads this flag before displaying any caption.

### Setting: ReduceMotion (Motion)

Bool. Default: false.

When true, the following motion effects are suppressed:
- Camera sway (idle breathing animation on the player camera)
- Parallax (depth-layer separation on UI or scene elements)
- Vertex snapping (PSXLit `_snapResolution` overridden to 0 globally)
- Affine UV warping (PSXLit `_affineIntensity` overridden to 0 globally)

Shadow dithering, post-process resolution downscale, posterization, and
all non-geometric effects remain active under ReduceMotion. If PSXEnabled
is also toggled off, all PSX effects are suppressed regardless.

The change takes effect within one frame via `OnSettingsChanged`.

### Setting: ScreenShakeScale (Motion)

Float in [0, 1]. Default: 1.0.

Multiplies all screen shake impulses before they are applied to the camera.
At 0, all screen shake is suppressed. At 1.0, shake plays at authored intensity.
Intermediate values scale linearly. This setting is independent of ReduceMotion
— a player may want reduced shake but still allow camera sway and parallax.

### Setting: TimerMultiplier (Timing)

Enum with underlying float multiplier. Default: Normal.

| Option   | Multiplier | Description                                   |
|----------|------------|-----------------------------------------------|
| Normal   | 1.0        | Authored timer durations as designed          |
| Relaxed  | 1.5        | 50% more time for all timed interactions      |
| Extended | 2.0        | Double time for all timed interactions        |
| NoTimer  | 0          | Timers do not count down (infinite time)      |

Applied by `DayPhaseManager` and any system with a timed interaction. The
multiplier is applied to the designed timer duration at the point the timer
is started. A timer already running when the setting changes is not
retroactively adjusted.

The SettingsPanel dropdown uses hysteresis mapping when reading the current
float value back into a dropdown index (to handle floating-point imprecision
from PlayerPrefs):
- 0.0 – 1.25 → Normal
- 1.25 – 1.75 → Relaxed
- 1.75+ → Extended
- 0 (exactly) → NoTimer (or sentinel value detection)

### Setting: InvertScroll (Controls)

Bool. Default: false.

When true, scroll wheel and drag-scroll input directions are inverted. Applied
at the input processing layer, before any system reads scroll delta. All
scrollable UI panels and any scroll-driven gameplay interaction must route
through the centralized scroll input accessor that applies this inversion.

### Setting: ResolutionScale (Performance)

Float in [0.5, 1.0]. Default: 1.0.

Applied via `ScalableBufferManager.ResizeBuffers(resolutionScale, resolutionScale)`.
At 1.0, the render buffer is native resolution. At 0.5, the render buffer is
half-resolution in each dimension (quarter total pixel count). This is
independent of PSX resolutionDivisor — both can be active simultaneously,
compounding the pixel reduction.

`ResetAll()` explicitly calls `ResizeBuffers(1, 1)` to restore native
resolution as part of the full reset.

### Setting: QualityPreset (Performance)

Int. Default: -1 (auto).

Passed to `IrisQualityManager.SetPreset(int)`. At -1, IrisQualityManager
selects an appropriate preset based on detected hardware capabilities. Positive
integers correspond to named quality tiers defined in IrisQualityManager.
The accessibility system stores and restores this value but defers all
interpretation to IrisQualityManager.

### Setting: PSXEnabled (Performance)

Bool. Default: true.

Enables or disables the PSXPostProcessFeature fullscreen post-process pass.
When false, the retro aesthetic is removed — resolution downscale, posterization,
and dithering do not run. Object shaders (PSXLit) remain assigned but their
fullscreen-dependent contributions are reduced.

This setting is also the target of the F4 developer hotkey (editor/debug builds
only). Changing it fires `OnSettingsChanged`, which `PSXRenderController`
listens to in order to enable/disable the renderer feature.

### SettingsPanel UI

The SettingsPanel contains six tabs, one per category. When a tab becomes
active, the panel populates its controls from the current `AccessibilitySettings`
values. A `_suppressCallbacks` bool is set to true during population and
restored to false afterward, preventing the act of populating controls from
triggering settings-changed events.

User interactions with controls (sliders, dropdowns, toggles) call the
corresponding AccessibilitySettings setter, which writes to PlayerPrefs and
fires `OnSettingsChanged`. The SettingsPanel does not batch changes — each
control interaction fires the event independently unless the user code wraps
in `BeginChanges()`/`EndChanges()`.

## Formulas

**TimerMultiplier application:**
```
actualDuration = designedDuration × TimerMultiplier
```
- NoTimer case: TimerMultiplier = 0 → actualDuration = 0 → timer never expires
- Example: designed duration = 120s, Relaxed (1.5×) → actualDuration = 180s

**ResolutionScale buffer size:**
```
renderBufferWidth  = nativeWidth  × ResolutionScale
renderBufferHeight = nativeHeight × ResolutionScale
```
- Applied via `ScalableBufferManager.ResizeBuffers(ResolutionScale, ResolutionScale)`
- At ResolutionScale=0.5, 1920×1080 → 960×540 render buffer

**Effective pixel resolution with both PSX and ResolutionScale active:**
```
effectiveWidth  = floor((nativeWidth  × ResolutionScale) / resolutionDivisor)
effectiveHeight = floor((nativeHeight × ResolutionScale) / resolutionDivisor)
```
- Example: 1920×1080, ResolutionScale=0.75, resolutionDivisor=3.0
  → render buffer: 1440×810 → PSX downscale: 480×270

**TextScale application:**
```
renderedFontSize = authoredFontSize × TextScale
```
- All subscribed TMP elements multiply their authored size by this scalar

**ScreenShake application:**
```
appliedShakeIntensity = designedShakeIntensity × ScreenShakeScale
```
- At ScreenShakeScale=0: appliedShakeIntensity = 0 (no shake)

**TimerMultiplier hysteresis (dropdown read-back):**
```
if storedMultiplier == 0:         dropdown = NoTimer
elif storedMultiplier < 1.25:     dropdown = Normal
elif storedMultiplier < 1.75:     dropdown = Relaxed
else:                             dropdown = Extended
```

## Edge Cases

**ResetAll during active play session:** All settings revert to defaults. A
timer currently running at Relaxed speed is not retroactively adjusted — it
continues at its original calculated duration. The new TimerMultiplier applies
only to timers started after the reset. Designers should ensure the apartment
state is not corrupted by mid-session defaults (e.g., ResolutionScale snapping
to 1.0 during a slow-device session is valid; ScalableBufferManager handles
the resize).

**BeginChanges/EndChanges nesting:** Nesting these calls is not supported. A
second `BeginChanges()` before `EndChanges()` is a bug. The system should log
a warning if `BeginChanges()` is called while already in a batch.

**PlayerPrefs corruption:** If a stored value is outside its defined range
(e.g., TextScale = 5.0 from a corrupted pref), the property getter should
clamp to the safe range on read and re-write the clamped value. This prevents
downstream systems from receiving out-of-range values.

**OnSettingsChanged with no subscribers:** The event fires without error. This
is the normal state before any scene is loaded.

**Domain reload (editor):** `[RuntimeInitializeOnLoadMethod]` clears all static
event subscribers. Any system that subscribes in Awake will resubscribe on
the next play session. Static subscriptions from editor tooling (if any) must
resubscribe manually.

**AmbienceVolume interaction with MoodMachine:** MoodMachine adjusts ambience
dynamically in a 0.6→0.3 range. AccessibilitySettings.AmbienceVolume scales
the AudioMixer group. If AmbienceVolume=0.5, the effective range becomes
0.3→0.15. Both can be 0 simultaneously (silent ambience); this is valid.

**CaptionsEnabled with no caption text authored:** The caption overlay displays
nothing. No error. Missing caption data should be flagged during content review,
not at runtime.

**TextScale at 1.5 overflowing UI containers:** This is a UI layout bug, not
an accessibility bug. All UI containers must be tested at TextScale=1.5 before
release. The accessibility system is not responsible for layout — it provides
the value, layout is the SettingsPanel designer's responsibility.

**InvertScroll applied to mouse wheel in UI lists:** All scrollable lists must
route through the centralized scroll accessor. Any list that reads Input.mouseScrollDelta
directly will not respect InvertScroll and must be refactored.

**QualityPreset=-1 on a device IrisQualityManager does not recognize:** The
auto-detection path must have a fallback to the lowest quality preset rather
than throwing an unhandled exception.

**F4 key in release build:** The F4 toggle must be guarded with a development-
build check. Shipping a release build with a live PSX toggle key would expose
an unintended accessibility shortcut and potentially confuse players.

## Dependencies

**AccessibilitySettings writes to / calls:**
- `AudioManager` — volume group levels (Master, Music, SFX, Ambience, UI)
- `PSXRenderController` — PSXEnabled toggle, ReduceMotion parameters
- `IrisQualityManager` — QualityPreset application
- `ScalableBufferManager` — ResizeBuffers on ResolutionScale change and ResetAll
- `DayPhaseManager` — TimerMultiplier applied to phase timers

**AccessibilitySettings is read by:**
- All TextMeshPro UI elements that subscribe to TextScale
- All systems displaying semantic color (ColorblindMode consumers)
- Camera sway, parallax, and screen shake systems (ReduceMotion, ScreenShakeScale)
- PSXLit object shaders via PSXObjectSettings (ReduceMotion → snap/affine override)
- Captions overlay (CaptionsEnabled)
- Scroll input processor (InvertScroll)
- Any timed interaction (TimerMultiplier)

**SettingsPanel** is the primary user-facing interface for AccessibilitySettings.
It does not own any settings state — it reads from and writes to
AccessibilitySettings.

**MoodMachine** interacts indirectly through AmbienceVolume: MoodMachine
drives the ambience AudioMixer group dynamically; AccessibilitySettings scales
the same group. Both are valid concurrent writers — the AudioMixer accepts the
last-written value, so MoodMachine's per-frame updates will override any
static AccessibilitySettings write unless the system multiplies correctly.
Implementation must ensure MoodMachine reads AccessibilitySettings.AmbienceVolume
and scales its output accordingly rather than writing absolute values.

## Tuning Knobs

| Setting              | Default | Safe Range  | Notes                                              |
|----------------------|---------|-------------|----------------------------------------------------|
| ColorblindMode       | Normal  | enum        | Color remapping; confirm palette with art director |
| HighContrast         | false   | bool        | Per-component implementation required              |
| TextScale            | 1.0     | 0.8 – 1.5   | All containers tested at 1.5×                      |
| MasterVolume         | 1.0     | 0.0 – 1.0   | Global gain                                        |
| MusicVolume          | 1.0     | 0.0 – 1.0   | Music sub-group                                    |
| SFXVolume            | 1.0     | 0.0 – 1.0   | SFX sub-group                                      |
| AmbienceVolume       | 1.0     | 0.0 – 1.0   | Scales MoodMachine's dynamic range                 |
| UIVolume             | 1.0     | 0.0 – 1.0   | UI audio sub-group                                 |
| CaptionsEnabled      | false   | bool        | Caption content must be authored separately        |
| ReduceMotion         | false   | bool        | Suppresses snap, affine, sway, parallax            |
| ScreenShakeScale     | 1.0     | 0.0 – 1.0   | Linear scale on all shake impulses                 |
| TimerMultiplier      | 1.0     | 0, 1.0, 1.5, 2.0 | NoTimer=0; affects all timed phase durations  |
| InvertScroll         | false   | bool        | All scroll consumers must route through accessor   |
| ResolutionScale      | 1.0     | 0.5 – 1.0   | ScalableBufferManager; stacks with PSX divisor     |
| QualityPreset        | -1      | -1 to N     | -1 = auto; N defined by IrisQualityManager         |
| PSXEnabled           | true    | bool        | Disables fullscreen retro post-process             |

## Acceptance Criteria

1. **PlayerPrefs persistence:** Set TextScale to 1.3. Quit and relaunch. Confirm
   TextScale reads as 1.3 without any manual re-application.

2. **OnSettingsChanged fires once per BeginChanges/EndChanges block:** Call
   `BeginChanges()`, change MusicVolume, SFXVolume, and UIVolume, call
   `EndChanges()`. Confirm `OnSettingsChanged` fired exactly once.

3. **ResetAll restores all defaults:** Modify all 16 settings to non-default
   values. Call `ResetAll()`. Read all 16 settings and confirm each matches
   its documented default. Confirm `ScalableBufferManager.ResizeBuffers(1,1)`
   was called.

4. **ColorblindMode Deuteranopia:** Set ColorblindMode to Deuteranopia. Confirm
   all semantic "happy" color indicators display blue. Confirm all semantic
   "sad" indicators display orange.

5. **ReduceMotion suppresses vertex snap:** Enable ReduceMotion. Confirm a
   PSXLit object moves without vertex snapping (smooth continuous motion).
   Confirm the change took effect within one rendered frame.

6. **ReduceMotion live update:** While the game is running, toggle ReduceMotion
   from false to true. Vertex snap should stop within one frame without
   requiring a restart.

7. **TimerMultiplier Extended doubles timer:** In a timed interaction with a
   designed duration of 60 seconds, set TimerMultiplier to Extended (2.0).
   Confirm the timer runs for 120 seconds before expiring.

8. **TimerMultiplier NoTimer:** Set TimerMultiplier to NoTimer. Start a timed
   interaction. Confirm the timer never expires regardless of how long it runs.

9. **ResolutionScale=0.5 halves buffer dimensions:** Set ResolutionScale to 0.5.
   Confirm `ScalableBufferManager.ResizeBuffers(0.5, 0.5)` was called.

10. **PSXEnabled toggle via setting:** Set PSXEnabled to false. Confirm
    PSXPostProcessFeature is disabled (scene renders without downscale or
    dithering). Set PSXEnabled to true. Confirm the effect returns.

11. **InvertScroll inverts scroll delta:** Set InvertScroll to true. Scroll
    the mouse wheel up. Confirm the scroll input accessor returns a negative
    delta (inverted). Confirm all scrollable UI panels respond to the inverted
    direction.

12. **TextScale at 1.5 does not clip:** Set TextScale to 1.5. Navigate every
    UI panel. Confirm no text is clipped, truncated, or overflows its container.

13. **AmbienceVolume and MoodMachine coexist:** Set AmbienceVolume to 0.5. Let
    MoodMachine shift mood from 0 to 1 (ambience range 0.6→0.3). Confirm the
    audible ambience range is approximately 0.3→0.15 (halved by the setting).

14. **_suppressCallbacks prevents loop:** Open the SettingsPanel. Confirm that
    populating the controls does not trigger any `OnSettingsChanged` events
    (verify with a test listener that counts events during panel open).

15. **Domain reload clears subscribers:** In the editor, subscribe a test
    listener to `OnSettingsChanged`, exit play mode, enter play mode again.
    Confirm the test listener from the previous session is no longer subscribed.

16. **PlayerPrefs range clamping:** Manually write a TextScale value of 99.0
    to PlayerPrefs key "Iris_TextScale". Launch the game. Confirm
    AccessibilitySettings.TextScale reads as 1.5 (clamped to max), and the
    PlayerPrefs key has been rewritten to 1.5.

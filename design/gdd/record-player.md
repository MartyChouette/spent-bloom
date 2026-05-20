---
status: reverse-documented
source: [src/gameplay/RecordSlot.cs, src/gameplay/VinylDisc.cs, src/gameplay/AlbumSleeve.cs,
         src/gameplay/ToneArm.cs, src/data/RecordDefinition.cs]
date: 2026-05-20
---

# Record Player

> **Note**: This document was reverse-engineered from the existing implementation.

**Status**: Reverse-documented
**Depends on**: ObjectGrabber, AudioManager, MoodMachine, ReactableTag,
AlbumSleeve, ToneArm, VinylDisc, RecordDefinition (ScriptableObject)

---

## Overview

The record player is a fully physical vinyl workflow: the player peeks at an
album sleeve, extracts the disc, carries it across the room, places it on the
turntable, watches the tone arm swing down, and hears music begin. Each step
has tactile feedback through animation and sound. The system integrates with
both the MoodMachine (the playing record's mood value becomes a named source)
and the date evaluation system (ReactableTag drives music judgment during date
phases). Auto-eject handles the common case of swapping records gracefully.

## Player Fantasy

The player should feel the ritual of vinyl. Pulling a record out of its sleeve,
walking it over, setting the needle — these gestures are slow, deliberate, and
satisfying in proportion to their care. The apartment should feel different
depending on what's playing, and the act of choosing a record should feel like
choosing a mood. Pausing music by clicking the turntable, rather than a UI
button, reinforces that everything in this space is tangible.

## Detailed Rules

### Components

- **RecordSlot**: Singleton component on the turntable. Manages placement,
  playback state, auto-eject, and lid animation.
- **VinylDisc**: Attached to each disc prefab. Carries a reference to its
  RecordDefinition. Manages grab state and platter spin animation.
- **AlbumSleeve**: Attached to each sleeve. Manages peek animation (tilt +
  vinyl slide), extract interaction, and vinyl return animation.
- **ToneArm**: Attached to the physical tone arm. Manages swing-to-play and
  swing-to-rest animations with needle SFX.

### Interaction Step 1 — PEEK

Hovering the cursor over an AlbumSleeve triggers the peek animation:
- The sleeve tilts (tiltSpeed applied each frame)
- The vinyl inside slides upward (peekSpeed applied each frame)
- A peek SFX plays on hover-enter

The peek animation reverses on hover-exit, returning to the resting state.
This gives the player a preview that a vinyl disc is present without committing
to extraction.

### Interaction Step 2 — EXTRACT

Clicking an AlbumSleeve calls `AlbumSleeve.ExtractVinyl()`:
- The VinylDisc is unparented from the sleeve
- Physics is enabled on the disc
- The disc's world-space scale is preserved through the unparenting operation
- The ObjectGrabber immediately picks up the disc (it enters carry state)

### Interaction Step 3 — CARRY

While the player holds a VinylDisc, it follows the cursor using a spring-damper
grab (handled by ObjectGrabber). The disc does not use rigid physics during
carry — it floats smoothly.

A magnetic snap zone is active around the turntable platter at a 0.6-meter
radius. When a carried disc enters this radius, it snaps visually toward the
placement point, giving the player guidance that the turntable will accept a
drop.

### Interaction Step 4 — PLACE

Clicking the turntable while holding a disc calls `RecordSlot.TryAcceptVinyl()`:
- The disc is positioned at `platePlacementPoint` (a child transform on the
  RecordSlot, centered on the platter)
- The disc is NOT reparented to the turntable — it remains a free object at a
  fixed world position
- The RecordSlot's lid opens automatically (0.4s smoothstep tween)
- If a different vinyl was already on the platter and playing (or paused), it
  is automatically ejected back to its matching AlbumSleeve before the new
  disc is accepted

### Interaction Step 5 — PLAY

Once a disc is on the platter, clicking the turntable body (not the disc)
begins playback:
- `ToneArm.SwingToPlay()` animates the arm over 1.2 seconds using an
  EaseInOut curve
- When the swing completes, a needle-drop SFX plays
- `AudioManager.PlayMusic(clip, volume)` starts playback of the record's
  `musicClip` (or loads from `musicClipPath` via Resources.Load)
- `MoodMachine.SetSource("Music", moodValue)` registers the record's mood
  contribution
- `ReactableTag.IsActive` is set to true on the RecordSlot, enabling date
  evaluation integration

Platter spin begins simultaneously with playback:
- The platter rotates at 33.3 degrees per second (vinyl RPM equivalent)
- Speed ramps up from 0 over `spinTransitionDuration` (1.5 seconds) using
  `MoveTowards` for a gradual, smooth acceleration

### Interaction Step 6 — PAUSE

Clicking the turntable body while music is playing pauses playback:
- `ToneArm.SwingToRest()` animates the arm back, with the needle-lift SFX
  playing at the start of the swing (before the arm reaches rest)
- `AudioManager.PauseMusic()` pauses the audio clip, preserving playback
  position
- `ReactableTag.IsActive` is set to false
- Platter spin decelerates using `MoveTowards` until it reaches 0

### Interaction Step 7 — RESUME

Clicking the turntable body while paused resumes playback:
- `ToneArm.SwingToPlay()` (same 1.2s animation as initial play)
- `AudioManager.ResumeMusic()` resumes from the preserved pause position
- `ReactableTag.IsActive` is set to true
- Platter spin ramps back up over `spinTransitionDuration`

### Interaction Step 8 — EJECT TO HAND

Clicking the vinyl disc on the platter while the system is in a paused state:
- `AudioManager.StopMusic()` (playback position is discarded)
- `MoodMachine.RemoveSource("Music")` removes the mood contribution
- `ToneArm.SnapToRest()` moves the arm to rest position instantly (no tween)
- The disc is configured for grab: physics enabled, ready for ObjectGrabber
- The lid remains open until the player takes the disc away, then closes

### Interaction Step 9 — RETURN TO SLEEVE

Clicking the matching AlbumSleeve while holding a VinylDisc returns it:
- The disc is reparented to the sleeve
- A 0.3-second slide-in animation plays, returning the disc to its resting
  position inside the sleeve
- Physics is disabled on the disc
- The sleeve returns to its un-peeked resting state once the animation completes

### Auto-Eject

When `TryAcceptVinyl()` is called and a different vinyl is already on the
platter:
1. The current record stops (StopMusic, RemoveSource)
2. ToneArm snaps to rest (instant)
3. The current disc is ejected to its matching AlbumSleeve via the same
   return-to-sleeve sequence (0.3s animation)
4. The new disc is then placed on the platter
5. The lid opens (or stays open if already open)

Auto-eject does not occur if the same vinyl is already on the platter (placing
it again is a no-op).

### Lid Behavior

The RecordSlot lid is a separate transform animated via smoothstep tween over
0.4 seconds. Closed state rotation: (-80, 0, 0). Open state rotation: (0, 0, 0).
The lid opens automatically when a vinyl is placed. The lid may be closed manually
(interaction not yet specified in source — confirm with implementation).

### Music Loading

RecordDefinition supports two music loading strategies:
- `musicClip`: a direct AudioClip reference (assigned in Inspector)
- `musicClipPath`: a string path for `Resources.Load<AudioClip>()`, used for
  clips too large to keep in memory at all times

If both are assigned, `musicClip` takes priority. If neither is assigned, no
audio plays (RecordSlot should log a warning).

### Date Integration

While a record is playing (`ReactableTag.IsActive = true`):
- Phase 1 (music judgment): the date NPC evaluates the playing record's genre
  and mood as part of their first impression
- Phase 3 (item evaluation): the record is included as an interactive item
  subject to date scoring rules

See the date-session design document for full phase rules.

## Formulas

**Platter rotation speed:**
```
rotationThisFrame = MoveTowards(currentSpeed, targetSpeed, (33.3 / spinTransitionDuration) × deltaTime)
plateTransform.rotation += rotationThisFrame × deltaTime
```
- `targetSpeed` = 33.3 deg/sec during play, 0 during pause/stop
- `spinTransitionDuration` = 1.5s
- Ramp rate = 33.3 / 1.5 = 22.2 deg/sec per second of acceleration
- Example: After 0.75s of spin-up, currentSpeed ≈ 16.65 deg/sec (halfway)

**Lid tween:**
```
t = smoothstep(0, 1, elapsedTime / 0.4)
lid.localRotation = Lerp(closedAngle, openAngle, t)
```
- `closedAngle` = (-80, 0, 0) Euler
- `openAngle` = (0, 0, 0) Euler
- Duration = 0.4 seconds
- smoothstep: `t = t × t × (3 - 2t)` for eased motion

**ToneArm swing:**
```
t = EaseInOut(elapsedTime / 1.2)
arm.localRotation = Lerp(restAngle, playAngle, t)
```
- Duration = 1.2 seconds
- EaseInOut prevents mechanical snap at start and end of swing
- Needle-drop SFX fires at t=1.0 (swing complete) during SwingToPlay
- Needle-lift SFX fires at t=0.0 (swing start) during SwingToRest

**Perfume-equivalent: magnetic snap radius:**
```
snapActive = Vector3.Distance(disc.position, platePlacementPoint.position) < 0.6
```
- Snap radius = 0.6 meters in world space

## Edge Cases

**Placing same vinyl again:** If the vinyl on the platter matches the incoming
vinyl, `TryAcceptVinyl()` is a no-op. No auto-eject, no animation restart.

**Dropping vinyl on the floor (not on turntable):** Physics takes over once
the grab is released outside the snap radius. The disc becomes a physics prop.
The player can pick it up again. No state is corrupted.

**Clicking turntable body while disc is in transit (0.3s slide-in animation):**
Play interaction should be blocked until the placement animation completes.
Implementation should guard with an `isAnimating` flag.

**ToneArm interrupted mid-swing:** If the player interacts with the turntable
while the arm is mid-swing, the swing animation should complete or snap to its
destination before responding to the new input. Interrupting mid-swing risks
visual artifacts and audio sync issues.

**musicClip and musicClipPath both null:** No audio plays. RecordSlot logs a
warning. The record still places, the arm still swings, the ReactableTag still
activates — only the audio is absent.

**AlbumSleeve has no vinyl remaining after extract:** The sleeve enters an
empty state. Hovering the empty sleeve shows no peek animation (nothing to
slide). Clicking the empty sleeve is a no-op (no extract available).

**Returning wrong vinyl to sleeve:** The player may attempt to slide vinyl B
into sleeve A. The system should only accept the disc whose `RecordDefinition`
matches the sleeve's reference. A mismatch results in no action and potentially
a rejection SFX (confirm with implementation).

**Auto-eject during date phase:** Auto-ejecting a record mid-date removes its
MoodMachine source immediately, shifting atmosphere. This is intended — the
player bears responsibility for swapping records during a date.

**SnapToRest during eject-to-hand:** The tone arm snaps instantly rather than
tweening. This is intentional — clicking a disc to take it is an urgent action
and the 1.2s swing would feel like delay.

## Dependencies

**RecordSlot reads from:**
- `RecordDefinition` (ScriptableObject) — title, artist, genre, moodValue,
  musicClip, musicClipPath, volume

**RecordSlot writes to / calls:**
- `AudioManager.PlayMusic()`, `PauseMusic()`, `ResumeMusic()`, `StopMusic()`
- `MoodMachine.SetSource("Music", moodValue)` / `RemoveSource("Music")`
- `ReactableTag.IsActive` — signals date evaluation system
- `ToneArm.SwingToPlay()`, `SwingToRest()`, `SnapToRest()`
- `AlbumSleeve.ExtractVinyl()`, return-to-sleeve animation

**RecordSlot is read by:**
- Date evaluation system (via `ReactableTag`) — Phase 1 and Phase 3 scoring
- `MoodMachine` — receives "Music" source updates

**ObjectGrabber** manages disc carry physics and drop detection. RecordSlot
listens for drop events within snap radius.

**AlbumSleeve** is a sibling system that manages individual disc storage.
RecordSlot coordinates with AlbumSleeve for extract and auto-eject return.

## Tuning Knobs

| Knob                      | Current Default  | Safe Range         | Affects                                       |
|---------------------------|------------------|--------------------|-----------------------------------------------|
| Platter speed             | 33.3 deg/sec     | 20 – 45            | Visual spin speed; 33.3 matches real vinyl RPM|
| Spin transition duration  | 1.5s             | 0.5 – 3.0s         | Acceleration feel on play/pause               |
| ToneArm swing duration    | 1.2s             | 0.6 – 2.0s         | Ritualistic weight of starting a record       |
| Snap radius               | 0.6m             | 0.3 – 1.0m         | How easy it is to place a disc on the platter |
| Lid tween duration        | 0.4s             | 0.2 – 0.8s         | Speed of lid open/close                       |
| Vinyl slide-in duration   | 0.3s             | 0.15 – 0.6s        | Speed of return-to-sleeve animation           |
| peekSpeed                 | (from source)    | (from source)      | Responsiveness of hover peek animation        |
| tiltSpeed                 | (from source)    | (from source)      | Responsiveness of sleeve tilt on hover        |
| RecordDefinition.moodValue| per-record (0–1) | 0.0 – 1.0          | This record's contribution to MoodMachine     |
| RecordDefinition.volume   | per-record (0–1) | 0.5 – 1.0          | Per-record playback volume                    |

## Acceptance Criteria

1. **Peek animation triggers:** Hover over a sleeved vinyl. The sleeve tilts
   and the disc slides upward. Hover-exit reverses the animation.

2. **Extract unparents disc:** Click a sleeved vinyl. The VinylDisc is no
   longer a child of AlbumSleeve and is held by ObjectGrabber.

3. **Magnetic snap:** Carry a disc to within 0.6m of the turntable. The disc
   visually snaps toward the platter placement point.

4. **Placement positions disc correctly:** Drop the disc on the turntable. The
   disc appears centered at `platePlacementPoint`. It is not parented to the
   turntable transform.

5. **Lid opens on placement:** Place a disc. The lid completes its open tween
   within 0.4 seconds.

6. **ToneArm swing duration:** Start playback. Measure the time between click
   and needle-drop SFX. Duration is 1.2 seconds (±0.1).

7. **Music plays after arm swing:** After the needle-drop SFX, music is audible
   and the AudioManager reports IsPlaying=true.

8. **MoodMachine source registered:** While music plays, MoodMachine has an
   active source keyed "Music" with a value matching the record's moodValue.

9. **ReactableTag active while playing:** While music plays, the RecordSlot's
   ReactableTag.IsActive is true. After pause, it is false.

10. **Pause preserves position:** Pause a song at ~30 seconds. Resume. The
    music continues from ~30 seconds, not from the beginning.

11. **Eject-to-hand stops music:** Click the disc on the platter while paused.
    AudioManager.IsPlaying is false. MoodMachine has no "Music" source. The
    arm snaps to rest instantly (no tween).

12. **Return to sleeve:** Hold an extracted disc. Click its matching sleeve.
    The disc reparents and the 0.3s slide-in animation plays.

13. **Auto-eject on new placement:** Place Vinyl A (playing). Then place Vinyl B.
    Vinyl A animates back to its sleeve. Vinyl B is on the platter.

14. **Same vinyl re-placement is no-op:** Place Vinyl A. Eject it. Place it
    again. No auto-eject animation plays (nothing to eject).

15. **Resources.Load path:** Assign only `musicClipPath` (no `musicClip`).
    Confirm music plays. Confirm no NullReferenceException.

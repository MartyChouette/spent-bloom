---
status: reverse-documented
source: [src/rendering/PSXPostProcessFeature.cs, src/rendering/PSXObjectSettings.cs,
         src/rendering/PSXRenderController.cs, assets/shaders/PSXLit.shader,
         assets/shaders/PSXPost.shader]
date: 2026-05-20
---

# PSX Rendering

> **Note**: This document was reverse-engineered from the existing implementation.

**Status**: Reverse-documented
**Depends on**: URP Renderer Feature pipeline, VolumeManager, AccessibilitySettings,
IrisQualityManager, Shader "Iris/PSXLit", Shader "Iris/Fullscreen/PSXPost"

---

## Overview

The PSX rendering system gives Iris/Spent Bloom its signature lo-fi aesthetic
by applying three cooperative layers of retro distortion: the PSXLit object
shader introduces vertex snapping, affine UV warping, and shadow dithering at
the geometry level; the PSXPostProcessFeature fullscreen pass applies resolution
downscale, color depth reduction, ordered dithering, and tilt-shift blur; and
the PSXObjectSettings component enables per-object overrides of any shader
parameter without creating material instances. The system is globally
togglable for accessibility and performance, and can be compared against the
clean render at runtime via a hotkey. Design rule: artist materials are never
swapped to PSXLit — the shader is assigned at scene build time and stays as
authored.

## Player Fantasy

The player should feel like they are playing a game remembered imperfectly —
soft edges where sharpness should be, colors that pool rather than grade, a
room that feels handmade rather than rendered. The PSX look is not nostalgia
for its own sake: it flattens emotional distance, making the apartment feel
warmer and more intimate than a clean render would. The aesthetic should
recede into atmosphere, noticed only when absent.

## Detailed Rules

### Layer 1: Object Shader (PSXLit)

The PSXLit shader is applied to geometry that should participate in the retro
aesthetic. It operates in three distinct modes per-vertex:

**Vertex Snapping:**
Vertex positions in clip space are quantized to a lower-resolution grid before
rasterization. This produces the characteristic swimming geometry of early 3D
hardware. The grid resolution is set by `_snapResolution` (see Per-Object
Overrides). The global default is equivalent to a 160×120 virtual framebuffer.

**Affine Texture Mapping:**
Standard UV interpolation is perspective-correct (divides by W). Affine
mapping skips the W division, producing the warped, sliding textures of
PSX-era polygons. `_affineIntensity` blends between perspective-correct (0)
and fully affine (1). The global default is 1.0 (fully affine).

**Shadow Dithering:**
Shadow boundaries use Bayer pattern stippling rather than smooth gradients.
`_shadowDither` controls intensity. At 0, shadows are unaffected. At 1, the
full Bayer pattern is applied to the shadow penumbra.

### Layer 2: Post-Process (PSXPostProcessFeature)

The URP Renderer Feature runs a fullscreen blit each frame using the PSXPost
shader. It applies four effects in sequence:

**Resolution Downscale:**
The screen is downscaled by `resolutionDivisor` (default 3.0, supports
fractional values) using point filtering, then upscaled back to native
resolution with point filtering. This produces large square pixels. At
resolutionDivisor=3.0, a 1920×1080 screen renders at 640×360.

**Color Depth Reduction (Posterization):**
Each color channel is quantized to `colorDepth` levels (default 32). Values
between quantization steps are rounded, not interpolated. This banding
matches the limited color palette of early hardware.

**Ordered Dithering:**
A Bayer matrix is tiled across the screen and used to threshold-dither dark
regions, adding texture to otherwise flat posterized areas. Two Bayer
patterns are used, selected by luminance thresholds:
- Above `shadowThreshold` (default 0.5): no dithering applied
- Between `shadowThreshold` and `deepShadowThreshold` (default 0.15):
  fine pattern (`finePattern`, default Bayer4x4)
- Below `deepShadowThreshold`: coarse pattern (`coarsePattern`, default Bayer2x2)

Dither intensity is scaled by `ditherIntensity` (default 0.5) and by a zoom
factor derived from camera size (see Formulas).

**Tilt-Shift Blur:**
A focal band is defined by `tiltShiftCenter` (0–1 of screen height, default 0.5)
and `tiltShiftWidth` (default 0.15). Pixels outside the focal band are blurred
by `tiltShiftRadius` (default 8, kernel radius in pixels). `tiltShiftAmount`
(default 0) scales the overall effect — at 0, no blur is applied. This simulates
miniature photography depth-of-field.

### Layer 3: Per-Object Overrides (PSXObjectSettings)

`PSXObjectSettings` is a MonoBehaviour placed on any GameObject that needs to
deviate from global PSX parameters. It writes to a fresh MaterialPropertyBlock
per renderer, preserving shared materials and never creating material instances.

The override system uses sentinel values to distinguish "use global" from
"use custom":

| Property           | Sentinel (use global) | Override range        |
|--------------------|-----------------------|-----------------------|
| `_snapResolution`  | -1                    | 0 (disabled) or >0   |
| `_affineIntensity` | -1                    | 0.0 – 1.0            |
| `_shadowDither`    | -1                    | 0.0 – 1.0            |

Setting `_snapResolution` to 0 disables vertex snapping on that object
entirely (useful for UI elements or objects that must remain stable).
Setting `_affineIntensity` to 0 on an object gives it perspective-correct
UVs while keeping the global post-process active.

**Implementation constraint:** Always call `SetPropertyBlock` before reading
back values. Never call `GetPropertyBlock` on a fresh MPB (it will return
default values, not material values). PSXObjectSettings initializes a fresh
MPB in Awake and writes to it immediately.

### Accessibility Integration

`AccessibilitySettings.PSXEnabled` (default true) globally enables or disables
the PSXPostProcessFeature. When false, the post-process blit is skipped; the
object shader properties remain but have reduced visual impact without the
fullscreen pass.

`AccessibilitySettings.ReduceMotion` (default false) suppresses vertex snapping
and affine texture mapping when true. This eliminates the swimming geometry
effect that can cause discomfort for motion-sensitive players. Shadow dithering
and post-process effects remain active under ReduceMotion.

### Runtime Toggle

The F4 key toggles PSXEnabled at runtime for A/B visual comparison during
development and quality review. This toggle is available in development builds
and editor play mode. It is equivalent to toggling the accessibility setting
and does not require a scene reload.

### Design Rule: No Material Swaps

Artist materials are assigned at scene build time and are never changed to
PSXLit by code. If a new asset requires PSX treatment, its material is authored
with PSXLit in the editor. This rule exists to prevent runtime material leaks,
unexpected shader compilation stalls, and uncontrolled visual changes.

## Formulas

**Screen resolution after downscale:**
```
renderWidth  = floor(nativeWidth  / resolutionDivisor)
renderHeight = floor(nativeHeight / resolutionDivisor)
```
- Example at default (3.0) on 1920×1080: 640×360 render resolution
- Fractional divisor (e.g., 2.5): 768×432 render resolution

**Posterization per channel:**
```
quantized = floor(channel × colorDepth) / colorDepth
```
- `channel` ∈ [0, 1]
- `colorDepth` = 32 default → 32 distinct values per channel
- Example: channel=0.73, colorDepth=32 → floor(0.73×32)/32 = floor(23.36)/32
  = 23/32 = 0.71875

**Dither zoom scaling:**
```
ditherZoom = cameraOrthographicSize / ditherZoomReference
scaledDitherIntensity = ditherIntensity × ditherZoom
```
- `ditherZoomReference` = 5.0 (the camera size at which ditherIntensity reads
  as authored)
- At cameraSize=5.0: scaledDitherIntensity = ditherIntensity × 1.0
- At cameraSize=2.5: scaledDitherIntensity = ditherIntensity × 0.5 (finer dither
  at closer zoom, tiles more densely)
- At cameraSize=10.0: scaledDitherIntensity = ditherIntensity × 2.0 (coarser
  at wider zoom)

**Bayer luminance threshold decision:**
```
luminance = dot(color.rgb, float3(0.299, 0.587, 0.114))
if luminance > shadowThreshold:      pattern = none
elif luminance > deepShadowThreshold: pattern = finePattern
else:                                 pattern = coarsePattern
```
- `shadowThreshold` = 0.5 default
- `deepShadowThreshold` = 0.15 default

**Tilt-shift blur falloff:**
```
distanceFromFocal = abs(screenUV.y - tiltShiftCenter) - (tiltShiftWidth / 2)
blurWeight = saturate(distanceFromFocal / tiltShiftRadius) × tiltShiftAmount
```
- `blurWeight` = 0 inside focal band, ramps to `tiltShiftAmount` outside
- Blur kernel samples `tiltShiftRadius` pixels in screen space

## Edge Cases

**resolutionDivisor below 1.0:** Renders at higher than native resolution, which
is wasteful and produces no PSX effect. The system should clamp the minimum to
1.0. Fractional values between 1.0 and 2.0 are valid for subtle pixelation.

**colorDepth = 256:** Maximum fidelity — posterization is imperceptible. Valid
as a "PSX light" mode for accessibility without full disabling.

**colorDepth = 4:** Extreme banding, intentional for artistic emphasis on
specific scenes. No system error, but QA should verify the palette looks
authored rather than broken.

**PSXObjectSettings on a skinned mesh renderer:** The MPB must be set on the
SkinnedMeshRenderer component, not MeshRenderer. The component type must be
checked in Awake when fetching the renderer reference.

**Object with PSXObjectSettings and no renderer:** Awake should log a warning
and disable the component. No null reference propagation.

**ReduceMotion enabled mid-session:** PSXObjectSettings components read
`AccessibilitySettings.ReduceMotion` every frame (or subscribe to
OnSettingsChanged). If the setting changes mid-session, vertex snap and affine
parameters should update within one frame via OnSettingsChanged callback.

**Camera size = 0 (edge case in zoom formula):** `ditherZoom` would be 0,
making dither invisible. This should not occur in normal gameplay but should
be guarded with a max(cameraSize, 0.01) to prevent divide-by-zero if
`ditherZoomReference` were ever 0.

**PSXEnabled toggled while a fade is active:** The ScreenFade panel remains
white (by design — see technical preferences). Toggling PSXEnabled during a
fade transition does not affect the fade panel or its color.

**F4 hotkey in release build:** The F4 runtime toggle must be disabled or
removed in release builds. A #if UNITY_EDITOR or Debug.isDebugBuild guard
is required.

**Materials that use PSXLit but are assigned to UI Canvas objects:** Canvas
objects render outside the standard URP camera pass. PSXPostProcessFeature does
not process UI canvas overlays. This is expected behavior — UI intentionally
bypasses the retro effect.

## Dependencies

**PSX Rendering reads from:**
- `AccessibilitySettings.PSXEnabled` — global on/off
- `AccessibilitySettings.ReduceMotion` — suppresses snap and affine
- `VolumeManager` — URP volume stack for post-process integration
- `IrisQualityManager` — may adjust `resolutionDivisor` or disable feature
  based on quality preset

**PSX Rendering writes to / is applied by:**
- `Shader.Find("Iris/PSXLit")` — object-level effect
- `Shader.Find("Iris/Fullscreen/PSXPost")` — screen-level effect
- `MaterialPropertyBlock` per renderer (via PSXObjectSettings)

**Systems that trigger PSX state changes:**
- `AccessibilitySettings.OnSettingsChanged` — PSXEnabled, ReduceMotion,
  ResolutionScale changes
- `MoodMachine` / `AtmosphereController` — adjust post-process volume
  parameters (bloom, grain, exposure, vignette) that interact with the
  PSX pass ordering
- F4 key input handler (editor/debug only)

**PSX Rendering is depended upon by:**
- `AtmosphereController` — post-process parameter adjustments assume PSX pass
  is active; some multipliers (grain ×2.5) are calibrated for PSX baseline
- All scene visual systems — the PSX pass is the final aesthetic layer over
  all other rendering

## Tuning Knobs

| Knob                    | Current Default | Safe Range          | Affects                                            |
|-------------------------|-----------------|---------------------|----------------------------------------------------|
| resolutionDivisor       | 3.0             | 1.0 – 8.0           | Pixel size; core aesthetic strength                |
| colorDepth              | 32              | 4 – 256             | Color banding intensity                            |
| ditherIntensity         | 0.5             | 0.0 – 1.0           | Shadow texture / grain feel                        |
| shadowThreshold         | 0.5             | 0.3 – 0.8           | Luminance above which dither is suppressed         |
| deepShadowThreshold     | 0.15            | 0.0 – 0.4           | Luminance below which coarse dither applies        |
| ditherZoomReference     | 5.0             | 1.0 – 20.0          | Camera size at which dither reads as designed      |
| finePattern             | Bayer4x4        | Bayer2x2/4x4/8x8    | Detail in mid-shadow dither                        |
| coarsePattern           | Bayer2x2        | Bayer2x2/4x4        | Texture in deep-shadow dither                      |
| vertexSnapResolution    | 160×120         | 80×60 – 320×240     | Geometry swim intensity; lower = more PSX          |
| affineIntensity         | 1.0             | 0.0 – 1.0           | UV warp strength; 0 = perspective-correct          |
| tiltShiftAmount         | 0               | 0.0 – 1.0           | Miniature blur enable/strength                     |
| tiltShiftCenter         | 0.5             | 0.0 – 1.0           | Focal band vertical position                       |
| tiltShiftWidth          | 0.15            | 0.05 – 0.5          | Focal band height (fraction of screen)             |
| tiltShiftRadius         | 8               | 2 – 32              | Blur kernel size in pixels outside focal band      |

## Acceptance Criteria

1. **Resolution downscale:** At resolutionDivisor=3.0 on a 1920×1080 display,
   the effective render resolution is 640×360. Pixel edges are visible as
   squares at native display size.

2. **Posterization:** At colorDepth=4, color output shows no more than 4 distinct
   values per channel. Screenshot comparison confirms visible banding.

3. **Dither in shadows:** In a scene with shadow-receiving geometry, pixels at
   luminance < 0.15 show coarse Bayer2x2 dither pattern. Pixels at luminance
   between 0.15 and 0.5 show Bayer4x4 pattern. Pixels above 0.5 are undithered.

4. **Dither zoom scaling:** At cameraSize=5.0 (reference), dither appears as
   designed. At cameraSize=10.0, dither tiles appear coarser (scaled by 2.0×).
   At cameraSize=2.5, dither tiles appear finer (scaled by 0.5×).

5. **Vertex snapping:** On a moving object with PSXLit material, vertices
   visibly snap to grid positions. At `_snapResolution=0`, the same object
   moves smoothly (no snapping).

6. **Affine UV warping:** On a large polygon with a grid texture and PSXLit
   at `_affineIntensity=1.0`, UV warping is visible at polygon corners. At
   `_affineIntensity=0.0`, the texture is perspective-correct.

7. **Per-object override does not create material instances:** After a scene
   with 10 PSXObjectSettings components runs for 60 seconds, confirm that
   `Resources.FindObjectsOfTypeAll<Material>()` count has not increased from
   baseline (no material instances created at runtime).

8. **PSXEnabled=false disables post-process:** Toggle AccessibilitySettings
   .PSXEnabled to false. The fullscreen blit is skipped. Scene renders without
   downscale, posterization, or dithering. Object shader properties remain but
   their visual contribution is minimal without the fullscreen pass.

9. **ReduceMotion suppresses vertex snap and affine:** With ReduceMotion=true,
   a PSXLit object moves smoothly (no vertex snap) and UV coordinates are
   perspective-correct. Shadow dithering remains visible.

10. **F4 toggle:** In editor play mode, pressing F4 toggles the post-process
    effect off and back on within one frame. The scene visual difference is
    immediately apparent.

11. **ReduceMotion live update:** Enable ReduceMotion while in play mode. The
    change takes effect within one rendered frame, without requiring restart.

12. **Tilt-shift at amount=1.0:** With `tiltShiftAmount=1.0`, pixels outside
    the focal band (defined by center and width) are blurred. Pixels inside
    the focal band are unblurred. The blur radius corresponds to `tiltShiftRadius`
    pixels in screen space.

using UnityEngine;

/// <summary>
/// Drives rendering parameters from NemaWellbeing.Overall.
/// Low wellbeing = hazy, dissociative (double exposure, heavy bloom, harsh dither, vignette).
/// High wellbeing = sharp, clear, present.
/// All values are capped by AccessibilitySettings and composed with MoodMachine.
/// </summary>
public class WellbeingRenderDriver : MonoBehaviour
{
    public static WellbeingRenderDriver Instance { get; private set; }

    [Header("Renderer Features")]
    [Tooltip("Reference to the DoubleExposureFeature on the URP renderer asset.")]
    [SerializeField] private DoubleExposureFeature _doubleExposure;

    [Tooltip("Reference to the PSXPostProcessFeature on the URP renderer asset.")]
    [SerializeField] private PSXPostProcessFeature _psxPostProcess;

    [Header("Transition")]
    [Tooltip("How fast visual parameters shift toward their target (units/sec). Lower = more gradual.")]
    [SerializeField] private float _lerpSpeed = 0.3f;

    // ── Wellbeing-driven target ranges ──────────────────────────
    // Each pair is (lowWellbeing, highWellbeing). Lerped by Overall.

    // DoubleExposureFeature
    private const float DE_BlendLow = 0.5f,   DE_BlendHigh = 0f;
    private const float DE_BlurLow = 5f,      DE_BlurHigh = 0f;
    private const float DE_DecayLow = 0.02f,  DE_DecayHigh = 0.3f;
    private const float DE_BloomLow = 0.2f,   DE_BloomHigh = 0.8f;

    // AtmosphereController
    private const float AC_BloomIntLow = 1.2f,    AC_BloomIntHigh = 0.4f;
    private const float AC_BloomThreshLow = 0.3f,  AC_BloomThreshHigh = 0.9f;
    private const float AC_BloomScatLow = 0.9f,    AC_BloomScatHigh = 0.5f;
    private const float AC_ExposureLow = -0.1f,    AC_ExposureHigh = 0.4f;
    private const float AC_VignetteLow = 0.5f,     AC_VignetteHigh = 0.2f;

    // PSXPostProcessFeature
    private const float PSX_DitherLow = 0.9f, PSX_DitherHigh = 0.3f;

    // Neutral defaults (used when DynamicVisualsEnabled = false)
    private const float Neutral_Overall = 0.6f;

    // Current smoothed values
    private float _curDEBlend, _curDEBlur, _curDEDecay, _curDEBloom;
    private float _curBloomInt, _curBloomThresh, _curBloomScat;
    private float _curExposure, _curVignette;
    private float _curDither;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Initialize smoothed values to neutral
        float n = Neutral_Overall;
        _curDEBlend = Mathf.Lerp(DE_BlendLow, DE_BlendHigh, n);
        _curDEBlur = Mathf.Lerp(DE_BlurLow, DE_BlurHigh, n);
        _curDEDecay = Mathf.Lerp(DE_DecayLow, DE_DecayHigh, n);
        _curDEBloom = Mathf.Lerp(DE_BloomLow, DE_BloomHigh, n);
        _curBloomInt = Mathf.Lerp(AC_BloomIntLow, AC_BloomIntHigh, n);
        _curBloomThresh = Mathf.Lerp(AC_BloomThreshLow, AC_BloomThreshHigh, n);
        _curBloomScat = Mathf.Lerp(AC_BloomScatLow, AC_BloomScatHigh, n);
        _curExposure = Mathf.Lerp(AC_ExposureLow, AC_ExposureHigh, n);
        _curVignette = Mathf.Lerp(AC_VignetteLow, AC_VignetteHigh, n);
        _curDither = Mathf.Lerp(PSX_DitherLow, PSX_DitherHigh, n);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void LateUpdate()
    {
        float overall = NemaWellbeing.Instance != null ? NemaWellbeing.Instance.Overall : Neutral_Overall;

        // If dynamic visuals are disabled, lock to neutral
        if (!AccessibilitySettings.DynamicVisualsEnabled)
            overall = Neutral_Overall;

        float dt = Time.unscaledDeltaTime;
        float speed = _lerpSpeed * dt;

        // Calculate targets from wellbeing
        float tDEBlend = Mathf.Lerp(DE_BlendLow, DE_BlendHigh, overall);
        float tDEBlur = Mathf.Lerp(DE_BlurLow, DE_BlurHigh, overall);
        float tDEDecay = Mathf.Lerp(DE_DecayLow, DE_DecayHigh, overall);
        float tDEBloom = Mathf.Lerp(DE_BloomLow, DE_BloomHigh, overall);
        float tBloomInt = Mathf.Lerp(AC_BloomIntLow, AC_BloomIntHigh, overall);
        float tBloomThresh = Mathf.Lerp(AC_BloomThreshLow, AC_BloomThreshHigh, overall);
        float tBloomScat = Mathf.Lerp(AC_BloomScatLow, AC_BloomScatHigh, overall);
        float tExposure = Mathf.Lerp(AC_ExposureLow, AC_ExposureHigh, overall);
        float tVignette = Mathf.Lerp(AC_VignetteLow, AC_VignetteHigh, overall);
        float tDither = Mathf.Lerp(PSX_DitherLow, PSX_DitherHigh, overall);

        // Compose with mood (mood 0=sunny, 1=stormy modulates within wellbeing range)
        float mood = 0f;
        if (AccessibilitySettings.DynamicVisualsEnabled && MoodMachine.Instance != null)
            mood = MoodMachine.Instance.Mood;

        // Mood pushes bloom down and grain/exposure darker (same direction as low wellbeing)
        tBloomInt = Mathf.Lerp(tBloomInt, tBloomInt * 0.25f, mood);
        tBloomScat = Mathf.Lerp(tBloomScat, tBloomScat * 0.5f, mood);
        tExposure = Mathf.Lerp(tExposure, tExposure - 0.4f, mood);

        // Smooth toward targets
        _curDEBlend = Mathf.MoveTowards(_curDEBlend, tDEBlend, speed);
        _curDEBlur = Mathf.MoveTowards(_curDEBlur, tDEBlur, speed * 5f);
        _curDEDecay = Mathf.MoveTowards(_curDEDecay, tDEDecay, speed);
        _curDEBloom = Mathf.MoveTowards(_curDEBloom, tDEBloom, speed);
        _curBloomInt = Mathf.MoveTowards(_curBloomInt, tBloomInt, speed);
        _curBloomThresh = Mathf.MoveTowards(_curBloomThresh, tBloomThresh, speed);
        _curBloomScat = Mathf.MoveTowards(_curBloomScat, tBloomScat, speed);
        _curExposure = Mathf.MoveTowards(_curExposure, tExposure, speed);
        _curVignette = Mathf.MoveTowards(_curVignette, tVignette, speed);
        _curDither = Mathf.MoveTowards(_curDither, tDither, speed);

        // ── Apply accessibility caps ────────────────────────────

        // ReduceMotion kills double exposure entirely
        float deMax = AccessibilitySettings.ReduceMotion ? 0f : AccessibilitySettings.DoubleExposureMax;
        float bloomMax = AccessibilitySettings.BloomMax;
        float ditherMax = AccessibilitySettings.DitherMax;
        float vigMax = AccessibilitySettings.VignetteMax;

        // HighContrast reduces vignette cap
        if (AccessibilitySettings.HighContrast)
            vigMax = Mathf.Min(vigMax, 0.3f);

        float finalDEBlend = Mathf.Min(_curDEBlend, deMax);
        float finalDEBlur = deMax > 0f ? _curDEBlur : 0f;
        float finalDEDecay = deMax > 0f ? _curDEDecay : 0.3f;
        float finalDEBloom = _curDEBloom;
        float finalBloomInt = _curBloomInt * bloomMax;
        float finalBloomThresh = _curBloomThresh;
        float finalBloomScat = _curBloomScat;
        float finalExposure = _curExposure;
        float finalVignette = Mathf.Min(_curVignette, vigMax);
        float finalDither = Mathf.Min(_curDither, ditherMax);

        // ── Apply to DoubleExposureFeature ──────────────────────
        if (_doubleExposure != null)
        {
            _doubleExposure.settings.blendIntensity = finalDEBlend;
            _doubleExposure.settings.blurRadius = finalDEBlur;
            _doubleExposure.settings.feedbackDecay = finalDEDecay;
            _doubleExposure.settings.bloomThreshold = finalDEBloom;
        }

        // ── Apply to AtmosphereController ───────────────────────
        var atmo = AtmosphereController.Instance;
        if (atmo != null)
        {
            // Suppress AtmosphereController's own mood overrides since we handle mood here
            atmo.SuppressMoodOverrides = true;

            atmo.BloomIntensity = finalBloomInt;
            atmo.BloomThreshold = finalBloomThresh;
            atmo.BloomScatter = finalBloomScat;
            atmo.PostExposure = finalExposure;
            atmo.VignetteIntensity = finalVignette;
        }

        // ── Apply to PSXPostProcessFeature ──────────────────────
        if (_psxPostProcess != null)
        {
            _psxPostProcess.settings.ditherIntensity = finalDither;
        }
    }
}

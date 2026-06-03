using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Ensures essential systems are initialized when playing the apartment scene
/// directly from the editor without going through the main menu.
/// Also polls for settings changes during play so Inspector tweaks are visible live.
/// Stripped from builds.
/// </summary>
public class EditorSceneBootstrap : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("Fallback Config")]
    [Tooltip("GameModeConfig to use when ActiveConfig is null (skipped main menu).")]
    [SerializeField] private GameModeConfig _fallbackConfig;

    [Tooltip("Default player name when skipping main menu.")]
    [SerializeField] private string _fallbackPlayerName = "Nema";

    [Header("Live Debug Overrides")]
    [Tooltip("Override bloom intensity at runtime. -1 = don't override.")]
    [Range(-1f, 3f)]
    [SerializeField] private float _bloomIntensity = -1f;

    [Tooltip("Override bloom threshold at runtime. -1 = don't override.")]
    [Range(-1f, 2f)]
    [SerializeField] private float _bloomThreshold = -1f;

    [Tooltip("Override post exposure at runtime. -99 = don't override.")]
    [Range(-99f, 2f)]
    [SerializeField] private float _postExposure = -99f;

    [Tooltip("Override vignette intensity at runtime. -1 = don't override.")]
    [Range(-1f, 1f)]
    [SerializeField] private float _vignetteIntensity = -1f;

    [Tooltip("Override dither intensity at runtime. -1 = don't override.")]
    [Range(-1f, 1f)]
    [SerializeField] private float _ditherIntensity = -1f;

    [Tooltip("Override double exposure blend at runtime. -1 = don't override.")]
    [Range(-1f, 1f)]
    [SerializeField] private float _doubleExposureBlend = -1f;

    private void Awake()
    {
        if (MainMenuManager.ActiveConfig != null) return;

        Debug.Log("[EditorSceneBootstrap] No ActiveConfig — applying editor defaults.");

        if (_fallbackConfig != null)
        {
            var prop = typeof(MainMenuManager).GetProperty("ActiveConfig",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            prop?.SetValue(null, _fallbackConfig);
        }

        if (string.IsNullOrEmpty(PlayerData.PlayerName))
            PlayerData.PlayerName = _fallbackPlayerName;

        TimeScaleManager.ClearAll();

        var shaders = ShaderCollection.Instance;
        if (shaders != null)
            Debug.Log($"[EditorSceneBootstrap] ShaderCollection loaded: {shaders.shaders.Length} shaders.");
    }

    private void LateUpdate()
    {
        // Poll live overrides so Inspector changes are visible immediately
        var atmo = AtmosphereController.Instance;
        if (atmo != null)
        {
            if (_bloomIntensity >= 0f) atmo.BloomIntensity = _bloomIntensity;
            if (_bloomThreshold >= 0f) atmo.BloomThreshold = _bloomThreshold;
            if (_postExposure > -90f) atmo.PostExposure = _postExposure;
            if (_vignetteIntensity >= 0f) atmo.VignetteIntensity = _vignetteIntensity;
        }

        if (_ditherIntensity >= 0f)
        {
            // Find PSXPostProcessFeature via renderer — settings are public
            var urpAsset = UnityEngine.Rendering.Universal.UniversalRenderPipeline.asset;
            if (urpAsset != null)
            {
                // PSXPostProcessFeature.settings.ditherIntensity can't be accessed
                // without a reference. Use PSXRenderController if available.
                var psx = PSXRenderController.Instance;
                if (psx != null)
                {
                    // PSXRenderController exposes dither via its own inspector,
                    // but we can set it directly on the feature if we have a ref.
                    // For now, this is a placeholder — the dither override requires
                    // a serialized reference to the renderer feature.
                }
            }
        }

        // Double exposure override would need a serialized DoubleExposureFeature reference.
        // These are renderer-asset-level features, not easily polled from a scene component.
        // WellbeingRenderDriver handles these when wired.
    }
#endif
}

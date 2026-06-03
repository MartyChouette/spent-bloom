using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Ensures essential systems are initialized when playing the apartment scene
/// directly from the editor without going through the main menu.
/// Stripped from builds. Runs before any scene-scoped singletons need the data.
/// </summary>
public class EditorSceneBootstrap : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("Fallback Config")]
    [Tooltip("GameModeConfig to use when ActiveConfig is null (skipped main menu).")]
    [SerializeField] private GameModeConfig _fallbackConfig;

    [Tooltip("Default player name when skipping main menu.")]
    [SerializeField] private string _fallbackPlayerName = "Nema";

    private void Awake()
    {
        // Only run if we skipped the main menu (ActiveConfig is null)
        if (MainMenuManager.ActiveConfig != null) return;

        Debug.Log("[EditorSceneBootstrap] No ActiveConfig — applying editor defaults.");

        // Set a game mode config so systems that check it don't break
        if (_fallbackConfig != null)
        {
            var prop = typeof(MainMenuManager).GetProperty("ActiveConfig",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            prop?.SetValue(null, _fallbackConfig);
        }

        // Set player name
        if (string.IsNullOrEmpty(PlayerData.PlayerName))
            PlayerData.PlayerName = _fallbackPlayerName;

        // Ensure time scale is normal (not stuck from previous play session)
        TimeScaleManager.ClearAll();

        // Force shader collection load
        var shaders = ShaderCollection.Instance;
        if (shaders != null)
            Debug.Log($"[EditorSceneBootstrap] ShaderCollection loaded: {shaders.shaders.Length} shaders.");

        // PSXRenderController and AtmosphereController self-initialize in Start(),
        // no need to force them here.
    }
#endif
}

using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Creates an overlay camera at runtime that renders only the Character layer
/// ("Face", layer 17) WITHOUT PSX post-processing. This separates Nema from
/// the environment so she gets clean, high-fidelity lighting while the world
/// stays crunchy.
///
/// Auto-spawns via RuntimeInitializeOnLoadMethod. No scene setup required.
/// Follows the same pattern as HeldItemOverlayCamera.
/// </summary>
public class CharacterOverlayCamera : MonoBehaviour
{
    private static CharacterOverlayCamera s_instance;
    private Camera _overlayCam;

    /// <summary>
    /// The layer index used for character rendering. Other scripts (NemaController)
    /// read this to move Nema onto the correct layer. -1 if the layer wasn't found.
    /// </summary>
    public static int CharacterLayer { get; private set; } = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoSpawn()
    {
        if (s_instance != null) return;

        CharacterLayer = LayerMask.NameToLayer("Face");
        if (CharacterLayer < 0)
        {
            Debug.LogWarning("[CharacterOverlayCamera] 'Face' layer not found. Character overlay disabled.");
            return;
        }

        var go = new GameObject("[CharacterOverlayCamera]");
        DontDestroyOnLoad(go);
        s_instance = go.AddComponent<CharacterOverlayCamera>();
    }

    private void Awake()
    {
        if (s_instance != null && s_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        s_instance = this;
    }

    private void OnDestroy()
    {
        if (s_instance == this) s_instance = null;
    }

    private void LateUpdate()
    {
        var mainCam = Camera.main;
        if (mainCam == null)
        {
            if (_overlayCam != null) _overlayCam.enabled = false;
            return;
        }

        EnsureOverlayCamera(mainCam);
        SyncToMainCamera(mainCam);
    }

    private void EnsureOverlayCamera(Camera mainCam)
    {
        if (_overlayCam != null) return;

        var camGO = new GameObject("CharacterOverlay");
        camGO.transform.SetParent(transform, false);
        _overlayCam = camGO.AddComponent<Camera>();

        // Clear depth so character composites on top of the PSX-processed base.
        // Depth occlusion against environment is lost (prototype trade-off).
        _overlayCam.clearFlags = CameraClearFlags.Depth;
        _overlayCam.cullingMask = 1 << CharacterLayer;
        _overlayCam.depth = mainCam.depth + 1;

        // URP overlay — no post-processing on this camera
        var overlayData = _overlayCam.GetUniversalAdditionalCameraData();
        overlayData.renderType = CameraRenderType.Overlay;
        overlayData.renderPostProcessing = false;
        overlayData.requiresColorOption = CameraOverrideOption.Off;
        overlayData.requiresDepthOption = CameraOverrideOption.Off;

        // Exclude character layer from main camera so she doesn't double-render
        mainCam.cullingMask &= ~(1 << CharacterLayer);

        // Add to main camera's stack — insert at 0 so character renders
        // before held items (held items should stay on top of everything)
        var mainData = mainCam.GetUniversalAdditionalCameraData();
        mainData.renderType = CameraRenderType.Base;
        if (!mainData.cameraStack.Contains(_overlayCam))
            mainData.cameraStack.Insert(0, _overlayCam);

        _overlayCam.enabled = true;

        Debug.Log("[CharacterOverlayCamera] Overlay camera created and stacked.");
    }

    private void SyncToMainCamera(Camera mainCam)
    {
        if (_overlayCam == null) return;

        _overlayCam.orthographic = mainCam.orthographic;
        _overlayCam.orthographicSize = mainCam.orthographicSize;
        _overlayCam.fieldOfView = mainCam.fieldOfView;
        _overlayCam.nearClipPlane = mainCam.nearClipPlane;
        _overlayCam.farClipPlane = mainCam.farClipPlane;

        _overlayCam.transform.position = mainCam.transform.position;
        _overlayCam.transform.rotation = mainCam.transform.rotation;
    }
}

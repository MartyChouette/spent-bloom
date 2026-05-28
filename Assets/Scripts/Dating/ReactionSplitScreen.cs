using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Diagonal split-screen effect for Phase 3 item reactions.
/// When triggered, a second camera frames the date character's face and
/// slides in from the top-right corner with a diagonal wipe.
/// </summary>
public class ReactionSplitScreen : MonoBehaviour
{
    public static ReactionSplitScreen Instance { get; private set; }

    [Header("Camera")]
    [Tooltip("Distance from the character's head for the reaction close-up.")]
    [SerializeField] private float _cameraDistance = 1.2f;

    [Tooltip("Height offset above the head bone.")]
    [SerializeField] private float _cameraHeightOffset = 0.1f;

    [Tooltip("Horizontal offset (positive = camera right of character).")]
    [SerializeField] private float _cameraSideOffset = 0.3f;

    [Tooltip("Field of view for the reaction camera.")]
    [SerializeField] private float _cameraFOV = 35f;

    [Header("Animation")]
    [Tooltip("Seconds for the split to slide in.")]
    [SerializeField] private float _slideInDuration = 0.35f;

    [Tooltip("Seconds the reaction holds before sliding out.")]
    [SerializeField] private float _holdDuration = 1.8f;

    [Tooltip("Seconds for the split to slide out.")]
    [SerializeField] private float _slideOutDuration = 0.25f;

    [Header("Visuals")]
    [Tooltip("Edge line color.")]
    [SerializeField] private Color _edgeColor = new Color(1f, 1f, 1f, 0.8f);

    [Tooltip("Edge line width.")]
    [SerializeField] private float _edgeWidth = 0.005f;

    [Tooltip("Edge softness.")]
    [SerializeField] private float _edgeSoftness = 0.008f;

    [Header("Shader")]
    [Tooltip("Drag Iris/DiagonalSplit shader here for build inclusion.")]
    [SerializeField] private Shader _diagonalShader;

    // Runtime objects
    private Camera _reactionCam;
    private RenderTexture _rt;
    private Canvas _canvas;
    private RawImage _splitImage;
    private Material _splitMat;
    private Coroutine _activeSequence;

    private static readonly int SplitProgressID = Shader.PropertyToID("_SplitProgress");
    private static readonly int EdgeColorID = Shader.PropertyToID("_EdgeColor");
    private static readonly int EdgeWidthID = Shader.PropertyToID("_EdgeWidth");
    private static readonly int EdgeSoftnessID = Shader.PropertyToID("_EdgeSoftness");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Instance = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoSpawn()
    {
        if (Instance != null) return;
        var go = new GameObject("[ReactionSplitScreen]");
        go.AddComponent<ReactionSplitScreen>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildRenderTexture();
        BuildCamera();
        BuildUI();

        ForceOff();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (_splitMat != null) Destroy(_splitMat);
        if (_rt != null) { _rt.Release(); Destroy(_rt); }
        if (_reactionCam != null) Destroy(_reactionCam.gameObject);
        if (_canvas != null) Destroy(_canvas.gameObject);
    }

    /// <summary>Kill any active split and ensure camera + UI are off.</summary>
    public void ForceOff()
    {
        if (_activeSequence != null)
        {
            StopCoroutine(_activeSequence);
            _activeSequence = null;
        }
        if (_reactionCam != null) _reactionCam.enabled = false;
        if (_splitImage != null) _splitImage.gameObject.SetActive(false);
        if (_splitMat != null) _splitMat.SetFloat(SplitProgressID, 0f);
    }

    /// <summary>Clean up on any scene load so nothing leaks into the menu.</summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ForceOff();
    }

    /// <summary>
    /// Show the diagonal split with a close-up of the date character reacting.
    /// Call from DateInspectSystem after the reaction fires.
    /// </summary>
    public void Show(Transform characterRoot)
    {
        if (characterRoot == null) return;
        // Don't interrupt an active split — let it finish
        if (_activeSequence != null) return;
        _activeSequence = StartCoroutine(SplitSequence(characterRoot));
    }

    /// <summary>True while the split is animating or holding.</summary>
    public bool IsActive => _activeSequence != null;

    private IEnumerator SplitSequence(Transform characterRoot)
    {
        // Find head bone for framing
        var animator = characterRoot.GetComponentInChildren<Animator>();
        Transform headBone = null;
        if (animator != null)
            headBone = animator.GetBoneTransform(HumanBodyBones.Head);

        Vector3 lookAt = headBone != null
            ? headBone.position
            : characterRoot.position + Vector3.up * 1.5f;

        // Position reaction camera
        FrameCharacter(lookAt, characterRoot);
        _reactionCam.enabled = true;
        _splitImage.gameObject.SetActive(true);

        // Slide in (unscaled so it works during pause)
        yield return AnimateSplit(0f, 1f, _slideInDuration);

        // Hold (keep updating camera to follow head during animation)
        float holdElapsed = 0f;
        while (holdElapsed < _holdDuration)
        {
            holdElapsed += Time.unscaledDeltaTime;

            // Bail if the character was destroyed (date ended, scene changed)
            if (characterRoot == null) { ForceOff(); yield break; }

            if (headBone != null)
            {
                lookAt = headBone.position;
                FrameCharacter(lookAt, characterRoot);
            }
            yield return null;
        }

        // Slide out
        yield return AnimateSplit(1f, 0f, _slideOutDuration);

        _reactionCam.enabled = false;
        _splitImage.gameObject.SetActive(false);
        _activeSequence = null;
    }

    private void FrameCharacter(Vector3 lookAt, Transform characterRoot)
    {
        Vector3 charForward = characterRoot.forward;
        Vector3 charRight = characterRoot.right;

        Vector3 camPos = lookAt
            + charForward * _cameraDistance
            + Vector3.up * _cameraHeightOffset
            + charRight * _cameraSideOffset;

        _reactionCam.transform.position = camPos;
        _reactionCam.transform.LookAt(lookAt);
    }

    private IEnumerator AnimateSplit(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - (1f - t) * (1f - t) * (1f - t);
            float progress = Mathf.Lerp(from, to, eased);
            _splitMat.SetFloat(SplitProgressID, progress);
            yield return null;
        }
        _splitMat.SetFloat(SplitProgressID, to);
    }

    // ── Build helpers ─────────────────────────────────────────────

    private void BuildRenderTexture()
    {
        int w = Mathf.Max(Screen.width / 2, 480);
        int h = Mathf.Max(Screen.height / 2, 270);
        _rt = new RenderTexture(w, h, 16, RenderTextureFormat.Default);
        _rt.name = "ReactionSplitRT";
        _rt.Create();
    }

    private void BuildCamera()
    {
        var camGO = new GameObject("ReactionSplitCamera");
        camGO.transform.SetParent(transform);
        _reactionCam = camGO.AddComponent<Camera>();
        _reactionCam.targetTexture = _rt;
        _reactionCam.fieldOfView = _cameraFOV;
        _reactionCam.nearClipPlane = 0.1f;
        _reactionCam.farClipPlane = 50f;
        _reactionCam.clearFlags = CameraClearFlags.SolidColor;
        _reactionCam.backgroundColor = Color.black;
        _reactionCam.depth = 10;

        var mainCam = Camera.main;
        if (mainCam != null)
        {
            var mainData = mainCam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (mainData != null)
            {
                var camData = camGO.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                camData.SetRenderer(0);
            }
        }
    }

    private void BuildUI()
    {
        var canvasGO = new GameObject("ReactionSplitCanvas");
        canvasGO.transform.SetParent(transform);
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 90;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        var imgGO = new GameObject("SplitImage");
        imgGO.transform.SetParent(canvasGO.transform, false);
        var rt = imgGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        _splitImage = imgGO.AddComponent<RawImage>();
        _splitImage.texture = _rt;
        _splitImage.raycastTarget = false;

        if (_diagonalShader == null)
            _diagonalShader = Shader.Find("Iris/DiagonalSplit");

        if (_diagonalShader != null)
        {
            _splitMat = new Material(_diagonalShader);
            _splitMat.SetFloat(SplitProgressID, 0f);
            _splitMat.SetColor(EdgeColorID, _edgeColor);
            _splitMat.SetFloat(EdgeWidthID, _edgeWidth);
            _splitMat.SetFloat(EdgeSoftnessID, _edgeSoftness);
            _splitImage.material = _splitMat;
        }
    }
}

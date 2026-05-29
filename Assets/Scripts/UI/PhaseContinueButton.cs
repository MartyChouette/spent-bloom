using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Floating prompt shown between date phases. Replaces the old green button
/// with subtle lowercase italic text that breathes and drifts.
/// Auto-spawns as a global singleton. Call Show(callback) to display,
/// Hide() to dismiss. Clicks anywhere on the invisible overlay trigger the callback.
/// </summary>
public class PhaseContinueButton : MonoBehaviour
{
    public static PhaseContinueButton Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoSpawn()
    {
        if (Instance != null) return;
        var go = new GameObject("PhaseContinueButton");
        DontDestroyOnLoad(go);
        go.AddComponent<PhaseContinueButton>();
    }

    [Tooltip("Delay before the text fades in after Show() is called.")]
    [SerializeField] private float _showDelay = 0.5f;

    [Tooltip("Fade-in duration.")]
    [SerializeField] private float _fadeDuration = 0.5f;

    [Header("Breathing")]
    [Tooltip("Minimum alpha during breathing pulse.")]
    [SerializeField] private float _breathMin = 0.4f;

    [Tooltip("Maximum alpha during breathing pulse.")]
    [SerializeField] private float _breathMax = 0.8f;

    [Tooltip("Breathing cycle speed.")]
    [SerializeField] private float _breathSpeed = 1.5f;

    [Header("Drift")]
    [Tooltip("Vertical drift amplitude in pixels.")]
    [SerializeField] private float _driftAmount = 3f;

    [Tooltip("Drift cycle speed.")]
    [SerializeField] private float _driftSpeed = 0.8f;

    private Canvas _canvas;
    private CanvasGroup _canvasGroup;
    private Button _button;
    private TextMeshProUGUI _labelTMP;
    private RectTransform _labelRT;
    private System.Action _onClick;
    private Coroutine _fadeCoroutine;
    private bool _visible;
    private float _baseY;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        BuildUI();
        _canvas.gameObject.SetActive(false);
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m)
    {
        Hide();
    }

    private void Update()
    {
        if (!_visible || _labelRT == null) return;

        float t = Time.unscaledTime;

        // Breathing alpha pulse
        float breath = Mathf.Lerp(_breathMin, _breathMax, (Mathf.Sin(t * _breathSpeed * Mathf.PI * 2f) + 1f) * 0.5f);
        _canvasGroup.alpha = breath;

        // Gentle vertical drift
        float drift = Mathf.Sin(t * _driftSpeed * Mathf.PI * 2f) * _driftAmount;
        _labelRT.anchoredPosition = new Vector2(_labelRT.anchoredPosition.x, _baseY + drift);
    }

    // ── Public API ──────────────────────────────────────────────────

    /// <summary>Show the prompt with custom label text.</summary>
    public void Show(System.Action onClicked, string label)
    {
        Show(onClicked);
        if (_labelTMP != null) _labelTMP.text = label.ToLowerInvariant();
    }

    /// <summary>Show the prompt with default "continue" text.</summary>
    public void Show(System.Action onClicked)
    {
        if (_labelTMP != null) _labelTMP.text = "continue";
        _onClick = onClicked;
        _canvas.gameObject.SetActive(true);
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;

        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);

        if (AccessibilitySettings.ReduceMotion)
        {
            _canvasGroup.alpha = _breathMax;
            _visible = true;
        }
        else
        {
            _canvasGroup.alpha = 0f;
            _visible = false;
            _fadeCoroutine = StartCoroutine(FadeIn());
        }
    }

    /// <summary>Hide the prompt immediately.</summary>
    public void Hide()
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }
        _onClick = null;
        _visible = false;
        if (_canvas != null)
            _canvas.gameObject.SetActive(false);
    }

    // ── Build UI in code ────────────────────────────────────────────

    private void BuildUI()
    {
        // Canvas
        var canvasGO = new GameObject("PhaseContinueCanvas");
        canvasGO.transform.SetParent(transform, false);

        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 201;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        _canvasGroup = canvasGO.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;

        // Click target — sized around the text, not fullscreen, so it doesn't block gameplay
        var clickGO = new GameObject("ClickTarget");
        clickGO.transform.SetParent(canvasGO.transform, false);

        var clickRT = clickGO.AddComponent<RectTransform>();
        clickRT.anchorMin = new Vector2(1f, 0f);
        clickRT.anchorMax = new Vector2(1f, 0f);
        clickRT.pivot = new Vector2(1f, 0f);
        clickRT.anchoredPosition = new Vector2(-40f, 60f);
        clickRT.sizeDelta = new Vector2(350f, 80f);

        var clickImg = clickGO.AddComponent<Image>();
        clickImg.color = new Color(0f, 0f, 0f, 0f); // invisible

        _button = clickGO.AddComponent<Button>();
        _button.targetGraphic = clickImg;
        var nav = _button.navigation;
        nav.mode = Navigation.Mode.None;
        _button.navigation = nav;
        _button.onClick.AddListener(OnButtonClicked);

        // Floating text label — bottom-right, bigger
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(clickGO.transform, false);

        _labelRT = labelGO.AddComponent<RectTransform>();
        _labelRT.anchorMin = Vector2.zero;
        _labelRT.anchorMax = Vector2.one;
        _labelRT.offsetMin = Vector2.zero;
        _labelRT.offsetMax = Vector2.zero;
        _baseY = 0f; // drift is relative to the click target's position

        _labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
        _labelTMP.text = "continue";
        _labelTMP.fontSize = 36f;
        _labelTMP.fontStyle = FontStyles.Italic;
        _labelTMP.color = new Color(0.85f, 0.82f, 0.78f);
        _labelTMP.alignment = TextAlignmentOptions.Center;
        _labelTMP.raycastTarget = false;

        var theme = IrisTextTheme.Active;
        if (theme != null && theme.primaryFont != null)
            _labelTMP.font = theme.primaryFont;
    }

    // ── Internals ───────────────────────────────────────────────────

    private void OnButtonClicked()
    {
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        var cb = _onClick;
        _onClick = null;
        Hide();
        cb?.Invoke();
    }

    private System.Collections.IEnumerator FadeIn()
    {
        if (_showDelay > 0f)
            yield return new WaitForSecondsRealtime(_showDelay);

        float elapsed = 0f;
        while (elapsed < _fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / _fadeDuration);
            _canvasGroup.alpha = Mathf.Lerp(0f, _breathMin, t);
            yield return null;
        }

        _visible = true;
        _fadeCoroutine = null;
    }
}

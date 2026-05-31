using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Settings panel controller. Manages tabbed sections and binds UI controls
/// to AccessibilitySettings. Opened from pause menu or main menu.
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    [Header("Root")]
    [Tooltip("The panel root to show/hide.")]
    [SerializeField] private GameObject _panelRoot;

    [Header("Tabs")]
    [SerializeField] private SettingsTabButton[] _tabButtons;
    [SerializeField] private GameObject[] _tabPanels;

    // ── Visual Tab ──────────────────────────────────────────────
    [Header("Visual")]
    [SerializeField] private TMP_Dropdown _colorblindDropdown;
    [SerializeField] private Toggle _highContrastToggle;
    [SerializeField] private Slider _textScaleSlider;
    [SerializeField] private TMP_Text _textScaleLabel;

    [Header("Visual Effects")]
    [SerializeField] private Slider _doubleExposureMaxSlider;
    [SerializeField] private TMP_Text _doubleExposureMaxLabel;
    [SerializeField] private Slider _bloomMaxSlider;
    [SerializeField] private TMP_Text _bloomMaxLabel;
    [SerializeField] private Slider _ditherMaxSlider;
    [SerializeField] private TMP_Text _ditherMaxLabel;
    [SerializeField] private Slider _vignetteMaxSlider;
    [SerializeField] private TMP_Text _vignetteMaxLabel;
    [SerializeField] private Toggle _dynamicVisualsToggle;

    // ── Audio Tab ───────────────────────────────────────────────
    [Header("Audio")]
    [SerializeField] private Slider _masterVolumeSlider;
    [SerializeField] private Slider _musicVolumeSlider;
    [SerializeField] private Slider _sfxVolumeSlider;
    [SerializeField] private Slider _ambienceVolumeSlider;
    [SerializeField] private Slider _uiVolumeSlider;
    [SerializeField] private Toggle _captionsToggle;

    // ── Motion Tab ──────────────────────────────────────────────
    [Header("Motion")]
    [SerializeField] private Toggle _reduceMotionToggle;
    [SerializeField] private Slider _screenShakeSlider;
    [SerializeField] private TMP_Text _screenShakeLabel;

    // ── Timing Tab ──────────────────────────────────────────────
    [Header("Timing")]
    [SerializeField] private TMP_Dropdown _timerDropdown;

    // ── Controls Tab ──────────────────────────────────────────
    [Header("Controls")]
    [SerializeField] private Toggle _invertScrollToggle;

    // ── Performance Tab ─────────────────────────────────────────
    [Header("Performance")]
    [SerializeField] private Slider _resolutionScaleSlider;
    [SerializeField] private TMP_Text _resolutionScaleLabel;
    [SerializeField] private TMP_Dropdown _qualityDropdown;
    [SerializeField] private Toggle _psxToggle;

    private int _currentTab;
    private bool _isOpen;
    private bool _suppressCallbacks;

    public bool IsOpen => _isOpen;

    private void Start()
    {
        // Auto-build visual effect controls if not wired in Inspector
        BuildVisualEffectControls();

        // Wire up listeners
        if (_colorblindDropdown != null) _colorblindDropdown.onValueChanged.AddListener(OnColorblindChanged);
        if (_highContrastToggle != null) _highContrastToggle.onValueChanged.AddListener(OnHighContrastChanged);
        if (_textScaleSlider != null)    _textScaleSlider.onValueChanged.AddListener(OnTextScaleChanged);
        if (_masterVolumeSlider != null) _masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        if (_musicVolumeSlider != null)  _musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        if (_sfxVolumeSlider != null)    _sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        if (_ambienceVolumeSlider != null) _ambienceVolumeSlider.onValueChanged.AddListener(OnAmbienceVolumeChanged);
        if (_uiVolumeSlider != null)     _uiVolumeSlider.onValueChanged.AddListener(OnUIVolumeChanged);
        if (_captionsToggle != null)     _captionsToggle.onValueChanged.AddListener(OnCaptionsChanged);
        if (_reduceMotionToggle != null) _reduceMotionToggle.onValueChanged.AddListener(OnReduceMotionChanged);
        if (_screenShakeSlider != null)  _screenShakeSlider.onValueChanged.AddListener(OnScreenShakeChanged);
        if (_timerDropdown != null)      _timerDropdown.onValueChanged.AddListener(OnTimerChanged);
        if (_resolutionScaleSlider != null) _resolutionScaleSlider.onValueChanged.AddListener(OnResolutionScaleChanged);
        if (_qualityDropdown != null)    _qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
        if (_psxToggle != null)          _psxToggle.onValueChanged.AddListener(OnPSXChanged);
        if (_invertScrollToggle != null) _invertScrollToggle.onValueChanged.AddListener(OnInvertScrollChanged);
        if (_doubleExposureMaxSlider != null) _doubleExposureMaxSlider.onValueChanged.AddListener(OnDoubleExposureMaxChanged);
        if (_bloomMaxSlider != null)    _bloomMaxSlider.onValueChanged.AddListener(OnBloomMaxChanged);
        if (_ditherMaxSlider != null)   _ditherMaxSlider.onValueChanged.AddListener(OnDitherMaxChanged);
        if (_vignetteMaxSlider != null) _vignetteMaxSlider.onValueChanged.AddListener(OnVignetteMaxChanged);
        if (_dynamicVisualsToggle != null) _dynamicVisualsToggle.onValueChanged.AddListener(OnDynamicVisualsChanged);

        // Initialize tab buttons
        if (_tabButtons != null)
        {
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                if (_tabButtons[i] != null)
                    _tabButtons[i].Initialize(this, i);
            }
        }

        if (_panelRoot != null)
            _panelRoot.SetActive(false);
    }

    // ── Open / Close ────────────────────────────────────────────

    public void Open()
    {
        if (_isOpen) return;
        _isOpen = true;

        ReadFromSettings();
        SwitchTab(0);

        if (_panelRoot != null)
            _panelRoot.SetActive(true);

        Debug.Log("[SettingsPanel] Opened.");
    }

    public void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;

        if (_panelRoot != null)
            _panelRoot.SetActive(false);

        Debug.Log("[SettingsPanel] Closed.");
    }

    /// <summary>Called by the Close button. Routes through pause menu to restore buttons.</summary>
    public void UI_Close()
    {
        if (PauseMenuController.Instance != null)
            PauseMenuController.Instance.CloseSettings();
        else
            Close();
    }

    // ── Tabs ────────────────────────────────────────────────────

    public void SwitchTab(int index)
    {
        _currentTab = index;

        if (_tabPanels != null)
        {
            for (int i = 0; i < _tabPanels.Length; i++)
            {
                if (_tabPanels[i] != null)
                    _tabPanels[i].SetActive(i == index);
            }
        }

        if (_tabButtons != null)
        {
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                if (_tabButtons[i] != null)
                    _tabButtons[i].SetActive(i == index);
            }
        }
    }

    // ── Read current values into UI controls ────────────────────

    private void ReadFromSettings()
    {
        _suppressCallbacks = true;

        // Visual
        if (_colorblindDropdown != null) _colorblindDropdown.value = (int)AccessibilitySettings.CurrentColorblindMode;
        if (_highContrastToggle != null) _highContrastToggle.isOn = AccessibilitySettings.HighContrast;
        if (_textScaleSlider != null)    _textScaleSlider.value = AccessibilitySettings.TextScale;
        UpdateTextScaleLabel();
        if (_doubleExposureMaxSlider != null) _doubleExposureMaxSlider.value = AccessibilitySettings.DoubleExposureMax;
        UpdateDoubleExposureMaxLabel();
        if (_bloomMaxSlider != null) _bloomMaxSlider.value = AccessibilitySettings.BloomMax;
        UpdateBloomMaxLabel();
        if (_ditherMaxSlider != null) _ditherMaxSlider.value = AccessibilitySettings.DitherMax;
        UpdateDitherMaxLabel();
        if (_vignetteMaxSlider != null) _vignetteMaxSlider.value = AccessibilitySettings.VignetteMax;
        UpdateVignetteMaxLabel();
        if (_dynamicVisualsToggle != null) _dynamicVisualsToggle.isOn = AccessibilitySettings.DynamicVisualsEnabled;

        // Audio
        if (_masterVolumeSlider != null) _masterVolumeSlider.value = AccessibilitySettings.MasterVolume;
        if (_musicVolumeSlider != null)  _musicVolumeSlider.value = AccessibilitySettings.MusicVolume;
        if (_sfxVolumeSlider != null)    _sfxVolumeSlider.value = AccessibilitySettings.SFXVolume;
        if (_ambienceVolumeSlider != null) _ambienceVolumeSlider.value = AccessibilitySettings.AmbienceVolume;
        if (_uiVolumeSlider != null)     _uiVolumeSlider.value = AccessibilitySettings.UIVolume;
        if (_captionsToggle != null)     _captionsToggle.isOn = AccessibilitySettings.CaptionsEnabled;

        // Motion
        if (_reduceMotionToggle != null) _reduceMotionToggle.isOn = AccessibilitySettings.ReduceMotion;
        if (_screenShakeSlider != null)  _screenShakeSlider.value = AccessibilitySettings.ScreenShakeScale;
        UpdateScreenShakeLabel();

        // Timing
        if (_timerDropdown != null) _timerDropdown.value = TimerMultiplierToIndex(AccessibilitySettings.TimerMultiplier);

        // Controls
        if (_invertScrollToggle != null) _invertScrollToggle.isOn = AccessibilitySettings.InvertScroll;

        // Performance
        if (_resolutionScaleSlider != null) _resolutionScaleSlider.value = AccessibilitySettings.ResolutionScale;
        UpdateResolutionScaleLabel();
        if (_qualityDropdown != null) _qualityDropdown.value = Mathf.Max(0, AccessibilitySettings.QualityPreset);
        if (_psxToggle != null) _psxToggle.isOn = AccessibilitySettings.PSXEnabled;

        _suppressCallbacks = false;
    }

    // ── UI Callbacks ────────────────────────────────────────────

    private void OnColorblindChanged(int val)
    {
        if (_suppressCallbacks) return;
        AccessibilitySettings.CurrentColorblindMode = (AccessibilitySettings.ColorblindMode)val;
    }

    private void OnHighContrastChanged(bool val)
    {
        if (_suppressCallbacks) return;
        AccessibilitySettings.HighContrast = val;
    }

    private void OnTextScaleChanged(float val)
    {
        if (_suppressCallbacks) return;
        AccessibilitySettings.TextScale = val;
        UpdateTextScaleLabel();
    }

    private void OnMasterVolumeChanged(float val)
    {
        if (_suppressCallbacks) return;
        AccessibilitySettings.MasterVolume = val;
    }

    private void OnMusicVolumeChanged(float val)
    {
        if (_suppressCallbacks) return;
        AccessibilitySettings.MusicVolume = val;
    }

    private void OnSFXVolumeChanged(float val)
    {
        if (_suppressCallbacks) return;
        AccessibilitySettings.SFXVolume = val;
    }

    private void OnAmbienceVolumeChanged(float val)
    {
        if (_suppressCallbacks) return;
        AccessibilitySettings.AmbienceVolume = val;
    }

    private void OnUIVolumeChanged(float val)
    {
        if (_suppressCallbacks) return;
        AccessibilitySettings.UIVolume = val;
    }

    private void OnCaptionsChanged(bool val)
    {
        if (_suppressCallbacks) return;
        AccessibilitySettings.CaptionsEnabled = val;
    }

    private void OnReduceMotionChanged(bool val)
    {
        if (_suppressCallbacks) return;
        AccessibilitySettings.ReduceMotion = val;
    }

    private void OnScreenShakeChanged(float val)
    {
        if (_suppressCallbacks) return;
        AccessibilitySettings.ScreenShakeScale = val;
        UpdateScreenShakeLabel();
    }

    private void OnTimerChanged(int val)
    {
        if (_suppressCallbacks) return;
        AccessibilitySettings.TimerMultiplier = IndexToTimerMultiplier(val);
    }

    private void OnResolutionScaleChanged(float val)
    {
        if (_suppressCallbacks) return;
        AccessibilitySettings.ResolutionScale = val;
        UpdateResolutionScaleLabel();
    }

    private void OnQualityChanged(int val)
    {
        if (_suppressCallbacks) return;
        AccessibilitySettings.QualityPreset = val;
    }

    private void OnPSXChanged(bool val)
    {
        if (_suppressCallbacks) return;
        AccessibilitySettings.PSXEnabled = val;
    }

    private void OnInvertScrollChanged(bool val)
    {
        if (_suppressCallbacks) return;
        AccessibilitySettings.InvertScroll = val;
    }

    private void OnDoubleExposureMaxChanged(float val)
    {
        if (_suppressCallbacks) return;
        AccessibilitySettings.DoubleExposureMax = val;
        UpdateDoubleExposureMaxLabel();
    }

    private void OnBloomMaxChanged(float val)
    {
        if (_suppressCallbacks) return;
        AccessibilitySettings.BloomMax = val;
        UpdateBloomMaxLabel();
    }

    private void OnDitherMaxChanged(float val)
    {
        if (_suppressCallbacks) return;
        AccessibilitySettings.DitherMax = val;
        UpdateDitherMaxLabel();
    }

    private void OnVignetteMaxChanged(float val)
    {
        if (_suppressCallbacks) return;
        AccessibilitySettings.VignetteMax = val;
        UpdateVignetteMaxLabel();
    }

    private void OnDynamicVisualsChanged(bool val)
    {
        if (_suppressCallbacks) return;
        AccessibilitySettings.DynamicVisualsEnabled = val;
    }

    // ── Reset Buttons ───────────────────────────────────────────

    public void UI_ResetAll()
    {
        AccessibilitySettings.ResetAll();
        ReadFromSettings();
    }

    // ── Timer index mapping ─────────────────────────────────────
    // 0 = Normal (1.0), 1 = Relaxed (1.5), 2 = Extended (2.0), 3 = No Timer (0)

    private static float IndexToTimerMultiplier(int index) => index switch
    {
        0 => 1.0f,
        1 => 1.5f,
        2 => 2.0f,
        3 => 0f,
        _ => 1.0f
    };

    private static int TimerMultiplierToIndex(float mult)
    {
        if (mult <= 0f)    return 3;
        if (mult <= 1.25f) return 0;
        if (mult <= 1.75f) return 1;
        return 2;
    }

    // ── Label helpers ───────────────────────────────────────────

    private void UpdateTextScaleLabel()
    {
        if (_textScaleLabel != null)
            _textScaleLabel.text = $"{AccessibilitySettings.TextScale:P0}";
    }

    private void UpdateScreenShakeLabel()
    {
        if (_screenShakeLabel != null)
            _screenShakeLabel.text = $"{AccessibilitySettings.ScreenShakeScale:P0}";
    }

    private void UpdateResolutionScaleLabel()
    {
        if (_resolutionScaleLabel != null)
            _resolutionScaleLabel.text = $"{AccessibilitySettings.ResolutionScale:P0}";
    }

    private void UpdateDoubleExposureMaxLabel()
    {
        if (_doubleExposureMaxLabel != null)
            _doubleExposureMaxLabel.text = $"{AccessibilitySettings.DoubleExposureMax:P0}";
    }

    private void UpdateBloomMaxLabel()
    {
        if (_bloomMaxLabel != null)
            _bloomMaxLabel.text = $"{AccessibilitySettings.BloomMax:P0}";
    }

    private void UpdateDitherMaxLabel()
    {
        if (_ditherMaxLabel != null)
            _ditherMaxLabel.text = $"{AccessibilitySettings.DitherMax:P0}";
    }

    private void UpdateVignetteMaxLabel()
    {
        if (_vignetteMaxLabel != null)
            _vignetteMaxLabel.text = $"{AccessibilitySettings.VignetteMax:P0}";
    }

    // ── Auto-build visual effect controls ──────────────────────

    /// <summary>
    /// Programmatically creates the visual effect sliders and toggle
    /// if they weren't wired in the Inspector. Appends them to the
    /// Visual tab panel (index 0 in _tabPanels).
    /// </summary>
    private void BuildVisualEffectControls()
    {
        // Skip if already wired
        if (_doubleExposureMaxSlider != null) return;

        // Find the Visual tab panel (first tab)
        if (_tabPanels == null || _tabPanels.Length == 0 || _tabPanels[0] == null) return;
        var visualTab = _tabPanels[0].transform;

        var theme = IrisTextTheme.Active;
        float y = -200f; // start below existing visual controls

        // Check how far down existing controls go
        for (int i = 0; i < visualTab.childCount; i++)
        {
            var rt = visualTab.GetChild(i).GetComponent<RectTransform>();
            if (rt != null)
            {
                float bottom = rt.anchoredPosition.y - rt.sizeDelta.y;
                if (bottom < y) y = bottom;
            }
        }
        y -= 20f; // padding

        // Section header
        y = CreateSettingsLabel(visualTab, "visual effects", y, theme, true);

        // Sliders
        (_doubleExposureMaxSlider, _doubleExposureMaxLabel) = CreateSettingsSlider(visualTab, "ghost trails", y, theme, 0f, 1f);
        y -= 50f;
        (_bloomMaxSlider, _bloomMaxLabel) = CreateSettingsSlider(visualTab, "glow intensity", y, theme, 0f, 1f);
        y -= 50f;
        (_ditherMaxSlider, _ditherMaxLabel) = CreateSettingsSlider(visualTab, "dither pattern", y, theme, 0f, 1f);
        y -= 50f;
        (_vignetteMaxSlider, _vignetteMaxLabel) = CreateSettingsSlider(visualTab, "edge darkening", y, theme, 0f, 1f);
        y -= 50f;

        // Toggle
        _dynamicVisualsToggle = CreateSettingsToggle(visualTab, "dynamic visuals", y, theme);
    }

    private static float CreateSettingsLabel(Transform parent, string text, float y, IrisTextTheme theme, bool bold)
    {
        var go = new GameObject($"Label_{text}");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(-40f, 30f);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = bold ? $"<b>{text}</b>" : text;
        tmp.fontSize = 18f;
        tmp.color = new Color(0.85f, 0.82f, 0.78f);
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.raycastTarget = false;
        tmp.richText = true;
        if (theme != null && theme.primaryFont != null) tmp.font = theme.primaryFont;

        return y - 35f;
    }

    private static (Slider slider, TMP_Text label) CreateSettingsSlider(
        Transform parent, string labelText, float y, IrisTextTheme theme, float min, float max)
    {
        // Container
        var go = new GameObject($"Slider_{labelText}");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(-40f, 40f);

        // Label (left side)
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        var labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0f, 0f);
        labelRT.anchorMax = new Vector2(0.4f, 1f);
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
        labelTMP.text = labelText;
        labelTMP.fontSize = 16f;
        labelTMP.color = new Color(0.75f, 0.72f, 0.68f);
        labelTMP.alignment = TextAlignmentOptions.Left;
        labelTMP.raycastTarget = false;
        if (theme != null && theme.primaryFont != null) labelTMP.font = theme.primaryFont;

        // Slider (center)
        var sliderGO = new GameObject("Slider");
        sliderGO.transform.SetParent(go.transform, false);
        var sliderRT = sliderGO.AddComponent<RectTransform>();
        sliderRT.anchorMin = new Vector2(0.42f, 0.3f);
        sliderRT.anchorMax = new Vector2(0.85f, 0.7f);
        sliderRT.offsetMin = Vector2.zero;
        sliderRT.offsetMax = Vector2.zero;

        // Background
        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(sliderGO.transform, false);
        var bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.14f, 0.16f, 0.9f);

        // Fill area
        var fillAreaGO = new GameObject("Fill Area");
        fillAreaGO.transform.SetParent(sliderGO.transform, false);
        var fillAreaRT = fillAreaGO.AddComponent<RectTransform>();
        fillAreaRT.anchorMin = Vector2.zero;
        fillAreaRT.anchorMax = Vector2.one;
        fillAreaRT.offsetMin = Vector2.zero;
        fillAreaRT.offsetMax = Vector2.zero;

        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(fillAreaGO.transform, false);
        var fillRT = fillGO.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color = new Color(0.5f, 0.48f, 0.55f, 0.8f);

        var slider = sliderGO.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = false;
        slider.fillRect = fillRT;
        slider.targetGraphic = bgImg;

        // Value label (right side)
        var valGO = new GameObject("Value");
        valGO.transform.SetParent(go.transform, false);
        var valRT = valGO.AddComponent<RectTransform>();
        valRT.anchorMin = new Vector2(0.87f, 0f);
        valRT.anchorMax = new Vector2(1f, 1f);
        valRT.offsetMin = Vector2.zero;
        valRT.offsetMax = Vector2.zero;

        var valTMP = valGO.AddComponent<TextMeshProUGUI>();
        valTMP.text = "100%";
        valTMP.fontSize = 14f;
        valTMP.color = new Color(0.6f, 0.58f, 0.55f);
        valTMP.alignment = TextAlignmentOptions.Right;
        valTMP.raycastTarget = false;
        if (theme != null && theme.primaryFont != null) valTMP.font = theme.primaryFont;

        return (slider, valTMP);
    }

    private static Toggle CreateSettingsToggle(Transform parent, string labelText, float y, IrisTextTheme theme)
    {
        var go = new GameObject($"Toggle_{labelText}");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(-40f, 35f);

        // Label
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        var labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0f, 0f);
        labelRT.anchorMax = new Vector2(0.8f, 1f);
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
        labelTMP.text = labelText;
        labelTMP.fontSize = 16f;
        labelTMP.color = new Color(0.75f, 0.72f, 0.68f);
        labelTMP.alignment = TextAlignmentOptions.Left;
        labelTMP.raycastTarget = false;
        if (theme != null && theme.primaryFont != null) labelTMP.font = theme.primaryFont;

        // Checkbox background
        var boxGO = new GameObject("Checkmark");
        boxGO.transform.SetParent(go.transform, false);
        var boxRT = boxGO.AddComponent<RectTransform>();
        boxRT.anchorMin = new Vector2(0.85f, 0.15f);
        boxRT.anchorMax = new Vector2(0.95f, 0.85f);
        boxRT.offsetMin = Vector2.zero;
        boxRT.offsetMax = Vector2.zero;

        var boxBg = boxGO.AddComponent<Image>();
        boxBg.color = new Color(0.15f, 0.14f, 0.16f, 0.9f);

        // Checkmark
        var checkGO = new GameObject("Check");
        checkGO.transform.SetParent(boxGO.transform, false);
        var checkRT = checkGO.AddComponent<RectTransform>();
        checkRT.anchorMin = new Vector2(0.15f, 0.15f);
        checkRT.anchorMax = new Vector2(0.85f, 0.85f);
        checkRT.offsetMin = Vector2.zero;
        checkRT.offsetMax = Vector2.zero;
        var checkImg = checkGO.AddComponent<Image>();
        checkImg.color = new Color(0.7f, 0.85f, 0.65f);

        var toggle = go.AddComponent<Toggle>();
        toggle.targetGraphic = boxBg;
        toggle.graphic = checkImg;
        toggle.isOn = true;

        return toggle;
    }
}

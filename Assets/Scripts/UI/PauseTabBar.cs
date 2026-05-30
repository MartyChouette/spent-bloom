using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Reusable horizontal tab bar for pause menu pages.
/// Programmatically builds tab buttons and manages panel switching.
/// </summary>
public class PauseTabBar : MonoBehaviour
{
    private static readonly Color ActiveBg = new Color(0.25f, 0.25f, 0.3f, 1f);
    private static readonly Color InactiveBg = new Color(0.15f, 0.15f, 0.18f, 0.8f);
    private static readonly Color ActiveLabel = Color.white;
    private static readonly Color InactiveLabel = new Color(0.7f, 0.7f, 0.7f, 1f);

    private GameObject[] _panels;
    private Image[] _backgrounds;
    private TMP_Text[] _labels;
    private int _currentTab;

    /// <summary>Currently active tab index.</summary>
    public int CurrentTab => _currentTab;

    /// <summary>Fired when the active tab changes.</summary>
    public event Action<int> OnTabChanged;

    /// <summary>
    /// Build the tab bar buttons and wire them to the given panels.
    /// Call once during BuildUI.
    /// </summary>
    public void Initialize(string[] tabNames, GameObject[] tabPanels)
    {
        _panels = tabPanels;
        _backgrounds = new Image[tabNames.Length];
        _labels = new TMP_Text[tabNames.Length];

        var theme = IrisTextTheme.Active;

        // Horizontal layout for buttons
        var barGO = new GameObject("TabButtons");
        barGO.transform.SetParent(transform, false);
        var barRT = barGO.AddComponent<RectTransform>();
        barRT.anchorMin = new Vector2(0f, 1f);
        barRT.anchorMax = new Vector2(1f, 1f);
        barRT.pivot = new Vector2(0.5f, 1f);
        barRT.anchoredPosition = new Vector2(0f, 0f);
        barRT.sizeDelta = new Vector2(0f, 32f);

        var hlg = barGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4f;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.padding = new RectOffset(4, 4, 0, 0);

        for (int i = 0; i < tabNames.Length; i++)
        {
            int tabIndex = i; // capture for closure

            var btnGO = new GameObject($"Tab_{tabNames[i]}");
            btnGO.transform.SetParent(barGO.transform, false);

            var bg = btnGO.AddComponent<Image>();
            bg.color = InactiveBg;
            _backgrounds[i] = bg;

            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = bg;
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.35f);
            colors.pressedColor = new Color(0.35f, 0.33f, 0.38f);
            btn.colors = colors;
            btn.onClick.AddListener(() => SwitchTab(tabIndex));

            var txtGO = new GameObject("Label");
            txtGO.transform.SetParent(btnGO.transform, false);
            var txtRT = txtGO.AddComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = Vector2.zero;
            txtRT.offsetMax = Vector2.zero;

            var tmp = txtGO.AddComponent<TextMeshProUGUI>();
            tmp.text = tabNames[i];
            tmp.fontSize = 14f;
            tmp.color = InactiveLabel;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            if (theme != null && theme.primaryFont != null) tmp.font = theme.primaryFont;
            _labels[i] = tmp;
        }

        SwitchTab(0);
    }

    /// <summary>
    /// Switch to the given tab index.
    /// </summary>
    public void SwitchTab(int index)
    {
        if (_panels == null || index < 0 || index >= _panels.Length) return;

        _currentTab = index;

        for (int i = 0; i < _panels.Length; i++)
        {
            bool active = i == index;
            if (_panels[i] != null) _panels[i].SetActive(active);
            if (_backgrounds != null && i < _backgrounds.Length && _backgrounds[i] != null)
                _backgrounds[i].color = active ? ActiveBg : InactiveBg;
            if (_labels != null && i < _labels.Length && _labels[i] != null)
                _labels[i].color = active ? ActiveLabel : InactiveLabel;
        }

        OnTabChanged?.Invoke(index);
    }
}

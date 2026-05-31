using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Reusable carousel for navigating sub-sections within a pause page.
/// Shows a title label with arrow indicators, dot indicators at the bottom,
/// and a subtle recessed background to communicate this is a sub-screen.
/// Parent page handles input and calls Next/Previous.
/// </summary>
public class PauseCarousel : MonoBehaviour
{
    private GameObject[] _sections;
    private string[] _names;
    private TMP_Text _titleLabel;
    private TMP_Text _dotsLabel;
    private TMP_Text _leftArrow;
    private TMP_Text _rightArrow;
    private int _currentIndex;

    /// <summary>Currently visible section index.</summary>
    public int CurrentIndex => _currentIndex;

    /// <summary>Fired when the active section changes.</summary>
    public event Action<int> OnSectionChanged;

    /// <summary>
    /// Build the carousel UI and wire it to the given sections.
    /// Call once during BuildUI.
    /// </summary>
    public void Initialize(string[] sectionNames, GameObject[] sectionRoots)
    {
        _sections = sectionRoots;
        _names = sectionNames;

        var theme = IrisTextTheme.Active;

        // Recessed background panel
        var bgGO = new GameObject("CarouselBG");
        bgGO.transform.SetParent(transform, false);
        var bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.05f, 0.05f, 0.07f, 0.5f);
        bgImg.raycastTarget = false;

        // Left arrow
        var leftGO = new GameObject("ArrowLeft");
        leftGO.transform.SetParent(transform, false);
        var leftRT = leftGO.AddComponent<RectTransform>();
        leftRT.anchorMin = new Vector2(0f, 0.4f);
        leftRT.anchorMax = new Vector2(0f, 0.6f);
        leftRT.pivot = new Vector2(0f, 0.5f);
        leftRT.anchoredPosition = new Vector2(6f, 0f);
        leftRT.sizeDelta = new Vector2(30f, 0f);

        _leftArrow = leftGO.AddComponent<TextMeshProUGUI>();
        _leftArrow.text = "\u25C0";
        _leftArrow.fontSize = 24f;
        _leftArrow.color = new Color(0.5f, 0.48f, 0.45f, 0.6f);
        _leftArrow.alignment = TextAlignmentOptions.Left;
        _leftArrow.raycastTarget = false;
        if (theme != null && theme.primaryFont != null) _leftArrow.font = theme.primaryFont;

        // Right arrow
        var rightGO = new GameObject("ArrowRight");
        rightGO.transform.SetParent(transform, false);
        var rightRT = rightGO.AddComponent<RectTransform>();
        rightRT.anchorMin = new Vector2(1f, 0.4f);
        rightRT.anchorMax = new Vector2(1f, 0.6f);
        rightRT.pivot = new Vector2(1f, 0.5f);
        rightRT.anchoredPosition = new Vector2(-6f, 0f);
        rightRT.sizeDelta = new Vector2(30f, 0f);

        _rightArrow = rightGO.AddComponent<TextMeshProUGUI>();
        _rightArrow.text = "\u25B6";
        _rightArrow.fontSize = 24f;
        _rightArrow.color = new Color(0.5f, 0.48f, 0.45f, 0.6f);
        _rightArrow.alignment = TextAlignmentOptions.Right;
        _rightArrow.raycastTarget = false;
        if (theme != null && theme.primaryFont != null) _rightArrow.font = theme.primaryFont;

        // Title label (top center, between arrows)
        var titleGO = new GameObject("CarouselTitle");
        titleGO.transform.SetParent(transform, false);
        var titleRT = titleGO.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.1f, 1f);
        titleRT.anchorMax = new Vector2(0.9f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -5f);
        titleRT.sizeDelta = new Vector2(0f, 30f);

        _titleLabel = titleGO.AddComponent<TextMeshProUGUI>();
        _titleLabel.fontSize = 20f;
        _titleLabel.fontStyle = FontStyles.Italic;
        _titleLabel.color = new Color(0.85f, 0.82f, 0.78f);
        _titleLabel.alignment = TextAlignmentOptions.Center;
        _titleLabel.raycastTarget = false;
        if (theme != null && theme.primaryFont != null) _titleLabel.font = theme.primaryFont;

        // Dot indicators (bottom center)
        var dotsGO = new GameObject("CarouselDots");
        dotsGO.transform.SetParent(transform, false);
        var dotsRT = dotsGO.AddComponent<RectTransform>();
        dotsRT.anchorMin = new Vector2(0f, 0f);
        dotsRT.anchorMax = new Vector2(1f, 0f);
        dotsRT.pivot = new Vector2(0.5f, 0f);
        dotsRT.anchoredPosition = new Vector2(0f, 10f);
        dotsRT.sizeDelta = new Vector2(0f, 20f);

        _dotsLabel = dotsGO.AddComponent<TextMeshProUGUI>();
        _dotsLabel.fontSize = 16f;
        _dotsLabel.color = new Color(0.6f, 0.58f, 0.55f);
        _dotsLabel.alignment = TextAlignmentOptions.Center;
        _dotsLabel.raycastTarget = false;
        if (theme != null && theme.primaryFont != null) _dotsLabel.font = theme.primaryFont;

        // Hint text (A/D keys)
        var hintGO = new GameObject("CarouselHint");
        hintGO.transform.SetParent(transform, false);
        var hintRT = hintGO.AddComponent<RectTransform>();
        hintRT.anchorMin = new Vector2(0f, 0f);
        hintRT.anchorMax = new Vector2(1f, 0f);
        hintRT.pivot = new Vector2(0.5f, 0f);
        hintRT.anchoredPosition = new Vector2(0f, 0f);
        hintRT.sizeDelta = new Vector2(0f, 14f);

        var hintTMP = hintGO.AddComponent<TextMeshProUGUI>();
        hintTMP.text = "A / D";
        hintTMP.fontSize = 11f;
        hintTMP.color = new Color(0.4f, 0.38f, 0.35f, 0.5f);
        hintTMP.alignment = TextAlignmentOptions.Center;
        hintTMP.raycastTarget = false;
        if (theme != null && theme.primaryFont != null) hintTMP.font = theme.primaryFont;

        SetIndex(0);
    }

    /// <summary>Navigate to the next section (wrapping).</summary>
    public void Next()
    {
        if (_sections == null || _sections.Length == 0) return;
        SetIndex((_currentIndex + 1) % _sections.Length);
    }

    /// <summary>Navigate to the previous section (wrapping).</summary>
    public void Previous()
    {
        if (_sections == null || _sections.Length == 0) return;
        SetIndex((_currentIndex - 1 + _sections.Length) % _sections.Length);
    }

    /// <summary>Jump to a specific section index.</summary>
    public void SetIndex(int index)
    {
        if (_sections == null || index < 0 || index >= _sections.Length) return;

        _currentIndex = index;

        for (int i = 0; i < _sections.Length; i++)
        {
            if (_sections[i] != null)
                _sections[i].SetActive(i == index);
        }

        if (_titleLabel != null && _names != null && index < _names.Length)
            _titleLabel.text = _names[index];

        UpdateDots();
        OnSectionChanged?.Invoke(index);
    }

    private void UpdateDots()
    {
        if (_dotsLabel == null || _sections == null) return;

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < _sections.Length; i++)
        {
            sb.Append(i == _currentIndex ? "\u25CF " : "\u25CB ");
        }
        _dotsLabel.text = sb.ToString().TrimEnd();
    }
}

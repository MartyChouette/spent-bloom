using System;
using UnityEngine;
using TMPro;

/// <summary>
/// Reusable carousel for navigating sub-sections within a pause page.
/// Shows a title label and dot indicators. Parent page handles input
/// and calls Next/Previous.
/// </summary>
public class PauseCarousel : MonoBehaviour
{
    private GameObject[] _sections;
    private string[] _names;
    private TMP_Text _titleLabel;
    private TMP_Text _dotsLabel;
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

        // Title label
        var titleGO = new GameObject("CarouselTitle");
        titleGO.transform.SetParent(transform, false);
        var titleRT = titleGO.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, 0f);
        titleRT.sizeDelta = new Vector2(0f, 30f);

        _titleLabel = titleGO.AddComponent<TextMeshProUGUI>();
        _titleLabel.fontSize = 20f;
        _titleLabel.fontStyle = FontStyles.Italic;
        _titleLabel.color = new Color(0.85f, 0.82f, 0.78f);
        _titleLabel.alignment = TextAlignmentOptions.Center;
        _titleLabel.raycastTarget = false;
        if (theme != null && theme.primaryFont != null) _titleLabel.font = theme.primaryFont;

        // Dot indicators
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
            _titleLabel.text = $"\u25C0  {_names[index]}  \u25B6";

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

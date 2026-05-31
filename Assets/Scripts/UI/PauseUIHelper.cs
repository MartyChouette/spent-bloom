using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shared factory methods for building pause menu UI elements programmatically.
/// Extracted from PausePageDates and PausePageSystem to avoid duplication.
/// </summary>
public static class PauseUIHelper
{
    /// <summary>
    /// Create a themed text label inside a layout group.
    /// </summary>
    public static TMP_Text CreateLabel(Transform parent, string text, float fontSize,
        Color color, IrisTextTheme theme, float height)
    {
        var go = new GameObject("Entry");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, height);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        tmp.richText = true;
        if (theme != null && theme.primaryFont != null) tmp.font = theme.primaryFont;

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;

        return tmp;
    }

    /// <summary>
    /// Create a section header label (bold, slightly larger).
    /// </summary>
    public static TMP_Text CreateHeaderLabel(Transform parent, string text,
        IrisTextTheme theme, float height = 35f)
    {
        var tmp = CreateLabel(parent, $"<b>{text}</b>", 22f,
            new Color(0.85f, 0.82f, 0.78f), theme, height);
        return tmp;
    }

    /// <summary>
    /// Create a button with background, hover colors, and label.
    /// </summary>
    public static Button CreateButton(Transform parent, string label, float yPos,
        IrisTextTheme theme, UnityEngine.Events.UnityAction onClick)
    {
        var btnGO = new GameObject($"Btn_{label}");
        btnGO.transform.SetParent(parent, false);

        var btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.5f, 1f);
        btnRT.anchorMax = new Vector2(0.5f, 1f);
        btnRT.pivot = new Vector2(0.5f, 1f);
        btnRT.anchoredPosition = new Vector2(0f, yPos);
        btnRT.sizeDelta = new Vector2(300f, 45f);

        var img = btnGO.AddComponent<Image>();
        img.color = new Color(0.15f, 0.14f, 0.16f, 0.9f);

        var btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.25f, 0.24f, 0.28f);
        colors.pressedColor = new Color(0.35f, 0.33f, 0.38f);
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        var txtGO = new GameObject("Label");
        txtGO.transform.SetParent(btnGO.transform, false);
        var txtRT = txtGO.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero;
        txtRT.offsetMax = Vector2.zero;

        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 20f;
        tmp.fontStyle = FontStyles.Italic;
        tmp.color = new Color(0.85f, 0.82f, 0.78f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        if (theme != null && theme.primaryFont != null) tmp.font = theme.primaryFont;

        return btn;
    }

    /// <summary>
    /// Create a complete scrollable list container (ScrollRect > Viewport > Content).
    /// Returns the content transform to parent items into.
    /// </summary>
    public static Transform CreateScrollableList(Transform parent, RectOffset margins = null)
    {
        if (margins == null) margins = new RectOffset(20, 20, 20, 20);

        // Scroll root
        var scrollGO = new GameObject("ScrollView");
        scrollGO.transform.SetParent(parent, false);
        var scrollRT = scrollGO.AddComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(margins.left, margins.bottom);
        scrollRT.offsetMax = new Vector2(-margins.right, -margins.top);

        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;

        // Viewport
        var viewGO = new GameObject("Viewport");
        viewGO.transform.SetParent(scrollGO.transform, false);
        var viewRT = viewGO.AddComponent<RectTransform>();
        viewRT.anchorMin = Vector2.zero;
        viewRT.anchorMax = Vector2.one;
        viewRT.offsetMin = Vector2.zero;
        viewRT.offsetMax = Vector2.zero;
        viewGO.AddComponent<Image>().color = Color.clear;
        viewGO.AddComponent<Mask>().showMaskGraphic = false;
        scrollRect.viewport = viewRT;

        // Content
        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(viewGO.transform, false);
        var contentRT = contentGO.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.sizeDelta = Vector2.zero;

        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2f;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.padding = new RectOffset(10, 10, 5, 5);

        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRT;

        return contentRT;
    }

    /// <summary>
    /// Destroy all children of a transform.
    /// </summary>
    public static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Object.Destroy(parent.GetChild(i).gameObject);
    }

    /// <summary>
    /// Ensure a page GameObject has a full-stretch RectTransform.
    /// Call at the start of BuildUI to fix sizing when pages are
    /// created manually in the scene without proper anchoring.
    /// </summary>
    public static void EnsureFullStretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}

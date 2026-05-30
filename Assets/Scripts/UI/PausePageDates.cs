using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pause menu page showing date history, calendar, and learned knowledge.
/// Shows each character dated, their arc progress, and what the player
/// has discovered about their preferences.
/// </summary>
public class PausePageDates : MonoBehaviour
{
    private Transform _listContainer;
    private TMP_Text _emptyLabel;
    private TMP_Text _dayLabel;
    private bool _built;

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (!_built) BuildUI();

        // Day counter
        if (_dayLabel != null && GameClock.Instance != null)
            _dayLabel.text = $"day {GameClock.Instance.CurrentDay}";

        // Clear entries
        if (_listContainer != null)
        {
            for (int i = _listContainer.childCount - 1; i >= 0; i--)
                Destroy(_listContainer.GetChild(i).gameObject);
        }

        var entries = DateHistory.Entries;
        if (entries == null || entries.Count == 0)
        {
            if (_emptyLabel != null) { _emptyLabel.gameObject.SetActive(true); _emptyLabel.text = "no dates yet"; }
            return;
        }
        if (_emptyLabel != null) _emptyLabel.gameObject.SetActive(false);

        var theme = IrisTextTheme.Active;

        // Group by character, show arc progress
        var characters = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<DateHistory.DateHistoryEntry>>();
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (!characters.ContainsKey(e.name))
                characters[e.name] = new System.Collections.Generic.List<DateHistory.DateHistoryEntry>();
            characters[e.name].Add(e);
        }

        foreach (var kvp in characters)
        {
            string charName = kvp.Key;
            var dates = kvp.Value;
            int successCount = 0;
            for (int i = 0; i < dates.Count; i++)
                if (dates[i].succeeded) successCount++;

            // Character header
            CreateLabel(_listContainer, $"<b>{charName}</b>  {successCount}/3 dates", 22f,
                successCount >= 3 ? new Color(0.7f, 0.85f, 0.65f) : new Color(0.85f, 0.82f, 0.78f),
                theme, 35f);

            // Each date entry
            for (int i = 0; i < dates.Count; i++)
            {
                var d = dates[i];
                string result = d.succeeded ? $"<color=#88CC88>{d.grade}</color>" : $"<color=#CC8888>failed</color>";
                string flower = !string.IsNullOrEmpty(d.flowerGrade) ? $"  flower: {d.flowerGrade}" : "";
                CreateLabel(_listContainer, $"  day {d.day}  {result}  {d.affection:F0}%{flower}", 16f,
                    new Color(0.7f, 0.68f, 0.65f), theme, 24f);

                // Learned preferences
                if (d.learnedLikes != null && d.learnedLikes.Count > 0)
                {
                    string likes = string.Join(", ", d.learnedLikes);
                    CreateLabel(_listContainer, $"    <color=#CC8899>likes: {likes}</color>", 14f,
                        new Color(0.6f, 0.58f, 0.55f), theme, 20f);
                }
                if (d.learnedDislikes != null && d.learnedDislikes.Count > 0)
                {
                    string dislikes = string.Join(", ", d.learnedDislikes);
                    CreateLabel(_listContainer, $"    <color=#8888AA>dislikes: {dislikes}</color>", 14f,
                        new Color(0.6f, 0.58f, 0.55f), theme, 20f);
                }
            }

            // Spacer
            CreateLabel(_listContainer, "", 10f, Color.clear, theme, 10f);
        }
    }

    private static void CreateLabel(Transform parent, string text, float fontSize, Color color, IrisTextTheme theme, float height)
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
    }

    private void BuildUI()
    {
        _built = true;

        var theme = IrisTextTheme.Active;

        // Day label at top
        var dayGO = new GameObject("DayLabel");
        dayGO.transform.SetParent(transform, false);
        var dayRT = dayGO.AddComponent<RectTransform>();
        dayRT.anchorMin = new Vector2(0f, 1f);
        dayRT.anchorMax = new Vector2(1f, 1f);
        dayRT.pivot = new Vector2(0.5f, 1f);
        dayRT.anchoredPosition = new Vector2(0f, -10f);
        dayRT.sizeDelta = new Vector2(0f, 35f);
        _dayLabel = dayGO.AddComponent<TextMeshProUGUI>();
        _dayLabel.fontSize = 24f;
        _dayLabel.fontStyle = FontStyles.Italic;
        _dayLabel.color = new Color(0.7f, 0.65f, 0.6f);
        _dayLabel.alignment = TextAlignmentOptions.Center;
        _dayLabel.raycastTarget = false;
        if (theme != null && theme.primaryFont != null) _dayLabel.font = theme.primaryFont;

        // Scroll view
        var scrollGO = new GameObject("DateScroll");
        scrollGO.transform.SetParent(transform, false);
        var scrollRT = scrollGO.AddComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(20f, 20f);
        scrollRT.offsetMax = new Vector2(-20f, -50f);

        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;

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
        _listContainer = contentRT;

        // Empty label
        var emptyGO = new GameObject("Empty");
        emptyGO.transform.SetParent(transform, false);
        var emptyRT = emptyGO.AddComponent<RectTransform>();
        emptyRT.anchorMin = new Vector2(0.5f, 0.5f);
        emptyRT.anchorMax = new Vector2(0.5f, 0.5f);
        emptyRT.sizeDelta = new Vector2(400f, 50f);
        _emptyLabel = emptyGO.AddComponent<TextMeshProUGUI>();
        _emptyLabel.fontSize = 22f;
        _emptyLabel.fontStyle = FontStyles.Italic;
        _emptyLabel.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        _emptyLabel.alignment = TextAlignmentOptions.Center;
        _emptyLabel.raycastTarget = false;
        if (theme != null && theme.primaryFont != null) _emptyLabel.font = theme.primaryFont;
    }
}

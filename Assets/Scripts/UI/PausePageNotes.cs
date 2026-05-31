using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Pause menu Notes page. Three tabs collecting player knowledge:
/// Tutorial (milestones + tutorial info bits), Dates (history + learned preferences),
/// Discoveries (non-tutorial info bits from exploration).
/// </summary>
public class PausePageNotes : MonoBehaviour
{
    private PauseTabBar _tabBar;
    private bool _built;

    // Content containers for each tab
    private Transform _tutorialContent;
    private Transform _datesContent;
    private Transform _discoveriesContent;

    private void OnEnable()
    {
        if (!_built) BuildUI();
        RefreshCurrentTab();
    }

    private void RefreshCurrentTab()
    {
        if (_tabBar == null) return;
        switch (_tabBar.CurrentTab)
        {
            case 0: RefreshTutorial(); break;
            case 1: RefreshDates(); break;
            case 2: RefreshDiscoveries(); break;
        }
    }

    // ─────────────────── Tutorial ───────────────────

    private void RefreshTutorial()
    {
        if (_tutorialContent == null) return;
        PauseUIHelper.ClearChildren(_tutorialContent);

        var theme = IrisTextTheme.Active;

        // Milestones from TutorialGateTracker
        PauseUIHelper.CreateHeaderLabel(_tutorialContent, "milestones", theme);

        if (TutorialGateTracker.Instance != null)
        {
            var tracker = TutorialGateTracker.Instance;
            string gateStatus = tracker.GatePassed ? "<color=#88CC88>gate passed</color>" : $"{tracker.UniqueMilestonesCompleted} completed";
            PauseUIHelper.CreateLabel(_tutorialContent, $"  {gateStatus}", 16f,
                new Color(0.7f, 0.68f, 0.65f), theme, 24f);

            var types = System.Enum.GetValues(typeof(TutorialGateTracker.MilestoneType));
            foreach (TutorialGateTracker.MilestoneType type in types)
            {
                bool done = tracker.HasCompleted(type);
                int count = tracker.GetCount(type);
                string check = done ? "<color=#88CC88>\u2713</color>" : "<color=#666666>\u2717</color>";
                string countStr = count > 1 ? $" (x{count})" : "";
                string name = FormatMilestoneName(type);
                PauseUIHelper.CreateLabel(_tutorialContent, $"  {check} {name}{countStr}", 15f,
                    done ? new Color(0.7f, 0.68f, 0.65f) : new Color(0.5f, 0.48f, 0.45f), theme, 22f);
            }
        }
        else
        {
            PauseUIHelper.CreateLabel(_tutorialContent, "  <color=#666666>no tutorial data</color>", 15f,
                new Color(0.5f, 0.5f, 0.5f), theme, 22f);
        }

        // Tutorial info bits from PlayerKnowledgeTracker
        var knowledge = Object.FindAnyObjectByType<PlayerKnowledgeTracker>();
        if (knowledge != null)
        {
            bool hasAny = false;
            var infoBits = knowledge.LearnedInfo;
            for (int i = 0; i < infoBits.Count; i++)
            {
                var info = infoBits[i];
                if (info == null || !info.isTutorial) continue;

                if (!hasAny)
                {
                    PauseUIHelper.CreateLabel(_tutorialContent, "", 10f, Color.clear, theme, 10f);
                    PauseUIHelper.CreateHeaderLabel(_tutorialContent, "learned", theme);
                    hasAny = true;
                }

                PauseUIHelper.CreateLabel(_tutorialContent,
                    $"  <b>{info.title}</b>", 16f,
                    new Color(0.85f, 0.82f, 0.78f), theme, 24f);

                if (!string.IsNullOrEmpty(info.description))
                {
                    PauseUIHelper.CreateLabel(_tutorialContent,
                        $"    {info.description}", 14f,
                        new Color(0.6f, 0.58f, 0.55f), theme, 20f);
                }
            }
        }

        PauseUIHelper.CreateLabel(_tutorialContent, "", 10f, Color.clear, theme, 10f);
    }

    private static string FormatMilestoneName(TutorialGateTracker.MilestoneType type)
    {
        return type switch
        {
            TutorialGateTracker.MilestoneType.ShoePaired => "pair shoes",
            TutorialGateTracker.MilestoneType.ShoesRacked => "rack shoes",
            TutorialGateTracker.MilestoneType.BookCollectionDone => "complete book collection",
            TutorialGateTracker.MilestoneType.GunplaDone => "assemble gunpla",
            TutorialGateTracker.MilestoneType.StainCleaned => "clean a stain",
            TutorialGateTracker.MilestoneType.TrashBinned => "bin trash",
            _ => type.ToString()
        };
    }

    // ─────────────────── Dates ───────────────────

    private void RefreshDates()
    {
        if (_datesContent == null) return;
        PauseUIHelper.ClearChildren(_datesContent);

        var theme = IrisTextTheme.Active;

        // Day counter
        if (GameClock.Instance != null)
        {
            PauseUIHelper.CreateLabel(_datesContent, $"<i>day {GameClock.Instance.CurrentDay}</i>", 24f,
                new Color(0.7f, 0.65f, 0.6f), theme, 35f);
        }

        var entries = DateHistory.Entries;
        if (entries == null || entries.Count == 0)
        {
            PauseUIHelper.CreateLabel(_datesContent, "<i>no dates yet</i>", 22f,
                new Color(0.5f, 0.5f, 0.5f, 0.6f), theme, 40f);
            return;
        }

        // Group by character
        var characters = new Dictionary<string, List<DateHistory.DateHistoryEntry>>();
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (!characters.ContainsKey(e.name))
                characters[e.name] = new List<DateHistory.DateHistoryEntry>();
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
            Color headerColor = successCount >= 3
                ? new Color(0.7f, 0.85f, 0.65f)
                : new Color(0.85f, 0.82f, 0.78f);
            PauseUIHelper.CreateHeaderLabel(_datesContent, $"{charName}  {successCount}/3 dates", theme, 35f)
                .color = headerColor;

            // Each date entry
            for (int i = 0; i < dates.Count; i++)
            {
                var d = dates[i];
                string result = d.succeeded
                    ? $"<color=#88CC88>{d.grade}</color>"
                    : $"<color=#CC8888>failed</color>";
                string flower = !string.IsNullOrEmpty(d.flowerGrade)
                    ? $"  flower: {d.flowerGrade}" : "";
                PauseUIHelper.CreateLabel(_datesContent,
                    $"  day {d.day}  {result}  {d.affection:F0}%{flower}", 16f,
                    new Color(0.7f, 0.68f, 0.65f), theme, 24f);

                // Learned preferences
                if (d.learnedLikes != null && d.learnedLikes.Count > 0)
                {
                    string likes = string.Join(", ", d.learnedLikes);
                    PauseUIHelper.CreateLabel(_datesContent,
                        $"    <color=#CC8899>likes: {likes}</color>", 14f,
                        new Color(0.6f, 0.58f, 0.55f), theme, 20f);
                }
                if (d.learnedDislikes != null && d.learnedDislikes.Count > 0)
                {
                    string dislikes = string.Join(", ", d.learnedDislikes);
                    PauseUIHelper.CreateLabel(_datesContent,
                        $"    <color=#8888AA>dislikes: {dislikes}</color>", 14f,
                        new Color(0.6f, 0.58f, 0.55f), theme, 20f);
                }
            }

            PauseUIHelper.CreateLabel(_datesContent, "", 10f, Color.clear, theme, 10f);
        }
    }

    // ─────────────────── Discoveries ───────────────────

    private void RefreshDiscoveries()
    {
        if (_discoveriesContent == null) return;
        PauseUIHelper.ClearChildren(_discoveriesContent);

        var theme = IrisTextTheme.Active;
        var knowledge = Object.FindAnyObjectByType<PlayerKnowledgeTracker>();

        if (knowledge == null)
        {
            PauseUIHelper.CreateLabel(_discoveriesContent, "<i>no discoveries yet</i>", 22f,
                new Color(0.5f, 0.5f, 0.5f, 0.6f), theme, 40f);
            return;
        }

        // Info bits (non-tutorial discoveries)
        bool anyInfo = false;
        var infoBits = knowledge.LearnedInfo;
        for (int i = 0; i < infoBits.Count; i++)
        {
            var info = infoBits[i];
            if (info == null || info.isTutorial) continue;

            if (!anyInfo)
            {
                PauseUIHelper.CreateHeaderLabel(_discoveriesContent, "discoveries", theme);
                anyInfo = true;
            }

            PauseUIHelper.CreateLabel(_discoveriesContent,
                $"  <b>{info.title}</b>", 16f,
                new Color(0.85f, 0.82f, 0.78f), theme, 24f);

            if (!string.IsNullOrEmpty(info.description))
            {
                PauseUIHelper.CreateLabel(_discoveriesContent,
                    $"    {info.description}", 14f,
                    new Color(0.6f, 0.58f, 0.55f), theme, 20f);
            }
        }

        // Discovered items
        bool anyItems = false;
        var items = knowledge.DiscoveredItems;
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item == null) continue;

            if (!anyItems)
            {
                PauseUIHelper.CreateLabel(_discoveriesContent, "", 10f, Color.clear, theme, 10f);
                PauseUIHelper.CreateHeaderLabel(_discoveriesContent, "discovered items", theme);
                anyItems = true;
            }

            PauseUIHelper.CreateLabel(_discoveriesContent,
                $"  {item.displayName}", 15f,
                new Color(0.7f, 0.68f, 0.65f), theme, 22f);
        }

        if (!anyInfo && !anyItems)
        {
            PauseUIHelper.CreateLabel(_discoveriesContent, "<i>no discoveries yet</i>", 22f,
                new Color(0.5f, 0.5f, 0.5f, 0.6f), theme, 40f);
        }
    }

    // ─────────────────── Build UI ───────────────────

    private void BuildUI()
    {
        _built = true;
        PauseUIHelper.EnsureFullStretch(gameObject);

        // Tab bar
        var tabBarGO = new GameObject("TabBar");
        tabBarGO.transform.SetParent(transform, false);
        var tabBarRT = tabBarGO.AddComponent<RectTransform>();
        tabBarRT.anchorMin = new Vector2(0f, 1f);
        tabBarRT.anchorMax = new Vector2(1f, 1f);
        tabBarRT.pivot = new Vector2(0.5f, 1f);
        tabBarRT.anchoredPosition = new Vector2(0f, -5f);
        tabBarRT.sizeDelta = new Vector2(-20f, 32f);
        _tabBar = tabBarGO.AddComponent<PauseTabBar>();

        // Tab panels
        var panelNames = new[] { "tutorial", "dates", "discoveries" };
        var panels = new GameObject[3];
        var contentContainers = new Transform[3];

        for (int i = 0; i < 3; i++)
        {
            var panelGO = new GameObject($"Panel_{panelNames[i]}");
            panelGO.transform.SetParent(transform, false);
            var panelRT = panelGO.AddComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = new Vector2(10f, 10f);
            panelRT.offsetMax = new Vector2(-10f, -45f); // below tab bar
            panels[i] = panelGO;

            contentContainers[i] = PauseUIHelper.CreateScrollableList(panelGO.transform,
                new RectOffset(0, 0, 0, 0));
        }

        _tutorialContent = contentContainers[0];
        _datesContent = contentContainers[1];
        _discoveriesContent = contentContainers[2];

        _tabBar.OnTabChanged += _ => RefreshCurrentTab();
        _tabBar.Initialize(panelNames, panels);
    }
}

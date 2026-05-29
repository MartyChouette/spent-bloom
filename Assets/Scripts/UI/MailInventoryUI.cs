using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays collected mail in a scrollable list. Used as a pause menu page
/// and also opened when clicking the mail pile in the apartment.
/// Attach to the Mail page root in PauseMenuController.
/// </summary>
public class MailInventoryUI : MonoBehaviour
{
    public static MailInventoryUI Instance { get; private set; }

    [Header("UI References (auto-built if null)")]
    [SerializeField] private Transform _listContainer;
    [SerializeField] private TMP_Text _emptyLabel;

    private bool _built;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        Refresh();
    }

    /// <summary>Rebuild the list from MailInventory.</summary>
    public void Refresh()
    {
        if (!_built) BuildUI();

        // Clear existing entries
        if (_listContainer != null)
        {
            for (int i = _listContainer.childCount - 1; i >= 0; i--)
                Destroy(_listContainer.GetChild(i).gameObject);
        }

        var all = MailInventory.All;

        if (all.Count == 0)
        {
            if (_emptyLabel != null)
            {
                _emptyLabel.gameObject.SetActive(true);
                _emptyLabel.text = "no mail yet";
            }
            return;
        }

        if (_emptyLabel != null)
            _emptyLabel.gameObject.SetActive(false);

        var theme = IrisTextTheme.Active;

        // Show newest first
        for (int i = all.Count - 1; i >= 0; i--)
        {
            var mail = all[i];
            CreateEntry(mail, theme);
        }
    }

    private void CreateEntry(CollectedMail mail, IrisTextTheme theme)
    {
        var entryGO = new GameObject($"Mail_{mail.senderName}");
        entryGO.transform.SetParent(_listContainer, false);

        var entryRT = entryGO.AddComponent<RectTransform>();
        entryRT.sizeDelta = new Vector2(0f, 50f);

        var layout = entryGO.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.padding = new RectOffset(10, 10, 5, 5);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        // Unread dot
        if (!mail.wasRead)
        {
            var dotGO = new GameObject("Dot");
            dotGO.transform.SetParent(entryGO.transform, false);
            var dotTMP = dotGO.AddComponent<TextMeshProUGUI>();
            dotTMP.text = "\u2022";
            dotTMP.fontSize = 20f;
            dotTMP.color = new Color(1f, 0.85f, 0.4f);
            dotTMP.raycastTarget = false;
            if (theme != null && theme.primaryFont != null) dotTMP.font = theme.primaryFont;
            var dotLE = dotGO.AddComponent<LayoutElement>();
            dotLE.preferredWidth = 20f;
        }

        // Type icon
        string typePrefix = mail.type switch
        {
            MailItemType.Package => "[box]",
            MailItemType.Catalog => "[ad]",
            _ => ""
        };

        // Label
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(entryGO.transform, false);
        var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();

        string displayName = mail.type == MailItemType.Package
            ? $"{typePrefix} {mail.itemDisplayName}"
            : $"{typePrefix} {mail.senderName}";
        labelTMP.text = $"{displayName}  <color=#666666>day {mail.dayReceived}</color>";
        labelTMP.fontSize = 18f;
        labelTMP.color = mail.wasRead ? new Color(0.6f, 0.6f, 0.58f) : new Color(0.85f, 0.82f, 0.78f);
        labelTMP.raycastTarget = false;
        if (theme != null && theme.primaryFont != null) labelTMP.font = theme.primaryFont;

        var labelLE = labelGO.AddComponent<LayoutElement>();
        labelLE.flexibleWidth = 1f;

        // Click to re-read (letters and catalogs only)
        if (mail.type != MailItemType.Package && mail.textLines != null && mail.textLines.Length > 0)
        {
            var btnImg = entryGO.AddComponent<Image>();
            btnImg.color = new Color(0f, 0f, 0f, 0f); // invisible click target

            var btn = entryGO.AddComponent<Button>();
            btn.targetGraphic = btnImg;

            var captured = mail; // capture for closure
            btn.onClick.AddListener(() =>
            {
                MailInventory.MarkRead(captured);
                MailTextOverlay.Show(captured.senderName, captured.textLines, () => Refresh());
            });
        }
    }

    private void BuildUI()
    {
        _built = true;

        // If references aren't wired, build a simple scroll view
        if (_listContainer != null) return;

        // Scroll view
        var scrollGO = new GameObject("MailScroll");
        scrollGO.transform.SetParent(transform, false);
        var scrollRT = scrollGO.AddComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(20f, 20f);
        scrollRT.offsetMax = new Vector2(-20f, -20f);

        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        // Viewport
        var viewGO = new GameObject("Viewport");
        viewGO.transform.SetParent(scrollGO.transform, false);
        var viewRT = viewGO.AddComponent<RectTransform>();
        viewRT.anchorMin = Vector2.zero;
        viewRT.anchorMax = Vector2.one;
        viewRT.offsetMin = Vector2.zero;
        viewRT.offsetMax = Vector2.zero;
        viewGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        viewGO.AddComponent<Mask>().showMaskGraphic = false;
        scrollRect.viewport = viewRT;

        // Content
        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(viewGO.transform, false);
        var contentRT = contentGO.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = new Vector2(0f, 0f);

        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4f;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

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

        var theme = IrisTextTheme.Active;
        if (theme != null && theme.primaryFont != null) _emptyLabel.font = theme.primaryFont;
    }

    /// <summary>
    /// Open the mail inventory as a standalone overlay (from clicking the mail pile in the apartment).
    /// </summary>
    public static void ShowOverlay()
    {
        // If the pause menu is open and has a Mail page, just switch to it
        // Otherwise, create a temporary overlay
        if (Instance != null && Instance.gameObject.activeInHierarchy)
        {
            Instance.Refresh();
            return;
        }

        var go = new GameObject("MailInventoryOverlay");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 205;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        go.AddComponent<GraphicRaycaster>();

        // Dark backdrop
        var bgGO = new GameObject("Backdrop");
        bgGO.transform.SetParent(go.transform, false);
        var bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.03f, 0.03f, 0.05f, 0.8f);
        var bgBtn = bgGO.AddComponent<Button>();
        bgBtn.targetGraphic = bgImg;
        bgBtn.onClick.AddListener(() => Destroy(go));

        // Panel
        var panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(go.transform, false);
        var panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.15f, 0.1f);
        panelRT.anchorMax = new Vector2(0.85f, 0.9f);
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;

        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.07f, 0.09f, 0.95f);
        panelImg.raycastTarget = true; // block clicks through to backdrop

        // Title
        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(panelGO.transform, false);
        var titleRT = titleGO.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -10f);
        titleRT.sizeDelta = new Vector2(0f, 40f);

        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "collected mail";
        titleTMP.fontSize = 24f;
        titleTMP.fontStyle = FontStyles.Italic;
        titleTMP.color = new Color(0.7f, 0.65f, 0.6f);
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.raycastTarget = false;

        var theme = IrisTextTheme.Active;
        if (theme != null && theme.primaryFont != null) titleTMP.font = theme.primaryFont;

        // Mail list
        var listGO = new GameObject("MailList");
        listGO.transform.SetParent(panelGO.transform, false);
        var listRT = listGO.AddComponent<RectTransform>();
        listRT.anchorMin = Vector2.zero;
        listRT.anchorMax = Vector2.one;
        listRT.offsetMin = new Vector2(0f, 0f);
        listRT.offsetMax = new Vector2(0f, -60f);

        var ui = listGO.AddComponent<MailInventoryUI>();
        ui.Refresh();
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Pause menu page for Nema. Carousel with four sections:
/// Wellbeing (flower petals), Personality (trait dots),
/// Outfit (current outfit), Flower (equilibrium indicator).
/// </summary>
public class PausePageNema : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("Drag the NemaPersonality ScriptableObject here.")]
    [SerializeField] private NemaPersonality _personality;

    [Header("Carousel Input")]
    [Tooltip("Navigate carousel left.")]
    [SerializeField] private InputActionReference _carouselLeft;
    [Tooltip("Navigate carousel right.")]
    [SerializeField] private InputActionReference _carouselRight;

    private PauseCarousel _carousel;
    private bool _built;

    // Section content text fields
    private TMP_Text _wellbeingText;
    private TMP_Text _moodLabel;
    private TMP_Text _personalityText;
    private TMP_Text _outfitText;
    private TMP_Text _flowerText;
    private TMP_Text _flowerCommentary;

    private void OnEnable()
    {
        Refresh();

        if (_carouselLeft != null && _carouselLeft.action != null)
        {
            _carouselLeft.action.performed += OnCarouselLeft;
            _carouselLeft.action.Enable();
        }
        if (_carouselRight != null && _carouselRight.action != null)
        {
            _carouselRight.action.performed += OnCarouselRight;
            _carouselRight.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (_carouselLeft != null && _carouselLeft.action != null)
        {
            _carouselLeft.action.performed -= OnCarouselLeft;
            _carouselLeft.action.Disable();
        }
        if (_carouselRight != null && _carouselRight.action != null)
        {
            _carouselRight.action.performed -= OnCarouselRight;
            _carouselRight.action.Disable();
        }
    }

    private void OnCarouselLeft(InputAction.CallbackContext ctx) => _carousel?.Previous();
    private void OnCarouselRight(InputAction.CallbackContext ctx) => _carousel?.Next();

    public void Refresh()
    {
        if (!_built) BuildUI();

        RefreshWellbeing();
        RefreshPersonality();
        RefreshOutfit();
        RefreshFlower();
    }

    // ─────────────────── Wellbeing ───────────────────

    private void RefreshWellbeing()
    {
        if (_wellbeingText == null || NemaWellbeing.Instance == null) return;

        NemaWellbeing.Instance.Recalculate();
        var wb = NemaWellbeing.Instance;

        _wellbeingText.text = "";
        for (int i = 0; i < NemaWellbeing.PetalCount; i++)
        {
            string name = NemaWellbeing.PetalNames[i].ToLower().PadRight(11);
            float val = wb.GetPetal(i);
            _wellbeingText.text += $"{name} {Bar(val)}  {val:P0}\n";
        }

        if (_moodLabel != null)
        {
            string mood = wb.Overall >= 0.8f ? "thriving"
                : wb.Overall >= 0.6f ? "content"
                : wb.Overall >= 0.4f ? "okay"
                : wb.Overall >= 0.2f ? "struggling"
                : "wilting";
            _moodLabel.text = mood;
            _moodLabel.color = wb.Overall >= 0.6f
                ? new Color(0.7f, 0.85f, 0.65f)
                : wb.Overall >= 0.3f
                    ? new Color(0.85f, 0.8f, 0.6f)
                    : new Color(0.85f, 0.5f, 0.5f);
        }
    }

    // ─────────────────── Personality ───────────────────

    private void RefreshPersonality()
    {
        if (_personalityText == null) return;

        if (_personality != null)
        {
            _personalityText.text =
                $"warm        {Dots(_personality.GetTrait(0))}\n" +
                $"transparent {Dots(_personality.GetTrait(1))}\n" +
                $"playful     {Dots(_personality.GetTrait(2))}\n" +
                $"bold        {Dots(_personality.GetTrait(3))}\n" +
                $"romantic    {Dots(_personality.GetTrait(4))}\n\n" +
                $"<color=#666666>points remaining: {_personality.PointsRemaining}</color>";
        }
        else
        {
            _personalityText.text = "<color=#666666>no personality data</color>";
        }
    }

    // ─────────────────── Outfit ───────────────────

    private void RefreshOutfit()
    {
        if (_outfitText == null) return;

        var outfit = OutfitSelector.Instance != null ? OutfitSelector.Instance.SelectedOutfit : null;

        if (outfit != null)
        {
            string tags = outfit.styleTags != null && outfit.styleTags.Length > 0
                ? string.Join(", ", outfit.styleTags)
                : "none";
            _outfitText.text =
                $"<b>{outfit.outfitName}</b>\n\n" +
                $"{outfit.description}\n\n" +
                $"<color=#888888>style: {tags}</color>";
        }
        else
        {
            _outfitText.text = "<color=#666666>no outfit selected</color>";
        }
    }

    // ─────────────────── Flower ───────────────────

    private void RefreshFlower()
    {
        if (_flowerText == null) return;

        float overall = NemaWellbeing.Instance != null ? NemaWellbeing.Instance.Overall : 0f;

        // Visual representation of flower growth
        string flowerState;
        string commentary;

        if (overall >= 0.8f)
        {
            flowerState = "in full bloom";
            commentary = "everything feels right. she's flourishing.";
        }
        else if (overall >= 0.6f)
        {
            flowerState = "growing steadily";
            commentary = "she's doing well. the petals are opening up.";
        }
        else if (overall >= 0.4f)
        {
            flowerState = "a small bud";
            commentary = "there's potential here, but something's missing.";
        }
        else if (overall >= 0.2f)
        {
            flowerState = "a thin stem";
            commentary = "she needs more from you. pay attention.";
        }
        else
        {
            flowerState = "barely a sprout";
            commentary = "she's hurting. things need to change.";
        }

        _flowerText.text =
            $"<b>nema's flower</b>\n\n" +
            $"{flowerState}\n\n" +
            $"{Bar(overall)}  {overall:P0}";

        if (_flowerCommentary != null)
            _flowerCommentary.text = commentary;
    }

    // ─────────────────── Helpers ───────────────────

    private static string Bar(float value)
    {
        int filled = Mathf.RoundToInt(value * 10);
        return new string('\u2588', filled) + new string('\u2591', 10 - filled);
    }

    private static string Dots(int value)
    {
        return new string('\u25CF', value) + new string('\u25CB', 5 - value);
    }

    // ─────────────────── Build UI ───────────────────

    private void BuildUI()
    {
        _built = true;
        PauseUIHelper.EnsureFullStretch(gameObject);
        var theme = IrisTextTheme.Active;

        // Carousel container (takes full page minus margins for dots)
        var carouselGO = new GameObject("Carousel");
        carouselGO.transform.SetParent(transform, false);
        var carouselRT = carouselGO.AddComponent<RectTransform>();
        carouselRT.anchorMin = Vector2.zero;
        carouselRT.anchorMax = Vector2.one;
        carouselRT.offsetMin = new Vector2(10f, 0f);
        carouselRT.offsetMax = new Vector2(-10f, 0f);
        _carousel = carouselGO.AddComponent<PauseCarousel>();
        _carousel.OnSectionChanged += _ => Refresh();

        // Create section roots
        var sectionNames = new[] { "wellbeing", "personality", "outfit", "flower" };
        var sectionRoots = new GameObject[4];

        for (int i = 0; i < 4; i++)
        {
            var sectionGO = new GameObject($"Section_{sectionNames[i]}");
            sectionGO.transform.SetParent(carouselGO.transform, false);
            var sRT = sectionGO.AddComponent<RectTransform>();
            sRT.anchorMin = Vector2.zero;
            sRT.anchorMax = Vector2.one;
            sRT.offsetMin = new Vector2(20f, 40f);   // above dots
            sRT.offsetMax = new Vector2(-20f, -40f);  // below carousel title
            sectionRoots[i] = sectionGO;
        }

        // ── Section 0: Wellbeing ──
        var moodGO = new GameObject("MoodLabel");
        moodGO.transform.SetParent(sectionRoots[0].transform, false);
        var moodRT = moodGO.AddComponent<RectTransform>();
        moodRT.anchorMin = new Vector2(0f, 1f);
        moodRT.anchorMax = new Vector2(1f, 1f);
        moodRT.pivot = new Vector2(0.5f, 1f);
        moodRT.anchoredPosition = new Vector2(0f, 0f);
        moodRT.sizeDelta = new Vector2(0f, 40f);
        _moodLabel = moodGO.AddComponent<TextMeshProUGUI>();
        _moodLabel.fontSize = 28f;
        _moodLabel.fontStyle = FontStyles.Italic;
        _moodLabel.alignment = TextAlignmentOptions.Center;
        _moodLabel.raycastTarget = false;
        if (theme != null && theme.primaryFont != null) _moodLabel.font = theme.primaryFont;

        var wbGO = new GameObject("WellbeingBars");
        wbGO.transform.SetParent(sectionRoots[0].transform, false);
        var wbRT = wbGO.AddComponent<RectTransform>();
        wbRT.anchorMin = Vector2.zero;
        wbRT.anchorMax = Vector2.one;
        wbRT.offsetMin = new Vector2(20f, 20f);
        wbRT.offsetMax = new Vector2(-20f, -50f);
        _wellbeingText = wbGO.AddComponent<TextMeshProUGUI>();
        _wellbeingText.fontSize = 18f;
        _wellbeingText.color = new Color(0.85f, 0.82f, 0.78f);
        _wellbeingText.alignment = TextAlignmentOptions.TopLeft;
        _wellbeingText.raycastTarget = false;
        if (theme != null && theme.primaryFont != null) _wellbeingText.font = theme.primaryFont;

        // ── Section 1: Personality ──
        var pGO = new GameObject("PersonalityDots");
        pGO.transform.SetParent(sectionRoots[1].transform, false);
        var pRT = pGO.AddComponent<RectTransform>();
        pRT.anchorMin = Vector2.zero;
        pRT.anchorMax = Vector2.one;
        pRT.offsetMin = new Vector2(20f, 20f);
        pRT.offsetMax = new Vector2(-20f, -10f);
        _personalityText = pGO.AddComponent<TextMeshProUGUI>();
        _personalityText.fontSize = 18f;
        _personalityText.color = new Color(0.85f, 0.82f, 0.78f);
        _personalityText.alignment = TextAlignmentOptions.TopLeft;
        _personalityText.raycastTarget = false;
        if (theme != null && theme.primaryFont != null) _personalityText.font = theme.primaryFont;

        // ── Section 2: Outfit ──
        var oGO = new GameObject("OutfitInfo");
        oGO.transform.SetParent(sectionRoots[2].transform, false);
        var oRT = oGO.AddComponent<RectTransform>();
        oRT.anchorMin = Vector2.zero;
        oRT.anchorMax = Vector2.one;
        oRT.offsetMin = new Vector2(20f, 20f);
        oRT.offsetMax = new Vector2(-20f, -10f);
        _outfitText = oGO.AddComponent<TextMeshProUGUI>();
        _outfitText.fontSize = 18f;
        _outfitText.color = new Color(0.85f, 0.82f, 0.78f);
        _outfitText.alignment = TextAlignmentOptions.TopLeft;
        _outfitText.textWrappingMode = TextWrappingModes.Normal;
        _outfitText.raycastTarget = false;
        _outfitText.richText = true;
        if (theme != null && theme.primaryFont != null) _outfitText.font = theme.primaryFont;

        // ── Section 3: Flower ──
        var fGO = new GameObject("FlowerState");
        fGO.transform.SetParent(sectionRoots[3].transform, false);
        var fRT = fGO.AddComponent<RectTransform>();
        fRT.anchorMin = new Vector2(0f, 0.3f);
        fRT.anchorMax = new Vector2(1f, 1f);
        fRT.offsetMin = new Vector2(20f, 0f);
        fRT.offsetMax = new Vector2(-20f, -10f);
        _flowerText = fGO.AddComponent<TextMeshProUGUI>();
        _flowerText.fontSize = 20f;
        _flowerText.color = new Color(0.85f, 0.82f, 0.78f);
        _flowerText.alignment = TextAlignmentOptions.Center;
        _flowerText.textWrappingMode = TextWrappingModes.Normal;
        _flowerText.raycastTarget = false;
        _flowerText.richText = true;
        if (theme != null && theme.primaryFont != null) _flowerText.font = theme.primaryFont;

        var fcGO = new GameObject("FlowerCommentary");
        fcGO.transform.SetParent(sectionRoots[3].transform, false);
        var fcRT = fcGO.AddComponent<RectTransform>();
        fcRT.anchorMin = new Vector2(0f, 0f);
        fcRT.anchorMax = new Vector2(1f, 0.3f);
        fcRT.offsetMin = new Vector2(30f, 20f);
        fcRT.offsetMax = new Vector2(-30f, 0f);
        _flowerCommentary = fcGO.AddComponent<TextMeshProUGUI>();
        _flowerCommentary.fontSize = 15f;
        _flowerCommentary.fontStyle = FontStyles.Italic;
        _flowerCommentary.color = new Color(0.6f, 0.58f, 0.55f);
        _flowerCommentary.alignment = TextAlignmentOptions.Center;
        _flowerCommentary.textWrappingMode = TextWrappingModes.Normal;
        _flowerCommentary.raycastTarget = false;
        if (theme != null && theme.primaryFont != null) _flowerCommentary.font = theme.primaryFont;

        _carousel.Initialize(sectionNames, sectionRoots);
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pause menu page showing Nema's wellbeing flower and personality traits.
/// Reads from NemaWellbeing (5 petals) and NemaPersonality (5 traits).
/// Auto-builds a simple text summary if no custom UI is wired.
/// </summary>
public class PausePageNema : MonoBehaviour
{
    [Header("Optional Wired UI")]
    [SerializeField] private TMP_Text _wellbeingText;
    [SerializeField] private TMP_Text _personalityText;
    [SerializeField] private TMP_Text _moodLabel;

    private bool _built;

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (!_built) BuildUI();

        // Wellbeing
        if (_wellbeingText != null && NemaWellbeing.Instance != null)
        {
            NemaWellbeing.Instance.Recalculate();
            var wb = NemaWellbeing.Instance;
            _wellbeingText.text =
                $"comfort    {Bar(wb.GetPetal(0))}  {wb.GetPetal(0):P0}\n" +
                $"romance    {Bar(wb.GetPetal(1))}  {wb.GetPetal(1):P0}\n" +
                $"expression {Bar(wb.GetPetal(2))}  {wb.GetPetal(2):P0}\n" +
                $"nature     {Bar(wb.GetPetal(3))}  {wb.GetPetal(3):P0}\n" +
                $"social     {Bar(wb.GetPetal(4))}  {wb.GetPetal(4):P0}";

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

        // Personality
        if (_personalityText != null)
        {
            var np = NemaPersonality.Instance;
            if (np != null)
            {
                _personalityText.text =
                    $"warm        {Dots(np.GetTrait(0))}\n" +
                    $"transparent {Dots(np.GetTrait(1))}\n" +
                    $"playful     {Dots(np.GetTrait(2))}\n" +
                    $"bold        {Dots(np.GetTrait(3))}\n" +
                    $"romantic    {Dots(np.GetTrait(4))}\n\n" +
                    $"<color=#666666>points remaining: {np.PointsRemaining}</color>";
            }
            else
            {
                _personalityText.text = "<color=#666666>no personality data</color>";
            }
        }
    }

    private static string Bar(float value)
    {
        int filled = Mathf.RoundToInt(value * 10);
        return new string('\u2588', filled) + new string('\u2591', 10 - filled);
    }

    private static string Dots(int value)
    {
        return new string('\u25CF', value) + new string('\u25CB', 5 - value);
    }

    private void BuildUI()
    {
        _built = true;
        if (_wellbeingText != null) return; // already wired

        var theme = IrisTextTheme.Active;

        // Mood label at top
        var moodGO = new GameObject("MoodLabel");
        moodGO.transform.SetParent(transform, false);
        var moodRT = moodGO.AddComponent<RectTransform>();
        moodRT.anchorMin = new Vector2(0f, 1f);
        moodRT.anchorMax = new Vector2(1f, 1f);
        moodRT.pivot = new Vector2(0.5f, 1f);
        moodRT.anchoredPosition = new Vector2(0f, -10f);
        moodRT.sizeDelta = new Vector2(0f, 40f);
        _moodLabel = moodGO.AddComponent<TextMeshProUGUI>();
        _moodLabel.fontSize = 28f;
        _moodLabel.fontStyle = FontStyles.Italic;
        _moodLabel.alignment = TextAlignmentOptions.Center;
        _moodLabel.raycastTarget = false;
        if (theme != null && theme.primaryFont != null) _moodLabel.font = theme.primaryFont;

        // Wellbeing (left side)
        var wbGO = new GameObject("Wellbeing");
        wbGO.transform.SetParent(transform, false);
        var wbRT = wbGO.AddComponent<RectTransform>();
        wbRT.anchorMin = new Vector2(0f, 0f);
        wbRT.anchorMax = new Vector2(0.5f, 1f);
        wbRT.offsetMin = new Vector2(30f, 30f);
        wbRT.offsetMax = new Vector2(-10f, -60f);
        _wellbeingText = wbGO.AddComponent<TextMeshProUGUI>();
        _wellbeingText.fontSize = 18f;
        _wellbeingText.color = new Color(0.85f, 0.82f, 0.78f);
        _wellbeingText.alignment = TextAlignmentOptions.TopLeft;
        _wellbeingText.raycastTarget = false;
        if (theme != null && theme.primaryFont != null) _wellbeingText.font = theme.primaryFont;

        // Personality (right side)
        var pGO = new GameObject("Personality");
        pGO.transform.SetParent(transform, false);
        var pRT = pGO.AddComponent<RectTransform>();
        pRT.anchorMin = new Vector2(0.5f, 0f);
        pRT.anchorMax = new Vector2(1f, 1f);
        pRT.offsetMin = new Vector2(10f, 30f);
        pRT.offsetMax = new Vector2(-30f, -60f);
        _personalityText = pGO.AddComponent<TextMeshProUGUI>();
        _personalityText.fontSize = 18f;
        _personalityText.color = new Color(0.85f, 0.82f, 0.78f);
        _personalityText.alignment = TextAlignmentOptions.TopLeft;
        _personalityText.raycastTarget = false;
        if (theme != null && theme.primaryFont != null) _personalityText.font = theme.primaryFont;
    }
}

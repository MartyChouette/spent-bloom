using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pause menu System page with Options, Quit to Menu, and Quit to Desktop buttons.
/// Auto-builds buttons that route to PauseMenuController methods.
/// </summary>
public class PausePageSystem : MonoBehaviour
{
    private bool _built;

    private void OnEnable()
    {
        if (!_built) BuildUI();
    }

    private void BuildUI()
    {
        _built = true;

        var theme = IrisTextTheme.Active;
        float y = -30f;

        // Quit to Menu
        MakeButton("quit to menu", y, theme, () =>
        {
            var pause = GetComponentInParent<PauseMenuController>();
            if (pause != null)
            {
                string menuScene = "mainmenu_nemahead";
                if (UnityEngine.SceneManagement.SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/{menuScene}.unity") < 0)
                    menuScene = "mainmenu";
                pause.GoToMainMenu(menuScene);
            }
        });
        y -= 60f;

        // Quit to Desktop
        MakeButton("quit to desktop", y, theme, () =>
        {
            var pause = GetComponentInParent<PauseMenuController>();
            if (pause != null) pause.QuitGame();
        });
    }

    private void MakeButton(string label, float yPos, IrisTextTheme theme, UnityEngine.Events.UnityAction onClick)
    {
        var btnGO = new GameObject($"Btn_{label}");
        btnGO.transform.SetParent(transform, false);

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
    }
}

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Full-screen overlay for reading a letter or catalog.
/// Dark backdrop, sender name, text body, click to dismiss.
/// </summary>
public class MailTextOverlay : MonoBehaviour
{
    private CanvasGroup _cg;
    private Action _onDismiss;

    /// <summary>
    /// Show a letter overlay. Calls onDismiss when the player clicks to close.
    /// </summary>
    public static MailTextOverlay Show(string senderName, string[] lines, Action onDismiss)
    {
        var go = new GameObject("MailTextOverlay");
        var overlay = go.AddComponent<MailTextOverlay>();
        overlay.Build(senderName, lines, onDismiss);
        return overlay;
    }

    private void Build(string senderName, string[] lines, Action onDismiss)
    {
        _onDismiss = onDismiss;

        // Canvas
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 210;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        gameObject.AddComponent<GraphicRaycaster>();

        _cg = gameObject.AddComponent<CanvasGroup>();
        _cg.alpha = 0f;

        // Dark backdrop (click to dismiss)
        var bgGO = new GameObject("Backdrop");
        bgGO.transform.SetParent(transform, false);
        var bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.03f, 0.03f, 0.05f, 0.85f);

        var bgBtn = bgGO.AddComponent<Button>();
        bgBtn.targetGraphic = bgImg;
        var nav = bgBtn.navigation;
        nav.mode = Navigation.Mode.None;
        bgBtn.navigation = nav;
        bgBtn.onClick.AddListener(Dismiss);

        // Letter panel
        var panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(bgGO.transform, false);
        var panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(700f, 500f);

        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.07f, 0.09f, 0.95f);
        panelImg.raycastTarget = false;

        var theme = IrisTextTheme.Active;

        // Sender name
        var senderGO = new GameObject("Sender");
        senderGO.transform.SetParent(panelGO.transform, false);
        var senderRT = senderGO.AddComponent<RectTransform>();
        senderRT.anchorMin = new Vector2(0f, 1f);
        senderRT.anchorMax = new Vector2(1f, 1f);
        senderRT.pivot = new Vector2(0.5f, 1f);
        senderRT.anchoredPosition = new Vector2(0f, -20f);
        senderRT.sizeDelta = new Vector2(-60f, 40f);

        var senderTMP = senderGO.AddComponent<TextMeshProUGUI>();
        senderTMP.text = senderName;
        senderTMP.fontSize = 22f;
        senderTMP.fontStyle = FontStyles.Italic;
        senderTMP.color = new Color(0.7f, 0.65f, 0.6f);
        senderTMP.alignment = TextAlignmentOptions.Left;
        senderTMP.raycastTarget = false;
        if (theme != null && theme.primaryFont != null) senderTMP.font = theme.primaryFont;

        // Body text
        var bodyGO = new GameObject("Body");
        bodyGO.transform.SetParent(panelGO.transform, false);
        var bodyRT = bodyGO.AddComponent<RectTransform>();
        bodyRT.anchorMin = Vector2.zero;
        bodyRT.anchorMax = Vector2.one;
        bodyRT.offsetMin = new Vector2(30f, 40f);
        bodyRT.offsetMax = new Vector2(-30f, -70f);

        var bodyTMP = bodyGO.AddComponent<TextMeshProUGUI>();
        bodyTMP.text = lines != null ? string.Join("\n\n", lines) : "";
        bodyTMP.fontSize = 20f;
        bodyTMP.color = new Color(0.85f, 0.82f, 0.78f);
        bodyTMP.alignment = TextAlignmentOptions.TopLeft;
        bodyTMP.textWrappingMode = TextWrappingModes.Normal;
        bodyTMP.raycastTarget = false;
        if (theme != null && theme.primaryFont != null) bodyTMP.font = theme.primaryFont;

        // Dismiss hint
        var hintGO = new GameObject("Hint");
        hintGO.transform.SetParent(panelGO.transform, false);
        var hintRT = hintGO.AddComponent<RectTransform>();
        hintRT.anchorMin = new Vector2(0f, 0f);
        hintRT.anchorMax = new Vector2(1f, 0f);
        hintRT.pivot = new Vector2(0.5f, 0f);
        hintRT.anchoredPosition = new Vector2(0f, 10f);
        hintRT.sizeDelta = new Vector2(-60f, 25f);

        var hintTMP = hintGO.AddComponent<TextMeshProUGUI>();
        hintTMP.text = "click to close";
        hintTMP.fontSize = 14f;
        hintTMP.fontStyle = FontStyles.Italic;
        hintTMP.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        hintTMP.alignment = TextAlignmentOptions.Center;
        hintTMP.raycastTarget = false;
        if (theme != null && theme.primaryFont != null) hintTMP.font = theme.primaryFont;

        // Fade in
        StartCoroutine(FadeIn());
    }

    private void Dismiss()
    {
        StartCoroutine(FadeOutAndDestroy());
    }

    private IEnumerator FadeIn()
    {
        float duration = 0.3f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _cg.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        _cg.alpha = 1f;
    }

    private IEnumerator FadeOutAndDestroy()
    {
        float duration = 0.2f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _cg.alpha = 1f - Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        _onDismiss?.Invoke();
        Destroy(gameObject);
    }
}

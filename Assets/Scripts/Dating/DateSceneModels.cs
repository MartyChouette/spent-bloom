using UnityEngine;

/// <summary>
/// Bridge between a DatePersonalDefinition (ScriptableObject) and scene-placed
/// per-phase character models. Place one of these in the scene for each date
/// character, drag the SO into <see cref="definition"/>, and wire the three
/// phase models. DateSessionManager auto-finds the matching set at runtime.
/// </summary>
public class DateSceneModels : MonoBehaviour
{
    [Tooltip("Which date character these models belong to.")]
    public DatePersonalDefinition definition;

    [Header("Per-Phase Models")]
    [Tooltip("Phase 1 — Arrival (e.g. standing greeting pose).")]
    public GameObject arrivalModel;

    [Tooltip("Phase 2 — Kitchen / Drinks (e.g. standing idle pose).")]
    public GameObject kitchenModel;

    [Tooltip("Phase 3 — Couch / Reveal (e.g. sitting pose).")]
    public GameObject couchModel;

    // ── Static registry (avoids FindObjectsByType) ──
    private static readonly System.Collections.Generic.List<DateSceneModels> s_all = new();
    public static System.Collections.Generic.IReadOnlyList<DateSceneModels> All => s_all;

    private Material _rimLightMat;

    private void OnEnable() => s_all.Add(this);
    private void OnDisable() => s_all.Remove(this);

    private void Awake()
    {
        // Ensure all models start hidden
        if (arrivalModel != null) arrivalModel.SetActive(false);
        if (kitchenModel != null) kitchenModel.SetActive(false);
        if (couchModel   != null) couchModel.SetActive(false);

        BuildRimLightMaterial();
    }

    /// <summary>Find the scene models for a given date definition. Returns null if not found.</summary>
    public static DateSceneModels FindForDate(DatePersonalDefinition date)
    {
        if (date == null) return null;
        for (int i = 0; i < s_all.Count; i++)
        {
            if (s_all[i].definition == date)
                return s_all[i];
        }
        return null;
    }

    /// <summary>Deactivate all models on this set and activate the target. Pass null to hide all.</summary>
    public void ShowOnly(GameObject target)
    {
        if (arrivalModel != null && arrivalModel != target) arrivalModel.SetActive(false);
        if (kitchenModel != null && kitchenModel != target) kitchenModel.SetActive(false);
        if (couchModel   != null && couchModel   != target) couchModel.SetActive(false);

        if (target != null)
        {
            target.SetActive(true);

            // Force animator to snap to its default state immediately so the
            // first visible frame shows the correct pose (no T-pose flash).
            var anim = target.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.Update(0f);
                anim.Update(0f); // double update ensures state machine + blend tree settle
            }

            // Models inactive during PSXRenderController's startup scan still
            // have URP/Standard shaders — swap to PSXLit to avoid stripped-shader
            // invisibility in builds.
            if (PSXRenderController.Instance != null)
                PSXRenderController.Instance.EnsureSwapped(target);

            // Append rim light pass to all renderers
            ApplyRimLight(target);
        }
    }

    /// <summary>Hide all models in this set.</summary>
    public void HideAll()
    {
        ShowOnly(null);
    }

    // ── Rim light ──────────────────────────────────────────────────

    private void BuildRimLightMaterial()
    {
        // Match Nema's rim light settings so all characters look consistent.
        // NemaController owns the canonical values; read them if available,
        // otherwise fall back to the same defaults.
        var shader = Shader.Find("Iris/RimLight");
        if (shader == null) return;

        _rimLightMat = new Material(shader);

        var nema = NemaController.Instance;
        if (nema != null)
        {
            _rimLightMat.SetColor("_RimColor", nema.RimLightColor);
            _rimLightMat.SetFloat("_RimPower", nema.RimLightPower);
            _rimLightMat.SetFloat("_RimIntensity", nema.RimLightIntensity);
        }
        else
        {
            _rimLightMat.SetColor("_RimColor", new Color(1f, 0.95f, 0.9f, 0.3f));
            _rimLightMat.SetFloat("_RimPower", 2.5f);
            _rimLightMat.SetFloat("_RimIntensity", 0.6f);
        }
    }

    private void ApplyRimLight(GameObject model)
    {
        if (_rimLightMat == null || model == null) return;

        foreach (var rend in model.GetComponentsInChildren<Renderer>(true))
        {
            if (rend is ParticleSystemRenderer || rend is LineRenderer) continue;

            var existing = rend.sharedMaterials;
            for (int i = 0; i < existing.Length; i++)
                if (existing[i] == _rimLightMat) goto skip;

            var combined = new Material[existing.Length + 1];
            existing.CopyTo(combined, 0);
            combined[existing.Length] = _rimLightMat;
            rend.sharedMaterials = combined;

            skip:;
        }
    }

    private void OnDestroy()
    {
        if (_rimLightMat != null)
            Destroy(_rimLightMat);
    }
}

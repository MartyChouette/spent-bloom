using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Light type for date preference matching.
/// </summary>
public enum SwitchLightType
{
    Lamp,       // standard apartment lights (overhead, floor lamp, desk lamp)
    Candle      // candle-only lighting
}

/// <summary>
/// Snapshot of the apartment's current lighting state.
/// </summary>
public struct LightingState
{
    public int lampsOn;
    public int candlesOn;
    public int totalOn;
    public int totalSwitches;

    /// <summary>True if no lights of any type are on.</summary>
    public bool IsDark => totalOn == 0;

    /// <summary>True if only candles are lit (no lamps).</summary>
    public bool IsCandleOnly => candlesOn > 0 && lampsOn == 0;

    /// <summary>True if any lamps are on.</summary>
    public bool HasLamps => lampsOn > 0;
}

/// <summary>
/// Clickable light switch that toggles one or more lights on/off.
/// Placed on the switch mesh (must be on Placeables layer for ObjectGrabber click detection).
/// ObjectGrabber routes clicks here when no PlaceableObject or DrawerController is found.
/// </summary>
public class LightSwitch : MonoBehaviour
{
    // ── Static registry ───────────────
    private static readonly List<LightSwitch> s_all = new();
    public static IReadOnlyList<LightSwitch> All => s_all;

    [Header("Lights")]
    [Tooltip("Lights controlled by this switch.")]
    [SerializeField] private Light[] _lights;

    [Header("Type")]
    [Tooltip("What kind of light this switch controls. Used by date lighting preferences.")]
    [SerializeField] private SwitchLightType _lightType = SwitchLightType.Lamp;

    [Header("State")]
    [Tooltip("Whether the light starts on. False = room starts dark, player must click switch.")]
    [SerializeField] private bool _startsOn = false;

    [Header("Visual")]
    [Tooltip("Renderer for the switch plate (optional — tints to show on/off state).")]
    [SerializeField] private Renderer _switchRenderer;

    [Tooltip("Color when on.")]
    [SerializeField] private Color _onColor = new Color(0.95f, 0.9f, 0.7f);

    [Tooltip("Color when off.")]
    [SerializeField] private Color _offColor = new Color(0.3f, 0.3f, 0.3f);

    [Header("Audio")]
    [Tooltip("SFX played when toggled.")]
    [SerializeField] private AudioClip _toggleSFX;

    private Material _instanceMat;
    private bool _isOn;

    public bool IsOn => _isOn;
    public SwitchLightType LightType => _lightType;

    private void Awake()
    {
        _isOn = _startsOn;

        if (_switchRenderer != null && _switchRenderer.sharedMaterial != null)
        {
            _instanceMat = new Material(_switchRenderer.sharedMaterial);
            _switchRenderer.material = _instanceMat;
        }

        ApplyState();
    }

    private void OnEnable() => s_all.Add(this);
    private void OnDisable() => s_all.Remove(this);

    /// <summary>Toggle the light on/off. Called by ObjectGrabber on click.</summary>
    public void Toggle()
    {
        _isOn = !_isOn;
        ApplyState();

        AudioManager.Instance?.PlaySFX(_toggleSFX);
        Debug.Log($"[LightSwitch] {name} ({_lightType}) toggled {(_isOn ? "ON" : "OFF")}.");
    }

    private void ApplyState()
    {
        if (_lights != null)
        {
            for (int i = 0; i < _lights.Length; i++)
            {
                if (_lights[i] != null)
                    _lights[i].enabled = _isOn;
            }
        }

        if (_instanceMat != null)
            _instanceMat.color = _isOn ? _onColor : _offColor;
    }

    private void OnDestroy()
    {
        if (_instanceMat != null)
            Destroy(_instanceMat);
    }

    // ── Static query ───────────────

    /// <summary>Query the current apartment lighting state across all registered switches.</summary>
    public static LightingState GetCurrentLighting()
    {
        var state = new LightingState();
        for (int i = 0; i < s_all.Count; i++)
        {
            var sw = s_all[i];
            state.totalSwitches++;
            if (!sw._isOn) continue;

            state.totalOn++;
            switch (sw._lightType)
            {
                case SwitchLightType.Lamp:   state.lampsOn++;   break;
                case SwitchLightType.Candle: state.candlesOn++; break;
            }
        }
        return state;
    }
}

using UnityEngine;

/// <summary>
/// Controls a drape mesh's wind animation via the Iris/Drape shader.
/// Pair with a window toggle — call SetOpen/SetClosed to start/stop the wind.
/// The wind strength lerps smoothly so drapes settle gradually when the window closes.
/// </summary>
public class DrapeController : MonoBehaviour
{
    [Header("Renderers")]
    [Tooltip("Drape renderers using the Iris/Drape shader. Auto-detected from children if empty.")]
    [SerializeField] private Renderer[] _renderers;

    [Header("Wind")]
    [Tooltip("Target wind strength when window is open (0-1).")]
    [SerializeField, Range(0f, 1f)] private float _openWindStrength = 0.8f;

    [Tooltip("How fast the wind builds up when the window opens (seconds to reach full).")]
    [SerializeField] private float _windBuildUp = 1.5f;

    [Tooltip("How fast the wind dies down when the window closes (seconds to reach zero).")]
    [SerializeField] private float _windDieDown = 2.5f;

    [Header("State")]
    [Tooltip("Whether the window starts open.")]
    [SerializeField] private bool _startsOpen = false;

    private MaterialPropertyBlock _mpb;
    private static readonly int WindStrengthID = Shader.PropertyToID("_WindStrength");

    private bool _isOpen;
    private float _currentWind;
    private float _targetWind;

    public bool IsOpen => _isOpen;

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();

        if (_renderers == null || _renderers.Length == 0)
            _renderers = GetComponentsInChildren<Renderer>();

        _isOpen = _startsOpen;
        _targetWind = _isOpen ? _openWindStrength : 0f;
        _currentWind = _targetWind; // snap to initial state
        ApplyWind();
    }

    private void Update()
    {
        if (Mathf.Approximately(_currentWind, _targetWind)) return;

        float speed = _currentWind < _targetWind
            ? 1f / Mathf.Max(_windBuildUp, 0.01f)
            : 1f / Mathf.Max(_windDieDown, 0.01f);

        _currentWind = Mathf.MoveTowards(_currentWind, _targetWind, speed * Time.deltaTime);
        ApplyWind();
    }

    /// <summary>Open the window — wind starts blowing the drapes.</summary>
    public void SetOpen()
    {
        _isOpen = true;
        _targetWind = _openWindStrength;
    }

    /// <summary>Close the window — drapes gradually settle.</summary>
    public void SetClosed()
    {
        _isOpen = false;
        _targetWind = 0f;
    }

    /// <summary>Toggle open/closed.</summary>
    public void Toggle()
    {
        if (_isOpen) SetClosed();
        else SetOpen();
    }

    private void ApplyWind()
    {
        if (_renderers == null) return;
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null) continue;
            _renderers[i].GetPropertyBlock(_mpb);
            _mpb.SetFloat(WindStrengthID, _currentWind);
            _renderers[i].SetPropertyBlock(_mpb);
        }
    }
}

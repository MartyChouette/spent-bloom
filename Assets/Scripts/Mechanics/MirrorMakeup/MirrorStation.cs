using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Manages entering and exiting the mirror close-up view.
/// Click the mirror in the apartment to enter. ESC or a back button to exit.
/// While active, raises a dedicated CinemachineCamera and shows the mirror UI
/// (makeup tools, outfit selection, pimple coverage).
/// Follows the same camera-priority pattern as NewspaperManager.
/// </summary>
public class MirrorStation : MonoBehaviour
{
    public static MirrorStation Instance { get; private set; }

    [Header("Camera")]
    [Tooltip("CinemachineCamera aimed at the mirror / Nema's face. Raised to priority 30 when active.")]
    [SerializeField] private CinemachineCamera _mirrorCamera;

    [Header("References")]
    [Tooltip("MirrorMakeupManager to enable/disable.")]
    [SerializeField] private MirrorMakeupManager _makeupManager;

    [Tooltip("FaceCanvas for wellbeing-driven pimple generation.")]
    [SerializeField] private FaceCanvas _faceCanvas;

    [Tooltip("OutfitSelector to show during mirror use.")]
    [SerializeField] private OutfitSelector _outfitSelector;

    [Tooltip("Root GameObject of the mirror scene objects (face, overlay, tools). Toggled on enter/exit.")]
    [SerializeField] private GameObject _mirrorRoot;

    [Header("Audio")]
    [SerializeField] private AudioClip _openSFX;
    [SerializeField] private AudioClip _closeSFX;

    private bool _isActive;
    private const int PriorityActive = 30;
    private const int PriorityInactive = 0;

    public bool IsActive => _isActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        // Start hidden
        if (_mirrorCamera != null)
            _mirrorCamera.Priority = PriorityInactive;
        if (_mirrorRoot != null)
            _mirrorRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!_isActive) return;

        // ESC to exit
        if (Input.GetKeyDown(KeyCode.Escape) ||
            (IrisInput.Instance != null && IrisInput.Instance.Pause.WasPressedThisFrame()))
        {
            ExitMirror();
        }
    }

    /// <summary>
    /// Enter the mirror close-up. Called by ObjectGrabber when the player clicks the mirror.
    /// Only works during Exploration phase (not during dates).
    /// </summary>
    public void EnterMirror()
    {
        if (_isActive) return;

        // Only during exploration
        if (DayPhaseManager.Instance != null &&
            DayPhaseManager.Instance.CurrentPhase != DayPhaseManager.DayPhase.Exploration)
            return;

        _isActive = true;

        // Update pimples based on current wellbeing
        if (_faceCanvas != null && NemaWellbeing.Instance != null)
        {
            NemaWellbeing.Instance.Recalculate();
            _faceCanvas.SetPimpleCountFromWellbeing(NemaWellbeing.Instance.Overall);
            _faceCanvas.Regenerate();
        }

        // Show mirror scene objects
        if (_mirrorRoot != null)
            _mirrorRoot.SetActive(true);

        // Raise camera
        if (_mirrorCamera != null)
            _mirrorCamera.Priority = PriorityActive;

        // Enable makeup interaction
        if (_makeupManager != null)
            _makeupManager.enabled = true;

        // Disable apartment grabber while in mirror
        if (ObjectGrabber.Instance != null)
            ObjectGrabber.Instance.SetEnabled(false);

        if (_openSFX != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(_openSFX);

        Debug.Log($"[MirrorStation] Entered mirror. Wellbeing={NemaWellbeing.Instance?.Overall:F2}, Pimples={_faceCanvas?.TotalPimpleCount}");
    }

    /// <summary>Exit the mirror and return to apartment view.</summary>
    public void ExitMirror()
    {
        if (!_isActive) return;
        _isActive = false;

        // Lower camera
        if (_mirrorCamera != null)
            _mirrorCamera.Priority = PriorityInactive;

        // Disable makeup interaction
        if (_makeupManager != null)
            _makeupManager.enabled = false;

        // Hide mirror scene objects
        if (_mirrorRoot != null)
            _mirrorRoot.SetActive(false);

        // Re-enable apartment grabber
        if (ObjectGrabber.Instance != null)
            ObjectGrabber.Instance.SetEnabled(true);

        if (_closeSFX != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(_closeSFX);

        Debug.Log("[MirrorStation] Exited mirror.");
    }
}

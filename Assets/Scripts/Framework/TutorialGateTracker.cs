using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Tracks apartment activity milestones during the tutorial period (day 1).
/// The tutorial gate opens when the player completes any 2 unique milestones,
/// but tracking continues after the gate passes for scoring and other systems.
/// </summary>
public class TutorialGateTracker : MonoBehaviour
{
    public static TutorialGateTracker Instance { get; private set; }

    public enum MilestoneType
    {
        ShoePaired,
        ShoesRacked,
        BookCollectionDone,
        GunplaDone,
        StainCleaned,
        TrashBinned
    }

    [Header("Gate")]
    [Tooltip("How many unique milestones the player must complete before the gate opens.")]
    [SerializeField] private int _requiredMilestones = 2;

    /// <summary>Fired when a new unique milestone type is completed. Arg = type.</summary>
    public UnityEvent<MilestoneType> OnMilestoneCompleted;

    /// <summary>Fired once when enough unique milestones have been completed.</summary>
    public UnityEvent OnGatePassed;

    private readonly HashSet<MilestoneType> _completedTypes = new();
    private readonly Dictionary<MilestoneType, int> _counts = new();
    private bool _gatePassed;

    /// <summary>True once the required number of unique milestones have been completed.</summary>
    public bool GatePassed => _gatePassed;

    /// <summary>Number of unique milestone types completed so far.</summary>
    public int UniqueMilestonesCompleted => _completedTypes.Count;

    /// <summary>Check if a specific milestone type has been completed at least once.</summary>
    public bool HasCompleted(MilestoneType type) => _completedTypes.Contains(type);

    /// <summary>Get the total count for a specific milestone type.</summary>
    public int GetCount(MilestoneType type) => _counts.TryGetValue(type, out int c) ? c : 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[TutorialGateTracker] Duplicate instance destroyed.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Record a milestone. Call this from any system when a milestone-worthy
    /// action occurs (shoe paired, trash binned, etc.).
    /// </summary>
    public void RecordMilestone(MilestoneType type)
    {
        // Increment count
        if (!_counts.ContainsKey(type))
            _counts[type] = 0;
        _counts[type]++;

        // Track unique completion
        bool isNew = _completedTypes.Add(type);
        if (isNew)
        {
            Debug.Log($"[TutorialGateTracker] New milestone: {type} ({_completedTypes.Count}/{_requiredMilestones})");
            OnMilestoneCompleted?.Invoke(type);
        }

        // Check gate
        if (!_gatePassed && _completedTypes.Count >= _requiredMilestones)
        {
            _gatePassed = true;
            Debug.Log("[TutorialGateTracker] Gate passed!");
            OnGatePassed?.Invoke();
        }
    }
}

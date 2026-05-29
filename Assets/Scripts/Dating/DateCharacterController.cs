using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Controls a date NPC. Stationary per-phase models toggled by DateSessionManager.
/// Evaluates nearby ReactableTags via seated excursions (no walking).
/// During investigation, highlights the target item and emits ambient particles
/// indicating the date's sentiment before the verdict.
/// </summary>
public class DateCharacterController : MonoBehaviour
{
    public enum CharState { Idle, Sitting, Investigating, Dismissed }

    // ──────────────────────────────────────────────────────────────
    // Configuration
    // ──────────────────────────────────────────────────────────────
    [Header("Investigation")]
    [Tooltip("Seconds spent investigating an object before returning to sitting.")]
    [SerializeField] private float investigateDuration = 3f;

    [Header("Excursions")]
    [Tooltip("Seconds sitting before considering an excursion.")]
    [SerializeField] private float excursionInterval = 4f;

    [Tooltip("Chance of investigating each excursion check (0-1).")]
    [Range(0f, 1f)]
    [SerializeField] private float excursionChance = 0.9f;

    [Header("Audio")]
    [Tooltip("SFX played when the NPC starts investigating an item.")]
    [SerializeField] private AudioClip investigateSFX;

    [Tooltip("SFX played when the NPC reacts to an item.")]
    [SerializeField] private AudioClip reactionSFX;

    [Tooltip("Ambient hum while considering an item (loops quietly during investigation).")]
    [SerializeField] private AudioClip _thinkingSFX;

    [Header("Ambient Particles")]
    [Tooltip("Emit gentle ambient particles near the investigated item during the thinking phase.")]
    [SerializeField] private bool _emitAmbientParticles = true;

    [Tooltip("Particle rate during investigation (particles per second).")]
    [SerializeField] private float _ambientParticleRate = 4f;

    // ──────────────────────────────────────────────────────────────
    // Events
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Fired when the character reacts to a ReactableTag. DateSessionManager subscribes.
    /// Args: (tag, reactionType, displayName)
    /// </summary>
    public event Action<ReactableTag, ReactionType, string> OnReaction;

    // ──────────────────────────────────────────────────────────────
    // Runtime state
    // ──────────────────────────────────────────────────────────────
    private NavMeshAgent _agent;
    private CharState _state = CharState.Idle;
    private ReactableTag _currentTarget;
    private float _sitTimer;
    private float _investigateTimer;
    private bool _excursionsEnabled;

    // Investigation visuals
    private InteractableHighlight _currentHighlight;
    private ReactionType _pendingReaction;
    private bool _reactionEvaluated;
    private ParticleSystem _ambientPS;
    private GameObject _ambientPSGO;

    public CharState CurrentState => _state;
    public ReactableTag CurrentTarget => _currentTarget;

    // ──────────────────────────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────────────────────────

    /// <summary>Initialize the character at a position.</summary>
    public void Initialize(Vector3 position)
    {
        _agent = GetComponent<NavMeshAgent>();
        if (_agent != null)
        {
            _agent.enabled = false;
            transform.position = position;
            _agent.enabled = true;

            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                _agent.Warp(hit.position);
        }
        else
        {
            transform.position = position;
        }

        _state = CharState.Idle;
        Debug.Log($"[DateCharacterController] Initialized at {position}");
    }

    /// <summary>Teleport the NPC to a new position.</summary>
    public void WarpTo(Vector3 position)
    {
        if (_agent == null) _agent = GetComponent<NavMeshAgent>();

        if (_agent != null)
        {
            _agent.enabled = false;
            transform.position = position;
            _agent.enabled = true;

            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                _agent.Warp(hit.position);
        }
        else
        {
            transform.position = position;
        }

        ClearInvestigationVisuals();
        _currentTarget = null;
        Debug.Log($"[DateCharacterController] Warped to {position}");
    }

    /// <summary>Set the NPC to sitting/idle state.</summary>
    public void SetSitting()
    {
        _state = CharState.Sitting;
        _sitTimer = 0f;
        ClearInvestigationVisuals();
        _currentTarget = null;
        if (_agent != null && _agent.isOnNavMesh)
            _agent.ResetPath();
    }

    /// <summary>Allow the character to start evaluating ReactableTags from their seat.</summary>
    public void EnableExcursions()
    {
        _excursionsEnabled = true;
        _sitTimer = 0f;
        Debug.Log("[DateCharacterController] Excursions enabled.");
    }

    /// <summary>Stop the character from starting new excursions.</summary>
    public void DisableExcursions()
    {
        _excursionsEnabled = false;
        ClearInvestigationVisuals();
        Debug.Log("[DateCharacterController] Excursions disabled.");
    }

    /// <summary>Force the character to investigate a specific target (e.g. drink delivery).</summary>
    public void InvestigateSpecific(Transform target)
    {
        if (_state == CharState.Dismissed) return;

        ClearInvestigationVisuals();
        _currentTarget = target.GetComponent<ReactableTag>();
        _state = CharState.Investigating;
        _investigateTimer = 0f;
        _reactionEvaluated = false;

        if (_currentTarget != null)
            BeginInvestigationVisuals();

        if (investigateSFX != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(investigateSFX);
    }

    /// <summary>Mark the character as dismissed.</summary>
    public void Dismiss()
    {
        _state = CharState.Dismissed;
        _excursionsEnabled = false;
        ClearInvestigationVisuals();
        _currentTarget = null;
        Debug.Log("[DateCharacterController] Dismissed.");
    }

    // ──────────────────────────────────────────────────────────────
    // Update
    // ──────────────────────────────────────────────────────────────

    private void Update()
    {
        switch (_state)
        {
            case CharState.Sitting:
                if (!_excursionsEnabled) break;
                _sitTimer += Time.deltaTime;
                if (_sitTimer >= excursionInterval)
                {
                    _sitTimer = 0f;
                    TryExcursion();
                }
                break;

            case CharState.Investigating:
                _investigateTimer += Time.deltaTime;

                // Pre-evaluate early to know which ambient particles to show
                if (!_reactionEvaluated && _investigateTimer >= 0.5f)
                    PreEvaluate();

                // Opinion phase — fire the actual reaction event
                if (_investigateTimer >= 2.0f && _investigateTimer < 2.0f + Time.deltaTime)
                    EvaluateCurrentTarget();

                // Done investigating — return to sitting
                if (_investigateTimer >= investigateDuration)
                {
                    ClearInvestigationVisuals();
                    _currentTarget = null;
                    _state = CharState.Sitting;
                    _sitTimer = 0f;
                }
                break;
        }
    }

    private void OnDisable()
    {
        ClearInvestigationVisuals();
    }

    // ──────────────────────────────────────────────────────────────
    // Internal
    // ──────────────────────────────────────────────────────────────

    private void TryExcursion()
    {
        if (UnityEngine.Random.value > excursionChance) return;

        // Find a random active, non-private ReactableTag
        var candidates = new List<ReactableTag>();
        foreach (var tag in ReactableTag.All)
        {
            if (!tag.IsActive) continue;
            if (tag.IsPrivate) continue;
            candidates.Add(tag);
        }

        if (candidates.Count == 0) return;

        ClearInvestigationVisuals();
        _currentTarget = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        _state = CharState.Investigating;
        _investigateTimer = 0f;
        _reactionEvaluated = false;

        BeginInvestigationVisuals();

        if (investigateSFX != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(investigateSFX);

        Debug.Log($"[DateCharacterController] Evaluating {_currentTarget.gameObject.name}");
    }

    /// <summary>Pre-evaluate to determine ambient particle color before the verdict fires.</summary>
    private void PreEvaluate()
    {
        _reactionEvaluated = true;

        if (_currentTarget == null) return;
        var dateSession = DateSessionManager.Instance;
        if (dateSession == null || dateSession.CurrentDate == null) return;

        _pendingReaction = ReactionEvaluator.EvaluateReactable(_currentTarget, dateSession.CurrentDate.preferences);

        // Start ambient particles now that we know the sentiment
        if (_emitAmbientParticles && _currentTarget != null)
            StartAmbientParticles(_currentTarget.transform.position, _pendingReaction);
    }

    private void EvaluateCurrentTarget()
    {
        if (_currentTarget == null) return;

        var dateSession = DateSessionManager.Instance;
        if (dateSession == null || dateSession.CurrentDate == null) return;

        var reaction = _reactionEvaluated
            ? _pendingReaction
            : ReactionEvaluator.EvaluateReactable(_currentTarget, dateSession.CurrentDate.preferences);

        if (reactionSFX != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(reactionSFX);

        // Burst particles at the item on verdict
        DateSessionManager.SpawnReactionParticles(
            _currentTarget.transform.position + Vector3.up * 0.15f, reaction);

        string itemDisplayName = _currentTarget.DisplayName;
        OnReaction?.Invoke(_currentTarget, reaction, itemDisplayName);

        Debug.Log($"[DateCharacterController] Reacted to {itemDisplayName}: {reaction}");
    }

    // ──────────────────────────────────────────────────────────────
    // Investigation Visuals
    // ──────────────────────────────────────────────────────────────

    private void BeginInvestigationVisuals()
    {
        if (_currentTarget == null) return;

        // Highlight the item being looked at
        _currentHighlight = _currentTarget.GetComponent<InteractableHighlight>();
        if (_currentHighlight != null)
            _currentHighlight.SetGazeHighlighted(true);
    }

    private void ClearInvestigationVisuals()
    {
        // Remove highlight
        if (_currentHighlight != null)
        {
            _currentHighlight.SetGazeHighlighted(false);
            _currentHighlight = null;
        }

        // Stop ambient particles
        StopAmbientParticles();
    }

    private void StartAmbientParticles(Vector3 position, ReactionType reaction)
    {
        StopAmbientParticles();

        _ambientPSGO = new GameObject("AmbientReactionPS");
        _ambientPSGO.transform.position = position + Vector3.up * 0.2f;

        _ambientPS = _ambientPSGO.AddComponent<ParticleSystem>();
        _ambientPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _ambientPS.Clear(true);

        var main = _ambientPS.main;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = true;
        main.duration = 5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
        main.gravityModifier = -0.15f; // float upward gently
        main.maxParticles = 20;

        // Color by sentiment
        Color particleColor = reaction switch
        {
            ReactionType.Like => new Color(1f, 0.7f, 0.8f, 0.7f),     // soft pink
            ReactionType.Dislike => new Color(0.5f, 0.45f, 0.6f, 0.5f), // muted purple
            _ => new Color(0.8f, 0.8f, 0.75f, 0.4f)                    // faint warm grey
        };
        main.startColor = particleColor;

        var emission = _ambientPS.emission;
        emission.rateOverTime = _ambientParticleRate;

        var shape = _ambientPS.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.12f;

        var sizeOverLifetime = _ambientPS.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
            AnimationCurve.EaseInOut(0f, 0.5f, 1f, 0f));

        _ambientPS.Play();
    }

    private void StopAmbientParticles()
    {
        if (_ambientPS != null)
        {
            _ambientPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        if (_ambientPSGO != null)
        {
            Destroy(_ambientPSGO);
            _ambientPSGO = null;
        }
        _ambientPS = null;
    }
}

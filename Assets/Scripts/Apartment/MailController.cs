using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scene singleton that delivers mail at a fixed hour each day during Exploration.
/// Items slide under the door with a poof + spring animation.
/// Place in the apartment scene with _doorSlidePoint set to the floor near the entrance.
/// </summary>
public class MailController : MonoBehaviour
{
    public static MailController Instance { get; private set; }

    [Header("Delivery Point")]
    [Tooltip("Where mail items appear (floor near the door).")]
    [SerializeField] private Transform _doorSlidePoint;

    [Tooltip("Random offset range for multiple items so they don't stack.")]
    [SerializeField] private float _scatterRadius = 0.15f;

    [Header("Schedule")]
    [Tooltip("Scheduled deliveries by day. One SO per day.")]
    [SerializeField] private MailDeliveryDefinition[] _schedule;

    [Tooltip("Fallback delivery for days with no scheduled mail (junk mail, etc).")]
    [SerializeField] private MailDeliveryDefinition _fallback;

    [Tooltip("Game hour when mail arrives (e.g. 10 = 10am).")]
    [SerializeField] private float _deliveryHour = 10f;

    [Header("Prefabs")]
    [Tooltip("Default envelope/letter mesh for Letter and Catalog items.")]
    [SerializeField] private GameObject _letterPrefab;

    [Tooltip("Default box mesh for Package items.")]
    [SerializeField] private GameObject _packagePrefab;

    [Header("Audio")]
    [Tooltip("SFX played when mail arrives (poof/slide).")]
    [SerializeField] private AudioClip _deliverySFX;

    [Tooltip("SFX played when opening a letter or package.")]
    [SerializeField] private AudioClip _openSFX;
    public AudioClip OpenSFX => _openSFX;

    private bool _deliveredToday;
    private readonly List<GameObject> _activeMailObjects = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (DayManager.Instance != null)
            DayManager.Instance.OnDayChanged.AddListener(OnDayChanged);
    }

    private void Update()
    {
        if (_deliveredToday) return;
        if (GameClock.Instance == null) return;
        if (GameClock.Instance.CurrentHour < _deliveryHour) return;

        // Only deliver during Exploration phase
        if (DayPhaseManager.Instance == null) return;
        if (DayPhaseManager.Instance.CurrentPhase != DayPhaseManager.DayPhase.Exploration) return;

        _deliveredToday = true;
        StartCoroutine(DeliverMail());
    }

    private void OnDayChanged(int newDay)
    {
        _deliveredToday = false;

        // Clean up uncollected mail from yesterday
        for (int i = _activeMailObjects.Count - 1; i >= 0; i--)
        {
            if (_activeMailObjects[i] != null)
                Destroy(_activeMailObjects[i]);
        }
        _activeMailObjects.Clear();
    }

    private MailDeliveryDefinition FindDeliveryForDay(int day)
    {
        if (_schedule != null)
        {
            for (int i = 0; i < _schedule.Length; i++)
            {
                if (_schedule[i] != null && _schedule[i].day == day)
                    return _schedule[i];
            }
        }
        return _fallback;
    }

    private IEnumerator DeliverMail()
    {
        int day = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
        var delivery = FindDeliveryForDay(day);

        if (delivery == null || delivery.items == null || delivery.items.Length == 0)
        {
            Debug.Log("[MailController] No mail for today.");
            yield break;
        }

        Vector3 basePos = _doorSlidePoint != null
            ? _doorSlidePoint.position
            : transform.position;

        Debug.Log($"[MailController] Delivering {delivery.items.Length} item(s) for day {day}.");

        for (int i = 0; i < delivery.items.Length; i++)
        {
            var item = delivery.items[i];

            // Pick the right prefab
            GameObject prefab = item.type == MailItemType.Package ? _packagePrefab : _letterPrefab;

            // Scatter position so items don't overlap
            Vector3 offset = new Vector3(
                Random.Range(-_scatterRadius, _scatterRadius),
                0f,
                Random.Range(-_scatterRadius, _scatterRadius)
            );
            Vector3 spawnPos = basePos + offset;

            GameObject mailObj;
            if (prefab != null)
            {
                mailObj = Instantiate(prefab, spawnPos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            }
            else
            {
                // Fallback: create a simple cube placeholder
                mailObj = GameObject.CreatePrimitive(
                    item.type == MailItemType.Package ? PrimitiveType.Cube : PrimitiveType.Quad);
                mailObj.transform.position = spawnPos;
                mailObj.transform.localScale = item.type == MailItemType.Package
                    ? new Vector3(0.15f, 0.12f, 0.1f)
                    : new Vector3(0.12f, 0.08f, 0.01f);
                if (item.type != MailItemType.Package)
                    mailObj.transform.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
            }

            mailObj.name = $"Mail_{item.type}_{item.senderName}";

            // Ensure mail has a collider for raycasting
            if (mailObj.GetComponent<Collider>() == null)
                mailObj.AddComponent<BoxCollider>();

            // Attach pickup handler
            var pickup = mailObj.AddComponent<MailPickup>();
            pickup.Setup(item, day);

            _activeMailObjects.Add(mailObj);

            // Play delivery SFX
            if (_deliverySFX != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(_deliverySFX);

            // Poof + spring animation
            StartCoroutine(SpawnAnimation(mailObj.transform));

            // Stagger between items
            if (i < delivery.items.Length - 1)
                yield return new WaitForSeconds(0.35f);
        }
    }

    private IEnumerator SpawnAnimation(Transform t)
    {
        if (t == null) yield break;

        // Start at scale 0
        t.localScale = Vector3.zero;

        // Poof particles
        SpawnPoof(t.position);

        // Spring scale: 0 → 1.2 → 0.95 → 1.0
        yield return MailPickup.SpringScale(t, 0f, 1f, 0.4f);
    }

    private void SpawnPoof(Vector3 position)
    {
        var poofGO = new GameObject("DeliveryPoof");
        poofGO.transform.position = position;

        var ps = poofGO.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.3f;
        main.startLifetime = 0.6f;
        main.startSpeed = 2f;
        main.startSize = 0.1f;
        main.startColor = new Color(0.95f, 0.92f, 0.85f, 0.8f);
        main.maxParticles = 16;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 16) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.08f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        ps.Play();
        Destroy(poofGO, 1.5f);
    }
}

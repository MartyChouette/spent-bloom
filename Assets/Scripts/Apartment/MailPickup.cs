using System.Collections;
using UnityEngine;

/// <summary>
/// Attached to a spawned mail item on the floor. Handles click-to-read (letters)
/// or click-to-open (packages). Called by ObjectGrabber when the player clicks.
/// </summary>
public class MailPickup : MonoBehaviour
{
    private MailItem _mailItem;
    private int _dayReceived;
    private bool _consumed;

    /// <summary>Configure this pickup with its mail data.</summary>
    public void Setup(MailItem item, int day)
    {
        _mailItem = item;
        _dayReceived = day;
    }

    /// <summary>
    /// Called by ObjectGrabber on click. Returns true if consumed (blocks further click handling).
    /// </summary>
    public bool TryInteract()
    {
        if (_consumed || _mailItem == null) return false;
        _consumed = true;

        switch (_mailItem.type)
        {
            case MailItemType.Letter:
            case MailItemType.Catalog:
                OpenLetter();
                break;
            case MailItemType.Package:
                StartCoroutine(OpenPackage());
                break;
        }
        return true;
    }

    private void OpenLetter()
    {
        // Add to inventory
        MailInventory.Collect(_mailItem, _dayReceived);

        // Mark as read immediately since the player is opening it
        var collected = MailInventory.All;
        if (collected.Count > 0)
            MailInventory.MarkRead(collected[collected.Count - 1]);

        // Play open SFX
        if (AudioManager.Instance != null)
        {
            var controller = MailController.Instance;
            if (controller != null && controller.OpenSFX != null)
                AudioManager.Instance.PlaySFX(controller.OpenSFX);
        }

        // Show text overlay
        MailTextOverlay.Show(_mailItem.senderName, _mailItem.textLines, () =>
        {
            // Poof and destroy
            SpawnPoof();
            Destroy(gameObject);
        });
    }

    private IEnumerator OpenPackage()
    {
        // Add to inventory
        MailInventory.Collect(_mailItem, _dayReceived);
        var collected = MailInventory.All;
        if (collected.Count > 0)
            MailInventory.MarkRead(collected[collected.Count - 1]);

        // Play open SFX
        if (AudioManager.Instance != null)
        {
            var controller = MailController.Instance;
            if (controller != null && controller.OpenSFX != null)
                AudioManager.Instance.PlaySFX(controller.OpenSFX);
        }

        // Box burst animation
        yield return SpringScale(transform, 1f, 1.3f, 0.15f);

        // Spawn the item inside
        if (_mailItem.itemPrefab != null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 0.3f;
            var item = Instantiate(_mailItem.itemPrefab, spawnPos, Quaternion.identity);
            // Spring the new item in
            StartCoroutine(SpringScale(item.transform, 0f, 1f, 0.4f));
        }

        // Shrink and destroy the box
        yield return SpringScale(transform, 1.3f, 0f, 0.25f);
        Destroy(gameObject);
    }

    private void SpawnPoof()
    {
        // Simple particle burst at this position
        var poofGO = new GameObject("MailPoof");
        poofGO.transform.position = transform.position;

        var ps = poofGO.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.3f;
        main.startLifetime = 0.5f;
        main.startSpeed = 1.5f;
        main.startSize = 0.08f;
        main.startColor = new Color(0.9f, 0.88f, 0.82f, 0.7f);
        main.maxParticles = 12;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        ps.Play();
        Destroy(poofGO, 1f);
    }

    /// <summary>Spring scale animation with overshoot.</summary>
    public static IEnumerator SpringScale(Transform t, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / duration);
            // Elastic ease-out
            float overshoot = to + (to - from) * 0.2f;
            float scale;
            if (p < 0.6f)
                scale = Mathf.Lerp(from, overshoot, p / 0.6f);
            else if (p < 0.8f)
                scale = Mathf.Lerp(overshoot, to * 0.95f, (p - 0.6f) / 0.2f);
            else
                scale = Mathf.Lerp(to * 0.95f, to, (p - 0.8f) / 0.2f);

            if (t != null)
                t.localScale = Vector3.one * scale;
            yield return null;
        }
        if (t != null)
            t.localScale = Vector3.one * to;
    }
}

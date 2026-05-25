using UnityEngine;

/// <summary>
/// Spawns a burst of celebratory particles from a position.
/// Used when items pair, collections progress, or milestones complete.
/// Self-destructs after particles finish.
/// </summary>
public static class CelebrationBurst
{
    /// <summary>
    /// Spawn a celebration particle burst at the given position.
    /// </summary>
    /// <param name="position">World position to emit from.</param>
    /// <param name="radius">Emission sphere radius.</param>
    /// <param name="count">Number of particles.</param>
    /// <param name="color">Base color. Defaults to warm gold.</param>
    public static void Spawn(Vector3 position, float radius = 0.12f, int count = 18, Color? color = null)
    {
        var go = new GameObject("CelebrationBurst");
        go.transform.position = position;

        // Render on overlay layer so particles draw on top
        int heldLayer = LayerMask.NameToLayer("HeldItem");
        if (heldLayer >= 0)
            go.layer = heldLayer;

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        Color baseColor = color ?? new Color(1f, 0.85f, 0.3f, 0.9f);
        Color secondColor = new Color(1f, 0.6f, 0.2f, 0.8f);

        var main = ps.main;
        main.playOnAwake = false;
        main.duration = 0.6f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
        main.gravityModifier = 0.3f;
        main.maxParticles = count + 5;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;
        main.startColor = new ParticleSystem.MinMaxGradient(baseColor, secondColor);

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius;

        // Particles shrink over lifetime
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.5f, 0.7f),
            new Keyframe(1f, 0f)
        ));

        // Fade in then out
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = gradient;

        // Slight drag so particles arc outward then settle
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.speedModifier = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 0.2f)
        ));

        // Simple circle material
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                  ?? Shader.Find("Particles/Standard Unlit");
        if (shader != null)
        {
            var mat = new Material(shader);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_ALPHABLEND_ON");
            renderer.material = mat;
        }

        ps.Play();
    }
}

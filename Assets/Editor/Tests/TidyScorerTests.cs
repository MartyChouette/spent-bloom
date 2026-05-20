using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Edit-mode unit tests for TidyScorer formula logic.
/// Implements spec: design/gdd/tidiness-scoring.md
///
/// TidyScorer is a MonoBehaviour singleton that depends on scene objects
/// (CleaningManager, PlaceableObject registry, ReactableTag registry,
/// LivingFlowerPlantManager). Rather than spinning up the full scene, these
/// tests exercise the underlying math directly via static helper methods that
/// mirror the production formulas exactly.
///
/// Formula under test (from TidyScorer.GetAreaTidiness):
///   tidiness = stainClean * 0.25 + objectClean * 0.25 + smellClean * 0.25 + clutterClean * 0.25
///
/// where:
///   objectClean  = 1 - Clamp01(messCount  / maxExpectedMess   [3])
///   clutterClean = 1 - Clamp01(floorItems / maxExpectedClutter [3])
///   smellClean   = 1 - Clamp01(totalSmell / smellThreshold     [1.5])
///   totalSmell  *= 1 - (avgPlantHealth * plantSmellReduction   [0.15])  when plants exist
///
/// Run via Window > General > Test Runner (Edit Mode tab).
/// </summary>
public class TidyScorerTests
{
    // ── Default config values (mirrors TidyScorer serialized defaults) ──

    private const float StainWeight   = 0.25f;
    private const float ObjectWeight  = 0.25f;
    private const float SmellWeight   = 0.25f;
    private const float ClutterWeight = 0.25f;

    private const int   MaxExpectedMess    = 3;
    private const int   MaxExpectedClutter = 3;
    private const float SmellThreshold     = 1.5f;
    private const float PlantSmellReduction = 0.15f;

    // ── Formula helpers (replicate TidyScorer private methods verbatim) ──

    /// <summary>
    /// Replicates TidyScorer.GetObjectClean denominator logic without scene objects.
    /// </summary>
    private static float ObjectClean(int messCount)
        => 1f - Mathf.Clamp01((float)messCount / MaxExpectedMess);

    /// <summary>
    /// Replicates TidyScorer.GetClutterClean denominator logic without scene objects.
    /// </summary>
    private static float ClutterClean(int floorItems)
        => 1f - Mathf.Clamp01((float)floorItems / MaxExpectedClutter);

    /// <summary>
    /// Replicates TidyScorer.GetSmellClean without scene objects.
    /// avgPlantHealth = -1 means no plants present (no reduction applied).
    /// </summary>
    private static float SmellClean(float rawSmell, float avgPlantHealth = -1f)
    {
        float totalSmell = rawSmell;
        if (avgPlantHealth >= 0f)
            totalSmell *= 1f - (avgPlantHealth * PlantSmellReduction);
        return 1f - Mathf.Clamp01(totalSmell / SmellThreshold);
    }

    /// <summary>
    /// Weighted sum identical to TidyScorer.GetAreaTidiness.
    /// </summary>
    private static float Tidiness(float stain, float obj, float smell, float clutter)
        => stain * StainWeight + obj * ObjectWeight + smell * SmellWeight + clutter * ClutterWeight;

    // ─────────────────────────────────────────────────────────────
    // Aggregate formula — AllClean / AllDirty
    // ─────────────────────────────────────────────────────────────

    [Test]
    public void AllClean_ReturnsOne()
    {
        // Arrange
        float stain   = 1f;
        float obj     = ObjectClean(0);   // 0 mess items
        float smell   = SmellClean(0f);   // 0 smell, no plants
        float clutter = ClutterClean(0);  // 0 floor items

        // Act
        float result = Tidiness(stain, obj, smell, clutter);

        // Assert
        Assert.AreEqual(1f, result, 0.001f);
    }

    [Test]
    public void AllDirty_ReturnsZero()
    {
        // Arrange — each signal at absolute worst
        float stain   = 0f;
        float obj     = ObjectClean(MaxExpectedMess);     // saturated at 0
        float smell   = SmellClean(SmellThreshold);       // saturated at 0
        float clutter = ClutterClean(MaxExpectedClutter); // saturated at 0

        // Act
        float result = Tidiness(stain, obj, smell, clutter);

        // Assert
        Assert.AreEqual(0f, result, 0.001f);
    }

    // ─────────────────────────────────────────────────────────────
    // Weight parity — each signal contributes equally
    // ─────────────────────────────────────────────────────────────

    [Test]
    public void WeightsAreEqual_StainOnly_ContributesQuarter()
    {
        // Arrange — only stain clean; all others dirty
        float stain   = 1f;
        float obj     = 0f;
        float smell   = 0f;
        float clutter = 0f;

        // Act
        float result = Tidiness(stain, obj, smell, clutter);

        // Assert — stain alone → 1.0 * 0.25 = 0.25
        Assert.AreEqual(0.25f, result, 0.001f);
    }

    [Test]
    public void WeightsAreEqual_ObjectOnly_ContributesQuarter()
    {
        // Arrange — only object clean; all others dirty
        float stain   = 0f;
        float obj     = 1f;
        float smell   = 0f;
        float clutter = 0f;

        // Act
        float result = Tidiness(stain, obj, smell, clutter);

        // Assert — objectClean alone → 1.0 * 0.25 = 0.25
        Assert.AreEqual(0.25f, result, 0.001f);
    }

    [Test]
    public void WeightsAreEqual_SmellOnly_ContributesQuarter()
    {
        // Arrange — only smell clean; all others dirty
        float stain   = 0f;
        float obj     = 0f;
        float smell   = 1f;
        float clutter = 0f;

        // Act
        float result = Tidiness(stain, obj, smell, clutter);

        // Assert — smellClean alone → 1.0 * 0.25 = 0.25
        Assert.AreEqual(0.25f, result, 0.001f);
    }

    [Test]
    public void WeightsAreEqual_ClutterOnly_ContributesQuarter()
    {
        // Arrange — only clutter clean; all others dirty
        float stain   = 0f;
        float obj     = 0f;
        float smell   = 0f;
        float clutter = 1f;

        // Act
        float result = Tidiness(stain, obj, smell, clutter);

        // Assert — clutterClean alone → 1.0 * 0.25 = 0.25
        Assert.AreEqual(0.25f, result, 0.001f);
    }

    [Test]
    public void WeightsSumToOne()
    {
        // Arrange — all four weights must sum to 1.0
        float sum = StainWeight + ObjectWeight + SmellWeight + ClutterWeight;

        // Assert
        Assert.AreEqual(1f, sum, 0.001f);
    }

    // ─────────────────────────────────────────────────────────────
    // Smell signal
    // ─────────────────────────────────────────────────────────────

    [Test]
    public void SmellAtThreshold_ReturnsZero()
    {
        // Arrange — totalSmell exactly at the threshold (1.5) → smellClean = 0
        float smell = SmellClean(SmellThreshold);

        // Assert
        Assert.AreEqual(0f, smell, 0.001f);
    }

    [Test]
    public void SmellAtHalf_ReturnsHalf()
    {
        // Arrange — totalSmell = 0.75 = threshold/2 → smellClean = 0.5
        float smell = SmellClean(SmellThreshold * 0.5f);

        // Assert
        Assert.AreEqual(0.5f, smell, 0.001f);
    }

    // ─────────────────────────────────────────────────────────────
    // Plant smell reduction
    // ─────────────────────────────────────────────────────────────

    [Test]
    public void PlantReduction_HealthyPlants_Reduces15Percent()
    {
        // Arrange — totalSmell = 1.0, avgHealth = 1.0 → reduced by 15% to 0.85
        // smellClean = 1 - (0.85 / 1.5) ≈ 0.4333
        float rawSmell = 1.0f;
        float avgHealth = 1.0f;
        float expectedSmell = rawSmell * (1f - avgHealth * PlantSmellReduction); // 0.85
        float expected = 1f - Mathf.Clamp01(expectedSmell / SmellThreshold);

        // Act
        float result = SmellClean(rawSmell, avgHealth);

        // Assert
        Assert.AreEqual(expected, result, 0.001f);
    }

    [Test]
    public void PlantReduction_HalfHealth_Reduces7Point5Percent()
    {
        // Arrange — avgHealth = 0.5 → reduction factor = 0.5 * 0.15 = 0.075
        // totalSmell = 1.0 → reduced to 0.925
        float rawSmell = 1.0f;
        float avgHealth = 0.5f;
        float expectedSmell = rawSmell * (1f - avgHealth * PlantSmellReduction); // 0.925
        float expected = 1f - Mathf.Clamp01(expectedSmell / SmellThreshold);

        // Act
        float result = SmellClean(rawSmell, avgHealth);

        // Assert
        Assert.AreEqual(expected, result, 0.001f);
    }

    [Test]
    public void PlantReduction_NoPlants_NoReduction()
    {
        // Arrange — no plants present; raw smell is used directly
        float rawSmell = 1.0f;
        float expectedNoReduction = 1f - Mathf.Clamp01(rawSmell / SmellThreshold);

        // Act — pass avgPlantHealth = -1 to signal "no plants"
        float result = SmellClean(rawSmell, -1f);

        // Assert
        Assert.AreEqual(expectedNoReduction, result, 0.001f);
    }

    // ─────────────────────────────────────────────────────────────
    // Object mess threshold
    // ─────────────────────────────────────────────────────────────

    [Test]
    public void MessThreshold_ThreeItems_ReturnsZero()
    {
        // Arrange — messCount = maxExpectedMess (3) → objectClean = 0
        float obj = ObjectClean(MaxExpectedMess);

        // Assert
        Assert.AreEqual(0f, obj, 0.001f);
    }

    [Test]
    public void MessThreshold_ZeroItems_ReturnsOne()
    {
        // Arrange — no misplaced objects
        float obj = ObjectClean(0);

        // Assert
        Assert.AreEqual(1f, obj, 0.001f);
    }

    // ─────────────────────────────────────────────────────────────
    // Floor clutter threshold
    // ─────────────────────────────────────────────────────────────

    [Test]
    public void ClutterThreshold_ThreeItems_ReturnsZero()
    {
        // Arrange — floorItems = maxExpectedClutter (3) → clutterClean = 0
        float clutter = ClutterClean(MaxExpectedClutter);

        // Assert
        Assert.AreEqual(0f, clutter, 0.001f);
    }

    [Test]
    public void ClutterThreshold_ZeroItems_ReturnsOne()
    {
        // Arrange — no floor items
        float clutter = ClutterClean(0);

        // Assert
        Assert.AreEqual(1f, clutter, 0.001f);
    }
}

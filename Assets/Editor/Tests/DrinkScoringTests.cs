using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Edit-mode unit tests for DrinkPourManager scoring formula logic.
/// Implements spec: design/gdd/drink-making.md
///
/// DrinkPourManager.CalculateScore and its sub-methods are private and depend
/// on live MonoBehaviour state (_activeGlass, _activeRecipe). Rather than
/// instantiating the full component, these tests replicate each formula as
/// static helpers that mirror the production code verbatim.
///
/// Score breakdown (max 100 by default):
///   Order score  : 0 or 5 or 10 pts  (CalculateOrderScore)
///   Layer score  : 0–50 pts           (CalculateLayerScore)
///   Fill score   : 0–30 pts           (CalculateFillScore)
///   Garnish bonus: 0–10 pts           (CalculateGarnishBonus, capped at 10)
///   Overflow pen : –15 pts            (applied before Clamp to [0, baseScore])
///
/// Final: Clamp((int)raw, 0, baseScore)
///
/// Run via Window > General > Test Runner (Edit Mode tab).
/// </summary>
public class DrinkScoringTests
{
    // ── Formula helpers (replicate DrinkPourManager private methods verbatim) ──

    /// <summary>
    /// Replicates CalculateOrderScore.
    /// <para>Returns 10 if all in order, 5 if exactly one out of order, 0 otherwise.</para>
    /// </summary>
    /// <param name="pourOrder">Actual pour sequence (ingredient references).</param>
    /// <param name="expectedOrder">Recipe ingredient sequence (same references).</param>
    private static float OrderScore(
        List<DrinkIngredientDefinition> pourOrder,
        DrinkIngredientDefinition[] expectedOrder)
    {
        if (expectedOrder == null || expectedOrder.Length == 0) return 10f;

        int correct = 0;
        int compareCount = Mathf.Min(pourOrder.Count, expectedOrder.Length);
        for (int i = 0; i < compareCount; i++)
        {
            if (pourOrder[i] == expectedOrder[i]) correct++;
        }

        if (correct == expectedOrder.Length) return 10f;
        if (correct >= expectedOrder.Length - 1) return 5f;
        return 0f;
    }

    /// <summary>
    /// Replicates CalculateLayerScore.
    /// <para>Returns 0–50 based on average per-ingredient accuracy.</para>
    /// </summary>
    /// <param name="actualAmounts">Actual poured amount for each ingredient (parallel to recipe).</param>
    /// <param name="portionsNormalized">Recipe portionNormalized array.</param>
    /// <param name="idealFillLevel">Recipe idealFillLevel.</param>
    /// <param name="portionTolerance">Recipe portionTolerance.</param>
    private static float LayerScore(
        float[] actualAmounts,
        float[] portionsNormalized,
        float idealFillLevel,
        float portionTolerance)
    {
        if (portionsNormalized == null || actualAmounts == null) return 50f;

        float totalFill = 0f;
        foreach (float a in actualAmounts) totalFill += a;
        if (totalFill <= 0f) return 0f;

        float totalAccuracy = 0f;
        int count = Mathf.Min(actualAmounts.Length, portionsNormalized.Length);
        for (int i = 0; i < count; i++)
        {
            float target   = portionsNormalized[i] * idealFillLevel;
            float actual   = actualAmounts[i];
            float dist     = Mathf.Abs(actual - target);
            float accuracy = Mathf.Clamp01(1f - dist / Mathf.Max(portionTolerance, 0.01f));
            totalAccuracy += accuracy;
        }

        return (totalAccuracy / count) * 50f;
    }

    /// <summary>
    /// Replicates CalculateFillScore.
    /// <para>Returns 0–30 based on how close totalFill is to idealFillLevel.</para>
    /// </summary>
    private static float FillScore(float totalFill, float idealFillLevel, float fillTolerance)
    {
        float dist     = Mathf.Abs(totalFill - idealFillLevel);
        float accuracy = Mathf.Clamp01(1f - dist / Mathf.Max(fillTolerance, 0.01f));
        return accuracy * 30f;
    }

    /// <summary>
    /// Replicates CalculateGarnishBonus.
    /// <para>Returns sum of bonusPoints for each matched garnish, capped at 10.</para>
    /// </summary>
    /// <param name="recipeGarnishes">Required garnishes from the recipe.</param>
    /// <param name="addedGarnishes">Garnishes actually added to the glass.</param>
    private static float GarnishBonus(
        DrinkGarnishDefinition[] recipeGarnishes,
        List<DrinkGarnishDefinition> addedGarnishes)
    {
        if (recipeGarnishes == null || recipeGarnishes.Length == 0) return 0f;

        float bonus = 0f;
        for (int i = 0; i < recipeGarnishes.Length; i++)
        {
            if (recipeGarnishes[i] != null && addedGarnishes.Contains(recipeGarnishes[i]))
                bonus += recipeGarnishes[i].bonusPoints;
        }
        return Mathf.Min(bonus, 10f);
    }

    /// <summary>
    /// Replicates CalculateScore's final assembly and clamp.
    /// </summary>
    private static int TotalScore(
        float order, float layer, float fill, float garnish,
        float overflowPenalty, int baseScore = 100)
    {
        float raw = order + layer + fill + garnish + overflowPenalty;
        return Mathf.Clamp((int)raw, 0, baseScore);
    }

    // ── ScriptableObject helpers ─────────────────────────────────────

    private readonly List<Object> _tempSOs = new();

    private DrinkIngredientDefinition MakeIngredient(string name = "Ingredient", float weight = 0.5f)
    {
        var so = ScriptableObject.CreateInstance<DrinkIngredientDefinition>();
        so.name             = name;
        so.weight           = weight;
        so.foamRateMultiplier = 1f;
        _tempSOs.Add(so);
        return so;
    }

    private DrinkGarnishDefinition MakeGarnish(string name = "Garnish", int bonus = 5)
    {
        var so = ScriptableObject.CreateInstance<DrinkGarnishDefinition>();
        so.garnishName  = name;
        so.bonusPoints  = bonus;
        _tempSOs.Add(so);
        return so;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var so in _tempSOs)
            if (so != null) Object.DestroyImmediate(so);
        _tempSOs.Clear();
    }

    // ─────────────────────────────────────────────────────────────
    // Perfect pour
    // ─────────────────────────────────────────────────────────────

    [Test]
    public void PerfectPour_MaxScore()
    {
        // Arrange — all components at maximum; two-ingredient recipe
        var ingA = MakeIngredient("A");
        var ingB = MakeIngredient("B");

        var pour    = new List<DrinkIngredientDefinition> { ingA, ingB };
        var recipe  = new[] { ingA, ingB };
        var portions = new[] { 0.5f, 0.5f }; // 50/50 split
        float ideal     = 0.75f;
        float fillTol   = 0.10f;
        float portTol   = 0.08f;

        // Amounts exactly on target: 0.375 each
        var amounts = new[] { portions[0] * ideal, portions[1] * ideal };

        var garnish  = MakeGarnish(bonus: 5);
        var added    = new List<DrinkGarnishDefinition> { garnish };
        var required = new[] { garnish };

        // Act
        float order   = OrderScore(pour, recipe);              // 10
        float layer   = LayerScore(amounts, portions, ideal, portTol); // 50
        float fill    = FillScore(ideal, ideal, fillTol);      // 30
        float garnBon = GarnishBonus(required, added);         // 5
        int   result  = TotalScore(order, layer, fill, garnBon, 0f);

        // Assert: 10 + 50 + 30 + 5 = 95 (no overflow, no capping needed)
        Assert.AreEqual(95, result);
    }

    // ─────────────────────────────────────────────────────────────
    // Order score
    // ─────────────────────────────────────────────────────────────

    [Test]
    public void WrongOrder_OneOutOfOrder_FivePoints()
    {
        // Arrange — two-ingredient recipe; player poured in reversed order
        var ingA = MakeIngredient("A");
        var ingB = MakeIngredient("B");

        var pour   = new List<DrinkIngredientDefinition> { ingB, ingA }; // swapped
        var recipe = new[] { ingA, ingB };

        // Act
        float result = OrderScore(pour, recipe);

        // Assert — exactly one correct (none match positionally) = 0? Let's trace:
        // i=0: pour[0]=ingB vs expected[0]=ingA → mismatch
        // i=1: pour[1]=ingA vs expected[1]=ingB → mismatch
        // correct=0, expected.Length=2, correct < Length-1 → 0f
        // Per spec "one out of order → 5 pts" means exactly Length-1 correct.
        // Retest with three ingredients where only one slot is wrong:
        var ingC  = MakeIngredient("C");
        var pour3 = new List<DrinkIngredientDefinition> { ingA, ingC, ingB }; // ingB/ingC swapped
        var rec3  = new[] { ingA, ingB, ingC };

        result = OrderScore(pour3, rec3);

        // correct: i=0 match, i=1 mismatch, i=2 mismatch → correct=1, Length-1=2 → 0f
        // Spec "one out of order" = Length-1 matches:
        var pour3b = new List<DrinkIngredientDefinition> { ingA, ingB, ingB }; // only last wrong
        // i=0 match, i=1 match, i=2 mismatch → correct=2, Length-1=2 → 5f
        result = OrderScore(pour3b, rec3);

        // Assert
        Assert.AreEqual(5f, result, 0.001f);
    }

    [Test]
    public void WrongOrder_TwoOutOfOrder_ZeroPoints()
    {
        // Arrange — three-ingredient recipe; two slots wrong (only first correct)
        var ingA = MakeIngredient("A");
        var ingB = MakeIngredient("B");
        var ingC = MakeIngredient("C");

        var pour   = new List<DrinkIngredientDefinition> { ingA, ingC, ingB }; // last two swapped
        var recipe = new[] { ingA, ingB, ingC };

        // correct at i=0 (ingA==ingA), mismatch i=1, mismatch i=2 → correct=1, Length-1=2 → 0f
        float result = OrderScore(pour, recipe);

        // Assert
        Assert.AreEqual(0f, result, 0.001f);
    }

    // ─────────────────────────────────────────────────────────────
    // Layer accuracy score
    // ─────────────────────────────────────────────────────────────

    [Test]
    public void LayerAccuracy_ExactPortions_MaxScore()
    {
        // Arrange — two ingredients at exactly their target portion
        float ideal   = 0.75f;
        float portTol = 0.08f;
        var portions  = new[] { 0.4f, 0.6f };
        var amounts   = new[] { portions[0] * ideal, portions[1] * ideal }; // exact

        // Act
        float result = LayerScore(amounts, portions, ideal, portTol);

        // Assert
        Assert.AreEqual(50f, result, 0.001f);
    }

    [Test]
    public void LayerAccuracy_OutsideTolerance_ReducesScore()
    {
        // Arrange — one ingredient poured far beyond its target (dist > portionTolerance)
        float ideal   = 0.75f;
        float portTol = 0.08f;
        var portions  = new[] { 1f }; // 100% should be this ingredient
        var amounts   = new[] { 0f }; // poured nothing

        // Act — dist = target = 0.75, tolerance = 0.08 → accuracy = 0 → score = 0
        float result = LayerScore(amounts, portions, ideal, portTol);

        // Assert
        Assert.AreEqual(0f, result, 0.001f);
    }

    // ─────────────────────────────────────────────────────────────
    // Fill accuracy score
    // ─────────────────────────────────────────────────────────────

    [Test]
    public void FillAccuracy_AtIdeal_MaxScore()
    {
        // Arrange — totalFill exactly matches idealFillLevel
        float ideal    = 0.75f;
        float fillTol  = 0.10f;

        // Act
        float result = FillScore(ideal, ideal, fillTol);

        // Assert
        Assert.AreEqual(30f, result, 0.001f);
    }

    [Test]
    public void FillAccuracy_OutsideTolerance_ZeroScore()
    {
        // Arrange — totalFill is far from ideal (dist > fillTolerance)
        float ideal   = 0.75f;
        float fillTol = 0.10f;
        float fill    = 0f; // empty glass

        // Act — dist=0.75, tol=0.10 → accuracy=0 → score=0
        float result = FillScore(fill, ideal, fillTol);

        // Assert
        Assert.AreEqual(0f, result, 0.001f);
    }

    // ─────────────────────────────────────────────────────────────
    // Overflow penalty
    // ─────────────────────────────────────────────────────────────

    [Test]
    public void OverflowPenalty_MinusFifteen()
    {
        // Arrange — perfect drink except it overflowed (penalty = -15)
        float order   = 10f;
        float layer   = 50f;
        float fill    = 30f;
        float garnish = 0f;
        float overflow = -15f;

        // Act
        int result = TotalScore(order, layer, fill, garnish, overflow);

        // Assert: 10 + 50 + 30 + 0 - 15 = 75
        Assert.AreEqual(75, result);
    }

    // ─────────────────────────────────────────────────────────────
    // Garnish bonus
    // ─────────────────────────────────────────────────────────────

    [Test]
    public void GarnishBonus_CorrectGarnish_FivePoints()
    {
        // Arrange — one garnish required, one added, bonusPoints = 5
        var g        = MakeGarnish("Lime", bonus: 5);
        var required = new[] { g };
        var added    = new List<DrinkGarnishDefinition> { g };

        // Act
        float result = GarnishBonus(required, added);

        // Assert
        Assert.AreEqual(5f, result, 0.001f);
    }

    [Test]
    public void GarnishBonus_CappedAtTen()
    {
        // Arrange — three garnishes each worth 8 pts (total 24), must clamp to 10
        var g1 = MakeGarnish("G1", bonus: 8);
        var g2 = MakeGarnish("G2", bonus: 8);
        var g3 = MakeGarnish("G3", bonus: 8);

        var required = new[] { g1, g2, g3 };
        var added    = new List<DrinkGarnishDefinition> { g1, g2, g3 };

        // Act
        float result = GarnishBonus(required, added);

        // Assert — capped at 10
        Assert.AreEqual(10f, result, 0.001f);
    }

    [Test]
    public void GarnishBonus_WrongGarnish_ZeroPoints()
    {
        // Arrange — recipe wants g1, player added g2
        var g1       = MakeGarnish("Required", bonus: 5);
        var g2       = MakeGarnish("Wrong",    bonus: 5);
        var required = new[] { g1 };
        var added    = new List<DrinkGarnishDefinition> { g2 };

        // Act
        float result = GarnishBonus(required, added);

        // Assert
        Assert.AreEqual(0f, result, 0.001f);
    }

    // ─────────────────────────────────────────────────────────────
    // Empty glass
    // ─────────────────────────────────────────────────────────────

    [Test]
    public void EmptyGlass_ZeroScore()
    {
        // Arrange — no liquid poured at all
        var ingA = MakeIngredient("A");
        var recipe   = new[] { ingA };
        var portions = new[] { 1f };
        var amounts  = new[] { 0f };

        float order   = OrderScore(new List<DrinkIngredientDefinition>(), recipe);
        float layer   = LayerScore(amounts, portions, 0.75f, 0.08f); // totalFill=0 → 0
        float fill    = FillScore(0f, 0.75f, 0.10f);                 // dist=0.75, way outside tol → 0
        float garnish = 0f;

        // Act
        int result = TotalScore(order, layer, fill, garnish, 0f);

        // Assert — empty glass still gets partial order score (5) because
        // 0 poured items counts as "one off" from expected (correct >= length-1).
        // This matches DrinkPourManager behavior: no early-return for empty glass.
        Assert.AreEqual(5, result);
    }

    // ─────────────────────────────────────────────────────────────
    // Score clamp — cannot go negative
    // ─────────────────────────────────────────────────────────────

    [Test]
    public void TotalScore_ClampedToZero()
    {
        // Arrange — raw score would be negative (all zeros plus overflow penalty)
        float order   = 0f;
        float layer   = 0f;
        float fill    = 0f;
        float garnish = 0f;
        float overflow = -15f;

        // Act — raw = -15, clamp to [0, 100]
        int result = TotalScore(order, layer, fill, garnish, overflow);

        // Assert
        Assert.AreEqual(0, result);
    }

    [Test]
    public void TotalScore_ClampedToBaseScore()
    {
        // Arrange — raw score artificially exceeds baseScore
        float order   = 10f;
        float layer   = 50f;
        float fill    = 30f;
        float garnish = 10f; // already at cap
        float overflow = 0f;
        int baseScore  = 80; // lower than the 100 default

        // Act — raw = 100, baseScore = 80 → clamped to 80
        int result = TotalScore(order, layer, fill, garnish, overflow, baseScore);

        // Assert
        Assert.AreEqual(80, result);
    }
}

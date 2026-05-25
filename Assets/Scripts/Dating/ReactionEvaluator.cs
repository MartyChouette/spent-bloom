using System.Collections.Generic;
using UnityEngine;

/// <summary>Pour quality tier derived from drink score.</summary>
public enum PourQualityTier
{
    Bad,       // 0-39
    Mediocre,  // 40-59
    Good,      // 60-89
    Perfect    // 90-100
}

/// <summary>
/// Result of a detailed drink evaluation. Contains the final reaction,
/// the pour quality tier, and a list of noted response lines the character
/// will say in sequence during the verdict.
/// </summary>
public struct DrinkVerdict
{
    public ReactionType reaction;
    public PourQualityTier pourTier;
    public List<string> notes;
}

/// <summary>
/// Static utility for evaluating how a date character reacts to various stimuli.
/// </summary>
public static class ReactionEvaluator
{
    /// <summary>Evaluate a reactable object against date preferences.</summary>
    // Tags that every date universally dislikes (no config needed)
    private static readonly string[] s_universalDislike = { "pest" };

    public static ReactionType EvaluateReactable(ReactableTag tag, DatePreferences prefs)
    {
        if (tag == null || prefs == null) return ReactionType.Neutral;

        // Universal dislikes — pests, etc.
        foreach (string t in tag.Tags)
            foreach (string ud in s_universalDislike)
                if (string.Equals(t, ud, System.StringComparison.OrdinalIgnoreCase))
                    return ReactionType.Dislike;

        // Check if this is a living plant — health affects the reaction
        var plant = tag.GetComponent<LivingFlowerPlant>();

        foreach (string t in tag.Tags)
        {
            foreach (string liked in prefs.likedTags)
            {
                if (string.Equals(t, liked, System.StringComparison.OrdinalIgnoreCase))
                {
                    // If it's a plant, both health AND watering quality affect reaction
                    if (plant != null)
                        return EvaluatePlantWithWater(plant);
                    return ReactionType.Like;
                }
            }
        }

        foreach (string t in tag.Tags)
        {
            foreach (string disliked in prefs.dislikedTags)
            {
                if (string.Equals(t, disliked, System.StringComparison.OrdinalIgnoreCase))
                    return ReactionType.Dislike;
            }
        }

        return ReactionType.Neutral;
    }

    /// <summary>Map plant health to a reaction: healthy = Like, wilting = Neutral, dying = Dislike.</summary>
    private static ReactionType EvaluatePlantHealth(float health)
    {
        if (health >= 0.6f) return ReactionType.Like;
        if (health >= 0.3f) return ReactionType.Neutral;
        return ReactionType.Dislike;
    }

    /// <summary>
    /// Evaluate a plant considering both health AND watering quality.
    /// Perfect water + good health = Like. Stressed water downgrades the reaction.
    /// </summary>
    private static ReactionType EvaluatePlantWithWater(LivingFlowerPlant plant)
    {
        var baseReaction = EvaluatePlantHealth(plant.Health);

        // Water stress downgrades the reaction by one tier
        var waterState = plant.GetWaterState();
        if (waterState != LivingFlowerPlant.WaterState.Perfect)
        {
            if (baseReaction == ReactionType.Like) return ReactionType.Neutral;
            if (baseReaction == ReactionType.Neutral) return ReactionType.Dislike;
        }

        return baseReaction;
    }

    /// <summary>Evaluate a drink against date preferences and quality (legacy — no glass contents).</summary>
    public static ReactionType EvaluateDrink(DrinkRecipeDefinition recipe, int score, DatePreferences prefs)
    {
        return EvaluateDrinkDetailed(recipe, score, prefs, null).reaction;
    }

    /// <summary>Map a raw score (0-100) to a pour quality tier.</summary>
    public static PourQualityTier GetPourTier(int score)
    {
        if (score >= 90) return PourQualityTier.Perfect;
        if (score >= 60) return PourQualityTier.Good;
        if (score >= 40) return PourQualityTier.Mediocre;
        return PourQualityTier.Bad;
    }

    // Fallback lines when no authored note exists
    private static readonly string[] s_pourPerfect  = { "Poured beautifully.", "That's a perfect pour." };
    private static readonly string[] s_pourGood     = { "Nicely done.", "That looks good." };
    private static readonly string[] s_pourMediocre = { "Hmm, a bit uneven.", "This could use some work." };
    private static readonly string[] s_pourBad      = { "This is... rough.", "Did you even try?" };
    private static readonly string[] s_likedIngredientFallback   = { "Ooh, I like what's in this." };
    private static readonly string[] s_dislikedIngredientFallback = { "There's something in here I don't love." };
    private static readonly string[] s_specialIngredientFallback  = { "You remembered my favorite!" };

    private static string Pick(string[] arr) => arr[UnityEngine.Random.Range(0, arr.Length)];

    /// <summary>
    /// Detailed drink evaluation that checks actual glass contents, pour quality,
    /// ingredient preferences, and collects noted response lines.
    /// </summary>
    /// <param name="recipe">The recipe that was attempted (for recipe-level like/dislike).</param>
    /// <param name="score">Pour quality score 0-100.</param>
    /// <param name="prefs">Character's preferences.</param>
    /// <param name="glass">The actual glass contents (null if unavailable — falls back to recipe-only evaluation).</param>
    public static DrinkVerdict EvaluateDrinkDetailed(
        DrinkRecipeDefinition recipe, int score, DatePreferences prefs, DrinkGlass glass)
    {
        var verdict = new DrinkVerdict
        {
            reaction = ReactionType.Neutral,
            pourTier = GetPourTier(score),
            notes = new List<string>()
        };

        if (prefs == null) return verdict;

        // ── Scoring: accumulate positive/negative signals ──
        // Each factor nudges a running tally. Final reaction is derived from the sum.
        int sentiment = 0; // positive = like, negative = dislike

        // --- Recipe preference ---
        bool isLikedRecipe = false;
        bool isDislikedRecipe = false;
        if (recipe != null && prefs.likedDrinks != null)
        {
            foreach (var liked in prefs.likedDrinks)
                if (liked == recipe) { isLikedRecipe = true; break; }
        }
        if (recipe != null && prefs.dislikedDrinks != null)
        {
            foreach (var disliked in prefs.dislikedDrinks)
                if (disliked == recipe) { isDislikedRecipe = true; break; }
        }

        if (isLikedRecipe) sentiment += 2;
        if (isDislikedRecipe) sentiment -= 2;

        // --- Pour quality ---
        switch (verdict.pourTier)
        {
            case PourQualityTier.Perfect: sentiment += 2; break;
            case PourQualityTier.Good:    sentiment += 1; break;
            case PourQualityTier.Mediocre: break; // no change
            case PourQualityTier.Bad:     sentiment -= 2; break;
        }

        // Pour quality note (always included)
        string pourNote = prefs.GetPourQualityNote(verdict.pourTier);
        if (pourNote == null)
        {
            pourNote = verdict.pourTier switch
            {
                PourQualityTier.Perfect  => Pick(s_pourPerfect),
                PourQualityTier.Good     => Pick(s_pourGood),
                PourQualityTier.Mediocre => Pick(s_pourMediocre),
                _                        => Pick(s_pourBad)
            };
        }
        verdict.notes.Add(pourNote);

        // --- Ingredient checks (only if we have the actual glass) ---
        if (glass != null)
        {
            // Special ingredient
            if (prefs.specialIngredient != null)
            {
                bool found = false;
                for (int i = 0; i < glass.Layers.Count; i++)
                {
                    if (glass.Layers[i].ingredient == prefs.specialIngredient)
                    { found = true; break; }
                }

                if (found)
                {
                    sentiment += 3;
                    string note = prefs.GetSpecialIngredientNote() ?? Pick(s_specialIngredientFallback);
                    verdict.notes.Add(note);
                }
            }

            // Liked ingredients
            if (prefs.likedIngredients != null)
            {
                foreach (var liked in prefs.likedIngredients)
                {
                    if (liked == null || liked == prefs.specialIngredient) continue;
                    for (int i = 0; i < glass.Layers.Count; i++)
                    {
                        if (glass.Layers[i].ingredient == liked)
                        {
                            sentiment += 1;
                            string note = prefs.GetDrinkIngredientNote(liked) ?? Pick(s_likedIngredientFallback);
                            verdict.notes.Add(note);
                            break;
                        }
                    }
                }
            }

            // Disliked ingredients
            if (prefs.dislikedIngredients != null)
            {
                foreach (var disliked in prefs.dislikedIngredients)
                {
                    if (disliked == null) continue;
                    for (int i = 0; i < glass.Layers.Count; i++)
                    {
                        if (glass.Layers[i].ingredient == disliked)
                        {
                            sentiment -= 2;
                            string note = prefs.GetDrinkIngredientNote(disliked) ?? Pick(s_dislikedIngredientFallback);
                            verdict.notes.Add(note);
                            break;
                        }
                    }
                }
            }
        }

        // --- Derive final reaction from sentiment tally ---
        if (sentiment >= 3)       verdict.reaction = ReactionType.Like;
        else if (sentiment >= 1)  verdict.reaction = ReactionType.Like;
        else if (sentiment <= -2) verdict.reaction = ReactionType.Dislike;
        else                      verdict.reaction = ReactionType.Neutral;

        return verdict;
    }

    /// <summary>Evaluate an outfit against the date's style preferences.</summary>
    public static ReactionType EvaluateOutfit(OutfitDefinition outfit, DatePreferences prefs)
    {
        if (outfit == null || prefs == null) return ReactionType.Neutral;
        if (outfit.styleTags == null) return ReactionType.Neutral;

        // Check liked outfit tags first
        if (prefs.likedOutfitTags != null)
        {
            foreach (string tag in outfit.styleTags)
            {
                foreach (string liked in prefs.likedOutfitTags)
                {
                    if (string.Equals(tag, liked, System.StringComparison.OrdinalIgnoreCase))
                        return ReactionType.Like;
                }
            }
        }

        // Check disliked outfit tags
        if (prefs.dislikedOutfitTags != null)
        {
            foreach (string tag in outfit.styleTags)
            {
                foreach (string disliked in prefs.dislikedOutfitTags)
                {
                    if (string.Equals(tag, disliked, System.StringComparison.OrdinalIgnoreCase))
                        return ReactionType.Dislike;
                }
            }
        }

        return ReactionType.Neutral;
    }

    /// <summary>Evaluate floor clutter against a date's tolerance. clutterScore 0 = cluttered, 1 = clean.</summary>
    public static ReactionType EvaluateClutter(float clutterScore, float tolerance)
    {
        if (clutterScore >= tolerance) return ReactionType.Like;
        if (clutterScore >= tolerance * 0.5f) return ReactionType.Neutral;
        return ReactionType.Dislike;
    }

    /// <summary>Evaluate apartment cleanliness/tidiness. 0 = filthy, 1 = spotless.</summary>
    public static ReactionType EvaluateCleanliness(float tidiness)
    {
        if (tidiness >= 0.8f) return ReactionType.Like;
        if (tidiness >= 0.5f) return ReactionType.Neutral;
        return ReactionType.Dislike;
    }

    /// <summary>Evaluate the sprayed perfume against the date's perfume preferences.</summary>
    public static ReactionType EvaluatePerfume(PerfumeDefinition perfume, DatePreferences prefs)
    {
        return EvaluatePerfume(perfume, prefs, PerfumeBottle.LastSprayIntensity);
    }

    /// <summary>Evaluate perfume with explicit spray intensity (0-1).</summary>
    public static ReactionType EvaluatePerfume(PerfumeDefinition perfume, DatePreferences prefs, float intensity)
    {
        if (prefs == null) return ReactionType.Neutral;
        if (perfume == null) return prefs.noPerfumeReaction;

        string tag = perfume.perfumeTag;
        if (string.IsNullOrEmpty(tag)) return ReactionType.Neutral;

        bool liked = false;
        bool disliked = false;

        for (int i = 0; i < prefs.likedPerfumeTags.Length; i++)
            if (string.Equals(tag, prefs.likedPerfumeTags[i], System.StringComparison.OrdinalIgnoreCase))
            { liked = true; break; }

        for (int i = 0; i < prefs.dislikedPerfumeTags.Length; i++)
            if (string.Equals(tag, prefs.dislikedPerfumeTags[i], System.StringComparison.OrdinalIgnoreCase))
            { disliked = true; break; }

        // Intensity modifies the reaction:
        // Liked perfume:  1/3 = Neutral (too subtle), 2/3 = Like (sweet spot), 3/3 = Dislike (overwhelming)
        // Disliked:       1/3 = Neutral (barely notice), 2/3 = Dislike, 3/3 = Dislike
        // Neutral tag:    1/3 = Neutral, 2/3 = Neutral, 3/3 = Dislike (too strong)
        if (liked)
        {
            if (intensity > 0.99f) return ReactionType.Dislike;
            return intensity > 0.34f ? ReactionType.Like : ReactionType.Neutral;
        }

        if (disliked)
            return intensity > 0.34f ? ReactionType.Dislike : ReactionType.Neutral;

        return ReactionType.Neutral;
    }

    /// <summary>Evaluate how the current mood matches the date's preferences (dormant — kept for future use).</summary>
    public static ReactionType EvaluateMood(float currentMood, DatePreferences prefs)
    {
        if (prefs == null) return ReactionType.Neutral;

        if (currentMood >= prefs.preferredMoodMin && currentMood <= prefs.preferredMoodMax)
            return ReactionType.Like;

        float distMin = Mathf.Abs(currentMood - prefs.preferredMoodMin);
        float distMax = Mathf.Abs(currentMood - prefs.preferredMoodMax);
        float distance = Mathf.Min(distMin, distMax);

        if (distance > 0.3f)
            return ReactionType.Dislike;

        return ReactionType.Neutral;
    }
}

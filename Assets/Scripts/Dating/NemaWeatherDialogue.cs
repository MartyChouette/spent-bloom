using UnityEngine;

/// <summary>
/// Static dialogue table for Nema's reactions to weather changes.
/// 30-second cooldown prevents spamming during timeline interpolation.
/// Checks DialogueDatabase first, falls back to hardcoded arrays.
/// </summary>
public static class NemaWeatherDialogue
{
    private const float Cooldown = 30f;
    private static float s_lastCommentTime = -100f;

    // Moment keys per weather state (mapped to CSV WeatherLine entries)
    private static readonly string[] s_weatherMoments =
    {
        "WeatherLine",  // Clear (index 0)
        "WeatherLine",  // Overcast (index 1)
        "WeatherLine",  // Rainy (index 2)
        "WeatherLine",  // Stormy (index 3)
        "WeatherLine",  // Snowy (index 4)
        "WeatherLine",  // FallingLeaves (index 5)
    };

    // CSV IDs by weather state for direct lookup
    private static readonly string[][] s_csvIdPrefixes =
    {
        new[] { "CORE-WX-CLEAR1", "CORE-WX-CLEAR2", "CORE-WX-CLEAR3", "CORE-WX-CLEAR4" },
        new[] { "CORE-WX-OVER1", "CORE-WX-OVER2", "CORE-WX-OVER3", "CORE-WX-OVER4" },
        new[] { "CORE-WX-RAIN1", "CORE-WX-RAIN2", "CORE-WX-RAIN3", "CORE-WX-RAIN4" },
        new[] { "CORE-WX-STORM1", "CORE-WX-STORM2", "CORE-WX-STORM3", "CORE-WX-STORM4" },
        new[] { "CORE-WX-SNOW1", "CORE-WX-SNOW2", "CORE-WX-SNOW3", "CORE-WX-SNOW4" },
        new[] { "CORE-WX-FALL1", "CORE-WX-FALL2", "CORE-WX-FALL3", "CORE-WX-FALL4" },
    };

    // Hardcoded fallback
    private static readonly string[][] s_lines =
    {
        new[] { "What a beautiful day...", "The sun feels so nice.", "Perfect weather for a date.", "I love clear skies like this." },
        new[] { "Feels cozy with those clouds.", "I like this moody sky.", "Hmm, hope it doesn't rain.", "Overcast days have a certain charm." },
        new[] { "I love the sound of rain.", "Rainy days are the best for staying in.", "Good thing we're inside.", "Rain makes everything feel so intimate." },
        new[] { "Wow, listen to that storm!", "A little dramatic out there...", "Storms make everything feel exciting.", "I hope the power doesn't go out..." },
        new[] { "Snow! It's so pretty.", "I want to go outside and play in it.", "Everything looks magical.", "Hot cocoa weather..." },
        new[] { "Autumn vibes... I love it.", "Look at those leaves.", "This is my favorite time of year.", "The colors are gorgeous." },
    };

    public static void ReactToWeather(WeatherSystem.WeatherState state)
    {
        if (Time.time - s_lastCommentTime < Cooldown) return;
        if (DialoguePortraitBox.Instance == null) return;

        int idx = (int)state;
        if (idx < 0 || idx >= s_lines.Length) return;

        // Try DialogueDatabase by random ID
        string line = null;
        if (idx < s_csvIdPrefixes.Length)
        {
            var ids = s_csvIdPrefixes[idx];
            string pickedId = ids[Random.Range(0, ids.Length)];
            line = DialogueDatabase.GetById(pickedId)?.line;
        }

        // Fallback to hardcoded
        if (string.IsNullOrEmpty(line))
        {
            var pool = s_lines[idx];
            line = pool[Random.Range(0, pool.Length)];
        }

        DialoguePortraitBox.Instance.Say(line, 3f);
        s_lastCommentTime = Time.time;
    }
}

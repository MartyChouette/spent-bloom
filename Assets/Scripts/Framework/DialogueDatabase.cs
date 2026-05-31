using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime dialogue lookup from dialogue-master.csv.
/// Loads CSV from StreamingAssets at startup, builds dictionaries for
/// fast lookup by ID, character+moment+sentiment, and character+tag.
/// All game text routes through here.
/// </summary>
public static class DialogueDatabase
{
    [System.Serializable]
    public class DialogueLine
    {
        public string id;
        public string character;
        public string date;       // "1", "2", "3", "All", or ""
        public string day;        // in-game day number or ""
        public string moment;     // phase/trigger key
        public string tag;        // category tag (cottage, groovy, etc.)
        public string sentiment;  // Like, Dislike, Neutral, or ""
        public string line;       // the actual text
        public string notes;
    }

    // Primary index: ID → line
    private static readonly Dictionary<string, DialogueLine> s_byId = new();

    // Grouped index: (character, moment, sentiment) → list of lines
    private static readonly Dictionary<string, List<DialogueLine>> s_byMoment = new();

    // Tag index: (character, tag, sentiment) → list of lines
    private static readonly Dictionary<string, List<DialogueLine>> s_byTag = new();

    // Day index: (day, moment) → list of lines
    private static readonly Dictionary<string, List<DialogueLine>> s_byDay = new();

    // All lines for iteration
    private static readonly List<DialogueLine> s_all = new();

    public static IReadOnlyList<DialogueLine> All => s_all;
    public static bool IsLoaded => s_all.Count > 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void DomainReset()
    {
        s_byId.Clear();
        s_byMoment.Clear();
        s_byTag.Clear();
        s_byDay.Clear();
        s_all.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        LoadFromStreamingAssets();
    }

    // ─────────────────── Loading ───────────────────

    private static void LoadFromStreamingAssets()
    {
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, "dialogue-master.csv");

        if (!System.IO.File.Exists(path))
        {
            Debug.LogWarning($"[DialogueDatabase] CSV not found at {path}");
            return;
        }

        string csv = System.IO.File.ReadAllText(path);
        ParseCSV(csv);
        Debug.Log($"[DialogueDatabase] Loaded {s_all.Count} lines from {path}");
    }

    private static void ParseCSV(string csv)
    {
        s_byId.Clear();
        s_byMoment.Clear();
        s_byTag.Clear();
        s_byDay.Clear();
        s_all.Clear();

        var lines = csv.Split('\n');
        if (lines.Length < 2) return;

        // Skip header row
        for (int i = 1; i < lines.Length; i++)
        {
            string row = lines[i].TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(row)) continue;
            if (row.StartsWith("#")) continue; // comment rows

            var fields = ParseCSVRow(row);
            if (fields.Length < 8) continue;

            string id = fields[0].Trim();
            if (string.IsNullOrEmpty(id)) continue;

            var entry = new DialogueLine
            {
                id = id,
                character = fields[1].Trim(),
                date = fields[2].Trim(),
                day = fields[3].Trim(),
                moment = fields[4].Trim(),
                tag = fields[5].Trim(),
                sentiment = fields[6].Trim(),
                line = fields[7].Trim(),
                notes = fields.Length > 10 ? fields[10].Trim() : ""
            };

            // Skip entries with no line text (stubs to fill in)
            // But keep them in s_byId for structure awareness
            s_byId[entry.id] = entry;
            s_all.Add(entry);

            if (string.IsNullOrEmpty(entry.line)) continue;

            // Index by (character, moment, sentiment)
            if (!string.IsNullOrEmpty(entry.moment))
            {
                string momentKey = MakeMomentKey(entry.character, entry.date, entry.moment, entry.sentiment);
                if (!s_byMoment.TryGetValue(momentKey, out var momentList))
                {
                    momentList = new List<DialogueLine>();
                    s_byMoment[momentKey] = momentList;
                }
                momentList.Add(entry);

                // Also index without date for fallback
                string fallbackKey = MakeMomentKey(entry.character, "", entry.moment, entry.sentiment);
                if (fallbackKey != momentKey)
                {
                    if (!s_byMoment.TryGetValue(fallbackKey, out var fbList))
                    {
                        fbList = new List<DialogueLine>();
                        s_byMoment[fallbackKey] = fbList;
                    }
                    fbList.Add(entry);
                }
            }

            // Index by (character, tag, sentiment)
            if (!string.IsNullOrEmpty(entry.tag))
            {
                string tagKey = MakeTagKey(entry.character, entry.date, entry.tag, entry.sentiment);
                if (!s_byTag.TryGetValue(tagKey, out var tagList))
                {
                    tagList = new List<DialogueLine>();
                    s_byTag[tagKey] = tagList;
                }
                tagList.Add(entry);
            }

            // Index by (day, moment)
            if (!string.IsNullOrEmpty(entry.day) && !string.IsNullOrEmpty(entry.moment))
            {
                string dayKey = $"{entry.day}|{entry.moment}";
                if (!s_byDay.TryGetValue(dayKey, out var dayList))
                {
                    dayList = new List<DialogueLine>();
                    s_byDay[dayKey] = dayList;
                }
                dayList.Add(entry);
            }
        }
    }

    // ─────────────────── CSV Parsing ───────────────────

    /// <summary>
    /// Parse a CSV row respecting quoted fields (handles commas inside quotes).
    /// </summary>
    private static string[] ParseCSVRow(string row)
    {
        var fields = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();

        for (int i = 0; i < row.Length; i++)
        {
            char c = row[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < row.Length && row[i + 1] == '"')
                    {
                        current.Append('"'); // escaped quote
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
        }
        fields.Add(current.ToString());
        return fields.ToArray();
    }

    // ─────────────────── Key Builders ───────────────────

    private static string MakeMomentKey(string character, string date, string moment, string sentiment)
    {
        return $"{character}|{date}|{moment}|{sentiment}";
    }

    private static string MakeTagKey(string character, string date, string tag, string sentiment)
    {
        return $"{character}|{date}|{tag}|{sentiment}";
    }

    // ─────────────────── Lookup API ───────────────────

    /// <summary>Get a specific line by its unique ID.</summary>
    public static string GetById(string id)
    {
        if (s_byId.TryGetValue(id, out var entry) && !string.IsNullOrEmpty(entry.line))
            return entry.line;
        return null;
    }

    /// <summary>Get the full entry by ID.</summary>
    public static DialogueLine GetEntryById(string id)
    {
        return s_byId.TryGetValue(id, out var entry) ? entry : null;
    }

    /// <summary>
    /// Get a random line for a character at a specific moment.
    /// Falls back: character+date → character+any date → _Fallback+any date.
    /// </summary>
    public static string GetLine(string character, string date, string moment, string sentiment = "")
    {
        // Try character + specific date
        var key = MakeMomentKey(character, date, moment, sentiment);
        if (s_byMoment.TryGetValue(key, out var list) && list.Count > 0)
            return list[Random.Range(0, list.Count)].line;

        // Try character + any date
        key = MakeMomentKey(character, "", moment, sentiment);
        if (s_byMoment.TryGetValue(key, out list) && list.Count > 0)
            return list[Random.Range(0, list.Count)].line;

        // Try _Fallback
        key = MakeMomentKey("_Fallback", "", moment, sentiment);
        if (s_byMoment.TryGetValue(key, out list) && list.Count > 0)
            return list[Random.Range(0, list.Count)].line;

        // Try _Fallback + All
        key = MakeMomentKey("_Fallback", "All", moment, sentiment);
        if (s_byMoment.TryGetValue(key, out list) && list.Count > 0)
            return list[Random.Range(0, list.Count)].line;

        return null;
    }

    /// <summary>
    /// Get a reaction line for a character reacting to a tagged item.
    /// Falls back: character+date+tag → character+any+tag → _Fallback generic.
    /// </summary>
    public static string GetTagReaction(string character, string date, string tag, string sentiment)
    {
        // Try character + specific date + tag
        var key = MakeTagKey(character, date, tag, sentiment);
        if (s_byTag.TryGetValue(key, out var list) && list.Count > 0)
            return list[Random.Range(0, list.Count)].line;

        // Try character + any date + tag
        key = MakeTagKey(character, "", tag, sentiment);
        if (s_byTag.TryGetValue(key, out list) && list.Count > 0)
            return list[Random.Range(0, list.Count)].line;

        // Fall back to generic reaction
        return GetLine(character, date, "R-GenericReact", sentiment);
    }

    /// <summary>
    /// Get all lines for a specific day and moment (e.g. mail for day 2).
    /// </summary>
    public static List<DialogueLine> GetByDay(int day, string moment)
    {
        string key = $"{day}|{moment}";
        return s_byDay.TryGetValue(key, out var list) ? list : null;
    }

    /// <summary>
    /// Get all lines matching a moment (across all characters).
    /// Useful for _System lines like tutorial cards, dream screen, etc.
    /// </summary>
    public static List<DialogueLine> GetAllByMoment(string moment)
    {
        var results = new List<DialogueLine>();
        foreach (var entry in s_all)
        {
            if (entry.moment == moment && !string.IsNullOrEmpty(entry.line))
                results.Add(entry);
        }
        return results;
    }

    /// <summary>
    /// Get a random system/fallback line for a moment.
    /// Shorthand for GetLine("_System", "", moment).
    /// </summary>
    public static string GetSystemLine(string moment)
    {
        return GetLine("_System", "", moment) ?? GetLine("_Fallback", "", moment);
    }
}

using UnityEngine;

/// <summary>
/// Pronoun set for the player character.
/// </summary>
public enum PronounSet
{
    She,    // she/her/hers
    He,     // he/him/his
    They    // they/them/theirs
}

/// <summary>
/// Static session holder for player identity.
/// Set by NameEntryScreen, read by NewspaperManager, dialogue, etc.
/// </summary>
public static class PlayerData
{
    public static string PlayerName { get; set; } = "Nema";
    public static PronounSet Pronouns { get; set; } = PronounSet.She;

    /// <summary>Subject pronoun: she / he / they</summary>
    public static string Subject => Pronouns switch
    {
        PronounSet.She => "she",
        PronounSet.He => "he",
        _ => "they"
    };

    /// <summary>Object pronoun: her / him / them</summary>
    public static string Object => Pronouns switch
    {
        PronounSet.She => "her",
        PronounSet.He => "him",
        _ => "them"
    };

    /// <summary>Possessive pronoun: her / his / their</summary>
    public static string Possessive => Pronouns switch
    {
        PronounSet.She => "her",
        PronounSet.He => "his",
        _ => "their"
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        PlayerName = "Nema";
        Pronouns = PronounSet.She;
    }
}

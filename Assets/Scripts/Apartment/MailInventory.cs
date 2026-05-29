using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A collected mail entry stored in the inventory.
/// </summary>
[System.Serializable]
public class CollectedMail
{
    public string senderName;
    public string[] textLines;
    public string itemDisplayName;
    public int dayReceived;
    public MailItemType type;
    public bool wasRead;
}

/// <summary>
/// Persistent registry of all mail the player has collected.
/// Saved and restored with the game.
/// </summary>
public static class MailInventory
{
    private static readonly List<CollectedMail> s_collected = new();

    public static IReadOnlyList<CollectedMail> All => s_collected;
    public static int UnreadCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < s_collected.Count; i++)
                if (!s_collected[i].wasRead) count++;
            return count;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        s_collected.Clear();
    }

    /// <summary>Add a mail item to the collection.</summary>
    public static void Collect(MailItem item, int day)
    {
        s_collected.Add(new CollectedMail
        {
            senderName = item.senderName,
            textLines = item.textLines,
            itemDisplayName = item.itemDisplayName,
            dayReceived = day,
            type = item.type,
            wasRead = false
        });
    }

    /// <summary>Mark a collected mail entry as read.</summary>
    public static void MarkRead(CollectedMail mail)
    {
        mail.wasRead = true;
    }

    /// <summary>Load from save data.</summary>
    public static void LoadFrom(List<CollectedMail> data)
    {
        s_collected.Clear();
        if (data != null)
            s_collected.AddRange(data);
    }

    /// <summary>Get save data.</summary>
    public static List<CollectedMail> SaveData()
    {
        return new List<CollectedMail>(s_collected);
    }
}

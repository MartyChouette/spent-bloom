using UnityEngine;

/// <summary>
/// Type of mail item.
/// </summary>
public enum MailItemType
{
    Letter,     // readable text from a sender
    Package,    // box containing a PlaceableObject
    Catalog     // flavor text (fake ads, teases)
}

/// <summary>
/// A single item in a mail delivery.
/// </summary>
[System.Serializable]
public class MailItem
{
    [Tooltip("What kind of mail this is.")]
    public MailItemType type = MailItemType.Letter;

    [Tooltip("Who sent this (displayed in the letter header).")]
    public string senderName = "Unknown";

    [Tooltip("Text content for Letters and Catalogs. Each entry is a paragraph.")]
    [TextArea(2, 6)]
    public string[] textLines;

    [Tooltip("For Packages: the item prefab that pops out of the box.")]
    public GameObject itemPrefab;

    [Tooltip("Display name for the item (shown in inventory for packages).")]
    public string itemDisplayName;
}

/// <summary>
/// Defines what mail arrives on a given day.
/// Create one SO per day's delivery, or use day=0 as a fallback for unscheduled days.
/// </summary>
[CreateAssetMenu(fileName = "MailDelivery_Day", menuName = "Iris/Mail Delivery")]
public class MailDeliveryDefinition : ScriptableObject
{
    [Tooltip("Which game day this delivery arrives. 0 = fallback for any day without a specific delivery.")]
    public int day;

    [Tooltip("Items in this delivery.")]
    public MailItem[] items;
}

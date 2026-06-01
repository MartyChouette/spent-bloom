using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Iris/Newspaper Pool")]
public class NewspaperPoolDefinition : ScriptableObject
{
    [Header("Ad Pools")]
    [Tooltip("Full pool of personal ads to draw from each day.")]
    public List<DatePersonalDefinition> personalAds = new List<DatePersonalDefinition>();

    [Tooltip("Full pool of commercial ads to draw from each day.")]
    public List<CommercialAdDefinition> commercialAds = new List<CommercialAdDefinition>();

    [Header("Daily Layout")]
    [Tooltip("How many personal ads appear each day.")]
    [Range(1, 8)]
    public int personalAdsPerDay = 4;

    [Tooltip("How many commercial ads appear each day.")]
    [Range(0, 6)]
    public int commercialAdsPerDay = 3;

    [Header("Scheduled Dates")]
    [Tooltip("If set, this character is forced as the only selectable ad on Day 1.")]
    public DatePersonalDefinition tutorialDate;

    [Tooltip("Legacy single Day 2 date (ignored if day2Dates has entries).")]
    public DatePersonalDefinition day2Date;

    [Tooltip("Characters available on Day 2. All are selectable (not locked).")]
    public List<DatePersonalDefinition> day2Dates = new List<DatePersonalDefinition>();

    [Header("Repeat Rules")]
    [Tooltip("Can the same ad appear on consecutive days?")]
    public bool allowRepeats;

    [Header("Newspaper")]
    [Tooltip("Title displayed at the top of the newspaper.")]
    public string newspaperTitle = "The Daily Bloom";

    [Header("Visuals")]
    [Tooltip("Optional background sprite for the newspaper page. Leave null for default beige.")]
    public Sprite backgroundSprite;
}

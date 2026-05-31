using UnityEngine;
using TMPro;

/// <summary>
/// Pause menu Calendar page. Scrollable day-by-day timeline showing
/// dates, mail received, and plants spawned for each day.
/// </summary>
public class PausePageCalendar : MonoBehaviour
{
    private bool _built;
    private Transform _content;

    private void OnEnable()
    {
        if (!_built) BuildUI();
        Refresh();
    }

    public void Refresh()
    {
        if (_content == null) return;
        PauseUIHelper.ClearChildren(_content);

        var theme = IrisTextTheme.Active;
        int currentDay = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;

        for (int day = currentDay; day >= 1; day--)
        {
            bool isToday = day == currentDay;

            // Day header
            string dayText = isToday ? $"day {day}  <color=#88CC88>(today)</color>" : $"day {day}";
            var header = PauseUIHelper.CreateHeaderLabel(_content, dayText, theme, 35f);
            if (isToday) header.color = new Color(0.9f, 0.87f, 0.82f);

            bool hasContent = false;

            // Dates that occurred this day
            var entries = DateHistory.Entries;
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    var e = entries[i];
                    if (e.day != day) continue;
                    hasContent = true;

                    string result = e.succeeded
                        ? $"<color=#88CC88>{e.grade}</color>"
                        : $"<color=#CC8888>failed</color>";
                    string flower = !string.IsNullOrEmpty(e.flowerGrade)
                        ? $"  flower: {e.flowerGrade}" : "";
                    PauseUIHelper.CreateLabel(_content,
                        $"  date with <b>{e.name}</b>  {result}  {e.affection:F0}%{flower}", 15f,
                        new Color(0.7f, 0.68f, 0.65f), theme, 22f);
                }
            }

            // Mail received this day
            var mail = MailInventory.All;
            if (mail != null)
            {
                for (int i = 0; i < mail.Count; i++)
                {
                    var m = mail[i];
                    if (m.dayReceived != day) continue;
                    hasContent = true;

                    string typeStr = m.type switch
                    {
                        MailItemType.Letter => "letter",
                        MailItemType.Package => "package",
                        MailItemType.Catalog => "catalog",
                        _ => "mail"
                    };
                    string readMark = m.wasRead ? "" : " <color=#CCAA66>(unread)</color>";
                    PauseUIHelper.CreateLabel(_content,
                        $"  {typeStr} from <b>{m.senderName}</b>{readMark}", 15f,
                        new Color(0.7f, 0.68f, 0.65f), theme, 22f);
                }
            }

            // Plants spawned this day
            if (LivingFlowerPlantManager.Instance != null)
            {
                var plants = LivingFlowerPlantManager.Instance.ActivePlants;
                for (int i = 0; i < plants.Count; i++)
                {
                    var plant = plants[i];
                    if (plant == null || plant.SpawnDay != day) continue;
                    hasContent = true;

                    string health = plant.IsDead
                        ? "<color=#CC8888>wilted</color>"
                        : $"health: {plant.Health:P0}";
                    PauseUIHelper.CreateLabel(_content,
                        $"  flower planted ({plant.CharacterName})  {health}", 15f,
                        new Color(0.7f, 0.68f, 0.65f), theme, 22f);
                }
            }

            if (!hasContent)
            {
                PauseUIHelper.CreateLabel(_content, "  <color=#555555>nothing notable</color>", 14f,
                    new Color(0.5f, 0.5f, 0.5f), theme, 20f);
            }

            // Spacer between days
            PauseUIHelper.CreateLabel(_content, "", 6f, Color.clear, theme, 6f);
        }
    }

    private void BuildUI()
    {
        _built = true;
        PauseUIHelper.EnsureFullStretch(gameObject);
        _content = PauseUIHelper.CreateScrollableList(transform, new RectOffset(20, 20, 15, 15));
    }
}

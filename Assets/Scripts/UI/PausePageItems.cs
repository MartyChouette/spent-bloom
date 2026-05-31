using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Pause menu Items page. Six tabs showing categorized items:
/// Plants, Books, Drinks, Decor, Key Items, Mail.
/// Each item shows its status and which date characters like/dislike it.
/// </summary>
public class PausePageItems : MonoBehaviour
{
    private PauseTabBar _tabBar;
    private bool _built;

    private Transform _plantsContent;
    private Transform _booksContent;
    private Transform _drinksContent;
    private Transform _decorContent;
    private Transform _keyItemsContent;
    private Transform _mailContent;

    // Cached date character definitions for "who likes this"
    private DatePersonalDefinition[] _dateCharacters;

    private void OnEnable()
    {
        if (!_built) BuildUI();
        _dateCharacters = Resources.FindObjectsOfTypeAll<DatePersonalDefinition>();
        RefreshCurrentTab();
    }

    private void RefreshCurrentTab()
    {
        if (_tabBar == null) return;
        switch (_tabBar.CurrentTab)
        {
            case 0: RefreshPlants(); break;
            case 1: RefreshBooks(); break;
            case 2: RefreshDrinks(); break;
            case 3: RefreshDecor(); break;
            case 4: RefreshKeyItems(); break;
            case 5: RefreshMail(); break;
        }
    }

    // ─────────────────── Who Likes This ───────────────────

    private string GetWhoLikesThis(string[] itemTags)
    {
        if (itemTags == null || itemTags.Length == 0 || _dateCharacters == null)
            return "";

        var liked = new List<string>();
        var disliked = new List<string>();

        foreach (var character in _dateCharacters)
        {
            if (character == null) continue;
            var prefs = character.preferences;
            if (prefs == null) continue;

            bool likes = false;
            bool dislikes = false;

            foreach (string tag in itemTags)
            {
                if (!likes && prefs.likedTags != null)
                {
                    for (int i = 0; i < prefs.likedTags.Length; i++)
                        if (prefs.likedTags[i] == tag) { likes = true; break; }
                }
                if (!dislikes && prefs.dislikedTags != null)
                {
                    for (int i = 0; i < prefs.dislikedTags.Length; i++)
                        if (prefs.dislikedTags[i] == tag) { dislikes = true; break; }
                }
            }

            if (likes) liked.Add(character.characterName);
            if (dislikes) disliked.Add(character.characterName);
        }

        string result = "";
        if (liked.Count > 0) result += $"<color=#CC8899>liked by: {string.Join(", ", liked)}</color>";
        if (disliked.Count > 0)
        {
            if (result.Length > 0) result += "  ";
            result += $"<color=#8888AA>disliked by: {string.Join(", ", disliked)}</color>";
        }
        return result;
    }

    private string[] GetReactableTagsForObject(GameObject obj)
    {
        if (obj == null) return null;
        var tag = obj.GetComponent<ReactableTag>();
        if (tag == null || !tag.IsActive) return null;
        return tag.Tags;
    }

    // ─────────────────── Plants ───────────────────

    private void RefreshPlants()
    {
        if (_plantsContent == null) return;
        PauseUIHelper.ClearChildren(_plantsContent);
        var theme = IrisTextTheme.Active;

        // Apartment pots
        var pots = WaterablePlant.All;
        if (pots != null && pots.Count > 0)
        {
            PauseUIHelper.CreateHeaderLabel(_plantsContent, "apartment plants", theme);
            for (int i = 0; i < pots.Count; i++)
            {
                var pot = pots[i];
                if (pot == null || pot.definition == null) continue;

                string name = pot.definition.plantName;
                string water = Bar(pot.WaterLevel);
                PauseUIHelper.CreateLabel(_plantsContent,
                    $"  <b>{name}</b>  water: {water}", 16f,
                    new Color(0.7f, 0.68f, 0.65f), theme, 24f);

                string prefs = GetWhoLikesThis(GetReactableTagsForObject(pot.gameObject));
                if (!string.IsNullOrEmpty(prefs))
                    PauseUIHelper.CreateLabel(_plantsContent, $"    {prefs}", 13f,
                        new Color(0.6f, 0.58f, 0.55f), theme, 18f);
            }
        }

        // Living flowers from dates
        if (LivingFlowerPlantManager.Instance != null)
        {
            var flowers = LivingFlowerPlantManager.Instance.ActivePlants;
            if (flowers.Count > 0)
            {
                PauseUIHelper.CreateLabel(_plantsContent, "", 8f, Color.clear, theme, 8f);
                PauseUIHelper.CreateHeaderLabel(_plantsContent, "flowers from dates", theme);

                for (int i = 0; i < flowers.Count; i++)
                {
                    var flower = flowers[i];
                    if (flower == null) continue;

                    string healthStr = flower.IsDead
                        ? "<color=#CC8888>wilted</color>"
                        : $"health: {Bar(flower.Health)}";
                    string waterStr = flower.GetWaterState().ToString().ToLower();
                    PauseUIHelper.CreateLabel(_plantsContent,
                        $"  <b>{flower.CharacterName}'s flower</b>  {healthStr}  ({waterStr})", 16f,
                        new Color(0.7f, 0.68f, 0.65f), theme, 24f);
                }
            }
        }

        if ((pots == null || pots.Count == 0) &&
            (LivingFlowerPlantManager.Instance == null || LivingFlowerPlantManager.Instance.ActivePlants.Count == 0))
        {
            PauseUIHelper.CreateLabel(_plantsContent, "<i>no plants yet</i>", 22f,
                new Color(0.5f, 0.5f, 0.5f, 0.6f), theme, 40f);
        }
    }

    // ─────────────────── Books ───────────────────

    private void RefreshBooks()
    {
        if (_booksContent == null) return;
        PauseUIHelper.ClearChildren(_booksContent);
        var theme = IrisTextTheme.Active;

        bool anyBooks = false;
        var all = PlaceableObject.All;
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i] == null || all[i].Category != ItemCategory.Book) continue;
            var book = all[i].GetComponent<BookItem>();
            if (book == null || book.Definition == null) continue;

            if (!anyBooks)
            {
                PauseUIHelper.CreateHeaderLabel(_booksContent, "bookshelf", theme);
                anyBooks = true;
            }

            var def = book.Definition;
            string hidden = def.hasHiddenItem ? "  <color=#CCAA66>(has hidden item)</color>" : "";
            PauseUIHelper.CreateLabel(_booksContent,
                $"  <b>{def.title}</b>  by {def.author}{hidden}", 16f,
                new Color(0.7f, 0.68f, 0.65f), theme, 24f);

            string prefs = GetWhoLikesThis(def.reactionTags);
            if (!string.IsNullOrEmpty(prefs))
                PauseUIHelper.CreateLabel(_booksContent, $"    {prefs}", 13f,
                    new Color(0.6f, 0.58f, 0.55f), theme, 18f);
        }

        if (!anyBooks)
        {
            PauseUIHelper.CreateLabel(_booksContent, "<i>no books found</i>", 22f,
                new Color(0.5f, 0.5f, 0.5f, 0.6f), theme, 40f);
        }
    }

    // ─────────────────── Drinks ───────────────────

    private void RefreshDrinks()
    {
        if (_drinksContent == null) return;
        PauseUIHelper.ClearChildren(_drinksContent);
        var theme = IrisTextTheme.Active;

        var recipes = Resources.FindObjectsOfTypeAll<DrinkRecipeDefinition>();
        if (recipes == null || recipes.Length == 0)
        {
            PauseUIHelper.CreateLabel(_drinksContent, "<i>no recipes available</i>", 22f,
                new Color(0.5f, 0.5f, 0.5f, 0.6f), theme, 40f);
            return;
        }

        PauseUIHelper.CreateHeaderLabel(_drinksContent, "recipes", theme);

        for (int i = 0; i < recipes.Length; i++)
        {
            var recipe = recipes[i];
            if (recipe == null) continue;

            // Build ingredient list
            string ingredients = "";
            if (recipe.ingredients != null && recipe.ingredients.Length > 0)
            {
                var names = new List<string>();
                for (int j = 0; j < recipe.ingredients.Length; j++)
                {
                    if (recipe.ingredients[j] != null)
                        names.Add(recipe.ingredients[j].ingredientName);
                }
                ingredients = string.Join(", ", names);
            }

            string glass = recipe.requiredGlass != null ? recipe.requiredGlass.glassName : "";

            PauseUIHelper.CreateLabel(_drinksContent,
                $"  <b>{recipe.drinkName}</b>", 16f,
                new Color(0.7f, 0.68f, 0.65f), theme, 24f);

            string details = "";
            if (!string.IsNullOrEmpty(glass)) details += glass;
            if (!string.IsNullOrEmpty(ingredients))
            {
                if (details.Length > 0) details += "  |  ";
                details += ingredients;
            }
            if (!string.IsNullOrEmpty(details))
                PauseUIHelper.CreateLabel(_drinksContent, $"    <color=#888888>{details}</color>", 13f,
                    new Color(0.6f, 0.58f, 0.55f), theme, 18f);
        }
    }

    // ─────────────────── Decor ───────────────────

    private void RefreshDecor()
    {
        if (_decorContent == null) return;
        PauseUIHelper.ClearChildren(_decorContent);
        var theme = IrisTextTheme.Active;

        bool anyDecor = false;
        var all = PlaceableObject.All;
        for (int i = 0; i < all.Count; i++)
        {
            var obj = all[i];
            if (obj == null) continue;
            if (!obj.CanWallMount && !obj.WallOnly) continue;

            if (!anyDecor)
            {
                PauseUIHelper.CreateHeaderLabel(_decorContent, "wall decor", theme);
                anyDecor = true;
            }

            string name = obj.gameObject.name;
            string atHome = obj.IsAtHome ? "<color=#88CC88>placed</color>" : "<color=#CCAA66>not placed</color>";
            PauseUIHelper.CreateLabel(_decorContent,
                $"  {name}  {atHome}", 16f,
                new Color(0.7f, 0.68f, 0.65f), theme, 24f);

            string prefs = GetWhoLikesThis(GetReactableTagsForObject(obj.gameObject));
            if (!string.IsNullOrEmpty(prefs))
                PauseUIHelper.CreateLabel(_decorContent, $"    {prefs}", 13f,
                    new Color(0.6f, 0.58f, 0.55f), theme, 18f);
        }

        if (!anyDecor)
        {
            PauseUIHelper.CreateLabel(_decorContent, "<i>no wall decor</i>", 22f,
                new Color(0.5f, 0.5f, 0.5f, 0.6f), theme, 40f);
        }
    }

    // ─────────────────── Key Items ───────────────────

    private void RefreshKeyItems()
    {
        if (_keyItemsContent == null) return;
        PauseUIHelper.ClearChildren(_keyItemsContent);
        var theme = IrisTextTheme.Active;

        // Gunpla
        var gunpla = GunplaFigure.All;
        if (gunpla != null && gunpla.Count > 0)
        {
            PauseUIHelper.CreateHeaderLabel(_keyItemsContent, "gunpla", theme);
            for (int i = 0; i < gunpla.Count; i++)
            {
                var fig = gunpla[i];
                if (fig == null) continue;

                string status = fig.IsComplete ? "<color=#88CC88>complete</color>"
                    : $"sword: {(fig.HasSword ? "\u2713" : "\u2717")}  wings: {(fig.HasWings ? "\u2713" : "\u2717")}";
                PauseUIHelper.CreateLabel(_keyItemsContent,
                    $"  gunpla figure  {status}", 16f,
                    new Color(0.7f, 0.68f, 0.65f), theme, 24f);
            }
        }

        // Perfume
        var perfumes = PerfumeBottle.All;
        if (perfumes != null && perfumes.Count > 0)
        {
            PauseUIHelper.CreateLabel(_keyItemsContent, "", 8f, Color.clear, theme, 8f);
            PauseUIHelper.CreateHeaderLabel(_keyItemsContent, "perfume", theme);
            for (int i = 0; i < perfumes.Count; i++)
            {
                var bottle = perfumes[i];
                if (bottle == null || bottle.Definition == null) continue;

                var def = bottle.Definition;
                string tag = !string.IsNullOrEmpty(def.perfumeTag) ? $"  ({def.perfumeTag})" : "";
                PauseUIHelper.CreateLabel(_keyItemsContent,
                    $"  <b>{def.perfumeName}</b>{tag}", 16f,
                    new Color(0.7f, 0.68f, 0.65f), theme, 24f);

                if (!string.IsNullOrEmpty(def.description))
                    PauseUIHelper.CreateLabel(_keyItemsContent,
                        $"    {def.description}", 13f,
                        new Color(0.6f, 0.58f, 0.55f), theme, 18f);

                // Check who likes this perfume tag
                if (!string.IsNullOrEmpty(def.perfumeTag))
                {
                    string prefs = GetWhoLikesThis(new[] { def.perfumeTag });
                    if (!string.IsNullOrEmpty(prefs))
                        PauseUIHelper.CreateLabel(_keyItemsContent, $"    {prefs}", 13f,
                            new Color(0.6f, 0.58f, 0.55f), theme, 18f);
                }
            }
        }

        // Disco bulbs
        var bulbs = Resources.FindObjectsOfTypeAll<DiscoBulbDefinition>();
        if (bulbs != null && bulbs.Length > 0)
        {
            PauseUIHelper.CreateLabel(_keyItemsContent, "", 8f, Color.clear, theme, 8f);
            PauseUIHelper.CreateHeaderLabel(_keyItemsContent, "disco bulbs", theme);
            for (int i = 0; i < bulbs.Length; i++)
            {
                var bulb = bulbs[i];
                if (bulb == null) continue;

                string pattern = bulb.pattern.ToString().ToLower();
                PauseUIHelper.CreateLabel(_keyItemsContent,
                    $"  <b>{bulb.bulbName}</b>  ({pattern})", 16f,
                    new Color(0.7f, 0.68f, 0.65f), theme, 24f);

                string prefs = GetWhoLikesThis(bulb.reactionTags);
                if (!string.IsNullOrEmpty(prefs))
                    PauseUIHelper.CreateLabel(_keyItemsContent, $"    {prefs}", 13f,
                        new Color(0.6f, 0.58f, 0.55f), theme, 18f);
            }
        }

        if ((gunpla == null || gunpla.Count == 0) &&
            (perfumes == null || perfumes.Count == 0) &&
            (bulbs == null || bulbs.Length == 0))
        {
            PauseUIHelper.CreateLabel(_keyItemsContent, "<i>no key items</i>", 22f,
                new Color(0.5f, 0.5f, 0.5f, 0.6f), theme, 40f);
        }
    }

    // ─────────────────── Mail ───────────────────

    private void RefreshMail()
    {
        if (_mailContent == null) return;
        PauseUIHelper.ClearChildren(_mailContent);
        var theme = IrisTextTheme.Active;

        var mail = MailInventory.All;
        if (mail == null || mail.Count == 0)
        {
            PauseUIHelper.CreateLabel(_mailContent, "<i>no mail collected</i>", 22f,
                new Color(0.5f, 0.5f, 0.5f, 0.6f), theme, 40f);
            return;
        }

        // Show newest first
        for (int i = mail.Count - 1; i >= 0; i--)
        {
            var m = mail[i];

            string typeStr = m.type switch
            {
                MailItemType.Letter => "letter",
                MailItemType.Package => "package",
                MailItemType.Catalog => "catalog",
                _ => "mail"
            };
            string readMark = m.wasRead ? "" : "  <color=#CCAA66>(unread)</color>";
            Color headerColor = m.wasRead
                ? new Color(0.7f, 0.68f, 0.65f)
                : new Color(0.85f, 0.82f, 0.78f);

            PauseUIHelper.CreateLabel(_mailContent,
                $"<b>{typeStr} from {m.senderName}</b>  day {m.dayReceived}{readMark}", 16f,
                headerColor, theme, 24f);

            // Show mail text (first few lines)
            if (m.textLines != null)
            {
                int maxLines = Mathf.Min(m.textLines.Length, 3);
                for (int j = 0; j < maxLines; j++)
                {
                    PauseUIHelper.CreateLabel(_mailContent,
                        $"  {m.textLines[j]}", 13f,
                        new Color(0.6f, 0.58f, 0.55f), theme, 18f);
                }
                if (m.textLines.Length > 3)
                    PauseUIHelper.CreateLabel(_mailContent,
                        "  <color=#555555>...</color>", 13f,
                        new Color(0.5f, 0.5f, 0.5f), theme, 16f);
            }

            if (!string.IsNullOrEmpty(m.itemDisplayName))
                PauseUIHelper.CreateLabel(_mailContent,
                    $"  <color=#CCAA66>contained: {m.itemDisplayName}</color>", 13f,
                    new Color(0.6f, 0.58f, 0.55f), theme, 18f);

            PauseUIHelper.CreateLabel(_mailContent, "", 6f, Color.clear, theme, 6f);
        }
    }

    // ─────────────────── Helpers ───────────────────

    private static string Bar(float value)
    {
        int filled = Mathf.RoundToInt(value * 10);
        return new string('\u2588', filled) + new string('\u2591', 10 - filled);
    }

    // ─────────────────── Build UI ───────────────────

    private void BuildUI()
    {
        _built = true;
        PauseUIHelper.EnsureFullStretch(gameObject);

        // Tab bar
        var tabBarGO = new GameObject("TabBar");
        tabBarGO.transform.SetParent(transform, false);
        var tabBarRT = tabBarGO.AddComponent<RectTransform>();
        tabBarRT.anchorMin = new Vector2(0f, 1f);
        tabBarRT.anchorMax = new Vector2(1f, 1f);
        tabBarRT.pivot = new Vector2(0.5f, 1f);
        tabBarRT.anchoredPosition = new Vector2(0f, -5f);
        tabBarRT.sizeDelta = new Vector2(-20f, 32f);
        _tabBar = tabBarGO.AddComponent<PauseTabBar>();

        // Tab panels
        var tabNames = new[] { "plants", "books", "drinks", "decor", "key items", "mail" };
        var panels = new GameObject[6];
        var contents = new Transform[6];

        for (int i = 0; i < 6; i++)
        {
            var panelGO = new GameObject($"Panel_{tabNames[i]}");
            panelGO.transform.SetParent(transform, false);
            var panelRT = panelGO.AddComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = new Vector2(10f, 10f);
            panelRT.offsetMax = new Vector2(-10f, -45f);
            panels[i] = panelGO;

            contents[i] = PauseUIHelper.CreateScrollableList(panelGO.transform,
                new RectOffset(0, 0, 0, 0));
        }

        _plantsContent = contents[0];
        _booksContent = contents[1];
        _drinksContent = contents[2];
        _decorContent = contents[3];
        _keyItemsContent = contents[4];
        _mailContent = contents[5];

        _tabBar.OnTabChanged += _ => RefreshCurrentTab();
        _tabBar.Initialize(tabNames, panels);
    }
}

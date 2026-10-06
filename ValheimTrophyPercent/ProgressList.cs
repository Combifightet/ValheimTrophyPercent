using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ValheimTrophyPercent
{
    public static class ProgressList
    {
        private static GameObject PinnedPanel;
        private static RectTransform Container;

        [HarmonyPatch(typeof(Hud), "Awake")]
        public static class Hud_Awake_Patch
        {
            public static void Postfix(Hud __instance)
            {
                if (__instance.m_rootObject == null) return;

                // Create the base UI panel and attach it directly to the HUD root
                PinnedPanel = new GameObject("PinnedTrophiesPanel");
                PinnedPanel.transform.SetParent(__instance.m_rootObject.transform, false);
                
                Container = PinnedPanel.AddComponent<RectTransform>();
                
                // Anchor to Top-Right of the screen
                Container.anchorMin = new Vector2(1, 1);
                Container.anchorMax = new Vector2(1, 1);
                Container.pivot = new Vector2(1, 1);
                
                // Offset down by 250 pixels (below the minimap's default footprint) and 20 pixels left
                Container.anchoredPosition = new Vector2(-30, -260);
                Container.sizeDelta = new Vector2(250, 400);

                VerticalLayoutGroup layout = PinnedPanel.AddComponent<VerticalLayoutGroup>();
                layout.childAlignment = TextAnchor.UpperRight;
                layout.spacing = 5f;

                UpdatePinnedUI();
            }
        }

        public static void UpdatePinnedUI()
        {
            if (PinnedPanel == null || ObjectDB.instance == null) return;

            foreach (Transform child in PinnedPanel.transform)
            {
                UnityEngine.Object.Destroy(child.gameObject);
            }

            foreach (string trophyPrefab in TrophyManager.PinnedTrophies)
            {
                GameObject prefab = ObjectDB.instance.GetItemPrefab(trophyPrefab);
                if (prefab == null) continue;

                ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
                string localizedName = Localization.instance.Localize(itemDrop.m_itemData.m_shared.m_name);
                Sprite icon = itemDrop.m_itemData.GetIcon();

                CreateRow(trophyPrefab, localizedName, icon);
            }
        }

        private static void CreateRow(string trophyPrefab, string localizedName, Sprite icon)
        {
            GameObject row = new GameObject("PinnedRow");
            row.transform.SetParent(PinnedPanel.transform, false);
            RectTransform rowRect = row.AddComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(250, 30);
            
            HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 10f;
            rowLayout.childAlignment = TextAnchor.MiddleRight;
            rowLayout.childControlWidth = false;

            // Calculate formatted text: e.g. "1/5 Neck"
            string enemyCleanName = "";
            float dropChance = 1f;
            if (TrophyManager.TrophyToEnemy.TryGetValue(trophyPrefab, out string en)) enemyCleanName = en;
            if (TrophyManager.TrophyDropChances.TryGetValue(trophyPrefab, out float dc)) dropChance = dc;

            int kills = string.IsNullOrEmpty(enemyCleanName) ? 0 : TrophyManager.GetKillCount(enemyCleanName);
            int expected = dropChance > 0 ? Mathf.RoundToInt(1f / dropChance) : 1;
            string displayText = $"{kills}/{expected} {localizedName}";

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(row.transform, false);
            TMP_Text text = textObj.AddComponent<TextMeshProUGUI>();
            if (TrophyManager.ValheimFont != null) text.font = TrophyManager.ValheimFont; // <-- Applied Font Fix
            
            text.text = displayText;
            text.fontSize = 18;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Right;
            text.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 30);

            // Add Icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(row.transform, false);
            Image img = iconObj.AddComponent<Image>();
            img.sprite = icon;
            iconObj.GetComponent<RectTransform>().sizeDelta = new Vector2(25, 25);
        }
    }
}
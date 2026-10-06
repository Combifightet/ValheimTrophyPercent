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

                PinnedPanel = new GameObject("PinnedTrophiesPanel");
                PinnedPanel.transform.SetParent(__instance.m_rootObject.transform, false);
                
                Container = PinnedPanel.AddComponent<RectTransform>();
                
                // Anchor to Top-Right of the screen
                Container.anchorMin = new Vector2(1, 1);
                Container.anchorMax = new Vector2(1, 1);
                Container.pivot = new Vector2(1, 1);
                
                // Offset down by 250 pixels (below the minimap's default footprint) and 20 pixels left
                Container.anchoredPosition = new Vector2(-30, -260);
                Container.sizeDelta = new Vector2(250, 0); // Allow it to shrink

                VerticalLayoutGroup layout = PinnedPanel.AddComponent<VerticalLayoutGroup>();
                layout.childAlignment = TextAnchor.UpperRight;
                layout.spacing = 2f;
                // Stop the layout from spreading elements across the screen
                layout.childControlHeight = true;
                layout.childControlWidth = true;
                layout.childForceExpandHeight = false; 
                layout.childForceExpandWidth = false;

                // Add size fitter so the list tightens up based on rows
                ContentSizeFitter fitter = PinnedPanel.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.MinSize;

                UpdatePinnedUI();
            }
        }

        // Add an update patch to keep the pinned list in sync with your kills automatically
        [HarmonyPatch(typeof(Hud), "Update")]
        public static class Hud_Update_Patch
        {
            private static float updateTimer = 0f;
            public static void Postfix()
            {
                if (PinnedPanel == null || !PinnedPanel.activeInHierarchy) return;
                
                updateTimer += Time.deltaTime;
                if (updateTimer > 2f) // Refresh kills every 2 seconds
                {
                    updateTimer = 0f;
                    UpdatePinnedUI();
                }
            }
        }

        public static void UpdatePinnedUI()
        {
            if (PinnedPanel == null || ObjectDB.instance == null || Hud.instance == null) return;
            
            TrophyManager.CacheEnemyData();
            // Don't generate UI if cache isn't ready (prevents 0/1 bug when loading into world)
            if (TrophyManager.TrophyToEnemy.Count == 0) return;

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
            
            LayoutElement layoutElement = row.AddComponent<LayoutElement>();
            layoutElement.minHeight = 30f;
            layoutElement.minWidth = 250f;

            HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 10f;
            rowLayout.childAlignment = TextAnchor.MiddleRight;
            
            // disable forced expansion to prevent the icon from stretching vertically
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = false; 
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            string enemyKey = "";
            float dropChance = 1f;
            if (TrophyManager.TrophyToEnemy.TryGetValue(trophyPrefab, out string en)) enemyKey = en;
            if (TrophyManager.TrophyDropChances.TryGetValue(trophyPrefab, out float dc)) dropChance = dc;

            int kills = string.IsNullOrEmpty(enemyKey) ? 0 : TrophyManager.GetKillCount(enemyKey);
            int expected = dropChance > 0 ? Mathf.RoundToInt(1f / dropChance) : 1;
            
            string displayText = $"{kills} / {expected} {localizedName}";

            GameObject textObj = UnityEngine.Object.Instantiate(Hud.instance.m_hoverName.gameObject, row.transform);
            textObj.name = "Text";
            
            TMP_Text text = textObj.GetComponent<TMP_Text>();
            text.text = displayText;
            text.fontSize = 18;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Right;
            text.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 30);
            textObj.SetActive(true);

            // Add Icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(row.transform, false);
            Image img = iconObj.AddComponent<Image>();
            img.sprite = icon;
            img.preserveAspect = true;
            iconObj.GetComponent<RectTransform>().sizeDelta = new Vector2(25, 25);
        }
    }
}
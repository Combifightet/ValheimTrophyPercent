using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace ValheimTrophyPercent
{
    [HarmonyPatch(typeof(InventoryGui), "UpdateTrophyList")] 
    public static class InventoryGui_UpdateTrophyList_Patch
    {
        public static void Postfix(InventoryGui __instance, ref List<GameObject> ___m_trophyList, RectTransform ___m_trophieListRoot, GameObject ___m_trophieElementPrefab, float ___m_trophieListSpace)
        {
            if (Player.m_localPlayer == null) return;

            // fallback font grab just in case
            if (TrophyManager.ValheimFont == null && ___m_trophieElementPrefab != null)
            {
                TMP_Text templateText = ___m_trophieElementPrefab.transform.Find("name").GetComponent<TMP_Text>();
                if (templateText != null) TrophyManager.ValheimFont = templateText.font;
            }

            TrophyManager.CacheEnemyData();
            List<string> collected = Player.m_localPlayer.GetTrophies();
            int total = TrophyManager.GetTotalTrophies();
            
            // 1. Add Percentage/Counter UI
            AddProgressText(___m_trophieListRoot, collected.Count, total);

            // 2. Add Toggle Buttons to EXISTING collected trophies
            for (int i = 0; i < collected.Count; i++)
            {
                GameObject uiElement = ___m_trophyList[i];
                string trophyPrefabName = collected[i];
                AddPinButtonToElement(uiElement, trophyPrefabName);
            }

            // 3. Add "Killed but Missing" Trophies
            List<string> missing = TrophyManager.GetKilledButMissingTrophies();
            float lowestY = 0f;

            foreach (string missingTrophy in missing)
            {
                GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(missingTrophy);
                if (itemPrefab == null) continue;

                ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
                GameObject gameObject = UnityEngine.Object.Instantiate(___m_trophieElementPrefab, ___m_trophieListRoot);
                gameObject.SetActive(true);
                
                RectTransform rectTransform = gameObject.transform as RectTransform;
                
                // Position logic based on vanilla grid
                rectTransform.anchoredPosition = new Vector2(
                    (float)component.m_itemData.m_shared.m_trophyPos.x * ___m_trophieListSpace, 
                    (float)component.m_itemData.m_shared.m_trophyPos.y * -___m_trophieListSpace
                );
                
                lowestY = Mathf.Min(lowestY, rectTransform.anchoredPosition.y - ___m_trophieListSpace);
                string locName = Localization.instance.Localize(component.m_itemData.m_shared.m_name);
                
                // Set Icon and tint it dark/gray to indicate it's missing
                Image iconImg = rectTransform.Find("icon_bkg/icon").GetComponent<Image>();
                iconImg.sprite = component.m_itemData.GetIcon();
                iconImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

                rectTransform.Find("name").GetComponent<TMP_Text>().text = locName + " <color=red>(Missing)</color>";
                rectTransform.Find("description").GetComponent<TMP_Text>().text = "You have slain this beast, but the trophy eludes you...";

                ___m_trophyList.Add(gameObject);
                
                // Add pin button to missing trophies as well
                AddPinButtonToElement(gameObject, missingTrophy);
            }

            // Resize the container to fit the newly added elements
            float finalSize = Mathf.Max(0f, -lowestY);
            ___m_trophieListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, finalSize + 100f);
        }

        private static void AddProgressText(RectTransform root, int collected, int total)
        {
            // Create a simple text element at the top left of the root
            GameObject textObj = new GameObject("ProgressText");
            textObj.transform.SetParent(root.parent, false); // Attach to parent so it doesn't scroll away, or root if you want it to scroll
            
            RectTransform rt = textObj.AddComponent<RectTransform>();
            // FIX: Set a proper Top-Left Pivot and Size Delta to prevent the text from clipping 
            rt.pivot = new Vector2(0, 1); 
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(400, 40); 
            rt.anchoredPosition = new Vector2(10, -5); 

            TMP_Text text = textObj.AddComponent<TextMeshProUGUI>();
            if (TrophyManager.ValheimFont != null) text.font = TrophyManager.ValheimFont;
            
            // FIX: Allow text to overflow its container rather than vanishing
            text.overflowMode = TextOverflowModes.Overflow; 
            text.enableWordWrapping = false;
            text.text = $"<color=orange>Trophies:</color> {collected} / {total}";
            text.fontSize = 24;
        }

        private static void AddPinButtonToElement(GameObject uiElement, string trophyPrefabName)
        {
            // Create a small UI Button in the corner of the trophy element
            GameObject btnObj = new GameObject("PinButton");
            btnObj.transform.SetParent(uiElement.transform, false);
            
            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1, 1);
            btnRect.anchorMax = new Vector2(1, 1);
            btnRect.anchoredPosition = new Vector2(-15, -15);
            btnRect.sizeDelta = new Vector2(30, 30);

            Image img = btnObj.AddComponent<Image>();
            img.color = TrophyManager.PinnedTrophies.Contains(trophyPrefabName) ? Color.green : Color.gray;

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => 
            {
                TrophyManager.TogglePin(trophyPrefabName);
                // Update button color visually immediately
                img.color = TrophyManager.PinnedTrophies.Contains(trophyPrefabName) ? Color.green : Color.gray;
            });

            // Add text to the button
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            TMP_Text txt = textObj.AddComponent<TextMeshProUGUI>();
            if (TrophyManager.ValheimFont != null) txt.font = TrophyManager.ValheimFont;
            txt.text = "*"; 
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.Center;
        }
    }
}
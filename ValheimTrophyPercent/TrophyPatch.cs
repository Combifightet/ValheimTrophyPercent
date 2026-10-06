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
            if (Player.m_localPlayer == null || ___m_trophieElementPrefab == null) return;

            // Gather Valheim's internal data and sprites
            TrophyManager.LoadToggleSprites();
            TrophyManager.CacheEnemyData();

            List<string> collected = Player.m_localPlayer.GetTrophies();
            int total = TrophyManager.GetTotalTrophies();
            
			// 1. Add Percentage/Counter UI
            AddProgressText(___m_trophieListRoot, ___m_trophieElementPrefab, collected.Count, total);

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

        private static void AddProgressText(RectTransform root, GameObject templatePrefab, int collected, int total)
        {
            // clone the template text object to prevent Font Asset warnings
            GameObject templateText = templatePrefab.transform.Find("name").gameObject;
            GameObject textObj = UnityEngine.Object.Instantiate(templateText, root.parent);
            textObj.name = "ProgressText";
            
            RectTransform rt = textObj.GetComponent<RectTransform>();
            rt.pivot = new Vector2(0, 1); 
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(400, 40); 
            rt.anchoredPosition = new Vector2(10, -5); 

            TMP_Text text = textObj.GetComponent<TMP_Text>();
            text.overflowMode = TextOverflowModes.Overflow; 
            text.enableWordWrapping = false;
            text.text = $"<color=orange>Trophies:</color> {collected} / {total}";
            text.fontSize = 24;
            text.alignment = TextAlignmentOptions.TopLeft;
        }

        private static void AddPinButtonToElement(GameObject uiElement, string trophyPrefabName)
        {
            // create a small UI Button in the corner of the trophy element
            GameObject btnObj = new GameObject("PinButton");
            btnObj.transform.SetParent(uiElement.transform, false);
            
            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1, 1);
            btnRect.anchorMax = new Vector2(1, 1);
            btnRect.anchoredPosition = new Vector2(-22, -22); // Shifted slightly for a 28x28 box
            btnRect.sizeDelta = new Vector2(28, 28); // Valheim checkboxes are typically 28x28

            // 1. The Background Box
            Image bgImg = btnObj.AddComponent<Image>();
            if (TrophyManager.CheckboxBackground != null) bgImg.sprite = TrophyManager.CheckboxBackground;
            else bgImg.color = new Color(0, 0, 0, 0.5f); 

            // Make the button interactable
            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bgImg;

            // 2. The Checkmark 
            GameObject checkObj = new GameObject("Checkmark");
            checkObj.transform.SetParent(btnObj.transform, false);
            RectTransform checkRect = checkObj.AddComponent<RectTransform>();
            checkRect.anchorMin = Vector2.zero;
            checkRect.anchorMax = Vector2.one;
            checkRect.sizeDelta = Vector2.zero; 
            checkRect.anchoredPosition = Vector2.zero;
            
            Image checkImg = checkObj.AddComponent<Image>();
            if (TrophyManager.Checkmark != null) checkImg.sprite = TrophyManager.Checkmark;
            
            // Set a nice bronze/gold color for the checkmark
            checkImg.color = new Color(0.8f, 0.5f, 0.2f, 1f); 
            checkImg.enabled = TrophyManager.PinnedTrophies.Contains(trophyPrefabName);

            // Toggle state on click
            btn.onClick.AddListener(() => 
            {
                TrophyManager.TogglePin(trophyPrefabName);
                checkImg.enabled = TrophyManager.PinnedTrophies.Contains(trophyPrefabName);
            });

            // 3. Add Valheim's Native Hover Tooltip
            UITooltip tooltip = btnObj.AddComponent<UITooltip>();
            tooltip.m_topic = "Track Trophy";
            tooltip.m_text = "Pin this trophy's progress to your HUD.";
            if (TrophyManager.DefaultTooltipPrefab != null)
            {
                tooltip.m_tooltipPrefab = TrophyManager.DefaultTooltipPrefab;
            }
        }
    }
}
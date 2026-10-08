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
            
            Transform panel = __instance.m_trophiesPanel.transform;

            // 1. Append Percentage/Counter UI cleanly to the native title text instead of floating over trophies
            string localizedTitle = Localization.instance.Localize("$inventory_trophies");
            TMP_Text titleText = null;
            
            // Deep search to find the header text regardless of internal prefab naming
            foreach (TMP_Text t in panel.GetComponentsInChildren<TMP_Text>(true))
            {
                string tName = t.name.ToLower();
                if (tName == "topic" || tName == "title" || tName == "text_title")
                {
                    titleText = t;
                    break;
                }
                if (t.text.StartsWith(localizedTitle) || t.text.StartsWith(localizedTitle.ToUpper()))
                {
                    titleText = t;
                    break;
                }
            }

            if (titleText != null)
            {
                // Disable wrapping so the appended text isn't cut off horizontally
                titleText.overflowMode = TextOverflowModes.Overflow;
                titleText.enableWordWrapping = false;
                
                string baseText = localizedTitle;
                if (titleText.text.StartsWith(localizedTitle.ToUpper()))
                    baseText = localizedTitle.ToUpper();

                titleText.text = $"{baseText} <color=orange>({collected.Count}/{total})</color>";
            }

            // 1b. Inject the "Clear All" button in the bottom left
            AddClearAllButton(panel);

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

        private static void AddClearAllButton(Transform panel)
        {
            if (panel.Find("ClearAllButton") != null) return;

            // Try to find ANY existing button to steal its Valheim styling (Wood Sprite and Font)
            Button templateBtn = panel.GetComponentInChildren<Button>(true);
            Sprite btnSprite = null;
            TMP_FontAsset btnFont = TrophyManager.GetValheimFont();

            if (templateBtn != null)
            {
                Image templateImg = templateBtn.GetComponent<Image>();
                if (templateImg != null) btnSprite = templateImg.sprite;
                
                TMP_Text templateTxt = templateBtn.GetComponentInChildren<TMP_Text>(true);
                if (templateTxt != null) btnFont = templateTxt.font;
            }

            // Create from complete scratch to avoid ALL Valheim/ZenUI prefab quirks, masks, and layout groups
            GameObject clearBtnObj = new GameObject("ClearAllButton");
            clearBtnObj.transform.SetParent(panel, false);
            clearBtnObj.transform.SetAsLastSibling(); // Ensure it draws on top of the background

            RectTransform rt = clearBtnObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.sizeDelta = new Vector2(140f, 40f);
            
            // Hardcode to 30 pixels from the left and 20 pixels from the bottom
            rt.anchoredPosition = new Vector2(30f, 20f);

            // Add LayoutElement with ignoreLayout to bypass any auto-layouts (Crucial for ZenUI compatibility)
            LayoutElement layout = clearBtnObj.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;

            // Apply stolen Valheim visuals
            Image img = clearBtnObj.AddComponent<Image>();
            img.type = Image.Type.Sliced;
            if (btnSprite != null) img.sprite = btnSprite;
            else img.color = new Color(0.15f, 0.15f, 0.15f, 1f); // Fallback to dark grey if sprite fails

            // Make it clickable
            Button btn = clearBtnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => 
            {
                TrophyManager.PinnedTrophies.Clear();
                ValheimMod.Instance.SavePinnedConfig();
                ProgressList.UpdatePinnedUI();
                
                if (InventoryGui.instance != null)
                {
                    AccessTools.Method(typeof(InventoryGui), "UpdateTrophyList")?.Invoke(InventoryGui.instance, null);
                }
            });

            // Add Text child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(clearBtnObj.transform, false);
            
            RectTransform textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;
            textRt.anchoredPosition = Vector2.zero;

            TextMeshProUGUI txt = textObj.AddComponent<TextMeshProUGUI>();
            txt.text = "Clear All";
            if (btnFont != null) txt.font = btnFont;
            txt.fontSize = 20;
            txt.color = new Color(1f, 0.7f, 0.2f); // Valheim UI orange/gold
            txt.alignment = TextAlignmentOptions.Center;
            txt.enableWordWrapping = false;
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
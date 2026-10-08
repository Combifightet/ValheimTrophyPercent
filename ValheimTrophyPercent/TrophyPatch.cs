using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

namespace ValheimTrophyPercent
{
    // Container object for re-sorting the UI elements 
    public class TrophyItemData
    {
        public GameObject GameObject;
        public string PrefabName;
        public int X;
        public int Y;
    }

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

            List<TrophyItemData> allTrophies = new List<TrophyItemData>();

            // 2. Add Toggle Buttons to EXISTING collected trophies and extract sorting data
            for (int i = 0; i < collected.Count; i++)
            {
                GameObject uiElement = ___m_trophyList[i];
                string trophyPrefabName = collected[i];
                
                GameObject prefab = ObjectDB.instance.GetItemPrefab(trophyPrefabName);
                if (prefab != null)
                {
                    ItemDrop drop = prefab.GetComponent<ItemDrop>();
                    Vector2Int pos = drop.m_itemData.m_shared.m_trophyPos;
                    allTrophies.Add(new TrophyItemData { GameObject = uiElement, PrefabName = trophyPrefabName, X = pos.x, Y = pos.y });
                }

                AddPinButtonToElement(uiElement, trophyPrefabName);
            }

            // 3. Add "Killed but Missing" Trophies
            List<string> missing = TrophyManager.GetKilledButMissingTrophies();

            foreach (string missingTrophy in missing)
            {
                GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(missingTrophy);
                if (itemPrefab == null) continue;

                ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
                GameObject gameObject = UnityEngine.Object.Instantiate(___m_trophieElementPrefab, ___m_trophieListRoot);
                gameObject.SetActive(true);
                
                string locName = Localization.instance.Localize(component.m_itemData.m_shared.m_name);
                
                // Set Icon and tint it dark/gray to indicate it's missing
                Image iconImg = gameObject.transform.Find("icon_bkg/icon").GetComponent<Image>();
                iconImg.sprite = component.m_itemData.GetIcon();
                iconImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

                gameObject.transform.Find("name").GetComponent<TMP_Text>().text = locName + " <color=red>(Missing)</color>";
                gameObject.transform.Find("description").GetComponent<TMP_Text>().text = "You have slain this beast, but the trophy eludes you...";

                ___m_trophyList.Add(gameObject);
                AddPinButtonToElement(gameObject, missingTrophy);

                Vector2Int pos = component.m_itemData.m_shared.m_trophyPos;
                allTrophies.Add(new TrophyItemData { GameObject = gameObject, PrefabName = missingTrophy, X = pos.x, Y = pos.y });
            }

            // 4. Custom Grid & Wrapping Layout
            // Remove any dynamically created headers from a previous opening
            foreach (Transform child in ___m_trophieListRoot)
            {
                if (child.name == "BiomeHeader") UnityEngine.Object.Destroy(child.gameObject);
            }

            // Group the items by their internal Y-axis value (which Valheim uses to separate biomes)
            var groupedTrophies = allTrophies.GroupBy(t => t.Y).OrderBy(g => g.Key);
            
            float currentY = -20f; 
            float elementSpace = ___m_trophieListSpace;
            // Provide a sensible fallback if the rect hasn't generated its physical boundaries yet
            float maxWidth = ___m_trophieListRoot.rect.width > 100f ? ___m_trophieListRoot.rect.width - 20f : 650f;

            foreach (var group in groupedTrophies)
            {
                CreateBiomeHeader(___m_trophieListRoot, GetBiomeName(group.Key), currentY);
                currentY -= 40f; 
                
                float currentX = 0f;
                // Lay out elements left-to-right based on internal X sorting
                foreach (var item in group.OrderBy(t => t.X))
                {
                    // Detect if the trophy will go out of bounds and wrap it to a new row
                    if (currentX + elementSpace > maxWidth)
                    {
                        currentX = 0f;
                        currentY -= elementSpace;
                    }
                    
                    RectTransform rt = item.GameObject.transform as RectTransform;
                    rt.anchoredPosition = new Vector2(currentX, currentY);
                    
                    currentX += elementSpace;
                }
                
                // Finalize row spacing before moving to the next biome block
                currentY -= elementSpace; 
            }

            // Resize the container to fit the dynamically rebuilt elements
            float finalSize = Mathf.Abs(currentY) + 100f;
            ___m_trophieListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, finalSize);
        }

        private static string GetBiomeName(int y)
        {
            switch (y)
            {
                case 0: return "Meadows";
                case 1: return "Black Forest";
                case 2: return "Swamp";
                case 3: return "Mountains";
                case 4: return "Plains";
                case 5: return "Ocean";
                case 6: return "Mistlands";
                case 7: return "Ashlands";
                case 8: return "Deep North";
                default: return "Events & Other";
            }
        }

        private static void CreateBiomeHeader(RectTransform root, string title, float yPos)
        {
            GameObject headerObj = new GameObject("BiomeHeader");
            headerObj.transform.SetParent(root, false);
            
            RectTransform rt = headerObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0, 1);
            
            // Indent the title slightly
            rt.anchoredPosition = new Vector2(10f, yPos);
            rt.sizeDelta = new Vector2(0, 30);
            
            TextMeshProUGUI txt = headerObj.AddComponent<TextMeshProUGUI>();
            txt.text = title;
            txt.font = TrophyManager.GetValheimFont();
            txt.fontSize = 24;
            txt.color = new Color(1f, 0.7f, 0.2f);
            txt.alignment = TextAlignmentOptions.Left;
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
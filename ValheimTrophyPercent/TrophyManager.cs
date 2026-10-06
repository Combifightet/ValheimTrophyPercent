using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ValheimTrophyPercent
{
    public static class TrophyManager
    {
        // Store our pinned trophy prefab names (e.g., "TrophyBoar")
        public static HashSet<string> PinnedTrophies = new HashSet<string>();

        // Caches the total number of trophies in the game
        public static int TotalTrophiesInGame = -1;
        public static TMP_FontAsset ValheimFont;

        // caches to map "TrophyBoar" -> "Boar" and its drop chance (e.g. 0.15)
        public static Dictionary<string, string> TrophyToEnemy = new Dictionary<string, string>();
        public static Dictionary<string, float> TrophyDropChances = new Dictionary<string, float>();

        // New cached sprites for our native checkbox look
        public static Sprite CheckboxBackground;
        public static Sprite Checkmark;

        // Searches memory for an authentic Valheim checkbox and steals its sprites
        public static void LoadToggleSprites()
        {
            if (CheckboxBackground != null && Checkmark != null) return;
            
            Toggle[] toggles = Resources.FindObjectsOfTypeAll<Toggle>();
            foreach (Toggle t in toggles)
            {
                if (t.targetGraphic != null && t.graphic != null)
                {
                    Image bg = t.targetGraphic as Image;
                    Image check = t.graphic as Image;
                    if (bg != null && check != null && bg.sprite != null && check.sprite != null)
                    {
                        CheckboxBackground = bg.sprite;
                        Checkmark = check.sprite;
                        return;
                    }
                }
            }
        }

        public static void LoadConfig(string configStr)
        {
            PinnedTrophies.Clear();
            if (!string.IsNullOrEmpty(configStr))
            {
                string[] parts = configStr.Split(',');
                foreach (string p in parts)
                {
                    if (!string.IsNullOrEmpty(p)) PinnedTrophies.Add(p);
                }
            }
        }

        public static TMP_FontAsset GetValheimFont()
        {
            if (ValheimFont == null)
            {
                TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                foreach (TMP_FontAsset font in fonts)
                {
                    if (font.name == "AveriaSerifLibre-Bold" || font.name == "AveriaSerifLibre-Light")
                    {
                        ValheimFont = font;
                        return ValheimFont;
                    }
                }
                if (fonts.Length > 0) ValheimFont = fonts[0];
            }
            return ValheimFont;
        }

        public static void TogglePin(string trophyPrefabName)
        {
            if (PinnedTrophies.Contains(trophyPrefabName))
                PinnedTrophies.Remove(trophyPrefabName);
            else
                PinnedTrophies.Add(trophyPrefabName);
            
            // Save immediately whenever the user clicks the checkbox
            ValheimMod.Instance.SavePinnedConfig();
            ProgressList.UpdatePinnedUI();
        }

        public static void CacheEnemyData()
        {
            if (TrophyToEnemy.Count > 0 || ZNetScene.instance == null) return;

            foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
            {
                CharacterDrop charDrop = prefab.GetComponent<CharacterDrop>();
                Character character = prefab.GetComponent<Character>();
                
                if (charDrop != null && character != null)
                {
                    // use m_name (e.g., "$enemy_neck") to match Valheim's internal kill tracker perfectly
                    string enemyKey = character.m_name; 

                    foreach (var drop in charDrop.m_drops)
                    {
                        if (drop.m_prefab == null) continue;
                        ItemDrop itemDrop = drop.m_prefab.GetComponent<ItemDrop>();
                        
                        if (itemDrop != null && itemDrop.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophy)
                        {
                            string trophyName = drop.m_prefab.name;
                            if (!TrophyToEnemy.ContainsKey(trophyName))
                            {
                                TrophyToEnemy[trophyName] = enemyKey;
                                TrophyDropChances[trophyName] = drop.m_chance;
                            }
                        }
                    }
                }
            }
        }

        public static int GetKillCount(string enemyKey)
        {
            if (Game.instance == null) return 0;
            PlayerProfile profile = Game.instance.GetPlayerProfile();
            
            // Only check the master aggregate bucket to prevent double counting
            if (profile.m_playerStats[0].m_enemyStats[0] != null && 
                profile.m_playerStats[0].m_enemyStats[0].TryGetValue(enemyKey, out float kCount))
            {
                return (int)kCount;
            }
            return 0;
        }

        public static int GetTotalTrophies()
        {
            if (TotalTrophiesInGame != -1) return TotalTrophiesInGame;

            TotalTrophiesInGame = 0;
            foreach (GameObject item in ObjectDB.instance.m_items)
            {
                ItemDrop drop = item.GetComponent<ItemDrop>();
                if (drop != null && drop.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophy)
                {
                    TotalTrophiesInGame++;
                }
            }
            return TotalTrophiesInGame;
        }

        public static List<string> GetKilledButMissingTrophies()
        {
            CacheEnemyData(); // ensure we have data
            List<string> missing = new List<string>();
            List<string> collected = Player.m_localPlayer.GetTrophies();

            foreach (var kvp in TrophyToEnemy)
            {
                string trophyName = kvp.Key;
                string enemyKey = kvp.Value;

                if (!collected.Contains(trophyName) && GetKillCount(enemyKey) > 0)
                {
                    missing.Add(trophyName);
                }
            }
            return missing;
        }
    }
}
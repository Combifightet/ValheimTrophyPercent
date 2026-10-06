using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace ValheimTrophyPercent
{
    public static class TrophyManager
    {
        // Store our pinned trophy prefab names (e.g., "TrophyBoar")
        public static HashSet<string> PinnedTrophies = new HashSet<string>();

        // Caches the total number of trophies in the game
        public static int TotalTrophiesInGame = -1;
        
        // Store Valheim's font here so all our custom UI can use it safely
        public static TMP_FontAsset ValheimFont;

        // Caches to map "TrophyBoar" -> "Boar" and its drop chance (e.g. 0.15)
        public static Dictionary<string, string> TrophyToEnemy = new Dictionary<string, string>();
        public static Dictionary<string, float> TrophyDropChances = new Dictionary<string, float>();

        public static void TogglePin(string trophyPrefabName)
        {
            if (PinnedTrophies.Contains(trophyPrefabName))
                PinnedTrophies.Remove(trophyPrefabName);
            else
                PinnedTrophies.Add(trophyPrefabName);
            
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
            int kills = 0;
            
            for (int i = 0; i < 10; i++)
            {
                for (int k = 0; k < 5; k++)
                {
                    if (profile.m_playerStats[i].m_enemyStats[k] != null &&
                        profile.m_playerStats[i].m_enemyStats[k].TryGetValue(enemyKey, out float kCount))
                    {
                        kills += (int)kCount;
                    }
                }
            }
            return kills;
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
            CacheEnemyData(); // Ensure we have data
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
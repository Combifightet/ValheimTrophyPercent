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

        public static void TogglePin(string trophyPrefabName)
        {
            if (PinnedTrophies.Contains(trophyPrefabName))
                PinnedTrophies.Remove(trophyPrefabName);
            else
                PinnedTrophies.Add(trophyPrefabName);
            
            ProgressList.UpdatePinnedUI();
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
            List<string> missing = new List<string>();
            List<string> collected = Player.m_localPlayer.GetTrophies();
            PlayerProfile profile = Game.instance.GetPlayerProfile();

            // Gather all enemy kills across all difficulty categories
            HashSet<string> killedEnemies = new HashSet<string>();
            for (int i = 0; i < 10; i++)
            {
                for (int k = 0; k < 5; k++)
                {
                    if (profile.m_playerStats[i].m_enemyStats[k] != null)
                    {
                        foreach (var stat in profile.m_playerStats[i].m_enemyStats[k])
                        {
                            if (stat.Value > 0) killedEnemies.Add(stat.Key);
                        }
                    }
                }
            }

            // Cross-reference kills with CharacterDrops in ZNetScene
            if (ZNetScene.instance != null)
            {
                foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
                {
                    // Clean up prefab name to match kill stat keys (removes (Clone) etc.)
                    string cleanName = Utils.GetPrefabName(prefab); 

                    if (killedEnemies.Contains(cleanName))
                    {
                        CharacterDrop charDrop = prefab.GetComponent<CharacterDrop>();
                        if (charDrop != null)
                        {
                            foreach (var drop in charDrop.m_drops)
                            {
                                if (drop.m_prefab == null) continue;
                                ItemDrop itemDrop = drop.m_prefab.GetComponent<ItemDrop>();
                                
                                if (itemDrop != null && itemDrop.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophy)
                                {
                                    string trophyName = drop.m_prefab.name;
                                    if (!collected.Contains(trophyName) && !missing.Contains(trophyName))
                                    {
                                        missing.Add(trophyName);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return missing;
        }
    }
}
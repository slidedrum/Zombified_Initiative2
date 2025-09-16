using GameData;
using Player;
using System;
using System.Linq;
using UnityEngine;
using Zombified_Initiative;

namespace ZombieTweak2
{
    public static class zDebug
    {
        private static void printAllNames()
        {
            System.Collections.Generic.HashSet<string> ArchetypeNames = new System.Collections.Generic.HashSet<string>();
            System.Collections.Generic.HashSet<string> terminalItemShortNames = new System.Collections.Generic.HashSet<string>();
            System.Collections.Generic.HashSet<string> terminalItemLongNames = new System.Collections.Generic.HashSet<string>();
            System.Collections.Generic.HashSet<string> PublicNames = new System.Collections.Generic.HashSet<string>();
            System.Collections.Generic.HashSet<string> DataBlockPublicNames = new System.Collections.Generic.HashSet<string>();
            System.Collections.Generic.List<Item> allItems = GameObject.FindObjectsOfType<Item>().ToList();
            foreach (Item item in allItems) 
            {
                PublicNames.Add(item?.PublicName ?? "");
                DataBlockPublicNames.Add(item?.ItemDataBlock?.publicName ?? "");
                ArchetypeNames.Add(item?.ArchetypeName ?? "");
                terminalItemShortNames.Add(item?.ItemDataBlock?.terminalItemShortName ?? "");
                terminalItemLongNames.Add(item?.ItemDataBlock?.terminalItemLongName ?? "");
            }
            ZiMain.log.LogInfo("PublicNames:");
            ZiMain.log.LogInfo(string.Join("\n", PublicNames));
            ZiMain.log.LogInfo("DataBlockPublicNames:");
            ZiMain.log.LogInfo(string.Join("\n", DataBlockPublicNames));
            ZiMain.log.LogInfo("ArchetypeNames:");
            ZiMain.log.LogInfo(string.Join("\n", ArchetypeNames));
            ZiMain.log.LogInfo("terminalItemShortNames:");
            ZiMain.log.LogInfo(string.Join("\n", terminalItemShortNames));
            ZiMain.log.LogInfo("terminalItemLongNames:");
            ZiMain.log.LogInfo(string.Join("\n", terminalItemLongNames));
        }
        private static void printAllInventoryItems()
        {
            var allItemTypes = ItemSpawnManager.m_itemDataPerInventorySlot;
            for (int i = 0; i < allItemTypes.Count; i++)
            {
                var slotName = ((InventorySlot)i).ToString();
                var items = allItemTypes[i];
                ZiMain.log.LogInfo($"{slotName} ({items.Count}):");
                foreach (var item in items)
                {
                    ZiMain.log.LogInfo($"\t - {item.publicName}");
                }
            }
        }
        private static void printWhatBotsCanPickUp()
        {
            //ItemDataBlock.s_blockIDByName has all ids
            //RootPlayerBotAction.s_itemBasePrios has what bots can pick up
            foreach (var item in RootPlayerBotAction.s_itemBasePrios)
            {
                uint id = item.Key;
                float priority = item.Value;
                ItemDataBlock block = ItemDataBlock.s_blockByID[id];
                var name = block.publicName;
                ZiMain.log.LogMessage($"{name}:{priority}");
            }
        }
    }
}

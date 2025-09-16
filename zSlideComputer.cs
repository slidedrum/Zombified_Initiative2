using GameData;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using Zombified_Initiative;

namespace ZombieTweak2
{
    public static class zSlideComputer
    {
        //This class is for handling things like stopping bots from do unwanted actions

        public static Il2CppSystem.Collections.Generic.Dictionary<uint,float> itemPrios = RootPlayerBotAction.s_itemBasePrios;
        public static Il2CppSystem.Collections.Generic.Dictionary<uint, float> OriginalItemPrios = new ();
        private static Il2CppReferenceArray<ItemDataBlock> consumableItems;
        private static List<string> consumableItemPublicNames;
        private static List<string> consumableItemNames;
        private static List<string> fullGlowStickNames;
        private static List<string> shortGlowStickNames;

        public static void Init()
        {
            if (OriginalItemPrios.count == 0)
                FirstTimeSetup();
            itemPrios.Clear();
            foreach (var kvp in OriginalItemPrios)
            {
                itemPrios[kvp.Key] = kvp.Value;
            }
            consumableItems = ItemSpawnManager.m_itemDataPerInventorySlot[(int)InventorySlot.Consumable];
            consumableItemPublicNames = consumableItems.Select(item => item.publicName).ToList();
            consumableItemNames = consumableItems.Select(item => item.name).ToList();
            fullGlowStickNames = new List<string> { "CONSUMABLE_GlowStick", "CONSUMABLE_GlowStick_Christmas", "CONSUMABLE_GlowStick_Halloween", "CONSUMABLE_GlowStick_Yellow" };
            shortGlowStickNames = new List<string> { "Glow Stick", "Red Glow Stick", "Glow Stick Orange", "Glow Stick Yellow" };
            
        }
        public static void FirstTimeSetup()
        {
            I_SetBotItemPriority("CONSUMABLE_GlowStick_Yellow", 10);
            foreach (var kvp in RootPlayerBotAction.s_itemBasePrios)
            {
                OriginalItemPrios[kvp.Key] = kvp.Value;
            }
        }
        public static void Update()
        {
            
        }
        public static Dictionary<string, float> GetBotItems()
        {
            Dictionary<string,float> botItems = new();
            foreach (Il2CppSystem.Collections.Generic.KeyValuePair<uint, float> item in RootPlayerBotAction.s_itemBasePrios)
            {
                uint id = item.Key;
                float priority = item.Value;
                ItemDataBlock block = ItemDataBlock.s_blockByID[id];
                string name = block.publicName;
                botItems[name] = priority;
                ZiMain.log.LogMessage($"{name}:{priority}");
            }
            return botItems;
        }

        public static string ConvertItemPublicName(string name)
        {
            if (consumableItemNames.Contains(name))
            {
                return name;
            }
            if (consumableItemPublicNames.Contains(name))
            {
                return consumableItemNames[consumableItemPublicNames.IndexOf(name)];
            }
            //ZiMain.log.LogWarning($"Name '{name}' doesn't apear to be a consumable.");
            //ZiMain.log.LogWarning($"consumableItemNames:");
            //ZiMain.log.LogWarning(string.Join("\n",consumableItemNames));
            //ZiMain.log.LogWarning($"consumableItemPublicNames:");
            //ZiMain.log.LogWarning(string.Join("\n", consumableItemPublicNames));
            return name;
  
        }
        private static bool SetBotItemPriority(string itemName, float priority)
        {
            itemName = ConvertItemPublicName(itemName);
            if (fullGlowStickNames.Contains(itemName))
            { 
                //Why are there 4 glowsticks?!
                //And why does the red one have the color first?!

                foreach (string glowStick in fullGlowStickNames)
                {
                    bool uhOh = I_SetBotItemPriority(glowStick, priority);
                    if (!uhOh)
                    {
                        ZiMain.log.LogError($"Something went VERY wrong. {glowStick} not found!");
                        return false;
                    }
                }
                return true;
            }
            return I_SetBotItemPriority(itemName,priority);
        }
        public static bool SetBotItemPriority(uint id, float priority)
        {
            if (itemPrios.ContainsKey(id))
            {
                itemPrios[id] = priority;
                return true;
            }
            return false;
        }
        private static bool I_SetBotItemPriority(string itemName, float priority)
        {
            if (ItemDataBlock.s_blockIDByName.ContainsKey(itemName))
            {
                uint id = ItemDataBlock.GetBlockID(itemName);
                itemPrios[id] = priority;
                return true;
            }
            return false;
        }
    }
}

using GameData;
using GTFO.API;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Player;
using SNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Zombified_Initiative;

namespace ZombieTweak2
{
    public static class zSlideComputer
    {
        //This class is for handling things like stopping bots from do unwanted actions

        public static Il2CppSystem.Collections.Generic.Dictionary<uint,float> itemPrios = new ();
        public static Il2CppSystem.Collections.Generic.Dictionary<uint, float> zerodItemPrios = new ();
        public static Il2CppSystem.Collections.Generic.Dictionary<uint, float> OriginalItemPrios = new();
        public static Dictionary<int, bool> PickUpPerms = new ();
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
            RootPlayerBotAction.s_itemBasePrios = itemPrios;
            consumableItems = ItemSpawnManager.m_itemDataPerInventorySlot[(int)InventorySlot.Consumable];
            consumableItemPublicNames = consumableItems.Select(item => item.publicName).ToList();
            consumableItemNames = consumableItems.Select(item => item.name).ToList();
            fullGlowStickNames = new List<string> { "CONSUMABLE_GlowStick", "CONSUMABLE_GlowStick_Christmas", "CONSUMABLE_GlowStick_Halloween", "CONSUMABLE_GlowStick_Yellow" };
            shortGlowStickNames = new List<string> { "Glow Stick", "Red Glow Stick", "Glow Stick Orange", "Glow Stick Yellow" };

            List<PlayerAIBot> playerAiBots = ZiMain.GetBotList();
            foreach (PlayerAIBot bot in playerAiBots)
            {
                PickUpPerms[bot.GetInstanceID()] = true;
            }
        }
        public static void FirstTimeSetup()
        {
            I_SetBotItemPriority("CONSUMABLE_GlowStick_Yellow", 10);
            foreach (var kvp in RootPlayerBotAction.s_itemBasePrios)
            {
                OriginalItemPrios[kvp.Key] = kvp.Value;
                zerodItemPrios[kvp.Key] = 0;
            }
            //Might need to modify PlayerAIBot.s_recognisedItemTypes?
            //PlayerAIBot.KnowsHowToUseItem also could be relevent.
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
            if (ItemDataBlock.s_blockByID.ContainsKey(id))
            {
                itemPrios[id] = priority;
                if (!PlayerAIBot.s_recognisedItemTypes.Contains(id))
                    PlayerAIBot.s_recognisedItemTypes.Add(id);
                return true;
            }
            return false;
        }
        private static bool I_SetBotItemPriority(string itemName, float priority)
        {
            if (ItemDataBlock.s_blockIDByName.ContainsKey(itemName))
            {
                uint id = ItemDataBlock.GetBlockID(itemName);
                return SetBotItemPriority(id, priority);
            }
            return false;
        }
        public static void FlipPickupPermission(List<PlayerAIBot> botSelection, bool allowed)
        {
            foreach (PlayerAIBot bot in botSelection)
            {
                SetPickupPermissions(bot,!GetPickupPermission(bot));
            }
        }
        public static void TogglePickupPermission(List<PlayerAIBot> botSelection, bool allowed)
        {
            var unselectedCount = 0;
            var selectedCount = 0;
            foreach (PlayerAIBot bot in botSelection)
            {
                if (GetPickupPermission(bot))
                    selectedCount++;
                else
                    unselectedCount++;
            }
            bool majority = selectedCount > unselectedCount;
            foreach (PlayerAIBot bot in botSelection)
            {
                SetPickupPermissions(bot, !majority);
            }
        }
        public static void SetPickupPermission(List<PlayerAIBot> botSelection, bool allowed)
        {
            foreach (var bot in botSelection)
                SetPickupPermission(bot.GetInstanceID(), allowed);
        }
        public static void SetPickupPermissions(PlayerAIBot bot, bool allowed)
        {
            SetPickupPermission(bot.GetInstanceID(), allowed);
        }
        public static void SetPickupPermission(int id, bool allowed)
        {
            PickUpPerms[id] = allowed;
        }
        public static bool GetPickupPermission(PlayerAIBot bot)
        {
            return PickUpPerms[bot.GetInstanceID()];
        }
        public static bool GetPickupPermission(int id)
        {
            if (PickUpPerms.ContainsKey(id))
                return PickUpPerms[id];
            ZiMain.log.LogWarning($"Unknown bot asked for pickup perms id:{id}.");
            return false;
        }


    }
}

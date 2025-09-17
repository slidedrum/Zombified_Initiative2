using GameData;
using GTFO.API;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Player;
using SNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using ZombieTweak2.zMenu;
using Zombified_Initiative;

namespace ZombieTweak2
{
    public static class zSlideComputer
    {
        //This class is for handling things like stopping bots from do unwanted actions

        public static Il2CppSystem.Collections.Generic.Dictionary<uint,float> itemPrios = new ();
        public static Il2CppSystem.Collections.Generic.Dictionary<uint, float> zerodItemPrios = new ();
        public static Il2CppSystem.Collections.Generic.Dictionary<uint, float> OriginalItemPrios = new();
        public static Dictionary<int, bool> PickUpPerms = new (); //bot.Agent.Owner.PlayerSlotIndex()
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
                PickUpPerms[bot.Agent.Owner.PlayerSlotIndex()] = true;
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
        //Using bot.Agent.Owner.PlayerSlotIndex() as a key is good for transfering over network.
        //Issues may arrise when the number of bots changes mid mission.  
        //TODO handle that
        public static void FlipPickupPermission()
        {
            foreach (var bot in zMenus.botSelection)
            {
                if (!bot.Value) //unselected, ignore.
                    continue;
                TogglePickupPermission(bot.Key);
            }
        }
        public static void FlipPickupPermission(List<int> idList)
        {
            foreach (int id in idList)
            {
                SetPickupPermission(id, !GetPickupPermission(id));
            }
        }
        public static void FlipPickupPermission(List<PlayerAIBot> botList)
        {
            foreach (PlayerAIBot bot in botList)
            {
                SetPickupPermission(bot,!GetPickupPermission(bot));
            }
        }
        public static void FlipPickupPermission(Dictionary<int, bool> botSelection)
        {
            foreach (var bot in botSelection)
            {
                if (!bot.Value) //unselected, ignore.
                    continue;
                TogglePickupPermission(bot.Key);
            }
        }
        public static void TogglePickupPermission()
        { 
            var botSelection = zMenus.botSelection;
            TogglePickupPermission(botSelection);
        }
        public static void TogglePickupPermission(int id)
        {
            SetPickupPermission(id, !GetPickupPermission(id));
        }
        public static void TogglePickupPermission(List<int> idList)
        {
            var dissabledCount = 0;
            var enabledCount = 0;
            foreach (int id in idList)
            {
                if (GetPickupPermission(id))
                    enabledCount++;
                else
                    dissabledCount++;
            }
            bool majority = enabledCount > dissabledCount;
            foreach (int id in idList)
            {
                SetPickupPermission(id, !majority);
            }
        }
        public static void TogglePickupPermission(List<PlayerAIBot> botList)
        {
            var dissabledCount = 0;
            var enabledCount = 0;
            foreach (PlayerAIBot bot in botList)
            {
                if (GetPickupPermission(bot))
                    enabledCount++;
                else
                    dissabledCount++;
            }
            bool majority = enabledCount > dissabledCount;
            foreach (PlayerAIBot bot in botList)
            {
                SetPickupPermission(bot, !majority);
            }
        }
        public static void TogglePickupPermission(Dictionary<int, bool> botSelection)
        {
            var dissabledCount = 0;
            var enabledCount = 0;
            foreach (var bot in botSelection)
            {
                if (!bot.Value) //unselected, ignore.
                    continue;
                if (GetPickupPermission(bot.Key))
                    enabledCount++;
                else
                    dissabledCount++;
            }
            bool majority = enabledCount > dissabledCount;
            foreach (var bot in botSelection)
            {
                if (!bot.Value) //unselected, ignore.
                    continue;
                SetPickupPermission(bot.Key,!majority);
            }
        }
        public static void SetPickupPermission(int id, bool allowed)
        {
            ZiMain.log.LogMessage($"Setting pickup perm for id {id} to {allowed}");
            PickUpPerms[id] = allowed;
        }
        public static void SetPickupPermission(PlayerAIBot bot, bool allowed)
        {
            SetPickupPermission(bot.Agent.Owner.PlayerSlotIndex(), allowed);
        }
        public static void SetPickupPermission(List<int> idList, bool allowed)
        {
            foreach (int id in idList)
                SetPickupPermission(id, allowed);
        }
        public static void SetPickupPermission(List<PlayerAIBot> botList, bool allowed)
        {
            foreach (var bot in botList)
                SetPickupPermission(bot.Agent.Owner.PlayerSlotIndex(), allowed);
        }
        public static bool GetPickupPermission(PlayerAIBot bot)
        {
            if (bot == null)
            {
                ZiMain.log.LogError($"Can't get pickup perms when bot is null");
                return true;
            }
            if (bot.Agent == null) 
            { 
                ZiMain.log.LogError($"Can't get pickup perms when Agent is null");
                return true; 
            }
            if (bot.Agent.Owner == null) 
            {
                ZiMain.log.LogError($"Can't get pickup perms when Owner is null");
                return true; 
            }
            return PickUpPerms[bot.Agent.Owner.PlayerSlotIndex()];
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

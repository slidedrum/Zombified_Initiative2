using GameData;
using GTFO.API;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ZombieTweak2.zMenu;
using ZombieTweak2.zNetworking;
using Zombified_Initiative;
using static ZombieTweak2.zNetworking.pStructs;

namespace ZombieTweak2
{
    public static class zSlideComputer
    {
        //This class is for handling things like stopping bots from do unwanted actions

        public static Il2CppSystem.Collections.Generic.Dictionary<uint, float> itemPrios = new();
        public static Il2CppSystem.Collections.Generic.Dictionary<uint, int> resourceThresholds = new();
        public static Il2CppSystem.Collections.Generic.Dictionary<uint, bool> enabledItemPrios = new ();
        public static Il2CppSystem.Collections.Generic.Dictionary<uint, bool> enabledResourceShares = new ();
        public static Il2CppSystem.Collections.Generic.Dictionary<uint, float> OriginalItemPrios = new();
        public static Dictionary<int, bool> PickUpPerms = new (); //bot.Agent.Owner.PlayerSlotIndex()
        public static Dictionary<int, bool> SharePerms = new (); //bot.Agent.Owner.PlayerSlotIndex()
        public static Dictionary<int, bool> MovePerms = new (); //bot.Agent.Owner.PlayerSlotIndex()


        public static Il2CppReferenceArray<ItemDataBlock> consumableItems 
        { 
            get 
            {
                return ItemSpawnManager.m_itemDataPerInventorySlot?[(int)InventorySlot.Consumable];
            } 
        }
        private static Il2CppReferenceArray<ItemDataBlock> _consumableItems;
        public static List<string> consumableItemPublicNames 
        { 
            get 
            {
                if (consumableItems.Equals(_consumableItems))
                    return _consumableItemPublicNames;
                _consumableItems = consumableItems;
                _consumableItemPublicNames = consumableItems.Select(item => item.publicName).ToList();
                return _consumableItemPublicNames; 
            } 
        }
        private static List<string> _consumableItemPublicNames;
        public static List<string> consumableItemNames 
        {
            get 
            {
                if (consumableItems.Equals(_consumableItems))
                    return _consumableItemNames;
                _consumableItems = consumableItems;
                _consumableItemNames = consumableItems.Select(item => item.name).ToList();
                return _consumableItemNames;
            } 
        }
        private static List<string> _consumableItemNames;


        public static List<string> fullGlowStickNames { get; private set; }
        public static List<string> shortGlowStickNames { get; private set; }
        

        public static void Init()
        {
            if (OriginalItemPrios.count == 0)
                FirstTimeSetup();
            itemPrios.Clear();
            foreach (var kvp in OriginalItemPrios)
            {
                itemPrios[kvp.Key] = kvp.Value;
                enabledItemPrios[kvp.Key] = true;
            }
            RootPlayerBotAction.s_itemBasePrios = itemPrios;
            fullGlowStickNames = new List<string> { "CONSUMABLE_GlowStick", "CONSUMABLE_GlowStick_Christmas", "CONSUMABLE_GlowStick_Halloween", "CONSUMABLE_GlowStick_Yellow" };
            shortGlowStickNames = new List<string> { "Glow Stick", "Red Glow Stick", "Glow Stick Orange", "Glow Stick Yellow" };

            List<PlayerAIBot> playerAiBots = ZiMain.GetBotList();
            foreach (PlayerAIBot bot in playerAiBots)
            {
                PickUpPerms[bot.Agent.Owner.PlayerSlotIndex()] = true;
                SharePerms[bot.Agent.Owner.PlayerSlotIndex()] = true;
                MovePerms[bot.Agent.Owner.PlayerSlotIndex()] = true;
            }
            foreach (ItemDataBlock block in ItemSpawnManager.m_itemDataPerInventorySlot[(int)InventorySlot.ResourcePack])
            {
                uint itemID = ItemDataBlock.s_blockIDByName[block.name]; //there's got to be a better way to get the playerID.
                resourceThresholds[itemID] = 100;
                enabledResourceShares[itemID] = true;
            }
        }
        public static void FirstTimeSetup()
        {
            I_SetBotItemPriority("CONSUMABLE_GlowStick_Yellow", 10);
            foreach (var kvp in RootPlayerBotAction.s_itemBasePrios)
            {
                OriginalItemPrios[kvp.Key] = kvp.Value;
            }
        }
        public static float GetItemPrio(uint itemID)
        {
            foreach (var kv in enabledItemPrios)
            {
                if (kv.Key == itemID)
                {
                    if (!kv.Value) return 0f;

                    foreach (var kv2 in itemPrios)
                    {
                        if (kv2.Key == itemID)
                            return kv2.Value;
                    }
                    return 0f;
                }
            }
            return 0f;
        }
        public static void ResetAllItemPrio()
        {
            ZiMain.log.LogMessage("Ressting all item priorties to default");
            foreach (var kvp in OriginalItemPrios)
            {
                SetBotItemPriority(kvp.Key, kvp.Value);
            }
        }
        public static void ResetItemPrio(uint itemID)
        {
            if (!itemPrios.ContainsKey(itemID))
                return;
            itemPrios[itemID] = OriginalItemPrios[itemID];
        }
        public static void ToggleItemPrioDisabled(uint itemID)
        {
            if (!enabledItemPrios.ContainsKey(itemID))
                return;
            SetItemPrioDisabled(itemID, !enabledItemPrios[itemID]);
        }
        public static void SetItemPrioDisabled(uint itemID, bool allowed, ulong netSender = 0)
        {
            if (!enabledItemPrios.ContainsKey(itemID))
            {
                ZiMain.log.LogWarning($"Attemted to set item pick up allow for unknown id: {itemID} - {allowed}");
                return;
            }
            if (netSender == 0)
            {
                pStructs.pItemPrioDisable info = new pStructs.pItemPrioDisable();
                info.id = itemID;
                info.allowed = allowed;
                NetworkAPI.InvokeEvent<pStructs.pItemPrioDisable>("SetItemPrioDisable", info);
            }
            enabledItemPrios[itemID] = allowed;
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
        public static void SetResourceThreshold(uint itemID, int threshold, ulong netSender = 0)
        {
            if (!resourceThresholds.ContainsKey(itemID))
            {
                ZiMain.log.LogWarning($"Attemted to set resource threshold for unknown id: {itemID} - {threshold}");
                return;
            }
            if (netSender == 0)
            {
                pStructs.pResourceThreshold info = new pStructs.pResourceThreshold();
                info.id = itemID;
                info.threshold = threshold;
                NetworkAPI.InvokeEvent<pStructs.pResourceThreshold>("SetResourceThreshold", info);
            }
            resourceThresholds[itemID] = Math.Clamp(threshold, 0, 100);
        }
        public static int GetResourceThreshold(uint itemID)
        {
            if (!resourceThresholds.ContainsKey(itemID))
            {
                ZiMain.log.LogWarning($"Attemted to get resource threshold for unknown id: {itemID}");
                return 0;
            }
            return resourceThresholds[itemID];
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
        public static bool SetBotItemPriority(uint id, float priority, ulong netSender = 0) //flowthrough main
        {
            if (netSender == 0)
            {
                pItemPrio info = new pItemPrio();
                info.id = id;
                info.prio = priority;
                NetworkAPI.InvokeEvent<pStructs.pItemPrio>("SetItemPrio",info);
            }
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
        public static void ToggleResourceSharePermission(uint itemID)
        {
            if (!enabledResourceShares.ContainsKey(itemID))
                return;
            SetResourceSharePermission(itemID,!GetResourceSharePermission(itemID));
        }
        public static bool GetResourceSharePermission(uint itemID)
        {
            if (!enabledResourceShares.ContainsKey(itemID))
            {
                ZiMain.log.LogWarning($"tried to get resoruce share perms for unkown item id:{itemID}.");
                return false;
            }
            return enabledResourceShares[itemID];
        }
        public static void SetResourceSharePermission(uint itemID, bool allowed,ulong netSender = 0)
        {
            if (!enabledResourceShares.ContainsKey(itemID))
                return;
            if (netSender == 0)
            {
                pStructs.pResourceThresholdDisable info = new pStructs.pResourceThresholdDisable();
                info.id = itemID;
                info.allowed = allowed;
                NetworkAPI.InvokeEvent<pStructs.pResourceThresholdDisable>("SetResourceThresholdDisable", info);
            }
            enabledResourceShares[itemID] = allowed;
        }
        public static void ToggleBotSharePermission(int playerID)
        {
            if (!SharePerms.ContainsKey(playerID))
                return;
            SetSharePermission(playerID,!GetSharePermission(playerID));
        }
        public static void ToggleBotSharePermission()
        {
            var botSelection = SelectionMenuClass.botSelection;
            ToggleSharePermission(botSelection);
        }
        public static void ToggleSharePermission(Dictionary<int, bool> botSelection)
        {
            var disabledCount = 0;
            var enabledCount = 0;
            foreach (var bot in botSelection)
            {
                if (!bot.Value) //unselected, ignore.
                    continue;
                if (GetSharePermission(bot.Key))
                    enabledCount++;
                else
                    disabledCount++;
            }
            bool majority = enabledCount > disabledCount;
            foreach (var bot in botSelection)
            {
                if (!bot.Value) //unselected, ignore.
                    continue;
                SetSharePermission(bot.Key, !majority);
            }
        }
        //Using bot.Agent.Owner.PlayerSlotIndex() as a key is good for transfering over network.
        //Issues may arrise when the number of bots changes mid mission.  
        //TODO handle that
        //TODO add more variants of get/set/toggle/flip for all settings and possible ways you'd want to call it.
        public static void FlipPickupPermission()
        {
            foreach (var bot in SelectionMenuClass.botSelection)
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
            var botSelection = SelectionMenuClass.botSelection;
            TogglePickupPermission(botSelection);
        }
        public static void TogglePickupPermission(int id)
        {
            SetPickupPermission(id, !GetPickupPermission(id));
        }
        public static void TogglePickupPermission(List<int> idList)
        {
            var disabledCount = 0;
            var enabledCount = 0;
            foreach (int id in idList)
            {
                if (GetPickupPermission(id))
                    enabledCount++;
                else
                    disabledCount++;
            }
            bool majority = enabledCount > disabledCount;
            foreach (int id in idList)
            {
                SetPickupPermission(id, !majority);
            }
        }
        public static void TogglePickupPermission(List<PlayerAIBot> botList)
        {
            var disabledCount = 0;
            var enabledCount = 0;
            foreach (PlayerAIBot bot in botList)
            {
                if (GetPickupPermission(bot))
                    enabledCount++;
                else
                    disabledCount++;
            }
            bool majority = enabledCount > disabledCount;
            foreach (PlayerAIBot bot in botList)
            {
                SetPickupPermission(bot, !majority);
            }
        }
        public static void TogglePickupPermission(Dictionary<int, bool> botSelection)
        {
            var disabledCount = 0;
            var enabledCount = 0;
            foreach (var bot in botSelection)
            {
                if (!bot.Value) //unselected, ignore.
                    continue;
                if (GetPickupPermission(bot.Key))
                    enabledCount++;
                else
                    disabledCount++;
            }
            bool majority = enabledCount > disabledCount;
            foreach (var bot in botSelection)
            {
                if (!bot.Value) //unselected, ignore.
                    continue;
                SetPickupPermission(bot.Key,!majority);
            }
        }
        public static void RemoveActionsOfType(PlayerAgent agent, Type actionType)
        {
            //todo add more variants of this method with different arguments
            if (!typeof(PlayerBotActionBase).IsAssignableFrom(actionType))
                return;
            if (!agent.Owner.IsBot)
                return;
            List<PlayerBotActionBase> actionsToRemove = new List<PlayerBotActionBase>();
            PlayerAIBot aiBot = agent.gameObject.GetComponent<PlayerAIBot>();
            foreach (var action in aiBot.Actions)
            {
                if (action.GetIl2CppType().Name == actionType.Name)
                {
                    actionsToRemove.Add(action);
                }
            }
            foreach (var action in actionsToRemove)
            {
                aiBot.StopAction(action.DescBase);
            }
        }
        public static void SetSharePermission(int playerID, bool allowed, ulong netSender = 0)
        {
            if (!SharePerms.ContainsKey(playerID))
            {
                ZiMain.log.LogWarning($"Tried to set share perm for invalid player id: {playerID}, allowed:{allowed}");
                return;
            }
            if (netSender == 0)
            {
                pStructs.pSharePermission info = new pStructs.pSharePermission();
                info.playerID = playerID;
                info.allowed = allowed;
                NetworkAPI.InvokeEvent<pStructs.pSharePermission>("SetSharePermission", info);
            }
            ZiMain.log.LogMessage($"Setting share perm for id {playerID} to {allowed}");
            SharePerms[playerID] = allowed;
            PlayerManager.TryGetPlayerAgent(ref playerID, out var agent);
            if (agent != null)
                RemoveActionsOfType(agent, typeof(PlayerBotActionShareResourcePack));
        }
        public static void SetPickupPermission(int playerID, bool allowed, ulong netSender = 0)
        {
            if (!PickUpPerms.ContainsKey(playerID))
            {
                ZiMain.log.LogWarning($"Tried to set pickup perm for invalid player id: {playerID}, allowed:{allowed}");
                return;
            }
            if (netSender == 0)
            {
                pStructs.pPickupPermission info = new pStructs.pPickupPermission();
                info.playerID = playerID;
                info.allowed = allowed;
                NetworkAPI.InvokeEvent<pStructs.pPickupPermission>("SetPickupPermission", info);
            }
            ZiMain.log.LogMessage($"Setting pickup perm for id {playerID} to {allowed}");
            PickUpPerms[playerID] = allowed;
            PlayerManager.TryGetPlayerAgent(ref playerID, out var agent);
            if (agent != null)
                RemoveActionsOfType(agent, typeof(PlayerBotActionCollectItem));
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
        public static bool GetSharePermission(int id)
        {
            if (SharePerms.ContainsKey(id))
                return SharePerms[id];
            ZiMain.log.LogWarning($"Unknown bot asked for share perms id:{id}.");
            return false;
        }
    }
}

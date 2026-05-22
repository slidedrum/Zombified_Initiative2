using GameData;
using Player;
using SlideMenu;
using System;
using System.Collections.Generic;
using System.Linq;
using BotControl.Menus;
using BotControl;

namespace BotControl
{
    public static class zSlideComputer
    {
        //This class is for handling things like stopping bots from do unwanted actions

        //public static Il2CppSystem.Collections.Generic.Dictionary<uint, float> itemPrios = new();
        //public static Il2CppSystem.Collections.Generic.Dictionary<uint, int> resourceThresholds = new();
        //public static Il2CppSystem.Collections.Generic.Dictionary<uint, bool> enabledItemPrios = new ();
        //public static Il2CppSystem.Collections.Generic.Dictionary<uint, bool> enabledResourceShares = new ();
        //public static Il2CppSystem.Collections.Generic.Dictionary<uint, float> OriginalItemPrios = new();
        public static OverrideTree<float?> ActionPriorities;
        public static OverrideTree<bool?> ActionPermissions;
        //public static OverrideTree<int?> ItemPickupPriorities;
        //public static Dictionary<string, sMenu.sMenuNode> actionNameToMenuNodes;
        public static class PermissionDefinitions
        {
            private static Dictionary<string, PermissionDefinition> permissionDeffinitions = new();

            private static List<string> _actionKeysCache = new();
            private static List<string> ActionKeys
            {
                get
                {
                    // Check semantic equality (ignoring order)
                    bool matches =
                        _actionKeysCache.Count == permissionDeffinitions.Count &&
                        !_actionKeysCache.Except(permissionDeffinitions.Keys).Any() &&
                        !permissionDeffinitions.Keys.Except(_actionKeysCache).Any();

                    // Rebuild cache if mismatch
                    if (!matches)
                    {
                        _actionKeysCache = permissionDeffinitions.Keys
                            .OrderBy(k => k, StringComparer.Ordinal)
                            .ToList();
                    }

                    return _actionKeysCache;
                }
            }
            private class PermissionDefinition
            {
                //Might be able to remove the PermissionDefinition entirely?
                //Even if not, probably want to rename it, as it doesn't really describe what it is anymore.
                //public Dictionary<int, bool?> perms;
                public sMenu.sMenuNode node;
                public List<Type> actionTypesToCull;
                //public float defaultPriority;
                public string key;
                internal PermissionDefinition(string key, sMenu.sMenuNode node = null, List<Type> ActionTypesToCull = null)
                {
                    this.key = key;
                    //this.perms = new Dictionary<int, bool?>();
                    //for (int i = 1; i < 4; i++)
                    //    perms[i] = defaultPerm;
                    this.node = node;
                    this.actionTypesToCull = ActionTypesToCull;
                    if (this.actionTypesToCull == null)
                        actionTypesToCull = new List<Type>();
                    //this.defaultPriority = defaultPriority ?? 0f;
                }
            }
            public static void ClearPermissionDefinitions()
            {
                permissionDeffinitions.Clear();
            }
            public static void CreatePermissionDeffinition(string key, bool? defaultPerm = true, sMenu.sMenuNode node = null, sMenu menu = null, Type ActionTypeToCull = null, float? defaultPriority = null, string parrentKey = null, bool hasDefaultValue = false)
            {
                List<Type> actionTypesToCull = new();
                if (ActionTypeToCull != null)
                    actionTypesToCull.Add(ActionTypeToCull);
                CreatePermissionDeffinition(key, defaultPerm, node, menu, actionTypesToCull, defaultPriority, parrentKey, hasDefaultValue);
            }
            public static void CreatePermissionDeffinition(string key, bool? defaultPerm = true, sMenu.sMenuNode node = null, sMenu menu = null, List<Type> ActionTypesToCull = null, float? defaultPriority = null, string parrentKey = null, bool hasDefaultValue = false)
            {
                //PermissionDefinition permissionDef = new PermissionDefinition(key, node, ActionTypesToCull);
                //permissionDeffinitions.Add(key, permissionDef);
                //Might be able to remove the PermissionDefinition entirely?
                if (defaultPriority != null)
                {
                    zSlideComputer.ActionPriorities.AddNode("Default" + key, defaultPriority, (string?)null, defaultValue: defaultPriority);
                    zSlideComputer.ActionPriorities.AddNode(key, defaultPriority, "Default" + key, defaultValue: defaultPriority).onChanged.Listen(AutomaticActionMenuClass.GenericUpdateNodePrioDisplay, [node]);
                }
                var permissionsNode = ActionPermissions.AddNode(key, defaultPerm, parrentKey, defaultValue: defaultPerm, hasDefaultValue: hasDefaultValue);
                permissionsNode.onChanged.Listen((Action<List<Type>>)zSlideComputer.RemoveActionsOfType, args: [ActionTypesToCull]);
                if (node != null)
                {
                    //actionNameToMenuNodes[key] = node;
                    permissionsNode.onChanged.Listen(AutomaticActionMenuClass.GenericUpdateNodeAllowedDisplay, args: [key, node]);
                }
                if (menu != null)
                    permissionsNode.onChanged.Listen(AutomaticActionMenuClass.GenericUpdateNodeAllowedDisplay, args: [key, menu.centerNode]);

            }
            public static bool KeyExists(string key)
            {
                return ActionKeys.Contains(key);
            }
            public static int KeyToId(string key)
            {
                if (!KeyExists(key))
                {
                    ZiMain.log.LogWarning($"Unknown actionKey '{key}' when converting to id.");
                    return -1;
                }
                return ActionKeys.IndexOf(key);
            }
            public static string IdToKey(int id)
            {
                if (id < 0 || id >= ActionKeys.Count)
                {
                    ZiMain.log.LogWarning($"Unknown actionId '{id}' when converting to key.");
                    return null;
                }
                return ActionKeys[id];
            }
        }

        

        

        public static void Init()
        {

        }
        public static void FirstTimeSetup()
        {

        }
        //public static float GetItemPrio(uint itemID)
        //{
        //    foreach (var kv in enabledItemPrios)
        //    {
        //        if (kv.Key == itemID)
        //        {
        //            if (!kv.Value) return 0f;

        //            foreach (var kv2 in itemPrios)
        //            {
        //                if (kv2.Key == itemID)
        //                    return kv2.Value;
        //            }
        //            return 0f;
        //        }
        //    }
        //    return 0f;
        //}
        //public static void ResetAllItemPrio()
        //{
        //    ZiMain.log.LogMessage("Ressting all item priorties to default");
        //    foreach (var kvp in OriginalItemPrios)
        //    {
        //        SetBotItemPriority(kvp.Key, kvp.Value);
        //    }
        //}
        //public static void ResetItemPrio(uint itemID)
        //{
        //    if (!itemPrios.ContainsKey(itemID))
        //        return;
        //    itemPrios[itemID] = OriginalItemPrios[itemID];
        //}
        //public static void ToggleItemPrioDisabled(uint itemID)
        //{
        //    if (!enabledItemPrios.ContainsKey(itemID))
        //        return;
        //    SetItemPrioDisabled(itemID, !enabledItemPrios[itemID]);
        //}
        //public static void SetItemPrioDisabled(uint itemID, bool allowed, ulong netSender = 0)
        //{
        //    if (!enabledItemPrios.ContainsKey(itemID))
        //    {
        //        ZiMain.log.LogWarning($"Attemted to set item pick up allow for unknown id: {itemID} - {allowed}");
        //        return;
        //    }
        //    if (netSender == 0)
        //    {
        //        pStructs.pItemPrioDisable info = new pStructs.pItemPrioDisable();
        //        info.id = itemID;
        //        info.allowed = allowed;
        //        NetworkAPI.InvokeEvent<pStructs.pItemPrioDisable>("SetItemPrioDisable", info);
        //    }
        //    enabledItemPrios[itemID] = allowed;
        //}
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
        //public static string ConvertItemPublicName(string name)
        //{
        //    if (consumableItemNames.Contains(name))
        //    {
        //        return name;
        //    }
        //    if (consumableItemPublicNames.Contains(name))
        //    {
        //        return consumableItemNames[consumableItemPublicNames.IndexOf(name)];
        //    }
        //    //ZiMain.log.LogWarning($"Name '{glowstickName}' doesn't apear to be a consumable.");
        //    //ZiMain.log.LogWarning($"consumableItemNames:");
        //    //ZiMain.log.LogWarning(string.Join("\n",consumableItemNames));
        //    //ZiMain.log.LogWarning($"consumableItemPublicNames:");
        //    //ZiMain.log.LogWarning(string.Join("\n", consumableItemPublicNames));
        //    return name;
  
        //}
        //public static void SetResourceThreshold(uint itemID, int threshold, ulong netSender = 0)
        //{
        //    if (!resourceThresholds.ContainsKey(itemID))
        //    {
        //        ZiMain.log.LogWarning($"Attemted to set resource threshold for unknown id: {itemID} - {threshold}");
        //        return;
        //    }
        //    if (netSender == 0)
        //    {
        //        pStructs.pResourceThreshold info = new pStructs.pResourceThreshold();
        //        info.id = itemID;
        //        info.threshold = threshold;
        //        NetworkAPI.InvokeEvent<pStructs.pResourceThreshold>("SetResourceThreshold", info);
        //    }
        //    resourceThresholds[itemID] = Math.Clamp(threshold, 0, 100);
        //}
        //public static int GetResourceThreshold(uint itemID)
        //{
        //    if (!resourceThresholds.ContainsKey(itemID))
        //    {
        //        ZiMain.log.LogWarning($"Attemted to get resource threshold for unknown id: {itemID}");
        //        return 0;
        //    }
        //    return resourceThresholds[itemID];
        //}
        //private static bool SetBotItemPriority(string itemName, float priority)
        //{
        //    itemName = ConvertItemPublicName(itemName);
        //    if (fullGlowStickNames.Contains(itemName))
        //    { 
        //        //Why are there 4 glowsticks?!
        //        //And why does the red one have the color first?!

        //        foreach (string glowStick in fullGlowStickNames)
        //        {
        //            bool uhOh = I_SetBotItemPriority(glowStick, priority);
        //            if (!uhOh)
        //            {
        //                ZiMain.log.LogError($"Something went VERY wrong. {glowStick} not found!");
        //                return false;
        //            }
        //        }
        //        return true;
        //    }
        //    return I_SetBotItemPriority(itemName,priority);
        //}
        //public static bool SetBotItemPriority(uint id, float priority, ulong netSender = 0) //flowthrough main
        //{
        //    if (netSender == 0)
        //    {
        //        pItemPrio info = new pItemPrio();
        //        info.id = id;
        //        info.prio = priority;
        //        NetworkAPI.InvokeEvent<pStructs.pItemPrio>("SetItemPrio",info);
        //    }
        //    I_SetBotItemPriority(id, priority);
        //    return false;
        //}
        //private static bool I_SetBotItemPriority(string itemName, float priority)
        //{
        //    if (ItemDataBlock.s_blockIDByName.ContainsKey(itemName))
        //    {
        //        uint id = ItemDataBlock.GetBlockID(itemName);
        //        return I_SetBotItemPriority(id, priority);
        //    }
        //    return false;
        //}
        //private static bool I_SetBotItemPriority(uint itemID, float priority)
        //{
        //    if (ItemDataBlock.s_blockByID.ContainsKey(itemID))
        //    {
        //        itemPrios[itemID] = priority;
        //        if (!PlayerAIBot.s_recognisedItemTypes.Contains(itemID))
        //            PlayerAIBot.s_recognisedItemTypes.Add(itemID);
        //        if (GlowStickIds.Contains(itemID))
        //        {
        //            foreach (var glowstickID in GlowStickIds)
        //            {
        //                itemPrios[glowstickID] = priority;
        //            }
        //        }
        //        return true;
        //    }
        //    return false;
        //}

        //TODO - refactor this bullshit
        //public static void OldSetActionPermission(string actionKey, bool allowed, int playerID = -1, ulong netSender = 0)
        //{
        //    if (!zSlideComputer.PermissionDefinitions.KeyExists(actionKey))
        //    {
        //        ZiMain.log.LogWarning($"Unknown actionKey '{actionKey}' when setting action perms for id:{playerID}, allowed:{allowed}.");
        //        return;
        //    }
        //    if (netSender == 0)
        //    {
        //        pStructs.pGenericPermission info = new pStructs.pGenericPermission();
        //        info.playerID = playerID;
        //        info.allowed = allowed;
        //        int id = zSlideComputer.PermissionDefinitions.KeyToId(actionKey);
        //        if (id == -1)
        //        {
        //            ZiMain.log.LogWarning($"Unknown actionKey '{actionKey}' when setting action perms for id:{playerID}, allowed:{allowed}.");
        //            return;
        //        }
        //        info.actionID = id;
        //        NetworkAPI.InvokeEvent<pStructs.pGenericPermission>($"SetActionPermission", info);
        //    }
        //    ZiMain.log.LogMessage($"Setting {actionKey} perm for id {playerID} to {allowed}");
        //    zSlideComputer.PermissionDefinitions.SetAllowed(actionKey, allowed, playerID);
        //    if (playerID != -1)
        //    {
        //        PlayerManager.TryGetPlayerAgent(ref playerID, out var agent);
        //        if (agent == null)
        //            return;
        //        foreach (Type type in zSlideComputer.PermissionDefinitions.GetActionTypesToCull(actionKey))
        //            RemoveActionsOfType(agent, type);
        //        return;
        //    }
        //    var bots = ZiMain.GetBotList();
        //    foreach (PlayerAIBot bot in bots)
        //    {
        //        playerID = bot.Agent.Owner.PlayerSlotIndex();
        //        PlayerManager.TryGetPlayerAgent(ref playerID, out var agent);
        //        if (agent == null)
        //            continue;
        //        foreach (Type type in zSlideComputer.PermissionDefinitions.GetActionTypesToCull(actionKey))
        //            RemoveActionsOfType(agent, type);
        //    }
        //}
        //internal static void GenericToggleAllowed(string actionKey, bool allowDissabled = false)
        //{
        //    sMenu.sMenuNode node = zSlideComputer.actionNameToMenuNodes[actionKey];
        //    GenericToggleAllowed(actionKey, node, allowDissabled);
        //}
        internal static void GenericToggleAllowed(string actionKey, sMenu.sMenuNode node, bool allowDissabled = false)
        {
            if (!allowDissabled && !node.gameObject.activeInHierarchy)
                return;
            bool allowed = !(bool)zSlideComputer.ActionPermissions.ValueAt(actionKey);
            zSlideComputer.ActionPermissions.SetValue(actionKey, allowed);
        }

        //public static void ToggleResourceSharePermission(uint itemID)
        //{
        //    if (!enabledResourceShares.ContainsKey(itemID))
        //        return;
        //    SetResourceSharePermission(itemID, !GetResourceSharePermission(itemID));
        //}
        //public static bool GetResourceSharePermission(uint itemID)
        //{
        //    if (!enabledResourceShares.ContainsKey(itemID))
        //    {
        //        ZiMain.log.LogWarning($"tried to get resoruce share perms for unkown item id:{itemID}.");
        //        return false;
        //    }
        //    return enabledResourceShares[itemID];
        //}
        //public static void SetResourceSharePermission(uint itemID, bool allowed, ulong netSender = 0)
        //{
        //    if (!enabledResourceShares.ContainsKey(itemID))
        //        return;
        //    if (netSender == 0)
        //    {
        //        pStructs.pResourceThresholdDisable info = new pStructs.pResourceThresholdDisable();
        //        info.id = itemID;
        //        info.allowed = allowed;
        //        NetworkAPI.InvokeEvent<pStructs.pResourceThresholdDisable>("SetResourceThresholdDisable", info);
        //    }
        //    enabledResourceShares[itemID] = allowed;
        //}
        //public static void ToggleSharePermission(Dictionary<int, bool> botSelection)
        //{
        //    var disabledCount = 0;
        //    var enabledCount = 0;
        //    foreach (var bot in botSelection)
        //    {
        //        if (!bot.Value) //unselected, ignore.
        //            continue;
        //        if (GetSharePermission(bot.Key))
        //            enabledCount++;
        //        else
        //            disabledCount++;
        //    }
        //    bool majority = enabledCount > disabledCount;
        //    foreach (var bot in botSelection)
        //    {
        //        if (!bot.Value) //unselected, ignore.
        //            continue;
        //        SetSharePermission(bot.Key, !majority);
        //    }
        //}
        //Using bot.Agent.Owner.PlayerSlotIndex() as a key is good for transfering over network.
        //Issues may arrise when the number of bots changes mid mission.  
        //TODO handle that
        //TODO add more variants of get/set/toggle/flip for all settings and possible ways you'd want to call it.
        //public static void FlipPickupPermission()
        //{
        //    foreach (var bot in SelectionMenuClass.botSelection)
        //    {
        //        if (!bot.Value) //unselected, ignore.
        //            continue;
        //        TogglePickupPermission(bot.Key);
        //    }
        //}
        //public static void FlipPickupPermission(List<int> idList)
        //{
        //    foreach (int id in idList)
        //    {
        //        SetPickupPermission(id, !GetPickupPermission(id));
        //    }
        //}
        //public static void FlipPickupPermission(List<PlayerAIBot> botList)
        //{
        //    foreach (PlayerAIBot bot in botList)
        //    {
        //        SetPickupPermission(bot,!GetPickupPermission(bot));
        //    }
        //}
        //public static void FlipPickupPermission(Dictionary<int, bool> botSelection)
        //{
        //    foreach (var bot in botSelection)
        //    {
        //        if (!bot.Value) //unselected, ignore.
        //            continue;
        //        TogglePickupPermission(bot.Key);
        //    }
        //}
        //public static void TogglePickupPermission()
        //{ 
        //    var botSelection = SelectionMenuClass.botSelection;
        //    TogglePickupPermission(botSelection);
        //}
        //public static void TogglePickupPermission(int id)
        //{
        //    SetPickupPermission(id, !GetPickupPermission(id));
        //}
        //public static void TogglePickupPermission(List<int> idList)
        //{
        //    var disabledCount = 0;
        //    var enabledCount = 0;
        //    foreach (int id in idList)
        //    {
        //        if (GetPickupPermission(id))
        //            enabledCount++;
        //        else
        //            disabledCount++;
        //    }
        //    bool majority = enabledCount > disabledCount;
        //    foreach (int id in idList)
        //    {
        //        SetPickupPermission(id, !majority);
        //    }
        //}
        //public static void TogglePickupPermission(List<PlayerAIBot> botList)
        //{
        //    var disabledCount = 0;
        //    var enabledCount = 0;
        //    foreach (PlayerAIBot bot in botList)
        //    {
        //        if (GetPickupPermission(bot))
        //            enabledCount++;
        //        else
        //            disabledCount++;
        //    }
        //    bool majority = enabledCount > disabledCount;
        //    foreach (PlayerAIBot bot in botList)
        //    {
        //        SetPickupPermission(bot, !majority);
        //    }
        //}
        public static void RemoveActionsOfType(List<Type> actionTypes)
        {
            foreach (var actionType in actionTypes)
                RemoveActionsOfType(actionType);
        }
        public static void RemoveActionsOfType(Type actionType)
        { 
            var allBots = ZiMain.GetBotList();
            foreach(var bot in allBots)
                RemoveActionsOfType(bot.Agent, actionType);
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
        //public static void SetSharePermission(int playerID, bool allowed, ulong netSender = 0)
        //{
        //    if (!SharePerms.ContainsKey(playerID))
        //    {
        //        ZiMain.log.LogWarning($"Tried to set share perm for invalid player id: {playerID}, allowed:{allowed}");
        //        return;
        //    }
        //    if (netSender == 0)
        //    {
        //        pStructs.pSharePermission info = new pStructs.pSharePermission();
        //        info.playerID = playerID;
        //        info.allowed = allowed;
        //        NetworkAPI.InvokeEvent<pStructs.pSharePermission>("SetSharePermission", info);
        //    }
        //    ZiMain.log.LogMessage($"Setting share perm for id {playerID} to {allowed}");
        //    SharePerms[playerID] = allowed;
        //    PlayerManager.TryGetPlayerAgent(ref playerID, out var agent);
        //    if (agent != null)
        //        RemoveActionsOfType(agent, typeof(PlayerBotActionShareResourcePack));
        //}
        //public static void SetPickupPermission(int playerID, bool allowed, ulong netSender = 0)
        //{
        //    if (!PickUpPerms.ContainsKey(playerID))
        //    {
        //        ZiMain.log.LogWarning($"Tried to set pickup perm for invalid player id: {playerID}, allowed:{allowed}");
        //        return;
        //    }
        //    if (netSender == 0)
        //    {
        //        pStructs.pPickupPermission info = new pStructs.pPickupPermission();
        //        info.playerID = playerID;
        //        info.allowed = allowed;
        //        NetworkAPI.InvokeEvent<pStructs.pPickupPermission>("SetPickupPermission", info);
        //    }
        //    ZiMain.log.LogMessage($"Setting pickup perm for id {playerID} to {allowed}");
        //    PickUpPerms[playerID] = allowed;
        //    PlayerManager.TryGetPlayerAgent(ref playerID, out var agent);
        //    if (agent != null)
        //        RemoveActionsOfType(agent, typeof(PlayerBotActionCollectItem));
        //}
        //public static void SetPickupPermission(PlayerAIBot bot, bool allowed)
        //{
        //    SetPickupPermission(bot.Agent.Owner.PlayerSlotIndex(), allowed);
        //}
        //public static void SetPickupPermission(List<int> idList, bool allowed)
        //{
        //    foreach (int id in idList)
        //        SetPickupPermission(id, allowed);
        //}
        //public static void SetPickupPermission(List<PlayerAIBot> botList, bool allowed)
        //{
        //    foreach (var bot in botList)
        //        SetPickupPermission(bot.Agent.Owner.PlayerSlotIndex(), allowed);
        //}
        //public static bool GetPickupPermission(PlayerAIBot bot)
        //{
        //    if (bot == null)
        //    {
        //        ZiMain.log.LogError($"Can't get pickup perms when bot is null");
        //        return true;
        //    }
        //    if (bot.Agent == null) 
        //    { 
        //        ZiMain.log.LogError($"Can't get pickup perms when Agent is null");
        //        return true; 
        //    }
        //    if (bot.Agent.Owner == null) 
        //    {
        //        ZiMain.log.LogError($"Can't get pickup perms when Owner is null");
        //        return true; 
        //    }
        //    return PickUpPerms[bot.Agent.Owner.PlayerSlotIndex()];
        //}
        //public static bool GetPickupPermission(int id)
        //{
        //    if (PickUpPerms.ContainsKey(id))
        //        return PickUpPerms[id];
        //    ZiMain.log.LogWarning($"Unknown bot asked for pickup perms id:{id}.");
        //    return false;
        //}

        //public static bool GetActionPermission(string actionKey, int playerID = 1)
        //{
        //    if (zSlideComputer.PermissionDefinitions.KeyExists(actionKey))
        //    {
        //        return zSlideComputer.PermissionDefinitions.GetAllowed(actionKey, playerID);
        //    }
        //    else
        //    {
        //        ZiMain.log.LogWarning($"Unknown actionKey '{actionKey}' when getting action perms for id:{playerID}.");
        //        return false;
        //    }
        //}
        
        //public static bool GetUnlockPermission(int id)
        //{
        //    if (perms["Unlock"].ContainsKey(id))
        //        return perms["Unlock"][id];
        //    ZiMain.log.LogWarning($"Unknown bot asked for unlock perms id:{id}.");
        //    return false;
        //}

        //public static void SetUnlockPermission(int playerID, bool allowed, ulong netSender = 0)
        //{
        //    if (!perms["Unlock"].ContainsKey(playerID))
        //    {
        //        ZiMain.log.LogWarning($"Tried to set unlock perm for invalid player id: {playerID}, allowed:{allowed}");
        //        return;
        //    }
        //    if (netSender == 0)
        //    {
        //        pStructs.pSharePermission info = new pStructs.pSharePermission();
        //        info.playerID = playerID;
        //        info.allowed = allowed;
        //        NetworkAPI.InvokeEvent<pStructs.pSharePermission>("SetUnlockPermission", info);
        //    }
        //    ZiMain.log.LogMessage($"Setting unlock perm for id {playerID} to {allowed}");
        //    perms["Unlock"][playerID] = allowed;
        //    PlayerManager.TryGetPlayerAgent(ref playerID, out var agent);
        //    if (agent != null)
        //        RemoveActionsOfType(agent, typeof(PlayerBotActionUnlock));
        //}


        //public static bool GetBioTrackerPermission(int id)
        //{
        //    if (BioTrackerPerms.ContainsKey(id))
        //        return BioTrackerPerms[id];
        //    ZiMain.log.LogWarning($"Unknown bot asked for BioTracker perms id:{id}.");
        //    return false;
        //}

        //public static void SetBioTrackerPermission(int playerID, bool allowed, ulong netSender = 0)
        //{
        //    if (!BioTrackerPerms.ContainsKey(playerID))
        //    {
        //        ZiMain.log.LogWarning($"Tried to set BioTracker perm for invalid player id: {playerID}, allowed:{allowed}");
        //        return;
        //    }
        //    if (netSender == 0)
        //    {
        //        pStructs.pSharePermission info = new pStructs.pSharePermission();
        //        info.playerID = playerID;
        //        info.allowed = allowed;
        //        NetworkAPI.InvokeEvent<pStructs.pSharePermission>("SetBioTrackerPermission", info);
        //    }
        //    ZiMain.log.LogMessage($"Setting BioTracker perm for id {playerID} to {allowed}");
        //    BioTrackerPerms[playerID] = allowed;
        //    //PlayerManager.TryGetPlayerAgent(ref playerID, out var agent);
        //    //if (agent != null)
        //    //{
        //    //    RemoveActionsOfType(agent, typeof(PlayerBotActionUseEnemyScanner));
        //    //    RemoveActionsOfType(agent, typeof(PlayerBotActionUseBioscan));
        //    //}
        //}

        //public static bool GetRevivePlayersPermission(int id)
        //{
        //    if (RevivePlayersPerms.ContainsKey(id))
        //        return RevivePlayersPerms[id];
        //    ZiMain.log.LogWarning($"Unknown bot asked for revive player perms id:{id}.");
        //    return false;
        //}
        //public static bool GetReviveBotsPermission(int id)
        //{
        //    if (ReviveBotsPerms.ContainsKey(id))
        //        return ReviveBotsPerms[id];
        //    ZiMain.log.LogWarning($"Unknown bot asked for revive bot perms id:{id}.");
        //    return false;
        //}
        //public static void SetRevivePlayersPermission(int playerID, bool allowed, ulong netSender = 0)
        //{
        //    if (!RevivePlayersPerms.ContainsKey(playerID))
        //    {
        //        ZiMain.log.LogWarning($"Tried to set Revive player perm for invalid player id: {playerID}, allowed:{allowed}");
        //        return;
        //    }
        //    if (netSender == 0)
        //    {
        //        pStructs.pSharePermission info = new pStructs.pSharePermission();
        //        info.playerID = playerID;
        //        info.allowed = allowed;
        //        NetworkAPI.InvokeEvent<pStructs.pSharePermission>("SetRevivePlayerPermission", info);
        //    }
        //    ZiMain.log.LogMessage($"Setting Revive player perm for id {playerID} to {allowed}");
        //    RevivePlayersPerms[playerID] = allowed;
        //    PlayerManager.TryGetPlayerAgent(ref playerID, out var agent);
        //    if (agent != null)
        //    {
        //        RemoveActionsOfType(agent, typeof(PlayerBotActionRevive));
        //    }
        //}
        //public static void SetReviveBotsPermission(int playerID, bool allowed, ulong netSender = 0)
        //{
        //    if (!ReviveBotsPerms.ContainsKey(playerID))
        //    {
        //        ZiMain.log.LogWarning($"Tried to set Revive bot perm for invalid player id: {playerID}, allowed:{allowed}");
        //        return;
        //    }
        //    if (netSender == 0)
        //    {
        //        pStructs.pSharePermission info = new pStructs.pSharePermission();
        //        info.playerID = playerID;
        //        info.allowed = allowed;
        //        NetworkAPI.InvokeEvent<pStructs.pSharePermission>("SetReviveBotPermission", info);
        //    }
        //    ZiMain.log.LogMessage($"Setting Revive bot perm for id {playerID} to {allowed}");
        //    ReviveBotsPerms[playerID] = allowed;
        //    PlayerManager.TryGetPlayerAgent(ref playerID, out var agent);
        //    if (agent != null)
        //    {
        //        RemoveActionsOfType(agent, typeof(PlayerBotActionRevive));
        //    }
        //}

        //public static bool GetAttackPermission(int id)
        //{
        //    if (AttackPerms.ContainsKey(id))
        //        return AttackPerms[id];
        //    ZiMain.log.LogWarning($"Unknown bot asked for Attack perms id:{id}.");
        //    return false;
        //}

        //public static void SetAttackPermission(int playerID, bool allowed, ulong netSender = 0)
        //{
        //    if (!AttackPerms.ContainsKey(playerID))
        //    {
        //        ZiMain.log.LogWarning($"Tried to set Attack perm for invalid player id: {playerID}, allowed:{allowed}");
        //        return;
        //    }
        //    if (netSender == 0)
        //    {
        //        pStructs.pSharePermission info = new pStructs.pSharePermission();
        //        info.playerID = playerID;
        //        info.allowed = allowed;
        //        NetworkAPI.InvokeEvent<pStructs.pSharePermission>("SetAttackPermission", info);
        //    }
        //    ZiMain.log.LogMessage($"Setting Attack perm for id {playerID} to {allowed}");
        //    AttackPerms[playerID] = allowed;
        //    PlayerManager.TryGetPlayerAgent(ref playerID, out var agent);
        //    if (agent != null)
        //    {
        //        RemoveActionsOfType(agent, typeof(PlayerBotActionAttack));
        //    }
        //}
        
        //public static bool GetPingPermission(int id)
        //{
        //    if (PingPerms.ContainsKey(id))
        //        return PingPerms[id];
        //    ZiMain.log.LogWarning($"Unknown bot asked for unlock perms id:{id}.");
        //    return false;
        //}
        //public static bool GetPingPermission(PlayerAIBot bot)
        //{
        //    if (bot == null)
        //    {
        //        ZiMain.log.LogError($"Can't get pickup perms when bot is null");
        //        return true;
        //    }
        //    if (bot.Agent == null)
        //    {
        //        ZiMain.log.LogError($"Can't get pickup perms when Agent is null");
        //        return true;
        //    }
        //    if (bot.Agent.Owner == null)
        //    {
        //        ZiMain.log.LogError($"Can't get pickup perms when Owner is null");
        //        return true;
        //    }
        //    return PingPerms[bot.Agent.Owner.PlayerSlotIndex()];
        //}
        //public static void SetPingPermission(PlayerAIBot bot, bool allowed)
        //{
        //    SetPingPermission(bot.Agent.Owner.PlayerSlotIndex(), allowed);
        //}
        //public static void SetPingPermission(List<int> idList, bool allowed)
        //{
        //    foreach (int id in idList)
        //        SetPingPermission(id, allowed);
        //}
        //public static void SetPingPermission(List<PlayerAIBot> botList, bool allowed)
        //{
        //    foreach (var bot in botList)
        //        SetPingPermission(bot.Agent.Owner.PlayerSlotIndex(), allowed);
        //}
        //public static void SetPingPermission(int playerID, bool allowed, ulong netSender = 0)
        //{
        //    if (!PingPerms.ContainsKey(playerID))
        //    {
        //        ZiMain.log.LogWarning($"Tried to set unlock perm for invalid player id: {playerID}, allowed:{allowed}");
        //        return;
        //    }
        //    if (netSender == 0)
        //    {
        //        pStructs.pSharePermission info = new pStructs.pSharePermission();
        //        info.playerID = playerID;
        //        info.allowed = allowed;
        //        NetworkAPI.InvokeEvent<pStructs.pSharePermission>("SetPingPermission", info);
        //    }
        //    ZiMain.log.LogMessage($"Setting ping perm for id {playerID} to {allowed}");
        //    PingPerms[playerID] = allowed;
        //    PlayerManager.TryGetPlayerAgent(ref playerID, out var agent);
        //    if (agent != null)
        //        RemoveActionsOfType(agent, typeof(PlayerBotActionHighlight));
        //}
        //public static bool GetSharePermission(PlayerAIBot bot)
        //{
        //    if (bot == null)
        //    {
        //        ZiMain.log.LogError($"Can't get share perms when bot is null");
        //        return true;
        //    }
        //    if (bot.Agent == null)
        //    {
        //        ZiMain.log.LogError($"Can't get share perms when Agent is null");
        //        return true;
        //    }
        //    if (bot.Agent.Owner == null)
        //    {
        //        ZiMain.log.LogError($"Can't get share perms when Owner is null");
        //        return true;
        //    }
        //    return SharePerms[bot.Agent.Owner.PlayerSlotIndex()];
        //}
        //public static bool GetSharePermission(int id)
        //{
        //    if (SharePerms.ContainsKey(id))
        //        return SharePerms[id];
        //    ZiMain.log.LogWarning($"Unknown bot asked for share perms id:{id}.");
        //    return false;
        //}
    }
}

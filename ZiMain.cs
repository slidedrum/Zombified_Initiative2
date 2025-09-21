using Agents;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Enemies;
using GTFO.API;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using LevelGeneration;
using Player;
using SNetwork;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using ZombieTweak2;
using ZombieTweak2.zMenu;
using ZombieTweak2.zNetworking;
using static ZombieTweak2.zNetworking.pStructs;

/*
 == TODO == Priority: todo menu breaks when loading checkpoint

 -- TODO -- DONE -- Re - create send bot to do manual action
 -- TODO -- DONE -- fix pickup action failing sometimes
 -- TODO -- DONE -- Change cancel to look up
 -- TODO -- DONE -- make look down select yourself
 -- TODO -- DONE -- Customize resource share thresholds, or however that works
 -- TODO -- DONE -- Refactor all NetworkAPI usage
 -- TODO -- DONE -- Make buttons change color when held
 -- TODO -- DONE -- Customizable resource share thresholds
 -- TODO -- DONE -- Block individual resource shares
 -- TODO -- Investigate if using playerslotindex as an ID is problematic when number of bots changes.  Swtich to some other ID if needed.
 -- TODO -- When blocking actions (resource pickup/share/etc) chaeck if any existing actions exist, and cancel them instantly.
 -- TOOD -- When sharing resources, if someone else is already giving the target the same item, don't double up.
 -- TODO -- Replace selected bots system with global settings, and then bot spesific overides.
 -- TODO -- Dynamically remove the bot selection/overide menu when there is only 1 bot.
 -- TODO -- Remove ReceiveZINetInfo completely.
 -- TODO -- Remove zlogger enterly.
 -- TODO -- Refactor FlexibleEvent to use args=[] instead of a new arg.  this lets me have optional args for more functonality.
 -- TODO -- Make system for lerping between to values over time.  Should be arbitrary vars and maybe even support method args somehow.
 -- TODO -- Make "i need health/ammo" quck action overide share permission
 -- TODO -- Make smart select pick up turrets
 -- TODO -- Fix bot extra data only updating when you look away
 -- TODO -- Detect end of extra info string instead of assuming this is the last thing for better compat with other mods.
 -- TODO -- Move methods arround to other classes that make more sense
 -- TODO -- Handle bots joining/leaving or any other way the bot count can change mid mission.
 -- TODO -- Error when exiting q menu if radial menu is open
 -- TODO -- Unheld selected might have problems.
 -- TODO -- Double tap smart select on a bot to have them follow you.
 -- TODO -- Move updateNodeThresholdDisplay and similar to the set methds not as node listeners.
 -- TODO -- Add option to let bots open lockers
 -- TODO -- Add per bot overides for individual share/pickup perms.
 -- TODO -- Add sounds
 -- TODO -- Add options menu with things like default states and key rebinding
 -- TODO -- Add mele only restriction
 -- TODO -- When perms removed, remove any current actions that are no longer allowed
 -- TODO -- Add quick settings part of the menu for things like "auto select followed bots" 
 -- TODO -- Add STFU button
 -- TODO -- Add option for menue's to have seprate x/y scale.
 -- TODO -- Add external list of manual actions.  be sure to clean it when actions are terminated.  add them from SendBotTo- methods.
 -- TODO -- Use a string builder ZiMain.onActionRemoved
 -- TODO -- Move/refactor  GetAgent and getpStruct methods in ZiMain
 -- TODO -- Nullchecks in SendBotToShareResourcePack
 -- TODO -- Clear out and remove PlayConfirmSound hook.
 -- TODO -- Remake SendBotToKillEnemy method
 -- TODO -- Move SetRelativePosition into a listener so it can be disabled.
 -- TODO -- Add option to change menu close angle
 -- TODO -- Make text parts in nodes private and add setters and getters for font stuff.
 -- TODO -- Add menu title and subtitle. Use that for tooltips.
 -- TODO -- Change the way scroll priority works to visual treat 0 prio as red and disabled
 -- TODO -- Make updateNodePriorityDisplay and similar method's args order consistant
 -- TODO -- Remove the flip/toggle selection nodes, and instead make hold action
 -- TODO -- Add arange node offests and possible different node aragement types
 -- TODO -- Add option to set selection to the bots that are following you.
 -- TODO -- Add node ID system as it might be an issue if you have two nodes that have the same text for some reason.
 -- TODO -- Inside pickup perms details menu make 5 item filters that you can switch between by scrolling on center node.
            ALL - ENCOUNTERED - RESOURCES - PLACEABLES - THROWABLES - FAVORITES
 -- TODO -- Set methods for text parts
 -- TODO -- Come up with a better more consistant naming scheme for pStructs and encoding/decoding methods
 -- TODO -- Make network packets only send after a 100ms delay, and send the most up to date value 100ms later.
 -- TODO -- Make clients ask host for current value after every settings change to resolve dysync and conflicts.
 -- TODO -- Send inventory sync command when bots run out of resources from a manual action?
 -- TODO -- Investigate what is causing recolor of some menu elements with other mods (Archive essentials?)
 -- TODO -- Investigate what is causing first letter of bot name to mess up with other mods (Arhcive essentials?)
 -- TODO -- Investigate compat with BetterBots.  Seems to break pickup blocking?
 -- TODO -- In smart select, add some leeway for very close objects, maybe inside of 2 units lerp between max angle of given, and 180 at 0 units.
 -- TODO -- Remake attack my target methods and put it under actions.

share with placed turrets

Investigate bots not picking up held items like turbines
Something breaks with essensials and better bots
[Error  :Il2CppInterop] Exception in IL2CPP-to-Managed trampoline, not passing it to il2cpp: Il2CppInterop.Runtime.Il2CppException: System.NullReferenceException: Object reference not set to an instance of an object.
--- BEGIN IL2CPP STACK TRACE ---
System.NullReferenceException: Object reference not set to an instance of an object.

--- END IL2CPP STACK TRACE ---

   at Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(IntPtr returnedException) in /home/runner/work/Il2CppInterop/Il2CppInterop/Il2CppInterop.Runtime/Il2CppException.cs:line 36
   at UnityEngine.Component.get_transform()
   at ZombieTweak2.zMenu.zMenu.MoveInfrontOfCamera()
   at ZombieTweak2.zMenu.zMenu.Open()
   at ZombieTweak2.zMenu.zMenuManager.Update()
   at ZombieTweak2.FlexibleEvent.Invoke()
   at ZombieTweak2.zUpdater.Update()
   at Trampoline_VoidThisZombieTweak2.zUpdaterUpdate(IntPtr , Il2CppMethodInfo* )
[Error  :Il2CppInterop] Exception in IL2CPP-to-Managed trampoline, not passing it to il2cpp: Il2CppInterop.Runtime.Il2CppException: System.NullReferenceException: Object reference not set to an instance of an object.
--- BEGIN IL2CPP STACK TRACE ---
System.NullReferenceException: Object reference not set to an instance of an object.

--- END IL2CPP STACK TRACE ---

   at Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(IntPtr returnedException) in /home/runner/work/Il2CppInterop/Il2CppInterop/Il2CppInterop.Runtime/Il2CppException.cs:line 36
   at UnityEngine.Component.get_transform()
   at ZombieTweak2.zMenu.zMenu.MoveInfrontOfCamera()
   at ZombieTweak2.zMenu.zMenu.Open()
   at ZombieTweak2.zMenu.zMenuManager.Update()
   at ZombieTweak2.FlexibleEvent.Invoke()
   at ZombieTweak2.zUpdater.Update()
   at Trampoline_VoidThisZombieTweak2.zUpdaterUpdate(IntPtr , Il2CppMethodInfo* )
[Error  :Il2CppInterop] Exception in IL2CPP-to-Managed trampoline, not passing it to il2cpp: Il2CppInterop.Runtime.Il2CppException: System.NullReferenceException: Object reference not set to an instance of an object.
--- BEGIN IL2CPP STACK TRACE ---
System.NullReferenceException: Object reference not set to an instance of an object.

--- END IL2CPP STACK TRACE ---

   at Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(IntPtr returnedException) in /home/runner/work/Il2CppInterop/Il2CppInterop/Il2CppInterop.Runtime/Il2CppException.cs:line 36
   at UnityEngine.Component.get_transform()
   at ZombieTweak2.zMenu.zMenu.MoveInfrontOfCamera()
   at ZombieTweak2.zMenu.zMenu.Open()
   at ZombieTweak2.zMenu.zMenuManager.Update()
   at ZombieTweak2.FlexibleEvent.Invoke()
   at ZombieTweak2.zUpdater.Update()
   at Trampoline_VoidThisZombieTweak2.zUpdaterUpdate(IntPtr , Il2CppMethodInfo* )
[Error  :Il2CppInterop] Exception in IL2CPP-to-Managed trampoline, not passing it to il2cpp: Il2CppInterop.Runtime.Il2CppException: System.NullReferenceException: Object reference not set to an instance of an object.
--- BEGIN IL2CPP STACK TRACE ---
System.NullReferenceException: Object reference not set to an instance of an object.

--- END IL2CPP STACK TRACE ---

   at Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(IntPtr returnedException) in /home/runner/work/Il2CppInterop/Il2CppInterop/Il2CppInterop.Runtime/Il2CppException.cs:line 36
   at UnityEngine.Component.get_transform()
   at ZombieTweak2.zMenu.zMenu.MoveInfrontOfCamera()
   at ZombieTweak2.zMenu.zMenu.Open()
   at ZombieTweak2.zMenu.zMenuManager.Update()
   at ZombieTweak2.FlexibleEvent.Invoke()
   at ZombieTweak2.zUpdater.Update()
   at Trampoline_VoidThisZombieTweak2.zUpdaterUpdate(IntPtr , Il2CppMethodInfo* )
[Error  :Il2CppInterop] Exception in IL2CPP-to-Managed trampoline, not passing it to il2cpp: Il2CppInterop.Runtime.Il2CppException: System.NullReferenceException: Object reference not set to an instance of an object.
--- BEGIN IL2CPP STACK TRACE ---
System.NullReferenceException: Object reference not set to an instance of an object.

--- END IL2CPP STACK TRACE ---

   at Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(IntPtr returnedException) in /home/runner/work/Il2CppInterop/Il2CppInterop/Il2CppInterop.Runtime/Il2CppException.cs:line 36
   at UnityEngine.Component.get_transform()
   at ZombieTweak2.zMenu.zMenu.MoveInfrontOfCamera()
   at ZombieTweak2.zMenu.zMenu.Open()
   at ZombieTweak2.zMenu.zMenuManager.Update()
   at ZombieTweak2.FlexibleEvent.Invoke()
   at ZombieTweak2.zUpdater.Update()
   at Trampoline_VoidThisZombieTweak2.zUpdaterUpdate(IntPtr , Il2CppMethodInfo* )


 -- TODO -- BUG -- When holding a node then look away, when you re-open menu node still highlighted.

Want to make combine resource mod
want to make custom blacklist pickups - DONE
want to fix attack not always working
want to make attack wake room sometimes
want to make "clear room" command
want to completely re-write collection logic, not just priority logic
want to add chat commands for people who don't have the mod.
want to add new bot actions like hold position, look for resource type (in nearby rooms), ping item (go to term, then run ping command)
want to make "go here" command
want to make "home" location function where they "follow" a set location but aren't strickly stuck to it if they get into combat, similar to following a player.

found bot commands in PUI_CommunicationMenu.execute
public enum eGameEvent might be useful
*/


namespace Zombified_Initiative;

[BepInDependency("dev.gtfomodding.gtfo-api")]
[BepInPlugin("com.hirnukuono.zombified_initiative", "Zombified Initiative", "0.9.6")]
public class ZiMain : BasePlugin
{ //this class should contain all methods to call actions, any helpers to faciliate that, and inital setup,
    public static ManualLogSource log;

    public static Dictionary<string, PlayerAIBot> BotTable = new();
    public static PlayerChatManager _chatManager;
    public static PUI_CommunicationMenu _menu;
    public static bool rootmenusetup = false;

    public static float _manualActionsHaste = 1f;
    public static float _manualActionsPriority = 5f;
    public static List<PlayerBotActionBase> manualActions = new();
    [Obsolete]
    public struct ZINetInfo
    {

        // funktio assign target 0
        // funktio unassign target 1
        // funktio assignaim 2
        // funktio unassignaim 3
        // funktio fireguns 4
        public static string NetworkIdentity { get => nameof(ZINetInfo); }
        public int FUNC;
        public int SLOT;
        public int ITEMTYPE;
        public int ITEMSERIAL;
        public int AGENTID;

        public ZINetInfo(int func, int slot, int itemtype, int itemserial, int agentid) : this()
        {
            FUNC = func; SLOT = slot; ITEMTYPE = itemtype; ITEMSERIAL = itemserial;  AGENTID = agentid; 
            log.LogInfo($"sent a package {func} - {slot} - {itemtype} - {itemserial} - {agentid}");
        }
    }
    [Obsolete]
    public struct ZIInfo
    {
        public int FUNC;
        public int SLOT;
        public int ITEMTYPE;
        public int ITEMSERIAL;
        public int AGENTID;
        public ZIInfo(int func, int slot, int itemtype, int itemserial, int agentid) : this()
        {
            FUNC = func; SLOT = slot; ITEMTYPE = itemtype; ITEMSERIAL = itemserial; AGENTID = agentid;
        }
        public ZIInfo(ZINetInfo network) : this()
        {
            FUNC = network.FUNC;
            SLOT = network.SLOT;
            ITEMTYPE = network.ITEMTYPE;
            ITEMSERIAL = network.ITEMSERIAL;
            AGENTID = network.AGENTID;
        }
    }

    public override void Load()
    {
        Harmony m_Harmony = new Harmony("ZombieController");
        m_Harmony.PatchAll();
        ClassInjector.RegisterTypeInIl2Cpp<zUpdater>();
        ClassInjector.RegisterTypeInIl2Cpp<zCameraEvents>();
        //var ZombieController = AddComponent<zController>();

        //NetworkAPI.RegisterEvent<ZINetInfo>(ZINetInfo.NetworkIdentity, zController.ReceiveZINetInfo);
        //NetworkAPI.RegisterEvent<ZISendBotToPickupItemInfo>("sendBotToPickupItem", SendBotToPickupItem);

        NetworkAPI.RegisterEvent<pItemPrioDisable>          ("SetItemPrioDisable",              zNetworking.ReciveSetItemPrioDisable);
        NetworkAPI.RegisterEvent<pItemPrio>                 ("SetItemPrio",                     zNetworking.ReciveSetItemPrio);
        NetworkAPI.RegisterEvent<pResourceThreshold>        ("SetResourceThreshold",            zNetworking.reciveSetResourceThreshold);
        NetworkAPI.RegisterEvent<pResourceThresholdDisable> ("SetResourceThresholdDisable",     zNetworking.ReciveSetResourceThresholdDisable);
        NetworkAPI.RegisterEvent<pSharePermission>          ("SetSharePermission",              zNetworking.ReciveSetSharePermission);
        NetworkAPI.RegisterEvent<pPickupPermission>         ("SetPickupPermission",             zNetworking.ReciveSetPickupPermission);
        NetworkAPI.RegisterEvent<pPickupItemInfo>           ("RequestToPickupItem",             zNetworking.ReciveRequestToPickupItem);
        NetworkAPI.RegisterEvent<pShareResourceInfo>        ("RequestToShareResourcePack",      zNetworking.ReciveRequestToShareResource);

        //LG_Factory.add_OnFactoryBuildDone((Action)ZombieController.SetupCamera);
        LG_Factory.add_OnFactoryBuildDone((Action)zSlideComputer.Init);
        //EventAPI.OnExpeditionStarted += ZombieController.Initialize;
        log = Log;
        zActionSub.addOnRemoved((Action<PlayerAIBot, PlayerBotActionBase>)onActionRemoved);
        zActionSub.addOnAdded((Action<PlayerAIBot, PlayerBotActionBase>)onActionAdded);

        EventAPI.OnManagersSetup += () =>
        {
            zUpdater.CreateInstance();
            zUpdater.onUpdate.Listen(zMenuManager.Update);
            zUpdater.onLateUpdate.Listen(zMenuManager.LateUpdate);
            zUpdater.onUpdate.Listen(zSmartSelect.update);
        };
        LG_Factory.add_OnFactoryBuildDone((Action)zMenuManager.SetupCamera);
        LG_Factory.add_OnFactoryBuildDone((Action)zMenus.CreateMenus);
        
    }



    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {

    }

    private void onActionAdded(PlayerAIBot bot, PlayerBotActionBase action)
    {

    }
    public static bool isManualAction(PlayerBotActionBase action)
    {
        //TODO add external list of manual actions.  be sure to clean it when actions are terminated.
        string typeName = action.GetIl2CppType().Name;
        float haste = 0f;
        float prio = 0f;
        if (typeName == "PlayerBotActionCollectItem")
        {
            var descriptor = action.DescBase.Cast<PlayerBotActionCollectItem.Descriptor>();
            haste = descriptor.Haste;
            prio = descriptor.Prio;
        }
        else if (typeName == "PlayerBotActionShareResourcePack")
        {
            var descriptor = action.DescBase.Cast<PlayerBotActionShareResourcePack.Descriptor>();
            haste = descriptor.Haste;
            prio = descriptor.Prio;
        }
        else if (typeName == "PlayerBotActionAttack")
        {
            var descriptor = action.DescBase.Cast<PlayerBotActionAttack.Descriptor>();
            haste = descriptor.Haste;
            prio = descriptor.Prio;
        }
        if (haste == _manualActionsHaste && prio == _manualActionsPriority) return true;
        return false;
    }

    public static void onActionRemoved(PlayerAIBot bot , PlayerBotActionBase action)
    { //TODO - Use a string builder
        string typeName = action.GetIl2CppType().Name;
        bool manualAction = isManualAction(action);
        if (typeName == "PlayerBotActionCollectItem")
        {
            var descriptor = action.DescBase.Cast<PlayerBotActionCollectItem.Descriptor>();
            log.LogInfo($"{bot.Agent.PlayerName} completed collect {descriptor.TargetItem.PublicName} task with status: {action.DescBase.Status}  access layers {descriptor.m_accessLayers}");
            string article = manualAction ? "the" : "a";
            if (action.DescBase.Status == PlayerBotActionBase.Descriptor.StatusType.Successful)
            {
                InventorySlot slot = descriptor.TargetItem.ItemDataBlock.inventorySlot;
                AmmoType ammoType = slot == InventorySlot.Consumable ? AmmoType.CurrentConsumable : slot == InventorySlot.ResourcePack ? AmmoType.ResourcePackRel : AmmoType.None;
                float ammoLeft = bot.Backpack.AmmoStorage.GetAmmoInPack(ammoType);
                sendChatMessage($"I collected {article} {descriptor.TargetItem.PublicName} ({ammoLeft}).", bot.Agent);
            }
            else if (action.DescBase.Status == PlayerBotActionBase.Descriptor.StatusType.Failed)
                sendChatMessage($"I coul't get {article} {descriptor.TargetItem.PublicName}.", bot.Agent);
            else if (action.DescBase.Status == PlayerBotActionBase.Descriptor.StatusType.Interrupted)
                sendChatMessage($"I can't get {article} {descriptor.TargetItem.PublicName} right now.", bot.Agent);
            else if (action.DescBase.Status == PlayerBotActionBase.Descriptor.StatusType.Aborted)
                sendChatMessage($"I can't get {article} {descriptor.TargetItem.PublicName} right now.", bot.Agent);
            else if (action.DescBase.Status == PlayerBotActionBase.Descriptor.StatusType.Stopped)
                sendChatMessage($"I can't get {article} {descriptor.TargetItem.PublicName} right now.", bot.Agent);
            else
                sendChatMessage($"I can't get {article} {descriptor.TargetItem.PublicName} status {action.DescBase.Status}.", bot.Agent);
        }
        if (typeName == "PlayerBotActionShareResourcePack")
        {
            log.LogInfo("PlayerBotActionShareResourcePack removed");
            var descriptor = action.DescBase.Cast<PlayerBotActionShareResourcePack.Descriptor>();
            log.LogInfo("descriptor cast");
            float ammoLeft = bot.Backpack.AmmoStorage.GetAmmoInPack(AmmoType.ResourcePackRel);
            log.LogInfo($"Got ammo left {ammoLeft}");
            log.LogInfo($"{bot.Agent.PlayerName} completed share");
            log.LogInfo($" {descriptor.Item.PublicName} task with status: ");
            log.LogInfo($"{action.DescBase.Status}  ");
            log.LogInfo($"access layers {descriptor.m_accessLayers}");
            string article = manualAction ? "the" : "a";
            string receverOrMyslef = descriptor.Receiver == bot.Agent ? "myself" : descriptor.Receiver.PlayerName;
            log.LogInfo($"Got receiver or myself {receverOrMyslef}");
            if (action.DescBase.Status == PlayerBotActionBase.Descriptor.StatusType.Successful)
                sendChatMessage($"I gave {receverOrMyslef} {article} {descriptor.Item.PublicName} ({ammoLeft}%).", bot.Agent);
            else if (action.DescBase.Status == PlayerBotActionBase.Descriptor.StatusType.Failed)
                sendChatMessage($"I coul't give {receverOrMyslef} {article} {descriptor.Item.PublicName} ({ammoLeft}%).", bot.Agent);
            else if (action.DescBase.Status == PlayerBotActionBase.Descriptor.StatusType.Interrupted)
                sendChatMessage($"I can't give {receverOrMyslef} {article} {descriptor.Item.PublicName} ({ammoLeft}%) right now.", bot.Agent);
            else if (action.DescBase.Status == PlayerBotActionBase.Descriptor.StatusType.Aborted)
                sendChatMessage($"I can't give {receverOrMyslef} {article} {descriptor.Item.PublicName} ({ammoLeft}%) right now.", bot.Agent);
            else if (action.DescBase.Status == PlayerBotActionBase.Descriptor.StatusType.Stopped)
                sendChatMessage($"I can't give {receverOrMyslef} {article} {descriptor.Item.PublicName} ({ammoLeft}%) right now.", bot.Agent);
            else
                sendChatMessage($"I can't give {receverOrMyslef} {article} {descriptor.Item.PublicName} ({ammoLeft}%) status {action.DescBase.Status}.", bot.Agent);
        }
    }
    public static void slowUpdate()
    {
        
    }
    public static void sendChatMessage(string message,PlayerAgent sender = null, PlayerAgent receiver = null)
    {
        PlayerChatManager.WantToSentTextMessage(sender != null ? sender : PlayerManager.GetLocalPlayerAgent(), message, receiver);
    }
    [Obsolete]

    public static void attackMonster(string bot, EnemyAgent monster)
    {
        log.LogInfo($"bot " + bot + " attack");
        SendBotToKillEnemyOld(bot, monster, PlayerBotActionAttack.StanceEnum.All, PlayerBotActionAttack.AttackMeansEnum.All, PlayerBotActionWalk.Descriptor.PostureEnum.Stand);
    }

    public static List<PlayerAIBot> GetBotList()
    {
        //TODO this is bad there's got to be a better way.  Though it's pretty cheap regardless.
        //TODO add caching?
        List<PlayerAIBot> playerAiBots = new();
        var playerAgentsInLevel = PlayerManager.PlayerAgentsInLevel;
        foreach (var agent in playerAgentsInLevel)
        {
            if (agent.Owner.IsBot)
            {
                playerAiBots.Add(agent.gameObject.GetComponent<PlayerAIBot>());
            }
        }
        return playerAiBots;
    }
    public static void SendBotTokillEnemy(PlayerAIBot bot, Agent enemy, PlayerBotActionAttack.StanceEnum stance = PlayerBotActionAttack.StanceEnum.All, PlayerBotActionAttack.AttackMeansEnum means = PlayerBotActionAttack.AttackMeansEnum.All, PlayerBotActionWalk.Descriptor.PostureEnum posture = PlayerBotActionWalk.Descriptor.PostureEnum.Stand, PlayerAgent commander = null)
    {

    }
    public static Item TryGetItemInLevelFromItemData(pItemData itemData)
    {
        //Do we need this? This should probably move to pStructss
        Item item;
        PlayerBackpackManager.TryGetItemInLevelFromItemData(itemData, out item);
        return item;
    }
    public static PlayerAgent GetAgentFrom_pStruct(SNetStructs.pPlayer player_struct)
    {
        //Do we need this? This should probably move to pStructs
        if (!player_struct.TryGetPlayer(out SNet_Player player))
            return null;
        return player.PlayerAgent.TryCast<PlayerAgent>();
    }
    public static SNetStructs.pPlayer Get_pStructFromAgent(PlayerAgent agent)
    {
        //Do we need this?
        SNetStructs.pPlayer player = new();
        player.SetPlayer(agent.Owner);
        return player;
    }
    public static PlayerAgent GetAgentFrom_pPlayer(SNetStructs.pPlayer player_struct)
    {
        //Do we need this? This should probably move to pStructs
        if (!player_struct.TryGetPlayer(out SNet_Player player))
            return null;
        return player.PlayerAgent.TryCast<PlayerAgent>();
    }
    public static SNetStructs.pPlayer Get_pPlayerFromAgent(PlayerAgent agent)
    {
        //Do we need this? This should probably move to pStructs
        SNetStructs.pPlayer player = new();
        player.SetPlayer(agent.Owner);
        return player;
    }
    public static void SendBotToPickupItem(PlayerAIBot aiBot, ItemInLevel item, PlayerAgent commander = null, ulong netsender = 0)
    {
        //todo add to manual action list for refrence later.
        if (!SNet.IsMaster) //Are we a client?
        {
            if (netsender != 0) //Is this request coming from a different client?
                return;
            //request host
            pPickupItemInfo info = new pPickupItemInfo();
            info.item.replicatorRef.SetID(item.GetComponent<LG_PickupItem_Sync>().m_stateReplicator.Replicator); //TODO Nullcheck?
            //info.item = pStructs.Get_pStructFromRefrence(item);
            info.playerAgent = pStructs.Get_pStructFromRefrence(aiBot.Agent);
            info.commander = pStructs.Get_pStructFromRefrence(commander); //This might be a problem in commander is null?  Not sure. TODO look into it.
            NetworkAPI.InvokeEvent<pPickupItemInfo>("RequestToPickupItem", info);
            return;
        }
        log.LogInfo($"{commander.PlayerName} is sending {aiBot.Agent.PlayerName} to pick up {item.PublicName} with the new method");
        float prio = 5f;
        float haste = 1f;
        PlayerBotActionCollectItem.Descriptor desc = new(aiBot)
        {
            TargetItem = item,
            TargetContainer = item.container,
            TargetPosition = item.transform.position,
            Prio = prio,
            Haste = haste,
        };
        sendChatMessage($"Picking up {item.PublicName}",aiBot.Agent,commander);
        aiBot.StartAction(desc);
    }
    public static void SendBotToShareResourcePack(PlayerAIBot aiBot, PlayerAgent receiver, PlayerAgent commander = null, ulong netsender = 0)
    {
        //todo add to manual action list for refrence later.
        if (!SNet.IsMaster)//Are we a client?
        {
            if (netsender != 0)//Is this request coming from a different client?
                return;
            //request host
            pStructs.pShareResourceInfo info = new pStructs.pShareResourceInfo();
            info.sender =       pStructs.Get_pStructFromRefrence(aiBot.Agent);
            info.receiver =     pStructs.Get_pStructFromRefrence(receiver);
            info.commander =    pStructs.Get_pStructFromRefrence(commander); //This might be a problem in commander is null?  Not sure. TODO look into it.
            NetworkAPI.InvokeEvent<pStructs.pShareResourceInfo>("RequestToShareResourcePack", info);
            return;
        }
        float prio = 5f;
        float haste = 1f;
        BackpackItem backpackItem = null;
        ZiMain.log.LogInfo($"{aiBot.Agent.PlayerName} was told by {commander?.PlayerName ?? "someone"} with netid {netsender} to try to share resource pack with new method to {receiver.PlayerName}");
        var gotBackpackItem = aiBot.Backpack.HasBackpackItem(InventorySlot.ResourcePack) &&
                              aiBot.Backpack.TryGetBackpackItem(InventorySlot.ResourcePack, out backpackItem);
        if (!gotBackpackItem)
            return;
        var resourcePack = backpackItem.Instance.Cast<ItemEquippable>();
        aiBot.Inventory.DoEquipItem(resourcePack);//is this needed?  Does the action not handle this?
        PlayerBotActionShareResourcePack.Descriptor desc = new(aiBot)
        {
            Receiver = receiver,
            Item = resourcePack,
            Prio = prio,
            Haste = haste,
        };
        float ammoLeft = aiBot.Backpack.AmmoStorage.GetAmmoInPack(AmmoType.ResourcePackRel);
        sendChatMessage($"Sharing my {resourcePack.PublicName} ({ammoLeft}%) with {receiver.PlayerName}.",aiBot.Agent,commander);
        aiBot.StartAction(desc);
    }

    [Obsolete]
    public static void SendBotToKillEnemyOld(String chosenBot, Agent enemy, PlayerBotActionAttack.StanceEnum stance = PlayerBotActionAttack.StanceEnum.All, PlayerBotActionAttack.AttackMeansEnum means = PlayerBotActionAttack.AttackMeansEnum.All, PlayerBotActionWalk.Descriptor.PostureEnum posture = PlayerBotActionWalk.Descriptor.PostureEnum.Stand)
    { //TODO - might change chosen bot to be a PlayerAiBot instead of a string.
      // - otherwise looks fine?
        var bot = ZiMain.BotTable[chosenBot];
        if (bot == null)
            return;

        ExecuteBotActionOld(bot, new PlayerBotActionAttack.Descriptor(bot)
        {
            Stance = stance,
            Means = means,
            Posture = posture,
            TargetAgent = enemy,
            Prio = _manualActionsPriority,
            Haste = _manualActionsHaste,
        },
            "Added kill enemy action to " + bot.Agent.PlayerName, 0, bot.m_playerAgent.PlayerSlotIndex, 0, 0, enemy.m_replicator.Key + 1);
    }
    
    [Obsolete]
    public static void attackMyTarget(string bot, bool everyone = false)
    {
        var monster = zSearch.GetMonsterUnderPlayerAim();
        if (monster == null) return;
        if (everyone)
        {
            log.LogInfo("all bots attack");
            foreach (var iBot in ZiMain.BotTable.Keys)
            {
                attackMonster(iBot, monster);
            }
        }
        else
        {
            attackMonster(bot, monster);
        }
    }
    [Obsolete]
    public static void ExecuteBotActionOld(PlayerAIBot bot, PlayerBotActionBase.Descriptor descriptor, string message, int func, int slot, int itemtype, int itemserial, int agentid)
    { //TODO refactor all NetworkAPI usage -- DONE
        if (SNet.IsMaster)
        {
            bot.StartAction(descriptor);
            log.LogInfo(message);
        }
        if (!SNet.IsMaster) NetworkAPI.InvokeEvent<ZiMain.ZINetInfo>("ZINetInfo", new ZiMain.ZINetInfo(func, slot, itemtype, itemserial, agentid));
    }

} // plugin

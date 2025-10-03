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
using UnityEngine;
using UnityEngine.SceneManagement;
using ZombieTweak2;
using ZombieTweak2.zMenu;
using ZombieTweak2.zNetworking;
using ZombieTweak2.zRootBotPlayerAction;
using static ZombieTweak2.zNetworking.pStructs;

/*
 == TODO == Priority: Clean up the mess I made creating custom actions.

 -- TODO -- DONE -- Re - create send bot to do manual action
 -- TODO -- DONE -- fix pickup action failing sometimes
 -- TODO -- DONE -- Change cancel to look up
 -- TODO -- DONE -- make look down select yourself
 -- TODO -- DONE -- Customize resource share thresholds, or however that works
 -- TODO -- DONE -- Refactor all NetworkAPI usage
 -- TODO -- DONE -- Make buttons change color when held
 -- TODO -- DONE -- Customizable resource share thresholds
 -- TODO -- DONE -- Block individual resource shares
 -- TODO -- DONE -- Menu breaks when loading checkpoint
 -- TODO -- DONE -- Add support for carrying items like turbines
 -- TODO -- DONE -- When blocking actions (resource pickup/share/etc) chaeck if any existing actions exist, and cancel them instantly.
 -- TODO -- DONE -- Make bots call out with voicelines when you quick select them.
 -- TODO -- DONE -- want to make "clear room" command
 -- TODO -- DONE -- want to make custom blacklist pickups
 -- TODO -- DONE -- want to fix attack not always working
 -- TODO -- DONE -- want to make attack wake room sometimes
 -- TOOD -- DONE -- Not perfect. When sharing resources, if someone else is already giving the visTarget the same item, don't double up.  Can still happen, but much less likely.
 -- TODO -- DONE -- Remake attack my visTarget methods and put it under actions.
 -- TODO -- DONE -- Investigate what is causing recolor of some menu elements with other mods (Archive essentials?)
 -- TODO -- DONE -- Investigate what is causing first letter of bot name to mess up with other mods (Arhcive essentials?)
 -- TODO -- DONE -- Investigate compat with BetterBots.  Seems to break pickup blocking?
 -- TODO -- DONE -- Clear out and remove PlayConfirmSound hook.
 -- TODO -- DONE -- Remake SendBotToKillEnemy metho
 -- TODO -- DONE -- Remove ReceiveZINetInfo completely.
 -- TODO -- DONE -- Remove zlogger enterly.
 -- TODO -- DONE -- Add external list of manual actions.  be sure to clean it when actions are terminated.  add them from SendBotTo- methods.
 -- TODO -- DONE -- When perms removed, remove any current actions that are no longer allowed
 -- TODO -- PARTIALLY DONE -- Add sounds
 -- TODO -- Investigate if using playerslotindex as an ID is problematic when number of bots changes.  Swtich to some other ID if needed.
 -- TODO -- Replace selected bots system with global settings, and then bot spesific overides.
 -- TODO -- Dynamically remove the bot selection/overide menu when there is only 1 bot.
 -- TODO -- Refactor FlexibleEvent to use args=[] instead of a new arg.  this lets me have optional args for more functonality.
 -- TODO -- Make system for lerping between to values over time.  Should be arbitrary vars and maybe even support method args somehow.
 -- TODO -- Make "i need health/ammo" quck action overide share permission
 -- TODO -- Make smart select pick up turrets
 -- TODO -- Fix bot extra data only updating when you look away
 -- TODO -- Detect end of extra info string instead of assuming this is the last thing for better compat with other mods.
 -- TODO -- Move methods arround to other classes that make more sense
 -- TODO -- Handle bots joining/leaving or any other way the bot count can change mid mission.
 -- TODO -- Error when exiting q menu if radial menu is open
 -- TODO -- Unheld selected node event might have problems.
 -- TODO -- Double tap smart select on a bot to have them follow you.
 -- TODO -- Move updateNodeThresholdDisplay and similar to the set methds not as node listeners.
 -- TODO -- Add option to let bots open lockers
 -- TODO -- Add per bot overides for individual share/pickup perms.
 -- TODO -- Add options menu with things like default states and key rebinding
 -- TODO -- Add mele only restriction
 -- TODO -- Add quick settings part of the menu for things like "auto select followed bots" 
 -- TODO -- Add STFU button
 -- TODO -- Add option for menue's to have seprate x/y scale.
 -- TODO -- Use a string builder ZiMain.onActionRemoved
 -- TODO -- Move/refactor  GetAgent and getpStruct methods in ZiMain
 -- TODO -- Nullchecks in SendBotToShareResourcePack
 -- TODO -- Move SetRelativePosition into a listener so it can be disabled.
 -- TODO -- Add option to change menu close angle
 -- TODO -- Make text parts in nodes private and add setters and getters for font stuff.
 -- TODO -- Add menu title and subtitle. Use that for tooltips.
 -- TODO -- Change the way scroll priority works to visualy treat 0 prio as red and disabled
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
 -- TODO -- In smart select, add some leeway for very close objects, maybe inside of 2 units lerp between max angle of given, and 180 at 0 units.
 -- TODO -- make fulltextpart position perfectly match on submenus even when line len of title/subtitle doesn't match.
 -- TODO -- handle bots spamming chat with the same message over and over (usually failed to do thing)
 -- TODO -- Make bots ping items they find even if they don't pick them up.
 -- TODO -- Make chances for bots waking sleepers when attempting to kill them configurable
 -- TODO -- Make clear room command handle special enemies differently.
 -- TODO -- Make clear room command only know about eneimes someone has seen.
 -- TODO -- share with placed turrets
 -- TODO -- BUG -- When holding a node then look away, when you re-open menu node still highlighted.
 -- TODO -- Make "don't pick up this item" context command that lets bots pick up things, but not this spesific item/locker.

Want to make combine resource mod
PreLitVolume.GetFogDensity(Vector3) could be useful


want to completely re-write collection logic, not just priority logic
want to add chat commands for people who don't have the mod.
want to add new bot actions like hold position, look for resource type (in nearby rooms), ping item (go to term, then run ping command)
want to make "go here" command
want to make "home" location function where they "follow" a set location but aren't strickly stuck to it if they get into combat, similar to following a player.
want to make it so bots will open doors to get back to the person they are folling if they have to.

found bot commands in PUI_CommunicationMenu.execute
public enum eGameEvent might be useful
sounds are in namespace AK public class EVENTS
*/


namespace Zombified_Initiative;

[BepInDependency("dev.gtfomodding.gtfo-api")]
[BepInPlugin("com.hirnukuono.zombified_initiative", "Zombified Initiative", "0.9.6")]
[BepInDependency("com.east.bb", BepInDependency.DependencyFlags.SoftDependency)]
public class ZiMain : BasePlugin
{ //this class should contain all methods to call actions, any helpers to faciliate that, and inital setup,
    public static ManualLogSource log;
    internal static bool newRootBotPlayerAction = true;
    public static Dictionary<string, PlayerAIBot> BotTable = new();
    public static PlayerChatManager _chatManager;
    public static PUI_CommunicationMenu _menu;
    public static bool rootmenusetup = false;

    public static System.Random rng = new System.Random();
    public static bool HasBetterBots { get; private set; }

    public static int approachWakeChance = 5;
    public static int wakeChancePerSecond = 20;

    public override void Load()
    {
        HasBetterBots = IL2CPPChainloader.Instance.Plugins.ContainsKey("com.east.bb");
        Harmony m_Harmony = new Harmony("ZombieController");
        m_Harmony.PatchAll();
        ClassInjector.RegisterTypeInIl2Cpp<zUpdater>();
        ClassInjector.RegisterTypeInIl2Cpp<zCameraEvents>();

        NetworkAPI.RegisterEvent<pItemPrioDisable>          ("SetItemPrioDisable",              zNetworking.ReciveSetItemPrioDisable);
        NetworkAPI.RegisterEvent<pItemPrio>                 ("SetItemPrio",                     zNetworking.ReciveSetItemPrio);
        NetworkAPI.RegisterEvent<pResourceThreshold>        ("SetResourceThreshold",            zNetworking.reciveSetResourceThreshold);
        NetworkAPI.RegisterEvent<pResourceThresholdDisable> ("SetResourceThresholdDisable",     zNetworking.ReciveSetResourceThresholdDisable);
        NetworkAPI.RegisterEvent<pSharePermission>          ("SetSharePermission",              zNetworking.ReciveSetSharePermission);
        NetworkAPI.RegisterEvent<pPickupPermission>         ("SetPickupPermission",             zNetworking.ReciveSetPickupPermission);
        NetworkAPI.RegisterEvent<pPickupItemInfo>           ("RequestToPickupItem",             zNetworking.ReciveRequestToPickupItem);
        NetworkAPI.RegisterEvent<pShareResourceInfo>        ("RequestToShareResourcePack",      zNetworking.ReciveRequestToShareResource);
        NetworkAPI.RegisterEvent<pAttackEnemyInfo>          ("RequestToKillEnemy",              zNetworking.ReciveRequestToKillEnemy);

        //EventAPI.OnExpeditionStarted += ZombieController.Initialize;
        log = Log;
        zActionSub.addOnRemoved((Action<PlayerAIBot, PlayerBotActionBase>)onActionRemoved);
        zActionSub.addOnAdded((Action<PlayerAIBot, PlayerBotActionBase>)onActionAdded);
        EventAPI.OnManagersSetup += () =>
        {
            zUpdater.CreateInstance();
            zUpdater.onUpdate.Listen(zMenuManager.Update);
            zUpdater.onUpdate.Listen(zSmartSelect.Update);
            zUpdater.onUpdate.Listen(zActionSub.Update);
            zUpdater.onUpdate.Listen(zSearch.Update);
            zUpdater.onUpdate.Listen(zDebug.debugUpdate);
            zUpdater.onUpdate.Listen(zVisitedManager.Update);
            zUpdater.onLateUpdate.Listen(zMenuManager.LateUpdate);
        };
        LG_Factory.add_OnFactoryBuildDone((Action)zSlideComputer.Init);
        LG_Factory.add_OnFactoryBuildDone((Action)zMenuManager.SetupCamera);
        LG_Factory.add_OnFactoryBuildDone((Action)zMenus.CreateMenus);
        
    }



    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {

    }

    private void onActionAdded(PlayerAIBot bot, PlayerBotActionBase action)
    {

    }



    public static void onActionRemoved(PlayerAIBot bot , PlayerBotActionBase action)
    { //TODO - Use a string builder
        string typeName = action.GetIl2CppType().Name;
        bool manualAction = zActions.isManualAction(action.DescBase);
        if (manualAction)
        {
            PlayerBotActionBase.Descriptor actionToRemove = null;
            foreach (var desc in zActions.manualActions)
            {
                if (desc.Pointer == action.DescBase.Pointer)
                {
                    actionToRemove = desc;
                    break;
                }
            }
            if (actionToRemove != null)
                zActions.manualActions.Remove(actionToRemove);
        }
        if (typeName == "PlayerBotActionCarryExpeditionItem")
        {
            //This actually triggers when they drop the item.
            var descriptor = action.DescBase.Cast<PlayerBotActionCarryExpeditionItem.Descriptor>();
            log.LogInfo($"{bot.Agent.PlayerName} completed collect {descriptor.TargetItem._PublicName_k__BackingField} task with status: {action.DescBase.Status}  access layers {descriptor.m_accessLayers}");
            sendChatMessage($"I put down the {descriptor.TargetItem._PublicName_k__BackingField}.", bot.Agent);
            //What happens when the temp drop it out of combat?  Does that trigger here?
        }
        else if (typeName == "PlayerBotActionCollectItem")
        {
            var descriptor = action.DescBase.Cast<PlayerBotActionCollectItem.Descriptor>();
            var carrycore = descriptor.TargetItem.gameObject.GetComponent<CarryItemPickup_Core>();
            string publicName = descriptor.TargetItem.PublicName;
            string actionName = "collected";
            if (carrycore != null)
            {
                publicName = descriptor.TargetItem._PublicName_k__BackingField;
                actionName = "picked up";
            }
            log.LogInfo($"{bot.Agent.PlayerName} completed collect {publicName} task with status: {action.DescBase.Status}  access layers {descriptor.m_accessLayers}");
            string article = manualAction ? "the" : "a";
            if (action.DescBase.Status == PlayerBotActionBase.Descriptor.StatusType.Successful)
            {
                InventorySlot slot = descriptor.TargetItem.ItemDataBlock.inventorySlot;
                AmmoType ammoType = slot == InventorySlot.Consumable ? AmmoType.CurrentConsumable : slot == InventorySlot.ResourcePack ? AmmoType.ResourcePackRel : AmmoType.None;
                float ammoLeft = bot.Backpack.AmmoStorage.GetAmmoInPack(ammoType);
                string typeUsesPercent = slot == InventorySlot.ResourcePack ? "%" : "";
                string ammocount = "";
                if (ammoLeft > 0)
                    ammocount = " (" + ammoLeft + typeUsesPercent + ")";
                sendChatMessage($"I {actionName} {article} {publicName}{ammocount}.", bot.Agent);
            }
            else if (action.DescBase.Status == PlayerBotActionBase.Descriptor.StatusType.Failed)
                sendChatMessage($"I coul't get {article} {publicName}.", bot.Agent);
            else if (action.DescBase.Status == PlayerBotActionBase.Descriptor.StatusType.Interrupted)
                sendChatMessage($"I can't get {article} {publicName} right now.", bot.Agent);
            else if (action.DescBase.Status == PlayerBotActionBase.Descriptor.StatusType.Aborted)
                sendChatMessage($"I can't get {article} {publicName} right now.", bot.Agent);
            else if (action.DescBase.Status == PlayerBotActionBase.Descriptor.StatusType.Stopped)
                sendChatMessage($"I can't get {article} {publicName} right now.", bot.Agent);
            else
                sendChatMessage($"I can't get {article} {publicName} status {action.DescBase.Status}.", bot.Agent);
        }
        else if (typeName == "PlayerBotActionShareResourcePack")
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
        //else if (typeName == "PlayerBotActionTravel")
        //{
        //    if (manualAction)
        //    {
        //        PlayerBotActionAttack.Descriptor attackAction = null;
        //        foreach (var mAction in manualActions)
        //        {
        //            var type = mAction.GetIl2CppType();
        //            string name = type.DeclaringType != null ? type.DeclaringType.Name : type.Name;

        //            if (name.Contains("PlayerBotActionAttack"))
        //            {
        //                attackAction = mAction.Cast<PlayerBotActionAttack.Descriptor>();
        //                break;
        //            }
        //        }
        //        if (attackAction != null && !attackAction.IsTerminated())
        //        {
        //            if (action.DescBase.Status == PlayerBotActionBase.Descriptor.StatusType.Successful)
        //            {
        //                if (rng.Next(0, 5) == 0)
        //                {
        //                    wakeUpRoom(bot, attackAction.TargetAgent.gameObject);
        //                    action.DescBase.SetCompletionStatus(PlayerBotActionBase.Descriptor.StatusType.Failed);
        //                }
        //            }
        //        }
        //    }
        //}
        else if (typeName == "PlayerBotActionAttack")
        {
            if (manualAction)
            {
                //TODO make this happen on combat instead.
                //bot.Actions[0].Cast<RootPlayerBotAction>().m_followLeaderAction.Prio = 14;
            }
        }
    }
    
    public static void CheckForWakeChance(PlayerAIBot aiBot, GameObject enemyGo, PlayerBotActionBase.Descriptor descriptor)
    {
        if (enemyGo == null)
            return;
        if (rng.Next(0, approachWakeChance) != 0)
            return;
        wakeUpRoom(aiBot, enemyGo);
    }
    public static void wakeUpRoom(PlayerAIBot aiBot, GameObject enemyGo)
    {
        var locomotionTarget = enemyGo.GetComponent<EnemyLocomotion>();
        var enemy = enemyGo.GetComponent<EnemyAgent>();
        //locomotionTarget.ChangeState(ES_StateEnum.HibernateWakeUp); //Somehow this can cause an unkillable enemy??
        enemy.PropagateTargetFull(enemy);
        SendBotToKillEnemy(aiBot, enemy);
    }
    public static void slowUpdate()
    {
        
    }
    public static void sendChatMessage(string message,PlayerAgent sender = null, PlayerAgent receiver = null)
    {
        PlayerChatManager.WantToSentTextMessage(sender != null ? sender : PlayerManager.GetLocalPlayerAgent(), message, receiver);
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
    public static float attackPrio = 5f;
    public static float attackHaste = 0.5f;


    public static PlayerBotActionAttack.Descriptor SendBotToKillEnemy(PlayerAIBot aiBot, EnemyAgent enemy, PlayerAgent commander = null, ulong netsender = 0, PlayerBotActionAttack.StanceEnum stance = PlayerBotActionAttack.StanceEnum.All, PlayerBotActionAttack.AttackMeansEnum means = PlayerBotActionAttack.AttackMeansEnum.Melee, PlayerBotActionWalk.Descriptor.PostureEnum posture = PlayerBotActionWalk.Descriptor.PostureEnum.Crouch)
    {

        if (!SNet.IsMaster) //Are we a client?
        {
            if (netsender != 0) //Is this request coming from a different client?
                return null;
            //request host
            pAttackEnemyInfo info = new pAttackEnemyInfo();
            info.enemy = pStructs.Get_pStructFromRefrence(enemy);
            info.aiBot = pStructs.Get_pStructFromRefrence(aiBot.Agent);
            info.commander = pStructs.Get_pStructFromRefrence(commander); //This might be a problem in commander is null?  Not sure. TODO look into it.
            NetworkAPI.InvokeEvent<pAttackEnemyInfo>("RequestToKillEnemy", info);
            return null;
        }
        var descriptor = new PlayerBotActionAttack.Descriptor(aiBot)
        {
            Stance = stance,
            Means = means,
            Posture = posture,
            TargetAgent = enemy,
            Prio = attackPrio,
            Haste = attackHaste,
        };
        aiBot.Actions[0].Cast<RootPlayerBotAction>().m_followLeaderAction.Prio = attackPrio - 1;
        zActions.manualActions.Add(descriptor);
        sendChatMessage($"Killing the {enemy.EnemyData.name}.", aiBot.Agent, commander);
        //TODO figure out how to make them crouch instead of stand.
        PlayerVoiceManager.WantToSay(aiBot.Agent.CharacterID, AK.EVENTS.PLAY_CL_WILLDO);
        aiBot.StartAction(descriptor);
        return descriptor;
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
    public static void SendBotToCarryItem(PlayerAIBot aiBot, CarryItemPickup_Core item, PlayerAgent commander = null, ulong netsender = 0)
    {
        //todo add to manual action list for refrence later.
        //TODO split this up into it's own netaction instead of piggybacking on sendbottopickupitem.
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
        log.LogInfo($"{commander.PlayerName} is sending {aiBot.Agent.PlayerName} to carry {item._PublicName_k__BackingField} with the new method");
        float prio = 4f;
        PlayerBotActionCarryExpeditionItem.Descriptor desc = new(aiBot)
        {
            TargetItem = item,
            Prio = prio,
        };
        sendChatMessage($"Carrying {item._PublicName_k__BackingField}", aiBot.Agent, commander);
        PlayerVoiceManager.WantToSay(aiBot.Agent.CharacterID, AK.EVENTS.PLAY_CL_WILLDO); //will do
        zActions.manualActions.Add(desc);
        aiBot.StartAction(desc);
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
        //Is this an item we should carry?
        var carrycore = item.gameObject.GetComponent<CarryItemPickup_Core>();
        if (carrycore != null)
        {
            SendBotToCarryItem(aiBot, carrycore, commander, netsender);
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
        PlayerVoiceManager.WantToSay(commander.CharacterID, AK.EVENTS.PLAY_CL_GRABTHEITEM);
        FlexibleMethodDefinition barkback = new FlexibleMethodDefinition(PlayerVoiceManager.WantToSay, [aiBot.Agent.CharacterID, AK.EVENTS.PLAY_CL_WILLDO]);
        zUpdater.InvokeStatic(barkback, 1f);
        sendChatMessage($"Picking up {item.PublicName}",aiBot.Agent,commander);
        zActions.manualActions.Add(desc);
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
        PlayerVoiceManager.WantToSay(aiBot.Agent.CharacterID, AK.EVENTS.PLAY_CL_WILLDO);
        aiBot.StartAction(desc);
    }
    public static void SendBotToClearCurrentRoom(PlayerAIBot aiBot = null, PlayerAgent commander = null, ulong netsender = 0, PlayerBotActionBase.Descriptor arg_descriptor = null)
    {
        
        if (arg_descriptor != null && arg_descriptor.Status != PlayerBotActionBase.Descriptor.StatusType.Successful)
        {
            log.LogInfo($"Unsucsefull last kill {arg_descriptor.Status}");
            return;
        }
        if (commander == null)
            commander = PlayerManager.GetLocalPlayerAgent();
        var allEnemies = commander.CourseNode.m_enemiesInNode;
        if (aiBot == null)
        {
            PlayerAgent closestBot = null;
            float closestBotDistnace = float.MaxValue;
            foreach (var botCandidate in PlayerManager.PlayerAgentsInLevel)
            {
                if (!botCandidate.Owner.IsBot)
                    continue;
                float distanceToBot = (commander.gameObject.transform.position - botCandidate.gameObject.transform.position).sqrMagnitude;
                if (distanceToBot < closestBotDistnace)
                {
                    closestBotDistnace = distanceToBot;
                    closestBot = botCandidate;
                }
            }
            if (closestBot == null)
                return;
            aiBot = closestBot.gameObject.GetComponent<PlayerAIBot>();
        }
        if (allEnemies.Count <= 0)
        {
            sendChatMessage("I have killed all enemies in the room", aiBot.gameObject.GetComponent<PlayerAgent>(), commander);
            return;
        }

        EnemyAgent closestEnemy = null;
        float closestEnemyDistnace = float.MaxValue;
        foreach (var enemy in allEnemies)
        {
            float distanceToEnemy = (aiBot.gameObject.transform.position - enemy.gameObject.transform.position).sqrMagnitude;
            if (distanceToEnemy < closestEnemyDistnace)
            {
                closestEnemyDistnace = distanceToEnemy;
                closestEnemy = enemy;
            }
        }
        var descriptor = SendBotToKillEnemy(aiBot, closestEnemy, commander);
        FlexibleMethodDefinition callback = new(SendBotToClearCurrentRoom,[aiBot,commander, netsender]);
        zActionSub.addOnTerminated(descriptor, callback);
    }
} // plugin

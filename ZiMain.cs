using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BotControl.Networking;
using BotControl.zRootBotPlayerAction;
using CellMenu;
using Enemies;
using GTFO.API;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using LevelGeneration;
using Player;
using SlideMenu;
using SNetwork;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using BotControl.SmartSelect;
using static BotControl.Networking.pStructs;

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
 -- TOOD -- DONE -- Not perfect. When sharing resourcesActions, if someone else is already giving the visTarget the same item, don't double up.  Can still happen, but much less likely.
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
 -- TODO -- NeedsToMove methods arround to other classes that make more sense
 -- TODO -- Handle bots joining/leaving or any other way the bot count can change mid mission.
 -- TODO -- Error when exiting q menu if radial menu is open
 -- TODO -- Unheld selected node event might have problems.
 -- TODO -- Double tap smart select on a bot to have them follow you.
 -- TODO -- NeedsToMove updateNodeThresholdDisplay and similar to the set methds not as node listeners.
 -- TODO -- Add option to let bots open lockers
 -- TODO -- Add per bot overides for individual share/pickup perms.
 -- TODO -- Add options menu with things like default states and key rebinding
 -- TODO -- Add mele only restriction
 -- TODO -- Add quick settings part of the menu for things like "auto select followed bots" 
 -- TODO -- Add STFU button
 -- TODO -- Add option for menue's to have seprate x/y scale.
 -- TODO -- Use a string builder ZiMain.onActionRemoved
 -- TODO -- NeedsToMove/refactor  GetAgent and getpStruct methods in ZiMain
 -- TODO -- Nullchecks in SendBotToShareResourcePack
 -- TODO -- NeedsToMove SetRelativePosition into a listener so it can be disabled.
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
 -- TODO -- Send inventory sync command when bots run out of resourcesActions from a manual action?
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
 -- TODO -- Figure out why I can't add new struct types for network api events.  When I create a new struct, then the last arg of RegisterEvent gets treated as a method group, instead of a unity action.

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


namespace BotControl;

[BepInDependency("dev.gtfomodding.gtfo-api")]
[BepInPlugin("com.SlideDrum.Slides-Bot-Control", "Slides Bot Control", ZiMain.version)]
[BepInDependency("com.east.bb", BepInDependency.DependencyFlags.SoftDependency)]
public class ZiMain : BasePlugin
{ //this class should contain all methods to call actions, any helpers to faciliate that, and inital setup,
    public const string version = "1.0.3";
    public static ManualLogSource log;
    internal static bool newRootBotPlayerAction = true;
    public static Dictionary<string, PlayerAIBot> BotTable = new();
    public static PlayerChatManager _chatManager;
    public static PUI_CommunicationMenu _menu;
    public static bool rootmenusetup = false;

    public static System.Random rng = new System.Random();
    private static bool? _HasBetterBots;
    public static bool HasBetterBots
    {
        get
        {
            if (_HasBetterBots == null)
                _HasBetterBots = IL2CPPChainloader.Instance.Plugins.ContainsKey("com.east.bb");
            return (bool)_HasBetterBots;
        }
    }

    public static int approachWakeChance = 5;
    public static int wakeChancePerSecond = 20;

    internal const bool debugMode = false;
    internal const bool customActions = false;
    internal static bool VoiceMenu = false;
    internal static bool extraActionMenus =false;
    public static Harmony m_Harmony;

    public override void Load()
    {
        
        m_Harmony = new Harmony("BotControl");
        m_Harmony.PatchAll();
        ClassInjector.RegisterTypeInIl2Cpp<zUpdater>();
        ClassInjector.RegisterTypeInIl2Cpp<zCameraEvents>();
         
        //NetworkAPI.RegisterEvent<pItemPrioDisable>          ("SetItemPrioDisable",              zNetworking.ReciveSetItemPrioDisable);
        //NetworkAPI.RegisterEvent<pItemPrio>                 ("SetItemPrio",                     zNetworking.ReciveSetItemPrio);
        //NetworkAPI.RegisterEvent<pResourceThreshold>        ("SetResourceThreshold",            zNetworking.reciveSetResourceThreshold);
        //NetworkAPI.RegisterEvent<pResourceThresholdDisable> ("SetResourceThresholdDisable",     zNetworking.ReciveSetResourceThresholdDisable);
        //NetworkAPI.RegisterEvent<pGenericPermission>        ("SetActionPermission",             zNetworking.ReciveSetActionPermission);
        NetworkAPI.RegisterEvent<pPickupItemInfo>           ("RequestToPickupItem",               zNetworking.ReciveRequestToPickupItem);
        NetworkAPI.RegisterEvent<pPickupSentryInfo>         ("RequestToPickupSentry",             zNetworking.ReciveRequestToPickupSentry);
        NetworkAPI.RegisterEvent<pPlaceSentryInfo>          ("RequestToPlaceSentry",              zNetworking.ReciveRequestToPlaceSentry);
        NetworkAPI.RegisterEvent<pShareResourceInfo>        ("RequestToShareResourcePack",        zNetworking.ReciveRequestToShareResource);
        NetworkAPI.RegisterEvent<pAttackEnemyInfo>          ("RequestToKillEnemy",                zNetworking.ReciveRequestToKillEnemy);
        NetworkAPI.RegisterEvent<pThrowDataInfo>            ("RequestToThrowItem",                zNetworking.ReciveRequestToThrowItem);
        NetworkAPI.RegisterEvent<pBoolOverideTreeInfo>      ("SetBoolOverideTree",                zNetworking.ReciveSetBoolOverideTree);
        NetworkAPI.RegisterEvent<pIntOverideTreeInfo>       ("SetIntOverideTree",                 zNetworking.ReciveSetIntOverideTree);
        NetworkAPI.RegisterEvent<pFloatOverideTreeInfo>     ("SetFloatOverideTree",               zNetworking.ReciveSetFloatOverideTree);

        //EventAPI.OnExpeditionStarted += ZombieController.Initialize;
        log = Log;
        zActionSub.addOnRemoved((Action<PlayerAIBot, PlayerBotActionBase>)onActionRemoved);
        zActionSub.addOnAdded((Action<PlayerAIBot, PlayerBotActionBase>)onActionAdded);
        EventAPI.OnManagersSetup += () =>
        {
            zUpdater.CreateInstance();
            zUpdater.onUpdate.Listen(sMenuManager.Update);
            zUpdater.onUpdate.Listen(zActionSub.Update);
            //zUpdater.onUpdate.Listen(zSearchOld.Update);
            zUpdater.onUpdate.Listen(zDebug.debugUpdate);
            //zUpdater.onUpdate.Listen(zVisitedManager.Update);
            zUpdater.onUpdate.Listen(zSmartSelect.Update);
            zUpdater.onLateUpdate.Listen(sMenuManager.LateUpdate);
            OnLateLoad();
        };
        LG_Factory.add_OnFactoryBuildDone((Action)zSlideComputer.Init);
        //LG_Factory.add_OnFactoryBuildDone((Action)sMenuManager.SetupCamera);
        //LG_Factory.add_OnFactoryBuildDone((Action)zMenus.CreateMenus);
    }
    public static void OnLateLoad()
    {
        if (HasBetterBots)
        {
            BBCompat.OnInit();
        }
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
            if ((bool)zSlideComputer.ActionPermissions.ValueAt("Notify pickup"))
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
                if ((bool)zSlideComputer.ActionPermissions.ValueAt("Notify pickup"))
                    sendChatMessage($"I {actionName} {article} {publicName}{ammocount}.", bot.Agent);
            }
            else if ((bool)zSlideComputer.ActionPermissions.ValueAt("Notify pickup fail"))
            {
                if (action.DescBase.Status == PlayerBotActionBase.Descriptor.StatusType.Failed)
                    sendChatMessage($"I couldn't get {article} {publicName}.", bot.Agent);
                else if (action.DescBase.Status == PlayerBotActionBase.Descriptor.StatusType.Interrupted)
                    sendChatMessage($"I can't get {article} {publicName} right now.", bot.Agent);
                else if (action.DescBase.Status == PlayerBotActionBase.Descriptor.StatusType.Aborted)
                    sendChatMessage($"I can't get {article} {publicName} right now.", bot.Agent);
                else if (action.DescBase.Status == PlayerBotActionBase.Descriptor.StatusType.Stopped)
                    sendChatMessage($"I can't get {article} {publicName} right now.", bot.Agent);
                else if (!(action.DescBase.Status == PlayerBotActionBase.Descriptor.StatusType.Active))
                    sendChatMessage($"I can't get {article} {publicName} status {action.DescBase.Status}.", bot.Agent);
            }

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
                if ((bool)zSlideComputer.ActionPermissions.ValueAt("Notify resource share"))
                    sendChatMessage($"I gave {receverOrMyslef} {article} {descriptor.Item.PublicName} ({ammoLeft}%).", bot.Agent);
                else { }
            else if ((bool)zSlideComputer.ActionPermissions.ValueAt("Notify share fail"))
            {
                if (action.DescBase.Status == PlayerBotActionBase.Descriptor.StatusType.Failed)
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
    public static void BotBarkBack(int botId, uint voiceID, string subtitle, float delay = 1f)
    {
        FlexibleMethodDefinition barkback = new FlexibleMethodDefinition(BotBark, [botId, voiceID, subtitle]);
        zUpdater.InvokeStatic(barkback, delay);

    }
    internal static void BotBark(int botId, uint voiceID, string subtitle = "")
    {
        PlayerVoiceManager.WantToSay(botId, voiceID);
        if (subtitle != "")
            zStaticRefrences.Subtitles.ShowSingleLineSubtitle(subtitle, 1);
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
        zBotActions.SendBotToKillEnemy(aiBot, enemy);
    }
    public static void slowUpdate()
    {
        
    }
    private static (string, int, int) previousMessage;
    public static void sendChatMessage(string message,PlayerAgent sender = null, PlayerAgent receiver = null)
    {
        var thisMessage = (message, sender.PlayerSlotIndex, sender.PlayerSlotIndex);
        bool same = thisMessage == previousMessage;
        previousMessage = thisMessage;
        if (same)
            return;
        if ((bool)zSlideComputer.ActionPermissions.ValueAt("TalkInChat"))
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



    
    public static void PlayUiSound(uint e)
    {
        CM_PageBase.PostSound(e, "PlayUiSound");
    }
    
} // plugin

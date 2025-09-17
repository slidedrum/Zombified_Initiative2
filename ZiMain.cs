using Agents;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Enemies;
using Gear;
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

//todo fix pickup action failing sometimes  -- Done?  
//todo change cancel to look up -- done
//todo make look down select yourself -- done
//todo make "i need health/ammo" quck action overide share permission
//todo make smart select pick up turrets
//todo fix bot extra data only updating when you look away
//todo track down KeyNotFoundException: The given key was not present in the dictionary.
//todo move methods arround to other classes that make more sense

//todo refactor PlayConfirmSound hook to not be so dumb -- probably just going to remvoe support for q menu

//want to make custom blacklist pickups - IN PROGRESS
//want to fix attack not always working
//want to make attack wake room sometimes
//want to make "clear room" command

//want to make "go here" command
//want to make "home" location function where they "follow" a set location but aren't strickly stuck to it if they get into combat, similar to following a player.

//found bot commands in PUI_CommunicationMenu.execute
//RootPlayerBotAction is where actions are chosen
//if (item2.pItemData.itemID_gearCRC != 116U && item2.pItemData.itemID_gearCRC != 30U) is what says don't pick up.  hard coded.
//ItemDataBlock.s_blockIDByName has all ids
//RootPlayerBotAction.s_itemBasePrios has what bots can pick up


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
    public static List<botAction> botActions = new();
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
        ClassInjector.RegisterTypeInIl2Cpp<zComputer>();
        ClassInjector.RegisterTypeInIl2Cpp<zUpdater>();
        var ZombieController = AddComponent<zController>();

        NetworkAPI.RegisterEvent<ZINetInfo>(ZINetInfo.NetworkIdentity, zController.ReceiveZINetInfo);
        NetworkAPI.RegisterEvent<ZISendBotToPickupItemInfo>("sendBotToPickupItem", SendBotToPickupItem);

        LG_Factory.add_OnFactoryBuildDone((Action)ZombieController.OnFactoryBuildDone);
        LG_Factory.add_OnFactoryBuildDone((Action)zSlideComputer.Init);
        EventAPI.OnExpeditionStarted += ZombieController.Initialize;
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
        LG_Factory.add_OnFactoryBuildDone((Action)zMenuManager.OnFactoryBuildDone);
        LG_Factory.add_OnFactoryBuildDone((Action)zMenus.CreateMenus);
        
    }



    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {

    }

    private void onActionAdded(PlayerAIBot bot, PlayerBotActionBase action)
    {

    }
    public static bool isManualAction(PlayerBotActionBase action)
    {//TODO - This honestly might be fine.  But I'd love a more generic way that doesn't rely on typename.
        string typeName = action.GetIl2CppType().Name;
        float haste = 0f;
        float prio = 0f;
        if (typeName == "PlayerBotActionCollectItem")
        {
            var descriptor = action.DescBase.Cast<PlayerBotActionCollectItem.Descriptor>();
            haste = descriptor.Haste;
            prio = descriptor.Prio;
        }
        if (typeName == "PlayerBotActionShareResourcePack")
        {
            var descriptor = action.DescBase.Cast<PlayerBotActionShareResourcePack.Descriptor>();
            haste = descriptor.Haste;
            prio = descriptor.Prio;
        }
        if (typeName == "PlayerBotActionAttack")
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
            var descriptor = action.DescBase.Cast<PlayerBotActionShareResourcePack.Descriptor>();
            float ammoLeft = bot.Backpack.AmmoStorage.GetAmmoInPack(AmmoType.ResourcePackRel);
            log.LogInfo($"{bot.Agent.PlayerName} completed share {descriptor.Item.PublicName} task with status: {action.DescBase.Status}  access layers {descriptor.m_accessLayers}");
            string article = manualAction ? "the" : "a";
            string receverOrMyslef = descriptor.Receiver == bot.Agent ? "myself" : descriptor.Receiver.PlayerName;
            if (action.DescBase.Status == PlayerBotActionBase.Descriptor.StatusType.Successful)
                sendChatMessage($"I gave {receverOrMyslef} {article} {descriptor.Item.PublicName}({ammoLeft}%).", bot.Agent);
            else if (action.DescBase.Status == PlayerBotActionBase.Descriptor.StatusType.Failed)
                sendChatMessage($"I coul't give {receverOrMyslef} {article} {descriptor.Item.PublicName}({ammoLeft}%).", bot.Agent);
            else if (action.DescBase.Status == PlayerBotActionBase.Descriptor.StatusType.Interrupted)
                sendChatMessage($"I can't give {receverOrMyslef} {article} {descriptor.Item.PublicName}({ammoLeft}%) right now.", bot.Agent);
            else if (action.DescBase.Status == PlayerBotActionBase.Descriptor.StatusType.Aborted)
                sendChatMessage($"I can't give {receverOrMyslef} {article} {descriptor.Item.PublicName}({ammoLeft}%) right now.", bot.Agent);
            else if (action.DescBase.Status == PlayerBotActionBase.Descriptor.StatusType.Stopped)
                sendChatMessage($"I can't give {receverOrMyslef} {article} {descriptor.Item.PublicName}({ammoLeft}%) right now.", bot.Agent);
            else
                sendChatMessage($"I can't give {receverOrMyslef} {article} {descriptor.Item.PublicName}({ammoLeft}%) status {action.DescBase.Status}.", bot.Agent);
        }
    }
    public static void slowUpdate()
    {
        
    }
    public static void sendChatMessage(string message,PlayerAgent sender = null, PlayerAgent reciver = null)
    {
        PlayerChatManager.WantToSentTextMessage(sender != null ? sender : PlayerManager.GetLocalPlayerAgent(), message, reciver);
    }
    [Obsolete]

    public static void attackMonster(string bot, EnemyAgent monster)
    {
        log.LogInfo($"bot " + bot + " attack");
        SendBotToKillEnemyOld(bot, monster, PlayerBotActionAttack.StanceEnum.All, PlayerBotActionAttack.AttackMeansEnum.All, PlayerBotActionWalk.Descriptor.PostureEnum.Stand);
    }
    public static void setPickupPermission(string bot, bool allowed)
    { //TODO - figure out network stuff and refactor this. Right now I'm a little scared to touch this because I can't test it.
        foreach (KeyValuePair<String, PlayerAIBot> iBotTable in ZiMain.BotTable)
        {
            string botName = iBotTable.Key;
            PlayerAIBot playerAIBot = iBotTable.Value;
            if (bot == botName)
            {
                log.LogInfo($"{botName} pickup perm set to {allowed}");
                if (!SNet.IsMaster) NetworkAPI.InvokeEvent<ZiMain.ZINetInfo>("ZINetInfo", new ZiMain.ZINetInfo(2, playerAIBot.m_playerAgent.PlayerSlotIndex, 0, 0, 0));
                if (SNet.IsMaster)
                {
                    zComputer botComp = playerAIBot.GetComponent<zComputer>();
                    if (botComp.pickupaction != null) botComp.pickupaction.DescBase.SetCompletionStatus(PlayerBotActionBase.Descriptor.StatusType.Failed);
                    botComp.allowedpickups = allowed;
                }
            }
        }
    }
    public static void togglePickupPermission(string bot, bool everyone = false)
    { //TODO - rework so it doesn't need a for loop
      //TODO - remove everyone arg
        foreach (KeyValuePair<String, PlayerAIBot> iBotTable in ZiMain.BotTable)
        {
            string botName = iBotTable.Key;
            PlayerAIBot playerAIBot = iBotTable.Value;
            if (everyone || bot == botName)
            {
                log.LogInfo($"{botName} toggle resource pickups");
                if (!SNet.IsMaster) NetworkAPI.InvokeEvent<ZiMain.ZINetInfo>("ZINetInfo", new ZiMain.ZINetInfo(2, playerAIBot.m_playerAgent.PlayerSlotIndex, 0, 0, 0));
                if (SNet.IsMaster)
                {
                    zComputer botComp = playerAIBot.GetComponent<zComputer>();
                    if (botComp.pickupaction != null) botComp.pickupaction.DescBase.SetCompletionStatus(PlayerBotActionBase.Descriptor.StatusType.Failed);
                    botComp.allowedpickups = !botComp.allowedpickups;
                }
            }
        }
    }
    public static List<PlayerAIBot> GetBotList()
    {//TODO this is bad there's got to be a better way.  Though it's pretty cheap regardless.
        List<PlayerAIBot> playerAiBots = new();
        var playerAgentsInLevel = PlayerManager.PlayerAgentsInLevel;
        foreach (var agent in playerAgentsInLevel)
        {
            var aiBot = agent.gameObject.GetComponent<PlayerAIBot>();
            if (aiBot != null)
            {
                playerAiBots.Add(aiBot);
            }
        }
        return playerAiBots;
    }

    public static void SendBotTokillEnemy(PlayerAIBot bot, Agent enemy, PlayerBotActionAttack.StanceEnum stance = PlayerBotActionAttack.StanceEnum.All, PlayerBotActionAttack.AttackMeansEnum means = PlayerBotActionAttack.AttackMeansEnum.All, PlayerBotActionWalk.Descriptor.PostureEnum posture = PlayerBotActionWalk.Descriptor.PostureEnum.Stand, PlayerAgent commander = null)
    {

    }
    public static Item TryGetItemInLevelFromItemData(pItemData itemData)
    {
        Item item;
        PlayerBackpackManager.TryGetItemInLevelFromItemData(itemData, out item);
        return item;
    }
    public static PlayerAgent GetAgentFrom_pStruct(SNetStructs.pPlayer player_struct)
    {
        if (!player_struct.TryGetPlayer(out SNet_Player player))
            return null;
        return player.PlayerAgent.TryCast<PlayerAgent>();
    }

    public static SNetStructs.pPlayer Get_pStructFromAgent(PlayerAgent agent)
    {
        SNetStructs.pPlayer player = new();
        player.SetPlayer(agent.Owner);
        return player;
    }
    public static PlayerAgent GetAgentFrom_pPlayer(SNetStructs.pPlayer player_struct)
    {
        if (!player_struct.TryGetPlayer(out SNet_Player player))
            return null;
        return player.PlayerAgent.TryCast<PlayerAgent>();
    }

    public static SNetStructs.pPlayer Get_pPlayerFromAgent(PlayerAgent agent)
    {
        SNetStructs.pPlayer player = new();
        player.SetPlayer(agent.Owner);
        return player;
    }
    public struct ZISendBotToPickupItemInfo
    {
        public int botId;
        public pItemData item;
    }
    private static void SendBotToPickupItem(ulong sender, ZISendBotToPickupItemInfo info)
    {
        log.LogInfo($"Networked message! botID: {info.botId} - pItemData: {info.item}");
        Item _item;
        PlayerBackpackManager.TryGetItemInLevelFromItemData(info.item, out _item);
        //log.LogInfo($"Got _item {(_item == null ? "null" : _item.Pointer)}");
        //ItemInLevel item = _item.gameObject.GetComponent<ItemInLevel>();
        //log.LogInfo($"Got item {item} - {item.PublicName}");
        //PlayerAgent bot = GetAgentFromId(info.botId).gameObject.GetComponent<PlayerAgent>();
        //log.LogInfo($"got bot {bot} - {bot.PlayerName}");
    }
    public static void SendBotToPickupItem(PlayerAIBot bot, ItemInLevel item, PlayerAgent commander = null)
    {

    }
    public static void SendBotToShareResourcePack(PlayerAIBot sender, PlayerAgent reciver, PlayerAgent commander = null)
    {

    }

    
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
    public static void SendBotToPickupItemOld(string chosenBot, ItemInLevel item)
    { //TODO - refactor all NetworkAPI usage
      //TODO - saving item types as ints?  there's got to be a better way.
        int itemtype = 0;
        int itemserial = 0;
        PlayerAIBot bot = ZiMain.BotTable[chosenBot];
        if (bot == null)
            return;





        //int botId = GetIdFromAgent(bot.Agent);
        //pItemData itemData = item.pItemData;
        //ZISendBotToPickupItemInfo info = new ZISendBotToPickupItemInfo
        //{
        //    botId = botId,
        //    item = itemData
        //};
        //SendBotToPickupItem(0, info);




        var res = item.TryCast<ResourcePackPickup>();
        if (res != null && res.m_packType == eResourceContainerSpawnType.AmmoWeapon) itemtype = 1;
        if (res != null && res.m_packType == eResourceContainerSpawnType.AmmoTool) itemtype = 2;
        if (res != null && res.m_packType == eResourceContainerSpawnType.Health) itemtype = 3;
        if (res != null && res.m_packType == eResourceContainerSpawnType.Disinfection) itemtype = 4;
        if (res != null) itemserial = res.m_serialNumber;
        var descriptor = new PlayerBotActionCollectItem.Descriptor(bot)
        {
            TargetItem = item,
            TargetContainer = item.container,
            TargetPosition = item.transform.position,
            Prio = _manualActionsPriority,
            Haste = _manualActionsHaste,
        };
        var task = new botAction(bot, item, item.container, item.transform.position, _manualActionsPriority, _manualActionsHaste, "Added collect item action to " + bot.Agent.PlayerName, 4, bot.m_playerAgent.PlayerSlotIndex, itemtype, itemserial, 0, 0, descriptor);
        botActions.Add(task);
        log.LogInfo("added to botactions list, total " + botActions.Count);
        ExecuteBotActionOld(bot, descriptor,
            "Added collect item action to " + bot.Agent.PlayerName, 3, bot.m_playerAgent.PlayerSlotIndex, itemtype, itemserial, 0);
    }
    public static void SendBotToShareResourcePackOld(String sender, PlayerAgent reciver, PlayerAgent commander = null)
    { //TODO - might change chosen bot to be a PlayerAiBot instead of a string.
      // - otherwise looks fine?
        var bot = ZiMain.BotTable[sender];
        PlayerAgent agent = bot.m_playerAgent;
        if (bot == null)
            return;
        if (!bot.Backpack.TryGetBackpackItem(InventorySlot.ResourcePack, out BackpackItem pack))
        {
            return;
        }
        ZiMain.sendChatMessage($"Sharing {pack.Name} with: " + reciver.PlayerName, agent, commander != null ? commander : PlayerManager.GetLocalPlayerAgent());
        BackpackItem backpackItem = null;
        var gotBackpackItem = bot.Backpack.HasBackpackItem(InventorySlot.ResourcePack) &&
                              bot.Backpack.TryGetBackpackItem(InventorySlot.ResourcePack, out backpackItem);
        if (!gotBackpackItem)
            return;

        var resourcePack = backpackItem.Instance.Cast<ItemEquippable>();
        bot.Inventory.DoEquipItem(resourcePack);

        ZiMain.ExecuteBotActionOld(bot, new PlayerBotActionShareResourcePack.Descriptor(bot)
        {
            Receiver = reciver,
            Item = resourcePack,
            Prio = _manualActionsPriority,
            Haste = _manualActionsHaste,
        },
            "Added share resource action to " + bot.Agent.PlayerName, 4, bot.m_playerAgent.PlayerSlotIndex, 0, 0, reciver.m_replicator.Key + 1);
    }
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
    //[Obsolete]
    public static void ExecuteBotActionOld(PlayerAIBot bot, PlayerBotActionBase.Descriptor descriptor, string message, int func, int slot, int itemtype, int itemserial, int agentid)
    { //TODO refactor all NetworkAPI usage
        if (SNet.IsMaster)
        {
            bot.StartAction(descriptor);
            log.LogInfo(message);
        }
        if (!SNet.IsMaster) NetworkAPI.InvokeEvent<ZiMain.ZINetInfo>("ZINetInfo", new ZiMain.ZINetInfo(func, slot, itemtype, itemserial, agentid));
    }

} // plugin

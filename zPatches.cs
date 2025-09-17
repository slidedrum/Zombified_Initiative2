using Agents;
using GameData;
using GTFO.API;
using HarmonyLib;
using Player;
using SNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ZombieTweak2;
using static Zombified_Initiative.ZiMain;

namespace Zombified_Initiative;

[HarmonyPatch]
public class ZombifiedPatches
{
    //This file contains all harmony patches.
    //Might split this up later if there gets to be too many of them.

    public static float newPrio = 0f;
    [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.GetItemPrio))]
    [HarmonyPostfix]
    public static void GetItemPrio(PlayerAIBot __instance, InventorySlot itemSlot, uint itemID, ref float __result)
    {
        //This is supposed to be a full prefix replacement where the original method never runs.
        //But for some reason the output is not re-created perfectly.  That would't be a big deal honestly.
        //Except for the fact that it causes bots to constantly pick up/put down items in a loop.
        //So instead I only run my custom logic as a fallback.
        //This approach still allows me to support arbitrary item pickups not normally in the list, without breaking the logic.
        //Theoretically if there are a bunch of new items in the list, they could get into a "hot potato" loop.  but I'm calling that a "known shippable" for now.
        if (!zSlideComputer.GetPickupPermission(__instance))
        {
            //Perms dissabled
            __result = 0f;
            return;
        }
        if (__result != 0) //did we already get a result?
            return;
        if (!RootPlayerBotAction.s_itemBasePrios.ContainsKey(itemID)) //do we have a new result?
            return;
        __result = 0f;

        ItemDataBlock itemDataBlock;
        if (ItemDataBlock.s_blockByID.ContainsKey(itemID))
        {
            itemDataBlock = ItemDataBlock.s_blockByID[itemID];
        }
        else
        {
            ZiMain.log.LogError($"Tried to get priority for unknow id: {itemID}");
            return;
        }
        if (!RootPlayerBotAction.s_itemBasePrios.ContainsKey(itemID))
        {
            ZiMain.log.LogWarning($"Tried to get priority for unmapped item: ({itemID}){itemDataBlock.name} in {itemSlot}");
            return;
        }
        ZiMain.log.LogInfo($"Getting item pritiy ({itemID}){itemDataBlock.name} in {itemSlot}");

        float basePriority = RootPlayerBotAction.s_itemBasePrios[itemID];

        List<PlayerAgent> playerAgentsList = PlayerManager.PlayerAgentsInLevel.ToArray().ToList();

        float highestAmmoCap = 0f;
        float currentTotalAmmoOfOtherBotsNotThisBot = 0f;
        float priorityFloor = 0.15f;
        int foundMe = 0;
        int otherAgents = 0;
        foreach (PlayerAgent agent in playerAgentsList)
        {
            if (agent.CharacterID == __instance.Agent.CharacterID)
            {
                foundMe++;
                continue;
            }
            otherAgents++;
            PlayerBackpack agentBackpack = PlayerBackpackManager.GetBackpack(agent.Owner);
            BackpackItem agentItem = null;
            if (agentBackpack != null)
                if (!agentBackpack.TryGetBackpackItem(itemSlot, itemID, out agentItem))
                    continue;
            if (agentItem == null)
                continue;
            float ammoCap = 1;
            float agentAmmo = 0;
            InventorySlotAmmo pack = null;
            switch (itemSlot)
            {
                case InventorySlot.ResourcePack:
                    pack = agentBackpack?.AmmoStorage?.ResourcePackAmmo;
                    priorityFloor = 0.5f;
                    break;
                case InventorySlot.Consumable:
                    pack = agentBackpack?.AmmoStorage?.ConsumableAmmo;
                    priorityFloor = 0.25f;
                    break;
                default:

                    break;
            }
            if (pack != null)
            {
                ammoCap = pack.AmmoMaxCap;
                agentAmmo = pack.AmmoInPack;
            }
            else
            {
                ammoCap = 1;
                agentAmmo = 1;
            }
            highestAmmoCap = Math.Max(ammoCap, highestAmmoCap);
            currentTotalAmmoOfOtherBotsNotThisBot += agentAmmo;
        }
        if (foundMe == 0)
        {
            var alive = __instance.Agent is UnityEngine.Object uObj && uObj;
            //ZiMain.log.LogError($"Could not find this agent! canceling and returning 0?");
            //ZiMain.log.LogError($"\nCharID ({__instance.Agent?.CharacterID ?? -1337})");
            //ZiMain.log.LogError($"\nGlobalID ({(alive ? __instance.Agent.GlobalID : -1337)})");
            //ZiMain.log.LogError($"\nGameObjectID ({(alive ? __instance.Agent.GameObjectID : -1337)})");
            //ZiMain.log.LogError($"\nGetInstanceID ({(alive ? __instance.Agent.GetInstanceID() : -1337)})");
            //ZiMain.log.LogError($"\nGetPriorityID ({(alive ? __instance.Agent.GetPriorityID() : -1337)})");
            //ZiMain.log.LogError($"\n_CharacterID_k__BackingField ({__instance.Agent?._CharacterID_k__BackingField ?? -1337})");
            //ZiMain.log.LogError($"\n_GameObjectID_k__BackingField ({__instance.Agent?._GameObjectID_k__BackingField ?? -1337})");
            zDebug.nofindagent = __instance.Agent;
        }
        //ZiMain.log.LogMessage($"Foundme: {foundMe}");
        //ZiMain.log.LogMessage($"otherAgents count: {otherAgents}");
        //ZiMain.log.LogMessage($"highestAmmoCap: {highestAmmoCap}");
        //ZiMain.log.LogMessage($"currentTotalAmmo: {currentTotalAmmoOfOtherBotsNotThisBot}");
        //ZiMain.log.LogMessage($"basePriority: {basePriority}");
        //ZiMain.log.LogMessage($"priorityFloor: {priorityFloor}");

        if (highestAmmoCap > 0.0f && otherAgents > 0)
        {

            float maxOtherTotal = playerAgentsList.Count * highestAmmoCap;
            float fillFactor = currentTotalAmmoOfOtherBotsNotThisBot / maxOtherTotal;
            float minPriority = basePriority * priorityFloor;
            //ZiMain.log.LogMessage($"maxOtherTotal: {maxOtherTotal}");
            //ZiMain.log.LogMessage($"fillFactor: {fillFactor}");
            //ZiMain.log.LogMessage($"minPriority: {minPriority}");
            __result = Mathf.Lerp(basePriority, minPriority, fillFactor);
        }
        else
        {
            __result = basePriority;
        }
        //ZiMain.log.LogMessage($"Priority: {__result}");
        newPrio = __result;
        return;
    }
    [HarmonyPatch(typeof(PlayerAIBot), nameof(PlayerAIBot.SetEnabled))]
    [HarmonyPostfix]

    public static void AddComp(PlayerAIBot __instance, bool state)
    {
        if (!state) return;
        if (!__instance.gameObject.GetComponent<zComputer>())
        {
            log.LogInfo($"adding zombified component to {__instance.Agent.PlayerName} ..");
            var gaa = __instance.Agent.gameObject.AddComponent<zComputer>();
            gaa.Initialize();
            return;
        }
    }

    [HarmonyPatch(typeof(PlayerAgent), nameof(PlayerAgent.OnDestroy))]
    [HarmonyPrefix]

    public static void DestroyMenu(PlayerAgent __instance)
    {
        var tempcomp = __instance.gameObject.GetComponent<zComputer>();
        if (tempcomp != null)
        {
            log.LogInfo($"zombiebot leaving, buh byeeee");
            tempcomp.started = false;
        }
    }
    [HarmonyPatch(typeof(PlaceNavMarkerOnGO), nameof(PlaceNavMarkerOnGO.UpdateExtraInfo))]
    [HarmonyPostfix]
    public static void UpdateExtraInfoPatch(PlaceNavMarkerOnGO __instance)
    {
        string original = __instance.m_extraInfo;
        var agent = __instance.m_player;
        zComputer zombie = agent.gameObject.GetComponent<zComputer>();
        if (original.Contains("Pickup:"))
        {
            bool dirty = false;
            List<string> lines = new List<string>(original.Split('\n'));
            foreach (string line in lines)
            {
                if (line.Contains("Pickup:") && (line.Contains("True") != zombie.allowedpickups))
                {
                    dirty = true;
                    break;
                }
                if (line.Contains("Share:") && (line.Contains("True") != zombie.allowedshare))
                {
                    dirty = true;
                    break;
                }
                if (line.Contains("Sentry:") && (line.Contains("True") == zombie.allowedpickups))
                {
                    dirty = true;
                    break;
                }
            }
            if (!dirty) return;
            //remove our custom info so it can be re added
            original = original.Substring(0, original.IndexOf("Pickup:")).TrimEnd();
        }
        if (zombie != null) { 
            string pickups = $"Pickup: <color=#FFA50066>{zombie.allowedpickups}</color>";
            string share = $"Share: <color=#FFA50066>{zombie.allowedshare}</color>";
            string move = $"Sentry: <color=#FFA50066>{!zombie.allowedmove}</color>";
            __instance.m_extraInfo = original + "<color=#CCCCCC66><size=70%>\n" + pickups + "\n" + share + "\n" + move + "</size></color>";
        }
    }
    [HarmonyPatch(typeof(PlayerBotActionBase), nameof(PlayerBotActionBase.CheckCollision))]
    [HarmonyPostfix]
    public static void CheckCollisionPatch(PlayerBotActionBase __instance, PlayerBotActionBase.Descriptor desc, bool __result)
    {
        if (__result)
        {
            string botName;

            if (desc == null)
            {
                botName = "desc is NULL";
            }
            else if (desc.Bot == null)
            {
                botName = "desc.Bot is NULL";
            }
            else if (desc.Bot.Agent == null)
            {
                botName = "desc.Bot.Agent is NULL";
            }
            else if (desc.Bot.Agent.PlayerName == null)
            {
                botName = "desc.Bot.Agent.PlayerName is NULL";
            }
            else
            {
                botName = desc.Bot.Agent.PlayerName;
            }

            string instanceType = __instance != null ? __instance.GetIl2CppType().Name : "INSTANCE IS NULL";
            string actionType = (desc != null && desc.ActionBase != null) ? desc.ActionBase.GetIl2CppType().Name : "desc.ActionBase is NULL";
            if (instanceType == "INSTANCE IS NULL" || instanceType == "PlayerBotActionCollectItem")
                ZiMain.log.LogWarning($"Action collision! botName={botName}, instanceType={instanceType}, actionType={actionType}");

        }
    }

    [HarmonyPatch(typeof(PlayerBotActionCollectItem), nameof(PlayerBotActionCollectItem.OnTravelActionEvent))]
    [HarmonyPrefix]
    public static bool PlayerBotActionCollectItemPatch(PlayerBotActionCollectItem __instance, PlayerBotActionBase.Descriptor descBase)
    {
        if (descBase != __instance.m_travelAction)
        {
            __instance.PrintError("Rogue action.");
        }
        if (descBase.IsTerminated())
        {
            __instance.m_travelAction = null;
            if (descBase.Status == PlayerBotActionBase.Descriptor.StatusType.Successful)
            {
                __instance.StartTransfer();
                return false;
            }
            if (__instance.VerifyCurrentPosition())
            {
                log.LogWarning("VerifyCurrentPosition - travel event failed, but within range anyway!");
                __instance.StartTransfer();
                return false;
            }
            log.LogWarning("VerifyCurrentPosition - travel event failed");
            __instance.m_desc.SetCompletionStatus(PlayerBotActionBase.Descriptor.StatusType.Failed);
        }
        return false;
    }
    [HarmonyPatch(typeof(CommunicationMenu), nameof(CommunicationMenu.PlayConfirmSound))]
    [HarmonyPrefix]

    public static void PlayConfirmSound(CommunicationMenu __instance)
    {
        zComputer whocomp = null;
        CommunicationNode node = ZiMain._menu.m_menu.CurrentNode;
        if (node.IsLastNode)
        {
            String jee = TextDataBlock.GetBlock(node.TextId).English;
            log.LogDebug($"teksti on " + jee);
            String who = jee.Split(new char[] { ' ' })[0].Trim();
            String wha = jee.Substring(who.Length).Trim();
            log.LogDebug($"teksti on " + jee + ", who on " + who + " ja wha on " + wha);
            if (wha == "attack my target")
            {
                bool everyone = who == "AllBots";
                ZiMain.attackMyTarget(who, everyone);
            }
            if (wha.Contains("pickup permission"))
            {
                foreach (KeyValuePair<String, PlayerAIBot> bt in ZiMain.BotTable)
                {
                    if (who == "AllBots" || who == bt.Key)
                    {
                        log.LogInfo($"{bt.Key} toggle resource pickups");
                        if (!SNet.IsMaster) NetworkAPI.InvokeEvent<ZiMain.ZINetInfo>("ZINetInfo", new ZiMain.ZINetInfo(2, bt.Value.m_playerAgent.PlayerSlotIndex, 0, 0, 0));
                        if (SNet.IsMaster)
                        {
                            whocomp = bt.Value.GetComponent<zComputer>();
                            if (whocomp.pickupaction != null) whocomp.pickupaction.DescBase.SetCompletionStatus(PlayerBotActionBase.Descriptor.StatusType.Failed);
                            whocomp.allowedpickups = !whocomp.allowedpickups;
                        }
                    }
                }
            }

            if (wha.Contains("share permission"))
            {
                foreach (KeyValuePair<String, PlayerAIBot> bt in ZiMain.BotTable)
                {
                    if (who == "AllBots" || who == bt.Key)
                    {
                        log.LogInfo($"{bt.Key} toggle resource use");
                        if (!SNet.IsMaster) NetworkAPI.InvokeEvent<ZiMain.ZINetInfo>("ZINetInfo", new ZiMain.ZINetInfo(1, bt.Value.m_playerAgent.PlayerSlotIndex, 0, 0, 0));
                        if (SNet.IsMaster)
                        {
                            whocomp = bt.Value.GetComponent<zComputer>();
                            if (whocomp.shareaction != null) whocomp.shareaction.DescBase.SetCompletionStatus(PlayerBotActionBase.Descriptor.StatusType.Failed);
                            whocomp.allowedshare = !whocomp.allowedshare;
                        }
                    }
                }
            }

            if (wha == "clear command queue")
            {
                foreach (KeyValuePair<String, PlayerAIBot> bt in ZiMain.BotTable)
                {
                    if (who == "AllBots" || who == bt.Key)
                    {
                        log.LogInfo($"{bt.Key} stop action");
                        if (!SNet.IsMaster) NetworkAPI.InvokeEvent<ZiMain.ZINetInfo>("ZINetInfo", new ZiMain.ZINetInfo(5, bt.Value.m_playerAgent.PlayerSlotIndex, 0, 0, 0));
                        if (SNet.IsMaster)
                        {
                            whocomp = bt.Value.GetComponent<zComputer>();
                            whocomp.PreventManualActions();
                        }
                    }
                }
            }

            if (wha == "pickup resource under my aim")
            {
                log.LogInfo($"bot " + who + " pickup resource");
                var item = zSearch.GetItemUnderPlayerAim();
                if (item != null)
                    SendBotToPickupItemOld(who, item);
            }

            if (wha == "supply resource (aimed or me)")
            {
                log.LogInfo($"bot " + who + " share resource");
                ZiMain.SendBotToShareResourcePackOld(who, zSearch.GetHumanUnderPlayerAim());
            }

            if (wha.Contains("sentry mode"))
            {
                if (who == "AllBots")
                {
                    log.LogInfo("all bots sentry mode");
                    foreach (KeyValuePair<String, PlayerAIBot> bt in ZiMain.BotTable)
                    {
                        var zombie = bt.Value.GetComponent<zComputer>();
                        zombie.allowedmove = !zombie.allowedmove;
                        if (zombie.allowedmove == true)
                        {
                            zombie.followaction.DescBase.Status = PlayerBotActionBase.Descriptor.StatusType.None;
                            zombie.travelaction.DescBase.Status = PlayerBotActionBase.Descriptor.StatusType.None;
                            zombie.updateExtraInfo();
                        }
                    }
                }
                else
                {
                    log.LogInfo($"bot " + who + " sentry mode");
                    var zombie = BotTable[who].GetComponent<zComputer>();
                    zombie.allowedmove = !BotTable[who].GetComponent<zComputer>().allowedmove;
                    if (zombie.allowedmove == true)
                    { 
                        zombie.followaction.DescBase.Status = PlayerBotActionBase.Descriptor.StatusType.None;
                        zombie.travelaction.DescBase.Status = PlayerBotActionBase.Descriptor.StatusType.None;
                        zombie.updateExtraInfo();
                    }
                }
            }
        } // if islastnode
    } // playconfirm
} // zombifiedpatches

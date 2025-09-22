using GameData;
using HarmonyLib;
using Player;
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

    [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.UpdateActionShareResoursePack))]
    [HarmonyPrefix]
    public static bool UpdateActionShareResoursePack(RootPlayerBotAction __instance, ref PlayerBotActionBase.Descriptor bestAction)
    {
        //This is based on the original mono decomp. Not a re-creation.  Still quite modified though.
        //Todo have a custom threshold per type - DONE
        //Todo Block some types outright. - DONE
        //Todo if someone else is already giving the target the same item, don't double up.
        if (!__instance.m_shareResourceAction.IsTerminated())
        {
            //is there already share resource action?
            return false;
        }
        __instance.m_shareResourceAction.Prio = RootPlayerBotAction.m_prioSettings.ShareResource;
        if (!RootPlayerBotAction.CompareActionPrios(__instance.m_shareResourceAction, bestAction))
        {
            //Do we already have a more important action?
            return false;
        }
        if (__instance.m_bot.IsActionForbidden(__instance.m_shareResourceAction))
        {
            //Are we allowed to do this action?
            return false;
        }
        PlayerBackpack backpack = PlayerBackpackManager.GetBackpack(__instance.m_agent.Owner);
        BackpackItem backpackItem;
        if (!backpack.TryGetBackpackItem(InventorySlot.ResourcePack, out backpackItem))
        {
            //Do we have any resources to share?
            return false;
        }
        int agentID = __instance.m_agent.Owner.PlayerSlotIndex();
        var itemID = backpackItem.ItemID;
        if (!zSlideComputer.SharePerms[agentID])
        {
            //Do we have share perms?
            return false;
        }
        if (!zSlideComputer.enabledResourceShares[itemID])
        {
            //Is this item dissabled?
            return false;
        }
        

        PlayerAmmoStorage ammoStorage = backpack.AmmoStorage;
        float ammoutCanGivePercent = ammoStorage.ResourcePackAmmo.CostOfBullet / ammoStorage.ResourcePackAmmo.AmmoMaxCap;

        PlayerAgent chosenAgent = null;
        float topCandidateScore = 0f;

        for (int i = 0; i < PlayerManager.PlayerAgentsInLevel.Count; i++)
        {
            PlayerAgent candidateAgent = PlayerManager.PlayerAgentsInLevel[i];

            if (candidateAgent == null || !candidateAgent.Alive)
            {
                //if the agent is null, or they're dead ignore them.
                continue;
            }
            if (candidateAgent != __instance.m_agent)
            {
                float prio = __instance.m_shareResourceAction.Prio;
                Vector3 position = candidateAgent.Position;

                if (__instance.m_bot.ApplyRestrictionsToRootPosition(ref position, ref prio))
                {
                    //would moving to the candidate cause problems?
                    continue;
                }
            }
            switch (itemID)
            {
                case 102: //medpack
                    ammoutCanGivePercent = AgentModifierManager.ApplyModifier(__instance.m_agent, AgentModifier.HealSupport, ammoutCanGivePercent);//This is artifacts!;
                    break;
                case 127: //toolPack
                case 101: //Ammopack
                    ammoutCanGivePercent = AgentModifierManager.ApplyModifier(__instance.m_agent, AgentModifier.AmmoSupport, ammoutCanGivePercent);
                    break;
                case 132: //Disinfect
                default: //something weird
                    break;
            }
            float threshHold = 0.98f; //not needed fallback 
            if (zSlideComputer.resourceThresholds.ContainsKey(itemID))
            {
                int baseThreshold = zSlideComputer.resourceThresholds[itemID];
                float clampedThreshold = Mathf.Clamp01(baseThreshold / 100f);
                float lowerThreshold = Mathf.Lerp(0f, 0.98f, clampedThreshold);
                threshHold = Math.Min(lowerThreshold + ammoutCanGivePercent,0.98f);
            }
            else
            {
                ZiMain.log.LogWarning($"Tried share unknown resoruce pack ID {itemID}");
                return false;
            }
            PlayerAmmoStorage candidateAmmoStorage = PlayerBackpackManager.GetBackpack(candidateAgent.Owner).AmmoStorage;
            float candidateScore = 0f;
            //I'm not sure if m_gearAvailability is the best way to go here, Switching to itemID.  Hopefully that doesn't cause any unexpected issues.
            switch (itemID)
            {
                case 102:
                    if (candidateAgent.Damage.GetHealthRel() + ammoutCanGivePercent < threshHold) // Would you have less than threshHold after heals?
                    {
                        candidateScore = threshHold - candidateAgent.Damage.GetHealthRel(); // more damaged -> higher score
                    }
                    break;
                case 101:
                    if (candidateAmmoStorage.StandardAmmo.RelInPack + ammoutCanGivePercent < threshHold &&
                        candidateAmmoStorage.SpecialAmmo.RelInPack + ammoutCanGivePercent < threshHold) // Would we overflow primary or secondary ammo?
                    {
                        candidateScore = threshHold - (candidateAmmoStorage.StandardAmmo.RelInPack + candidateAmmoStorage.SpecialAmmo.RelInPack) / 2f; // average of both ammo pools
                    }
                    break;
                case 132:
                    if (candidateAgent.Damage.Infection * threshHold > ammoutCanGivePercent) // Would we overheal?
                    {
                        candidateScore = candidateAgent.Damage.Infection * threshHold; // score is how infected they are
                    }
                    break;
                case 127:
                    if (candidateAgent.NeedToolAmmo() && candidateAmmoStorage.ClassAmmo.RelInPack + ammoutCanGivePercent < threshHold) // would we overflow tool ammo?
                    {
                        candidateScore = threshHold - candidateAmmoStorage.ClassAmmo.RelInPack; // how much ammo are they missing?
                    }
                    break;
                default:
                    break;
            }
            if (candidateScore > 0f)//did they score at all?
            {
                candidateScore = Mathf.Clamp01(candidateScore / threshHold);//make sure the score is not over 1 and reduce it slightly.
                if (!candidateAgent.Owner.IsBot)
                {
                    candidateScore = Mathf.Lerp(candidateScore, 1f, 0.5f);//prioritize players.
                }

                if (candidateScore > topCandidateScore)
                {
                    chosenAgent = candidateAgent;
                    topCandidateScore = candidateScore;
                }
            }
        }
        if (chosenAgent != null)
        {
            __instance.m_shareResourceAction.Item = backpackItem.Instance.Cast<ItemEquippable>();
            __instance.m_shareResourceAction.Receiver = chosenAgent;
            __instance.m_shareResourceAction.Haste = 0.8f;
            bestAction = __instance.m_shareResourceAction;
        }
        return false;
    }


    [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.UpdateActionCollectItem))]
    [HarmonyPrefix]
    public static bool UpdateActionCollectItem(RootPlayerBotAction __instance)
    {
        if (!zSlideComputer.GetPickupPermission(__instance.m_agent.Owner.PlayerSlotIndex()))
        {
            return false;
        }
        return true;
    }

    public static float newPrio = 0f;
    [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.GetItemPrio))]
    [HarmonyPrefix]
    public static bool GetItemPrio(RootPlayerBotAction __instance, InventorySlot itemSlot, uint itemID, ref float __result)
    {
        //This is a full re-implentation of the original method.  But without the hard coded values.
        //This approach allows me to support arbitrary item pickups not normally in the list, without breaking the logic.
        //Theoretically if there are a bunch of new items in the list, they could get into a "hot potato" loop.  but I'm calling that a "known shippable" for now.
        
        //var originalResult = __result;
        __result = 0f;
        if (!zSlideComputer.GetPickupPermission(__instance.m_agent.Owner.PlayerSlotIndex()))
            return false;
        if (!zSlideComputer.enabledItemPrios.ContainsKey(itemID) || !zSlideComputer.enabledItemPrios[itemID])
            return false;
        ItemDataBlock itemDataBlock;
        if (ItemDataBlock.s_blockByID.ContainsKey(itemID))
        {
            itemDataBlock = ItemDataBlock.s_blockByID[itemID];
        }
        else
        {
            ZiMain.log.LogError($"Tried to get priority for unknow id: {itemID}");
            return false;
        }
        if (!RootPlayerBotAction.s_itemBasePrios.ContainsKey(itemID))
        {
            ZiMain.log.LogWarning($"Tried to get priority for unmapped item: ({itemID}){itemDataBlock.name} in {itemSlot}");
            return false;
        }
        __result = 0f;
        float basePriority = RootPlayerBotAction.s_itemBasePrios[itemID];

        List<PlayerAgent> playerAgentsList = PlayerManager.PlayerAgentsInLevel.ToArray().ToList();

        float highestAmmoCap = 0f;
        float currentTotalAmmoOfOtherBotsNotThisBot = 0f;
        float priorityFloor = 0.15f;
        int foundMe = 0;
        int otherAgents = 0;
        foreach (PlayerAgent agent in playerAgentsList)
        {
            if (agent.CharacterID == __instance.m_agent.CharacterID)
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
            var alive = __instance.m_agent is UnityEngine.Object uObj && uObj;
            ZiMain.log.LogError($"Could not find this agent! This will probably cause issues. {alive}");
        }
        float maxOtherTotal = -1f;
        float fillFactor = -1f;
        float minPriority = -1f;
        if (highestAmmoCap > 0.0f && otherAgents > 0)
        {
            maxOtherTotal = playerAgentsList.Count * highestAmmoCap;
            fillFactor = currentTotalAmmoOfOtherBotsNotThisBot / maxOtherTotal;
            minPriority = basePriority * priorityFloor;

            __result = Mathf.Lerp(basePriority, minPriority, fillFactor);
        }
        else
        {
            __result = basePriority;
        }
        //if (Math.Abs(__result - originalResult) > 5)
        //{ 
        //    //Leaving this as a prefix for a bit to make sure this actually works.
        //    ZiMain.log.LogWarning($"Priority: {__result} vs {originalResult}");
        //    ZiMain.log.LogMessage($"Foundme: {foundMe}");
        //    ZiMain.log.LogMessage($"otherAgents count: {otherAgents}");
        //    ZiMain.log.LogMessage($"highestAmmoCap: {highestAmmoCap}");
        //    ZiMain.log.LogMessage($"currentTotalAmmo: {currentTotalAmmoOfOtherBotsNotThisBot}");
        //    ZiMain.log.LogMessage($"basePriority: {basePriority}");
        //    ZiMain.log.LogMessage($"priorityFloor: {priorityFloor}");
        //    ZiMain.log.LogMessage($"maxOtherTotal: {maxOtherTotal}");
        //    ZiMain.log.LogMessage($"fillFactor: {fillFactor}");
        //    ZiMain.log.LogMessage($"minPriority: {minPriority}");
        //}
        return false;
    }

    [HarmonyPatch(typeof(PlaceNavMarkerOnGO), nameof(PlaceNavMarkerOnGO.UpdateExtraInfo))]
    [HarmonyPostfix]
    public static void UpdateExtraInfoPatch(PlaceNavMarkerOnGO __instance)
    {
        string original = __instance.m_extraInfo;
        PlayerAgent agent = __instance.m_player;
        if (agent == null || agent.Owner == null || !agent.Owner.IsBot)
            return;
        int playerID = agent.Owner.PlayerSlotIndex();//todo cache the ID somewhere
        bool allowedPickup = zSlideComputer.PickUpPerms[playerID];
        bool allowedShare = zSlideComputer.SharePerms[playerID];
        bool allowedMove = zSlideComputer.MovePerms[playerID];
        if (original.Contains("Pickup:"))
        {
            bool dirty = false;
            List<string> lines = new List<string>(original.Split('\n'));
            foreach (string line in lines)
            {
                if (line.Contains("Pickup:") && (line.Contains("True") != allowedPickup)) 
                {
                    dirty = true;
                    break;
                }
                if (line.Contains("Share:") && (line.Contains("True") != allowedShare)) 
                {
                    dirty = true;
                    break;
                }
                if (line.Contains("Sentry:") && (line.Contains("True") == allowedMove))
                {
                    dirty = true;
                    break;
                }
            }
            if (!dirty) return;
            //remove our custom info so it can be re added
            original = original.Substring(0, original.IndexOf("Pickup:")).TrimEnd(); //TODO detect actual end of our string instead of assuming this is the last thing for better compat.
        }
        string pickups = $"Pickup: <color=#FFA50066>{allowedPickup}</color>";
        string share = $"Share: <color=#FFA50066>{allowedShare}</color>";
        string move = $"Sentry: <color=#FFA50066>{!allowedMove}</color>";
        __instance.m_extraInfo = original + "<color=#CCCCCC66><size=70%>\n" + pickups + "\n" + share + "\n" + move + "</size></color>";
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
} // zombifiedpatches

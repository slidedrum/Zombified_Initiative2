using AIGraph;
using BetterBots.Components;
using GameData;
using Gear;
using HarmonyLib;
using LevelGeneration;
using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using ZombieTweak2;
using ZombieTweak2.zRootBotPlayerAction;
using static Zombified_Initiative.ZiMain;

namespace Zombified_Initiative;

[HarmonyPatch]
public class ZombifiedPatches
{
    //This file contains all harmony patches.
    //Might split this up later if there gets to be too many of them.

    [HarmonyPatch(typeof(PlayerBotActionBase.Descriptor), nameof(PlayerBotActionBase.Descriptor.InternalOnTerminated))]
    [HarmonyPostfix]
    public static void InternalOnTerminated(PlayerBotActionBase.Descriptor __instance)
    {
        if (zActionSub.actionCallbacks.ContainsKey(__instance.Pointer))
        {
            zActionSub.actionCallbacks[__instance.Pointer].Invoke();
        }
    }
    //[HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.OnObjectHighlighted))]
    //[HarmonyPrefix]
    //public static void OnObjectHighlighted(PlayerManager instance, AIG_CourseNode courseNode, GameObject go)
    //{
    //    //if (!PlayerManager.Current.BotAIData.ObjectsToHighlight.ContainsKey(courseNode))
    //    //    PlayerManager.Current.MapHighlightableObjects(courseNode);
    //    ZiMain.log.LogInfo($"Pinged {go.name}");
    //    if (PlayerManager.Current.BotAIData.ObjectsToHighlight[courseNode].Contains(go))
    //        ZiMain.log.LogInfo($"Bot pingable {go.name}");
    //}
    //[HarmonyPatch(typeof(LG_ResourceContainer_Storage), nameof(LG_ResourceContainer_Storage.SetPickupInteractions))]
    //[HarmonyPostfix]
    //public static void SetPickupInteractions(LG_ResourceContainer_Storage instance, float wait, bool active)
    //{
    //    if (!active)
    //        return;
    //    var node = instance.m_core.SpawnNode;
    //    if (!PlayerManager.Current.BotAIData.ObjectsToHighlight.ContainsKey(node))
    //        PlayerManager.Current.MapHighlightableObjects(node);
    //    foreach (var pickup in instance.PickupInteractions)
    //    {
    //        if (pickup?.gameObject == null) continue;
    //        PlayerManager.Current.BotAIData.ObjectsToHighlight[node].Add(pickup.gameObject);
    //        ZiMain.log.LogInfo($"Added to pings {pickup.gameObject.name}");
    //    }
    //}
    //[HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.UpdateActionHighlight))]
    //[HarmonyPostfix]
    //public static void UpdateActionHighlight(RootPlayerBotAction instance, ref PlayerBotActionBase.Descriptor bestAction)
    //{
    //    if (!instance.m_highlightAction.IsTerminated())
    //    {
    //        return;
    //    }
    //    instance.m_highlightAction.Prio = RootPlayerBotAction.m_prioSettings.Highlight;
    //    if (!RootPlayerBotAction.CompareActionPrios(instance.m_highlightAction, bestAction))
    //    {
    //        return;
    //    }
    //    if (instance.m_bot.IsActionForbidden(instance.m_highlightAction))
    //    {
    //        return;
    //    }
    //    AIG_CourseNode courseNode = instance.m_agent.CourseNode;
    //    Il2CppSystem.Collections.Generic.List<GameObject> list = PlayerManager.Current.MapHighlightableObjects(courseNode);
    //    GameObject gameObject = null;
    //    Vector3 position = instance.m_agent.Position;
    //    Vector3 vector = Vector3.zero;
    //    float num = RootPlayerBotAction.s_highlightSearchDistance * RootPlayerBotAction.s_highlightSearchDistance;
    //    PlayerBotActionHighlight.Descriptor.TargetTypeEnum targetTypeEnum = PlayerBotActionHighlight.Descriptor.TargetTypeEnum.Door;
    //    for (int i = 0; i < list.Count; i++)
    //    {
    //        GameObject candidateObject = list[i];
    //        if (candidateObject == null) 
    //        {
    //            //list.RemoveAt(i--);
    //            continue;
    //        }
    //        Vector3 vector2 = candidateObject.transform.position - position;
    //        float sqrMagnitude = vector2.sqrMagnitude;
    //        ZiMain.log.LogInfo($"ping candidate: {candidateObject.name}");
    //        //if (sqrMagnitude > num || instance.IsAnyHumanPlayerNear(candidateObject.transform.position, 0.1f))
    //        //    continue;
    //        RootPlayerBotAction.s_tempObjReservation.CharacterID = instance.m_agent.CharacterID;
    //        RootPlayerBotAction.s_tempObjReservation.Object = candidateObject;
    //        Vector3 vector3 = candidateObject.transform.position;
    //        if (PlayerManager.Current.IsObjectReserved(RootPlayerBotAction.s_tempObjReservation))
    //            continue;


    //        vector = candidateObject.transform.position;
    //        gameObject = candidateObject;
    //        break;
    //        if (false)
    //        {
    //            LG_Gate gateComponenet = candidateObject.GetComponentInParent<LG_Gate>();
    //            PlayerBotActionHighlight.Descriptor.TargetTypeEnum targetTypeEnum2;
    //            targetTypeEnum2 = PlayerBotActionHighlight.Descriptor.TargetTypeEnum.Terminal;
    //            if (gateComponenet != null)
    //            {
    //                if (!PlayerBotActionUseEnemyScanner.Descriptor.CheckGateNeedsScanning(gateComponenet))
    //                {
    //                    list.RemoveAt(i--);
    //                    continue;
    //                }
    //                if (!instance.GetPositionInFrontOfGate(gateComponenet, vector2, instance.DescBase.Prio, out vector3))
    //                {
    //                    continue;
    //                }
    //                targetTypeEnum2 = PlayerBotActionHighlight.Descriptor.TargetTypeEnum.Door;
    //            }
    //            else
    //            {
    //                LG_ResourceContainer_Storage storageComponenet = candidateObject.GetComponentInParent<LG_ResourceContainer_Storage>();
    //                if (storageComponenet != null)
    //                {
    //                    LG_WeakResourceContainer component = storageComponenet.gameObject.GetComponent<LG_WeakResourceContainer>();
    //                    if (component == null || component.ISOpen)
    //                    {
    //                        list.RemoveAt(i--);
    //                        continue;
    //                    }
    //                    vector3 = candidateObject.transform.position - candidateObject.transform.up * RootPlayerBotAction.s_highlightStandDistance;
    //                    if (!instance.SnapPositionToNav(vector3, out vector3))
    //                    {
    //                        continue;
    //                    }
    //                    RootPlayerBotAction.s_tempPosReservation.CharacterID = instance.m_agent.CharacterID;
    //                    RootPlayerBotAction.s_tempPosReservation.Position = vector3;
    //                    if (PlayerManager.Current.IsPositionReserved(RootPlayerBotAction.s_tempPosReservation))
    //                    {
    //                        continue;
    //                    }
    //                    targetTypeEnum2 = PlayerBotActionHighlight.Descriptor.TargetTypeEnum.Container;
    //                }
    //                else
    //                {
    //                    LG_ComputerTerminal terminalComponent = candidateObject.GetComponentInParent<LG_ComputerTerminal>();
    //                    if (terminalComponent != null)
    //                    {
    //                        Vector3 forward = terminalComponent.m_CameraAlign.forward;
    //                        forward.Set(-forward.x, 0f, -forward.z);
    //                        forward.Normalize();
    //                        vector3 = candidateObject.transform.position + forward * 1.5f;
    //                        targetTypeEnum2 = PlayerBotActionHighlight.Descriptor.TargetTypeEnum.Terminal;
    //                    }
    //                    else
    //                    {
    //                        targetTypeEnum2 = PlayerBotActionHighlight.Descriptor.TargetTypeEnum.Terminal;
    //                        Transform current = candidateObject.transform;
    //                        int depth = 0;
    //                        LG_WeakResourceContainer rootContainer = null;
    //                        while (current != null && depth < 7)
    //                        {
    //                            rootContainer = current.GetComponent<LG_WeakResourceContainer>();
    //                            if (rootContainer != null)
    //                                break;
    //                            current = current.parent;
    //                            depth++;
    //                        }
    //                        if (rootContainer == null)
    //                            continue;
    //                        vector3 = rootContainer.transform.position - rootContainer.transform.up * RootPlayerBotAction.s_highlightStandDistance;
    //                    }
    //                }
    //            }
    //            float prio = instance.m_highlightAction.Prio;
    //            if (instance.m_bot.ApplyRestrictionsToRootPosition(ref vector3, ref prio))
    //                continue;
    //            Vector3 vector4 = candidateObject.transform.position + Vector3.up * 0.3f;
    //            RaycastHit raycastHit;
    //            if (instance.m_bot.CanSeePosition(instance.m_agent.EyePosition, vector4, LayerManager.MASK_WORLD, out raycastHit) || raycastHit.transform.gameObject == candidateObject || raycastHit.transform.IsChildOf(candidateObject.transform.parent.transform) || candidateObject.transform.IsChildOf(raycastHit.transform))
    //            {
    //                gameObject = candidateObject;
    //                targetTypeEnum = targetTypeEnum2;
    //                num = sqrMagnitude;
    //                vector = vector3;
    //            }
    //        }
    //    }
    //    if (gameObject != null)
    //    {
    //        instance.m_highlightAction.TargetType = targetTypeEnum;
    //        instance.m_highlightAction.TargetGO = gameObject;
    //        instance.m_highlightAction.TargetPosition = vector;
    //        instance.m_highlightAction.CourseNode = courseNode;
    //        bestAction = instance.m_highlightAction;
    //    }
    //}
    //[HarmonyPatch(typeof(PlayerBotActionHighlight), nameof(PlayerBotActionHighlight.MoveOut))]
    //[HarmonyPrefix]
    //private static bool MoveOut(PlayerBotActionHighlight instance)
    //{

    //    // GetCorners the Type for the Descriptor class
    //    Type descriptorType = typeof(PlayerBotActionHighlight.Descriptor);

    //    // GetCorners the private static field
    //    FieldInfo fieldInfo = descriptorType.GetField(
    //        "NativeMethodInfoPtr_OnTravelActionEvent_Public_Void_Descriptor_0",
    //        BindingFlags.NonPublic | BindingFlags.Static
    //    );

    //    if (fieldInfo == null)
    //    {
    //        throw new Exception("Field not found!");
    //    }

    //    // Read the IntPtr value from the static field
    //    IntPtr methodPtr = (IntPtr)fieldInfo.GetValue(null);
    //    PlayerBotActionTravel.Descriptor descriptor = new PlayerBotActionTravel.Descriptor(instance.m_bot)
    //    {
    //        ParentActionBase = instance,
    //        Prio = instance.m_desc.Prio,
    //        EventDelegate = new PlayerBotActionBase.Descriptor.EventDelegateFunc(methodPtr),
    //        Haste = 0.5f,
    //        Radius = 10f,
    //        Persistent = false,
    //        RadiusHeightTolerance = 3f,
    //        DestinationType = PlayerBotActionTravel.Descriptor.DestinationEnum.Position,
    //        DestinationPos = instance.m_desc.TargetPosition
    //    };
    //    if (instance.m_bot.RequestAction(descriptor))
    //    {
    //        instance.m_travelAction = descriptor;
    //        instance.SetState(PlayerBotActionHighlight.State.Move);
    //        return false;
    //    }
    //    instance.m_desc.SetCompletionStatus(PlayerBotActionBase.Descriptor.StatusType.Failed);
    //    return false;
    //}
    //[HarmonyPatch(typeof(PlayerBotActionHighlight), nameof(PlayerBotActionHighlight.VerifyTarget))]
    //[HarmonyPrefix]
    //private static bool VerifyTarget(PlayerBotActionHighlight instance, ref bool __result)
    //{
    //    if (instance.m_desc.TargetGO == null)
    //    {
    //        __result = false;
    //        return false;
    //    }
    //    if (instance.IsAnyHumanPlayerNear(instance.m_desc.TargetGO.transform.position, 3f))
    //    {
    //        __result = false;
    //        return false;
    //    }
    //    if (!PlayerManager.Current.IsObjectHighlightable(instance.m_desc.CourseNode, instance.m_desc.TargetGO))
    //    {
    //        __result = false;
    //        return false;
    //    }
    //    __result = true;
    //    return false;
    //}
    //[HarmonyPatch(typeof(PlayerBotActionHighlight), nameof(PlayerBotActionHighlight.DetermineLookAtPosition))]
    //[HarmonyPrefix]
    //public static bool DetermineLookAtPosition(PlayerBotActionHighlight instance)
    //{
    //    instance.m_lookAtPosition = instance.m_desc.TargetGO.transform.position;
    //    return false;
    //}
    [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.UpdateActionShareResoursePack))]
    [HarmonyPrefix]
    public static bool UpdateActionShareResoursePack(RootPlayerBotAction __instance, ref PlayerBotActionBase.Descriptor bestAction)
    {
        //This is based on the original mono decomp. Not a re-creation.  Still quite modified though.
        //Todo have a custom threshold per type - DONE
        //Todo Block some types outright. - DONE
        //Todo if someone else is already giving the visTarget the same item, don't double up.
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
            //Do we have any resourcesActions to share?
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


        if (ZiMain.HasBetterBots && CheckBetterBotsDanger(__instance.m_agent.gameObject))
        {
            return false; // skip sharing if bot is in danger
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
                foreach (var agent in PlayerManager.PlayerAgentsInLevel)
                {
                    //Check if any other bots are giving the same visTarget the same resource
                    if (!agent.Owner.IsBot)
                        continue;
                    PlayerAIBot aiBot = agent.gameObject.GetComponent<PlayerAIBot>();
                    foreach (var action in aiBot.Actions)
                    {
                        if (action.GetIl2CppType().Name == "PlayerBotActionShareResourcePack")
                        {
                            var descriptor = action.DescBase.Cast<PlayerBotActionShareResourcePack.Descriptor>();
                            if (descriptor.Item.ItemDataBlock.persistentID == itemID
                                && descriptor.Receiver.CharacterID == candidateAgent.CharacterID)
                            {
                                //if other bots are sharing that resource, reduce threshold so nothing is wasted
                                baseThreshold -= (int)ammoStorage.ResourcePackAmmo.CostOfBullet;
                            }
                        }
                    }
                }
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
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool CheckBetterBotsDanger(GameObject agentGO)
    {
        BotRecorder component = agentGO.GetComponent<BotRecorder>();
        if (component != null)
        {
            return component.IsInDangerousSituation();
        }
        return false;
    }

    private static PlayerBotActionBase.Descriptor originalBestAction;
    [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.UpdateActionCollectItem))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)] //Needed for betterbots compat
    public static void FirstUpdateActionCollectItem(RootPlayerBotAction __instance, ref PlayerBotActionBase.Descriptor bestAction)
    {
        originalBestAction = bestAction;
    }

    [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.UpdateActionCollectItem))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)] //Needed for betterbots compat
    public static void UpdateActionCollectItem(RootPlayerBotAction __instance, ref PlayerBotActionBase.Descriptor bestAction)
    {
        if (!zSlideComputer.GetPickupPermission(__instance.m_agent.Owner.PlayerSlotIndex()))
        {
            bestAction = originalBestAction;
        }

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
        //WTF is this?  Just a debugCube thing?
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
    public static Dictionary<int, float> lastTimeCheckedForWakeUp = new();

    
    [HarmonyPatch(typeof(PlayerBotActionMelee), nameof(PlayerBotActionMelee.UpdateTravelAction))]
    [HarmonyPrefix]
    public static bool UpdateTravelAction(PlayerBotActionMelee __instance)
    {
        //Yet another method I had to completely re-create because of a hard coded value.  This should be almost identicle to the original method
        //The only change is making the travel action inherit the haste of the parent action.
        if (__instance.m_desc == null)
            return false;

        // If travel is disabled, stop any current travel action
        if (!__instance.m_desc.Travel)
        {
            if (__instance.m_travelAction != null && __instance.m_travelAction.Status == PlayerBotActionBase.Descriptor.StatusType.Active)
            {
                __instance.m_bot?.StopAction(__instance.m_travelAction);
            }
            return false;
        }
        GameObject targetObject = null;

        // Determine visTarget: either the agent's GameObject or a preset visTarget
        if (__instance.m_desc.TargetAgent != null)
        {
            targetObject = __instance.m_desc.TargetAgent.gameObject;
        }
        else if (__instance.m_desc.TargetGameObject != null)
        {
            targetObject = __instance.m_desc.TargetGameObject;
        }

        // Check if existing travel action exists
        if (__instance.m_travelAction != null)
        {
            // If destination already matches visTarget, no update needed
            if (__instance.m_travelAction.Status == PlayerBotActionBase.Descriptor.StatusType.Active &&
                __instance.m_travelAction.DestinationObject == targetObject)
            {
                return false;
            }

            // Stop the old travel action if active
            if (__instance.m_travelAction.Status == PlayerBotActionBase.Descriptor.StatusType.Active)
            {
                __instance.m_bot?.StopAction(__instance.m_travelAction);
            }
        }
        // Request a new travel action
        if (__instance.m_bot != null && __instance.m_bot.RequestAction(__instance.m_travelAction))
        {
            if (__instance.m_travelAction != null)
            {
                __instance.m_travelAction.DestinationObject = targetObject;
                __instance.m_travelAction.Haste = __instance.m_desc.Haste;
                var attackAction = __instance.m_bot.Actions[0].Cast<RootPlayerBotAction>().m_attackAction;
                var attackActionDescriptor = attackAction?.Cast<PlayerBotActionAttack.Descriptor>();
                if (!attackAction?.IsTerminated() ?? false && attackActionDescriptor != null)
                {
                    __instance.m_travelAction.WalkPosture = attackActionDescriptor.Posture;
                }
                if (targetObject != null && targetObject.transform != null)
                {
                    Vector3 pos = targetObject.transform.position;
                    __instance.m_lastTravelToPosition = pos;
                }
            }
        }
        if (targetObject != null && targetObject.transform != null && zActions.isManualAction(__instance.m_descBase))
        {
            if (!lastTimeCheckedForWakeUp.ContainsKey(__instance.m_bot.GetInstanceID()))
            {
                lastTimeCheckedForWakeUp[__instance.m_bot.GetInstanceID()] = Time.time;
                return false;
            }
            if (Time.time - lastTimeCheckedForWakeUp[__instance.m_bot.GetInstanceID()] > 1f)
            {
                lastTimeCheckedForWakeUp[__instance.m_bot.GetInstanceID()] = Time.time;
                if (rng.Next(0, wakeChancePerSecond) == 0)
                {
                    ZiMain.wakeUpRoom(__instance.m_bot, targetObject);
                    __instance.DescBase.SetCompletionStatus(PlayerBotActionBase.Descriptor.StatusType.Failed);
                }
            }

        }
        return false;
    }


    [HarmonyPatch(typeof(PlayerBotActionAttack), nameof(PlayerBotActionAttack.UpdateMeleeAttack))]
    [HarmonyPrefix]
    private static void UpdateMeleeAttack(PlayerBotActionAttack __instance, bool push)
    {
        // 1) Stop any firing / nanoswarm actions (SafeStopAction used in decomp)
        __instance.SafeStopAction(__instance.m_fireAction);
        __instance.SafeStopAction(__instance.m_useNanoswarmAction);

        // 2) Prepare variables used for visTarget selection & strike decision
        Transform aimTransform = null;
        float vulnerableScore = 0.0f;
        bool strike = false;

        // 3) Preserve previous TargetGameObject if we recently selected a visTarget
        GameObject preservedTarget = null;
        if (__instance.m_meleeAction != null && __instance.m_meleeAction.TargetGameObject != null)
        {
            // If still within visTarget re-selection delay, prefer the existing visTarget object
            if (Time.time - __instance.m_targetSelectedTime < PlayerBotActionAttack.s_targetReselectionDelay)
            {
                preservedTarget = __instance.m_meleeAction.TargetGameObject;
            }
        }

        // 4) Choose aimTransform and compute strike boolean
        if (push)
        {
            // push branch uses EasyAimTarget on the visTarget agent
            if (__instance.m_currentAttackOption == null || __instance.m_currentAttackOption.TargetAgent == null)
            {
                // decomp jumps to a trap; return early here to match safe behavior
                return;
            }

            aimTransform = __instance.m_currentAttackOption.TargetAgent.EasyAimTarget;
            strike = true; // push => always attempt strike (decomp set bVar13 = true)
        }
        else
        {
            // non-push: find a vulnerable visTarget, passing preservedTarget (may be null)
            if (__instance.m_currentAttackOption == null)
            {
                return;
            }

            aimTransform = __instance.FindVulnerableTarget(__instance.m_currentAttackOption.TargetAgent,
                                                preservedTarget,
                                                out vulnerableScore);
            strike = (0.2f < vulnerableScore); // decomp used 0.2 < local_res20[0]
        }

        // 5) If we have no current attack option, abort (decomp branches to trap)
        if (__instance.m_currentAttackOption == null)
        {
            return;
        }

        // 6) Read stance from the current attack option
        var stance = __instance.m_currentAttackOption.Stance;

        // 7) If no melee descriptor exists, create and initialize one (matching decomp)
        if (__instance.m_meleeAction == null)
        {
            var descriptor = new PlayerBotActionMelee.Descriptor(__instance.m_bot);

            descriptor.ParentActionBase = __instance;
            descriptor.Prio = __instance.m_desc.Prio;

            // Event delegate — decomp allocated a delegate object
            //descriptor.EventDelegate = instance.OnMeleeActionEvent;

            // New fields present in IL2CPP decomp
            descriptor.Push = push;
            descriptor.Loop = !push;
            descriptor.Travel = (stance == PlayerBotActionAttack.StanceEnum.Engage /* == 2 in decomp */);

            // Values observed in the decomp
            descriptor.Haste = __instance.m_desc.Haste;
            descriptor.Force = 0.75f;
            descriptor.Strike = strike;


            // Set visTarget agent and (optionally) visTarget game object
            descriptor.TargetAgent = __instance.m_currentAttackOption.TargetAgent;
            if (aimTransform != null)
            {
                descriptor.TargetGameObject = aimTransform.gameObject;
            }

            // Assign weapon if it's the expected melee type (decomp did a type check)
            var itemWeapon = __instance.m_currentAttackOption.ItemToUse as MeleeWeaponThirdPerson;
            descriptor.Weapon = itemWeapon;

            // Ask the bot to run this descriptor; if accepted, keep pointer
            if (__instance.m_bot != null && __instance.m_bot.RequestAction(descriptor))
            {
                __instance.m_meleeAction = descriptor;
                if (zActions.isManualAction(descriptor))
                {
                    FlexibleMethodDefinition callback = new FlexibleMethodDefinition(CheckForWakeChance, [__instance.m_bot, descriptor.TargetAgent.gameObject]);
                    zActionSub.addOnTerminated(descriptor, callback);
                }
            }
            return;
        }

        // 8) Else (m_meleeAction already exists) — update its fields to reflect new selection
        // Update priority from attack descriptor
        if (__instance.m_desc != null)
        {
            __instance.m_meleeAction.Prio = __instance.m_desc.Prio;
        }

        // Update visTarget agent
        __instance.m_meleeAction.TargetAgent = __instance.m_currentAttackOption.TargetAgent;

        // Update TargetGameObject if we have an aim transform
        if (aimTransform != null)
        {
            __instance.m_meleeAction.TargetGameObject = aimTransform.gameObject;
        }

        // Update weapon (perform cast as in original; decomp did a runtime type check)
        var updatedWeapon = __instance.m_currentAttackOption.ItemToUse as MeleeWeaponThirdPerson;
        __instance.m_meleeAction.Weapon = updatedWeapon;

        // Update Strike bool
        __instance.m_meleeAction.Strike = strike;
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



    [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.UpdateActionCollectItem))]
    [HarmonyPrefix]
    public static bool UpdateActionCollectItemReCreation(RootPlayerBotAction __instance, ref PlayerBotActionBase.Descriptor bestAction)
    {

        // Local temporaries that match responsibilities seen in the decompiled code
        PlayerBotActionCollectItem.Descriptor collect = __instance.m_collectItemAction;
        Item chosenItem = null;
        float bestItemPrio = 0.0f;
        AIG_CourseNode activityNode = null;
        Vector3 activityEpicenter = Vector3.zero;
        Player.BackpackItem foundBackpackItem = null;

        // If there is an active collect action and it's NOT terminated -> do nothing
        if (collect != null)
        {
            if (!collect.IsTerminated())
                return false;
        }

        // If prio settings exist and collect action exists, set the collect action's priority
        if (RootPlayerBotAction.m_prioSettings != null && collect != null)
        {
            collect.Prio = RootPlayerBotAction.m_prioSettings.CollectItem;
        }

        // If we already have a best action and the collect action is present:
        // if the collect action's priority is less-or-equal to the current best, don't consider it.
        if (bestAction != null)
        {
            if (collect == null)
            {
                // decompiled flow jumped to end in this case; just return (no change to bestAction)
                return false;
            }

            if (collect.Prio <= bestAction.Prio)
                return false;
        }

        // If we have a backpack, and it already has slot 8 occupied -> don't pick up anything.
        if (__instance.m_backpack != null)
        {
            if (__instance.m_backpack.HasBackpackItem(InventorySlot.InLevelCarry))
                return false;
        }

        // If the bot exists and the action is forbidden for that bot -> don't proceed
        if (__instance.m_bot != null && __instance.m_bot.IsActionForbidden(collect))
            return false;

        // Get the activity epicenter and a course node (area) where we should search
        if (!__instance.GetActivityEpicenter(out activityNode, out activityEpicenter))
            return false;
        activityEpicenter = __instance.m_bot.transform.position; //TODO patch GetActivityEpicenter
        activityNode = __instance.m_bot.Agent.CourseNode;
        // Prepare temporary reservations (object + position) using static temp objects on RootPlayerBotAction
        if (__instance.m_agent != null && RootPlayerBotAction.s_tempObjReservation != null)
        {
            RootPlayerBotAction.s_tempObjReservation.CharacterID = __instance.m_agent.CharacterID;
        }

        if (collect != null && RootPlayerBotAction.s_tempObjReservation != null)
        {
            RootPlayerBotAction.s_tempObjReservation.Prio = collect.Prio;
        }

        if (__instance.m_agent != null && RootPlayerBotAction.s_tempPosReservation != null)
        {
            RootPlayerBotAction.s_tempPosReservation.CharacterID = __instance.m_agent.CharacterID;
        }

        if (collect != null && RootPlayerBotAction.s_tempPosReservation != null)
        {
            RootPlayerBotAction.s_tempPosReservation.Prio = collect.Prio;
        }

        // We'll search storage containers that are attached to the discovered course node
        int containerIndex = 0;
        int chosenContainerIndex = -1;
        LG_ResourceContainer_Storage chosenContainer = null;
        Vector3 candidateRootPos = Vector3.zero;
        float candidateRadius = 0f;

        // Prepare some static values
        float collectSearchDistance = RootPlayerBotAction.s_collectItemSearchDistance;
        float collectStandDistance = RootPlayerBotAction.s_collectItemStandDistance;
        float collectSearchDistanceSqr = collectSearchDistance * collectSearchDistance;

        // If the area node is null we won't find containers to search
        if (activityNode != null && activityNode.MetaData?.StorageContainers != null)
        {
            List<LG_ResourceContainer_Storage> storageList = activityNode.MetaData.StorageContainers.ToArray().ToList();
            // iterate through storage containers
            for (containerIndex = 0; containerIndex < storageList.Count; containerIndex++)
            {
                var container = storageList[containerIndex];
                if (container == null)
                    continue;

                // container may have an internal items array/list; iterate items in the container
                // The decompiled code walked a specific internal array; here we use the container's public API
                // to find candidate item GameObjects and Items.
                // First: check the container GameObject / weak resource container to see if it is "active" / valid
                var containerGO = container.gameObject;
                if (containerGO == null)
                    continue;

                var weakContainerComp = containerGO.GetComponent<LG_WeakResourceContainer>();
                if (weakContainerComp == null)
                    continue;

                // In the decompiled code there was a check on a char/flag inside the component.
                // We preserve the logic: if the weak container is not "enabled/open" skip.
                if (!weakContainerComp.ISOpen) // <-- IsOpen is a guessed property name mapping to that char flag
                    continue;

                // Reserve this container temporarily and check whether the reservation collides with existing reservations
                if (RootPlayerBotAction.s_tempObjReservation == null)
                    continue;

                RootPlayerBotAction.s_tempObjReservation.Object = containerGO;

                // If the object/container is reserved by PlayerManager, skip it
                if (PlayerManager.Current == null)
                    continue;

                if (PlayerManager.Current.IsObjectReserved(RootPlayerBotAction.s_tempObjReservation))
                    continue;

                // set the collect action's target container tentatively
                if (collect == null)
                    continue;

                collect.TargetContainer = container;

                // If bot.TestFailureRetry returns false, skip container
                if (__instance.m_bot == null)
                    continue;

                if (!__instance.m_bot.TestFailureRetry(collect))
                    continue;

                // get container transform position
                Transform containerTransform = container.GetComponent<Transform>();
                if (containerTransform == null)
                    continue;

                Vector3 containerPos = containerTransform.position;

                // compute squared distance from activity epicenter to container position
                Vector3 diff = containerPos - activityEpicenter;
                float sqrDist = diff.sqrMagnitude;

                // Only consider containers within the search radius
                if (sqrDist >= collectSearchDistanceSqr)
                    continue;

                // Check whether any human players are near the container (don't pick up if humans nearby)
                if (__instance.IsAnyHumanPlayerNear(containerPos, 4.0f))
                    continue;

                // Compute a stand position in front of the container using container up vector and stand distance
                Vector3 itemPos = containerTransform.position;
                Vector3 up = containerTransform.up;
                Vector3 standOffset = up * collectStandDistance;
                Vector3 standCandidate = itemPos - standOffset;

                // Snap to nav and get radius (SnapPositionToNav will write rootPos and radius)
                Vector3 rootPos;
                float radius;
                if (!__instance.SnapPositionToNav(standCandidate, out rootPos))
                    continue;

                // populate the temp position reservation
                if (RootPlayerBotAction.s_tempPosReservation == null)
                    continue;

                RootPlayerBotAction.s_tempPosReservation.Position = rootPos; // note: decomp used field shuffling
                //RootPlayerBotAction.s_tempPosReservation.Radius = radius;

                // If position is reserved, skip
                if (PlayerManager.Current == null)
                    continue;
                if (PlayerManager.Current.IsPositionReserved(RootPlayerBotAction.s_tempPosReservation))
                    continue;

                // Ask AIBot to apply restrictions to root position (may change priority)
                if (__instance.m_bot == null)
                    continue;

                float prioRef = collect.Prio;
                if (__instance.m_bot.ApplyRestrictionsToRootPosition(ref rootPos, ref prioRef))
                {
                    // ApplyRestrictionsToRootPosition returning true in our naming means "rejected" in the decompiled control flow
                    // (decompiled returned early if it returned true), so skip this container if it returns true.
                    continue;
                }

                // Test danger zones around candidate root position
                Vector3 posForDanger = new Vector3(rootPos.x, rootPos.y, rootPos.z);
                if (!__instance.m_bot.TestDangerZones(posForDanger, AIDangerZone.SeverityEnum.CertainDeath, out AIDangerZone.SeverityEnum danger))
                    continue;

                // At this point the container is valid. Iterate the items inside the container and evaluate each Item.
                // The original iterated over a coroutine list of easeLocalScaleRoutine entries (child gameobjects).
                // We'll traverse game objects / child items: find components of type Item in container children.
                // This mirrors the decompiled behavior of scanning children and calling GetComponentInParent<Item>().
                Component[] potentialItems = container.GetComponentsInChildren<Component>(true);
                if (potentialItems == null)
                    continue;

                // iterate potentialItems like the decompiled code scanned coroutines entries
                int counter = 0;
                foreach (var comp in potentialItems)
                {
                    // get actual UnityEngine.Object and check not null
                    if (comp == null)
                    {
                        counter++;
                        continue;
                    }

                    // In the decomp they did a series of type checks / generic checks; here we attempt to find Item in parent
                    Item itemComponent = comp.GetComponentInParent<Item>();
                    if (itemComponent == null)
                    {
                        counter++;
                        continue;
                    }

                    if (itemComponent.ItemDataBlock == null)
                    {
                        counter++;
                        continue;
                    }

                    // Check whether the bot knows how to use this item (by gear CRC / id)
                    uint gearCRC = itemComponent.pItemData.itemID_gearCRC;
                    if (!PlayerAIBot.KnowsHowToUseItem(gearCRC))
                    {
                        counter++;
                        continue;
                    }

                    // compute the item priority for picking it up
                    GameData.ItemDataBlock dataBlock = itemComponent.ItemDataBlock;
                    if (dataBlock == null)
                    {
                        counter++;
                        continue;
                    }

                    float itemPrio = __instance.GetItemPrio(dataBlock.inventorySlot, itemComponent.pItemData.itemID_gearCRC);

                    // if this item's prio is not greater than the currently best, skip
                    if (itemPrio <= bestItemPrio)
                    {
                        counter++;
                        continue;
                    }

                    // If item type exists and we have a backpack, compare against an existing backpack item slot
                    if (__instance.m_backpack != null)
                    {
                        Player.BackpackItem existing;
                        if (__instance.m_backpack.TryGetBackpackItem(dataBlock.inventorySlot, out existing))
                        {
                            // If the existing backpack item has equal-or-better priority, skip this item
                            float existingPrio = __instance.GetItemPrio(dataBlock.inventorySlot, existing.ItemID);
                            if (itemPrio <= existingPrio)
                            {
                                counter++;
                                continue;
                            }
                        }
                    }

                    // Found a better item candidate — remember it
                    chosenItem = itemComponent;
                    bestItemPrio = itemPrio;
                    chosenContainer = container;
                    // save final root pos/radius to assign to action if accepted
                    candidateRootPos = rootPos;
                    //candidateRadius = radius;

                    counter++;
                } // end foreach comp

                // If we found a chosenItem in this container, assign to the collect action and finish
                if (chosenItem != null)
                {
                    // assign the target item and container to the collect descriptor
                    collect.TargetItem = chosenItem;
                    collect.TargetContainer = chosenContainer;

                    // store candidateRootPos into collect.TargetPosition (match decompiled axis mapping)
                    collect.TargetPosition = candidateRootPos;
                    //collect.TargetPosition.fields.y = candidateRootPos.y;
                    //collect.TargetPosition.fields.z = candidateRootPos.z;

                    collect.Haste = 0.5f;

                    // set the bestAction reference to the collect action we built
                    bestAction = collect;
                    return false;
                }
                // else: continue to next container
            } // end for each container
        } // end if activityNode != null

        // No valid pick found -> nothing to set; just return
        return false;
    }
} // zombifiedpatches

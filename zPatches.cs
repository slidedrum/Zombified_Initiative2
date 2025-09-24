using BetterBots.Components;
using Enemies;
using GameData;
using Gear;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppInterop.Runtime.Runtime;
using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;
using ZombieTweak2;
using static Player.PlayerBotActionAttack;
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
                    //Check if any other bots are giving the same target the same resource
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
        //WTF is this?  Just a debug thing?
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

        // Determine target: either the agent's GameObject or a preset target
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
            // If destination already matches target, no update needed
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
        if (targetObject != null && targetObject.transform != null && ZiMain.isManualAction(__instance.m_descBase))
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

        // 2) Prepare variables used for target selection & strike decision
        Transform aimTransform = null;
        float vulnerableScore = 0.0f;
        bool strike = false;

        // 3) Preserve previous TargetGameObject if we recently selected a target
        GameObject preservedTarget = null;
        if (__instance.m_meleeAction != null && __instance.m_meleeAction.TargetGameObject != null)
        {
            // If still within target re-selection delay, prefer the existing target object
            if (Time.time - __instance.m_targetSelectedTime < PlayerBotActionAttack.s_targetReselectionDelay)
            {
                preservedTarget = __instance.m_meleeAction.TargetGameObject;
            }
        }

        // 4) Choose aimTransform and compute strike boolean
        if (push)
        {
            // push branch uses EasyAimTarget on the target agent
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
            // non-push: find a vulnerable target, passing preservedTarget (may be null)
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
            //descriptor.EventDelegate = __instance.OnMeleeActionEvent;

            // New fields present in IL2CPP decomp
            descriptor.Push = push;
            descriptor.Loop = !push;
            descriptor.Travel = (stance == PlayerBotActionAttack.StanceEnum.Engage /* == 2 in decomp */);

            // Values observed in the decomp
            descriptor.Haste = __instance.m_desc.Haste;
            descriptor.Force = 0.75f;
            descriptor.Strike = strike;


            // Set target agent and (optionally) target game object
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
                if (ZiMain.isManualAction(descriptor))
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

        // Update target agent
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
} // zombifiedpatches

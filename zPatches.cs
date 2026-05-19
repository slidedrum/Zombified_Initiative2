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
using ZombieTweak2.Menus;
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
    //        instance.SetState(PlayerBotActionHighlight.State.NeedsToMove);
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
    


    [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.UpdateActionCollectItem))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)] //Needed for betterbots compat
    public static bool UpdateActionCollectItem(RootPlayerBotAction __instance, ref PlayerBotActionBase.Descriptor bestAction)
    {
        bool allowed = (bool)zSlideComputer.ActionPermissions.ValueAt("Pickup");
        if (!allowed)
            return false;
        return true;
    }
    [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.UpdateActionUnlock))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)] //Needed for betterbots compat
    public static bool UpdateActionUnlock(RootPlayerBotAction __instance, ref PlayerBotActionBase.Descriptor bestAction)
    {
        if (!(bool)zSlideComputer.ActionPermissions.ValueAt("Unlock"))
            return false;
        return true;
    }
    [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.UpdateActionHighlight))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)] //Needed for betterbots compat
    public static bool UpdateActionPing(RootPlayerBotAction __instance, ref PlayerBotActionBase.Descriptor bestAction)
    {
        if (!(bool)zSlideComputer.ActionPermissions.ValueAt("Ping"))
            return false;
        return true;
    }

    [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.UpdateActionTagEnemies))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)] //Needed for betterbots compat
    public static bool UpdateActionTagEnemies(RootPlayerBotAction __instance, ref PlayerBotActionBase.Descriptor bestAction)
    {
        if (!(bool)zSlideComputer.ActionPermissions.ValueAt("Use BioTracker"))
            return false;
        return true;
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
        bool allowedPickup = (bool)zSlideComputer.ActionPermissions.ValueAt("Pickup");
        bool allowedShare = (bool)zSlideComputer.ActionPermissions.ValueAt("Share");
        bool allowedMove = (bool)zSlideComputer.ActionPermissions.ValueAt("Move");
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
        //Why am I hooking into this??

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
        //Why am I hooking into this?? What did I change??  Was this just for debugging?  I don't remember!

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



    



    
} // zombifiedpatches

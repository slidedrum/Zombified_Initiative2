using BotControl.Networking;
using GTFO.API;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Player;
using SNetwork;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static BotControl.Networking.pStructs;

namespace BotControl.Patches
{
    [HarmonyPatch]
    public static class ThrowItemPatch
    {
        // goal, make bot move to the commanders position BEFORE throwing an item.
        // We have two different ways to store a position in this action,
        // Vector3 TargetPosition and Transform TargetObject
        // depending on the PlayerBotActionThrowItem.TargetTypeEnum TargetType, one of them is always unused.
        // So assuming we set it up correctly in our custom call to create this action
        // we can use the unused variatn to store the commanders position.
        // and hook into FindPositionWithView to set the position the bot moves to.
        // UpdateStateTravel also seems to start the throw as soon as at has sight of the target, we'll need to change that.
        public static Dictionary<int, PlayerBotActionThrowItem.Descriptor> throwDescriptions = new();
        public static readonly Dictionary<pStructs.pThrowType, string> ThrowMappings = new()
        {
            { pStructs.pThrowType.FogRepeller, "Fog Repeller" },
            { pStructs.pThrowType.cFoam, "C-Foam Grenade" },
            { pStructs.pThrowType.Glowstick, "Glow Stick" }
        };

        public static Vector3 GetMovePosition(PlayerBotActionThrowItem __instance)
        {
            Vector3 MovePosition = __instance.m_bot.transform.position;
            var TargetType = __instance.m_desc.TargetType;
            switch (TargetType)
            {
                case PlayerBotActionThrowItem.TargetTypeEnum.Position:
                    MovePosition = __instance.m_desc.TargetObject.position;
                    break;
                case PlayerBotActionThrowItem.TargetTypeEnum.Object:
                    MovePosition = __instance.m_desc.TargetPosition;
                    break;
            }
            return MovePosition;
        }
        public static Vector3 GetTargetPosition(PlayerBotActionThrowItem __instance)
        {
            Vector3 targetPosition = __instance.m_bot.transform.position;
            switch (__instance.m_desc.TargetType)
            {
                case PlayerBotActionThrowItem.TargetTypeEnum.None:
                    break;

                case PlayerBotActionThrowItem.TargetTypeEnum.Position:
                    targetPosition = __instance.m_desc.TargetPosition;
                    break;

                case PlayerBotActionThrowItem.TargetTypeEnum.Object:
                    if (__instance.m_desc.TargetObject == null)
                        throw new NullReferenceException();
                    targetPosition = __instance.m_desc.TargetObject.position;
                    break;
            }
            return targetPosition;
        }
        [HarmonyPatch(typeof(PlayerBotActionThrowItem), nameof(PlayerBotActionThrowItem.VerifyCurrentPosition))]
        [HarmonyPrefix]
        public static bool PreVerifyCurrentPosition(PlayerBotActionThrowItem __instance, ref bool __result)
        {
            var MovePosition = GetMovePosition(__instance);
            if (MovePosition != __instance.m_bot.transform.position)
            {
                if (__instance.CheckPositionHasView(MovePosition, __instance.GetTargetPosition(), 0.9f))
                {
                    __result = false;
                    return false;
                }
            }
            //__result = __instance.CheckPositionHasView(__instance.m_agent.Position, __instance.GetTargetPosition(), 0.7225f);
            return true;
        }

        [HarmonyPatch(typeof(PlayerBotActionThrowItem), nameof(PlayerBotActionThrowItem.MoveOut))]
        [HarmonyPrefix]
        public static bool PreMoveOut(PlayerBotActionThrowItem __instance, Vector3 travelPosition)
        {
            return true;
        }
        [HarmonyPatch(typeof(PlayerBotActionThrowItem), nameof(PlayerBotActionThrowItem.FindPositionWithView))]
        [HarmonyPrefix]
        public static bool PreFindPositionWithView(PlayerBotActionThrowItem __instance, Vector3 currentPosition, Vector3 targetPosition, ref Vector3 resultPosition, ref bool __result)
        {
            resultPosition = currentPosition;
            __instance.m_state = PlayerBotActionThrowItem.State.Travel;
            var TargetType = __instance.m_desc.TargetType;
            Vector3 MovePosition = GetMovePosition(__instance);
            if (__instance.CheckPositionHasView(MovePosition, targetPosition, 0.7225f))
            { // Does MovePosition have view of target? 
                resultPosition = MovePosition;
                __result = true; // return true, it was sucsessfull.
                return false;
            }
            // move position does not have a view, fall back to original method.
            return true;
        }
        [HarmonyPatch(typeof(PlayerBotActionThrowItem), nameof(PlayerBotActionThrowItem.FindPositionWithView))]
        [HarmonyPostfix]
        public static void PostFindPositionWithView(PlayerBotActionThrowItem __instance, Vector3 currentPosition, Vector3 targetPosition, ref Vector3 resultPosition, ref bool __result)
        {
            ZiMain.log.LogDebug($"{resultPosition}");
            ZiMain.log.LogDebug($"{__result}");
        }

        [HarmonyPatch(typeof(PlayerBotActionThrowItem), nameof(PlayerBotActionThrowItem.UpdateStateTravel))]
        [HarmonyPrefix]
        public static bool PreUpdateStateTravel(PlayerBotActionThrowItem __instance)
        {// this method is still in the game, but it never actually triggers.  Fucking why (╯°□°)╯( ┻━┻
            __instance.UpdateEquipAction();
            Vector3 MovePosition = GetMovePosition(__instance);
            if (__instance.CheckPositionHasView(MovePosition, __instance.GetTargetPosition(), 0.9f)) // if the move positon can see the target, then let the bot move to the move position.
                if (!__instance.m_travelAction.IsTerminated()) // if the bot has made it to the move position, start the throw.
                    return false;
            return true;
        }

        [HarmonyPatch(typeof(PlayerAIBot), nameof(PlayerAIBot.StartAction))]
        [HarmonyPrefix]
        public static bool PreStartAction(PlayerAIBot __instance, PlayerBotActionBase.Descriptor desc)
        {
            var charId = __instance.Agent.CharacterID;
            var @throw = Il2CppType.Of<PlayerBotActionThrowItem.Descriptor>().FullName;
            var travel = Il2CppType.Of<PlayerBotActionTravel.Descriptor>().FullName;
            if (desc.GetIl2CppType().FullName == @throw) // we are getting the travel action sent here, but idk the destination.
            {
                var throwDesc = desc.Cast<PlayerBotActionThrowItem.Descriptor>();
                throwDescriptions[charId] = throwDesc;
            }
            else if (desc.GetIl2CppType().FullName == travel)
            {
                var traveldesc = desc.Cast<PlayerBotActionTravel.Descriptor>();
                if (traveldesc.ParentActionBase.GetIl2CppType().FullName == "Player.PlayerBotActionThrowItem")
                {
                    traveldesc.DestinationPos = GetMovePosition(traveldesc.ParentActionBase.Cast<PlayerBotActionThrowItem>());
                }
                else if (throwDescriptions.ContainsKey(charId) && throwDescriptions[charId] != null && !throwDescriptions[charId].IsTerminated())
                {
                    traveldesc.ParentActionBase.SafeStopAction(traveldesc);
                }
            }
            return true;
        }
        private static void DebugSimulateNetworkThrow()
        {
            var botlist = ZiMain.GetBotList();
            pThrowType throwType = 0;
            PlayerAgent targetAgent = null;
            Vector3 targetPosition;
            foreach (var bot in botlist)
            {
                var backpack = bot.Backpack;
                backpack.TryGetBackpackItem(InventorySlot.Consumable, out BackpackItem item);
                if (item == null)
                    continue;
                if (ThrowMappings.ContainsValue(item.Name))
                {
                    foreach(var type in ThrowMappings.Keys)
                    {
                        if (ThrowMappings[type] == item.Name)
                        {
                            throwType = type;
                            break;
                        }
                    }
                    targetAgent = bot.Agent;
                    break;
                }
            }
            if (targetAgent == null)
                return;
            targetPosition = zStaticRefrences.LocalPlayer.FPSCamera.CameraRayPos;
            pStructs.pThrowDataInfo info = new()
            {
                Commander = pStructs.Get_pStructFromRefrence(zStaticRefrences.LocalPlayer),
                Agent = pStructs.Get_pStructFromRefrence(targetAgent),
                ThrowType = throwType,
                MovePosition = zStaticRefrences.LocalPlayer.transform.position,
                TargetPosition = targetPosition,
            };
            zNetworking.ReciveRequestToThrowItem(0, info);
        }
        private static void OnButtonThrowItem(pThrowType throwType, PlayerAgent targetAgent)
        {
            Vector3 targetPosition = zStaticRefrences.LocalPlayer.FPSCamera.CameraRayPos;
            if (SNet.IsMaster)
            {
                SendBotToThrowItem(zStaticRefrences.LocalPlayer, targetAgent, throwType, zStaticRefrences.LocalPlayer.transform.position, targetPosition, 0);
            }
            pStructs.pThrowDataInfo info = new()
            {
                Commander = pStructs.Get_pStructFromRefrence(zStaticRefrences.LocalPlayer),
                Agent = pStructs.Get_pStructFromRefrence(targetAgent),
                ThrowType = throwType,
                MovePosition = zStaticRefrences.LocalPlayer.transform.position,
                TargetPosition = targetPosition,
            };
            NetworkAPI.InvokeEvent<pStructs.pThrowDataInfo>("RequestToThrowItem", info);
        }
        [HarmonyPatch(typeof(PUI_CommunicationMenu), nameof(PUI_CommunicationMenu.OnButtonPressedUseFogRepeller))]
        [HarmonyPrefix]
        public static bool PreOnButtonPressedUseFogRepeller(PUI_CommunicationMenu __instance, PUI_CommunicationButton button, PlayerAgent targetAgent, ref bool __result)
        {
            OnButtonThrowItem(pThrowType.FogRepeller, targetAgent);
            return false;
        }
        [HarmonyPatch(typeof(PUI_CommunicationMenu), nameof(PUI_CommunicationMenu.OnButtonPressedUseGlue))]
        [HarmonyPrefix]
        public static bool PreOnButtonPressedUseGlue(PUI_CommunicationMenu __instance, PUI_CommunicationButton button, PlayerAgent targetAgent, ref bool __result)
        {
            OnButtonThrowItem(pThrowType.cFoam, targetAgent);
            return false;
        }
        [HarmonyPatch(typeof(PUI_CommunicationMenu), nameof(PUI_CommunicationMenu.OnButtonPressedThrowGlowStick))]
        [HarmonyPrefix]
        public static bool PreOnButtonPressedThrowGlowStick(PUI_CommunicationMenu __instance, PUI_CommunicationButton button, PlayerAgent targetAgent, ref bool __result)
        {
            OnButtonThrowItem(pThrowType.Glowstick, targetAgent);
            return false;
        }
        public static bool SendBotToThrowItem(PlayerAgent Commander, PlayerAgent botAgent, pStructs.pThrowType ThrowType, Vector3 MovePosition, Vector3 TargetPosition, ulong netSender = 0)
        {
            if (!SNet.Master)
                return false;

            PlayerAIBot aiBot = botAgent.GetComponent<PlayerAIBot>();
            var backpack = aiBot.Backpack;
            backpack.TryGetBackpackItem(InventorySlot.Consumable, out var item);
            if (item == null)
            {
                ZiMain.log.LogWarning($"Wanted to throw {ThrowType} but found nothing.");
                return false;
            }
            if (item.Name != ThrowMappings[ThrowType])
            {
                ZiMain.log.LogWarning($"Invalid throw item to throw.  Wanted to throw {ThrowType} but found {item.Name}");
                return false;
            }

            PlayerBotActionThrowItem.Descriptor desc = new(aiBot)
            {
                Prio = 15f,
                Haste = 0.8f,
                TargetPosition = TargetPosition,
                TargetObject = Commander.transform,
                TargetType = PlayerBotActionThrowItem.TargetTypeEnum.Position,
                Item = item.Instance.Cast<ItemEquippable>(),
                MovementAllowed = true
            };
            aiBot.StartAction(desc);
            return false;
        }
        public static Vector3 _GetAimDirection(PlayerBotActionThrowItem __instance, Vector3 fromPos, ref float forceRel, bool straightShot, out float range)
        {
            if (__instance.m_desc == null)
                throw new NullReferenceException();

            Vector3 targetPos;
            switch (__instance.m_desc.TargetType)
            {
                case PlayerBotActionThrowItem.TargetTypeEnum.Position:
                    targetPos = __instance.m_desc.TargetPosition;
                    break;

                case PlayerBotActionThrowItem.TargetTypeEnum.Object:
                    if (__instance.m_desc.TargetObject == null)
                        throw new NullReferenceException();

                    targetPos = __instance.m_desc.TargetObject.position;
                    break;

                default:
                    if (__instance.m_agent == null)
                        throw new NullReferenceException();

                    targetPos = __instance.m_agent.Position;
                    break;
            }

            float horizontalDx = targetPos.x - fromPos.x;
            float horizontalDz = targetPos.z - fromPos.z;
            float verticalDelta = targetPos.y - fromPos.y;

            Vector2 horizontalDelta = new Vector2(horizontalDx, horizontalDz);
            float horizontalDistance = horizontalDelta.magnitude;
            float gravityMagnitude = Physics.gravity.magnitude;

            if (horizontalDistance == 0f)
            {
                range = 0f;
                return verticalDelta <= 0f ? Vector3.down : Vector3.up;
            }

            float launchSpeed = forceRel * PlayerBotActionThrowItem.s_throwVelocity;
            Vector2 horizontalDir = horizontalDelta / horizontalDistance;

            float gravityTerm =
                (-gravityMagnitude * 0.5f * horizontalDistance * horizontalDistance) /
                (launchSpeed * launchSpeed);

            float discriminant =
                horizontalDistance * horizontalDistance -
                (gravityTerm - verticalDelta) * gravityTerm * 4f;

            if (discriminant < 0f)
            {
                range = 1f;

                float pitchFactor;
                if (verticalDelta <= 0f)
                {
                    pitchFactor = 1f;
                }
                else
                {
                    Vector3 toTarget = (targetPos - fromPos).normalized;
                    pitchFactor = Mathf.Tan((toTarget.y + 1f) * 0.7853982f);
                }

                float invLen = 1f / Mathf.Sqrt(pitchFactor * pitchFactor + 1f);

                return new Vector3(
                    horizontalDir.x * invLen,
                    pitchFactor * invLen,
                    horizontalDir.y * invLen);
            }

            float sqrtDiscriminant = Mathf.Sqrt(discriminant);
            float denom = gravityTerm + gravityTerm;

            float lowArc = (-horizontalDistance - sqrtDiscriminant) / denom;
            float highArc = (-horizontalDistance + sqrtDiscriminant) / denom;

            float chosenArc = straightShot ? Mathf.Min(highArc, lowArc) : Mathf.Max(highArc, lowArc);
            range = 1f / Mathf.Max(highArc, lowArc);

            if (chosenArc > 2f && !straightShot)
            {
                float reducedForceRel = forceRel * 0.75f;
                Vector3 fallbackDir = _GetAimDirection(__instance, fromPos, ref reducedForceRel, false, out float fallbackRange);

                if (fallbackRange < 1f)
                {
                    forceRel = reducedForceRel;
                    return fallbackDir;
                }
            }

            float invDirectionLen = 1f / Mathf.Sqrt(chosenArc * chosenArc + 1f);

            return new Vector3(
                horizontalDir.x * invDirectionLen,
                chosenArc * invDirectionLen,
                horizontalDir.y * invDirectionLen);
        }

        //[HarmonyPatch(typeof(PlayerBotActionThrowItem), nameof(PlayerBotActionThrowItem.GetAimDirection))]
        //[HarmonyPrefix]
        //public static bool PreGetAimDirection()
        //public static bool PreGetAimDirection(PlayerBotActionThrowItem __instance,Vector3 fromPos, ref float forceRel, bool straightShot, ref float range, ref Vector3 __result)
        //{
        //    __result = _GetAimDirection(__instance, fromPos, ref forceRel, straightShot, out range);
        //    return false;
        //}
    }
}

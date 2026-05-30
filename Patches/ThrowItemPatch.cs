using HarmonyLib;
using Il2CppInterop.Runtime;
using Player;
using System;
using UnityEngine;

namespace BotControl.Patches
{
    [HarmonyPatch]
    public class ThrowItemPatch
    {
        // goal, make bot move to the commanders position BEFORE throwing an item.
        // We have two different ways to store a position in this action,
        // Vector3 TargetPosition and Transform TargetObject
        // depending on the PlayerBotActionThrowItem.TargetTypeEnum TargetType, one of them is always unused.
        // So assuming we set it up correctly in our custom call to create this action
        // we can use the unused variatn to store the commanders position.
        // and hook into FindPositionWithView to set the position the bot moves to.
        // UpdateStateTravel also seems to start the throw as soon as at has sight of the target, we'll need to change that.
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
        public static PlayerBotActionThrowItem.Descriptor throwDescription;
        [HarmonyPatch(typeof(PlayerAIBot), nameof(PlayerAIBot.StartAction))]
        [HarmonyPrefix]
        public static bool PreStartAction(PlayerAIBot __instance, PlayerBotActionBase.Descriptor desc)
        {
            var @throw = Il2CppType.Of<PlayerBotActionThrowItem.Descriptor>().FullName;
            var travel = Il2CppType.Of<PlayerBotActionTravel.Descriptor>().FullName;
            if (desc.GetIl2CppType().FullName == @throw) // we are getting the travel action sent here, but idk the destination.
            {
                var throwDesc = desc.Cast<PlayerBotActionThrowItem.Descriptor>();
                if (throwDesc.TargetType == PlayerBotActionThrowItem.TargetTypeEnum.Position)
                {
                    throwDesc.TargetObject = zStaticRefrences.LocalPlayer.transform;
                }
                if (throwDesc.TargetType == PlayerBotActionThrowItem.TargetTypeEnum.Object)
                {
                    throwDesc.TargetPosition = zStaticRefrences.LocalPlayer.transform.position;
                }
                throwDescription = throwDesc;
            }
            else if (desc.GetIl2CppType().FullName == travel)
            {
                var traveldesc = desc.Cast<PlayerBotActionTravel.Descriptor>();
                if (traveldesc.ParentActionBase.GetIl2CppType().FullName == "Player.PlayerBotActionThrowItem")
                {
                    traveldesc.DestinationPos = GetMovePosition(traveldesc.ParentActionBase.Cast<PlayerBotActionThrowItem>());
                }
                else if (throwDescription != null && !throwDescription.IsTerminated())
                {
                    traveldesc.ParentActionBase.SafeStopAction(traveldesc);
                }
            }
            return true;
        }
    }
}

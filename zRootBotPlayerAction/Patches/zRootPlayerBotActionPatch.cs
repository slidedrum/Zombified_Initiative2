using BetterBots.Utils;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Player;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using ZombieTweak2.zRootBotPlayerAction.BaseActionWrappers;
using Zombified_Initiative;

namespace ZombieTweak2.zRootBotPlayerAction.Patches
{
    [HarmonyPatch]
    public class RootPlayerBotActionPatch
    {
        [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.Update))]
        [HarmonyPrefix]
        public static bool PreUpdate(RootPlayerBotAction __instance, ref bool __result)
        {
            var data = zActions.GetOrCreateData(__instance);
            data.bestAction = null;
            return true;
        }
        [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.Update))]
        [HarmonyPostfix]
        public static void PostUpdate(RootPlayerBotAction __instance, ref bool __result)
        {
            var data = zActions.GetOrCreateData(__instance);
            foreach (var act in data.customActions)
            {
                act.compareAction(ref data.bestAction);
            }
            if (data.bestAction != null)
            {
                data.my_actions.Add(data.bestAction);
                __instance.m_bot.StartAction(data.bestAction);
            }
        }
        [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.StartAction))]
        [HarmonyPrefix]
        public static bool StartAction(RootPlayerBotAction __instance, PlayerBotActionBase.Descriptor actionDesc)
        {
            ZiMain.log.LogInfo($"Would have started action {actionDesc.GetIl2CppType()}");
            return false;
            //this.m_bot.StartAction(actionDesc);
            //This is still in the game but completely unused for some fucking reason.
        }
        [HarmonyPatch(typeof(PlayerBotActionFollow), nameof(PlayerBotActionFollow.Update))]
        [HarmonyPrefix]
        public static bool Update(PlayerBotActionFollow __instance, bool __result)
        {
            if (!__instance.IsActive())
            {
                __result = true;
                return false;
            }
            if (!__instance.VerifyClient())
            {
                __instance.m_desc.SetCompletionStatus(PlayerBotActionBase.Descriptor.StatusType.Failed);
                __result = true;
                return false;
            }
            PlayerBotActionFollow.State state = __instance.m_state;
            if (state != PlayerBotActionFollow.State.Idle)
            {
                if (state == PlayerBotActionFollow.State.Move)
                {
                    __instance.UpdateStateMove();
                }
            }
            else
            {
                __instance.UpdateStateIdle();
            }
            __result = !__instance.IsActive();
            return false;
        }
    }
}
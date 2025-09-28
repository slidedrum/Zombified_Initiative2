using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Security.Cryptography;
using Player;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using ZombieTweak2.zRootBotPlayerAction.BaseActionWrappers;
using ZombieTweak2.zRootBotPlayerAction.CustomActions;
using Zombified_Initiative;
using static Il2CppSystem.Globalization.CultureInfo;

namespace ZombieTweak2.zRootBotPlayerAction
{
    [HarmonyPatch]
    public static class RootActionRewritePatch
    {
        internal static readonly Dictionary<int, dataStore> RootActionDataStore = new();


        [HarmonyPatch(typeof(RootPlayerBotAction.Descriptor), nameof(RootPlayerBotAction.Descriptor.CreateAction))]
        [HarmonyPrefix]
        public static bool CreateAction(RootPlayerBotAction.Descriptor __instance, ref PlayerBotActionBase __result)
        {
            // Can't hook the constructor directly for some reason, so hook the method that calls the constructor and call my wrapper instead.
            // Might flip back to hooking the constructor if I can figure out how.
            __result = new zRootPlayerBotAction(__instance);
            return false;
        }
        public static bool BaseUpdate(RootPlayerBotAction __instance)
        {
            return !__instance.IsActive(); //this is really dumb but this is what the base game does. /shrug
        }
        [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.Update))]
        [HarmonyPrefix]
        public static bool Update(RootPlayerBotAction __instance, ref bool __result)
        {
            if (!ZiMain.newRootBotPlayerAction)
                return true;
            var data = zActions.GetOrCreateData(__instance);
            if (BaseUpdate(__instance))
            {
                __result = true;
                return false;
            }
            __instance.RefreshGearAvailability();
            PlayerBotActionBase.Descriptor bestAction = null;
            foreach (ICustomPlayerBotActionBase.IDescriptor actionDesc in data.allActions)
            {
                actionDesc.compareAction(__instance, ref bestAction);
            }
            if (bestAction != null)
            {
                __instance.StartAction(bestAction);
            }
            __result = !__instance.IsActive();
            return false;
        }
        [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.Stop))]
        [HarmonyPrefix]
        public static bool Stop(RootPlayerBotAction __instance)
        {
            var data = zActions.GetOrCreateData(__instance);
            foreach (var actionDesc in data.allActions)
            {
                __instance.SafeStopAction((PlayerBotActionBase.Descriptor)actionDesc);
            }
            return false;
        }
        public static void Z_StartAction(PlayerAIBot aiBot, CustomBotAction.Descriptor desc)
        {
            if (!desc.IsTerminated())
            {
                ZiMain.log.LogError("Action was queued while active: " + desc);
            }
            for (int i = 0; i < aiBot.m_actions.Count; i++)
            {
                if (aiBot.m_actions[i].DescBase == desc)
                {
                    aiBot.m_actions.RemoveAt(i);
                    break;
                }
            }
            desc.OnQueued();
            aiBot.RemoveCollidingActions(desc);
            aiBot.m_queuedActions.Add(desc);
        }

        [HarmonyPatch(typeof(PlayerAIBot), nameof(PlayerAIBot.StartQueuedActions))]
        [HarmonyPrefix]
        private static bool StartQueuedActions(PlayerAIBot __instance)
        {
            var data = zActions.GetOrCreateData(__instance);
            if (__instance.m_queuedActions.Count == 0)
            {
                return false;
            }
            var array = new Il2CppReferenceArray<PlayerBotActionBase.Descriptor>(__instance.m_queuedActions.Count);
            __instance.m_queuedActions.CopyTo(array);
            __instance.m_queuedActions.Clear();
            foreach (PlayerBotActionBase.Descriptor descriptor in array)
            {
                if (descriptor.Status == PlayerBotActionBase.Descriptor.StatusType.Queued)
                {
                    var strictDesc = zActions.GetStrictTypeInstance(descriptor);
                    if (strictDesc != null)
                    {
                        strictDesc.OnStarted();
                        PlayerBotActionBase playerBotActionBase = strictDesc.CreateAction(); //ERROR
                        __instance.RemoveCollidingActions((PlayerBotActionBase.Descriptor)strictDesc);
                        __instance.m_actions.Add(playerBotActionBase);
                    }
                    else
                    {
                        descriptor.OnStarted();
                        PlayerBotActionBase playerBotActionBase = descriptor.CreateAction(); //ERROR
                        __instance.RemoveCollidingActions(descriptor);
                        __instance.m_actions.Add(playerBotActionBase);
                    }
                }
            }
            return false;
        }
        [HarmonyPatch(typeof(PlayerBotActionBase.Descriptor), nameof(PlayerBotActionBase.Descriptor.CreateAction))]
        [HarmonyPostfix]
        static void CreateAction(PlayerBotActionBase.Descriptor __instance, ref PlayerBotActionBase __result)
        {
            if (__result is ICustomPlayerBotActionBase)
            {
                zActions.RegisterStrictTypeInstance(__result);
            }
        }
    }
}

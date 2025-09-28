using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Player;
using System.Collections.Generic;
using ZombieTweak2.zRootBotPlayerAction.BaseActionWrappers;
using Zombified_Initiative;

namespace ZombieTweak2.zRootBotPlayerAction
{
    [HarmonyPatch]
    public class RootActionRewritePatch
    {
        [HarmonyPatch(typeof(RootPlayerBotAction.Descriptor), nameof(RootPlayerBotAction.Descriptor.CreateAction))]
        [HarmonyPrefix]
        public static bool CreateAction(RootPlayerBotAction.Descriptor __instance,ref PlayerBotActionBase __result)
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
        [HarmonyPatch(typeof(PlayerAIBot), nameof(PlayerAIBot.StartQueuedActions))]
        [HarmonyPrefix]
        private static bool StartQueuedActions(PlayerAIBot __instance)
        {
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
                    var hasStrictDesc = zActions.GetStrictTypeInstance(descriptor);
                    if (hasStrictDesc is ICustomPlayerBotActionBase.IDescriptor strictDesc)
                    {
                        strictDesc.OnStarted();
                        PlayerBotActionBase playerBotActionBase = zActions.RegisterStrictTypeInstance(strictDesc.CreateAction());
                        __instance.RemoveCollidingActions((PlayerBotActionBase.Descriptor)strictDesc);
                        __instance.m_actions.Add(playerBotActionBase);
                    }
                    else
                    {
                        descriptor.OnStarted();
                        PlayerBotActionBase playerBotActionBase = descriptor.CreateAction();
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
        [HarmonyPatch(typeof(PlayerAIBot), nameof(PlayerAIBot.Update))]
        [HarmonyPrefix]
        public static bool Update(PlayerAIBot __instance)
        {
            __instance.InitValues();
            __instance.SleeperCheck();
            __instance.UpdateActions();
            __instance.StartQueuedActions();
            __instance.ApplyValues();
            return false;
        }
        [HarmonyPatch(typeof(PlayerAIBot), nameof(PlayerAIBot.UpdateActions))]
        [HarmonyPrefix]
        private static bool UpdateActions(PlayerAIBot __instance)
        {
            if (__instance.m_actions.Count == 0)
            {
                return false;
            }
            //PlayerBotActionBase[] array = new PlayerBotActionBase[__instance.m_actions.Count];
            var array = new Il2CppReferenceArray<PlayerBotActionBase>(__instance.m_actions.Count);
            __instance.m_actions.CopyTo(array, 0);
            for (int i = 0; i < array.Length; i++)
            {
                PlayerBotActionBase playerBotActionBase = array[i];
                PlayerAIBot.s_updatingAction = playerBotActionBase.DescBase;
                if (!playerBotActionBase.IsActive() || playerBotActionBase.Update())
                { //Has the action completed?
                    int num = i;
                    if (num >= __instance.m_actions.Count || __instance.m_actions[num] != playerBotActionBase)
                    { //Find the instance to remove it at
                        bool flag = false;
                        for (int j = 0; j < __instance.m_actions.Count; j++)
                        {
                            if (__instance.m_actions[j].Pointer == playerBotActionBase.Pointer) //this is failing as a direct comparison
                            {
                                flag = true;
                                num = j;
                                break;
                            }
                        }
                        if (!flag)
                        {
                            continue;
                        }
                    }
                    PlayerAIBot.s_updatingAction = null;
                    __instance.m_actions.RemoveAt(num);
                    playerBotActionBase.DescBase.OnExpired();
                    playerBotActionBase.Stop();
                }
            }
            PlayerAIBot.s_updatingAction = null;
            return false;
        }
    }
}

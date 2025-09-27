using HarmonyLib;
using Player;
using System.Collections.Generic;
using TMPro;
using ZombieTweak2.zRootBotPlayerAction.BaseActionWrappers;
using Zombified_Initiative;
using static Il2CppSystem.Globalization.CultureInfo;

namespace ZombieTweak2.zRootBotPlayerAction
{
    public class RootPlayerBotActionData
    {
        public OrderedSet<ICustomPlayerBotActionBase.IDescriptor> allActions = new();
    }
    [HarmonyPatch]
    public static class RootActionRewritePatch
    {
        internal static readonly Dictionary<int, RootPlayerBotActionData> RootActionDataStore = new();
        internal static RootPlayerBotActionData GetOrCreateData(PlayerBotActionBase botBase)
        {
            int botId = botBase.m_bot.GetInstanceID();
            if (!RootActionDataStore.TryGetValue(botId, out var data))
            {
                data = new RootPlayerBotActionData();
                RootActionDataStore[botId] = data;
            }
            return data;
        }

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
            var data = GetOrCreateData(__instance);
            if (BaseUpdate(__instance))
            {
                __result = true;
                return false;
            }
            __instance.RefreshGearAvailability();
            PlayerBotActionBase.Descriptor bestAction = null;
            foreach (var actionDesc in data.allActions)
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
            var data = GetOrCreateData(__instance);
            foreach (var actionDesc in data.allActions)
            {
                __instance.SafeStopAction((PlayerBotActionBase.Descriptor)actionDesc);
            }
            return false;
        }
    }
}

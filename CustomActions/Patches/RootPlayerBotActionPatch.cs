using AIGraph;
using HarmonyLib;
using Il2CppMono.Security.Interface;
using Player;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using UnityEngine;
using ZombieTweak2.zRootBotPlayerAction.CustomActions;
using static Il2CppSystem.Globalization.CultureInfo;

namespace ZombieTweak2.zRootBotPlayerAction.Patches
{
    [HarmonyPatch]
    public static class RootPlayerBotActionPatch
    {

        /* TODO fix this.  This is supposed to force the center position to be agent position if it's coming from downstream of UpdateActionCollectItem. 
        [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.UpdateActionCollectItem))]
        [HarmonyPrefix]
        public static void PreUpdateActionCollectItem(RootPlayerBotAction __instance)
        {
            var data = zActions.GetOrCreateData(__instance);
            data.consideringCollectItem = true;
        }
        [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.UpdateActionCollectItem))]
        [HarmonyPostfix]
        public static void PostUpdateActionCollectItem(RootPlayerBotAction __instance)
        {
            var data = zActions.GetOrCreateData(__instance);
            data.consideringCollectItem = false;
        }F
        [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.GetActivityEpicenter))]
        [HarmonyPostfix]
        public static void PostGetActivityEpicenter(RootPlayerBotAction __instance, ref AIG_CourseNode courseNode, ref Vector3 centerPosition, ref bool __result)
        {
            var data = zActions.GetOrCreateData(__instance);
            if (!data.consideringCollectItem)
                return;
            centerPosition = __instance.m_bot.transform.position;
            courseNode = __instance.m_bot.Agent.CourseNode;
            __result = courseNode != null;
        }*/
        [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.Update))]
        [HarmonyPrefix]
        public static bool PreUpdate(RootPlayerBotAction __instance, ref bool __result)
        {
            //Reset local best action to null
            var data = zActions.GetOrCreateData(__instance);
            data.bestAction = null;
            return true;
        }
        [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.StartAction))]
        [HarmonyPrefix]
        public static bool StartActionPatch(RootPlayerBotAction __instance, PlayerBotActionBase.Descriptor actionDesc)
        {
            //Set local best action to vanilla action
            var data = zActions.GetOrCreateData(__instance);
            data.bestAction = actionDesc;
            return false;
        }
        [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.Update))]
        [HarmonyPostfix]
        public static void PostUpdate(RootPlayerBotAction __instance, ref bool __result)
        {
            //After running the vanilla compareisons we need to run our custom action comparisons
            //against the vanilla best action.
            var data = zActions.GetOrCreateData(__instance);
            foreach (var act in data.customActions)
            {
                act.compareAction(ref data.bestAction);
            }
            if (data.bestAction != null)
            {
                __instance.m_bot.StartAction(data.bestAction);
            }
            __result = !__instance.IsActive();
        }
    }
}
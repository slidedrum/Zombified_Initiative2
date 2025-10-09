using AIGraph;
using HarmonyLib;
using Il2CppMono.Security.Interface;
using Player;
using System.Diagnostics.Metrics;
using UnityEngine;
using ZombieTweak2.zRootBotPlayerAction.CustomActions;

namespace ZombieTweak2.zRootBotPlayerAction.Patches
{
    [HarmonyPatch]
    public class RootPlayerBotActionPatch
    {
        [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.Update))]
        [HarmonyPrefix]
        public static bool PreUpdate(RootPlayerBotAction __instance, ref bool __result)
        {
            //We need to reset the best action watcher before we start calling vanilla actions.
            var data = zActions.GetOrCreateData(__instance);
            data.consideringActions = true;
            data.bestAction = null;
            switch (DramaManager.CurrentStateEnum)
            {
                case DRAMA_State.Exploration:
                    {
                        __instance.m_followLeaderAction.Prio = 1;
                        RootPlayerBotAction.m_prioSettings.FollowLeaderRadius = 15;
                        RootPlayerBotAction.s_followLeaderRadius = 15;
                        RootPlayerBotAction.s_followLeaderMaxDistance = 30;
                        break;
                    }
                case DRAMA_State.Alert:
                    {
                        __instance.m_followLeaderAction.Prio = 5;
                        RootPlayerBotAction.m_prioSettings.FollowLeaderRadius = 10;
                        RootPlayerBotAction.s_followLeaderRadius = 10;
                        RootPlayerBotAction.s_followLeaderMaxDistance = 30;
                        break;
                    }
                case DRAMA_State.Sneaking:
                    {
                        __instance.m_followLeaderAction.Prio = 2;
                        RootPlayerBotAction.m_prioSettings.FollowLeaderRadius = 5;
                        RootPlayerBotAction.s_followLeaderRadius = 5;
                        RootPlayerBotAction.s_followLeaderMaxDistance = 30;

                        break;
                    }
                case DRAMA_State.Encounter:
                    {
                        __instance.m_followLeaderAction.Prio = 7;
                        RootPlayerBotAction.m_prioSettings.FollowLeaderRadius = 4;
                        RootPlayerBotAction.s_followLeaderRadius = 4;
                        RootPlayerBotAction.s_followLeaderMaxDistance = 5;
                        break;
                    }
                case DRAMA_State.Combat:
                    {
                        __instance.m_followLeaderAction.Prio = 14;
                        RootPlayerBotAction.m_prioSettings.FollowLeaderRadius = 7;
                        RootPlayerBotAction.s_followLeaderRadius = 7;
                        RootPlayerBotAction.s_followLeaderMaxDistance = 10;
                        break;
                    }
                case DRAMA_State.Survival:
                    {
                        __instance.m_followLeaderAction.Prio = 14;
                        RootPlayerBotAction.m_prioSettings.FollowLeaderRadius = 7;
                        RootPlayerBotAction.s_followLeaderRadius = 7;
                        RootPlayerBotAction.s_followLeaderMaxDistance = 10;
                        break;
                    }
                case DRAMA_State.IntentionalCombat:
                    {
                        __instance.m_followLeaderAction.Prio = 14;
                        RootPlayerBotAction.m_prioSettings.FollowLeaderRadius = 7;
                        RootPlayerBotAction.s_followLeaderMaxDistance = 10;
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
            
            
            return true;
        }
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
        }
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
        [HarmonyPostfix]
        public static void PostUpdate(RootPlayerBotAction __instance, ref bool __result)
        {
            //after vanilla actions eval we need to eval custom actions.
            //Whatever vanilla action is best still gets called no matter what, might want to chagne that?  Might not be a problem?
            var data = zActions.GetOrCreateData(__instance);
            data.consideringActions = false;
            var baseAction = data.bestAction;
            foreach (var act in data.customActions)
            {
                act.compareAction(ref data.bestAction);
            }
            if (data.bestAction != null)
            {
                __instance.m_bot.StartAction(data.bestAction);
            }
        }
    }
}
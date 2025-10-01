using HarmonyLib;
using Il2CppMono.Security.Interface;
using Player;
using System.Diagnostics.Metrics;
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
        [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.Update))]
        [HarmonyPostfix]
        public static void PostUpdate(RootPlayerBotAction __instance, ref bool __result)
        {
            //after vanilla actions eval we need to eval custom actions.
            //Whatever vanilla action is best still gets called no matter what, might want to chagne that?  Might not be a problem?
            var data = zActions.GetOrCreateData(__instance);
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
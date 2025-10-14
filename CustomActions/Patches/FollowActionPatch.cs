using HarmonyLib;
using Player;
using ZombieTweak2.zMenu;
using ZombieTweak2.zRootBotPlayerAction;

namespace ZombieTweak2.CustomActions.Patches
{
    [HarmonyPatch]
    public class FollowActionPatch
    {
        public static void Setup()
        {
            //defaultFollowSettings = new();
            //followerSettings = new();
            //followSettingsOverides.Clear();
            //myFollowSettingsOverides.Clear();
            //myFollowSettingsOverides[DRAMA_State.Exploration] = new()
            //{
            //    prio = 1,
            //    followLeaderRadius = 15,
            //    followLeaderMaxDistance = 30,
            //};
            //myFollowSettingsOverides[DRAMA_State.Alert] = new()
            //{
            //    prio = 5,
            //    followLeaderRadius = 10,
            //    followLeaderMaxDistance = 30,
            //};
            //myFollowSettingsOverides[DRAMA_State.Sneaking] = new()
            //{
            //    prio = 2,
            //    followLeaderRadius = 5,
            //    followLeaderMaxDistance = 30,
            //};
            //myFollowSettingsOverides[DRAMA_State.Encounter] = new()
            //{
            //    prio = 7,
            //    followLeaderRadius = 4,
            //    followLeaderMaxDistance = 5,
            //};
            //myFollowSettingsOverides[DRAMA_State.Combat] = new()
            //{
            //    prio = 14,
            //    followLeaderRadius = 7,
            //    followLeaderMaxDistance = 10,
            //};
            //myFollowSettingsOverides[DRAMA_State.Survival] = new()
            //{
            //    prio = 14,
            //    followLeaderRadius = 7,
            //    followLeaderMaxDistance = 10,
            //};
            //myFollowSettingsOverides[DRAMA_State.IntentionalCombat] = new()
            //{
            //    prio = 14,
            //    followLeaderRadius = 7,
            //    followLeaderMaxDistance = 10,
            //};
        }

        [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.Update))]
        [HarmonyPrefix]
        public static bool PreUpdate(RootPlayerBotAction __instance, ref bool __result)
        {
            //We need to reset the best action watcher before we start calling vanilla actions.
            var data = zActions.GetOrCreateData(__instance);
            data.consideringActions = true;
            data.bestAction = null;

            //TODO set up parralell overideTrees for each bot
            //TODO if this gets called every frame, maybe cache the values untill something changes in overide tree
            __instance.         m_followLeaderAction.Prio =         (float)AutomaticActionMenuClass.FollowMenuClass.prio.GetValue();
            RootPlayerBotAction.m_prioSettings.FollowLeaderRadius = (float)AutomaticActionMenuClass.FollowMenuClass.followRadius.GetValue();
            RootPlayerBotAction.s_followLeaderRadius =              (float)AutomaticActionMenuClass.FollowMenuClass.followRadius.GetValue();
            RootPlayerBotAction.s_followLeaderMaxDistance =         (float)AutomaticActionMenuClass.FollowMenuClass.maxDistance.GetValue();
            return true;
        }
    }
}

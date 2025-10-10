using HarmonyLib;
using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZombieTweak2.zRootBotPlayerAction;

namespace ZombieTweak2.CustomActions.Patches
{
    [HarmonyPatch]
    public class FollowActionPatch
    {
        public struct followSetting
        {
            public int prio;
            public int followLeaderRadius;
            public int followLeaderMaxDistance;
            public followSetting()
            {
                prio = 14;
                followLeaderRadius = 7;
                followLeaderMaxDistance = 10;
            }
        }
        public static followSetting defaultFollowSettings;
        public static followSetting mainFollowerSettings;
        public static Dictionary<DRAMA_State, followSetting> followSettingsOverides = new();
        public static Dictionary<DRAMA_State, followSetting> myFollowSettingsOverides = new();
        public static void Setup()
        {
            defaultFollowSettings = new();
            mainFollowerSettings = new();
            followSettingsOverides.Clear();
            myFollowSettingsOverides.Clear();
            myFollowSettingsOverides[DRAMA_State.Exploration] = new()
            {
                prio = 1,
                followLeaderRadius = 15,
                followLeaderMaxDistance = 30,
            };
            myFollowSettingsOverides[DRAMA_State.Alert] = new()
            {
                prio = 5,
                followLeaderRadius = 10,
                followLeaderMaxDistance = 30,
            };
            myFollowSettingsOverides[DRAMA_State.Sneaking] = new()
            {
                prio = 2,
                followLeaderRadius = 5,
                followLeaderMaxDistance = 30,
            };
            myFollowSettingsOverides[DRAMA_State.Encounter] = new()
            {
                prio = 7,
                followLeaderRadius = 4,
                followLeaderMaxDistance = 5,
            };
            myFollowSettingsOverides[DRAMA_State.Combat] = new()
            {
                prio = 14,
                followLeaderRadius = 7,
                followLeaderMaxDistance = 10,
            };
            myFollowSettingsOverides[DRAMA_State.Survival] = new()
            {
                prio = 14,
                followLeaderRadius = 7,
                followLeaderMaxDistance = 10,
            };
            myFollowSettingsOverides[DRAMA_State.IntentionalCombat] = new()
            {
                prio = 14,
                followLeaderRadius = 7,
                followLeaderMaxDistance = 10,
            };
        }

        [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.Update))]
        [HarmonyPrefix]
        public static bool PreUpdate(RootPlayerBotAction __instance, ref bool __result)
        {
            //We need to reset the best action watcher before we start calling vanilla actions.
            var data = zActions.GetOrCreateData(__instance);
            data.consideringActions = true;
            data.bestAction = null;

            var currentDramaState = DramaManager.CurrentStateEnum;
            var followSettings = data.followSettingsOverides;
            var currentFollowSettings = followSettings.GetValueOrDefault(currentDramaState, mainFollowerSettings);
            __instance.m_followLeaderAction.Prio = currentFollowSettings.prio;
            RootPlayerBotAction.m_prioSettings.FollowLeaderRadius = currentFollowSettings.followLeaderRadius;
            RootPlayerBotAction.s_followLeaderRadius = currentFollowSettings.followLeaderRadius;
            RootPlayerBotAction.s_followLeaderMaxDistance = currentFollowSettings.followLeaderMaxDistance;
            return true;
        }
    }
}

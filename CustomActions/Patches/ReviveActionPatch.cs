using HarmonyLib;
using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZombieTweak2.CustomActions.Patches
{
    [HarmonyPatch]
    internal static class ReviveActionPatch
    {
        public static bool allowedToReviveBots = true;
        public static Dictionary<int,bool?> allowedToReviveOverrides;

        [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.UpdateActionReviveTeammate))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)] //Needed for betterbots compat
        public static bool UpdateActionReviveTeammatePrePatch(RootPlayerBotAction __instance, ref PlayerBotActionBase.Descriptor bestAction)
        { //re-implementation of this method with added checks.
            if (!__instance.m_reviveAction.IsTerminated())
            {
                return false;
            }
            __instance.m_reviveAction.Prio = RootPlayerBotAction.m_prioSettings.Revive;
            if (!RootPlayerBotAction.CompareActionPrios(__instance.m_reviveAction, bestAction))
            {
                return false;
            }
            if (__instance.m_bot.IsActionForbidden(__instance.m_reviveAction))
            {
                return false;
            }
            if (!zSlideComputer.GetActionPermission("Revive", __instance.m_agent.Owner.PlayerSlotIndex()))
                return false;
            RootPlayerBotAction.s_tempObjReservation.CharacterID = __instance.m_agent.CharacterID;
            for (int i = 0; i < PlayerManager.PlayerAgentsInLevel.Count; i++)
            {
                PlayerAgent playerAgent = PlayerManager.PlayerAgentsInLevel[i];
                if (playerAgent != null && playerAgent != __instance.m_agent && !playerAgent.Alive && playerAgent.ReviveInteraction.IsActive && !playerAgent.ReviveInteraction.IsBlocked)
                {
                    RootPlayerBotAction.s_tempObjReservation.Object = playerAgent.gameObject;
                    if (PlayerManager.Current.IsObjectUnReserved(RootPlayerBotAction.s_tempObjReservation))
                    {
                        float prio = __instance.m_reviveAction.Prio;
                        UnityEngine.Vector3 position = playerAgent.Position;
                        if (!__instance.m_bot.ApplyRestrictionsToRootPosition(ref position, ref prio) || (position - playerAgent.Position).sqrMagnitude <= 1f)
                        {
                            bool allowed = true;
                            if (!zSlideComputer.GetActionPermission("ReviveBots", __instance.m_bot.Agent.PlayerSlotIndex) && playerAgent.Owner.IsBot)
                                allowed = false;
                            else if (!zSlideComputer.GetActionPermission("RevivePlayers", __instance.m_bot.Agent.PlayerSlotIndex) && !playerAgent.Owner.IsBot)
                                allowed = false;
                            //else if (!zSlideComputer.GetReviveOveridesPermission(__instance.m_bot.Agent.PlayerSlotIndex, __instance.m_reviveAction.Client.PlayerSlotIndex) ?? false)
                            //    allowed = false;
                            if (!allowed)
                                continue;
                            __instance.m_reviveAction.Client = playerAgent;
                            bestAction = __instance.m_reviveAction;
                            return false;
                        }
                    }
                }
            }
            return false;
        }
    }

}

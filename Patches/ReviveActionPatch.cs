using BetterBots.Components;
using HarmonyLib;
using Player;
using System.Collections.Generic;
using ZombieTweak2;

namespace BotControl.Patches
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
            //if (__instance.m_bot.IsActionForbidden(__instance.m_reviveAction))
            if (IsActionForbiddenDebug(__instance.m_bot, __instance.m_reviveAction))
            {
                return false;
            }
            if (!(bool)zSlideComputer.ActionPermissions.ValueAt("Revive"))
                return false;
            if (ZiMain.HasBetterBots && !BBCompat.CheckReviveAllowed(__instance.m_agent))
            {
                return false;
            }
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
                            if (!(bool)zSlideComputer.ActionPermissions.ValueAt("ReviveBots") && playerAgent.Owner.IsBot)
                                allowed = false;
                            else if (!(bool)zSlideComputer.ActionPermissions.ValueAt("RevivePlayers") && !playerAgent.Owner.IsBot)
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
        public static bool IsActionForbiddenDebug(PlayerAIBot bot, PlayerBotActionBase.Descriptor desc)
        {
            for (int i = 0; i < bot.m_queuedActions.Count; i++)
            {
                //if (!bot.m_queuedActions[i].IsActionAllowed(desc))
                if (!IsActionAllowedDebug(bot.m_queuedActions[i], desc))
                {
                    return true;
                }
            }
            for (int j = 0; j < bot.m_actions.Count; j++)
            {
                //if (!bot.m_actions[j].IsActionAllowed(desc))
                if (!IsActionAllowedDebug(bot.m_actions[j].m_descBase, desc))
                {
                    return true;
                }
            }
            return false;
        }
        public static bool IsActionAllowedDebug(PlayerBotActionBase.Descriptor thisBase, PlayerBotActionBase.Descriptor desc)
        {
            if (thisBase.IsMyChild(desc))
            {
                return true;
            }
            if ((thisBase.GetAccessLayersRuntime() & desc.RequiredLayers) != PlayerBotActionBase.AccessLayers.None)
            {
                float num = ((thisBase.ActionBase != null) ? PlayerBotActionBase.Descriptor.s_minAbortPrioDiff : 0f);
                if (desc.Prio - thisBase.Prio < num)
                {
                    return false;
                }
            }
            return true;
        }
    }

}

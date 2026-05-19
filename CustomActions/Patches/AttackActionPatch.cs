using HarmonyLib;
using Player;
using SlideMenu;
using System.Collections.Generic;
using ZombieTweak2.Menus;

namespace ZombieTweak2.CustomActions.Patches
{
    [HarmonyPatch]
    public class AttackActionPatch
    {
        private static PlayerBotActionBase.Descriptor originalBestAction;
        public static Dictionary<PlayerBotActionAttack.AttackMeansEnum, bool> meansPerms = new()
        {
            {PlayerBotActionAttack.AttackMeansEnum.Bullet, true },
            {PlayerBotActionAttack.AttackMeansEnum.Special, true },
            {PlayerBotActionAttack.AttackMeansEnum.Melee, true },
            {PlayerBotActionAttack.AttackMeansEnum.Push, true },
            {PlayerBotActionAttack.AttackMeansEnum.NanoSwarmDebuff, true },
        };
        public static Dictionary<PlayerBotActionAttack.AttackMeansEnum, sMenu.sMenuNode> nodes = new()
        {
            {PlayerBotActionAttack.AttackMeansEnum.Bullet, AttackMenuClass.bulletNode },
            //{PlayerBotActionAttack.AttackMeansEnum.Special, AutomaticActionMenuClass.AttackMenuClass.secondaryNode },
            {PlayerBotActionAttack.AttackMeansEnum.Melee, AttackMenuClass.meleeNode },
            //{PlayerBotActionAttack.AttackMeansEnum.Push, AutomaticActionMenuClass.AttackMenuClass.pushNode },
        };
        public static void ToggleMeansPerms(PlayerBotActionAttack.AttackMeansEnum meansType)
        {
            bool allowed = !meansPerms[meansType];
            SetMeansPerms(meansType, allowed);
        }
        public static void SetMeansPerms(PlayerBotActionAttack.AttackMeansEnum meansType, bool allowed)
        {
            meansPerms[meansType] = allowed;
            var node = nodes[meansType];
            if (allowed)
                node.SetColor(sMenuManager.defaultColor);
            else
                node.SetColor(new UnityEngine.Color(0.25f, 0f, 0f));
        }
        [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.UpdateActionAttack))]
        [HarmonyPrefix]
        public static void PreUpdateActionAttack(RootPlayerBotAction __instance, ref PlayerBotActionBase.Descriptor bestAction)
        {
            originalBestAction = bestAction;
        }
        [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.UpdateActionAttack))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)] //Needed for betterbots compat
        public static bool UpdateActionAttack(RootPlayerBotAction __instance, ref PlayerBotActionBase.Descriptor bestAction)
        { //This is used as an early out if they are not allowed to attack at all.
            if (!(bool)zSlideComputer.ActionPermissions.ValueAt("Attack"))
                return false;
            return true;
        }
        [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.UpdateActionAttack))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)] //Needed for betterbots compat?
        public static void PostUpdateActionAttack(RootPlayerBotAction __instance, ref PlayerBotActionBase.Descriptor bestAction)
        { //this is used to restrict the means of the bots, so they can only use the selected means.
            if (bestAction == null)
                return;
            if (bestAction.TryCast<PlayerBotActionAttack.Descriptor>() == null)
                return;
            var newMeans = PlayerBotActionAttack.AttackMeansEnum.None;

            foreach (var kvp in meansPerms)
                if (kvp.Value)
                    newMeans |= kvp.Key;
            if (newMeans == __instance.m_attackAction.Means)
                return;
            __instance.m_attackAction.Means = newMeans;
            zSlideComputer.RemoveActionsOfType(__instance.m_agent, typeof(PlayerBotActionAttack));
        }

    }
}

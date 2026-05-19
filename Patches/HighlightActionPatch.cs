using HarmonyLib;
using Player;
using SlideMenu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZombieTweak2.Menus;

namespace ZombieTweak2.Patches
{
    [HarmonyPatch]
    public class HighlightActionPatch
    {
        private static PlayerBotActionBase.Descriptor originalBestAction;
        public static Dictionary<PlayerBotActionHighlight.Descriptor.TargetTypeEnum, bool> targetTypePerms = new()
        {
            {PlayerBotActionHighlight.Descriptor.TargetTypeEnum.Container, true },
            {PlayerBotActionHighlight.Descriptor.TargetTypeEnum.Door, true },
            {PlayerBotActionHighlight.Descriptor.TargetTypeEnum.Terminal, true },
        };
        public static Dictionary<PlayerBotActionHighlight.Descriptor.TargetTypeEnum, sMenu.sMenuNode> nodes = new()
        {
            {PlayerBotActionHighlight.Descriptor.TargetTypeEnum.Container, PingMenuClass.containersNode },
            {PlayerBotActionHighlight.Descriptor.TargetTypeEnum.Door, PingMenuClass.doorsNode },
            {PlayerBotActionHighlight.Descriptor.TargetTypeEnum.Terminal, PingMenuClass.terminalsNode },
        };
        public static void ToggleTargetTypePerms(PlayerBotActionHighlight.Descriptor.TargetTypeEnum targetType)
        {
            bool allowed = !targetTypePerms[targetType];
            SetTargetTypePerms(targetType, allowed);
        }
        public static void SetTargetTypePerms(PlayerBotActionHighlight.Descriptor.TargetTypeEnum targetType, bool allowed)
        {
            targetTypePerms[targetType] = allowed;
            var node = nodes[targetType];
            if (allowed)
                node.SetColor(sMenuManager.defaultColor);
            else
                node.SetColor(new UnityEngine.Color(0.25f, 0f, 0f));
        }
        [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.UpdateActionHighlight))]
        [HarmonyPrefix]
        public static void PreUpdateActionHighlight(RootPlayerBotAction __instance, ref PlayerBotActionBase.Descriptor bestAction)
        {
            originalBestAction = bestAction;
        }
        [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.UpdateActionHighlight))]
        [HarmonyPostfix]
        public static void PostUpdateActionHighlight(RootPlayerBotAction __instance, ref PlayerBotActionBase.Descriptor bestAction)
        {
            if (bestAction == null)
                return;
            if (bestAction.TryCast<PlayerBotActionHighlight.Descriptor>() == null)
                return;
            var targetType = __instance.m_highlightAction.TargetType;
            if (!targetTypePerms[targetType])
                bestAction = originalBestAction;
        }

    }
}

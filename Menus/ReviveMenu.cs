using Player;
using SlideMenu;
using System.Collections.Generic;

namespace BotControl.Menus
{
    public static class ReviveMenuClass
    {
        public static sMenu reviveMenu;
        public static sMenu.sMenuNode reviveNode;
        public static sMenu overidesMenu;
        public static sMenu.sMenuNode playersNode;
        public static sMenu.sMenuNode botsNode;
        public static void Setup(sMenu menu)
        {
            reviveMenu = menu;
            reviveNode = menu.GetNode();
            reviveNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
            reviveNode.AddListener(sMenuManager.nodeEvent.OnDoubleTapped, reviveMenu.Open);
            playersNode = reviveMenu.AddNode("Players");
            playersNode.AddListener(sMenuManager.nodeEvent.OnTapped, zSlideComputer.GenericToggleAllowed, "RevivePlayers", playersNode);
            botsNode = reviveMenu.AddNode("Bots");
            botsNode.AddListener(sMenuManager.nodeEvent.OnTapped, zSlideComputer.GenericToggleAllowed, "ReviveBots", botsNode);
            zSlideComputer.PermissionDefinitions.CreatePermissionDeffinition("RevivePlayers", null, playersNode, ActionTypeToCull: typeof(PlayerBotActionRevive), parrentKey: "Revive", hasDefaultValue: true);
            zSlideComputer.ActionPermissions.GetNodeFromIdent("RevivePlayers").onChanged.Listen(UpdateNodeDisplay, args: ["RevivePlayers", playersNode]);
            zSlideComputer.PermissionDefinitions.CreatePermissionDeffinition("ReviveBots", null, botsNode, ActionTypeToCull: typeof(PlayerBotActionRevive), parrentKey: "Revive", hasDefaultValue: true);
            zSlideComputer.ActionPermissions.GetNodeFromIdent("ReviveBots").onChanged.Listen(UpdateNodeDisplay, args: ["ReviveBots", botsNode]);
            menu.centerNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
            menu.centerNode.AddListener(sMenuManager.nodeEvent.OnTapped, menu.parrentMenu.Open);
            menu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediateSelected, AutomaticActionMenuClass.GenericResetSettings, args: [botsNode, false, "ReviveBots"]);
            menu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediateSelected, AutomaticActionMenuClass.GenericResetSettings, args: [playersNode, false, "RevivePlayers"]);
            menu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediateSelected, AutomaticActionMenuClass.GenericResetSettings, args: [reviveNode, false, "Revive"]);
            botsNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediateSelected, AutomaticActionMenuClass.GenericResetSettings, args: [botsNode, false, "ReviveBots"]);
            playersNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediateSelected, AutomaticActionMenuClass.GenericResetSettings, args: [playersNode, false, "RevivePlayers"]);

            reviveMenu.AddPannel(sMenu.sMenuPannel.Side.top, "Controls who the bots are allowed to revive");
            reviveMenu.AddPannel(sMenu.sMenuPannel.Side.top, "I plan to add a way to control revives of spesific plaers");
            reviveMenu.AddPannel(sMenu.sMenuPannel.Side.top, "Maybe an option to only revive their leader?");
        }
        public static void UpdateNodeDisplay(string actionKey, sMenu.sMenuNode node)
        {
            //AutomaticActionMenuClass.ApplyTextEffectBasedOnKeyTree(node, key, zSlideComputer.ActionPermissions, AutomaticActionMenuClass.textEffect.Bold);
            AutomaticActionMenuClass.GenericUpdateNodeAllowedDisplay(actionKey, node);
            //List<IOverrideTree> trees = new()
            //{
            //    zSlideComputer.ActionPermissions,
            //};
            //bool italic = AutomaticActionMenuClass.AnyTreeOverridesNullDefault(trees, actionKey);
            //bool star = !AutomaticActionMenuClass.AllMatchingDefaultValue(trees, actionKey);
            //AutomaticActionMenuClass.ApplyTextEffectToNode(node, AutomaticActionMenuClass.textEffect.Star, star);
            //AutomaticActionMenuClass.ApplyTextEffectToNode(node, AutomaticActionMenuClass.textEffect.Italic, italic);
        }
    }
}

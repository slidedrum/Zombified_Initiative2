using Player;
using SlideMenu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZombieTweak2.Menus
{
    public static class ReviveMenuClass
    {
        public static sMenu reviveMenu;
        public static sMenu.sMenuNode reviveNode;
        public static sMenu overidesMenu;
        public static sMenu.sMenuNode playersNode;
        public static sMenu.sMenuNode botsNode;
        public static Dictionary<string, sMenu.sMenuNode> overideNodes = new();
        public static void Setup(sMenu menu)
        {
            reviveMenu = menu;
            reviveNode = menu.GetNode();
            reviveNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
            reviveNode.AddListener(sMenuManager.nodeEvent.OnDoubleTapped, reviveMenu.Open);
            playersNode = reviveMenu.AddNode("Players");
            playersNode.AddListener(sMenuManager.nodeEvent.OnPressed, AutomaticActionMenuClass.GenericToggleAllowed, "RevivePlayers", null, playersNode);
            botsNode = reviveMenu.AddNode("Bots");
            botsNode.AddListener(sMenuManager.nodeEvent.OnPressed, AutomaticActionMenuClass.GenericToggleAllowed, "ReviveBots", null, botsNode);
            zSlideComputer.PermissionDefinitions.CreatePermissionDeffinition("RevivePlayers", true, playersNode, typeof(PlayerBotActionRevive));
            zSlideComputer.PermissionDefinitions.CreatePermissionDeffinition("ReviveBots", true, botsNode, typeof(PlayerBotActionRevive));
            //overidesMenu = new sMenu("overrides", reviveMenu);
            //reviveMenu.AddNode(overidesMenu);
            //var playerAgents = PlayerManager.PlayerAgentsInLevel;
            //foreach (PlayerAgent agent in playerAgents)
            //{
            //    string name = agent.PlayerName;
            //    overideNodes[name] = overidesMenu.AddNode(name, toggleOveridePerms, name);
            //}
        }
    }
}

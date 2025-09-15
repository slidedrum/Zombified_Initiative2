using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zombified_Initiative;

namespace ZombieTweak2.zMenu
{
    public static class zMenus
    {
        public static List<zMenu> botMenus;
        public static void CreateMenus()
        {
            zMenu selectionMenu = zMenuManager.createMenu("Bot selection", zMenuManager.mainMenu);
            zMenu actionMenu = zMenuManager.createMenu("Actions", zMenuManager.mainMenu);
            zMenu permmisionMenu = zMenuManager.createMenu("Permissions", zMenuManager.mainMenu);
            zMenuManager.mainMenu.AddNode(selectionMenu);
            zMenuManager.mainMenu.AddNode(actionMenu);
            zMenuManager.mainMenu.AddNode(permmisionMenu);
            selectionMenu.AddNode("Toggle all", SelectionToggleAllBots);
            selectionMenu.AddNode("Flip all", SelectionFlipAllBots);
        }
        private static void addBotMenus(zMenu menu)
        {
            List<PlayerAIBot> playerAiBots = Zi.GetBotList();
            foreach (PlayerAIBot bot in playerAiBots)
            {
                zMenu menu = zMenuManager.
                botMenus.Add(menu);
            }
        }
        private static void SelectionFlipAllBots()
        {
            
        }

        public static void SelectionToggleAllBots()
        {

        }
    }
}

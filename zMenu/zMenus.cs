using Player;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zombified_Initiative;

namespace ZombieTweak2.zMenu
{
    public static class zMenus
    {
        //This is the class that actually creates the menue instances
        //This is spesific to ZI
        //Very unfinished atm

        public static List<zMenu> botMenus;
        public static Dictionary<int, bool> botSelections = new();
        private static Dictionary<int, zMenu.zMenuNode> selectionBotNodes = new();
        public static Color selectedColor = new Color(0.25f, 0.16175f, 0.0f);
        private static zMenu selectionMenu;
        private static zMenu actionMenu;
        private static zMenu permissionMenu;

        public static void CreateMenus()
        {
            selectionMenu = zMenuManager.createMenu("Bot selection", zMenuManager.mainMenu);
            actionMenu = zMenuManager.createMenu("Actions", zMenuManager.mainMenu);
            permissionMenu = zMenuManager.createMenu("Permissions", zMenuManager.mainMenu);

            selectionMenu.AddNode("Toggle all", SelectionToggleAllBots);
            selectionMenu.AddNode("Flip all", SelectionFlipAllBots);
            List<PlayerAIBot> playerAiBots = ZiMain.GetBotList();
            foreach (PlayerAIBot bot in playerAiBots)
            {
                string botName = bot.m_playerAgent.PlayerName;
                int id = bot.GetInstanceID();
                botSelections[id] = true;
                zMenu.zMenuNode node = selectionMenu.AddNode(bot.m_playerAgent.PlayerName,toggleBotSelection, bot);
                selectionBotNodes[id] = node;
                updateColorBaesdOnSelection(node, bot);
                node.AddListener(zMenuManager.nodeEvent.OnUnpressedSelected, updateColorBaesdOnSelection, node, bot);
                node.parrentMenu.AddListener(zMenuManager.menuEvent.OnOpened, updateColorBaesdOnSelection, node, bot);
            }

            zMenu pickupPermMenu = zMenuManager.createMenu("Pickups", permissionMenu);
            pickupPermMenu.AddNode("Toggle", toglePickupPermissionForSelected);
            pickupPermMenu.AddNode("On", setPickupPermissionForSelected, true);
            pickupPermMenu.AddNode("Off", setPickupPermissionForSelected, false);
            zMenu sharePermMenu = zMenuManager.createMenu("Share", permissionMenu);
            sharePermMenu.AddNode("Toggle");
            sharePermMenu.AddNode("On");
            sharePermMenu.AddNode("Off");
            zMenu movePermMenu = zMenuManager.createMenu("Move", permissionMenu);
            movePermMenu.AddNode("Toggle");
            movePermMenu.AddNode("On");
            movePermMenu.AddNode("Off");
        }
        private static void toggleSharePermissionForSelected()
        {
            List<PlayerAIBot> SelectedBots = getSelectedBots();
            foreach (PlayerAIBot bot in SelectedBots)
            {
                //Zi.(bot.m_playerAgent.PlayerName);
            }
        }
        private static void toglePickupPermissionForSelected()
        {
            List<PlayerAIBot> SelectedBots = getSelectedBots();
            foreach (PlayerAIBot bot in SelectedBots) 
            {
                ZiMain.togglePickupPermission(bot.m_playerAgent.PlayerName);
            }
        }
        private static void setPickupPermissionForSelected(bool allowed)
        {
            List<PlayerAIBot> SelectedBots = getSelectedBots();
            foreach (PlayerAIBot bot in SelectedBots)
            {
                ZiMain.setPickupPermission(bot.m_playerAgent.PlayerName, allowed);
            }
        }
        public static zMenu.zMenuNode updateColorBaesdOnSelection(zMenu.zMenuNode node, PlayerAIBot bot)
        {
            if (checkForUntrackedBot(bot))
                return null;
            if (botSelections[bot.GetInstanceID()])
                node.SetColor(selectedColor);
            else
                node.SetColor(zMenuManager.defaultColor);
            return node;
        }
        public static PlayerAIBot toggleBotSelection(PlayerAIBot bot)
        {
            if (checkForUntrackedBot(bot))
                return null;
            botSelections[bot.GetInstanceID()] = !botSelections[bot.GetInstanceID()];
            return bot;
        }
        public static List<PlayerAIBot> getSelectedBots()
        {
            List<PlayerAIBot> selectedBots = new();
            var allBots = ZiMain.GetBotList();
            foreach (PlayerAIBot bot in allBots)
            {
                bool botSelected = botSelections[bot.GetInstanceID()];
                if (botSelected)
                {
                    selectedBots.Add(bot);
                }
            }
            return selectedBots;
        }
        public static PlayerAIBot setBotSelection(PlayerAIBot bot, bool selected)
        {
            if (checkForUntrackedBot(bot))
                return null;
            botSelections[bot.GetInstanceID()] = selected;
            return bot;
        }
        private static bool checkForUntrackedBot(PlayerAIBot bot)
        {
            if (bot == null)
            {
                ZiMain.log.LogError("Can't toggle bot selection of null!  This should not happen.");
                return true;
            }
            if (!botSelections.ContainsKey(bot.GetInstanceID()))
                throw new KeyNotFoundException($"The bot {bot} is not tracked for selection.  This should't happen.");
            return false;
        }
        private static void addBotMenus(zMenu menu)
        {

        }
        private static void SelectionFlipAllBots()
        {
            foreach (var bot in ZiMain.GetBotList())
            {
                toggleBotSelection(bot);
                updateColorBaesdOnSelection(selectionBotNodes[bot.GetInstanceID()], bot);
            }
        }
        public static void SelectionToggleAllBots()
        {
            int selectedCount = botSelections.Values.Count(value => value);
            int unselectedCount = botSelections.Values.Count() - selectedCount;
            bool majority = selectedCount > unselectedCount;
            foreach (var bot in ZiMain.GetBotList())
            {
                setBotSelection(bot,!majority);
                updateColorBaesdOnSelection(selectionBotNodes[bot.GetInstanceID()], bot);
            }
        }
    }
}

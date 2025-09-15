using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Zombified_Initiative;

namespace ZombieTweak2.zMenu
{
    public static class zMenus
    {
        public static List<zMenu> botMenus;
        public static Dictionary<int, bool> selectedBots = new();
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
            List<PlayerAIBot> playerAiBots = Zi.GetBotList();
            foreach (PlayerAIBot bot in playerAiBots)
            {
                string botName = bot.m_playerAgent.PlayerName;
                int id = bot.GetInstanceID();
                selectedBots[id] = true;
                zMenu.zMenuNode node = selectionMenu.AddNode(bot.m_playerAgent.PlayerName,toggleBotSelection, bot);
                selectionBotNodes[id] = node;
                updateColorBaesdOnSelection(node, bot);
                node.AddListener(zMenuManager.nodeEvent.OnPressed, updateColorBaesdOnSelection, node, bot);
                node.parrentMenu.AddListener(zMenuManager.menuEvent.OnOpened, updateColorBaesdOnSelection, node, bot);
            }

            zMenu pickupPermMenu = zMenuManager.createMenu("Pickups", permissionMenu);
            pickupPermMenu.AddNode("Toggle",);
            pickupPermMenu.AddNode("On");
            pickupPermMenu.AddNode("Off");
            zMenu sharePermMenu = zMenuManager.createMenu("Share", permissionMenu);
            sharePermMenu.AddNode("Toggle");
            sharePermMenu.AddNode("On");
            sharePermMenu.AddNode("Off");
            zMenu movePermMenu = zMenuManager.createMenu("Move", permissionMenu);
            movePermMenu.AddNode("Toggle");
            movePermMenu.AddNode("On");
            movePermMenu.AddNode("Off");



        }
        public static zMenu.zMenuNode updateColorBaesdOnSelection(zMenu.zMenuNode node, PlayerAIBot bot)
        {
            if (checkForUntrackedBot(bot))
                return null;
            if (selectedBots[bot.GetInstanceID()])
                node.SetColor(selectedColor);
            else
                node.SetColor(zMenuManager.defaultColor);
            return node;
        }
        public static PlayerAIBot toggleBotSelection(PlayerAIBot bot)
        {
            if (checkForUntrackedBot(bot))
                return null;
            selectedBots[bot.GetInstanceID()] = !selectedBots[bot.GetInstanceID()];
            return bot;
        }
        public static PlayerAIBot setBotSelection(PlayerAIBot bot, bool selected)
        {
            if (checkForUntrackedBot(bot))
                return null;
            selectedBots[bot.GetInstanceID()] = selected;
            return bot;
        }
        private static bool checkForUntrackedBot(PlayerAIBot bot)
        {
            if (bot == null)
            {
                Zi.log.LogError("Can't toggle bot selection of null!  This should not happen.");
                return true;
            }
            if (!selectedBots.ContainsKey(bot.GetInstanceID()))
                throw new KeyNotFoundException($"The bot {bot} is not tracked for selection.  This should't happen.");
            return false;
        }
        private static void addBotMenus(zMenu menu)
        {

        }
        private static void SelectionFlipAllBots()
        {
            foreach (var bot in Zi.GetBotList())
            {
                toggleBotSelection(bot);
                updateColorBaesdOnSelection(selectionBotNodes[bot.GetInstanceID()], bot);
            }
        }
        public static void SelectionToggleAllBots()
        {
            int selectedCount = selectedBots.Values.Count(value => value);
            int unselectedCount = selectedBots.Values.Count() - selectedCount;
            bool majority = selectedCount > unselectedCount;
            foreach (var bot in Zi.GetBotList())
            {
                setBotSelection(bot,!majority);
                updateColorBaesdOnSelection(selectionBotNodes[bot.GetInstanceID()], bot);
            }
        }
    }
}

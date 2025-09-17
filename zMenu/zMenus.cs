using Agents;
using Player;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Zombified_Initiative;

namespace ZombieTweak2.zMenu
{
    public static class zMenus
    {
        //This is the class that actually creates the menue instances
        //This is spesific to ZI
        //Very unfinished atm

        public static List<zMenu> botMenus;
        public static Dictionary<int, bool> botSelection = new();
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

            selectionMenu.AddNode("Toggle all", SelectionToggleAllBots).AddListener(zMenuManager.nodeEvent.OnUnpressedSelected, UpdateIndicatorForNode, selectionMenu.centerNode, botSelection);
            selectionMenu.AddNode("Flip all", SelectionFlipAllBots).AddListener(zMenuManager.nodeEvent.OnUnpressedSelected, UpdateIndicatorForNode, selectionMenu.centerNode, botSelection);
            UpdateIndicatorForNode(selectionMenu.centerNode, botSelection);

            List<PlayerAIBot> playerAiBots = ZiMain.GetBotList();
            foreach (PlayerAIBot bot in playerAiBots)
            {
                string botName = bot.m_playerAgent.PlayerName;
                int id = bot.Agent.Owner.PlayerSlotIndex();
                botSelection[id] = true;
                zMenu.zMenuNode node = selectionMenu.AddNode(bot.m_playerAgent.PlayerName,toggleBotSelection, bot);
                selectionBotNodes[id] = node;
                updateColorBaesdOnSelection(node, bot);
                node.AddListener(zMenuManager.nodeEvent.OnUnpressedSelected, updateColorBaesdOnSelection, node, bot);
                node.AddListener(zMenuManager.nodeEvent.OnUnpressedSelected, UpdateIndicatorForNode, selectionMenu.centerNode, botSelection);
                node.parrentMenu.AddListener(zMenuManager.menuEvent.OnOpened, updateColorBaesdOnSelection, node, bot);
            }
            var pickupNode = permissionMenu.AddNode("Pickups", zSlideComputer.TogglePickupPermission);
            pickupNode.AddListener(zMenuManager.nodeEvent.OnUnpressedSelected, UpdateIndicatorForNode, pickupNode, zSlideComputer.PickUpPerms);
            permissionMenu.AddListener(zMenuManager.menuEvent.OnOpened, UpdateIndicatorForNode, permissionMenu.centerNode, botSelection);
            UpdateIndicatorForNode(permissionMenu.centerNode, botSelection);
        }
        public static zMenu.zMenuNode UpdateIndicatorForNode(zMenu.zMenuNode node, Dictionary<int, bool> selectionPickUpPerms)
        {
            string endcolor = "</color>";
            string enabledColor = "<color=#FFA50066>";
            string dissabledColor = "<color=#CCCCCC66>";
            string sbSubtitle = "[";
            var last = selectionPickUpPerms.Last();
            foreach (var bot in selectionPickUpPerms)
            {
                if (bot.Value)
                    sbSubtitle += enabledColor;
                else
                    sbSubtitle += dissabledColor;
                PlayerAgent agent;
                int id = bot.Key;
                PlayerManager.TryGetPlayerAgent(ref id, out agent);
                string name = agent.PlayerName;
                sbSubtitle += name[0];
                sbSubtitle += endcolor;
                if (bot.Key != last.Key)
                    sbSubtitle += ',';
                else
                    sbSubtitle += ']';
            }
            node.subtitle = sbSubtitle;
            return node;
            return node;
        }
        public static zMenu.zMenuNode updateColorBaesdOnSelection(zMenu.zMenuNode node, PlayerAIBot bot)
        {
            if (checkForUntrackedBot(bot))
                return null;
            if (botSelection[bot.Agent.Owner.PlayerSlotIndex()])
                node.SetColor(selectedColor);
            else
                node.SetColor(zMenuManager.defaultColor);
            return node;
        }
        public static PlayerAIBot toggleBotSelection(PlayerAIBot bot)
        {
            if (checkForUntrackedBot(bot))
                return null;
            botSelection[bot.Agent.Owner.PlayerSlotIndex()] = !botSelection[bot.Agent.Owner.PlayerSlotIndex()];
            return bot;
        }
        public static List<PlayerAIBot> getSelectedBots()
        {
            List<PlayerAIBot> selectedBots = new();
            var allBots = ZiMain.GetBotList();
            foreach (PlayerAIBot bot in allBots)
            {
                bool botSelected = botSelection[bot.Agent.Owner.PlayerSlotIndex()];
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
            botSelection[bot.Agent.Owner.PlayerSlotIndex()] = selected;
            return bot;
        }
        private static bool checkForUntrackedBot(PlayerAIBot bot)
        {
            if (bot == null)
            {
                ZiMain.log.LogError("Can't toggle bot selection of null!  This should not happen.");
                return true;
            }
            if (!botSelection.ContainsKey(bot.Agent.Owner.PlayerSlotIndex()))
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
                updateColorBaesdOnSelection(selectionBotNodes[bot.Agent.Owner.PlayerSlotIndex()], bot);
            }
        }
        public static void SelectionToggleAllBots()
        {
            int selectedCount = botSelection.Values.Count(value => value);
            int unselectedCount = botSelection.Values.Count() - selectedCount;
            bool majority = selectedCount > unselectedCount;
            foreach (var bot in ZiMain.GetBotList())
            {
                setBotSelection(bot,!majority);
                updateColorBaesdOnSelection(selectionBotNodes[bot.Agent.Owner.PlayerSlotIndex()], bot);
            }
        }
    }
}

using Agents;
using GameData;
using Player;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Zombified_Initiative;
using static ZombieTweak2.zNetworking.pStructs;

namespace ZombieTweak2.zMenu
{
    public static class zMenus
    {
        //This is the class that actually creates the menue instances
        //This is spesific to ZI
        //Very unfinished atm

        public static List<zMenu> botMenus;

        public static Color selectedColor = new Color(0.25f, 0.16175f, 0.0f);
        private static zMenu selectionMenu;
        private static zMenu actionMenu;
        private static zMenu permissionMenu;
        private static zMenu pickupDetailsSubmenu;

        public static void CreateMenus()
        {
            selectionMenu = zMenuManager.createMenu("Bot selection", zMenuManager.mainMenu);
            actionMenu = zMenuManager.createMenu("Actions", zMenuManager.mainMenu);
            permissionMenu = zMenuManager.createMenu("Permissions", zMenuManager.mainMenu);
            pickupDetailsSubmenu = zMenuManager.createMenu("Pickups", permissionMenu);
            permissionMenu.DissableNode("Pickups");

            selectionMenu.AddNode("Toggle all", SelectionMenu.SelectionToggleAllBots).AddListener(zMenuManager.nodeEvent.OnUnpressedSelected, UpdateIndicatorForNode, selectionMenu.centerNode, SelectionMenu.botSelection);
            selectionMenu.AddNode("Flip all", SelectionMenu.SelectionFlipAllBots).AddListener(zMenuManager.nodeEvent.OnUnpressedSelected, UpdateIndicatorForNode, selectionMenu.centerNode, SelectionMenu.botSelection);
            //todo add option to set selection to the bots that are following you.
            List<PlayerAIBot> playerAiBots = ZiMain.GetBotList();
            foreach (PlayerAIBot bot in playerAiBots.AsEnumerable().Reverse())
            {
                string botName = bot.m_playerAgent.PlayerName;
                int id = bot.Agent.Owner.PlayerSlotIndex();
                zMenu.zMenuNode node = selectionMenu.AddNode(bot.m_playerAgent.PlayerName, SelectionMenu.toggleBotSelection, bot);
                SelectionMenu.selectionBotNodes[id] = node;
                node.AddListener(zMenuManager.nodeEvent.OnUnpressedSelected, SelectionMenu.updateColorBaesdOnSelection, node, bot);
                node.AddListener(zMenuManager.nodeEvent.OnUnpressedSelected, UpdateIndicatorForNode, selectionMenu.centerNode, SelectionMenu.botSelection);
                node.parrentMenu.AddListener(zMenuManager.menuEvent.OnOpened, SelectionMenu.updateColorBaesdOnSelection, node, bot);
            }
            foreach (PlayerAIBot bot in playerAiBots)
            {
                int id = bot.Agent.Owner.PlayerSlotIndex();
                SelectionMenu.botSelection[id] = true;
            }

            var pickupNode = permissionMenu.AddNode("Pickups").AddListener(zMenuManager.nodeEvent.OnTapped, zSlideComputer.TogglePickupPermission);
            pickupNode.AddListener(zMenuManager.nodeEvent.OnTapped, UpdateIndicatorForNode, pickupNode, zSlideComputer.PickUpPerms);
            pickupNode.AddListener(zMenuManager.nodeEvent.OnHeld, pickupDetailsSubmenu.Open);
            PermissionsMenuClass.setUpItemNodes(pickupDetailsSubmenu);
            pickupDetailsSubmenu.centerNode.AddListener(zMenuManager.nodeEvent.OnHeldSelected, zSlideComputer.ResetItemPrio);
            pickupDetailsSubmenu.radius = 175;
            selectionMenu.AddListener(zMenuManager.menuEvent.OnOpened, UpdateIndicatorForNode, selectionMenu.centerNode, SelectionMenu.botSelection);
            permissionMenu.AddListener(zMenuManager.menuEvent.OnOpened, UpdateIndicatorForNode, permissionMenu.centerNode, SelectionMenu.botSelection);
            permissionMenu.AddListener(zMenuManager.menuEvent.OnOpened, UpdateIndicatorForNode, pickupNode, zSlideComputer.PickUpPerms);
        }
        public static zMenu.zMenuNode UpdateIndicatorForNode(zMenu.zMenuNode node, Dictionary<int, bool> selectionPickUpPerms)
        {
            ZiMain.log.LogInfo($"Updatin selections for node {node.text}");
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
        }



        private static void addBotMenus(zMenu menu)
        {

        }

    }
    public static class PermissionsMenuClass
    {
        public static void setUpItemNodes(zMenu menu)
        {
            foreach (var item in zSlideComputer.itemPrios)
            {
                uint itemID = item.Key;
                ItemDataBlock block = ItemDataBlock.s_blockByID[itemID];
                string publicName = block.publicName;
                var node = menu.AddNode(publicName);
                node.AddListener(zMenuManager.nodeEvent.WhileSelected, ChangePrioBasedOnMouseWheel, itemID, node);
                node.AddListener(zMenuManager.nodeEvent.OnTapped, zSlideComputer.ToggleItemPrioDissabled, itemID);
                node.AddListener(zMenuManager.nodeEvent.OnTapped, updateNodePriorityDisplay, node, itemID);//TODO make these args order consistant.
                node.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, zSlideComputer.ResetItemPrio, itemID);
                menu.centerNode.AddListener(zMenuManager.nodeEvent.OnHeldSelected, updateNodePriorityDisplay, node, itemID);
                updateNodePriorityDisplay(node, itemID);
                node.fullTextPart.SetScale(0.75f, 0.75f);
                node.subtitlePart.SetScale(0.5f, 0.5f);
                node.titlePart.SetScale(0.3f, 0.3f);
            }
        }
        public static void updateNodePriorityDisplay(zMenu.zMenuNode node, uint itemID)
        {
            if (zSlideComputer.itemPrios[itemID] == zSlideComputer.OriginalItemPrios[itemID])
            {
                node.SetPrefix("");
                node.SetSuffix("");
            }
            else
            {
                node.SetPrefix("* ");
                node.SetSuffix(" *");
            }
            string hex = ColorUtility.ToHtmlStringRGB(GetPriorityColor(zSlideComputer.GetItemPrio(itemID)));
            node.SetSubtitle($"<color=#CC840066>[ </color><color=#{hex}>{zSlideComputer.GetItemPrio(itemID)}</color><color=#CC840066> ]</color>");
        }
        public static Color GetPriorityColor(float value)
        {
            // scale factor to dim colors
            float max = 0.25f;

            Color red = new Color(max, 0f, 0f);
            Color yellow = new Color(max, max, 0f);
            Color green = new Color(0f, max, 0f);
            Color blue = new Color(0f, 0f, max);

            if (value <= 25f)
                return Color.Lerp(red, yellow, value / 25f);
            if (value <= 50f)
                return Color.Lerp(yellow, green, (value - 25f) / 25f);
            return Color.Lerp(green, blue, (value - 50f) / 50f);
        }
        public static void ChangePrioBasedOnMouseWheel(uint id, zMenu.zMenuNode node, int increment = 10)
        {
            if (node == null || !node.gameObject.activeInHierarchy)
                return;
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            int normalizedScroll = (int)Mathf.Sign(scroll);
            if (scroll == 0f)
                return;
            float currentPrio = zSlideComputer.GetItemPrio(id);
            zSlideComputer.SetBotItemPriority(id, Mathf.Clamp(currentPrio + (normalizedScroll * increment),0,100));
            updateNodePriorityDisplay(node, id);
        }
    }
    public static class SelectionMenu
    {
        public static Dictionary<int, bool> botSelection = new();
        public static Dictionary<int, zMenu.zMenuNode> selectionBotNodes = new();
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
        public static void SelectionFlipAllBots()
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
                setBotSelection(bot, !majority);
                updateColorBaesdOnSelection(selectionBotNodes[bot.Agent.Owner.PlayerSlotIndex()], bot);
            }
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
        public static zMenu.zMenuNode updateColorBaesdOnSelection(zMenu.zMenuNode node, PlayerAIBot bot)
        {
            if (checkForUntrackedBot(bot))
                return null;
            if (botSelection[bot.Agent.Owner.PlayerSlotIndex()])
                node.SetColor(zMenus.selectedColor);
            else
                node.SetColor(zMenuManager.defaultColor);
            return node;
        }
    }
}

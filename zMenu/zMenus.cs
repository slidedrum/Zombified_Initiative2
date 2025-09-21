using GameData;
using Player;
using System;
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

        
        private static zMenu selectionMenu;
        private static zMenu actionMenu;
        public static zMenu permissionMenu;
        private static zMenu pickupDetailsSubmenu;
        private static zMenu shareDetailsSubmenu;
        private static string endcolor = "</color>";
        private static string enabledColor = "<color=#FFA50066>";
        private static string disabledColor = "<color=#CCCCCC33>";

        public static void CreateMenus()
        {
            selectionMenu = zMenuManager.createMenu("Bot selection", zMenuManager.mainMenu);
            actionMenu = zMenuManager.createMenu("Actions", zMenuManager.mainMenu);
            permissionMenu = zMenuManager.createMenu("Permissions", zMenuManager.mainMenu);
            pickupDetailsSubmenu = zMenuManager.createMenu("Pickups", permissionMenu, false);
            shareDetailsSubmenu = zMenuManager.createMenu("Share", permissionMenu, false);
            //todo remove the flip/toggle nodes, and instead make hold action
            selectionMenu.AddNode("Toggle all", SelectionMenuClass.SelectionToggleAllBots).AddListener(zMenuManager.nodeEvent.OnUnpressedSelected, UpdateIndicatorForNode, selectionMenu.centerNode, SelectionMenuClass.botSelection);
            selectionMenu.AddNode("Flip all", SelectionMenuClass.SelectionFlipAllBots).AddListener(zMenuManager.nodeEvent.OnUnpressedSelected, UpdateIndicatorForNode, selectionMenu.centerNode, SelectionMenuClass.botSelection);
            //todo add option to set selection to the bots that are following you.
            List<PlayerAIBot> playerAiBots = ZiMain.GetBotList();
            foreach (PlayerAIBot bot in playerAiBots.AsEnumerable().Reverse())
            {
                string botName = bot.m_playerAgent.PlayerName;
                int id = bot.Agent.Owner.PlayerSlotIndex();
                zMenu.zMenuNode node = selectionMenu.AddNode(bot.m_playerAgent.PlayerName, SelectionMenuClass.toggleBotSelection, bot);
                SelectionMenuClass.selectionBotNodes[id] = node;
                node.AddListener(zMenuManager.nodeEvent.OnUnpressedSelected, SelectionMenuClass.updateColorBaesdOnSelection, node, bot);
                node.AddListener(zMenuManager.nodeEvent.OnUnpressedSelected, UpdateIndicatorForNode, selectionMenu.centerNode, SelectionMenuClass.botSelection);
                node.parrentMenu.AddListener(zMenuManager.menuEvent.OnOpened, SelectionMenuClass.updateColorBaesdOnSelection, node, bot);
            }
            foreach (PlayerAIBot bot in playerAiBots)
            {
                int id = bot.Agent.Owner.PlayerSlotIndex();
                SelectionMenuClass.botSelection[id] = true;
            }
            //todo this might be an issue having two nodes with the same name.  Maybe add an ID getter system?
            var pickupNode = permissionMenu.AddNode("Pickups").AddListener(zMenuManager.nodeEvent.OnTapped, zSlideComputer.TogglePickupPermission);
            pickupNode.AddListener(zMenuManager.nodeEvent.OnTapped, UpdateIndicatorForNode, pickupNode, zSlideComputer.PickUpPerms);
            pickupNode.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, pickupDetailsSubmenu.Open);
            //TODO make 5 item filters that you can switch between by scrolling on center node.
            //ALL - ENCOUNTERED - RESOURCES - PLACEABLES - THROWABLES - FAVORITES
            pickupDetailsSubmenu.centerNode.ClearListeners(zMenuManager.nodeEvent.OnUnpressedSelected); 
            pickupDetailsSubmenu.centerNode.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, zSlideComputer.ResetAllItemPrio);
            pickupDetailsSubmenu.centerNode.AddListener(zMenuManager.nodeEvent.OnTapped, pickupDetailsSubmenu.parrentMenu.Open);
            PermissionsMenuClass.setUpItemNodes(pickupDetailsSubmenu);
            pickupDetailsSubmenu.radius = 35;
            selectionMenu.AddListener(zMenuManager.menuEvent.OnOpened, UpdateIndicatorForNode, selectionMenu.centerNode, SelectionMenuClass.botSelection);
            permissionMenu.AddListener(zMenuManager.menuEvent.OnOpened, UpdateIndicatorForNode, permissionMenu.centerNode, SelectionMenuClass.botSelection);
            permissionMenu.AddListener(zMenuManager.menuEvent.OnOpened, UpdateIndicatorForNode, pickupNode, zSlideComputer.PickUpPerms);

            var shareNode = permissionMenu.AddNode("Share").AddListener(zMenuManager.nodeEvent.OnTapped, zSlideComputer.ToggleBotSharePermission);
            shareNode.AddListener(zMenuManager.nodeEvent.OnTapped, UpdateIndicatorForNode, shareNode, zSlideComputer.SharePerms);
            permissionMenu.AddListener(zMenuManager.menuEvent.OnOpened, UpdateIndicatorForNode, shareNode, zSlideComputer.SharePerms);
            shareNode.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, shareDetailsSubmenu.Open);
            shareDetailsSubmenu.centerNode.ClearListeners(zMenuManager.nodeEvent.OnUnpressedSelected);
            shareDetailsSubmenu.centerNode.AddListener(zMenuManager.nodeEvent.OnTapped, shareDetailsSubmenu.parrentMenu.Open);
            shareDetailsSubmenu.AddNode("");
            ShareMenuClass.setUpPackNodes(shareDetailsSubmenu);

            permissionMenu.AddNode("Move");
        }
        public static zMenu.zMenuNode UpdateIndicatorForNode(zMenu.zMenuNode node, Dictionary<int, bool> selectionPickUpPerms)
        {
            ZiMain.log.LogInfo($"Updatin selections for node {node.text}");
            
            string sbSubtitle = "[";
            var last = selectionPickUpPerms.Last();
            foreach (var bot in selectionPickUpPerms)
            {
                if (bot.Value)
                    sbSubtitle += enabledColor;
                else
                    sbSubtitle += disabledColor;
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
            node.subtitlePart.SetScale(0.5f, 0.5f);
            return node;
        }
    }
    public static class ShareMenuClass
    {
        public static Dictionary<uint, zMenu.zMenuNode> packNodesByID = new Dictionary<uint, zMenu.zMenuNode>();

        public static void setUpPackNodes(zMenu menu)
        {
            var resourceDataBlocks = ItemSpawnManager.m_itemDataPerInventorySlot[(int)InventorySlot.ResourcePack];
            foreach (ItemDataBlock block in resourceDataBlocks)
            {
                uint itemID = ItemDataBlock.s_blockIDByName[block.name];
                string name = block.publicName;
                zMenu.zMenuNode node = menu.AddNode(name);
                node.AddListener(zMenuManager.nodeEvent.OnTapped, zSlideComputer.ToggleResourceSharePermission, itemID);
                node.AddListener(zMenuManager.nodeEvent.OnTapped, updateNodeThresholdDisplay, node, itemID);
                node.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, zSlideComputer.SetResourceSharePermission, itemID, true);
                node.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, zSlideComputer.SetResourceThreshold, itemID, 100);
                node.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, updateNodeThresholdDisplay, node, itemID);
                node.AddListener(zMenuManager.nodeEvent.WhileSelected, ChangeThresholdBasedOnMouseWheel, itemID, node, 5);
                menu.AddListener(zMenuManager.menuEvent.OnOpened, updateNodeThresholdDisplay, node, itemID);
                menu.centerNode.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, zSlideComputer.SetResourceThreshold, itemID, 100);
                menu.centerNode.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, zSlideComputer.SetResourceSharePermission, itemID, true);
                menu.centerNode.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, updateNodeThresholdDisplay, node, itemID);
                node.fullTextPart.SetScale(1f, 1f);
                node.subtitlePart.SetScale(0.75f, 0.75f);
                node.titlePart.SetScale(0.5f, 0.5f);
                packNodesByID[itemID] = node;
                if (menu.nodes.Count - 1 == (int)(resourceDataBlocks.Count/2))
                {
                    //TODO allow for rotation offsets or other modes for aranging nodes.
                    menu.AddNode("");
                }
            }
        }
        public static void updateNodeThresholdDisplay(zMenu.zMenuNode node, uint itemID)
        {
            if (zSlideComputer.GetResourceThreshold(itemID) == 100)
            {
                node.SetPrefix("");
                node.SetSuffix("");
            }
            else
            {
                node.SetPrefix("* ");
                node.SetSuffix(" *");
            }
            string hex = ColorUtility.ToHtmlStringRGB(GetThresholdColor(zSlideComputer.GetResourceThreshold(itemID)));
            node.SetSubtitle($"<color=#CC840066>[ </color><color=#{hex}>{zSlideComputer.GetResourceThreshold(itemID)}</color><color=#CC840066> ]</color>");
            if (zSlideComputer.enabledResourceShares[itemID])
                node.SetColor(zMenuManager.defaultColor);
            else
                node.SetColor(new Color(0.25f, 0f, 0f));
        }
        public static Color GetThresholdColor(float value)
        {
            // scale factor to dim colors
            float max = 0.25f;

            Color red = new Color(max, 0f, 0f);
            Color yellow = new Color(max, max, 0f);
            Color green = new Color(0f, max, 0f);

            if (value <= 40f) // 0 → 40: red → yellow
                return Color.Lerp(red, yellow, value / 40f);
            else // 40 → 100: yellow → green
                return Color.Lerp(yellow, green, (value - 40f) / 60f);
        }
        public static void ChangeThresholdBasedOnMouseWheel(uint itemID, zMenu.zMenuNode node, int increment = 10)
        {
            if (node == null || !node.gameObject.activeInHierarchy || !zSlideComputer.enabledResourceShares[itemID])
                return;
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            int normalizedScroll = (int)Mathf.Sign(scroll);
            if (scroll == 0f)
                return;
            float currentThreshold = zSlideComputer.GetResourceThreshold(itemID);
            zSlideComputer.SetResourceThreshold(itemID, Math.Clamp((int)currentThreshold + (normalizedScroll * increment), 0,100));
            updateNodeThresholdDisplay(node, itemID);
        }
    }
    public static class PermissionsMenuClass
    {
        public static Dictionary<uint,zMenu.zMenuNode> prioNodesByID = new Dictionary<uint,zMenu.zMenuNode>();
        public static void setUpItemNodes(zMenu menu)
        {
            zMenu.zMenuNode glowstickNode = null;
            foreach (var item in zSlideComputer.itemPrios)
            {
                uint itemID = item.Key;
                ItemDataBlock block = ItemDataBlock.s_blockByID[itemID];
                string publicName = block.publicName;
                zMenu.zMenuNode node = null;
                bool isGlowstick = zSlideComputer.shortGlowStickNames.Contains(publicName);
                if (isGlowstick)
                {
                    if (glowstickNode == null)
                    {
                        glowstickNode = menu.AddNode(zSlideComputer.shortGlowStickNames.FirstOrDefault());
                        prioNodesByID[itemID] = glowstickNode;
                    }
                    node = glowstickNode;
                }
                else
                {
                    node = menu.AddNode(publicName);
                    prioNodesByID[itemID] = node;
                }
                node.AddListener(zMenuManager.nodeEvent.WhileSelected, ChangePrioBasedOnMouseWheel, itemID, node);
                node.AddListener(zMenuManager.nodeEvent.OnTapped, zSlideComputer.ToggleItemPrioDisabled, itemID);
                node.AddListener(zMenuManager.nodeEvent.OnTapped, updateNodePriorityDisplay, node, itemID);//TODO make these args order consistant.
                node.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, zSlideComputer.ResetItemPrio, itemID);
                node.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, zSlideComputer.SetItemPrioDisabled, itemID, true);
                node.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, updateNodePriorityDisplay, node, itemID);
                menu.centerNode.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, zSlideComputer.SetItemPrioDisabled, itemID, true);
                menu.centerNode.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, updateNodePriorityDisplay, node, itemID);
                menu.AddListener(zMenuManager.menuEvent.OnOpened, updateNodePriorityDisplay, node, itemID);
                node.fullTextPart.SetScale(1f, 1f);
                node.subtitlePart.SetScale(0.75f, 0.75f);
                node.titlePart.SetScale(0.5f, 0.5f);
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
            if (zSlideComputer.enabledItemPrios[itemID])
                node.SetColor(zMenuManager.defaultColor);
            else
                node.SetColor(new Color(0.25f,0f,0f));
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
        public static void ChangePrioBasedOnMouseWheel(uint itemID, zMenu.zMenuNode node, int increment = 10)
        {
            if (node == null || !node.gameObject.activeInHierarchy || !zSlideComputer.enabledItemPrios[itemID])
                return;
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            int normalizedScroll = (int)Mathf.Sign(scroll);
            if (scroll == 0f)
                return;
            float currentPrio = zSlideComputer.GetItemPrio(itemID);
            zSlideComputer.SetBotItemPriority(itemID, Mathf.Clamp(currentPrio + (normalizedScroll * increment),0,100));
            updateNodePriorityDisplay(node, itemID);
        }
    }
    public static class SelectionMenuClass
    {
        public static Dictionary<int, bool> botSelection = new();
        public static Dictionary<int, zMenu.zMenuNode> selectionBotNodes = new();
        public static Color selectedColor = new Color(0.25f, 0.16175f, 0.0f);
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
                node.SetColor(selectedColor);
            else
                node.SetColor(zMenuManager.defaultColor);
            return node;
        }
    }
}

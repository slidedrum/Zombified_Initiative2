using GameData;
using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using ZombieTweak2.zRootBotPlayerAction.CustomActions;
using Zombified_Initiative;

namespace ZombieTweak2.zMenu
{
    public static class zMenus
    {
        //This is the class that actually creates the menue instances
        //This is spesific to ZI
        //Very unfinished atm
        private static zMenu AutomaticActionsMenu;
        public static List<zMenu> botMenus;
        private static string endcolor = "</color>";
        private static string enabledColor = "<color=#FFA50066>";
        private static string disabledColor = "<color=#CCCCCC33>";

        public static void CreateMenus()
        {
            AutomaticActionsMenu = zMenuManager.createMenu("Automatic Actions", zMenuManager.mainMenu);
            AutomaticActionMenuClass.Setup(AutomaticActionsMenu);
            zMenuManager.createMenu("Manual Actions", zMenuManager.mainMenu);
            zMenuManager.createMenu("Contextual Actions", zMenuManager.mainMenu);
            zMenuManager.createMenu("Settings", zMenuManager.mainMenu);
            zMenuManager.createMenu("Voice menu", zMenuManager.mainMenu);
            zMenuManager.createMenu("Debug", zMenuManager.mainMenu);
        }
        [Obsolete]
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
                name = Regex.Replace(name, "<[^>]+>", "");
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
    public static class AutomaticActionMenuClass
    {
        public static List<zMenu> autoActionMenus = new List<zMenu>();
        private static zMenu AutoActionMenu;
        private static int catagoryIndex = 0;
        private static Dictionary<string, List<string>> catagories = new();
        internal static void Setup(zMenu menu)
        {
            AutoActionMenu = menu;
            AutoActionMenu.radius = 30f;
            //Vanilla actions
            autoActionMenus.Add(zMenuManager.createMenu("Tag Enemies", AutoActionMenu));
            autoActionMenus.Add(zMenuManager.createMenu("Attack", AutoActionMenu));
            autoActionMenus.Add(zMenuManager.createMenu("Revive", AutoActionMenu));
            autoActionMenus.Add(zMenuManager.createMenu("Bioscan", AutoActionMenu));
            var shareMenu = zMenuManager.createMenu("Share", AutoActionMenu);
            autoActionMenus.Add(shareMenu);
            autoActionMenus.Add(zMenuManager.createMenu("Ping", AutoActionMenu));
            autoActionMenus.Add(zMenuManager.createMenu("Enemy Scanner", AutoActionMenu));
            var pickupmenu = zMenuManager.createMenu("Pickup", AutoActionMenu);
            autoActionMenus.Add(pickupmenu);
            autoActionMenus.Add(zMenuManager.createMenu("Follow", AutoActionMenu));
            autoActionMenus.Add(zMenuManager.createMenu("Unlock", AutoActionMenu));
            //Custom actions
            var exploremenu = zMenuManager.createMenu("Explore", AutoActionMenu);
            autoActionMenus.Add(exploremenu);

            ExploreMenuClass.Setup(exploremenu);
            PickupMenuClass.Setup(pickupmenu);
            ShareMenuClass.Setup(shareMenu);

            catagories["Favorites"] = new();
            catagories["Favorites"].Add("Pickup");
            catagories["Favorites"].Add("Pickup");
            catagories["Favorites"].Add("Share");
            catagories["Favorites"].Add("Explore");
            catagories["Favorites"].Add("Follow");
            catagories["Resources"] = new();
            catagories["Resources"].Add("Pickup");
            catagories["Resources"].Add("Share");

            menu.centerNode.AddListener(zMenuManager.nodeEvent.WhileSelected, UpdateCatagoryByScroll);
            SetCatagory("Favorites");
            
        }
        private static void UpdateCatagoryByScroll()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            int normalizedScroll = (int)Mathf.Sign(scroll);
            if (scroll == 0f)
                return;
            catagoryIndex += (int)normalizedScroll;
            if (catagoryIndex > catagories.Count())
                catagoryIndex = 0;
            if (catagoryIndex < 0)
                catagoryIndex = catagories.Count();
            if (catagoryIndex == catagories.Count())
            {
                SetCatagory("All");
                return;
            }
            SetCatagory(catagories.Keys.ElementAt(catagoryIndex));
        }

        internal static void SetCatagory(string catagory)
        {
            if (catagory == "All")
            {
                foreach (var node in AutoActionMenu.nodes)
                {
                    AutoActionMenu.EnableNode(node);
                }
                SetSubtitle("All");
                return;
            }
            SetSubtitle(catagory);
            if (!catagories.ContainsKey(catagory))
            {
                ZiMain.log.LogError($"Invalid auto action catagory: {catagory}.");
                return;
            }
            foreach (var node in AutoActionMenu.nodes)
            {
                if (catagories[catagory].Contains(node.text))
                    AutoActionMenu.EnableNode(node);
                else
                    AutoActionMenu.DisableNode(node);
            }
        }
        private static void SetSubtitle(String subtitle)
        {
            AutoActionMenu.centerNode.SetSubtitle($"<color=#CC840066>[ </color>{subtitle}<color=#CC840066> ]</color>");
        }
    }
    public static class ExploreMenuClass
    {
        private static zMenu exploreMenu;
        private static zMenu.zMenuNode exploreNode;
        internal static void Setup(zMenu menu)
        {
            exploreMenu = menu;
            exploreNode = exploreMenu.parrentMenu.GetNode(exploreMenu.centerNode.text);
            exploreNode.ClearListeners(zMenuManager.nodeEvent.OnUnpressedSelected);
            exploreNode.AddListener(zMenuManager.nodeEvent.OnTapped, ToggleExplorePerms);
            exploreNode.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, exploreMenu.Open);
        }
        private static void ToggleExplorePerms()
        {
            var bots = zSearch.GetAllBotAgents();
            foreach (var bot in bots)
            {
                ExploreAction.ToggleExplorePerm(bot);
            }
            if (ExploreAction.GetExplorePerm(bots[0]))
                exploreNode.SetColor(zMenuManager.defaultColor);
            else
                exploreNode.SetColor(new Color(0.25f, 0f, 0f));
        }
    }
    public static class ShareMenuClass
    {
        public static Dictionary<uint, zMenu.zMenuNode> packNodesByID = new Dictionary<uint, zMenu.zMenuNode>();

        public static void Setup(zMenu menu)
        {
            var resourceDataBlocks = ItemSpawnManager.m_itemDataPerInventorySlot[(int)InventorySlot.ResourcePack];
            foreach (ItemDataBlock block in resourceDataBlocks)
            {
                uint itemID = ItemDataBlock.s_blockIDByName[block.name];
                string name = block.publicName;
                zMenu.zMenuNode node = menu.AddNode(name);
                //TODO uncomment then when moved over to overide system instead of selection system.
                //var thisNode = menu.parrentMenu.GetNode(menu.centerNode.text);
                //thisNode.ClearListeners(zMenuManager.nodeEvent.OnUnpressedSelected);
                //thisNode.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, menu.Open);
                //thisNode.AddListener(zMenuManager.nodeEvent.OnTapped, TogglePerms);
                node.AddListener(zMenuManager.nodeEvent.OnTapped, zSlideComputer.ToggleResourceSharePermission, itemID);
                node.AddListener(zMenuManager.nodeEvent.OnTapped, updateNodeThresholdDisplay, node, itemID);
                node.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, zSlideComputer.SetResourceSharePermission, itemID, true);
                node.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, zSlideComputer.SetResourceThreshold, itemID, 100);
                node.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, updateNodeThresholdDisplay, node, itemID);
                node.AddListener(zMenuManager.nodeEvent.WhileSelected, ChangeThresholdBasedOnMouseWheel, itemID, node, 5);
                menu.AddListener(zMenuManager.menuEvent.OnOpened, updateNodeThresholdDisplay, node, itemID);
                menu.centerNode.ClearListeners(zMenuManager.nodeEvent.OnUnpressedSelected);
                menu.centerNode.AddListener(zMenuManager.nodeEvent.OnTapped, menu.parrentMenu.Open);
                menu.centerNode.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, zSlideComputer.SetResourceThreshold, itemID, 100);
                menu.centerNode.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, zSlideComputer.SetResourceSharePermission, itemID, true);
                menu.centerNode.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, updateNodeThresholdDisplay, node, itemID);
                node.fullTextPart.SetScale(1f, 1f);
                node.subtitlePart.SetScale(0.75f, 0.75f);
                node.titlePart.SetScale(0.5f, 0.5f);
                packNodesByID[itemID] = node;
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
        public static void TogglePerms()
        {
            //TODO change the whole system to be based on overides, not selections.
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
    public static class CullingMenuClass
    {
        public static void setupCullingMenu(zMenu menu) 
        {
            for (int i = 0; i < 32; i++)
            {
                string name = LayerMask.LayerToName(i);
                if (string.IsNullOrEmpty(name))
                    continue;
                Camera camera = Camera.main;
                var node = menu.AddNode(name);
                node.AddListener(zMenuManager.nodeEvent.OnUnpressedSelected, ToggleLayer, camera, i, node);
            }
        }
        public static void ToggleLayer(Camera camera, int layer, zMenu.zMenuNode node)
        {
            if (camera == null)
            {
                Debug.LogWarning("Camera is null!");
                return;
            }

            if (layer < 0 || layer > 31)
            {
                Debug.LogWarning("Layer index out of range (0-31)!");
                return;
            }

            // XOR the bit for the layer to toggle it
            camera.cullingMask ^= 1 << layer;
            bool isVisible = (camera.cullingMask & (1 << layer)) != 0;
            if (isVisible)
                node.SetColor(new Color(0, 0.2f, 0));
            else
                node.SetColor(new Color(0.2f, 0, 0));
        }
    }
    public static class DebugMenuClass
    {
        public enum DebugValueToChange
        {
            NodeGridSize,
            NodeMapSize,
            NodeVisitDistance,
            PropigationAmmount,
            PropigationSampleCount,
            NodesCreatedPerFrame,
            connectionChecksPerFrame,
        }
        public static void ChangeValueBasedOnMouseWheel(DebugValueToChange valueToChange, zMenu.zMenuNode node, float increment = 0.1f)
        {
            if (node == null || !node.gameObject.activeInHierarchy)
                return;
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            int normalizedScroll = (int)Mathf.Sign(scroll);
            if (scroll == 0f)
                return;
            float offset = normalizedScroll * increment;
            float value = 0;
            switch (valueToChange)
            {
                case DebugValueToChange.NodeMapSize:
                    zVisitedManager.SetNodeMapGridSize((int)offset + zVisitedManager.NodeMapGridSize);
                    value = zVisitedManager.NodeMapGridSize;
                    break;
                case DebugValueToChange.NodeGridSize:
                    zVisitedManager.SetNodeGridSize(offset + zVisitedManager.NodeGridSize);
                    value = zVisitedManager.NodeGridSize;
                    break;
                case DebugValueToChange.NodeVisitDistance:
                    zVisitedManager.SetNodeVisitDistance(offset + zVisitedManager.NodeVisitDistance);
                    value = zVisitedManager.NodeVisitDistance;
                    break;
                case DebugValueToChange.PropigationAmmount:
                    zVisitedManager.SetPropigationAmmount((int)offset + zVisitedManager.propigationAmmount);
                    value = zVisitedManager.propigationAmmount;
                    break;
                case DebugValueToChange.PropigationSampleCount:
                    zVisitedManager.SetPropigationSampleCount((int)offset + zVisitedManager.propigationSampleCount);
                    value = zVisitedManager.propigationSampleCount;
                    break;
                case DebugValueToChange.NodesCreatedPerFrame:
                    zVisitedManager.nodesCreatedPerFrame = Math.Max((int)offset + zVisitedManager.nodesCreatedPerFrame,1);
                    value = zVisitedManager.nodesCreatedPerFrame;
                    break;
                case DebugValueToChange.connectionChecksPerFrame:
                    zVisitedManager.connectionChecksPerFrame = Math.Max((int)offset + zVisitedManager.connectionChecksPerFrame,1);
                    value = zVisitedManager.connectionChecksPerFrame;
                    break;
                default:
                    Debug.LogWarning("Unknown DebugValueToChange: " + valueToChange);
                    break;
            }
            node.SetSubtitle($"{value}");
        }
    }
    public static class PickupMenuClass
    {
        public static Dictionary<uint,zMenu.zMenuNode> prioNodesByID = new Dictionary<uint,zMenu.zMenuNode>();
        private static Dictionary<string, List<string>> catagories = new();
        private static int catagoryIndex = 1;
        private static zMenu pickupMenu;
        public static void Setup(zMenu menu)
        {
            pickupMenu = menu;
            pickupMenu.radius = 25f;
            //ALL - ENCOUNTERED - RESOURCES - PLACEABLES - THROWABLES - FAVORITES
            catagories["Encountered"] = new();
            catagories["Resources"]   = new();
            catagories["Placeables"]  = new();
            catagories["Throwables"]  = new();
            catagories["Favorites"]   = new();
            catagories["Resources"] .Add("MediPack");
            catagories["Resources"] .Add("Ammo Pack");
            catagories["Resources"] .Add("Tool Refill Pack");
            catagories["Resources"] .Add("Disinfection Pack");
            catagories["Placeables"].Add("Lock Melter");
            catagories["Placeables"].Add("Explosive Trip Mine");
            catagories["Placeables"].Add("C-Foam Tripmine");
            catagories["Throwables"].Add("Glow Stick");
            catagories["Throwables"].Add("Fog Repeller");
            catagories["Throwables"].Add("C-Foam Grenade");
            catagories["Favorites"] .Add("MediPack");
            catagories["Favorites"] .Add("Ammo Pack");
            catagories["Favorites"] .Add("Tool Refill Pack");
            catagories["Favorites"] .Add("Disinfection Pack");
            catagories["Favorites"] .Add("C-Foam Grenade");

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
                        glowstickNode = pickupMenu.AddNode(zSlideComputer.shortGlowStickNames.FirstOrDefault());
                        prioNodesByID[itemID] = glowstickNode;
                    }
                    node = glowstickNode;
                }
                else
                {
                    node = pickupMenu.AddNode(publicName);
                    prioNodesByID[itemID] = node;
                }
                //TODO uncomment then when moved over to overide system instead of selection system.
                //var thisNode = menu.parrentMenu.GetNode(menu.centerNode.text);
                //thisNode.ClearListeners(zMenuManager.nodeEvent.OnUnpressedSelected);
                //thisNode.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, menu.Open);
                //thisNode.AddListener(zMenuManager.nodeEvent.OnTapped, TogglePerms);
                node.AddListener(zMenuManager.nodeEvent.WhileSelected, ChangePrioBasedOnMouseWheel, itemID, node);
                node.AddListener(zMenuManager.nodeEvent.OnTapped, zSlideComputer.ToggleItemPrioDisabled, itemID);
                node.AddListener(zMenuManager.nodeEvent.OnTapped, updateNodePriorityDisplay, node, itemID);//TODO make these args order consistant.
                node.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, ResetNodeSettings, itemID, node);
                pickupMenu.centerNode.ClearListeners(zMenuManager.nodeEvent.OnUnpressedSelected);
                pickupMenu.centerNode.AddListener(zMenuManager.nodeEvent.OnTapped, pickupMenu.parrentMenu.Open);
                pickupMenu.centerNode.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, ResetNodeSettings, itemID, node);
                pickupMenu.AddListener(zMenuManager.menuEvent.OnOpened, updateNodePriorityDisplay, node, itemID);
                node.fullTextPart.SetScale(1f, 1f);
                node.subtitlePart.SetScale(0.75f, 0.75f);
                node.titlePart.SetScale(0.5f, 0.5f);
                node.SetSize(0.75f);
            }
            pickupMenu.centerNode.AddListener(zMenuManager.nodeEvent.WhileSelected, UpdateCatagoryByScroll);
            SetCatagory("All");
        }
        internal static void Encounter(string friendlyName)
        {
            if (!catagories["Encountered"].Contains(friendlyName)) 
            {
                catagories["Encountered"].Add(friendlyName);
                if (catagoryIndex < catagories.Count() && catagories.Keys.ElementAt(catagoryIndex) == "Encountered")
                    SetCatagory("Encountered");
            }
        }
        private static void ResetNodeSettings(uint itemID, zMenu.zMenuNode node)
        {
            if (!node.gameObject.activeInHierarchy)
                return;
            zSlideComputer.ResetItemPrio(itemID);
            zSlideComputer.SetItemPrioDisabled(itemID, true);
            updateNodePriorityDisplay(node, itemID);
        }
        private static void UpdateCatagoryByScroll()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            int normalizedScroll = (int)Mathf.Sign(scroll);
            if (scroll == 0f)
                return;
            catagoryIndex += (int)normalizedScroll;
            if (catagoryIndex > catagories.Count())
                catagoryIndex = 0;
            if (catagoryIndex < 0)
                catagoryIndex = catagories.Count();
            if (catagoryIndex == catagories.Count())
            {
                SetCatagory("All");
                return;
            }
            SetCatagory(catagories.Keys.ElementAt(catagoryIndex));
        }
        internal static void SetCatagory(string catagory)
        {
            if (catagory == "All")
            {
                foreach (var node in pickupMenu.nodes)
                {
                    pickupMenu.EnableNode(node);
                }
                SetSubtitle("All");
                return;
            }
            SetSubtitle(catagory);
            if (!catagories.ContainsKey(catagory))
            {
                ZiMain.log.LogError($"Invalid auto action catagory: {catagory}.");
                return;
            }
            foreach (var node in pickupMenu.nodes)
            {
                if (catagories[catagory].Contains(node.text))
                    pickupMenu.EnableNode(node);
                else
                    pickupMenu.DisableNode(node);
            }
        }
        private static void SetSubtitle(String subtitle)
        {
            pickupMenu.centerNode.SetSubtitle($"<color=#CC840066>[ </color>{subtitle}<color=#CC840066> ]</color>");
        }
        public static void TogglePerms()
        {
            //TODO change the whole system to be based on overides, not selections.
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
    [Obsolete]
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

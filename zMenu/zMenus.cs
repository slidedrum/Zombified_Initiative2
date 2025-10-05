using CollisionRundown.Features.HUDs;
using GameData;
using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using ZombieTweak2.zRootBotPlayerAction.CustomActions;
using Zombified_Initiative;
using static Il2CppSystem.Linq.Expressions.Interpreter.NullableMethodCallInstruction;
using static ZombieTweak2.zMenu.DebugMenuClass;

namespace ZombieTweak2.zMenu
{
    public static class zMenus
    {
        //This is the class that actually creates the menue instances
        //This is spesific to ZI
        //Very unfinished atm

        public static List<zMenu> botMenus;

        
        private static zMenu selectionMenu;
        private static zMenu debugMenu;
        private static zMenu debugNodeMenu;
        private static zMenu debugNodeSettingsMenu;
        private static zMenu actionMenu;
        public static zMenu permissionMenu;
        private static zMenu pickupDetailsSubmenu;
        private static zMenu shareDetailsSubmenu;
        private static zMenu debugCameraCullingMenu;
        private static string endcolor = "</color>";
        private static string enabledColor = "<color=#FFA50066>";
        private static string disabledColor = "<color=#CCCCCC33>";

        public static void CreateMenus()
        {
            zMenuManager.createMenu("Automatic Actions", zMenuManager.mainMenu);
            zMenuManager.createMenu("Manual Actions", zMenuManager.mainMenu);
            zMenuManager.createMenu("Contextual Actions", zMenuManager.mainMenu);
            zMenuManager.createMenu("Settings", zMenuManager.mainMenu);
            zMenuManager.createMenu("Voice menu", zMenuManager.mainMenu);
            zMenuManager.createMenu("Debug", zMenuManager.mainMenu);
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
                node.SetSize(0.75f);
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

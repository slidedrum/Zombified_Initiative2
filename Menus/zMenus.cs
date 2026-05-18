using Player;
using SlideMenu;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
//using ZombieTweak2.zRootBotPlayerAction.CustomActions;
using Zombified_Initiative;

namespace ZombieTweak2.Menus
{
    public static class zMenus
    {
        //This is the class that actually creates the menue instances
        public static List<sMenu> botMenus;
        private static string endcolor = "</color>";
        private static string enabledColor = "<color=#FFA50066>";
        private static string disabledColor = "<color=#CCCCCC33>";

        public static void CreateMenus()
        {
            AutomaticActionMenuClass.Setup(sMenuManager.createMenu("Automatic Actions", sMenuManager.mainMenu));
            if (ZiMain.extraActionMenus) 
            { 
                ManualActionMenuClass.Setup(sMenuManager.createMenu("Manual Actions", sMenuManager.mainMenu));
                sMenuManager.createMenu("Contextual Actions", sMenuManager.mainMenu);
            }
            SettingsMenuClass.Setup(sMenuManager.createMenu("Settings", sMenuManager.mainMenu));
            if (ZiMain.VoiceMenu)
                sMenuManager.createMenu("Voice menu", sMenuManager.mainMenu);
            if (ZiMain.debugMode)
                DebugMenuClass.Setup(sMenuManager.createMenu("Debug", sMenuManager.mainMenu));
        }
    }
    public static class ManualActionMenuClass
    {
        public static sMenu manualActionMenu;
        public static sMenu.sMenuNode manuActionNode;
        public static void Setup(sMenu menu)
        {
            manualActionMenu = menu;
            manuActionNode = manualActionMenu.GetNode();
            manualActionMenu.AddNode("Clear Room", StartClearRoomAction);
        }
        public static void StartClearRoomAction()
        {
            return;
            //PlayerAgent playerAgent = PlayerManager.GetLocalPlayerAgent();
            //PlayerAIBot bot = null;
            //PlayerAgent botAgent = null;
            //float closestDistance = float.MaxValue;
            //foreach (var agent in PlayerManager.PlayerAgentsInLevel)
            //{
            //    if (!agent.Owner.IsBot || !agent.Alive)
            //        continue;
            //    float distance = Vector3.Distance(agent.Position, playerAgent.Position);
            //    if (distance < closestDistance)
            //    {
            //        closestDistance = distance;
            //        botAgent = agent;
            //        bot = agent.gameObject.GetComponent<PlayerAIBot>();
            //    }
            //}
            //var action = new ClearRoomAction.Descriptor(bot);
            //bot.StartAction(action);
        }
    }
    public static partial class AutomaticActionMenuClass
    {
        public static List<sMenu> autoActionMenus = new List<sMenu>();
        public static sMenu AutoActionMenu;
        public static Dictionary<string, OverrideTree<float?>> ActionPriorities = new();
        internal static void Setup(sMenu _menu)
        {
            AutoActionMenu = _menu;
            AutoActionMenu.radius = 130f;
            //Vanilla actions
            var bioTrackerMenu = sMenuManager.createMenu("Use BioTracker", AutoActionMenu);
            autoActionMenus.Add(bioTrackerMenu);
            var attackMenu = sMenuManager.createMenu("Attack", AutoActionMenu);
            autoActionMenus.Add(attackMenu);
            var reviveMenu = sMenuManager.createMenu("Revive", AutoActionMenu);
            autoActionMenus.Add(reviveMenu);
            //sMenu.sMenuNode node = reviveMenu.GetNode();
            var shareMenu = sMenuManager.createMenu("Share", AutoActionMenu);
            autoActionMenus.Add(shareMenu);
            var pingMenu = sMenuManager.createMenu("Ping", AutoActionMenu);
            autoActionMenus.Add(pingMenu);
            var pickupMenu = sMenuManager.createMenu("Pickup", AutoActionMenu);
            autoActionMenus.Add(pickupMenu);
            var followMenu = sMenuManager.createMenu("Follow", AutoActionMenu);
            autoActionMenus.Add(followMenu);
            //node.AddListener(sMenuManager.nodeEvent.WhileSelected, GenericUpdatePriorityBasedOnScroll, node);
            //node.AddListener(sMenuManager.nodeEvent.OnHeldImmediateSelected, GenericResetPrioSettings, node);
            var unlockMenu = sMenuManager.createMenu("Unlock", AutoActionMenu);
            autoActionMenus.Add(unlockMenu);

            AutoActionMenu.centerNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
            AutoActionMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnTapped, AutoActionMenu.parrentMenu.Open);

            zSlideComputer.PermissionDefinitions.CreatePermissionDeffinition("Revive", true, reviveMenu.GetNode(), typeof(PlayerBotActionRevive), 12);
            zSlideComputer.PermissionDefinitions.CreatePermissionDeffinition("Use BioTracker", true, bioTrackerMenu.GetNode(), typeof(PlayerBotActionUseEnemyScanner));
            zSlideComputer.PermissionDefinitions.CreatePermissionDeffinition("Attack", true, attackMenu.GetNode(), typeof(PlayerBotActionAttack));
            zSlideComputer.PermissionDefinitions.CreatePermissionDeffinition("Share", true, shareMenu.GetNode(), typeof(PlayerBotActionShareResourcePack), 10);
            zSlideComputer.PermissionDefinitions.CreatePermissionDeffinition("Ping", true, pingMenu.GetNode(), typeof(PlayerBotActionHighlight), 4.3f);
            zSlideComputer.PermissionDefinitions.CreatePermissionDeffinition("Follow", true, followMenu.GetNode(), typeof(PlayerBotActionFollow), 14f);
            zSlideComputer.PermissionDefinitions.CreatePermissionDeffinition("Pickup", true, pickupMenu.GetNode(), typeof(PlayerBotActionFollow), 4.2f);
            zSlideComputer.PermissionDefinitions.CreatePermissionDeffinition("Unlock", true, unlockMenu.GetNode(), typeof(PlayerBotActionUnlock), 4.1f);
            zSlideComputer.PermissionDefinitions.CreatePermissionDeffinition("Move", true, null, typeof(PlayerBotActionWalk));

            List<PlayerAIBot> playerAiBots = ZiMain.GetBotList();
                
            foreach (sMenu menu in autoActionMenus)
            {
                sMenu.sMenuNode node = menu.GetNode();
                string text = node.text;
                node.AddListener(sMenuManager.nodeEvent.OnTapped, GenericToggleAllowed, text, menu);
                AutoActionMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediateSelected, GenericSetAllowed, text, true, -1, menu);
                float defaultPiority = zSlideComputer.PermissionDefinitions.GetDefaultPriority(text);
                if (defaultPiority > 0f)
                {
                    node.AddListener(sMenuManager.nodeEvent.OnHeldImmediateSelected, GenericResetPrioSettings, node);
                    AutoActionMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediateSelected, GenericResetPrioSettings, node);
                    ActionPriorities.Add(text, new OverrideTree<float?>(defaultPiority).AddNode(text, null).Tree);
                    node.AddListener(sMenuManager.nodeEvent.WhileSelected, GenericUpdatePriorityBasedOnScroll, node);
                    GenericUpdateNodePrioDisplay(node);
                }
                node.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
                node.AddListener(sMenuManager.nodeEvent.OnDoubleTapped, menu.Open);
            }
            

            //Custom actions
            if (ZiMain.customActions)
            {
                var exploremenu = sMenuManager.createMenu("Explore", AutoActionMenu);
                autoActionMenus.Add(exploremenu);
                ExploreMenuClass.Setup(exploremenu);
            }

            AutoActionMenu.AddPannel(sMenu.sMenuPannel.Side.bottom, "Scroll in center => change catagory");
            AutoActionMenu.AddPannel(sMenu.sMenuPannel.Side.bottom, "Scroll on node => change priority");
            AutoActionMenu.AddPannel(sMenu.sMenuPannel.Side.right, "Tap => toggle");
            AutoActionMenu.AddPannel(sMenu.sMenuPannel.Side.right, "Double tap => submenu");
            AutoActionMenu.AddPannel(sMenu.sMenuPannel.Side.right, "Hold => reset");

            PickupMenuClass.Setup(pickupMenu);
            ShareMenuClass.Setup(shareMenu);
            FollowMenuClass.Setup(followMenu);
            UnlockMenuClass.Setup(unlockMenu);
            PingMenuClass.Setup(pingMenu);
            BioTrackerMenuClass.Setup(bioTrackerMenu);
            AttackMenuClass.Setup(attackMenu);
            ReviveMenuClass.Setup(reviveMenu);

            AutoActionMenu.AddCatagory("All");
            AutoActionMenu.AddCatagory("Favorites");
            AutoActionMenu.AddNodeToCatagory("Favorites", "Pickup");
            AutoActionMenu.AddNodeToCatagory("Favorites", "Share");
            AutoActionMenu.AddNodeToCatagory("Favorites", "Explore");
            AutoActionMenu.AddNodeToCatagory("Favorites", "Follow");
            AutoActionMenu.AddCatagory("Resources");
            AutoActionMenu.AddNodeToCatagory("Resources", "Pickup");
            AutoActionMenu.AddNodeToCatagory("Resources", "Share");
            AutoActionMenu.SetCatagory("Favorites");
        }
        internal static void GenericToggleAllowed(string actionKey, sMenu menu = null, sMenu.sMenuNode node = null, int playerID = -1, bool allowDissabled = false)
        {
            bool allowed = !zSlideComputer.PermissionDefinitions.GetAllowed(actionKey, playerID == -1 ? 1 : playerID);
            GenericSetAllowed(actionKey, allowed, playerID, menu, node, allowDissabled);
        }
        internal static void GenericSetAllowed(string actionKey, bool allowed, int playerID = -1, sMenu menu = null, sMenu.sMenuNode node = null, bool allowDissabled = false)
        {
            
            //sMenu menu = autoActionMenus.FirstOrDefault(menu => menu.centerNode.text == actionKey);
            if (node == null && menu == null)
            {
                zSlideComputer.SetActionPermission(actionKey, allowed, playerID);
                return;
            }
            if (node == null && menu != null)
                node = menu.GetNode();
            if (!node.gameObject.activeInHierarchy && !allowDissabled)
                return;
            zSlideComputer.SetActionPermission(actionKey, allowed, playerID);
            if (allowed)
            {
                node.SetColor(sMenuManager.defaultColor);
                menu?.centerNode.SetColor(sMenuManager.defaultColor);
            }
            else
            {
                node.SetColor(new Color(0.25f, 0f, 0f));
                menu?.centerNode.SetColor(new Color(0.25f, 0f, 0f));
            }
        }
        internal static void GenericUpdatePriorityBasedOnScroll(sMenu.sMenuNode node)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll == 0f)
                return;
            float normalizedScroll = (int)Mathf.Sign(scroll) * 0.1f;
            string text = node.text;
            OverrideTree<float?> prio = ActionPriorities[text];
            prio.SetValue(text, Math.Clamp(Mathf.Round(((float)prio.ValueAt(text) + normalizedScroll) * 10f) / 10f,1f,15f));
            GenericUpdateNodePrioDisplay(node);
        }
        private static void GenericUpdateNodePrioDisplay(sMenu.sMenuNode node)
        {
            string text = node.text;
            OverrideTree<float?> prio = ActionPriorities[text];
            if (prio.nodes[text].IsDefaultValue())
            {
                node.SetPrefix("");
                node.SetSuffix("");
            }
            else
            {
                node.SetPrefix("* ");
                node.SetSuffix(" *");
            }
            node.SetTitle($"Prio <color=#CC840066>[</color>{prio.ValueAt(text)}<color=#CC840066>]</color>");
        }
        private static void GenericResetPrioSettings(sMenu.sMenuNode node)
        {
            if (!node.gameObject.activeInHierarchy)
                return;
            string text = node.text;
            OverrideTree<float?> prio = ActionPriorities[text];
            prio.SetValue(text, null);
            GenericUpdateNodePrioDisplay(node);
        }
    }


    [Obsolete]
    public static class SelectionMenuClass
    {
        public static Dictionary<int, bool> botSelection = new();
        public static Dictionary<int, sMenu.sMenuNode> selectionBotNodes = new();
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
        public static sMenu.sMenuNode updateColorBaesdOnSelection(sMenu.sMenuNode node, PlayerAIBot bot)
        {
            if (checkForUntrackedBot(bot))
                return null;
            if (botSelection[bot.Agent.Owner.PlayerSlotIndex()])
                node.SetColor(selectedColor);
            else
                node.SetColor(sMenuManager.defaultColor);
            return node;
        }
    }
}

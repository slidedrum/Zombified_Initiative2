using CollisionRundown.Features.HUDs;
using GameData;
using Player;
using SlideMenu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using UnityEngine;
using UnityEngine.UIElements;
using ZombieTweak2.CustomActions.Patches;
using ZombieTweak2.zRootBotPlayerAction;
//using ZombieTweak2.zRootBotPlayerAction.CustomActions;
using Zombified_Initiative;

namespace ZombieTweak2
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
    public static class AutomaticActionMenuClass
    {
        public static List<sMenu> autoActionMenus = new List<sMenu>();
        private static sMenu AutoActionMenu;
        public static Dictionary<string, OverrideTree<float?>> ActionPriorities = new();
        public static Dictionary<string, List<Type>> actionMap = new() 
        {
            {"Use BioTracker", new List<Type> { typeof(PlayerBotActionUseEnemyScanner)} },
            {"Attack", new List<Type> { typeof(PlayerBotActionAttack) } },
            {"Revive", new List<Type> { typeof(PlayerBotActionRevive) } },
            {"Share", new List<Type> { typeof(PlayerBotActionShareResourcePack) } },
            {"Ping", new List<Type> { typeof(PlayerBotActionHighlight) } },
            {"Pickup", new List<Type> { typeof(PlayerBotActionCollectItem) } },
            {"Follow", new List<Type> { typeof(PlayerBotActionFollow) } },
            {"Unlock", new List<Type> { typeof(PlayerBotActionUnlock) } }
        };
        public static Dictionary<string, float> defaultPrios = new Dictionary<string, float>
            {
                { "Revive", 12f },
                { "Share", 10f },
                { "Ping", 4.3f },
                { "Pickup", 4.2f },
                { "Follow", 14f },
                { "Unlock", 4.1f }
            };
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

            List<PlayerAIBot> playerAiBots = ZiMain.GetBotList();
            foreach (sMenu menu in autoActionMenus)
            {
                sMenu.sMenuNode node = menu.GetNode();
                string text = node.text;
                zSlideComputer.perms.Add(text, new Dictionary<int, bool>());
                foreach (PlayerAIBot bot in playerAiBots)
                    zSlideComputer.perms[text].Add(bot.Agent.Owner.PlayerSlotIndex(), true);
                if (!defaultPrios.ContainsKey(text))
                    continue;
                if (text != "Follow")
                {
                    node.AddListener(sMenuManager.nodeEvent.WhileSelected, GenericUpdatePriorityBasedOnScroll, node);
                    node.AddListener(sMenuManager.nodeEvent.OnHeldImmediateSelected, GenericResetPrioSettings, node);
                    AutoActionMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediateSelected, GenericResetPrioSettings, node);
                }
                else
                    AutoActionMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediateSelected, FollowMenuClass.ResetSettings, node);
                ActionPriorities.Add(text, new OverrideTree<float?>(defaultPrios[text]).AddNode(text, null).Tree);
                node.AddListener(sMenuManager.nodeEvent.WhileSelected, GenericUpdatePriorityBasedOnScroll, node);
                node.AddListener(sMenuManager.nodeEvent.OnHeldImmediateSelected, GenericResetPrioSettings, node);
                node.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
                node.AddListener(sMenuManager.nodeEvent.OnDoubleTapped, menu.Open);
                GenericUpdateNodePrioDisplay(node);
            }
            

            //Custom actions
            if (ZiMain.customActions)
            {
                var exploremenu = sMenuManager.createMenu("Explore", AutoActionMenu);
                autoActionMenus.Add(exploremenu);
                ExploreMenuClass.Setup(exploremenu);
            }

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

            AutoActionMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, PickupMenuClass.setAllowed, true);
            AutoActionMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, ShareMenuClass.setAllowed, true);
            AutoActionMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, FollowMenuClass.setAllowed, true);
            AutoActionMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, UnlockMenuClass.setAllowed, true);
            AutoActionMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, PingMenuClass.setAllowed, true);
            AutoActionMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, BioTrackerMenuClass.setAllowed, true);
            AutoActionMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, AttackMenuClass.setAllowed, true);
            AutoActionMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, ReviveMenuClass.setAllowed, true);

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
            string text = node.text;
            OverrideTree<float?> prio = ActionPriorities[text];
            prio.SetValue(text, null);
            GenericUpdateNodePrioDisplay(node);
        }
        public static class ReviveMenuClass
        {
            public static sMenu reviveMenu;
            public static sMenu.sMenuNode reviveNode;
            public static sMenu overidesMenu;
            public static sMenu.sMenuNode playersNode;
            public static sMenu.sMenuNode botsNode;
            public static Dictionary<string,sMenu.sMenuNode> overideNodes = new();
            public static void Setup(sMenu menu)
            {
                reviveMenu = menu;
                reviveNode = menu.GetNode();
                reviveNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
                reviveNode.AddListener(sMenuManager.nodeEvent.OnTapped, togglePerms);
                reviveNode.AddListener(sMenuManager.nodeEvent.OnDoubleTapped, reviveMenu.Open);
                playersNode = reviveMenu.AddNode("Players", togglePlayerPerms);
                botsNode = reviveMenu.AddNode("Bots", toggleBotPerms);
                //overidesMenu = new sMenu("overrides", reviveMenu);
                //reviveMenu.AddNode(overidesMenu);
                //var playerAgents = PlayerManager.PlayerAgentsInLevel;
                //foreach (PlayerAgent agent in playerAgents)
                //{
                //    string name = agent.PlayerName;
                //    overideNodes[name] = overidesMenu.AddNode(name, toggleOveridePerms, name);
                //}
            }
            public static void togglePerms()
            {
                bool allowed = true;
                foreach (var bot in zSearch.GetAllBotAgents())
                {
                    allowed = !zSlideComputer.GetActionPermission("Revive", bot.PlayerSlotIndex); //This is a bad way to do it, i'm setting it multiple times.  But at least untill I make it so you can have different perms for different bots, it should all be fine!
                    zSlideComputer.SetActionPermission("Revive", bot.PlayerSlotIndex, allowed);
                }
                if (allowed)
                {
                    reviveNode.SetColor(sMenuManager.defaultColor);
                    reviveMenu.centerNode.SetColor(sMenuManager.defaultColor);
                }
                else
                {
                    reviveNode.SetColor(new Color(0.25f, 0f, 0f));
                    reviveMenu.centerNode.SetColor(new Color(0.25f, 0f, 0f));
                }
            }
            public static void setAllowed(bool allowed)
            {
                foreach (var bot in zSearch.GetAllBotAgents())
                {
                    zSlideComputer.SetActionPermission("Revive", bot.PlayerSlotIndex, allowed);
                }
                if (allowed)
                {
                    reviveNode.SetColor(sMenuManager.defaultColor);
                    reviveMenu.centerNode.SetColor(sMenuManager.defaultColor);
                }
                else
                {
                    reviveNode.SetColor(new Color(0.25f, 0f, 0f));
                    reviveMenu.centerNode.SetColor(new Color(0.25f, 0f, 0f));
                }
            }
            public static void togglePlayerPerms()
            {
                bool allowed = true;
                foreach (var bot in zSearch.GetAllBotAgents())
                {
                    allowed = !zSlideComputer.GetRevivePlayersPermission(bot.PlayerSlotIndex); //This is a bad way to do it, i'm setting it multiple times.  But at least untill I make it so you can have different perms for different bots, it should all be fine!
                    zSlideComputer.SetRevivePlayersPermission(bot.PlayerSlotIndex, allowed);
                }
                if (allowed)
                {
                    playersNode.SetColor(sMenuManager.defaultColor);
                }
                else
                {
                    playersNode.SetColor(new Color(0.25f, 0f, 0f));
                }
            }
            public static void toggleBotPerms()
            {
                bool allowed = true;
                foreach (var bot in zSearch.GetAllBotAgents())
                {
                    allowed = !zSlideComputer.GetReviveBotsPermission(bot.PlayerSlotIndex); //This is a bad way to do it, i'm setting it multiple times.  But at least untill I make it so you can have different perms for different bots, it should all be fine!
                    zSlideComputer.SetReviveBotsPermission(bot.PlayerSlotIndex, allowed);
                }
                if (allowed)
                {
                    botsNode.SetColor(sMenuManager.defaultColor);
                }
                else
                {
                    botsNode.SetColor(new Color(0.25f, 0f, 0f));
                }
            }
        }
        public static class AttackMenuClass
        {
            public static sMenu attackMenu;
            public static sMenu.sMenuNode attackNode;
            public static void Setup(sMenu menu)
            {
                attackMenu = menu;
                attackNode = menu.GetNode();
                attackNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
                attackNode.AddListener(sMenuManager.nodeEvent.OnTapped, togglePerms);
                attackNode.AddListener(sMenuManager.nodeEvent.OnDoubleTapped, attackMenu.Open);
            }
            public static void togglePerms()
            {
                bool allowed = true;
                foreach (var bot in zSearch.GetAllBotAgents())
                {
                    allowed = !zSlideComputer.GetActionPermission("Attack", bot.PlayerSlotIndex); //This is a bad way to do it, i'm setting it multiple times.  But at least untill I make it so you can have different perms for different bots, it should all be fine!
                    zSlideComputer.SetActionPermission("Attack", bot.PlayerSlotIndex, allowed);
                }
                if (allowed)
                {
                    attackNode.SetColor(sMenuManager.defaultColor);
                    attackMenu.centerNode.SetColor(sMenuManager.defaultColor);
                }
                else
                {
                    attackNode.SetColor(new Color(0.25f, 0f, 0f));
                    attackMenu.centerNode.SetColor(new Color(0.25f, 0f, 0f));
                }
            }
            public static void setAllowed(bool allowed)
            {
                foreach (var bot in zSearch.GetAllBotAgents())
                {
                    zSlideComputer.SetActionPermission("Attack", bot.PlayerSlotIndex, allowed);
                }
                if (allowed)
                {
                    attackNode.SetColor(sMenuManager.defaultColor);
                    attackMenu.centerNode.SetColor(sMenuManager.defaultColor);
                }
                else
                {
                    attackNode.SetColor(new Color(0.25f, 0f, 0f));
                    attackMenu.centerNode.SetColor(new Color(0.25f, 0f, 0f));
                }
            }
        }
        public static class BioTrackerMenuClass
        {
            public static sMenu bioTrackerMenu;
            public static sMenu.sMenuNode bioTrackerNode;
            public static void Setup(sMenu menu)
            {
                bioTrackerMenu = menu;
                bioTrackerNode = menu.GetNode();
                bioTrackerNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
                bioTrackerNode.AddListener(sMenuManager.nodeEvent.OnTapped, togglePerms);
                bioTrackerNode.AddListener(sMenuManager.nodeEvent.OnDoubleTapped, bioTrackerMenu.Open);
            }
            public static void togglePerms()
            {
                bool allowed = true;
                foreach (var bot in zSearch.GetAllBotAgents())
                {
                    allowed = !zSlideComputer.GetActionPermission("Use BioTracker", bot.PlayerSlotIndex); //This is a bad way to do it, i'm setting it multiple times.  But at least untill I make it so you can have different perms for different bots, it should all be fine!
                    zSlideComputer.SetActionPermission("Use BioTracker", bot.PlayerSlotIndex, allowed);
                }
                if (allowed)
                {
                    bioTrackerNode.SetColor(sMenuManager.defaultColor);
                    bioTrackerMenu.centerNode.SetColor(sMenuManager.defaultColor);
                }
                else
                {
                    bioTrackerNode.SetColor(new Color(0.25f, 0f, 0f));
                    bioTrackerMenu.centerNode.SetColor(new Color(0.25f, 0f, 0f));
                }
            }
            public static void setAllowed(bool allowed)
            {
                foreach (var bot in zSearch.GetAllBotAgents())
                {
                    zSlideComputer.SetActionPermission("Use BioTracker", bot.PlayerSlotIndex, allowed);
                }
                if (allowed)
                {
                    bioTrackerNode.SetColor(sMenuManager.defaultColor);
                    bioTrackerMenu.centerNode.SetColor(sMenuManager.defaultColor);
                }
                else
                {
                    bioTrackerNode.SetColor(new Color(0.25f, 0f, 0f));
                    bioTrackerMenu.centerNode.SetColor(new Color(0.25f, 0f, 0f));
                }
            }
        }
        public static class PingMenuClass
        {
            public static sMenu pingMenu;
            public static sMenu.sMenuNode pingNode;
            public static void Setup(sMenu menu)
            {
                pingMenu = menu;
                pingNode = menu.GetNode();
                pingNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
                pingNode.AddListener(sMenuManager.nodeEvent.OnTapped, togglePingPerms);
                pingNode.AddListener(sMenuManager.nodeEvent.OnDoubleTapped, pingMenu.Open);
            }
            public static void togglePingPerms()
            {
                bool allowed = true;
                foreach (var bot in zSearch.GetAllBotAgents())
                {
                    allowed = !zSlideComputer.GetActionPermission("Ping", bot.PlayerSlotIndex); //This is a bad way to do it, i'm setting it multiple times.  But at least untill I make it so you can have different perms for different bots, it should all be fine!
                    zSlideComputer.SetActionPermission("Ping", bot.PlayerSlotIndex, allowed);
                }
                if (allowed)
                {
                    pingNode.SetColor(sMenuManager.defaultColor);
                    pingMenu.centerNode.SetColor(sMenuManager.defaultColor);
                }
                else
                {
                    pingNode.SetColor(new Color(0.25f, 0f, 0f));
                    pingMenu.centerNode.SetColor(new Color(0.25f, 0f, 0f));
                }
            }
            public static void setAllowed(bool allowed)
            {
                foreach (var bot in zSearch.GetAllBotAgents())
                {
                    zSlideComputer.SetActionPermission("Ping", bot.PlayerSlotIndex, allowed);
                }
                if (allowed)
                {
                    pingNode.SetColor(sMenuManager.defaultColor);
                    pingMenu.centerNode.SetColor(sMenuManager.defaultColor);
                }
                else
                {
                    pingNode.SetColor(new Color(0.25f, 0f, 0f));
                    pingMenu.centerNode.SetColor(new Color(0.25f, 0f, 0f));
                }
            }
        }
        public static class UnlockMenuClass
        {
            public static sMenu unlockMenu;
            public static sMenu.sMenuNode unlockNode;
            public static void Setup(sMenu menu)
            {
                unlockMenu = menu;
                unlockNode = unlockMenu.GetNode();
                unlockNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
                unlockNode.AddListener(sMenuManager.nodeEvent.OnDoubleTapped, unlockMenu.Open);
                unlockNode.AddListener(sMenuManager.nodeEvent.OnTapped, TogglePerms);
            }
            public static void TogglePerms()
            {
                bool allowed = true;
                foreach (var bot in zSearch.GetAllBotAgents())
                {
                    allowed = !zSlideComputer.GetActionPermission("Unlock", bot.PlayerSlotIndex); //This is a bad way to do it, i'm setting it multiple times.  But at least untill I make it so you can have different perms for different bots, it should all be fine!
                    zSlideComputer.SetActionPermission("Unlock", bot.PlayerSlotIndex, allowed);
                }
                if (allowed)
                {
                    unlockNode.SetColor(sMenuManager.defaultColor);
                    unlockMenu.centerNode.SetColor(sMenuManager.defaultColor);
                }
                else
                {
                    unlockNode.SetColor(new Color(0.25f, 0f, 0f));
                    unlockMenu.centerNode.SetColor(new Color(0.25f, 0f, 0f));
                }
            }
            public static void setAllowed(bool allowed)
            {
                foreach (var bot in zSearch.GetAllBotAgents())
                {
                    zSlideComputer.SetActionPermission("Unlock", bot.PlayerSlotIndex, allowed);
                }
                if (allowed)
                {
                    unlockNode.SetColor(sMenuManager.defaultColor);
                    unlockMenu.centerNode.SetColor(sMenuManager.defaultColor);
                }
                else
                {
                    unlockNode.SetColor(new Color(0.25f, 0f, 0f));
                    unlockMenu.centerNode.SetColor(new Color(0.25f, 0f, 0f));
                }
            }
        }
        public static class PickupMenuClass
        {
            public static Dictionary<uint, sMenu.sMenuNode> prioNodesByID = new Dictionary<uint, sMenu.sMenuNode>();
            private static Dictionary<string, List<string>> catagories = new();
            private static int catagoryIndex = 1;
            public static sMenu pickupMenu;
            public static sMenu.sMenuNode pickupNode;
            public static void Setup(sMenu menu)
            {
                pickupMenu = menu;
                pickupMenu.radius = 125f;
                pickupNode = pickupMenu.GetNode();

                sMenu.sMenuNode glowstickNode = null;
                foreach (var item in zSlideComputer.itemPrios)
                {
                    uint itemID = item.Key;
                    ItemDataBlock block = ItemDataBlock.s_blockByID[itemID];
                    string publicName = block.publicName;
                    sMenu.sMenuNode node = null;
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
                    //thisNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
                    //thisNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, menu.Open);
                    //thisNode.AddListener(sMenuManager.nodeEvent.OnTapped, TogglePerms);
                    node.AddListener(sMenuManager.nodeEvent.WhileSelected, ChangePrioBasedOnMouseWheel, itemID, node);
                    node.AddListener(sMenuManager.nodeEvent.OnTapped, zSlideComputer.ToggleItemPrioDisabled, itemID);
                    node.AddListener(sMenuManager.nodeEvent.OnTapped, updateNodePriorityDisplay, node, itemID);//TODO make these args order consistant.
                    node.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, ResetNodeSettings, itemID, node);
                    pickupMenu.centerNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
                    pickupMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnTapped, pickupMenu.parrentMenu.Open);
                    pickupMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, ResetNodeSettings, itemID, node);
                    pickupMenu.AddListener(sMenuManager.menuEvent.OnOpened, updateNodePriorityDisplay, node, itemID);
                    node.fullTextPart.SetScale(1f, 1f);
                    node.subtitlePart.SetScale(0.75f, 0.75f);
                    node.titlePart.SetScale(0.5f, 0.5f);
                    node.SetSize(0.75f);
                }
                //pickupMenu.centerNode.AddListener(sMenuManager.nodeEvent.WhileSelected, UpdateCatagoryByScroll);
                //ALL - ENCOUNTERED - RESOURCES - PLACEABLES - THROWABLES - FAVORITES
                pickupMenu.AddCatagory("All");
                pickupMenu.AddCatagory("Encountered");
                pickupMenu.AddNodeToCatagory("Favorites", "Ammo Pack");
                pickupMenu.AddNodeToCatagory("Favorites", "MediPack");
                pickupMenu.AddNodeToCatagory("Favorites", "Tool Refill Pack");
                pickupMenu.AddNodeToCatagory("Favorites", "Disinfection Pack");
                pickupMenu.AddNodeToCatagory("Favorites", "C-Foam Grenade");
                pickupMenu.AddNodeToCatagory("Resources", "MediPack");
                pickupMenu.AddNodeToCatagory("Resources", "Ammo Pack");
                pickupMenu.AddNodeToCatagory("Resources", "Tool Refill Pack");
                pickupMenu.AddNodeToCatagory("Resources", "Disinfection Pack");
                pickupMenu.AddNodeToCatagory("Placeables", "Lock Melter");
                pickupMenu.AddNodeToCatagory("Placeables", "C-Foam Tripmine");
                pickupMenu.AddNodeToCatagory("Placeables", "Explosive Trip Mine");
                pickupMenu.AddNodeToCatagory("Throwables", "Glow Stick");
                pickupMenu.AddNodeToCatagory("Throwables", "Fog Repeller");
                pickupMenu.AddNodeToCatagory("Throwables", "C-Foam Grenade");
                pickupMenu.SetCatagory("All");
                pickupMenu.AddListener(sMenuManager.menuEvent.OnOpened, pickupMenu.UpdateCatagoryNodes);
                pickupNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
                pickupNode.AddListener(sMenuManager.nodeEvent.OnTapped, TogglePerms);
                pickupNode.AddListener(sMenuManager.nodeEvent.OnDoubleTapped, pickupMenu.Open);
            }
            internal static void Encounter(string friendlyName)
            {
                var node = pickupMenu.GetNode(friendlyName);
                if (node == null)
                    return;
                if (!pickupMenu.catagories.Keys.Contains("Encountered"))
                {
                    ZiMain.log.LogWarning($"Unable to encouter {friendlyName} because Encountered catagory not found in pickup menu.");
                    return;
                }
                if (!pickupMenu.catagories["Encountered"].Contains(node))
                {
                    pickupMenu.AddNodeToCatagory("Encountered", node);
                    pickupMenu.UpdateCatagoryNodes();
                }
            }
            private static void ResetNodeSettings(uint itemID, sMenu.sMenuNode node)
            {
                if (!node.gameObject.activeInHierarchy)
                    return;
                zSlideComputer.ResetItemPrio(itemID);
                zSlideComputer.SetItemPrioDisabled(itemID, true);
                updateNodePriorityDisplay(node, itemID);
            }
            public static void TogglePerms()
            {
                bool allowed = true;
                foreach (var bot in zSearch.GetAllBotAgents())
                {
                    allowed = !zSlideComputer.GetActionPermission("Pickup", bot.PlayerSlotIndex); //This is a bad way to do it, i'm setting it multiple times.  But at least untill I make it so you can have different perms for different bots, it should all be fine!
                    zSlideComputer.SetActionPermission("Pickup", bot.PlayerSlotIndex, allowed);
                }
                if (allowed)
                {
                    pickupNode.SetColor(sMenuManager.defaultColor);
                    pickupMenu.centerNode.SetColor(sMenuManager.defaultColor);
                }
                else
                {
                    pickupNode.SetColor(new Color(0.25f, 0f, 0f));
                    pickupMenu.centerNode.SetColor(new Color(0.25f, 0f, 0f));
                }
            }
            public static void setAllowed(bool allowed)
            {
                foreach (var bot in zSearch.GetAllBotAgents())
                {
                    zSlideComputer.SetActionPermission("Pickup", bot.PlayerSlotIndex, allowed);
                }
                if (allowed)
                {
                    pickupNode.SetColor(sMenuManager.defaultColor);
                    pickupMenu.centerNode.SetColor(sMenuManager.defaultColor);
                }
                else
                {
                    pickupNode.SetColor(new Color(0.25f, 0f, 0f));
                    pickupMenu.centerNode.SetColor(new Color(0.25f, 0f, 0f));
                }
            }
            public static void updateNodePriorityDisplay(sMenu.sMenuNode node, uint itemID)
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
                    node.SetColor(sMenuManager.defaultColor);
                else
                    node.SetColor(new Color(0.25f, 0f, 0f));
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
            public static void ChangePrioBasedOnMouseWheel(uint itemID, sMenu.sMenuNode node, int increment = 10)
            {
                if (node == null || !node.gameObject.activeInHierarchy || !zSlideComputer.enabledItemPrios[itemID])
                    return;
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                int normalizedScroll = (int)Mathf.Sign(scroll);
                if (scroll == 0f)
                    return;
                float currentPrio = zSlideComputer.GetItemPrio(itemID);
                zSlideComputer.SetBotItemPriority(itemID, Mathf.Clamp(currentPrio + (normalizedScroll * increment), 0, 100));
                updateNodePriorityDisplay(node, itemID);
            }
        }
        public static class ShareMenuClass
        {
            public static Dictionary<uint, sMenu.sMenuNode> packNodesByID = new Dictionary<uint, sMenu.sMenuNode>();
            public static sMenu shareMenu;
            public static sMenu.sMenuNode shareNode;

            public static void Setup(sMenu menu)
            {
                shareMenu = menu;
                shareNode = shareMenu.GetNode();
                var resourceDataBlocks = ItemSpawnManager.m_itemDataPerInventorySlot[(int)InventorySlot.ResourcePack];
                foreach (ItemDataBlock block in resourceDataBlocks)
                {
                    uint itemID = ItemDataBlock.s_blockIDByName[block.name];
                    string name = block.publicName;
                    sMenu.sMenuNode node = shareMenu.AddNode(name);
                    //TODO uncomment then when moved over to overide system instead of selection system.
                    //var thisNode = menu.parrentMenu.GetNode(menu.centerNode.text);
                    //thisNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
                    //thisNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, menu.Open);
                    //thisNode.AddListener(sMenuManager.nodeEvent.OnTapped, TogglePerms);
                    node.AddListener(sMenuManager.nodeEvent.OnTapped, zSlideComputer.ToggleResourceSharePermission, itemID);
                    node.AddListener(sMenuManager.nodeEvent.OnTapped, updateNodeThresholdDisplay, node, itemID);
                    node.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, zSlideComputer.SetResourceSharePermission, itemID, true);
                    node.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, zSlideComputer.SetResourceThreshold, itemID, 100);
                    node.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, updateNodeThresholdDisplay, node, itemID);
                    node.AddListener(sMenuManager.nodeEvent.WhileSelected, ChangeThresholdBasedOnMouseWheel, itemID, node, 5);
                    shareMenu.AddListener(sMenuManager.menuEvent.OnOpened, updateNodeThresholdDisplay, node, itemID);
                    shareMenu.centerNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
                    shareMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, zSlideComputer.SetResourceThreshold, itemID, 100);
                    shareMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, zSlideComputer.SetResourceSharePermission, itemID, true);
                    shareMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, updateNodeThresholdDisplay, node, itemID);
                    node.fullTextPart.SetScale(1f, 1f);
                    node.subtitlePart.SetScale(0.75f, 0.75f);
                    node.titlePart.SetScale(0.5f, 0.5f);
                    packNodesByID[itemID] = node;
                }
                shareMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnTapped, shareMenu.parrentMenu.Open);
                shareNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
                shareNode.AddListener(sMenuManager.nodeEvent.OnTapped, TogglePerms);
                shareNode.AddListener(sMenuManager.nodeEvent.OnDoubleTapped, shareMenu.Open);
            }
            public static void updateNodeThresholdDisplay(sMenu.sMenuNode node, uint itemID)
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
                    node.SetColor(sMenuManager.defaultColor);
                else
                    node.SetColor(new Color(0.25f, 0f, 0f));
            }
            public static void TogglePerms()
            {
                bool allowed = true;
                foreach (var bot in zSearch.GetAllBotAgents())
                {
                    allowed = !zSlideComputer.GetActionPermission("Share", bot.PlayerSlotIndex); //This is a bad way to do it, i'm setting it multiple times.  But at least untill I make it so you can have different perms for different bots, it should all be fine!
                    zSlideComputer.SetActionPermission("Share", bot.PlayerSlotIndex, allowed);
                }
                if (allowed)
                {
                    shareNode.SetColor(sMenuManager.defaultColor);
                    shareMenu.centerNode.SetColor(sMenuManager.defaultColor);
                }
                else
                {
                    shareNode.SetColor(new Color(0.25f, 0f, 0f));
                    shareMenu.centerNode.SetColor(new Color(0.25f, 0f, 0f));
                }
            }
            public static void setAllowed(bool allowed)
            {
                foreach (var bot in zSearch.GetAllBotAgents())
                {
                    zSlideComputer.SetActionPermission("Share", bot.PlayerSlotIndex, allowed);
                }
                if (allowed)
                {
                    shareNode.SetColor(sMenuManager.defaultColor);
                    shareMenu.centerNode.SetColor(sMenuManager.defaultColor);
                }
                else
                {
                    shareNode.SetColor(new Color(0.25f, 0f, 0f));
                    shareMenu.centerNode.SetColor(new Color(0.25f, 0f, 0f));
                }
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
            public static void ChangeThresholdBasedOnMouseWheel(uint itemID, sMenu.sMenuNode node, int increment = 10)
            {
                if (node == null || !node.gameObject.activeInHierarchy || !zSlideComputer.enabledResourceShares[itemID])
                    return;
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                int normalizedScroll = (int)Mathf.Sign(scroll);
                if (scroll == 0f)
                    return;
                float currentThreshold = zSlideComputer.GetResourceThreshold(itemID);
                zSlideComputer.SetResourceThreshold(itemID, Math.Clamp((int)currentThreshold + (normalizedScroll * increment), 0, 100));
                updateNodeThresholdDisplay(node, itemID);
            }
        }
        public static class ExploreMenuClass
        {
            private static sMenu exploreMenu;
            private static sMenu.sMenuNode exploreNode;
            internal static void Setup(sMenu menu)
            {
                exploreMenu = menu;
                exploreNode = exploreMenu.parrentMenu.GetNode(exploreMenu.centerNode.text);
                exploreNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
                exploreNode.AddListener(sMenuManager.nodeEvent.OnTapped, ToggleExplorePerms);
                exploreNode.AddListener(sMenuManager.nodeEvent.OnDoubleTapped, exploreMenu.Open);
            }
            private static void ToggleExplorePerms()
            {
                return;
                //var bots = zSearch.GetAllBotAgents();
                //foreach (var bot in bots)
                //{
                //    ExploreAction.ToggleExplorePerm(bot);
                //}
                //if (ExploreAction.GetExplorePerm(bots[0]))
                //    exploreNode.SetColor(sMenuManager.defaultColor);
                //else
                //    exploreNode.SetColor(new Color(0.25f, 0f, 0f));
            }
        }
        public static class FollowMenuClass
        {
            private static sMenu followMenu;
            private static sMenu.sMenuNode followMenuNode;
            private static Dictionary<DRAMA_State, sMenu.sMenuNode> stateNodes = new();
            private static Dictionary<string, sMenu.sMenuNode> catagoryNodes = new();
            public static DRAMA_State previousState;
            public static Color currentStateColor;
            public static Color defaultColor;
            public static OverrideTree<float?> prio = AutomaticActionMenuClass.ActionPriorities["Follow"];
            public static OverrideTree<int?> followRadius = new(7);
            public static OverrideTree<int?> maxDistance = new(10);
            public static OverrideTree<bool?> followEnabled = new(true);
            private static List<DRAMA_State> fightingStates = new();
            private static List<DRAMA_State> ignoredStates = new();

            internal static void Setup(sMenu menu)
            {
                defaultColor = menu.getTextColor();
                currentStateColor = new(0f, 0.2f, 0f);
                followMenu = menu;
                followMenuNode = followMenu.GetNode();
                previousState = DramaManager.CurrentStateEnum;
                FollowActionPatch.Setup();

                fightingStates.Add(DRAMA_State.Combat);
                fightingStates.Add(DRAMA_State.Encounter);
                fightingStates.Add(DRAMA_State.IntentionalCombat);
                fightingStates.Add(DRAMA_State.Alert);
                ignoredStates.Add(DRAMA_State.ElevatorGoingDown);
                ignoredStates.Add(DRAMA_State.ElevatorIdle);

                //prio = new(14);
                followRadius = new(7);
                maxDistance = new(10);
                followEnabled = new(true);
                followEnabled.AddNode("Main", true);
                //prio.AddNode("Follow", null);
                prio.AddNode("Fighting", null, "Follow", condition: () => { return fightingStates.Contains(DramaManager.CurrentStateEnum); });
                prio.AddNode("Stealth", null, "Follow", condition: () => { return DramaManager.CurrentStateEnum == DRAMA_State.Sneaking; });
                prio.AddNode("Explore", null, "Follow", condition: () => { return DramaManager.CurrentStateEnum == DRAMA_State.Exploration; });
                followRadius.AddNode("Follow", null);
                followRadius.AddNode("Fighting", null, "Follow", condition: () => { return fightingStates.Contains(DramaManager.CurrentStateEnum); });
                followRadius.AddNode("Stealth", null, "Follow", condition: () => { return DramaManager.CurrentStateEnum == DRAMA_State.Sneaking; });
                followRadius.AddNode("Explore", null, "Follow", condition: () => { return DramaManager.CurrentStateEnum == DRAMA_State.Exploration; });
                maxDistance.AddNode("Follow", null);
                maxDistance.AddNode("Fighting", null, "Follow", condition: () => { return fightingStates.Contains(DramaManager.CurrentStateEnum); });
                maxDistance.AddNode("Stealth", null, "Follow", condition: () => { return DramaManager.CurrentStateEnum == DRAMA_State.Sneaking; });
                maxDistance.AddNode("Explore", null, "Follow", condition: () => { return DramaManager.CurrentStateEnum == DRAMA_State.Exploration; });

                catagoryNodes["Fighting"] = AddCatagoryNode("Fighting");
                catagoryNodes["Stealth"] = AddCatagoryNode("Stealth");
                catagoryNodes["Explore"] = AddCatagoryNode("Explore");

                followMenu.AddCatagory("Basic");
                followMenu.AddNodeToCatagory("Basic", "Fighting");
                followMenu.AddNodeToCatagory("Basic", "Stealth");
                followMenu.AddNodeToCatagory("Basic", "Explore");
                followMenu.AddCatagory("Advanced");

                followMenu.AddPannel(sMenu.sMenuPannel.Side.bottom, "Top: Priority, how important is staing in range?");
                followMenu.AddPannel(sMenu.sMenuPannel.Side.bottom, "Bottom left: Range, how close should the bots be?");
                followMenu.AddPannel(sMenu.sMenuPannel.Side.bottom, "Bottom right: Max distance, When should bots sprint?");
                followMenu.AddPannel(sMenu.sMenuPannel.Side.right, "Scroll => change setting");
                followMenu.AddPannel(sMenu.sMenuPannel.Side.right, "Hold => reset");


                foreach (DRAMA_State state in Enum.GetValues(typeof(DRAMA_State)))
                {
                    if (ignoredStates.Contains(state))
                        continue;
                    string parentNode = "Follow";
                    switch (state)
                    {
                        case DRAMA_State.Sneaking:
                            parentNode = "Stealth";
                            break;
                        case DRAMA_State.Exploration:
                            parentNode = "Explore";
                            break;
                        case var s when fightingStates.Contains(s):
                            parentNode = "Fighting";
                            break;
                    }
                    prio.AddNode(state.ToString(), null, parentNode, () => { return DramaManager.CurrentStateEnum == state; });
                    followRadius.AddNode(state.ToString(), null, parentNode, () => { return DramaManager.CurrentStateEnum == state; });
                    maxDistance.AddNode(state.ToString(), null, parentNode, () => { return DramaManager.CurrentStateEnum == state; });

                    var stateNode = followMenu.AddNode(state.ToString());
                    stateNodes[state] = stateNode;
                    UpdateNodeSettingsDisplay(stateNode);
                    stateNode.titlePart.SetScale(0.5f);
                    stateNode.subtitlePart.SetScale(0.5f);
                    stateNode.AddListener(sMenuManager.nodeEvent.WhileSelected, UpdateNodeBasedOnScroll, stateNode);
                    stateNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, ResetSettings, stateNode);
                    followMenu.AddNodeToCatagory("Advanced", stateNode);
                }
                followMenu.AddListener(sMenuManager.menuEvent.WhileOpened, UpdateHighlightedState);
                followMenu.AddListener(sMenuManager.menuEvent.OnOpened, UpdateAllNodes);
                followMenu.AddListener(sMenuManager.menuEvent.OnCatagoryChanged, UpdateAllNodes);
                followMenu.centerNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
                followMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, ResetAllLocalSettings);
                followMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnTapped, followMenu.parrentMenu.Open);
                followMenuNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
                followMenuNode.AddListener(sMenuManager.nodeEvent.WhileSelected, UpdateNodeBasedOnScroll, followMenuNode);
                followMenuNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, ResetSettings, followMenuNode);
                followMenuNode.AddListener(sMenuManager.nodeEvent.OnTapped, TogglePerms);
                followMenuNode.AddListener(sMenuManager.nodeEvent.OnDoubleTapped, followMenu.Open);
                followMenu.radius = 130f;
                UpdateNodeSettingsDisplay(followMenuNode);
                followMenu.SetCatagory("Basic");
            }
            public static void setAllowed(bool enabled)
            {
                bool current = (bool)followEnabled.GetValue();

                // Already in desired state
                if (current == enabled)
                    return;

                followEnabled.SetValue("Main", enabled);

                var allbots = ZiMain.GetBotList();

                foreach (var bot in allbots)
                {
                    var data = zActions.GetOrCreateData(bot);

                    if (enabled)
                    {
                        // Restore original leader
                        if (bot.SyncValues.Leader != bot.Agent)
                        {
                            // Leader was externally changed, preserve it
                            data.actualLeader = bot.SyncValues.Leader;
                            continue;
                        }

                        if (data.actualLeader == bot.Agent)
                        {
                            ZiMain.log.LogWarning(
                                $"Actual leader for {bot.Agent.PlayerName} got lost. Resetting to local player");

                            data.actualLeader = PlayerManager.GetLocalPlayerAgent();
                        }

                        bot.SyncValues.Leader = data.actualLeader;
                    }
                    else
                    {
                        // Disable following by setting self as leader
                        if (bot.SyncValues.Leader == bot.Agent)
                        {
                            ZiMain.log.LogWarning(
                                $"Follow leader for {bot.Agent.PlayerName} got lost. Resetting to local player");

                            data.actualLeader = PlayerManager.GetLocalPlayerAgent();
                        }
                        else
                        {
                            // Backup current real leader
                            data.actualLeader = bot.SyncValues.Leader;
                        }

                        bot.SyncValues.Leader = bot.Agent;
                    }
                }

                UpdateToggleStateColors();
            }
            private static void TogglePerms()
            {
                setAllowed(!(bool)followEnabled.GetValue());
            }
            private static void OldToggleFollowEnabled()
            {
                bool allowed = (bool)followEnabled.SetValue("Main", !(bool)followEnabled.GetValue()); // Invert value
                var allbots = ZiMain.GetBotList();
                foreach (var bot in allbots)
                {
                    var data = zActions.GetOrCreateData(bot);
                    if (allowed)
                    {
                        if (bot.SyncValues.Leader != bot.Agent)//Leader was changed to something else, don't revert
                        {
                            data.actualLeader = bot.SyncValues.Leader;
                            continue;
                        }
                        if (data.actualLeader == bot.Agent)
                        {
                            ZiMain.log.LogWarning($"Actual leader for {bot.Agent.PlayerName} got lost.  Reseting to local player");
                            data.actualLeader = PlayerManager.GetLocalPlayerAgent();
                        }
                        bot.SyncValues.Leader = data.actualLeader;

                    }
                    else
                    {
                        if (bot.SyncValues.Leader == bot.Agent)
                        {
                            ZiMain.log.LogWarning($"Follow leader for {bot.Agent.PlayerName} got lost.  Reseting to local player");
                            data.actualLeader = PlayerManager.GetLocalPlayerAgent();
                        }
                        else
                        {
                            data.actualLeader = bot.SyncValues.Leader; // Backup the real leader
                        }
                        bot.SyncValues.Leader = bot.Agent; // Set leader to itself
                    }
                }
                UpdateToggleStateColors();
            }
            private static void UpdateToggleStateColors()
            {
                if ((bool)followEnabled.GetValue())
                {
                    followMenuNode.SetColor(sMenuManager.defaultColor);
                    followMenu.centerNode.SetColor(sMenuManager.defaultColor);
                }
                else
                {
                    followMenuNode.SetColor(new Color(0.25f, 0f, 0f));
                    followMenu.centerNode.SetColor(new Color(0.25f, 0f, 0f));
                }
            }
            private static sMenu.sMenuNode AddCatagoryNode(string catagory)
            {
                var catagoryNode = followMenu.AddNode(catagory);
                catagoryNode.titlePart.SetScale(0.5f);
                catagoryNode.subtitlePart.SetScale(0.5f);
                catagoryNode.AddListener(sMenuManager.nodeEvent.WhileSelected, UpdateNodeBasedOnScroll, catagoryNode);
                catagoryNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, ResetSettings, catagoryNode);
                return catagoryNode;
            }
            internal static void ResetSettings(sMenu.sMenuNode node)
            {
                string text = node.text;
                prio.SetValue(text, null);
                followRadius.SetValue(text, null);
                maxDistance.SetValue(text, null);
                UpdateNodeSettingsDisplay(node);
            }
            private static void ResetAllLocalSettings()
            {
                foreach (var Node in followMenu.currentCatagory)
                    ResetSettings(Node);
            }
            private static void UpdateHighlightedState(bool breakOnSameState = true)
            {
                if (breakOnSameState && previousState == DramaManager.CurrentStateEnum)
                    return;
                if (followMenu.currentCatagoryName == "Advanced")
                {
                    if (stateNodes.ContainsKey(DramaManager.CurrentStateEnum))
                    {
                        if (stateNodes.ContainsKey(previousState))
                            stateNodes[previousState].SetColor(defaultColor);
                        stateNodes[DramaManager.CurrentStateEnum].SetColor(currentStateColor);
                    }
                    else if (stateNodes.ContainsKey(previousState))
                    {
                        stateNodes[previousState].SetColor(defaultColor);
                    }
                }
                if (followMenu.currentCatagoryName == "Advanced")
                {
                    switch (DramaManager.CurrentStateEnum)
                    {
                        case DRAMA_State.Sneaking:
                            catagoryNodes["Stealth"].SetColor(currentStateColor);
                            catagoryNodes["Fighting"].SetColor(defaultColor);
                            catagoryNodes["Explore"].SetColor(defaultColor);
                            break;
                        case DRAMA_State.Exploration:
                            catagoryNodes["Stealth"].SetColor(defaultColor);
                            catagoryNodes["Fighting"].SetColor(defaultColor);
                            catagoryNodes["Explore"].SetColor(currentStateColor);
                            break;
                        case var s when fightingStates.Contains(s):
                            catagoryNodes["Stealth"].SetColor(defaultColor);
                            catagoryNodes["Fighting"].SetColor(currentStateColor);
                            catagoryNodes["Explore"].SetColor(defaultColor);
                            break;
                    }
                }
                previousState = DramaManager.CurrentStateEnum;
            }
            private static void UpdateNodeBasedOnScroll(sMenu.sMenuNode node)
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                int normalizedScroll = (int)Mathf.Sign(scroll);
                if (scroll == 0f)
                    return;
                string text = node.text;
                var pos = Camera.main.WorldToViewportPoint(node.gameObject.transform.position);
                pos = new Vector2(pos.x - 0.5f, pos.y - 0.5f) * -1;
                if (pos.y > Math.Abs(pos.x)) // TOP
                {
                    prio.SetValue(text, Math.Clamp((int)prio.ValueAt(text) + normalizedScroll, 1, 15));
                }
                else if (pos.x > 0) // RIGHT
                {
                    maxDistance.SetValue(text, Math.Clamp((int)maxDistance.ValueAt(text) + normalizedScroll, (int)followRadius.ValueAt(text), 60));
                }
                else // LEFT
                {
                    followRadius.SetValue(text, Math.Clamp((int)followRadius.ValueAt(text) + normalizedScroll, 1, (int)maxDistance.ValueAt(text)));
                }
                UpdateNodeSettingsDisplay(node);
            }
            private static void UpdateAllNodes()
            {
                foreach (var Node in followMenu.nodes)
                {
                    UpdateNodeSettingsDisplay(Node);
                }
            }
            private static void UpdateNodeSettingsDisplay(sMenu.sMenuNode node)
            {
                string text = node.text;
                if (prio.nodes[text].IsDefaultValue() && followRadius.nodes[text].IsDefaultValue() && maxDistance.nodes[text].IsDefaultValue())
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
                node.SetSubtitle($"Range <color=#CC840066>[</color>{followRadius.ValueAt(text)}/{maxDistance.ValueAt(text)}<color=#CC840066>]</color>");
            }
        }
    }
    public static class DebugMenuClass
    {
        public static sMenu debugMenu;
        public static sMenu debugNodeMenu;
        public static sMenu debugNodeSettingsMenu;
        public static sMenu debugCameraCullingMenu;

        public static void Setup(sMenu menu)
        {
            //debugMenu = sMenuManager.createMenu("debug", sMenuManager.mainMenu);
            debugMenu = menu;
            debugNodeMenu = sMenuManager.createMenu("Nodes", debugMenu);
            debugNodeSettingsMenu = sMenuManager.createMenu("Settings", debugNodeMenu);
            debugCameraCullingMenu = sMenuManager.createMenu("Camera culling", debugMenu);
            debugMenu.AddNode("Show title prompt", InGameTitle.DisplayDefault).AddListener(sMenuManager.nodeEvent.OnUnpressedSelected, debugMenu.Close); ;
            debugMenu.AddNode("ChecVis")
                .AddListener(sMenuManager.nodeEvent.OnUnpressedSelected, zDebug.setCheckVizTarget)
                .AddListener(sMenuManager.nodeEvent.OnUnpressedSelected, zDebug.debugCheckViz)
                .AddListener(sMenuManager.nodeEvent.OnHeldImmediate, zDebug.toggleVisCheck)
                .AddListener(sMenuManager.nodeEvent.OnHeldImmediate, sMenuManager.CloseAllMenues)
                .AddListener(sMenuManager.nodeEvent.OnTappedExclusive, sMenuManager.CloseAllMenues)
                .AddListener(sMenuManager.nodeEvent.OnDoubleTapped, zDebug.setVisCheck, false)
                .AddListener(sMenuManager.nodeEvent.OnDoubleTapped, sMenuManager.CloseAllMenues)
            ;
            debugMenu.AddNode("Find unexplored", zDebug.MarkUnexploredArea);
            debugMenu.AddNode("SendBotToExplore", zDebug.SendClosestBotToExplore);
            debugMenu.AddNode("Show corners", zDebug.debugCorners);
            //debugMenu.AddNode("Toggle explore",ExploreAction.ToggleCanExplore);
            debugNodeMenu.AddNode("Node I'm looking at", zDebug.GetNodeImLookingAT, [sMenuManager.mainMenu.gameObject.transform]);
            debugNodeMenu.AddNode("Toggle Nodes", zDebug.ToggleNodes);
            debugNodeMenu.AddNode("Toggle Connections", zDebug.ToggleConnections);
            debugNodeMenu.AddNode("Toggle Node Info", zDebug.ToggleNodeInfo);
            debugNodeSettingsMenu.radius = 130;
            var gridSizeNode = debugNodeSettingsMenu.AddNode("Grid Size");
            gridSizeNode.AddListener(sMenuManager.nodeEvent.WhileSelected, DebugMenuClass.ChangeValueBasedOnMouseWheel, [DebugValueToChange.NodeGridSize, gridSizeNode, 0.1f]);
            gridSizeNode.SetSubtitle($"{zVisitedManager.NodeGridSize}");
            var mapGridSizeNode = debugNodeSettingsMenu.AddNode("Map Grid Size");
            mapGridSizeNode.AddListener(sMenuManager.nodeEvent.WhileSelected, DebugMenuClass.ChangeValueBasedOnMouseWheel, [DebugValueToChange.NodeMapSize, mapGridSizeNode, 1f]);
            mapGridSizeNode.SetSubtitle($"{zVisitedManager.NodeMapGridSize}");
            var visitDistanceNode = debugNodeSettingsMenu.AddNode("Visit distnace");
            visitDistanceNode.AddListener(sMenuManager.nodeEvent.WhileSelected, DebugMenuClass.ChangeValueBasedOnMouseWheel, [DebugValueToChange.NodeVisitDistance, visitDistanceNode, 0.5f]);
            visitDistanceNode.SetSubtitle($"{zVisitedManager.NodeVisitDistance}");
            var propigationAmmountNode = debugNodeSettingsMenu.AddNode("Propigation ammount");
            propigationAmmountNode.AddListener(sMenuManager.nodeEvent.WhileSelected, DebugMenuClass.ChangeValueBasedOnMouseWheel, [DebugValueToChange.PropigationAmmount, propigationAmmountNode, 1f]);
            propigationAmmountNode.SetSubtitle($"{zVisitedManager.propigationAmmount}");
            var propigationSameCountNode = debugNodeSettingsMenu.AddNode("Propigation sample count");
            propigationSameCountNode.AddListener(sMenuManager.nodeEvent.WhileSelected, DebugMenuClass.ChangeValueBasedOnMouseWheel, [DebugValueToChange.PropigationSampleCount, propigationSameCountNode, 1f]);
            propigationSameCountNode.SetSubtitle($"{zVisitedManager.propigationSampleCount}");
            var nodesPerFrameNode = debugNodeSettingsMenu.AddNode("Nodes per frame");
            nodesPerFrameNode.AddListener(sMenuManager.nodeEvent.WhileSelected, DebugMenuClass.ChangeValueBasedOnMouseWheel, [DebugValueToChange.NodesCreatedPerFrame, nodesPerFrameNode, 1f]);
            nodesPerFrameNode.SetSubtitle($"{zVisitedManager.nodesCreatedPerFrame}");
            var connectionChecksPerFrameNode = debugNodeSettingsMenu.AddNode("Connections per frame");
            connectionChecksPerFrameNode.AddListener(sMenuManager.nodeEvent.WhileSelected, DebugMenuClass.ChangeValueBasedOnMouseWheel, [DebugValueToChange.connectionChecksPerFrame, connectionChecksPerFrameNode, 1f]);
            connectionChecksPerFrameNode.SetSubtitle($"{zVisitedManager.connectionChecksPerFrame}");
            CullingMenuClass.setupCullingMenu(debugCameraCullingMenu);
            debugCameraCullingMenu.radius = 140;
            debugCameraCullingMenu.setNodeSize(0.5f);
        }
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
        public static void ChangeValueBasedOnMouseWheel(DebugValueToChange valueToChange, sMenu.sMenuNode node, float increment = 0.1f)
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
                    zVisitedManager.nodesCreatedPerFrame = Math.Max((int)offset + zVisitedManager.nodesCreatedPerFrame, 1);
                    value = zVisitedManager.nodesCreatedPerFrame;
                    break;
                case DebugValueToChange.connectionChecksPerFrame:
                    zVisitedManager.connectionChecksPerFrame = Math.Max((int)offset + zVisitedManager.connectionChecksPerFrame, 1);
                    value = zVisitedManager.connectionChecksPerFrame;
                    break;
                default:
                    Debug.LogWarning("Unknown DebugValueToChange: " + valueToChange);
                    break;
            }
            node.SetSubtitle($"{value}");
        }
        public static class CullingMenuClass
        {
            public static void setupCullingMenu(sMenu menu)
            {
                for (int i = 0; i < 32; i++)
                {
                    string name = LayerMask.LayerToName(i);
                    if (string.IsNullOrEmpty(name))
                        continue;
                    Camera camera = Camera.main;
                    var node = menu.AddNode(name);
                    node.AddListener(sMenuManager.nodeEvent.OnUnpressedSelected, ToggleLayer, camera, i, node);
                }
            }
            public static void ToggleLayer(Camera camera, int layer, sMenu.sMenuNode node)
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
    }
    public static class SettingsMenuClass
    {
        public static float menuSizeStep = 0.1f;
        public static sMenu scaleMenu;
        public static sMenu.sMenuNode scaleNode;

        public static void Setup(sMenu menu)
        {

            scaleMenu = menu;
            scaleNode = scaleMenu.AddNode("Scale");
            scaleNode.AddListener(sMenuManager.nodeEvent.WhileSelected, UpdateScaleByScroll);
            scaleMenu.centerNode.AddListener(sMenuManager.nodeEvent.WhileSelected, UpdateScaleByScroll);
            scaleNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, ResetScale);
            UpdateScaleNodeSubtitle();
        }
        private static void ResetScale()
        {
            sMenuManager.menuSizeScaler = 1;
            UpdateScaleNodeSubtitle();
            sMenuManager.SetMenusScale(sMenuManager.menuSizeScaler);
        }
        private static void UpdateScaleByScroll()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            int normalizedScroll = (int)Mathf.Sign(scroll);
            if (scroll == 0f)
                return;
            UpdateScaleNodeSubtitle();
            sMenuManager.menuSizeScaler += normalizedScroll * menuSizeStep;
            sMenuManager.menuSizeScaler = zHelpers.Round(sMenuManager.menuSizeScaler, 1);
            sMenuManager.menuSizeScaler = Math.Clamp(sMenuManager.menuSizeScaler, 0.3f, 5f);
            sMenuManager.SetMenusScale(sMenuManager.menuSizeScaler);
        }
        private static void UpdateScaleNodeSubtitle()
        {
            scaleNode.SetSubtitle($"<color=#CC840066>[ </color>{sMenuManager.menuSizeScaler}<color=#CC840066> ]</color>");
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

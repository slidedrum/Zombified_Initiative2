using CollisionRundown.Features.HUDs;
using GameData;
using Il2CppSystem.Data;
using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using ZombieTweak2.CustomActions.Patches;
using ZombieTweak2.zRootBotPlayerAction;
using ZombieTweak2.zRootBotPlayerAction.CustomActions;
using Zombified_Initiative;
using static FluffyUnderware.DevTools.ConditionalAttribute;
using static ZombieTweak2.zMenu.zMenu;

namespace ZombieTweak2.zMenu
{
    public static class zMenus
    {
        //This is the class that actually creates the menue instances
        //This is spesific to ZI
        //Very unfinished atm
        public static List<zMenu> botMenus;
        private static string endcolor = "</color>";
        private static string enabledColor = "<color=#FFA50066>";
        private static string disabledColor = "<color=#CCCCCC33>";

        public static void CreateMenus()
        {
            AutomaticActionMenuClass.Setup(zMenuManager.createMenu("Automatic Actions", zMenuManager.mainMenu));
            ManualActionMenuClass.Setup(zMenuManager.createMenu("Manual Actions", zMenuManager.mainMenu));
            zMenuManager.createMenu("Contextual Actions", zMenuManager.mainMenu);
            SettingsMenuClass.Setup(zMenuManager.createMenu("Settings", zMenuManager.mainMenu));
            zMenuManager.createMenu("Voice menu", zMenuManager.mainMenu);
            DebugMenuClass.Setup(zMenuManager.createMenu("Debug", zMenuManager.mainMenu));
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
    public static class ManualActionMenuClass
    {
        public static zMenu manualActionMenu;
        public static zMenu.zMenuNode manuActionNode;
        public static void Setup(zMenu menu)
        {
            manualActionMenu = menu;
            manuActionNode = manualActionMenu.GetNode();
            manualActionMenu.AddNode("Clear Room", StartClearRoomAction);
        }
        public static void StartClearRoomAction()
        {
            PlayerAgent playerAgent = PlayerManager.GetLocalPlayerAgent();
            PlayerAIBot bot = null;
            PlayerAgent botAgent = null;
            float closestDistance = float.MaxValue;
            foreach (var agent in PlayerManager.PlayerAgentsInLevel)
            {
                if (!agent.Owner.IsBot || !agent.Alive)
                    continue;
                float distance = Vector3.Distance(agent.Position, playerAgent.Position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    botAgent = agent;
                    bot = agent.gameObject.GetComponent<PlayerAIBot>();
                }
            }
            var action = new ClearRoomAction.Descriptor(bot);
            bot.StartAction(action);
        }
    }
    public static class AutomaticActionMenuClass
    {
        public static List<zMenu> autoActionMenus = new List<zMenu>();
        private static zMenu AutoActionMenu;
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
            var pickupMenu = zMenuManager.createMenu("Pickup", AutoActionMenu);
            autoActionMenus.Add(pickupMenu);
            var followMenu =zMenuManager.createMenu("Follow", AutoActionMenu);
            autoActionMenus.Add(followMenu);
            autoActionMenus.Add(zMenuManager.createMenu("Unlock", AutoActionMenu));
            //Custom actions
            var exploremenu = zMenuManager.createMenu("Explore", AutoActionMenu);
            autoActionMenus.Add(exploremenu);

            ExploreMenuClass.Setup(exploremenu);
            PickupMenuClass.Setup(pickupMenu);
            ShareMenuClass.Setup(shareMenu);
            FollowMenuClass.Setup(followMenu);

            AutoActionMenu.AddCatagory("All");
            AutoActionMenu.AddCatagory("Favorites");
            AutoActionMenu.AddNodeToCatagory("Favorites","Pickup");
            AutoActionMenu.AddNodeToCatagory("Favorites", "Share");
            AutoActionMenu.AddNodeToCatagory("Favorites", "Explore");
            AutoActionMenu.AddNodeToCatagory("Favorites", "Follow");
            AutoActionMenu.AddCatagory("Resources");
            AutoActionMenu.AddNodeToCatagory("Resources", "Pickup");
            AutoActionMenu.AddNodeToCatagory("Resources", "Share");
            AutoActionMenu.SetCatagory("Favorites");
            
        }
        private static void SetSubtitle(String subtitle)
        {
            AutoActionMenu.centerNode.SetSubtitle($"<color=#CC840066>[ </color>{subtitle}<color=#CC840066> ]</color>");
        }
        public static class PickupMenuClass
        {
            public static Dictionary<uint, zMenu.zMenuNode> prioNodesByID = new Dictionary<uint, zMenu.zMenuNode>();
            private static Dictionary<string, List<string>> catagories = new();
            private static int catagoryIndex = 1;
            public static zMenu pickupMenu;
            public static zMenu.zMenuNode pickupNode;
            public static void Setup(zMenu menu)
            {
                pickupMenu = menu;
                pickupMenu.radius = 25f;
                pickupNode = pickupMenu.GetNode();

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
                //pickupMenu.centerNode.AddListener(zMenuManager.nodeEvent.WhileSelected, UpdateCatagoryByScroll);
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
                pickupMenu.AddListener(zMenuManager.menuEvent.OnOpened, pickupMenu.UpdateCatagoryNodes);
                pickupNode.ClearListeners(zMenuManager.nodeEvent.OnUnpressedSelected);
                pickupNode.AddListener(zMenuManager.nodeEvent.OnTapped, TogglePerms);
                pickupNode.AddListener(zMenuManager.nodeEvent.OnDoubleTapped, pickupMenu.Open);
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
            private static void ResetNodeSettings(uint itemID, zMenu.zMenuNode node)
            {
                if (!node.gameObject.activeInHierarchy)
                    return;
                zSlideComputer.ResetItemPrio(itemID);
                zSlideComputer.SetItemPrioDisabled(itemID, true);
                updateNodePriorityDisplay(node, itemID);
            }
            [Obsolete]
            public static bool pickupAllowed = true;
            public static void TogglePerms()
            {
                pickupAllowed = !pickupAllowed;
                foreach (var bot in zSearch.GetAllBotAgents())
                {
                    zSlideComputer.SetPickupPermission(bot.PlayerSlotIndex, pickupAllowed);
                }
                if (pickupAllowed)
                {
                    pickupNode.SetColor(zMenuManager.defaultColor);
                    pickupMenu.centerNode.SetColor(zMenuManager.defaultColor);
                }
                else
                {
                    pickupNode.SetColor(new Color(0.25f, 0f, 0f));
                    pickupMenu.centerNode.SetColor(new Color(0.25f, 0f, 0f));
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
            public static void ChangePrioBasedOnMouseWheel(uint itemID, zMenu.zMenuNode node, int increment = 10)
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
            public static Dictionary<uint, zMenu.zMenuNode> packNodesByID = new Dictionary<uint, zMenu.zMenuNode>();
            public static zMenu shareMenu;
            public static zMenu.zMenuNode shareNode;

            public static void Setup(zMenu menu)
            {
                shareMenu = menu;
                shareNode = shareMenu.GetNode();
                var resourceDataBlocks = ItemSpawnManager.m_itemDataPerInventorySlot[(int)InventorySlot.ResourcePack];
                foreach (ItemDataBlock block in resourceDataBlocks)
                {
                    uint itemID = ItemDataBlock.s_blockIDByName[block.name];
                    string name = block.publicName;
                    zMenu.zMenuNode node = shareMenu.AddNode(name);
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
                    shareMenu.AddListener(zMenuManager.menuEvent.OnOpened, updateNodeThresholdDisplay, node, itemID);
                    shareMenu.centerNode.ClearListeners(zMenuManager.nodeEvent.OnUnpressedSelected);
                    shareMenu.centerNode.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, zSlideComputer.SetResourceThreshold, itemID, 100);
                    shareMenu.centerNode.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, zSlideComputer.SetResourceSharePermission, itemID, true);
                    shareMenu.centerNode.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, updateNodeThresholdDisplay, node, itemID);
                    node.fullTextPart.SetScale(1f, 1f);
                    node.subtitlePart.SetScale(0.75f, 0.75f);
                    node.titlePart.SetScale(0.5f, 0.5f);
                    packNodesByID[itemID] = node;
                }
                shareMenu.centerNode.AddListener(zMenuManager.nodeEvent.OnTapped, shareMenu.parrentMenu.Open);
                shareNode.ClearListeners(zMenuManager.nodeEvent.OnUnpressedSelected);
                shareNode.AddListener(zMenuManager.nodeEvent.OnTapped, TogglePerms);
                shareNode.AddListener(zMenuManager.nodeEvent.OnDoubleTapped, shareMenu.Open);
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
            [Obsolete]
            public static bool shareAllowed = true;
            public static void TogglePerms()
            {
                shareAllowed = !shareAllowed;
                foreach (var bot in zSearch.GetAllBotAgents())
                {
                    zSlideComputer.SetSharePermission(bot.PlayerSlotIndex, shareAllowed);
                }
                if (shareAllowed)
                {
                    shareNode.SetColor(zMenuManager.defaultColor);
                    shareMenu.centerNode.SetColor(zMenuManager.defaultColor);
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
            public static void ChangeThresholdBasedOnMouseWheel(uint itemID, zMenu.zMenuNode node, int increment = 10)
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
            private static zMenu exploreMenu;
            private static zMenu.zMenuNode exploreNode;
            internal static void Setup(zMenu menu)
            {
                exploreMenu = menu;
                exploreNode = exploreMenu.parrentMenu.GetNode(exploreMenu.centerNode.text);
                exploreNode.ClearListeners(zMenuManager.nodeEvent.OnUnpressedSelected);
                exploreNode.AddListener(zMenuManager.nodeEvent.OnTapped, ToggleExplorePerms);
                exploreNode.AddListener(zMenuManager.nodeEvent.OnDoubleTapped, exploreMenu.Open);
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
        public static class FollowMenuClass
        {
            private static zMenu followMenu;
            private static zMenuNode followMenuNode;
            private static Dictionary<DRAMA_State, zMenuNode> stateNodes = new();
            private static Dictionary<string, zMenuNode> catagoryNodes = new();
            public static DRAMA_State previousState;
            public static Color currentStateColor;
            public static Color defaultColor;
            public static OverrideTree<int?> prio = new(14);
            public static OverrideTree<int?> followRadius = new(7);
            public static OverrideTree<int?> maxDistance = new(10);
            public static OverrideTree<bool?> followEnabled = new(true);
            private static List<DRAMA_State> fightingStates = new();
            private static List<DRAMA_State> ignoredStates = new();

            internal static void Setup(zMenu menu)
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

                prio = new(14);
                followRadius = new(7);
                maxDistance = new(10);
                followEnabled = new(true);
                followEnabled.AddNode("Main", true);
                prio.        AddNode("Follow",     null);
                prio.        AddNode("Fighting", null, "Follow", condition: () => { return fightingStates.Contains(DramaManager.CurrentStateEnum); });
                prio.        AddNode("Stealth",  null, "Follow", condition: () => { return DramaManager.CurrentStateEnum == DRAMA_State.Sneaking; });
                prio.        AddNode("Explore",  null, "Follow", condition: () => { return DramaManager.CurrentStateEnum == DRAMA_State.Exploration; });
                followRadius.AddNode("Follow",     null);
                followRadius.AddNode("Fighting", null, "Follow", condition: () => { return fightingStates.Contains(DramaManager.CurrentStateEnum); });
                followRadius.AddNode("Stealth",  null, "Follow", condition: () => { return DramaManager.CurrentStateEnum == DRAMA_State.Sneaking; });
                followRadius.AddNode("Explore",  null, "Follow", condition: () => { return DramaManager.CurrentStateEnum == DRAMA_State.Exploration; });
                maxDistance. AddNode("Follow",     null);
                maxDistance. AddNode("Fighting", null, "Follow", condition: () => { return fightingStates.Contains(DramaManager.CurrentStateEnum); });
                maxDistance. AddNode("Stealth",  null, "Follow", condition: () => { return DramaManager.CurrentStateEnum == DRAMA_State.Sneaking; });
                maxDistance. AddNode("Explore",  null, "Follow", condition: () => { return DramaManager.CurrentStateEnum == DRAMA_State.Exploration; });

                catagoryNodes["Fighting"] = AddCatagoryNode("Fighting");
                catagoryNodes["Stealth"] = AddCatagoryNode("Stealth");
                catagoryNodes["Explore"] = AddCatagoryNode("Explore");

                followMenu.AddCatagory("Basic");
                followMenu.AddNodeToCatagory("Basic", "Fighting");
                followMenu.AddNodeToCatagory("Basic", "Stealth");
                followMenu.AddNodeToCatagory("Basic", "Explore");
                followMenu.AddCatagory("Advanced");

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
                    stateNode.AddListener(zMenuManager.nodeEvent.WhileSelected, UpdateNodeBasedOnScroll, stateNode);
                    stateNode.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, ResetSettings, stateNode);
                    followMenu.AddNodeToCatagory("Advanced", stateNode);
                }
                followMenu.AddListener(zMenuManager.menuEvent.WhileOpened, UpdateHighlightedState);
                followMenu.AddListener(zMenuManager.menuEvent.OnOpened, UpdateAllNodes);
                followMenu.AddListener(zMenuManager.menuEvent.OnCatagoryChanged, UpdateAllNodes);
                followMenu.centerNode.ClearListeners(zMenuManager.nodeEvent.OnUnpressedSelected);
                followMenu.centerNode.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, ResetAllLocalSettings);
                followMenu.centerNode.AddListener(zMenuManager.nodeEvent.OnTapped, followMenu.parrentMenu.Open);
                followMenuNode.ClearListeners(zMenuManager.nodeEvent.OnUnpressedSelected);
                followMenuNode.AddListener(zMenuManager.nodeEvent.WhileSelected, UpdateNodeBasedOnScroll, followMenuNode);
                followMenuNode.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, ResetSettings, followMenuNode);
                followMenuNode.AddListener(zMenuManager.nodeEvent.OnTapped, ToggleFollowEnabled);
                followMenuNode.AddListener(zMenuManager.nodeEvent.OnDoubleTapped, followMenu.Open);
                followMenu.radius = 30f;
                UpdateNodeSettingsDisplay(followMenuNode);
                followMenu.SetCatagory("Basic");
            }

            private static void ToggleFollowEnabled()
            {
                bool allowed = (bool)followEnabled.SetValue("Main", !(bool)followEnabled.GetValue()); // Invert value
                var allbots = ZiMain.GetBotList();
                foreach ( var bot in allbots)
                {
                    var data = zActions.GetOrCreateData(bot);
                    var followAction = bot.m_rootAction.Cast<RootPlayerBotAction.Descriptor>().ActionBase.Cast<RootPlayerBotAction>().m_followLeaderAction.Cast<PlayerBotActionFollow.Descriptor>();
                    if (allowed)
                    {
                        if (data.actualLeader == null)
                        {
                            ZiMain.log.LogWarning($"Follow leader for {bot.Agent.PlayerName} not found.  Reseting to local player");
                            data.actualLeader = PlayerManager.GetLocalPlayerAgent();
                        }
                        followAction.Client = data.actualLeader;

                    }
                    else
                    {
                        if (followAction.Client == null)
                        {
                            ZiMain.log.LogWarning($"Follow leader for {bot.Agent.PlayerName} got lost.  Reseting to local player");
                            data.actualLeader = PlayerManager.GetLocalPlayerAgent();
                        }
                        else 
                        {
                            data.actualLeader = followAction.Client; // Backup the real leader
                        }
                        followAction.Client = bot.Agent; // Set leader to itself
                    }
                }
                UpdateToggleStateColors();
            }
            private static void UpdateToggleStateColors()
            {
                if ((bool)followEnabled.GetValue())
                {
                    followMenuNode.SetColor(zMenuManager.defaultColor);
                    followMenu.centerNode.SetColor(zMenuManager.defaultColor);
                }
                else
                {
                    followMenuNode.SetColor(new Color(0.25f, 0f, 0f));
                    followMenu.centerNode.SetColor(new Color(0.25f, 0f, 0f));
                }
            }

            private static zMenuNode AddCatagoryNode(string catagory)
            {
                var catagoryNode = followMenu.AddNode(catagory);
                catagoryNode.titlePart.SetScale(0.5f);
                catagoryNode.subtitlePart.SetScale(0.5f);
                catagoryNode.AddListener(zMenuManager.nodeEvent.WhileSelected, UpdateNodeBasedOnScroll, catagoryNode);
                catagoryNode.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, ResetSettings, catagoryNode);
                return catagoryNode;
            }
            private static void ResetSettings(zMenuNode node)
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
            private static void UpdateNodeBasedOnScroll(zMenuNode node)
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
                foreach(var Node in followMenu.nodes)
                {
                    UpdateNodeSettingsDisplay(Node);
                }
            }
            private static void UpdateNodeSettingsDisplay(zMenuNode node)
            {
                string text = node.text;
                if (prio.nodes[text].Parent.ValueAt()         == prio.ValueAt(text) && 
                    followRadius.nodes[text].Parent.ValueAt() == followRadius.ValueAt(text) && 
                    maxDistance.nodes[text].Parent.ValueAt()  == maxDistance.ValueAt(text)) //Holy shit this is a mess, TODO clean this.
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
        public static zMenu debugMenu;
        public static zMenu debugNodeMenu;
        public static zMenu debugNodeSettingsMenu;
        public static zMenu debugCameraCullingMenu;

        public static void Setup(zMenu menu)
        {
            //debugMenu = zMenuManager.createMenu("debug", zMenuManager.mainMenu);
            debugMenu = menu;
            debugNodeMenu = zMenuManager.createMenu("Nodes", debugMenu);
            debugNodeSettingsMenu = zMenuManager.createMenu("Settings", debugNodeMenu);
            debugCameraCullingMenu = zMenuManager.createMenu("Camera culling", debugMenu);
            debugMenu.AddNode("Show title prompt", InGameTitle.DisplayDefault).AddListener(zMenuManager.nodeEvent.OnUnpressedSelected, debugMenu.Close); ;
            debugMenu.AddNode("ChecVis")
                .AddListener(zMenuManager.nodeEvent.OnUnpressedSelected, zDebug.setCheckVizTarget)
                .AddListener(zMenuManager.nodeEvent.OnUnpressedSelected, zDebug.debugCheckViz)
                .AddListener(zMenuManager.nodeEvent.OnHeldImmediate, zDebug.toggleVisCheck)
                .AddListener(zMenuManager.nodeEvent.OnHeldImmediate, zMenuManager.CloseAllMenues)
                .AddListener(zMenuManager.nodeEvent.OnTappedExclusive, zMenuManager.CloseAllMenues)
                .AddListener(zMenuManager.nodeEvent.OnDoubleTapped, zDebug.setVisCheck, false)
                .AddListener(zMenuManager.nodeEvent.OnDoubleTapped, zMenuManager.CloseAllMenues)
            ;
            debugMenu.AddNode("Find unexplored", zDebug.MarkUnexploredArea);
            debugMenu.AddNode("SendBotToExplore", zDebug.SendClosestBotToExplore);
            debugMenu.AddNode("Show corners", zDebug.debugCorners);
            //debugMenu.AddNode("Toggle explore",ExploreAction.ToggleCanExplore);
            debugNodeMenu.AddNode("Node I'm looking at", zDebug.GetNodeImLookingAT, [zMenuManager.mainMenu.gameObject.transform]);
            debugNodeMenu.AddNode("Toggle Nodes", zDebug.ToggleNodes);
            debugNodeMenu.AddNode("Toggle Connections", zDebug.ToggleConnections);
            debugNodeMenu.AddNode("Toggle Node Info", zDebug.ToggleNodeInfo);
            debugNodeSettingsMenu.radius = 30;
            var gridSizeNode = debugNodeSettingsMenu.AddNode("Grid Size");
            gridSizeNode.AddListener(zMenuManager.nodeEvent.WhileSelected, DebugMenuClass.ChangeValueBasedOnMouseWheel, [DebugValueToChange.NodeGridSize, gridSizeNode, 0.1f]);
            gridSizeNode.SetSubtitle($"{zVisitedManager.NodeGridSize}");
            var mapGridSizeNode = debugNodeSettingsMenu.AddNode("Map Grid Size");
            mapGridSizeNode.AddListener(zMenuManager.nodeEvent.WhileSelected, DebugMenuClass.ChangeValueBasedOnMouseWheel, [DebugValueToChange.NodeMapSize, mapGridSizeNode, 1f]);
            mapGridSizeNode.SetSubtitle($"{zVisitedManager.NodeMapGridSize}");
            var visitDistanceNode = debugNodeSettingsMenu.AddNode("Visit distnace");
            visitDistanceNode.AddListener(zMenuManager.nodeEvent.WhileSelected, DebugMenuClass.ChangeValueBasedOnMouseWheel, [DebugValueToChange.NodeVisitDistance, visitDistanceNode, 0.5f]);
            visitDistanceNode.SetSubtitle($"{zVisitedManager.NodeVisitDistance}");
            var propigationAmmountNode = debugNodeSettingsMenu.AddNode("Propigation ammount");
            propigationAmmountNode.AddListener(zMenuManager.nodeEvent.WhileSelected, DebugMenuClass.ChangeValueBasedOnMouseWheel, [DebugValueToChange.PropigationAmmount, propigationAmmountNode, 1f]);
            propigationAmmountNode.SetSubtitle($"{zVisitedManager.propigationAmmount}");
            var propigationSameCountNode = debugNodeSettingsMenu.AddNode("Propigation sample count");
            propigationSameCountNode.AddListener(zMenuManager.nodeEvent.WhileSelected, DebugMenuClass.ChangeValueBasedOnMouseWheel, [DebugValueToChange.PropigationSampleCount, propigationSameCountNode, 1f]);
            propigationSameCountNode.SetSubtitle($"{zVisitedManager.propigationSampleCount}");
            var nodesPerFrameNode = debugNodeSettingsMenu.AddNode("Nodes per frame");
            nodesPerFrameNode.AddListener(zMenuManager.nodeEvent.WhileSelected, DebugMenuClass.ChangeValueBasedOnMouseWheel, [DebugValueToChange.NodesCreatedPerFrame, nodesPerFrameNode, 1f]);
            nodesPerFrameNode.SetSubtitle($"{zVisitedManager.nodesCreatedPerFrame}");
            var connectionChecksPerFrameNode = debugNodeSettingsMenu.AddNode("Connections per frame");
            connectionChecksPerFrameNode.AddListener(zMenuManager.nodeEvent.WhileSelected, DebugMenuClass.ChangeValueBasedOnMouseWheel, [DebugValueToChange.connectionChecksPerFrame, connectionChecksPerFrameNode, 1f]);
            connectionChecksPerFrameNode.SetSubtitle($"{zVisitedManager.connectionChecksPerFrame}");
            CullingMenuClass.setupCullingMenu(debugCameraCullingMenu);
            debugCameraCullingMenu.radius = 40;
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
    }
    public static class SettingsMenuClass
    {
        public static float menuSizeStep = 0.1f;
        public static zMenu scaleMenu;
        public static zMenu.zMenuNode scaleNode;

        public static void Setup(zMenu menu)
        {
 
            scaleMenu = menu;
            scaleNode = scaleMenu.AddNode("Scale");
            scaleNode.AddListener(zMenuManager.nodeEvent.WhileSelected, UpdateScaleByScroll);
            scaleMenu.centerNode.AddListener(zMenuManager.nodeEvent.WhileSelected, UpdateScaleByScroll);
            scaleNode.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, ResetScale);
            UpdateScaleNodeSubtitle();
        }
        private static void ResetScale()
        {
            zMenuManager.menuSizeScaler = 1;
            UpdateScaleNodeSubtitle();
            zMenuManager.SetMenusScale(zMenuManager.menuSizeScaler);
        }
        private static void UpdateScaleByScroll()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            int normalizedScroll = (int)Mathf.Sign(scroll);
            if (scroll == 0f)
                return;
            UpdateScaleNodeSubtitle();
            zMenuManager.menuSizeScaler += normalizedScroll * menuSizeStep;
            zMenuManager.menuSizeScaler = zHelpers.Round(zMenuManager.menuSizeScaler, 1);
            zMenuManager.menuSizeScaler = Math.Clamp(zMenuManager.menuSizeScaler, 0.3f, 5f);
            zMenuManager.SetMenusScale(zMenuManager.menuSizeScaler);
        }
        private static void UpdateScaleNodeSubtitle()
        {
            scaleNode.SetSubtitle($"<color=#CC840066>[ </color>{zMenuManager.menuSizeScaler}<color=#CC840066> ]</color>");
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

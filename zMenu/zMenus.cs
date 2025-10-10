using Agents;
using CollisionRundown.Features.HUDs;
using GameData;
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
using static Il2CppSystem.Globalization.CultureInfo;
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
            zMenuManager.createMenu("Manual Actions", zMenuManager.mainMenu);
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
        public static class PickupMenuClass
        {
            public static Dictionary<uint, zMenu.zMenuNode> prioNodesByID = new Dictionary<uint, zMenu.zMenuNode>();
            private static Dictionary<string, List<string>> catagories = new();
            private static int catagoryIndex = 1;
            private static zMenu pickupMenu;
            private static zMenu.zMenuNode pickupNode;
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

                pickupNode.ClearListeners(zMenuManager.nodeEvent.OnUnpressedSelected);
                pickupNode.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, pickupMenu.Open);
                pickupNode.AddListener(zMenuManager.nodeEvent.OnTapped, TogglePerms);
            }
            internal static void Encounter(string friendlyName)
            {
                if (!catagories.Keys.Contains("Encountered"))
                {
                    ZiMain.log.LogWarning($"Unable to encouter {friendlyName} because Encountered catagory not found in pickup menu.");
                    return;
                }
                if (!catagories["Encountered"].Contains(friendlyName))
                {
                    catagories["Encountered"].Add(friendlyName);
                    if (catagoryIndex < catagories.Count() && catagories.Keys.ElementAt(catagoryIndex) == "Encountered")
                        pickupMenu.SetCatagory("Encountered");
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
            private static zMenu shareMenu;
            private static zMenu.zMenuNode shareNode;

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
                    shareMenu.centerNode.AddListener(zMenuManager.nodeEvent.OnTapped, shareMenu.parrentMenu.Open);
                    shareMenu.centerNode.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, zSlideComputer.SetResourceThreshold, itemID, 100);
                    shareMenu.centerNode.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, zSlideComputer.SetResourceSharePermission, itemID, true);
                    shareMenu.centerNode.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, updateNodeThresholdDisplay, node, itemID);
                    node.fullTextPart.SetScale(1f, 1f);
                    node.subtitlePart.SetScale(0.75f, 0.75f);
                    node.titlePart.SetScale(0.5f, 0.5f);
                    packNodesByID[itemID] = node;
                }
                shareNode.ClearListeners(zMenuManager.nodeEvent.OnUnpressedSelected);
                shareNode.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, shareMenu.Open);
                shareNode.AddListener(zMenuManager.nodeEvent.OnTapped, TogglePerms);
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
        public static class FollowMenuClass
        {
            private static zMenu followMenu;
            private static zMenuNode followMenuNode;
            private static Dictionary<DRAMA_State, zMenuNode> stateNodes = new();
            public static DRAMA_State previousState;
            public static Color currentStateColor;
            public static Color defaultColor;

            internal static void Setup(zMenu menu)
            {
                defaultColor = menu.getTextColor();
                currentStateColor = new(0f, 0.2f, 0f);
                followMenu = menu;
                followMenuNode = followMenu.GetNode();
                previousState = DramaManager.CurrentStateEnum;
                FollowActionPatch.Setup();
                foreach (var agent in zSearch.GetAllBotAgents()) //Set default settings
                {
                    var data = zActions.GetOrCreateData(agent);
                    data.followSettingsOverides = new();

                } //Set default settings
                foreach (DRAMA_State state in Enum.GetValues(typeof(DRAMA_State)))
                {
                    if (!FollowActionPatch.myFollowSettingsOverides.ContainsKey(state))
                        continue;
                    var stateNode = followMenu.AddNode(state.ToString());
                    stateNodes[state] = stateNode;
                    UpdateStateNode(state);
                    stateNode.titlePart.SetScale(0.5f);
                    stateNode.subtitlePart.SetScale(0.5f);
                    stateNode.AddListener(zMenuManager.nodeEvent.WhileSelected, UpdateNodeBasedOnScroll, state);
                    stateNode.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, ResetSettings, state);
                }
                followMenu.AddListener(zMenuManager.menuEvent.WhileOpened, UpdateHighlightedState);
                followMenu.AddListener(zMenuManager.menuEvent.OnOpened, UpdateAllStateNodes);
                followMenu.centerNode.ClearListeners(zMenuManager.nodeEvent.OnUnpressedSelected);
                followMenu.centerNode.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, ResetAllStateSettings);
                followMenu.centerNode.AddListener(zMenuManager.nodeEvent.OnTapped, followMenu.parrentMenu.Open);
                followMenuNode.ClearListeners(zMenuManager.nodeEvent.OnUnpressedSelected);
                followMenuNode.AddListener(zMenuManager.nodeEvent.WhileSelected, UpdateMainNodeBasedOnScroll);
                followMenuNode.AddListener(zMenuManager.nodeEvent.OnHeldImmediate, ResetSettings);
                followMenuNode.AddListener(zMenuManager.nodeEvent.OnTapped, followMenu.Open);
                followMenu.radius = 30f;
                UpdateMainNode();
            }
            private static void ResetSettings()
            {
                FollowActionPatch.mainFollowerSettings = new();
                UpdateMainNode();
            }
            private static void ResetAllStateSettings()
            {
                FollowActionPatch.followSettingsOverides.Clear();
                foreach (var bot in zSearch.GetAllBotAgents())
                {
                    var data = zActions.GetOrCreateData(bot);
                    data.followSettingsOverides.Clear();
                }
                foreach(var state in stateNodes.Keys)
                {
                    UpdateStateNode(state);
                }
            }
            private static void ResetStateSettings(DRAMA_State state)
            {
                FollowActionPatch.followSettingsOverides.Remove(state);
                foreach(var bot in zSearch.GetAllBotAgents())
                {
                    var data = zActions.GetOrCreateData(bot);
                    data.followSettingsOverides.Remove(state);
                }
                UpdateStateNode(state);
            }
            private static void UpdateHighlightedState()
            {
                if (previousState == DramaManager.CurrentStateEnum)
                    return;
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
                previousState = DramaManager.CurrentStateEnum;

            }
            private static void UpdateMainNodeBasedOnScroll()
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                int normalizedScroll = (int)Mathf.Sign(scroll);
                if (scroll == 0f)
                    return;
                var node = followMenuNode;
                var pos = Camera.main.WorldToViewportPoint(node.gameObject.transform.position);
                pos = new Vector2(pos.x - 0.5f, pos.y - 0.5f) * -1;
                var tempSettings = FollowActionPatch.mainFollowerSettings;
                if (pos.y > Math.Abs(pos.x)) // TOP
                {
                    tempSettings.prio += normalizedScroll;
                    tempSettings.prio = Math.Clamp(tempSettings.prio, 1, 15);
                }
                else if (pos.x > 0) // RIGHT
                {
                    tempSettings.followLeaderMaxDistance += normalizedScroll;
                    tempSettings.followLeaderMaxDistance = Math.Clamp(tempSettings.followLeaderMaxDistance, tempSettings.followLeaderRadius, 60);
                }
                else // LEFT
                {
                    tempSettings.followLeaderRadius += normalizedScroll;
                    tempSettings.followLeaderRadius = Math.Clamp(tempSettings.followLeaderRadius, 1, tempSettings.followLeaderMaxDistance);
                }
                FollowActionPatch.mainFollowerSettings = tempSettings;
                foreach (var agent in zSearch.GetAllBotAgents())
                {
                    var data = zActions.GetOrCreateData(agent);
                    data.followSettingsOverides = FollowActionPatch.followSettingsOverides;
                }
                UpdateMainNode();
            }
            private static void UpdateMainNode()
            {
                var node = followMenuNode;
                if (!FollowActionPatch.mainFollowerSettings.Equals(FollowActionPatch.defaultFollowSettings))
                {
                    node.SetPrefix("* ");
                    node.SetSuffix(" *");
                }
                else
                {
                    node.SetPrefix("");
                    node.SetSuffix("");
                }
                node.SetTitle($"Prio <color=#CC840066>[</color>{FollowActionPatch.mainFollowerSettings.prio}<color=#CC840066>]</color>");
                node.SetSubtitle($"Range <color=#CC840066>[</color>{FollowActionPatch.mainFollowerSettings.followLeaderRadius}/{FollowActionPatch.mainFollowerSettings.followLeaderMaxDistance}<color=#CC840066>]</color>");
            }
            private static void UpdateNodeBasedOnScroll(DRAMA_State state)
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                int normalizedScroll = (int)Mathf.Sign(scroll);
                if (scroll == 0f)
                    return;
                var node = stateNodes[state];
                var pos = Camera.main.WorldToViewportPoint(node.gameObject.transform.position);
                pos = new Vector2(pos.x - 0.5f, pos.y - 0.5f) * -1;
                var tempSettings = FollowActionPatch.followSettingsOverides.GetValueOrDefault(state, FollowActionPatch.mainFollowerSettings);
                if (pos.y > Math.Abs(pos.x)) // TOP
                {
                    tempSettings.prio += normalizedScroll;
                    tempSettings.prio = Math.Clamp(tempSettings.prio, 1, 15);
                }
                else if (pos.x > 0) // RIGHT
                {
                    tempSettings.followLeaderMaxDistance += normalizedScroll;
                    tempSettings.followLeaderMaxDistance = Math.Clamp(tempSettings.followLeaderMaxDistance, tempSettings.followLeaderRadius, 60);
                }
                else // LEFT
                {
                    tempSettings.followLeaderRadius += normalizedScroll;
                    tempSettings.followLeaderRadius = Math.Clamp(tempSettings.followLeaderRadius, 1, tempSettings.followLeaderMaxDistance);
                }
                FollowActionPatch.followSettingsOverides[state] = tempSettings;
                if (tempSettings.Equals(FollowActionPatch.mainFollowerSettings))
                    FollowActionPatch.followSettingsOverides.Remove(state);
                foreach (var agent in zSearch.GetAllBotAgents())
                {
                    var data = zActions.GetOrCreateData(agent);
                    data.followSettingsOverides = FollowActionPatch.followSettingsOverides;
                }
                UpdateStateNode(state);
            }
            private static void UpdateAllStateNodes()
            {
                foreach(var state in stateNodes.Keys)
                {
                    UpdateStateNode(state);
                }
            }
            private static void UpdateStateNode(DRAMA_State state)
            {
                var node = stateNodes[state];
                if (!FollowActionPatch.followSettingsOverides.ContainsKey(state) || FollowActionPatch.followSettingsOverides[state].Equals(FollowActionPatch.mainFollowerSettings))
                {
                    node.SetPrefix("");
                    node.SetSuffix("");
                }
                else
                {
                    node.SetPrefix("* ");
                    node.SetSuffix(" *");
                }
                node.SetTitle($"Prio <color=#CC840066>[</color>{FollowActionPatch.followSettingsOverides.GetValueOrDefault(state, FollowActionPatch.mainFollowerSettings).prio}<color=#CC840066>]</color>"); //Holy shit this is a mess, TODO clean this.
                node.SetSubtitle($"Range <color=#CC840066>[</color>{FollowActionPatch.followSettingsOverides.GetValueOrDefault(state, FollowActionPatch.mainFollowerSettings).followLeaderRadius}/{FollowActionPatch.followSettingsOverides.GetValueOrDefault(state, FollowActionPatch.mainFollowerSettings).followLeaderMaxDistance}<color=#CC840066>]</color>");
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

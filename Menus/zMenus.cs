using Player;
using SlideMenu;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
//using ZombieTweak2.zRootBotPlayerAction.CustomActions;
using BotControl;

namespace BotControl.Menus
{
    public static class zMenus
    {
        //This is the class that actually creates the menue instances
        public static List<sMenu> botMenus;

        public static void CreateMenus()
        {
            sMenuManager.ClearAllMenus();
            OverrideTree<bool?>.ResetTrees();
            OverrideTree<float?>.ResetTrees();
            OverrideTree<int?>.ResetTrees();
            AutomaticActionMenuClass.Setup(sMenuManager.createMenu("Automatic Actions", sMenuManager.mainMenu));
            sMenuManager.mainMenu.AddPannel(sMenu.sMenuPannel.Side.top, "<size=150><color=#CC840066>Slide's Bot Control</color></size>");
            sMenuManager.mainMenu.AddPannel(sMenu.sMenuPannel.Side.top, $"<color=#CC840066>[ </color><color=#26262c>V{ZiMain.version}</color><color=#CC840066> ]</color>");
            sMenuManager.mainMenu.radius = 100f;
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
        public static List<sMenu> autoActionMenus;
        public static sMenu AutoActionMenu;

        internal static void Setup(sMenu _menu)
        {
            zSlideComputer.ActionPriorities = new(-1f, "ActionPriorities");
            zSlideComputer.ActionPermissions = new(true, "ActionPerms");
            autoActionMenus = new List<sMenu>();
            AutoActionMenu = _menu;
            AutoActionMenu.radius = 130f;
            autoActionMenus.Clear();
            //zSlideComputer.actionNameToMenuNodes = new();

            var bioTrackerMenu = sMenuManager.createMenu("Use BioTracker", AutoActionMenu);
            autoActionMenus.Add(bioTrackerMenu);
            var attackMenu = sMenuManager.createMenu("Attack", AutoActionMenu);
            autoActionMenus.Add(attackMenu);
            var reviveMenu = sMenuManager.createMenu("Revive", AutoActionMenu);
            autoActionMenus.Add(reviveMenu);
            var shareMenu = sMenuManager.createMenu("Share", AutoActionMenu);
            autoActionMenus.Add(shareMenu);
            var pingMenu = sMenuManager.createMenu("Ping", AutoActionMenu);
            autoActionMenus.Add(pingMenu);
            var pickupMenu = sMenuManager.createMenu("Pickup", AutoActionMenu);
            autoActionMenus.Add(pickupMenu);
            var followMenu = sMenuManager.createMenu("Follow", AutoActionMenu);
            autoActionMenus.Add(followMenu);
            var unlockMenu = sMenuManager.createMenu("Unlock", AutoActionMenu);
            autoActionMenus.Add(unlockMenu);

            zSlideComputer.PermissionDefinitions.ClearPermissionDefinitions();
            zSlideComputer.PermissionDefinitions.CreatePermissionDeffinition("Revive", true, menu: reviveMenu, node: reviveMenu.GetNode(), ActionTypeToCull: typeof(PlayerBotActionRevive), defaultPriority: 12); 
            zSlideComputer.PermissionDefinitions.CreatePermissionDeffinition("Use BioTracker", true, menu: bioTrackerMenu, node: bioTrackerMenu.GetNode(), ActionTypeToCull: typeof(PlayerBotActionUseEnemyScanner));
            zSlideComputer.PermissionDefinitions.CreatePermissionDeffinition("Attack", true, menu: attackMenu, node: attackMenu.GetNode(), ActionTypeToCull: typeof(PlayerBotActionAttack));
            zSlideComputer.PermissionDefinitions.CreatePermissionDeffinition("Share", true, menu: shareMenu, node: shareMenu.GetNode(), ActionTypeToCull: typeof(PlayerBotActionShareResourcePack), defaultPriority: 10);
            zSlideComputer.PermissionDefinitions.CreatePermissionDeffinition("Ping", true, menu: pingMenu, node: pingMenu.GetNode(), ActionTypeToCull: typeof(PlayerBotActionHighlight), defaultPriority: 4.3f);
            zSlideComputer.PermissionDefinitions.CreatePermissionDeffinition("Follow", true, menu: followMenu, node: followMenu.GetNode(), ActionTypeToCull: typeof(PlayerBotActionFollow), defaultPriority: 14f);
            zSlideComputer.PermissionDefinitions.CreatePermissionDeffinition("Pickup", true, menu: pickupMenu, node: pickupMenu.GetNode(), ActionTypeToCull: typeof(PlayerBotActionCollectItem), defaultPriority: 4.2f);
            zSlideComputer.PermissionDefinitions.CreatePermissionDeffinition("Drop", true, null, ActionTypeToCull: typeof(PlayerBotActionCollectItem));
            zSlideComputer.PermissionDefinitions.CreatePermissionDeffinition("Unlock", true, menu: unlockMenu, node: unlockMenu.GetNode(), ActionTypeToCull: typeof(PlayerBotActionUnlock), defaultPriority: 4.1f);
            zSlideComputer.PermissionDefinitions.CreatePermissionDeffinition("Move", true, null, ActionTypeToCull: typeof(PlayerBotActionWalk));


            foreach (sMenu menu in autoActionMenus)
            {
                sMenu.sMenuNode node = menu.GetNode();
                string actionName = node.text;
                //float defaultPriority = zSlideComputer.PermissionDefinitions.GetDefaultPriority(actionName);
                if (zSlideComputer.ActionPriorities.nodes.Keys.Contains(actionName))
                {
                    float defaultPriority = (float)zSlideComputer.ActionPriorities.GetNodeFromIdent(actionName).DefaultValue;
                    node.AddListener(sMenuManager.nodeEvent.OnHeldImmediateSelected, GenericResetSettings, node);
                    node.AddListener(sMenuManager.nodeEvent.WhileSelected, GenericUpdatePriorityBasedOnScroll, node);
                    GenericUpdateNodePrioDisplay(node);
                }
                node.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
                node.AddListener(sMenuManager.nodeEvent.OnTapped, zSlideComputer.GenericToggleAllowed, actionName, node);
                node.AddListener(sMenuManager.nodeEvent.OnDoubleTapped, menu.Open);
                AutoActionMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediateSelected, GenericResetSettings, node);
            }
            AutoActionMenu.centerNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
            AutoActionMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnTapped, AutoActionMenu.parrentMenu.Open);

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
            AutoActionMenu.AddNodeToCatagory("Favorites", "Attack");
            AutoActionMenu.AddCatagory("Resources");
            AutoActionMenu.AddNodeToCatagory("Resources", "Pickup");
            AutoActionMenu.AddNodeToCatagory("Resources", "Share");
            AutoActionMenu.AddCatagory("Behavior");
            AutoActionMenu.AddNodeToCatagory("Behavior", "Unlock");
            AutoActionMenu.AddNodeToCatagory("Behavior", "Ping");
            AutoActionMenu.AddNodeToCatagory("Behavior", "Follow");
            AutoActionMenu.AddCatagory("Combat");
            AutoActionMenu.AddNodeToCatagory("Combat", "Attack");
            AutoActionMenu.AddNodeToCatagory("Combat", "Revive");
            AutoActionMenu.AddNodeToCatagory("Behavior", "Use BioTracker");

            AutoActionMenu.SetCatagory("Favorites");
        }

        //internal static void GenericUpdateNodeAllowedDisplay(sMenu.sMenuNode node)
        //{
        //    string text = node.text;
        //    bool allowed = (bool)zSlideComputer.ActionPermissions.ValueAt(text);
        //    if (allowed)
        //        node.SetColor(sMenuManager.defaultColor);
        //    else
        //        node.SetColor(new Color(0.25f, 0f, 0f));
        //}
        internal static void GenericUpdateNodeAllowedDisplay(string actionKey, sMenu.sMenuNode node, Color? onColor = null, Color? offColor = null)
        {
            if (onColor == null)
                onColor = sMenuManager.defaultEnabledColor;
            if (offColor == null)
                offColor = sMenuManager.defaultDisabledColor;
            bool allowed = (bool)zSlideComputer.ActionPermissions.ValueAt(actionKey);
            if (allowed)
                node.SetColor((Color)onColor);
            else
                node.SetColor((Color)offColor);
            ApplyTextEffectsToNode(node, actionKey); //TODO this probably should not be bunlded inside this method.
        }
        public static void ApplyTextEffectsToNode(sMenu.sMenuNode node, string actionKey = null, List<IOverrideTree> extraTrees = null)
        {
            if (actionKey == null)
                actionKey = node.text;
            List<IOverrideTree> trees = new();
            if (zSlideComputer.ActionPermissions.HasKey(actionKey))
                trees.Add(zSlideComputer.ActionPermissions);
            if (zSlideComputer.ActionPriorities.HasKey(actionKey))
                trees.Add(zSlideComputer.ActionPriorities);
            if (extraTrees != null)
                foreach (var tree in extraTrees)
                    trees.Add(tree);
            bool star = AutomaticActionMenuClass.AnyTreeOverridesNullDefault(trees, actionKey);
            bool italic = !AutomaticActionMenuClass.AllMatchingDefaultValue(trees, actionKey);
            AutomaticActionMenuClass.ApplyTextEffectToNode(node, AutomaticActionMenuClass.textEffect.Star, star);
            AutomaticActionMenuClass.ApplyTextEffectToNode(node, AutomaticActionMenuClass.textEffect.Italic, italic);
        }
        internal static void GenericUpdatePriorityBasedOnScroll(sMenu.sMenuNode node)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll == 0f)
                return;
            float normalizedScroll = (int)Mathf.Sign(scroll) * 0.1f;
            string actionKey = node.text;
            zSlideComputer.ActionPriorities.SetValue(actionKey, Math.Clamp(Mathf.Round(((float)zSlideComputer.ActionPriorities.ValueAt(actionKey) + normalizedScroll) * 10f) / 10f,1f,15f));
            ApplyTextEffectsToNode(node);
            //GenericUpdateNodePrioDisplay(node);
        }

        public enum textEffect
        {
            Star,
            Bold,
            Italic,
            Underline,
            Color,
        }
        public static Dictionary<textEffect, (string prefix, string suffix)> textEffectDict = new()
        {
            { textEffect.Star, ("*", "*") },
            { textEffect.Bold, ("<b>", "</b>") },
            { textEffect.Italic, ("<i>", "</i>") },
            { textEffect.Underline, ("<u>", "</u>") },
            { textEffect.Color, ("<color=#{0}>", "</color>") },
        };
        //public static void GenericUpdateNodeDefaultDisplay(sMenu.sMenuNode node, string key)
        //{
        //    List<IOverrideTree> trees = new List<IOverrideTree>();
        //    trees.Add(zSlideComputer.ActionPermissions);
        //    trees.Add(zSlideComputer.ActionPriorities);
        //    //ApplyTextEffectBasedOnKeyTree(node, key, trees, textEffect.Bold);
        //    //ApplyTextEffectBasedOnKeyTree(node, key, zSlideComputer.ActionPriorities, textEffect.Star);
        //}
        //public static void ApplyTextEffectBasedOnKeyTree(sMenu.sMenuNode node, string key, IOverrideTree tree, textEffect effect)
        //{
        //    List<IOverrideTree> trees = new();
        //    trees.Add(tree);
        //    ApplyTextEffectBasedOnKeyTree(node, key, trees, effect);
        //}
        //public static void ApplyTextEffectBasedOnKeyTree(sMenu.sMenuNode node, string key, List<IOverrideTree> trees, textEffect effect)
        //{
        //    List<(string key, IOverrideTree tree)> keyTrees = new();
        //    foreach (IOverrideTree tree in trees)
        //        keyTrees.Add((key, tree));
        //    ApplyTextEffectBasedOnKeyTree(node, keyTrees, effect);
        //}
        //public static void ApplyTextEffectBasedOnKeyTree(sMenu.sMenuNode node, List<string> keys, IOverrideTree tree, textEffect effect)
        //{
        //    List<(string key, IOverrideTree tree)> keyTrees = new();
        //    foreach (string key in keys)
        //        keyTrees.Add((key, tree));
        //    ApplyTextEffectBasedOnKeyTree(node, keyTrees, effect);
        //}
        //public static void ApplyTextEffectBasedOnKeyTree(sMenu.sMenuNode node, List<(string key, IOverrideTree tree)> keyTrees, textEffect effect)
        //{
        //    List<(List<string> keys, IOverrideTree tree)> keysTrees = new();
        //    foreach ((string key, IOverrideTree tree) pair in keyTrees)
        //    {
        //        var key = pair.key;
        //        var tree = pair.tree;
        //        List<string> keys = new() { key };
        //        keysTrees.Add((keys, tree));
        //    }
        //    ApplyTextEffectBasedOnKeyTree(node, keysTrees, effect);
        //}
        //public static void ApplyTextEffectBasedOnKeyTree(sMenu.sMenuNode node, List<(List<string> keys, IOverrideTree tree)> keysTrees, textEffect effect)
        //{
        //    bool allDefault = true;
        //    foreach(var keyTree in keysTrees)
        //    {
        //        List<string> keys = keyTree.keys;
        //        IOverrideTree tree = keyTree.tree;
        //        foreach (string key in keys)
        //        {
        //            if (!tree.IsDefaultValue(key))
        //            {
        //                allDefault = false;
        //                break;
        //            }
        //        }
        //        if (!allDefault)
        //            break;
        //    }
        //    ApplyTextEffectToNode(node, effect, !allDefault);
        //}
        public static bool AnyTreeOverridesNullDefault(List<IOverrideTree> trees, string key)
        {
            foreach (var tree in trees)
            {
                bool HasDefaultValue = tree.IHasDefault(key);
                object DefaultValue = tree.IGetDefaultValue(key);
                bool DefaultValueIsNull = DefaultValue == null;
                bool HasValue = tree.IHasValue(key);
                bool HasParrent = tree.IHasParrent(key);
                if (HasValue && DefaultValueIsNull)
                    return true;
            }
            return false;
        }
        public static bool AnyHasValue(List<IOverrideTree> trees, string key)
        {
            foreach (var tree in trees)
                if (tree.IHasValue(key))
                    return true;
            return false;
        }
        public static bool AllMatchingDefaultValue(List<IOverrideTree> trees, string key)
        {
            foreach (var tree in trees)
                if (!tree.IMatchingDefaultValue(key))
                    return false;
            return true;
        }
        public static bool AllDefaultValue(List<IOverrideTree> trees, string key)
        {
            foreach (var tree in trees)
                if (!tree.IsDefaultValue(key))
                    return false;
            return true;
        }
        public static string ApplyTextEffect(string text, textEffect effect, bool enabled = true, Color? color = null)
        {
            string ret = text;

            string prefix = textEffectDict[effect].prefix;
            string suffix = textEffectDict[effect].suffix;

            if (effect == textEffect.Color)
            {
                string colorHex = ColorUtility.ToHtmlStringRGB(color ?? Color.white);
                prefix = string.Format(prefix, colorHex);
            }

            if (enabled)
            {
                if (!ret.Contains(prefix))
                    ret = prefix + ret;
                if (!ret.Contains(suffix))
                    ret += suffix;
            }
            else
            {
                ret = ret.Replace(prefix, "");
                ret = ret.Replace(suffix, "");
            }

            return ret;
        }
        public static void ApplyTextEffectToNode(sMenu.sMenuNode node, textEffect effect, bool enabled = true)
        {
            if (enabled)
            {
                if (!node.prefix.Contains(textEffectDict[effect].prefix))
                    node.SetPrefix(textEffectDict[effect].prefix + node.prefix);
                if (!node.suffix.Contains(textEffectDict[effect].suffix))
                    node.SetSuffix(node.suffix + textEffectDict[effect].suffix);
            }
            else
            {
                node.SetPrefix(node.prefix.Replace(textEffectDict[effect].prefix,""));
                node.SetSuffix(node.suffix.Replace(textEffectDict[effect].suffix,""));
            }
        }
        public static void GenericUpdateNodePrioDisplay(sMenu.sMenuNode node, string key = null)
        {
            if (key == null)
                key = node.text;
            node.SetTitle($"Prio <color=#CC840066>[</color>{zSlideComputer.ActionPriorities.ValueAt(key)}<color=#CC840066>]</color>");
            ApplyTextEffectsToNode(node, key);
        }
        public static void GenericResetSettings(sMenu.sMenuNode node, bool allowDissabled = false, string? actionKey = null)
        {
            if (!node.gameObject.activeInHierarchy && !allowDissabled)
                return;
            if (actionKey == null)
                actionKey = node.text;
            if (zSlideComputer.ActionPermissions.ResetToDefault(actionKey) != null)
                GenericUpdateNodeAllowedDisplay(actionKey, node);
            if (zSlideComputer.ActionPriorities.ResetToDefault(actionKey) != null)
                GenericUpdateNodePrioDisplay(node);
            //GenericApplyTextEffects(node, actionKey);
            
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

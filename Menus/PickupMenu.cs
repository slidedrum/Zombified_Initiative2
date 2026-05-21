using GameData;
using Player;
using SlideMenu;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ZombieTweak2.Patches;
using Zombified_Initiative;
using static SlideMenu.sMenu;

namespace ZombieTweak2.Menus
{
    public static class PickupMenuClass
    {
        //public static Dictionary<uint, sMenu.sMenuNode> prioNodesByID;
        public static sMenu pickupMenu;
        public static sMenu.sMenuNode pickupNode;
        public static OverrideTree<int?> pickupDistance;
        public static void Setup(sMenu menu)
        {
            //prioNodesByID = new Dictionary<uint, sMenu.sMenuNode>();
            pickupDistance = new(15, "pickupDistance");
            pickupMenu = menu;
            pickupMenu.radius = 125f;
            pickupNode = pickupMenu.GetNode();
            pickupDistance.AddNode("Pickup", null, "Default").onChanged.Listen(SetSearchDistance, [pickupDistance.ValueAt("Pickup")]).Listen(UpdateNodeSettingsDisplay, [pickupNode]);

            sMenu.sMenuNode glowstickNode = null;
            string glowstickNameToUse = PickupActionPatch.shortGlowStickNames.FirstOrDefault();

            foreach (string itemName in PickupActionPatch.fullItemNameList)
            {
                //uint itemID = itemName.Key;
                ItemDataBlock block = ItemDataBlock.GetBlock(itemName);
                string publicName = block.publicName;
                sMenu.sMenuNode node = null;
                uint id = block.persistentID;
                bool isGlowstick = PickupActionPatch.fullGlowStickNames.Contains(itemName);
                if (isGlowstick)
                {
                    if (glowstickNode == null)
                    {
                        glowstickNode = pickupMenu.AddNode(glowstickNameToUse);
                        zSlideComputer.ActionPriorities.AddNode("Pickup" + glowstickNameToUse, 10, "Pickup", defaultValue: 10f).onChanged.Listen(updateNodePriorityDisplay, args: [glowstickNameToUse, glowstickNode]);
                        zSlideComputer.ActionPriorities.AddNode("Pickup" + itemName, null, "Pickup" + glowstickNameToUse, defaultValue: null, hasDefaultValue: true);
                        zSlideComputer.ActionPermissions.AddNode("Pickup" + glowstickNameToUse, null, "Pickup", defaultValue: null, hasDefaultValue: true).onChanged.Listen(updateNodePriorityDisplay, args: [glowstickNameToUse, glowstickNode]);
                        zSlideComputer.ActionPermissions.AddNode("Pickup" + itemName, null, "Pickup" + glowstickNameToUse, defaultValue: null, hasDefaultValue: true);
                        //zSlideComputer.actionNameToMenuNodes.Add(glowstickNameTouse, glowstickNode);
                    }
                    else
                    {
                        zSlideComputer.ActionPriorities.AddNode("Pickup" + itemName, null, "Pickup" + glowstickNameToUse, defaultValue: null, hasDefaultValue: true);
                        zSlideComputer.ActionPermissions.AddNode("Pickup" + itemName, null, "Pickup" + glowstickNameToUse, defaultValue: null, hasDefaultValue: true);
                        continue;
                    }
                    node = glowstickNode;
                }
                else
                {
                    float priority = 0f;
                    if (RootPlayerBotAction.s_itemBasePrios.ContainsKey(id))
                        priority = RootPlayerBotAction.s_itemBasePrios[id];
                    node = pickupMenu.AddNode(publicName);
                    zSlideComputer.ActionPriorities.AddNode("Pickup" + itemName, priority, "Pickup", defaultValue: priority).onChanged.Listen(PickupMenuClass.updateNodePriorityDisplay, args: [itemName, node]);
                    zSlideComputer.ActionPermissions.AddNode("Pickup" + itemName, null, "Pickup", defaultValue: null, hasDefaultValue: true).onChanged.Listen(PickupMenuClass.updateNodePriorityDisplay, args: [itemName, node]);
                    if (!PlayerAIBot.s_recognisedItemTypes.Contains(id))
                        PlayerAIBot.s_recognisedItemTypes.Add(id);
                    //zSlideComputer.actionNameToMenuNodes.Add("Pickup"+itemName, node);
                }
                //TODO uncomment then when moved over to overide system instead of selection system.
                //var thisNode = menu.parrentMenu.GetNode(menu.centerNode.text);
                //thisNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
                //thisNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, menu.Open);
                //thisNode.AddListener(sMenuManager.nodeEvent.OnTapped, TogglePerms);
                node.AddListener(sMenuManager.nodeEvent.WhileSelected, ChangePrioBasedOnMouseWheel, itemName, node);
                node.AddListener(sMenuManager.nodeEvent.OnTapped, zSlideComputer.GenericToggleAllowed, "Pickup"+itemName, node);
                node.AddListener(sMenuManager.nodeEvent.OnTapped, updateNodePriorityDisplay, itemName, node);
                node.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, ResetNodeSettings, itemName, node);
                pickupMenu.centerNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
                pickupMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnTapped, pickupMenu.parrentMenu.Open);
                pickupMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, ResetNodeSettings, itemName, node);
                pickupMenu.AddListener(sMenuManager.menuEvent.OnOpened, updateNodePriorityDisplay, itemName, node);
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
            pickupNode.ClearListeners(sMenuManager.nodeEvent.WhileSelected);
            pickupNode.AddListener(sMenuManager.nodeEvent.OnDoubleTapped, pickupMenu.Open);
            pickupNode.AddListener(sMenuManager.nodeEvent.WhileSelected, UpdateNodeBasedOnScroll, pickupNode);
            pickupNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediateSelected, ResetSettings, pickupNode);
            pickupMenu.parrentMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediateSelected, ResetSettings, pickupNode);

            pickupMenu.AddPannel(sMenu.sMenuPannel.Side.top, "Controls what bots will pickup.");
            pickupMenu.AddPannel(sMenu.sMenuPannel.Side.bottom, "Scroll to change the priority of different items.");
            UpdateNodeSettingsDisplay(pickupNode);
        }
        private static void UpdateNodeBasedOnScroll(sMenu.sMenuNode node)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            float normalizedScroll = (int)Mathf.Sign(scroll);
            if (scroll == 0f)
                return;
            string text = node.text;
            var pos = Camera.main.WorldToViewportPoint(node.gameObject.transform.position);
            pos = new Vector2(pos.x - 0.5f, pos.y - 0.5f) * -1;
            //OverrideTree<float?> prio = AutomaticActionMenuClass.ActionPriorities[text];
            if (pos.y > Math.Abs(pos.x)) // TOP
            {
                normalizedScroll = normalizedScroll * 0.1f;
                float newValue = (float)zSlideComputer.ActionPriorities.ValueAt(text) + normalizedScroll;
                newValue = (float)Math.Round(newValue, 1);
                zSlideComputer.ActionPriorities.SetValue(text, Math.Clamp(newValue, 1, 15));
            }
            else
            {
                var newValue = Math.Clamp((int)pickupDistance.ValueAt(text) + (int)normalizedScroll, 1, 60);
                pickupDistance.SetValue(text, newValue);
            }
            UpdateNodeSettingsDisplay(node);
        }
        private static void UpdateNodeSettingsDisplay(sMenu.sMenuNode node)
        {
            string actionKey = node.text;
            //var prio = AutomaticActionMenuClass.ActionPriorities["Pickup"];
            List<IOverrideTree> extraTrees = new()
            {
                pickupDistance,
            };
            //bool italic = AutomaticActionMenuClass.AnyTreeOverridesNullDefault(trees, actionKey);
            //bool star = !AutomaticActionMenuClass.AllMatchingDefaultValue(trees, actionKey);
            //AutomaticActionMenuClass.ApplyTextEffectToNode(node, AutomaticActionMenuClass.textEffect.Star, star);
            //AutomaticActionMenuClass.ApplyTextEffectToNode(node, AutomaticActionMenuClass.textEffect.Italic, italic);
            AutomaticActionMenuClass.GenericUpdateNodePrioDisplay(node);
            AutomaticActionMenuClass.ApplyTextEffects(node, actionKey, extraTrees);
            //node.SetTitle($"Prio <color=#CC840066>[</color>{zSlideComputer.ActionPriorities.ValueAt(text)}<color=#CC840066>]</color>");
            node.SetSubtitle($"Range <color=#CC840066>[</color>{pickupDistance.ValueAt(actionKey)}<color=#CC840066>]</color>");
        }
        private static void SetSearchDistance(float distance)
        {
            RootPlayerBotAction.s_collectItemSearchDistance = distance;
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
        private static void ResetSettings(sMenu.sMenuNode node)
        {
            pickupDistance.ResetToDefault("Pickup");
            zSlideComputer.ActionPermissions.ResetToDefault("Pickup");
            zSlideComputer.ActionPriorities.ResetToDefault("Pickup");
            UpdateNodeSettingsDisplay(node);
        }
        private static void ResetNodeSettings(string itemName, sMenu.sMenuNode node)
        {
            if (!node.gameObject.activeInHierarchy)
                return;
            zSlideComputer.ActionPermissions.ResetToDefault("Pickup" + itemName);
            zSlideComputer.ActionPriorities.ResetToDefault("Pickup" + itemName);
            updateNodePriorityDisplay(itemName, node);
        }
        public static void updateNodePriorityDisplay(string itemName, sMenu.sMenuNode node)
        {
            string actionKey = "Pickup" + itemName;
            //AutomaticActionMenuClass.GenericUpdateNodeDefaultDisplay(node, actionKey);

            List<IOverrideTree> trees = new()
            {
                zSlideComputer.ActionPermissions,
                zSlideComputer.ActionPriorities,
            };
            bool italic = AutomaticActionMenuClass.AnyTreeOverridesNullDefault(trees, actionKey);
            bool star = !AutomaticActionMenuClass.AllMatchingDefaultValue(trees, actionKey);
            AutomaticActionMenuClass.ApplyTextEffectToNode(node, AutomaticActionMenuClass.textEffect.Star, star);
            AutomaticActionMenuClass.ApplyTextEffectToNode(node, AutomaticActionMenuClass.textEffect.Italic, italic);

            float prioValue = (float)zSlideComputer.ActionPriorities.ValueAt(actionKey);
            string hex = ColorUtility.ToHtmlStringRGB(GetPriorityColor(prioValue));
            node.SetSubtitle($"<color=#CC840066>[ </color><color=#{hex}>{prioValue}</color><color=#CC840066> ]</color>");
            AutomaticActionMenuClass.GenericUpdateNodeAllowedDisplay(actionKey, node);
            //if ((bool)zSlideComputer.ActionPermissions.ValueAt(actionKey))
            //    node.SetColor(sMenuManager.defaultColor);
            //else
            //    node.SetColor(new Color(0.25f, 0f, 0f));
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
        public static void ChangePrioBasedOnMouseWheel(string itemName, sMenu.sMenuNode node, int increment = 10)
        {
            if (node == null || !node.gameObject.activeInHierarchy)
                return;
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            int normalizedScroll = (int)Mathf.Sign(scroll);
            if (scroll == 0f)
                return;
            float currentPrio = (float)zSlideComputer.ActionPriorities.ValueAt("Pickup" + itemName);
            zSlideComputer.ActionPriorities.SetValue("Pickup" + itemName, Mathf.Clamp(currentPrio + normalizedScroll * increment, 0, 100));
            updateNodePriorityDisplay(itemName, node);
        }
    }
}

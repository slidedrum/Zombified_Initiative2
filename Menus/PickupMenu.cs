using GameData;
using Player;
using SlideMenu;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BotControl.Patches;
using BotControl;

namespace BotControl.Menus
{
    public static class PickupMenuClass
    {
        //public static Dictionary<uint, sMenu.sMenuNode> prioNodesByID;
        public static sMenu pickupMenu;
        public static sMenu.sMenuNode pickupNode;
        public static OverrideTree<int?> pickupDistance;
        public static Color dropEnabledColor = new Color(0.0f, 0.2f, 0.0f, 1f);
        public static string glowstickNameToUse = PickupActionPatch.shortGlowStickNames.FirstOrDefault();
        public static void Setup(sMenu menu)
        {
            //prioNodesByID = new Dictionary<uint, sMenu.sMenuNode>();
            pickupDistance = new(15, "pickupDistance");
            pickupMenu = menu;
            pickupMenu.radius = 125f;
            pickupNode = pickupMenu.GetNode();
            pickupDistance.AddNode("Pickup", null, "Default").onChanged.Listen(SetSearchDistance, [pickupDistance.ValueAt("Pickup")]).Listen(UpdateNodeSettingsDisplay, [pickupNode]);

            sMenu.sMenuNode glowstickNode = null;
            
            string glowstickActionKey = "Pickup" + glowstickNameToUse;

            foreach (string itemName in PickupActionPatch.fullItemNameList)
            {
                ItemDataBlock block = ItemDataBlock.GetBlock(itemName);
                string publicName = block.publicName;
                string actionKey = "Pickup" + itemName;
                sMenu.sMenuNode node = null;
                uint id = block.persistentID;
                bool isGlowstick = PickupActionPatch.fullGlowStickNames.Contains(itemName);
                if (isGlowstick)
                {
                    bool glowstickNodeExists = glowstickNode != null;
                    if (!glowstickNodeExists)
                    {
                        glowstickNode = pickupMenu.AddNode(glowstickNameToUse);
                        zSlideComputer.ActionPriorities.AddNode(glowstickActionKey, 10, "Pickup", defaultValue: 10f).onChanged.Listen(updateNodeDisplay, args: [glowstickNameToUse, glowstickNode]);
                        zSlideComputer.ActionPermissions.AddNode(glowstickActionKey, null, "Pickup", defaultValue: null, hasDefaultValue: true).onChanged.Listen(updateNodeDisplay, args: [glowstickNameToUse, glowstickNode]);
                        zSlideComputer.ActionPermissions.AddNode("Drop" + glowstickNameToUse, true, "Drop", defaultValue: true, hasDefaultValue: true).onChanged.Listen(updateNodeDisplay, args: [glowstickNameToUse, glowstickNode]); ;
                    }
                    zSlideComputer.ActionPermissions.AddNode(actionKey, null, glowstickActionKey, defaultValue: null, hasDefaultValue: true);
                    zSlideComputer.ActionPriorities.AddNode(actionKey, null, glowstickActionKey, defaultValue: null, hasDefaultValue: true);
                    zSlideComputer.ActionPermissions.AddNode("Drop" + itemName, true, "Drop" + glowstickNameToUse, defaultValue: true, hasDefaultValue: true);
                    node = glowstickNode;
                    if (glowstickNodeExists)
                        continue;
                }
                else
                {
                    float priority = 0f;
                    if (RootPlayerBotAction.s_itemBasePrios.ContainsKey(id))
                        priority = RootPlayerBotAction.s_itemBasePrios[id];
                    node = pickupMenu.AddNode(publicName);
                    zSlideComputer.ActionPriorities.AddNode(actionKey, priority, "Pickup", defaultValue: priority).onChanged.Listen(PickupMenuClass.updateNodeDisplay, args: [itemName, node]);
                    zSlideComputer.ActionPermissions.AddNode(actionKey, null, "Pickup", defaultValue: null, hasDefaultValue: true).onChanged.Listen(PickupMenuClass.updateNodeDisplay, args: [itemName, node]);
                    zSlideComputer.ActionPermissions.AddNode("Drop" + itemName, true, "Drop", defaultValue: true, hasDefaultValue: true).onChanged.Listen(PickupMenuClass.updateNodeDisplay, args: [itemName, node]);
                    if (!PlayerAIBot.s_recognisedItemTypes.Contains(id))
                        PlayerAIBot.s_recognisedItemTypes.Add(id);
                }
                string ColoredDropText = AutomaticActionMenuClass.ApplyTextEffect("Drop", AutomaticActionMenuClass.textEffect.Color, true, dropEnabledColor);
                if (isGlowstick)
                    actionKey = glowstickActionKey;
                node.SetTitle($"<color=#CC840066>[ </color>{ColoredDropText}<color=#CC840066> ]</color>");
                node.AddListener(sMenuManager.nodeEvent.WhileSelected, UpdateAdvancedNodeBasedOnScroll, itemName, node);
                node.AddListener(sMenuManager.nodeEvent.OnTapped, zSlideComputer.GenericToggleAllowed, actionKey, node);
                //node.AddListener(sMenuManager.nodeEvent.OnTapped, updateNodeDisplay, itemName, node);
                node.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, ResetNodeSettings, itemName, node);
                pickupMenu.centerNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
                pickupMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnTapped, pickupMenu.parrentMenu.Open);
                pickupMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, ResetNodeSettings, itemName, node);
                //pickupMenu.AddListener(sMenuManager.menuEvent.OnOpened, updateNodeDisplay, itemName, node);
                node.fullTextPart.SetScale(1f, 1f);
                node.subtitlePart.SetScale(0.75f, 0.75f);
                node.titlePart.SetScale(0.5f, 0.5f);
                node.SetSize(0.75f);
            }
            //pickupMenu.centerNode.AddListener(sMenuManager.nodeEvent.WhileSelected, UpdateCatagoryByScroll);
            //ALL - ENCOUNTERED - RESOURCES - PLACEABLES - THROWABLES - FAVORITES
            pickupMenu.AddCatagory("All");

            pickupMenu.AddNodeToCatagory("Default", "Ammo Pack");
            pickupMenu.AddNodeToCatagory("Default", "MediPack");
            pickupMenu.AddNodeToCatagory("Default", "Tool Refill Pack");
            pickupMenu.AddNodeToCatagory("Default", "Disinfection Pack");
            pickupMenu.AddNodeToCatagory("Default", "C-Foam Grenade");
            pickupMenu.AddNodeToCatagory("Default", "Lock Melter");
            pickupMenu.AddNodeToCatagory("Default", "C-Foam Tripmine");
            pickupMenu.AddNodeToCatagory("Default", "Explosive Trip Mine");
            pickupMenu.AddNodeToCatagory("Default", "Glow Stick");
            pickupMenu.AddNodeToCatagory("Default", "Fog Repeller");
            pickupMenu.AddNodeToCatagory("Default", "C-Foam Grenade");
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
            pickupMenu.AddCatagory("Encountered");
            pickupMenu.SetCatagory("Default");
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
        private static void UpdateAdvancedNodeBasedOnScroll(string itemName, sMenu.sMenuNode node, int increment = 10)
        {
            bool isGlowStick = PickupActionPatch.fullGlowStickNames.Contains(itemName);
            if (isGlowStick)
                itemName = glowstickNameToUse;
            if (node == null || !node.gameObject.activeInHierarchy)
                return;
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll == 0f)
                return;
            int normalizedScroll = (int)Mathf.Sign(scroll);
            var pos = Camera.main.WorldToViewportPoint(node.gameObject.transform.position);
            pos = new Vector2(pos.x - 0.5f, pos.y - 0.5f) * -1;
            if (pos.y > Math.Abs(pos.x)) // TOP
            {
                string actionKey = "Drop" + itemName;
                bool allowed = !(bool)zSlideComputer.ActionPermissions.ValueAt(actionKey);
                zSlideComputer.ActionPermissions.SetValue(actionKey, allowed);
            }
            else
            {
                float currentPrio = (float)zSlideComputer.ActionPriorities.ValueAt("Pickup" + itemName);
                zSlideComputer.ActionPriorities.SetValue("Pickup" + itemName, Mathf.Clamp(currentPrio + normalizedScroll * increment, 0, 100));
            }
            //updateNodeDisplay(itemName, node);
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
            //UpdateNodeSettingsDisplay(node);
        }
        private static void UpdateNodeSettingsDisplay(sMenu.sMenuNode node)
        {
            string actionKey = node.text;
            List<IOverrideTree> extraTrees = new()
            {
                pickupDistance,
            };
            AutomaticActionMenuClass.GenericUpdateNodePrioDisplay(node);
            AutomaticActionMenuClass.ApplyTextEffectsToNode(node, actionKey, extraTrees);
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
            //UpdateNodeSettingsDisplay(node);
        }
        private static void ResetNodeSettings(string itemName, sMenu.sMenuNode node)
        {
            if (!node.gameObject.activeInHierarchy)
                return;
            bool isGlowStick = PickupActionPatch.fullGlowStickNames.Contains(itemName);
            if (isGlowStick)
                itemName = glowstickNameToUse;
            zSlideComputer.ActionPermissions.ResetToDefault("Pickup" + itemName);
            zSlideComputer.ActionPermissions.ResetToDefault("Drop" + itemName);
            zSlideComputer.ActionPriorities.ResetToDefault("Pickup" + itemName);
            //updateNodeDisplay(itemName, node);
        }
        public static void updateNodeDisplay(string itemName, sMenu.sMenuNode node)
        {
            string pickupKey = "Pickup" + itemName;
            string dropKey = "Drop" + itemName;
            AutomaticActionMenuClass.GenericUpdateNodeAllowedDisplay(pickupKey, node);
            List<IOverrideTree> pickupTrees = new()
            {
                zSlideComputer.ActionPermissions,
                zSlideComputer.ActionPriorities,
            };
            List<IOverrideTree> dropTree = new()
            {
                zSlideComputer.ActionPermissions,
            };
            bool star = AutomaticActionMenuClass.AnyTreeOverridesNullDefault(pickupTrees, pickupKey) || AutomaticActionMenuClass.AnyTreeOverridesNullDefault(dropTree, dropKey);
            bool italic = !AutomaticActionMenuClass.AllMatchingDefaultValue(pickupTrees, pickupKey) || !AutomaticActionMenuClass.AllMatchingDefaultValue(dropTree, dropKey);
            AutomaticActionMenuClass.ApplyTextEffectToNode(node, AutomaticActionMenuClass.textEffect.Star, star);
            AutomaticActionMenuClass.ApplyTextEffectToNode(node, AutomaticActionMenuClass.textEffect.Italic, italic);

            float prioValue = (float)zSlideComputer.ActionPriorities.ValueAt(pickupKey);
            string hex = ColorUtility.ToHtmlStringRGB(GetPriorityColor(prioValue));
            node.SetSubtitle($"<color=#CC840066>[ </color><color=#{hex}>{prioValue}</color><color=#CC840066> ]</color>");


            Color color;
            if ((bool)zSlideComputer.ActionPermissions.ValueAt("Drop" + itemName))
                color = dropEnabledColor;
            else
                color = sMenuManager.defaultDisabledColor;
            string ColoredDropText = AutomaticActionMenuClass.ApplyTextEffect("Drop", AutomaticActionMenuClass.textEffect.Color, true, color);
            node.SetTitle($"<color=#CC840066>[ </color>{ColoredDropText}<color=#CC840066> ]</color>");
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
    }
}

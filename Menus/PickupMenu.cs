using GameData;
using SlideMenu;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zombified_Initiative;

namespace ZombieTweak2.Menus
{
    public static class PickupMenuClass
    {
        public static Dictionary<uint, sMenu.sMenuNode> prioNodesByID;
        private static Dictionary<string, List<string>> catagories;
        private static int catagoryIndex;
        public static sMenu pickupMenu;
        public static sMenu.sMenuNode pickupNode;
        public static OverrideTree<int?> pickupDistance;
        public static void Setup(sMenu menu)
        {
            prioNodesByID = new Dictionary<uint, sMenu.sMenuNode>();
            catagories = new();
            catagoryIndex = 1;
            pickupDistance = new(15, "pickupDistance");
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
            pickupNode.ClearListeners(sMenuManager.nodeEvent.WhileSelected);
            pickupNode.AddListener(sMenuManager.nodeEvent.OnDoubleTapped, pickupMenu.Open);
            pickupNode.AddListener(sMenuManager.nodeEvent.WhileSelected, UpdateNodeBasedOnScroll, pickupNode);
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
            OverrideTree<float?> prio = AutomaticActionMenuClass.ActionPriorities[text];
            if (pos.y > Math.Abs(pos.x)) // TOP
            {
                normalizedScroll = normalizedScroll * 0.1f;
                float newValue = (float)prio.ValueAt(text) + normalizedScroll;
                newValue = (float)Math.Round(newValue, 1);
                prio.SetValue(text, Math.Clamp(newValue, 1, 15));
            }
            else
            {
                pickupDistance.SetValue(text, Math.Clamp((int)pickupDistance.ValueAt(text) + (int)normalizedScroll, 1, 60));
            }
            UpdateNodeSettingsDisplay(node);
        }
        private static void UpdateNodeSettingsDisplay(sMenu.sMenuNode node)
        {
            string text = node.text;
            var prio = AutomaticActionMenuClass.ActionPriorities["Pickup"];
            if (prio.nodes[text].IsDefaultValue() && pickupDistance.nodes[text].IsDefaultValue())
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
            node.SetSubtitle($"Range <color=#CC840066>[</color>{pickupDistance.ValueAt(text)}<color=#CC840066>]</color>");
        }
        private static void SetSearchDistance(int playerID, float distance)
        {

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
            zSlideComputer.SetBotItemPriority(itemID, Mathf.Clamp(currentPrio + normalizedScroll * increment, 0, 100));
            updateNodePriorityDisplay(node, itemID);
        }
    }
}

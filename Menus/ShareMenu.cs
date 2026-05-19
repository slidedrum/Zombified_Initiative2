using GameData;
using Player;
using SlideMenu;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZombieTweak2.Menus
{
    public static class ShareMenuClass
    {
        public static Dictionary<uint, sMenu.sMenuNode> packNodesByID;
        public static sMenu shareMenu;
        public static sMenu.sMenuNode shareNode;

        public static void Setup(sMenu menu)
        {
            packNodesByID = new Dictionary<uint, sMenu.sMenuNode>();
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
            shareNode.AddListener(sMenuManager.nodeEvent.OnDoubleTapped, shareMenu.Open);

            shareMenu.AddPannel(sMenu.sMenuPannel.Side.top, "Controls what resources bots will automatically share");
            shareMenu.AddPannel(sMenu.sMenuPannel.Side.top, "You can also change the threshold you must be below for them to share.");
            shareMenu.AddPannel(sMenu.sMenuPannel.Side.bottom, "Scroll to change threshold.");
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
            zSlideComputer.SetResourceThreshold(itemID, Math.Clamp((int)currentThreshold + normalizedScroll * increment, 0, 100));
            updateNodeThresholdDisplay(node, itemID);
        }
    }
}

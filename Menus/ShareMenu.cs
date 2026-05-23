using SlideMenu;
using System;
using System.Collections.Generic;
using UnityEngine;
using BotControl.Patches;

namespace BotControl.Menus
{
    public static class ShareMenuClass
    {
        //public static Dictionary<string, sMenu.sMenuNode> packNodesByItemName;
        public static sMenu shareMenu;
        public static sMenu.sMenuNode shareNode;

        public static void Setup(sMenu menu)
        {
            //packNodesByItemName = new Dictionary<string, sMenu.sMenuNode>();
            shareMenu = menu;
            shareNode = shareMenu.GetNode();
            foreach (string itemName in ShareActionPatch.resourcePackItemNames)
            {
                sMenu.sMenuNode node = shareMenu.AddNode(itemName);
                zSlideComputer.ActionPermissions.AddNode("Share"+itemName, null, "Share", defaultValue: null, hasDefaultValue: true).onChanged.Listen(updateNodeThresholdDisplay, args: [node, itemName]);
                if (itemName == "DisinfectionPack")
                    zSlideComputer.ActionPriorities.AddNode("Share" + itemName, 20f, "Share", defaultValue: 20f).onChanged.Listen(updateNodeThresholdDisplay, args: [node, itemName]);
                else
                    zSlideComputer.ActionPriorities.AddNode("Share" + itemName, 80f, "Share", defaultValue: 80f).onChanged.Listen(updateNodeThresholdDisplay, args: [node, itemName]);
                node.AddListener(sMenuManager.nodeEvent.OnTapped, zSlideComputer.GenericToggleAllowed, "Share"+itemName, node);
                node.AddListener(sMenuManager.nodeEvent.OnTapped, updateNodeThresholdDisplay, node, itemName);
                node.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, ResetSettings, itemName, node);
                node.AddListener(sMenuManager.nodeEvent.WhileSelected, ChangeThresholdBasedOnMouseWheel, itemName, node, 5);
                shareMenu.AddListener(sMenuManager.menuEvent.OnOpened, updateNodeThresholdDisplay, node, itemName);
                shareMenu.centerNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
                shareMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, ResetSettings, itemName, node);
                node.fullTextPart.SetScale(1f, 1f);
                node.subtitlePart.SetScale(0.75f, 0.75f);
                node.titlePart.SetScale(0.5f, 0.5f);
                //zSlideComputer.actionNameToMenuNodes.Add("Share" + itemName, node);
                //packNodesByItemName[itemName] = node;
            }
            shareMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnTapped, shareMenu.parrentMenu.Open);
            shareNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
            shareNode.AddListener(sMenuManager.nodeEvent.OnDoubleTapped, shareMenu.Open);

            shareMenu.AddPannel(sMenu.sMenuPannel.Side.top, "Controls what resources bots will automatically share");
            shareMenu.AddPannel(sMenu.sMenuPannel.Side.top, "You can also change the threshold you must be below for them to share.");
            shareMenu.AddPannel(sMenu.sMenuPannel.Side.bottom, "Scroll to change threshold.");
        }
        public static void ResetSettings(string itemName, sMenu.sMenuNode node)
        {
            zSlideComputer.ActionPermissions.ResetToDefault("Share" + itemName);
            zSlideComputer.ActionPriorities.ResetToDefault("Share" + itemName);
            //sMenu.sMenuNode node = zSlideComputer.actionNameToMenuNodes["Share" + itemName];
            updateNodeThresholdDisplay(node, itemName);
        }
        public static void updateNodeThresholdDisplay(sMenu.sMenuNode node, string itemName)
        {
            string actionKey = "Share" + itemName;
            //AutomaticActionMenuClass.GenericUpdateNodeDefaultDisplay(node, actionKey);
            //if (zSlideComputer.ActionPriorities.IsDefaultValue(actionKey))
            //{
            //    node.SetPrefix("");
            //    node.SetSuffix("");
            //}
            //else
            //{
            //    node.SetPrefix("* ");
            //    node.SetSuffix(" *");
            //}
            //List<IOverrideTree> trees = new()
            //{
            //    zSlideComputer.ActionPermissions,
            //    zSlideComputer.ActionPriorities,
            //};
            //bool italic = AutomaticActionMenuClass.AnyTreeOverridesNullDefault(trees, actionKey);
            //bool star = !AutomaticActionMenuClass.AllMatchingDefaultValue(trees, actionKey);
            //AutomaticActionMenuClass.ApplyTextEffectToNode(node, AutomaticActionMenuClass.textEffect.Star, star);
            //AutomaticActionMenuClass.ApplyTextEffectToNode(node, AutomaticActionMenuClass.textEffect.Italic, italic);

            float threshold = (float)zSlideComputer.ActionPriorities.ValueAt(actionKey);

            string hex = ColorUtility.ToHtmlStringRGB(GetThresholdColor(threshold));
            if (itemName == "DisinfectionPack")
                hex = ColorUtility.ToHtmlStringRGB(GetThresholdColor(100 - threshold));
            //bool allowed = (bool)zSlideComputer.ActionPermissions.ValueAt(actionKey);
            node.SetSubtitle($"<color=#CC840066>[ </color><color=#{hex}>{threshold}</color><color=#CC840066> ]</color>");
            AutomaticActionMenuClass.GenericUpdateNodeAllowedDisplay(actionKey, node);
            //if (allowed)
            //    node.SetColor(sMenuManager.defaultColor);
            //else
            //    node.SetColor(new Color(0.25f, 0f, 0f));
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
        public static void ChangeThresholdBasedOnMouseWheel(string itemName, sMenu.sMenuNode node, int increment = 10)
        {
            if (node == null || !node.gameObject.activeInHierarchy)
                return;
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll == 0f)
                return;
            int normalizedScroll = (int)Mathf.Sign(scroll);
            if (normalizedScroll == 0)
                return;
            int currentThreshold = (int)zSlideComputer.ActionPriorities.ValueAt("Share" + itemName);
            int modifier = normalizedScroll * increment;
            int newValue = currentThreshold + modifier;
            newValue = Math.Clamp(newValue, 0, 100);
            zSlideComputer.ActionPriorities.SetValue("Share" + itemName, newValue);
            updateNodeThresholdDisplay(node, itemName);
        }
    }
}

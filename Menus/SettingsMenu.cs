using SlideMenu;
using System;
using UnityEngine;
using static Il2CppSystem.Threading.SemaphoreSlim;

namespace BotControl.Menus
{
    public static class SettingsMenuClass
    {
        public static float menuSizeStep = 0.1f;
        public static sMenu SettingsMenu;
        public static sMenu TalkPermsMenu;
        public static sMenu.sMenuNode scaleNode;
        public static sMenu.sMenuNode talkNode;
        public static Color onColor = new Color(0, 0.2f, 0);

        public static void Setup(sMenu menu)
        {
            SettingsMenu = menu;
            scaleNode = SettingsMenu.AddNode("Scale");
            TalkPermsMenu = sMenuManager.createMenu("Bots talk in chat", SettingsMenu);
            talkNode = TalkPermsMenu.GetNode();
            talkNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
            talkNode.AddListener(sMenuManager.nodeEvent.OnDoubleTapped, TalkPermsMenu.Open);
            zSlideComputer.ActionPermissions.AddNode("TalkInChat", true, (string)null, defaultValue: true).onChanged.Listen(AutomaticActionMenuClass.GenericUpdateNodeAllowedDisplay, args: ["TalkInChat", talkNode, onColor]);
            talkNode.AddListener(sMenuManager.nodeEvent.OnTapped, zSlideComputer.GenericToggleAllowed, args: ["TalkInChat", talkNode]);
            talkNode.SetColor(onColor);
            scaleNode.AddListener(sMenuManager.nodeEvent.WhileSelected, UpdateScaleByScroll);
            talkNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediateSelected, zSlideComputer.ActionPermissions.ResetToDefault, args: ["TalkInChat"]);
            SettingsMenu.centerNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
            SettingsMenu.centerNode.AddListener(sMenuManager.nodeEvent.WhileSelected, UpdateScaleByScroll);
            SettingsMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnTapped, SettingsMenu.parrentMenu.Open);
            SettingsMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediateSelected, ResetAllSettings);
            scaleNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, ResetScale);
            TalkPermsMenu.centerNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
            TalkPermsMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnTapped, TalkPermsMenu.parrentMenu.Open);
            TalkPermsMenu.AddListener(sMenuManager.menuEvent.OnOpened, AutomaticActionMenuClass.GenericUpdateNodeAllowedDisplay, args: ["TalkInChat", TalkPermsMenu.centerNode, onColor]);

            SetupNode(TalkPermsMenu, "Notify pickup");
            SetupNode(TalkPermsMenu, "Notify pickup fail");
            SetupNode(TalkPermsMenu, "Notify smart selected");
            SetupNode(TalkPermsMenu, "Notify confirm action");
            SetupNode(TalkPermsMenu, "Notify resource share");
            SetupNode(TalkPermsMenu, "Notify share fail");

            UpdateScaleNodeSubtitle();
            SettingsMenu.AddPannel(sMenu.sMenuPannel.Side.top, "More settings coming 'soon'!");
        }
        private static void SetupNode(sMenu parentMenu, string actionKey)
        {
            sMenu.sMenuNode node = parentMenu.AddNode(actionKey);
            zSlideComputer.ActionPermissions.AddNode(actionKey, null, "TalkInChat", defaultValue: null, hasDefaultValue: true).onChanged.Listen(AutomaticActionMenuClass.GenericUpdateNodeAllowedDisplay, args: [actionKey, node]);
            node.AddListener(sMenuManager.nodeEvent.OnTapped, zSlideComputer.GenericToggleAllowed, args: [actionKey, node]);
            node.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, zSlideComputer.ActionPermissions.ResetToDefault, args: [actionKey]);
            parentMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, zSlideComputer.ActionPermissions.ResetToDefault, args: [actionKey]);
        }
        private static void toggleTalk()
        {
            bool allowed = !(bool)zSlideComputer.ActionPermissions.ValueAt("TalkInChat");
            zSlideComputer.ActionPermissions.SetValue("TalkInChat", allowed);
        }
        private static void ResetAllSettings()
        {
            zSlideComputer.ActionPermissions.ResetToDefault("TalkInChat");
            ResetScale();
        }
        private static void ResetTalkInChat()
        {
            zSlideComputer.ActionPermissions.ResetToDefault("TalkInChat");
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
}

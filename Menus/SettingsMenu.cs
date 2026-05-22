using SlideMenu;
using System;
using UnityEngine;
using static Il2CppSystem.Threading.SemaphoreSlim;

namespace ZombieTweak2.Menus
{
    public static class SettingsMenuClass
    {
        public static float menuSizeStep = 0.1f;
        public static sMenu SettingsMenu;
        public static sMenu.sMenuNode scaleNode;
        public static sMenu.sMenuNode talkNode;
        public static Color onColor = new Color(0, 0.2f, 0);

        public static void Setup(sMenu menu)
        {
            SettingsMenu = menu;
            scaleNode = SettingsMenu.AddNode("Scale");
            talkNode = SettingsMenu.AddNode("Bots talk in chat");
            zSlideComputer.ActionPermissions.AddNode("TalkInChat", true, (string) null, defaultValue: true).onChanged.Listen(AutomaticActionMenuClass.GenericUpdateNodeAllowedDisplay, args: ["TalkInChat", talkNode, onColor]);
            talkNode.AddListener(sMenuManager.nodeEvent.OnTapped, toggleTalk);
            talkNode.SetColor(onColor);
            scaleNode.AddListener(sMenuManager.nodeEvent.WhileSelected, UpdateScaleByScroll);
            talkNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediateSelected, ResetTalkInChat);
            SettingsMenu.centerNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
            SettingsMenu.centerNode.AddListener(sMenuManager.nodeEvent.WhileSelected, UpdateScaleByScroll);
            SettingsMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnTapped, SettingsMenu.parrentMenu.Open);
            SettingsMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediateSelected, ResetAllSettings);

            scaleNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, ResetScale);
            UpdateScaleNodeSubtitle();
            SettingsMenu.AddPannel(sMenu.sMenuPannel.Side.top, "More settings coming 'soon'!");
        }
        private static void toggleTalk()
        {
            bool allowed = !(bool)zSlideComputer.ActionPermissions.ValueAt("TalkInChat");
            zSlideComputer.ActionPermissions.SetValue("TalkInChat", allowed);
        }
        private static void ResetAllSettings()
        {
            ResetTalkInChat();
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

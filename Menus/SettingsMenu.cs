using SlideMenu;
using System;
using UnityEngine;

namespace ZombieTweak2.Menus
{
    public static class SettingsMenuClass
    {
        public static float menuSizeStep = 0.1f;
        public static sMenu scaleMenu;
        public static sMenu.sMenuNode scaleNode;

        public static void Setup(sMenu menu)
        {
            scaleMenu = menu;
            scaleNode = scaleMenu.AddNode("Scale");
            scaleNode.AddListener(sMenuManager.nodeEvent.WhileSelected, UpdateScaleByScroll);
            scaleMenu.centerNode.AddListener(sMenuManager.nodeEvent.WhileSelected, UpdateScaleByScroll);
            scaleNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, ResetScale);
            UpdateScaleNodeSubtitle();
            scaleMenu.AddPannel(sMenu.sMenuPannel.Side.top, "More settings coming 'soon'!");
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

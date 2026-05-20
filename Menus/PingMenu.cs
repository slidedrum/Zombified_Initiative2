using Player;
using SlideMenu;
using System;
using UnityEngine;

namespace ZombieTweak2.Menus
{
    public static class PingMenuClass
    {
        public static sMenu pingMenu;
        public static sMenu.sMenuNode pingNode;
        public static void Setup(sMenu menu)
        {
            pingMenu = menu;
            pingNode = menu.GetNode();
            pingNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
            pingNode.AddListener(sMenuManager.nodeEvent.OnDoubleTapped, pingMenu.Open);

            foreach (PlayerBotActionHighlight.Descriptor.TargetTypeEnum type in
                     Enum.GetValues<PlayerBotActionHighlight.Descriptor.TargetTypeEnum>())
            {
                string name = type.ToString();
                sMenu.sMenuNode node = pingMenu.AddNode(name + "s");
                node.AddListener(sMenuManager.nodeEvent.OnTapped, zSlideComputer.GenericToggleAllowed, args: [name, node]);
                node.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, zSlideComputer.ActionPermissions.ResetToDefault, name);
                zSlideComputer.ActionPermissions.AddNode(name, null, "Ping", defaultValue: null, hasDefaultValue: true).onChanged.Listen(UpdateNodeDisplay, args: [name, node]);
                pingMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, zSlideComputer.ActionPermissions.ResetToDefault, name);
            }

            pingMenu.centerNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
            pingMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnTapped, pingMenu.parrentMenu.Open);
            pingMenu.AddPannel(sMenu.sMenuPannel.Side.top, "Controls what bots will point out and mark.");
            pingMenu.AddPannel(sMenu.sMenuPannel.Side.top, "I'd like to add the option for them to ping new things, like resource bags, or even scouts?");
        }
        public static void UpdateNodeDisplay(string key, sMenu.sMenuNode node)
        {
            if ((bool)zSlideComputer.ActionPermissions.ValueAt(key))
                node.SetColor(sMenuManager.defaultColor);
            else
                node.SetColor(new Color(0.25f, 0f, 0f));
        }
    }
}

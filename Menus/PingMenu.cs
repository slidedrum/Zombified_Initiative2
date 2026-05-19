using Player;
using SlideMenu;
using ZombieTweak2.CustomActions.Patches;

namespace ZombieTweak2.Menus
{
    public static class PingMenuClass
    {
        public static sMenu pingMenu;
        public static sMenu.sMenuNode pingNode;
        public static sMenu.sMenuNode doorsNode;
        public static sMenu.sMenuNode containersNode;
        public static sMenu.sMenuNode terminalsNode;
        public static void Setup(sMenu menu)
        {
            pingMenu = menu;
            pingNode = menu.GetNode();
            pingNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
            pingNode.AddListener(sMenuManager.nodeEvent.OnDoubleTapped, pingMenu.Open);

            doorsNode = pingMenu.AddNode("Doors", HighlightActionPatch.ToggleTargetTypePerms, PlayerBotActionHighlight.Descriptor.TargetTypeEnum.Door);
            containersNode = pingMenu.AddNode("Containers", HighlightActionPatch.ToggleTargetTypePerms, PlayerBotActionHighlight.Descriptor.TargetTypeEnum.Container);
            terminalsNode = pingMenu.AddNode("Terminals", HighlightActionPatch.ToggleTargetTypePerms, PlayerBotActionHighlight.Descriptor.TargetTypeEnum.Terminal);

            pingMenu.AddPannel(sMenu.sMenuPannel.Side.top, "Controls what bots will point out and mark.");
            pingMenu.AddPannel(sMenu.sMenuPannel.Side.top, "I'd like to add the option for them to ping new things, like resource bags, or even scouts?");
        }
    }
}

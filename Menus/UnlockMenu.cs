using SlideMenu;

namespace ZombieTweak2.Menus
{
    public static class UnlockMenuClass
    {
        public static sMenu unlockMenu;
        public static sMenu.sMenuNode unlockNode;
        public static void Setup(sMenu menu)
        {
            unlockMenu = menu;
            unlockNode = unlockMenu.GetNode();
            unlockNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
            unlockNode.AddListener(sMenuManager.nodeEvent.OnDoubleTapped, unlockMenu.Open);
            unlockMenu.AddPannel(sMenu.sMenuPannel.Side.top, "This controls if the bots are allowed to smash locks on doors/containers");
            unlockMenu.AddPannel(sMenu.sMenuPannel.Side.bottom, "No settins here (yet?)");
            unlockMenu.AddPannel(sMenu.sMenuPannel.Side.bottom, "I'm not ev en sure what settings you would want.");
        }
    }
}

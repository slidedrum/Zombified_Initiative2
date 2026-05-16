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
        }
    }
}

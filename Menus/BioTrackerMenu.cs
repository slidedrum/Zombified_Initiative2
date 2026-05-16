using SlideMenu;

namespace ZombieTweak2.Menus
{
    public static class BioTrackerMenuClass
    {
        public static sMenu bioTrackerMenu;
        public static sMenu.sMenuNode bioTrackerNode;
        public static void Setup(sMenu menu)
        {
            bioTrackerMenu = menu;
            bioTrackerNode = menu.GetNode();
            bioTrackerNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
            bioTrackerNode.AddListener(sMenuManager.nodeEvent.OnDoubleTapped, bioTrackerMenu.Open);
        }
    }
}

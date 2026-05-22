using SlideMenu;

namespace BotControl.Menus
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
            bioTrackerMenu.AddPannel(sMenu.sMenuPannel.Side.top, "This controls if bots will ping active enemies with a bio tracker.");
            bioTrackerMenu.AddPannel(sMenu.sMenuPannel.Side.top, "Also controls their voicelines for nearby enemies.");
            bioTrackerMenu.AddPannel(sMenu.sMenuPannel.Side.top, "Does nothing if no bots have a biotracker equiped.");
            bioTrackerMenu.AddPannel(sMenu.sMenuPannel.Side.bottom, "This menu has no settings (yet?)");
            bioTrackerMenu.AddPannel(sMenu.sMenuPannel.Side.bottom, "Not even sure what kind of settings you'd want?");
        }
    }
}

using SlideMenu;

namespace BotControl.Menus
{
    public static class ExploreMenuClass
    {
        private static sMenu exploreMenu;
        private static sMenu.sMenuNode exploreNode;
        internal static void Setup(sMenu menu)
        {
            exploreMenu = menu;
            exploreNode = exploreMenu.parrentMenu.GetNode(exploreMenu.centerNode.text);
            exploreNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
            exploreNode.AddListener(sMenuManager.nodeEvent.OnTapped, ToggleExplorePerms);
            exploreNode.AddListener(sMenuManager.nodeEvent.OnDoubleTapped, exploreMenu.Open);
        }
        private static void ToggleExplorePerms()
        {
            return;
            //var bots = zSearch.GetAllBotAgents();
            //foreach (var bot in bots)
            //{
            //    ExploreAction.ToggleExplorePerm(bot);
            //}
            //if (ExploreAction.GetExplorePerm(bots[0]))
            //    exploreNode.SetColor(sMenuManager.defaultColor);
            //else
            //    exploreNode.SetColor(new Color(0.25f, 0f, 0f));
        }
    }
}

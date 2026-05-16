using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SlideMenu
{
    public static class sMenus
    {
        //This is the class that actually creates the menue instances

        public static void CreateMenus()
        {
            sMenuManager.createMenu("TestMenu 1",sMenuManager.mainMenu).AddPannel(sMenu.sMenuPannel.Side.left, "Test1");
            sMenuManager.createMenu("TestMenu 2",sMenuManager.mainMenu);
            sMenuManager.createMenu("TestMenu 3",sMenuManager.mainMenu).GetNode().AddHoverText(sMenu.sMenuPannel.Side.right, "This is test");

        }
    }
}

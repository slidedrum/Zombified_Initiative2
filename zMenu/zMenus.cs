using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zombified_Initiative;

namespace ZombieTweak2.zMenu
{
    public static class zMenus
    {
        public static void CreateMenus()
        {
            zMenu oneMenu = zMenuManager.createMenu("One", zMenuManager.mainMenu);
            zMenu twoMenu = zMenuManager.createMenu("Two", zMenuManager.mainMenu);
            zMenuManager.mainMenu.AddNode("One", oneMenu.Open);
            zMenuManager.mainMenu.AddNode("Two", twoMenu.Open);
            zMenuManager.mainMenu.AddNode("Three");
            zMenuManager.mainMenu.AddNode("Four");
            zMenuManager.mainMenu.AddNode("Five");
           
            oneMenu.AddNode("option 1");
            oneMenu.AddNode("option 2");
            oneMenu.AddNode("option 3");
            oneMenu.AddNode("option 4");
            oneMenu.AddNode("option 5");

            twoMenu.AddNode("option 1");
            twoMenu.AddNode("option 2");
            twoMenu.AddNode("option 3");
            twoMenu.AddNode("option 4");
            twoMenu.AddNode("option 5");

        }
    }
}

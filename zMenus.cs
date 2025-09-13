using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zombified_Initiative;

namespace ZombieTweak2
{
    internal class zMenus
    {
        public static bool setup = false;
        public static ZMenu mainMenu = ZMenuManger.mainMenu;
        public static void setupRadialMenus()
        {
            if (!setup)
            {
                ZMenu allmenu = ZMenuManger.addMenu("All", mainMenu);
                ZMenu option1Menu = ZMenuManger.addMenu("option 1", allmenu);
                allmenu.AddNode("option 1", option1Menu.Show);
                allmenu.AddNode("option 2", testCallback);
                allmenu.AddNode("option 3", testCallback);
                allmenu.AddNode("option 4", testCallback);
                mainMenu.AddNode("All", allmenu.Show);
                mainMenu.AddNode("Dauda", testCallback);
                mainMenu.AddNode("Hacket", testCallback);
                mainMenu.AddNode("Bishop", testCallback);
                mainMenu.AddNode("Woods", testCallback);
                option1Menu.AddNode("longer message", testCallback);
                option1Menu.AddNode("This is a longer message", testCallback);
                option1Menu.AddNode("This is a message with a new line\nwhat does it look like?", testCallback);
                option1Menu.AddNode("This is a much longer message with a without a new line what does this look like?", testCallback);
                setup = true;
            }
        }
        public static void testCallback(ZMenuNode node)
        {
            Zi.sendChatMessage($"Menu button {node.text} pressed");
        }
    }
}

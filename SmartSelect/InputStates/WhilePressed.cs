using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotControl.SmartSelect
{
    internal static partial class ssInputHandler
    {
        public static void WhilePressed()
        {
            TriggerEvent(interactEvent.WhilePressed);
            if (LastPressExists && !KeyPress.lastPress.isTap)
            {
                WhileHold();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotControl.SmartSelect
{
    internal static partial class ssInputHandler
    {
        public static void WhileTapAndHold()
        {
            TriggerEvent(interactEvent.WhileTapAndHold);
            float timebetweenPresses = TimeSince(KeyPress.lastPress.previousKeyPress.previousKeyPress.upTime, KeyPress.lastPress.previousKeyPress.downTime);
            if (LastPressExists && KeyPress.lastPress.isDoublePress && timebetweenPresses < tapThreshold)
            {
                WhileDoubleTapAndHold();
            }
        }
    }
}

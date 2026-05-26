using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotControl.SmartSelect
{
    internal static partial class ssInputHandler
    {
        public static void WhileHold()
        {
            TriggerEvent(interactEvent.WhileHold);
            if (interactEventReadyState[interactEvent.OnHoldImmediate])
            {
                OnHoldImmediate();
            }
            if (PreviousPressExists && KeyPress.lastPress.previousKeyPress.isTap && KeyPress.lastPress.isDoublePress)
            {
                WhileTapAndHold();
            }
        }
    }
}

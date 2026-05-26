using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotControl.SmartSelect
{
    internal static partial class ssInputHandler
    {
        public static void OnPressed()
        {
            TriggerEvent(interactEvent.OnPressed);
            interactEventReadyState[interactEvent.OnUnpressedExclusive] = true;
            if (LastPressExists && KeyPress.lastPress.isDoublePress)
            {
                OnDoublePressed();
            }
        }
    }
}

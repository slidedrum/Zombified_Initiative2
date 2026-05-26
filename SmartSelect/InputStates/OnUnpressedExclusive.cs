using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotControl.SmartSelect
{
    internal static partial class ssInputHandler
    {
        private static List<interactEvent> interactOnPressedExclusions = new() { interactEvent.OnUnpressedExclusive };
        public static void OnUnpressedExclusive()
        {
            TriggerEvent(interactEvent.OnUnpressedExclusive);
            interactEventReadyState[interactEvent.OnUnpressedExclusive] = false;
            if (LastPressExists && KeyPress.lastPress.isTap && !KeyPress.lastPress.isDoublePress)
            {
                OnTappedExclusive();
            }
            ResetinteractEventReadyStates(interactOnPressedExclusions);
        }
    }
}

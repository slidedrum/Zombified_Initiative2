using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotControl.SmartSelect
{
    internal static partial class ssInputHandler
    {
        public static void WhileUnpressed()
        {
            TriggerEvent(interactEvent.WhileUnpressed);
            if (LastPressExists && TimeSince(KeyPress.lastPress.upTime) > tapThreshold)
            { // if time since the last press was released is longer than tap threshold.
                WhileUnpressedExclusive();
            }
        }
    }
}

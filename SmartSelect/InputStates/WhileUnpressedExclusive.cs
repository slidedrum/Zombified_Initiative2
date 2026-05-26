using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotControl.SmartSelect
{
    internal static partial class ssInputHandler
    {
        public static void WhileUnpressedExclusive()
        {
            TriggerEvent(interactEvent.WhileUnpressedExclusive);
            if (interactEventReadyState[interactEvent.OnUnpressedExclusive])
            {
                OnUnpressedExclusive(); 
            }
        }
    }
}

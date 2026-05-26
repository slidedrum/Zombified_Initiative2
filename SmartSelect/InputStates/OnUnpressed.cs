using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotControl.SmartSelect
{
    internal static partial class ssInputHandler
    {
        public static void OnUnpressed()
        {
            TriggerEvent(interactEvent.OnUnpressed);
            if (LastPressExists)
            {
                if (KeyPress.lastPress.isTap)
                {
                    OnTapped();
                }
                else
                {
                    OnHold();
                }
                if (KeyPress.lastPress.isDoublePress)
                {
                    OnDoubleTapped();
                }
            }
        }
    }
}

using System.Collections.Generic;

namespace BotControl.SmartSelect
{
    internal static partial class ssInputHandler
    {
        private static List<KeyPress.PressType> doubleTapSequence = new(){ KeyPress.PressType.tap, KeyPress.PressType.tap};
        public static void OnDoubleTapped()
        {
            TriggerEvent(interactEvent.OnDoubleTapped);
        }
    }
}

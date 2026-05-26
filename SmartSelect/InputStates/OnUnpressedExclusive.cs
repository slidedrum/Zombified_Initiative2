using System.Collections.Generic;

namespace BotControl.SmartSelect
{
    internal static partial class ssInputHandler
    {
        private static List<interactEvent> interactOnPressedExclusions = new() { interactEvent.OnUnpressedExclusive };
        public static void OnUnpressedExclusive()
        {
            TriggerEvent(interactEvent.OnUnpressedExclusive);
            interactEventReadyState[interactEvent.OnUnpressedExclusive] = false;
            ResetinteractEventReadyStates(interactOnPressedExclusions);
        }
    }
}

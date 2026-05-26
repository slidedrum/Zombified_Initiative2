namespace BotControl.SmartSelect
{
    internal static partial class ssInputHandler
    {
        public static void WhileDoubleTapAndHold()
        {
            TriggerEvent(interactEvent.WhileDoubleTapAndHold);
            if (interactEventReadyState[interactEvent.OnDoubleTapAndHoldImmediate])
            {
                OnDoubleTapAndHoldImmediate();
                interactEventReadyState[interactEvent.OnDoubleTapAndHoldImmediate] = false;
            }
        }
    }
}

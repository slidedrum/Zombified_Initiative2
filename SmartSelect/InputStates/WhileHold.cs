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
            if (PreviousPressExists && KeyPress.lastPress.PreviousKeyPress.IsTap && KeyPress.lastPress.IsDoublePress)
            {
                WhileTapAndHold();
            }
        }
    }
}

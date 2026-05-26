namespace BotControl.SmartSelect
{
    internal static partial class ssInputHandler
    {
        public static void OnHoldImmediate()
        {
            TriggerEvent(interactEvent.OnHoldImmediate);
            interactEventReadyState[interactEvent.OnHoldImmediate] = false;
            if (PreviousPressExists && KeyPress.lastPress.PreviousKeyPress.IsTap && KeyPress.lastPress.IsDoublePress)
            {
                OnTapAndHoldImmediate();
            }
        }
    }
}

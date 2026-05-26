namespace BotControl.SmartSelect
{
    internal static partial class ssInputHandler
    {
        public static void OnPressed()
        {
            TriggerEvent(interactEvent.OnPressed);
            interactEventReadyState[interactEvent.OnUnpressedExclusive] = true;
            if (LastPressExists && KeyPress.lastPress.IsDoublePress)
            {
                OnDoublePressed();
            }
        }
    }
}

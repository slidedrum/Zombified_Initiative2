namespace BotControl.SmartSelect
{
    internal static partial class ssInputHandler
    {
        public static void OnTapAndHold()
        {
            TriggerEvent(interactEvent.OnTapAndHold);
            if (PreviousPressExists && KeyPress.lastPress.PreviousKeyPress.IsDoublePress)
            {
                OnDoubleTapAndHold();
            }
        }
    }
}

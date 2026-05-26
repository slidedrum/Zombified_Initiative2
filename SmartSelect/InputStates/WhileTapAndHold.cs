namespace BotControl.SmartSelect
{
    internal static partial class ssInputHandler
    {
        public static void WhileTapAndHold()
        {
            TriggerEvent(interactEvent.WhileTapAndHold);
            float timebetweenPresses = TimeSince(KeyPress.lastPress.PreviousKeyPress.PreviousKeyPress.UpTimestamp, KeyPress.lastPress.PreviousKeyPress.DownTimestamp);
            if (LastPressExists && KeyPress.lastPress.IsDoublePress && timebetweenPresses < tapThreshold)
            {
                WhileDoubleTapAndHold();
            }
        }
    }
}

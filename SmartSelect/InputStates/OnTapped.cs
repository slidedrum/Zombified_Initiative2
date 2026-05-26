namespace BotControl.SmartSelect
{
    internal static partial class ssInputHandler
    {
        public static void OnTapped()
        {
            TriggerEvent(interactEvent.OnTapped);
            if (LastPressExists && KeyPress.lastPress.IsDoublePress)
            {
                OnDoubleTappedStrict();
            }
        }
    }
}

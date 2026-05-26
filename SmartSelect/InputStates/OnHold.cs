namespace BotControl.SmartSelect
{
    internal static partial class ssInputHandler
    {
        public static void OnHold()
        {
            TriggerEvent(interactEvent.OnHold);
            interactEventReadyState[interactEvent.OnHold] = false;
            if (PreviousPressExists && KeyPress.lastPress.previousKeyPress.isTap && KeyPress.lastPress.isDoublePress)
            {
                OnTapAndHold();
            }
        }
    }
}

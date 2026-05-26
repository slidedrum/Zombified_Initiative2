namespace BotControl.SmartSelect
{
    internal static partial class ssInputHandler
    {
        public static void OnUnpressed()
        {
            TriggerEvent(interactEvent.OnUnpressed);
            if (LastPressExists)
            {
                if (KeyPress.lastPress.IsTap)
                {
                    OnTapped();
                }
                else
                {
                    OnHold();
                }
                if (KeyPress.lastPress.IsDoublePress)
                {
                    OnDoubleTapped();
                }
            }
        }
    }
}

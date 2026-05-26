namespace BotControl.SmartSelect
{
    internal static partial class ssInputHandler
    {
        public static void WhilePressed()
        {
            TriggerEvent(interactEvent.WhilePressed);
            if (LastPressExists && !KeyPress.lastPress.IsTap)
            {
                WhileHold();
            }
        }
    }
}

namespace BotControl.SmartSelect
{
    internal static partial class ssInputHandler
    {
        public static void WhileUnpressed()
        {
            TriggerEvent(interactEvent.WhileUnpressed);
            if (LastPressExists)
            {
                if (TimeSince(KeyPress.lastPress.UpTimestamp) > tapThreshold)
                { // if time since the last press was released is longer than tap threshold.
                    WhileUnpressedExclusive();
                }
            }
        }
    }
}

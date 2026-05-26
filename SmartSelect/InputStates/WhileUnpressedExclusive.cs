namespace BotControl.SmartSelect
{
    internal static partial class ssInputHandler
    {
        public static void WhileUnpressedExclusive()
        {
            TriggerEvent(interactEvent.WhileUnpressedExclusive);
            if (interactEventReadyState[interactEvent.OnUnpressedExclusive])
            {
                OnUnpressedExclusive(); 
            }
        }
    }
}

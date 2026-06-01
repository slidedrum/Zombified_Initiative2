using LevelGeneration;

namespace BotControl.Patches.Extentions
{
    public static class WeakDoorExtensions
    {
        public static bool IsOpen(this LG_WeakDoor door)
        {
            return door != null && door.Gate.IsTraversable;
        }

        public static bool CanOpen(this LG_WeakDoor door)
        {
            return door != null
                && door.InteractionAllowed
                && door.LastStatus != eDoorStatus.Open
                && door.LastStatus != eDoorStatus.Destroyed;
        }

        public static bool CanClose(this LG_WeakDoor door)
        {
            return door != null
                && door.InteractionAllowed
                && door.LastStatus == eDoorStatus.Open;
        }
    }
}

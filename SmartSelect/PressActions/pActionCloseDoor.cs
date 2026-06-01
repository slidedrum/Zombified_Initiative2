using LevelGeneration;
using UnityEngine;

namespace BotControl.SmartSelect.PressActions
{
    public class pActionCloseDoor : PressAction
    {
        public override string FriendlyName => "Close Door";
        public override string FriendlyNameShort => "Close";
        public override bool Invoke(Component BestComponent)
        {
            LG_WeakDoor Door = BestComponent.Cast<LG_WeakDoor>();
            //TODO
            return false;
        }
    }
}

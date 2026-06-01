using LevelGeneration;
using UnityEngine;

namespace BotControl.SmartSelect.PressActions
{
    public class pActionSecureDoor : PressAction
    {
        public override string FriendlyName => "Secure Door";
        public override string FriendlyNameShort => "Secure";
        public override bool Invoke(Component BestComponent)
        {
            LG_WeakDoor Door = BestComponent.Cast<LG_WeakDoor>();
            //TODO
            // Throw cFoam at the door if they have it.
            return false;
        }
    }
}

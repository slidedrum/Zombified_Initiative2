using LevelGeneration;
using UnityEngine;

namespace BotControl.SmartSelect.PressActions
{
    public class pActionOpenDoor : PressAction
    {
        public override string FriendlyName => "Open Door";
        public override string FriendlyNameShort => "Open";
        public override bool Invoke(Component BestComponent)
        {
            LG_WeakDoor Door = BestComponent.Cast<LG_WeakDoor>();
            //TODO
            return false;
        }
    }
}

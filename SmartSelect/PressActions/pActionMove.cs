using LevelGeneration;
using UnityEngine;

namespace BotControl.SmartSelect.PressActions
{
    public class pActionMove : PressAction
    {
        public override string FriendlyName => "Move To";
        public override string FriendlyNameShort => "Move";
        public override bool Invoke(Component BestComponent)
        {
            // NOTE BestComponent is null!
            //TODO
            return false;
        }
    }
}

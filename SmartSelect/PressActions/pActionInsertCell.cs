using LevelGeneration;
using UnityEngine;

namespace BotControl.SmartSelect.PressActions
{
    public class pActionInsertCell : PressAction
    {
        public override string FriendlyName => "Insert Cell";
        public override string FriendlyNameShort => "Insert";
        public override bool Invoke(Component BestComponent)
        {
            LG_PowerGenerator_Core Generator = BestComponent.Cast<LG_PowerGenerator_Core>();
            //TODO
            return false;
        }
    }
}

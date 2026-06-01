using LevelGeneration;
using UnityEngine;

namespace BotControl.SmartSelect.PressActions
{
    public class pActionOpenContainer : PressAction
    {
        public override string FriendlyName => "Open Container";
        public override string FriendlyNameShort => "Open";
        public override bool Invoke(Component BestComponent)
        {
            LG_WeakResourceContainer container = BestComponent.Cast<LG_WeakResourceContainer>();
            //TODO
            // This might require custom action framework before i can do this.
            return false;
        }
    }
}

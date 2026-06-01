using LevelGeneration;
using Player;
using System.Linq;
using UnityEngine;

namespace BotControl.SmartSelect.PressActions
{
    public class pActionPlaceItem : PressAction
    {
        public override string FriendlyName => "Place Item InContainer";
        public override string FriendlyNameShort => "Place";
        public override bool Invoke(Component BestComponent)
        {
            LG_WeakResourceContainer Container = BestComponent.Cast<LG_WeakResourceContainer>();
            // TODO
            return false;
        }
    }
}

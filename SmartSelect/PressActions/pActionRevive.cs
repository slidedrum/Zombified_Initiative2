using Player;
using UnityEngine;

namespace BotControl.SmartSelect.PressActions
{
    public class pActionRevive : PressAction
    {
        public override string FriendlyName => "Revive";
        public override string FriendlyNameShort => "Revive";
        public override bool Invoke(Component BestComponent)
        {
            PlayerAgent Agent = BestComponent.Cast<PlayerAgent>();
            //TODO
            return false;
        }
    }
}

using LevelGeneration;
using UnityEngine;

namespace BotControl.SmartSelect.PressActions
{
    public class pActionRefillSentry : PressAction
    {
        public override string FriendlyName => "Refill Sentry";
        public override string FriendlyNameShort => "Refill";
        public override bool Invoke(Component BestComponent)
        {
            SentryGunInstance Sentry = BestComponent.Cast<SentryGunInstance>();
            //TODO
            return false;
        }
    }
}

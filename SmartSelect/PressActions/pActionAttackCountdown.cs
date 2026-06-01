using Enemies;
using LevelGeneration;
using UnityEngine;

namespace BotControl.SmartSelect.PressActions
{
    public class pActionAttackCountdown : PressAction
    {
        public override string FriendlyName => "Attack Countdown Enemy";
        public override string FriendlyNameShort => "Attack";
        public override bool Invoke(Component BestComponent)
        {
            EnemyAgent Enemy = BestComponent.Cast<EnemyAgent>();
            //TODO
            // Make sure to handle sleepers and big enemy types.
            // Sleepers should have a chance of being woke up the longer they are moving.
            // Big enemies should be handled differently somehow.
            // Scouts should have a very low chance of working somehow.  idk how that'll work tho.
            return false;
        }
    }
}

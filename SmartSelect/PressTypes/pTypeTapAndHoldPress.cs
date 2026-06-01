using BotControl.SmartSelect.PressActions;
using Il2CppInterop.Runtime;
using LevelGeneration;
using Player;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BotControl.SmartSelect.PressTypes
{
    public class pTypeTapAndHoldPress : PressType
    {
        private HashSet<Il2CppSystem.Type> _SelectableTypes;
        private Component _CurrentComponent = null;
        private PressAction _CurrentAction = null;
        public override HashSet<Il2CppSystem.Type> SelectableTypes => _SelectableTypes;
        public override Component CurrentComponent => _CurrentComponent;
        public override PressAction CurrentAction => _CurrentAction;

        public pTypeTapAndHoldPress()
        {
            _SelectableTypes = new HashSet<Il2CppSystem.Type>();
            _SelectableTypes.Add(Il2CppType.Of<PlayerAIBot>());
            _SelectableTypes.Add(Il2CppType.Of<SentryGunInstance>());
            _SelectableTypes.Add(Il2CppType.Of<LG_WeakDoor>());
        }
        public override bool SetCurrentAction()
        {
            PlayerAIBot BestBot = zSmartSelect.MainSelection.GetSelected<PlayerAIBot>().FirstOrDefault();
            if (BestBot == null)
            {
                _CurrentAction = null;
                return false;
            }
            var destinationPosition = zStaticRefrences.LocalPlayer.FPSCamera.CameraRayPos;
            if (zHelpers.PositionIsValidForAgent(BestBot.Agent, ref destinationPosition))
            {
                _CurrentAction = PressAction.GetAction("Move To");
                return true;
            }
            _CurrentAction = null;
            return false;
        }
        public override bool SeCurrentComponent()
        {
            return zSmartSelect.MainSelection.GetSelected<PlayerAIBot>().FirstOrDefault();
        }
    }
}

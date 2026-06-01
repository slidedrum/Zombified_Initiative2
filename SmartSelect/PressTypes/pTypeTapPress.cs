using BotControl.SmartSelect.PressActions;
using Il2CppInterop.Runtime;
using LevelGeneration;
using Player;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BotControl.SmartSelect.PressTypes
{
    public class pTypeTapPress : PressType
    {
        private HashSet<Il2CppSystem.Type> _SelectableTypes;
        private Component _CurrentComponent = null;
        private PressAction _CurrentAction = null;
        public override HashSet<Il2CppSystem.Type> SelectableTypes => _SelectableTypes;
        public override Component CurrentComponent => _CurrentComponent;
        public override PressAction CurrentAction => _CurrentAction;

        public pTypeTapPress()
        {
            _SelectableTypes = new HashSet<Il2CppSystem.Type>();
            _SelectableTypes.Add(Il2CppType.Of<PlayerAIBot>());
            _SelectableTypes.Add(Il2CppType.Of<SentryGunInstance>());
            _SelectableTypes.Add(Il2CppType.Of<LG_WeakDoor>());
        }
        public override bool SetCurrentAction()
        {
            Il2CppSystem.Type type = CurrentComponent?.GetIl2CppType();
            PlayerAIBot bot;
            if (type == null)
            {
                bool facingUp = Vector3.Angle(zStaticRefrences.CameraTransform.forward, Vector3.up) < 15f;
                if (facingUp && zSmartSelect.MainSelection.Selected<PlayerAIBot>())
                {
                    //DeselectAllBots();
                    _CurrentAction = PressAction.GetAction("Deselect All Bots");
                    return true;
                }
            }
            else if (zHelpers.IsOfType<PlayerAIBot>(type))
            {
                _CurrentAction = PressAction.GetAction("Select Bot");
                return true;
            }
            else if (zHelpers.IsOfType<SentryGunInstance>(type))
            {
                SentryGunInstance sentry = CurrentComponent.Cast<SentryGunInstance>();
                bot = sentry?.Owner?.GetComponent<PlayerAIBot>();
                if (bot != null)
                {
                    _CurrentAction = PressAction.GetAction("Pickup Sentry");
                    return true;
                }
            }
            else if (zHelpers.IsOfType<LG_WeakDoor>(type))
            {
                LG_WeakDoor Door = CurrentComponent.Cast<LG_WeakDoor>();
                if (!zSmartSelect.MainSelection.GetSelected<PlayerAIBot>().FirstOrDefault())
                {
                    if (!Door.InteractionAllowed)
                    {
                        _CurrentAction = null;
                        return false;
                    }
                    if (Door.Gate.IsTraversable)
                    {
                        _CurrentAction = PressAction.GetAction("Close Door");
                    }
                    _CurrentAction = PressAction.GetAction("Open Door");
                    return true;
                }
            }
            bot = zSmartSelect.GetBotLookingAt();
            if (bot != null)
            {
                _CurrentAction = PressAction.GetAction("Select Bot");
                return true;
            }
            _CurrentAction = null;
            return false; 
        }
        public override bool SeCurrentComponent()
        {
            _CurrentComponent = zSearch.FindBestInView(zStaticRefrences.CameraTransform, SelectableTypes, MaxAngle: SelectionAngle);
            if (_CurrentComponent == null)
                _CurrentComponent = zSmartSelect.GetBotLookingAt();
            return CurrentComponent != null;
        }
    }
}

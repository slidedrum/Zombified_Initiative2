using SlideMenu;
using System;
using System.Collections.Generic;
using UnityEngine;
using static BotControl.SmartSelect.zSmartSelect;

namespace BotControl.SmartSelect
{
    internal static class ssInputHandler
    {
        public enum interactEvent
        {
            OnPressed,
            WhilePressed,
            OnUnpressed,
            OnTapped,
            OnTappedExclusive,
            OnDoubleTapped,
            OnHeld,
            WhileHeld,
            OnHeldImmediate,
            OnDoubleTapAndHold,
            WhileDoubleTapAndHold,
        }
        private static Dictionary<interactEvent, FlexibleEvent> _eventMap;
        public static Dictionary<interactEvent, FlexibleEvent> eventMap
        {
            get
            {
                if (_eventMap == null)
                {
                    _eventMap = new();

                    foreach (interactEvent evt in Enum.GetValues<interactEvent>())
                    {
                        _eventMap[evt] = new FlexibleEvent();
                    }
                }
                return _eventMap;
            }
        }
        private static bool pressable = false;
        public static void Update()
        {
            bool ready = FocusStateManager.CurrentState == eFocusState.FPS || FocusStateManager.CurrentState == eFocusState.Dead;
            if (!ready) return;
            bool keyDown = Input.GetKeyDown(key);
            if (keyDown)
            {
                if (pressable) //is this the first frame of holding the button?
                {
                    eventMap[interactEvent.OnPressed].Invoke();
                }
                else //this is not the first frame of holding the button.
                {
                    
                }
                pressable = false;
            }
            else //we are not holding the button
            {
                pressable = true; //we can have a first frame again
            }
        }
    }
}

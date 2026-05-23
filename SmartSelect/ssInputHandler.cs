using SlideMenu;
using InControl;
using System.Collections.Generic;
using static ZombieTweak2.SmartSelect.zSmartSelect;
using System;

namespace ZombieTweak2.SmartSelect
{
    internal static class ssInputHandler
    {
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
        public static void Update()
        {
            bool ready = FocusStateManager.CurrentState == eFocusState.FPS || FocusStateManager.CurrentState == eFocusState.Dead;
            bool keyDown = Input.GetKeyDown(key);
            if (!ready) return;
            if (Input.GetKeyDown(key))
            {
                onKeyDown();
            }
            if (Input.GetKeyUp(key))
            {
                onKeyUp();
            }
            if (Input.GetKey(key))
            {
                onKey();
            }
        }
    }
}

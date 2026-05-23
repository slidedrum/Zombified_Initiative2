using SlideMenu;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BotControl.SmartSelect
{
    internal static class ssInputHandler
    {
        private const KeyCode key = KeyCode.V;

        private const float tapThreshold = 0.2f;
        private const float doubleTapThreshold = 0.3f;

        public enum interactEvent
        {
            OnPressed, //Triggered on the first frame the button is pressed
            WhilePressed, //Triggered on every frame the button is pressed
            OnUnpressed, //Triggered the first frame the button goes from pressed to unpressed
            OnTapped, //Triggered when the hold time is less than tap threshold
            OnTappedExclusive, //Triggered if tapped, but invoked after double tap window expires to make sure it's not a double tap
            OnDoubleTapped, //triggered on the first frame the 2nd tap is released
            OnHeld, //Triggered the first frame the button is released if the tap threshold is exceeded
            WhileHeld, //triggered every frame the button is held starting after the tap threshold is exceeded.
            OnHeldImmediate, //Triggered the first frame when the tap threshold is exceeded.
            OnTapAndHold, //Triggered after a single tap is triggered and then the tap threshold is exceded on what would be a double tap.  This is triggered on one frame, after the hold is released. (2 total presses)
            WhileTapAndHold, //Triggered after a single tap is triggered and then the tap threshold is exceded on what would be a double tap.  This is triggered every frame until the key is released. (2 total presses)
            OnTapAndHoldImmediate, //Triggered after a single tap is triggered and then the tap threshold is exceded on what would be a double tap.  This is triggered on the single frame when the tap threshold is passed. (2 total presses)
            OnDoubleTapAndHold, //Triggered after a double tap is triggered and then the tap threshold is exceded on what would be a tripple tap. This is triggered on one frame, after the hold is released. (3 total presses)
            WhileDoubleTapAndHold, //Triggered after a double tap is triggered and then the tap threshold is exceded on what would be a tripple tap.  This is triggered every frame until the key is released. (3 total presses)
            OnTapDoubleAndHoldImmediate, //Triggered after a double tap is triggered and then the tap threshold is exceded on what would be a tripple tap.  This is triggered on the single frame when the tap threshold is passed. (3 total presses)
        }

        private enum tapStage
        {
            None,
            SingleTap,
            DoubleTap,
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

        private static bool previousHeld = false;
        private static bool pendingExclusiveTap = false;

        private static bool holdQualified = false;
        private static bool tapAndHoldQualified = false;
        private static bool doubleTapAndHoldQualified = false;

        private static float pressTime = 0f;
        private static float pendingTapTime = 0f;

        private static tapStage currentStage = tapStage.None;

        public static void Update()
        {
            bool ready =
                FocusStateManager.CurrentState == eFocusState.FPS ||
                FocusStateManager.CurrentState == eFocusState.Dead;

            if (!ready)
                return;

            float now = Time.time;

            bool currentHeld = Input.GetKey(key);

            bool keyDown = currentHeld && !previousHeld;
            bool keyUp = !currentHeld && previousHeld;
            bool keyHeld = currentHeld;

            //====================================================
            // EXCLUSIVE TAP TIMEOUT
            //====================================================
            if (pendingExclusiveTap)
            {
                if (now - pendingTapTime > doubleTapThreshold)
                {
                    pendingExclusiveTap = false;

                    TriggerEvent(interactEvent.OnTappedExclusive);

                    currentStage = tapStage.None;
                }
            }

            //====================================================
            // PRESS
            //====================================================
            if (keyDown)
            {
                pressTime = now;

                holdQualified = false;
                tapAndHoldQualified = false;
                doubleTapAndHoldQualified = false;

                TriggerEvent(interactEvent.OnPressed);

                // determine chain stage
                if (pendingExclusiveTap &&
                    now - pendingTapTime <= doubleTapThreshold)
                {
                    if (currentStage == tapStage.SingleTap)
                    {
                        currentStage = tapStage.DoubleTap;
                    }
                }
            }

            //====================================================
            // HELD
            //====================================================
            if (keyHeld)
            {
                TriggerEvent(interactEvent.WhilePressed);

                float heldDuration = now - pressTime;

                bool crossedThreshold =
                    heldDuration >= tapThreshold;

                if (crossedThreshold)
                {
                    //================================================
                    // SINGLE HOLD
                    //================================================
                    if (currentStage == tapStage.None)
                    {
                        TriggerEvent(interactEvent.WhileHeld);

                        if (!holdQualified)
                        {
                            holdQualified = true;

                            TriggerEvent(interactEvent.OnHeldImmediate);
                        }
                    }

                    //================================================
                    // TAP + HOLD
                    //================================================
                    else if (currentStage == tapStage.SingleTap)
                    {
                        TriggerEvent(interactEvent.WhileTapAndHold);

                        if (!tapAndHoldQualified)
                        {
                            tapAndHoldQualified = true;

                            TriggerEvent(interactEvent.OnTapAndHoldImmediate);
                        }
                    }

                    //================================================
                    // DOUBLE TAP + HOLD
                    //================================================
                    else if (currentStage == tapStage.DoubleTap)
                    {
                        TriggerEvent(interactEvent.WhileDoubleTapAndHold);

                        if (!doubleTapAndHoldQualified)
                        {
                            doubleTapAndHoldQualified = true;

                            TriggerEvent(interactEvent.OnTapDoubleAndHoldImmediate);
                        }
                    }
                }
            }

            //====================================================
            // RELEASE
            //====================================================
            if (keyUp)
            {
                TriggerEvent(interactEvent.OnUnpressed);

                float heldDuration = now - pressTime;

                bool wasTap =
                    heldDuration < tapThreshold;

                //================================================
                // TAP
                //================================================
                if (wasTap)
                {
                    TriggerEvent(interactEvent.OnTapped);

                    // FIRST TAP
                    if (currentStage == tapStage.None)
                    {
                        pendingExclusiveTap = true;
                        pendingTapTime = now;

                        currentStage = tapStage.SingleTap;
                    }

                    // SECOND TAP
                    else if (currentStage == tapStage.SingleTap)
                    {
                        pendingExclusiveTap = true;
                        pendingTapTime = now;

                        TriggerEvent(interactEvent.OnDoubleTapped);

                        currentStage = tapStage.DoubleTap;
                    }

                    // THIRD TAP RESET
                    else if (currentStage == tapStage.DoubleTap)
                    {
                        pendingExclusiveTap = false;
                        currentStage = tapStage.None;
                    }
                }

                //================================================
                // HOLD RELEASE
                //================================================
                else
                {
                    // SINGLE HOLD
                    if (holdQualified)
                    {
                        TriggerEvent(interactEvent.OnHeld);

                        currentStage = tapStage.None;
                    }

                    // TAP + HOLD
                    else if (tapAndHoldQualified)
                    {
                        TriggerEvent(interactEvent.OnTapAndHold);

                        pendingExclusiveTap = false;
                        currentStage = tapStage.None;
                    }

                    // DOUBLE TAP + HOLD
                    else if (doubleTapAndHoldQualified)
                    {
                        TriggerEvent(interactEvent.OnDoubleTapAndHold);

                        pendingExclusiveTap = false;
                        currentStage = tapStage.None;
                    }
                }
            }

            previousHeld = currentHeld;
        }
        private static void TriggerEvent(interactEvent evnt)
        {
            eventMap[evnt].Invoke();
        }
    }
}
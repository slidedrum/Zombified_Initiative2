using SlideMenu;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BotControl.SmartSelect
{
    internal static class ssInputHandler
    {
        public enum interactEvent
        {
            OnPressed, //*Triggered on the first frame the button is pressed
            WhilePressed, //*Triggered on every frame the button is pressed
            OnUnpressed, //*Triggered the first frame the button goes from pressed to unpressed
            OnUnpressedExclusive, //Triggered for one frame when the button has been unpressed for tap threshold
            WhileUnpressed, //*Triggered on every frame the button is not pressed
            WhileUnpressedExclusive, //Triggered on every frame the button is not pressed, after tap threshold has expried.
            OnTapped, //*Triggered on release when the hold time is less than tap threshold
            OnTappedExclusive, //Triggered if tapped, but invoked after double tap window expires to make sure it's not a double tap
            OnDoublePressed, //*triggered on press the first frame the 2nd tap is pressed
            OnDoubleTapped, //*triggered on relese the first frame the 2nd tap is released, even if the 2nd press is a hold.
            OnDoubleTappedStrict, //*triggered on the first frame the 2nd tap is released. if both are within tap threshold
            OnDoubleTappedStrictExclusive, //triggered on the frame where the double tap and hold window expires if released at that time.
            OnDoubleTappedExclusive, //triggered on the frame where the double tap and hold window expires if held, or when the tap and hold triggers.
            OnHeld, //*Triggered the first frame the button is released if the tap threshold is exceeded
            WhileHeld, //*triggered every frame the button is held starting after the tap threshold is exceeded.
            WhileHeldExclusive, //triggered every frame the button is held starting after the tap threshold is exceeded, but only when not a tapandhold and not doubletapandhold
            OnHeldImmediate, //Triggered the first frame when the tap threshold is exceeded.
            OnHeldImmediateExclusive, //Triggered the first frame when the tap threshold is exceeded, but only the first hold, not on tapandhold and not on doubletapandhold
            OnTapAndHold, //*Triggered on release after a single tap is triggered and then the tap threshold is exceded on what would be a double tap.  This is triggered on one frame, after the hold is released. (2 total presses)
            OnTapAndHoldExclusive, //Triggered after a single tap is triggered and then the tap threshold is exceded on what would be a double tap.  This is triggered on one frame, after the hold is released. (2 total presses) but not on doubletap and hold
            WhileTapAndHold, //Triggered after a single tap is triggered and then the tap threshold is exceded on what would be a double tap.  This is triggered every frame until the key is released. (2 total presses)
            WhileTapAndHoldExclusive, //Triggered after a single tap is triggered and then the tap threshold is exceded on what would be a double tap.  This is triggered every frame until the key is released. (2 total presses) but not while doubletap and hold
            OnTapAndHoldImmediate, //Triggered after a single tap is triggered and then the tap threshold is exceded on what would be a double tap.  This is triggered on the single frame when the tap threshold is passed. (2 total presses)
            OnTapAndHoldImmediateExclusive, //Triggered after a single tap is triggered and then the tap threshold is exceded on what would be a double tap.  This is triggered on the single frame when the tap threshold is passed. (2 total presses) but not on the double tap and hold
            OnDoubleTapAndHold, //*Triggered after a double tap is triggered and then the tap threshold is exceded on what would be a tripple tap. This is triggered on one frame, after the hold is released. (3 total presses)
            WhileDoubleTapAndHold, //Triggered after a double tap is triggered and then the tap threshold is exceded on what would be a tripple tap.  This is triggered every frame until the key is released. (3 total presses)
            OnTapDoubleAndHoldImmediate, //Triggered after a double tap is triggered and then the tap threshold is exceded on what would be a tripple tap.  This is triggered on the single frame when the tap threshold is passed. (3 total presses)
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
        public enum InputEdge
        {
            Down,
            Up
        }
        public struct InputEvent
        {
            public float time;
            public InputEdge edge;
            public InputEvent(float time, InputEdge edge)
            {
                this.time = time;
                this.edge = edge;
            }
        }
        private const int maxTapCount = 2;
        private static float tapThreshold = 0.25f;
        private const KeyCode key = KeyCode.V;

        private static RollingBuffer<InputEvent> pressHistory = new(2 * maxTapCount + 1);
        private static Dictionary<interactEvent, bool> interactEventReadyState = new();
        private static bool previousFramekeyHeld;
        //private static bool WasTap(int pressCount = 0)
        //{
        //    //how long from when it was last released, to when it was last pressed
        //    int offset = pressCount * 2;
        //    bool currentlyDown = pressHistory.Get(offset).edge == InputEdge.Down;
        //    int lastReleaseIndex = offset;
        //    if (currentlyDown)
        //        lastReleaseIndex = offset + 1;
        //    int lastPressedindex = lastReleaseIndex + 1;
        //    InputEvent lastReleased = pressHistory.Get(lastReleaseIndex);
        //    InputEvent lastPressed = pressHistory.Get(lastPressedindex);
        //    bool tap = lastReleased.time - lastPressed.time < tapThreshold;
        //    return tap;
        //}
        private static bool IsTap(int historyOffset = 0)
        {
            //if history offset is 0, and we are currently pressing, we need to check against current time.
            //otherwise we can know for sure how long the hold was
            InputEvent lastPressed;
            InputEvent lastReleased;
            bool tap;
            int offset = historyOffset * 2;
            bool currentlyReleased = pressHistory.Get(offset).edge == InputEdge.Down;
            if (currentlyReleased)
            {

                if (offset == 0)
                {
                    lastPressed = pressHistory.Get(offset);
                    tap = Time.time - lastPressed.time < tapThreshold;
                    return tap;
                }
                else
                {
                    lastPressed = pressHistory.Get(offset);
                    lastReleased = pressHistory.Get(offset - 1);
                    tap = lastReleased.time - lastPressed.time < tapThreshold;
                    return tap;
                }
            }
            else
            {
                lastPressed = pressHistory.Get(offset + 1);
                lastReleased = pressHistory.Get(offset);
                tap = lastReleased.time - lastPressed.time < tapThreshold;
                return tap;
            }
        }
        private static InputEvent GetHistory(InputEdge edge, int offset = 0)
        {
            var ret = pressHistory.Get(offset);
            if (ret.edge == edge)
                return ret;
            else return pressHistory.Get(offset + 1);
        }
        public static void Update()
        {
            bool ready =
                FocusStateManager.CurrentState == eFocusState.FPS ||
                FocusStateManager.CurrentState == eFocusState.Dead;

            if (!ready)
                return;

            float time = Time.time;
            bool keyPressed = Input.GetKey(key);
            bool keyDown = keyPressed && !previousFramekeyHeld; // the button was not pressed last frame but is now
            bool keyUp = !keyPressed && previousFramekeyHeld; // the button was pressed last frame but is not now
            previousFramekeyHeld = keyPressed;
            InputEvent lastDown = GetHistory(InputEdge.Down);
            InputEvent lastUp = GetHistory(InputEdge.Up);
            InputEvent previousDown = GetHistory(InputEdge.Down, 2);
            InputEvent previousUp = GetHistory(InputEdge.Up, 2);
            float lastDownTime = lastDown.time;
            float lastUpTime = lastUp.time;
            float unpressedTime = lastDownTime - lastUpTime;
            float previousDownTime = previousDown.time;
            float previousUpTime = previousUp.time;
            float previousUnpressedTime = previousDownTime - previousUpTime;
            bool unpressedTimeWithinDoubleTapThreshold = unpressedTime < tapThreshold;
            bool previousUnpressedTimeWithinDoubleTapThreshold = previousUnpressedTime < tapThreshold;
            bool ThisIsATap = IsTap();
            bool PreviousIsATap = IsTap(1);
            bool oldestIsATap = IsTap(2);
            bool ThisIsADoubleTap = unpressedTimeWithinDoubleTapThreshold && PreviousIsATap;
            bool PreviousIsADoubleTap = previousUnpressedTimeWithinDoubleTapThreshold && oldestIsATap;

            if (keyDown) // first frame the key is pressed
            {
                pressHistory.Add(new InputEvent(time, InputEdge.Down)); // log the press
                TriggerEvent(interactEvent.OnPressed); // trigger on pressed event
                interactEventReadyState[interactEvent.OnUnpressedExclusive] = true; // ready for OnUnpressedExclusive event to be triggered again
                if (ThisIsADoubleTap)
                {
                    TriggerEvent(interactEvent.OnDoublePressed);
                }
            }
            if (keyUp) // first frame the key was unpressed
            {
                pressHistory.Add(new InputEvent(time, InputEdge.Up)); // log the unpress
                TriggerEvent(interactEvent.OnUnpressed); // trigger onUnpressed
                interactEventReadyState[interactEvent.OnHeldImmediate] = true; // ready for OnHeld event to be triggered again.

                if (ThisIsATap) // was this unpress a tap? 1 is the time the button went down
                {
                    TriggerEvent(interactEvent.OnTapped); // trigger tap event
                    interactEventReadyState[interactEvent.OnTappedExclusive] = true; // we are ready for this to be an exclusive tap
                    if (ThisIsADoubleTap)
                    {
                        TriggerEvent(interactEvent.OnDoubleTappedStrict); // trigger double tap strict event, both presses are taps
                        interactEventReadyState[interactEvent.OnDoubleTappedStrictExclusive] = true; // ready the double tap exclusive event
                    }
                }
                else // Was this unpress a hold?
                {
                    TriggerEvent(interactEvent.OnHeld); // triggerOnHeld
                    if (ThisIsADoubleTap)
                    {
                        TriggerEvent(interactEvent.OnTapAndHold);
                        interactEventReadyState[interactEvent.OnTapAndHoldExclusive] = true;
                    }
                    if (PreviousIsADoubleTap)
                    {
                        TriggerEvent(interactEvent.OnDoubleTapAndHold);
                    }
                }
                if (ThisIsADoubleTap)
                {
                    TriggerEvent(interactEvent.OnDoubleTapped);
                }

            }
            if (keyPressed) // is the key currently down
            {
                TriggerEvent(interactEvent.WhilePressed);
            }
            else
            { 
                TriggerEvent(interactEvent.WhileUnpressed);
            } 
        }
        private static void TriggerEvent(interactEvent evnt)
        {
            if (!evnt.ToString().ToLower().Contains("while"))
                ZiMain.log.LogInfo($"smart select event {evnt.ToString()} was triggered.");
            eventMap[evnt].Invoke();
        }
    }
    public class RollingBuffer<T>
    {
        private readonly T[] buffer;
        private int index = 0;
        private int count = 0;

        public int Count => count;
        public int Capacity => buffer.Length;

        public RollingBuffer(int capacity)
        {
            buffer = new T[capacity];
        }

        public void Add(T item)
        {
            buffer[index] = item;

            index = (index + 1) % buffer.Length;

            if (count < buffer.Length)
                count++;
        }

        // 0 = newest
        // 1 = previous
        // 2 = older
        public T Get(int index)
        {
            if (index >= count)
                return default;

            int item = (this.index - 1 - index + buffer.Length) % buffer.Length;

            return buffer[item];
        }
    }
}
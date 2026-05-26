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
            WhileUnpressed, //*Triggered on every frame the button is not pressed
            WhileUnpressedExclusive, //*Triggered on every frame the button is not pressed, after tap threshold has expried.
            OnUnpressedExclusive, //*Triggered for one frame when the button has been unpressed for tap threshold
            OnTapped, //*Triggered on release when the hold time is less than tap threshold
            OnTappedExclusive, //*Triggered if tapped, but invoked after double tap window expires to make sure it's not a double tap
            OnDoublePressed, //*triggered on press the first frame the 2nd tap is pressed
            OnDoubleTapped, //*triggered on relese the first frame the 2nd tap is released, even if the 2nd press is a hold.
            OnDoubleTappedStrict, //*triggered on the first frame the 2nd tap is released. if both are within tap threshold
            OnDoubleTappedStrictExclusive, //*triggered on the frame where the double tap and hold window expires if released at that time.
            OnDoubleTappedExclusive, //triggered on the frame where the double tap and hold window expires if held, or when the tap and hold triggers.
            WhileHeld, //*triggered every frame the button is held starting after the tap threshold is exceeded.
            OnHeld, //*Triggered the first frame the button is released if the tap threshold is exceeded
            onHeldExclusive, //*Triggered if held, but invoked ater double tap window has expried.
            OnHeldImmediate, //*Triggered the first frame when the tap threshold is exceeded.
            OnTapAndHold, //*Triggered on release after a single tap is triggered and then the tap threshold is exceded on what would be a double tap.  This is triggered on one frame, after the hold is released. (2 total presses)
            OnTapAndHoldImmediate, //*Triggered after a single tap is triggered and then the tap threshold is exceded on what would be a double tap.  This is triggered on the single frame when the tap threshold is passed. (2 total presses)
            WhileTapAndHold, //*Triggered after a single tap is triggered and then the tap threshold is exceded on what would be a double tap.  This is triggered every frame until the key is released. (2 total presses)
            OnTapDoubleAndHoldImmediate, //*Triggered after a double tap is triggered and then the tap threshold is exceded on what would be a tripple tap.  This is triggered on the single frame when the tap threshold is passed. (3 total presses)
            WhileDoubleTapAndHold, //*Triggered after a double tap is triggered and then the tap threshold is exceded on what would be a tripple tap.  This is triggered every frame until the key is released. (3 total presses)
            OnDoubleTapAndHold, //*Triggered after a double tap is triggered and then the tap threshold is exceded on what would be a tripple tap. This is triggered on one frame, after the hold is released. (3 total presses)
            
            WhileHeldExclusive, //triggered every frame the button is held starting after the tap threshold is exceeded, but only when not a tapandhold and not doubletapandhold
            OnHeldImmediateExclusive, //Triggered the first frame when the tap threshold is exceeded, but only the first hold, not on tapandhold and not on doubletapandhold
            OnTapAndHoldExclusive, //Triggered after a single tap is triggered and then the tap threshold is exceded on what would be a double tap.  This is triggered on one frame, after the hold is released. (2 total presses) but not on doubletap and hold
            WhileTapAndHoldExclusive, //Triggered after a single tap is triggered and then the tap threshold is exceded on what would be a double tap.  This is triggered every frame until the key is released. (2 total presses) but not while doubletap and hold
            OnTapAndHoldImmediateExclusive, //Triggered after a single tap is triggered and then the tap threshold is exceded on what would be a double tap.  This is triggered on the single frame when the tap threshold is passed. (2 total presses) but not on the double tap and hold
            
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
        public class InputEvent
        {
            public float time;
            public InputEdge edge;
            public InputEvent(float time, InputEdge edge)
            {
                this.time = time;
                this.edge = edge;
            }
        }
        public class KeyPress
        {
            public static KeyPress lastPress;
            private static RollingBuffer<KeyPress> pressHistory = new(maxTapCount);

            private InputEvent keyDown;
            private InputEvent? keyUp;
            public KeyPress? previousKeyPress;
            public float downTime => keyDown.time;
            public float upTime => keyUp?.time ?? Time.time;
            public float heldTime => upTime - downTime;
            public bool isTap => heldTime < tapThreshold;
            public float unpressedTimeBetweenPreviousPress => previousKeyPress == null ? tapThreshold + 1 : downTime - previousKeyPress.upTime;
            public bool isDoublePress => previousKeyPress == null ? false : previousKeyPress.isTap && unpressedTimeBetweenPreviousPress < tapThreshold;
            public KeyPress()
            {
                keyDown = new InputEvent(Time.time, InputEdge.Down);
                if (lastPress != null)
                    previousKeyPress = lastPress;
                lastPress = this;
                if (pressHistory.full)
                    pressHistory.Get(pressHistory.Capacity - 2).previousKeyPress = null; // Remove the last refrence to the key press that's no longer needed so it gets garbage collected.
                pressHistory.Add(this);
            }
            public static void SetUp()
            {
                lastPress._SetUp();
            }
            private void _SetUp()
            {
                keyUp = new InputEvent(Time.time, InputEdge.Up); ;
            }
        }
        private const int maxTapCount = 10;
        private static float tapThreshold = 0.5f;
        private const KeyCode key = KeyCode.V;
        private static Dictionary<interactEvent, bool> interactEventReadyState = new();
        private static bool previousFramekeyHeld;
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
            if (keyDown)
            {
                new KeyPress();
                TriggerEvent(interactEvent.OnPressed);
                if (KeyPress.lastPress.isDoublePress)
                {
                    TriggerEvent(interactEvent.OnDoublePressed);
                }
            }
            if (keyUp)
            {
                KeyPress.SetUp();
                TriggerEvent(interactEvent.OnUnpressed);
                interactEventReadyState[interactEvent.OnUnpressedExclusive] = true;
                interactEventReadyState[interactEvent.OnHeldImmediate] = true;
                if (KeyPress.lastPress.isTap)
                {
                    TriggerEvent(interactEvent.OnTapped);
                    interactEventReadyState[interactEvent.OnTappedExclusive] = true;

                }
                else // is last press a hold
                {
                    TriggerEvent(interactEvent.OnHeld);
                    interactEventReadyState[interactEvent.onHeldExclusive] = true;
                    if (KeyPress.lastPress.previousKeyPress.isTap)
                    {
                        if (KeyPress.lastPress.isDoublePress)
                        {
                            TriggerEvent(interactEvent.OnTapAndHold);
                            interactEventReadyState[interactEvent.OnTapAndHoldExclusive] = true;
                            interactEventReadyState[interactEvent.OnTapAndHoldImmediate] = true;
                            interactEventReadyState[interactEvent.OnTapAndHoldImmediateExclusive] = true;
                        }
                    }
                }
                if (KeyPress.lastPress.isDoublePress)
                {
                    TriggerEvent(interactEvent.OnDoubleTapped);
                    interactEventReadyState[interactEvent.OnDoubleTappedExclusive] = true;
                    if (KeyPress.lastPress.previousKeyPress.isDoublePress)
                    {
                        TriggerEvent(interactEvent.OnDoubleTapAndHold);
                        interactEventReadyState[interactEvent.OnTapDoubleAndHoldImmediate] = true;
                    }
                }
                if (KeyPress.lastPress.isDoublePress && KeyPress.lastPress.isTap)
                {
                    TriggerEvent(interactEvent.OnDoubleTappedStrict);
                    interactEventReadyState[interactEvent.OnDoubleTappedStrictExclusive] = true;
                }
            }
            if (keyPressed)
            {
                TriggerEvent(interactEvent.WhilePressed);
                if (TimeSince(KeyPress.lastPress.downTime) > tapThreshold) //if key is held
                {
                    TriggerEvent(interactEvent.WhileHeld);
                    if (interactEventReadyState[interactEvent.OnHeldImmediate])
                    {
                        TriggerEvent(interactEvent.OnHeldImmediate);
                        interactEventReadyState[interactEvent.OnHeldImmediate] = false;
                        if (KeyPress.lastPress.previousKeyPress.isTap)
                        {
                            if (KeyPress.lastPress.isDoublePress)
                            {
                                if (interactEventReadyState[interactEvent.OnTapAndHoldImmediate])
                                {
                                    TriggerEvent(interactEvent.OnTapAndHoldImmediate);
                                    interactEventReadyState[interactEvent.OnTapAndHoldImmediate] = false;
                                }
                                TriggerEvent(interactEvent.WhileTapAndHold);
                                if (KeyPress.lastPress.previousKeyPress.isDoublePress)
                                {
                                    TriggerEvent(interactEvent.WhileDoubleTapAndHold);
                                    if (interactEventReadyState[interactEvent.OnTapDoubleAndHoldImmediate])
                                    {
                                        TriggerEvent(interactEvent.OnTapDoubleAndHoldImmediate);
                                        interactEventReadyState[interactEvent.OnTapDoubleAndHoldImmediate] = false;
                                    }
                                }
                            }
                        }
                    }
                }
                else // key unpressed
                {
                    TriggerEvent(interactEvent.WhileUnpressed);
                    if (TimeSince(KeyPress.lastPress.upTime) > tapThreshold)
                    {
                        TriggerEvent(interactEvent.WhileUnpressedExclusive);
                        if (interactEventReadyState[interactEvent.OnUnpressedExclusive])
                        {
                            TriggerEvent(interactEvent.OnUnpressedExclusive);
                            interactEventReadyState[interactEvent.OnUnpressedExclusive] = false;
                        }
                        if (KeyPress.lastPress.isTap)
                        {
                            if (interactEventReadyState[interactEvent.OnTappedExclusive])
                            {
                                TriggerEvent(interactEvent.OnTappedExclusive);
                                interactEventReadyState[interactEvent.OnTappedExclusive] = false;
                            }
                            if (KeyPress.lastPress.isDoublePress)
                            {
                                if (interactEventReadyState[interactEvent.OnDoubleTappedStrictExclusive])
                                {
                                    TriggerEvent(interactEvent.OnDoubleTappedStrictExclusive);
                                    interactEventReadyState[interactEvent.OnDoubleTappedStrictExclusive] = false;
                                }

                            }
                        }
                        else // last press was a hold
                        {
                            if (interactEventReadyState[interactEvent.onHeldExclusive])
                            {
                                TriggerEvent(interactEvent.onHeldExclusive);
                                interactEventReadyState[interactEvent.onHeldExclusive] = false;
                            }
                        }
                    }
                }
            }
        }
        private static float TimeSince(float startTime, float? endTime = null)
        {
            if (endTime == null) endTime = Time.time;
            return (float)endTime - startTime;
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
        public bool full => count == Capacity;
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
            if (index >= count || index < 0)
                return default;

            int item = (this.index - 1 - index + buffer.Length) % buffer.Length;

            return buffer[item];
        }
    }
}
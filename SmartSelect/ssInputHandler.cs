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
            OnUnpressedExclusive, //*Triggered for one frame when the button has been unpressed for tap threshold
            WhileUnpressed, //*Triggered on every frame the button is not pressed
            WhileUnpressedExclusive, //*Triggered on every frame the button is not pressed, after tap threshold has expried.
            OnTapped, //*Triggered when the hold time is less than tap threshold
            OnTappedExclusive, //*Triggered if tapped, but invoked after double tap window expires to make sure it's not a double tap
            OnDoubleTapped, //*triggered on the first frame the 2nd tap is released
            OnDoubleTappedExclusive, //*triggered on the frame where the double tap and hold window expires.
            OnHeld, //*Triggered the first frame the button is released if the tap threshold is exceeded
            WhileHeld, //*triggered every frame the button is held starting after the tap threshold is exceeded.
            WhileHeldExclusive, //triggered every frame the button is held starting after the tap threshold is exceeded, but only when not a tapandhold and not doubletapandhold
            OnHeldImmediate, //*Triggered the first frame when the tap threshold is exceeded.
            OnHeldImmediateExclusive, //Triggered the first frame when the tap threshold is exceeded, but only the first hold, not on tapandhold and not on doubletapandhold
            OnTapAndHold, //Triggered after a single tap is triggered and then the tap threshold is exceded on what would be a double tap.  This is triggered on one frame, after the hold is released. (2 total presses)
            OnTapAndHoldExclusive, //Triggered after a single tap is triggered and then the tap threshold is exceded on what would be a double tap.  This is triggered on one frame, after the hold is released. (2 total presses) but not on doubletap and hold
            WhileTapAndHold, //Triggered after a single tap is triggered and then the tap threshold is exceded on what would be a double tap.  This is triggered every frame until the key is released. (2 total presses)
            WhileTapAndHoldExclusive, //Triggered after a single tap is triggered and then the tap threshold is exceded on what would be a double tap.  This is triggered every frame until the key is released. (2 total presses) but not while doubletap and hold
            OnTapAndHoldImmediate, //Triggered after a single tap is triggered and then the tap threshold is exceded on what would be a double tap.  This is triggered on the single frame when the tap threshold is passed. (2 total presses)
            OnTapAndHoldImmediateExclusive, //Triggered after a single tap is triggered and then the tap threshold is exceded on what would be a double tap.  This is triggered on the single frame when the tap threshold is passed. (2 total presses) but not on the double tap and hold
            OnDoubleTapAndHold, //Triggered after a double tap is triggered and then the tap threshold is exceded on what would be a tripple tap. This is triggered on one frame, after the hold is released. (3 total presses)
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

        public static void Update()
        {
            bool ready =
                FocusStateManager.CurrentState == eFocusState.FPS ||
                FocusStateManager.CurrentState == eFocusState.Dead;

            if (!ready)
                return;

            float time = Time.time;
            bool keyHeld = Input.GetKey(key);
            bool keyDown = keyHeld && !previousFramekeyHeld; // the button was not pressed last frame but is now
            bool keyUp = !keyHeld && previousFramekeyHeld; // the button was pressed last frame but is not now
            previousFramekeyHeld = keyHeld;

            if (keyDown) // first frame the key is pressed
            {
                pressHistory.Add(new InputEvent(time, InputEdge.Down)); // log the press
                interactEventReadyState[interactEvent.OnUnpressedExclusive] = true; // ready for OnUnpressedExclusive event to be triggered again
                TriggerEvent(interactEvent.OnPressed); // trigger on pressed event
            }
            if (keyUp) // first frame the key was unpressed
            {
                pressHistory.Add(new InputEvent(time, InputEdge.Up)); // log the unpress
                interactEventReadyState[interactEvent.OnHeldImmediate] = true; // ready for OnHeld event to be triggered again.
                TriggerEvent(interactEvent.OnUnpressed); // trigger onUnpressed
                if (pressHistory.Get(1).time > time - tapThreshold) // was this unpress a tap? 1 is the time the button went down
                {
                    TriggerEvent(interactEvent.OnTapped); // trigger tap event
                    interactEventReadyState[interactEvent.OnTappedExclusive] = true; // we are ready for this to be an exclusive tap
                    if (pressHistory.Get(1).time - pressHistory.Get(2).time < tapThreshold) // has too much time passed since the last tap for it to be a double tap candidate?
                    { // okay it's recent enough, but was it actually a tap?
                        if (pressHistory.Get(2).time - pressHistory.Get(3).time < tapThreshold) // was the previous press a tap?
                        { 
                            TriggerEvent(interactEvent.OnDoubleTapped); // trigger double tap event
                            interactEventReadyState[interactEvent.OnDoubleTappedExclusive] = true; // ready the double tap exclusive event
                        }
                    }
                }
                // 0 current up
                // 1 current down
                // 2 previous up
                // 3 previous down
                else // Was this unpress a hold?
                {
                    TriggerEvent(interactEvent.OnHeld); // triggerOnHeld
                }
            }
            if (keyHeld) // is the key currently down
            {
                TriggerEvent(interactEvent.WhilePressed); // trigger whilePressed
                if (pressHistory.Get(0).time < time - tapThreshold) // Is this a hold?  Has the tap threshold passed?
                {
                    TriggerEvent(interactEvent.WhileHeld); // trigger while held
                    if (interactEventReadyState[interactEvent.OnHeldImmediate]) // has onheldimmediate been triggered?
                    {
                        TriggerEvent(interactEvent.OnHeldImmediate); // trigger on held immediate
                        interactEventReadyState[interactEvent.OnHeldImmediate] = false; // make sure on held does not get triggered again
                    } // (interactEventReadyState[interactEvent.OnHeld])
                } // (pressHistory.Get(0).time < time - tapThreshold)
            } // (keyHeld)
            else // is the key currently up
            { 
                TriggerEvent(interactEvent.WhileUnpressed); // trigger while unpressed
                if (pressHistory.Get(0).time < time - tapThreshold) // has it been unpressed long enough that it's not part of a double tap?
                { // get when it was unpressed
                    TriggerEvent(interactEvent.WhileUnpressedExclusive); // trigger while unpressed exclusive
                    if (interactEventReadyState[interactEvent.OnUnpressedExclusive]) // has on unpressed exclusvie been triggered?
                    {
                        TriggerEvent(interactEvent.OnUnpressedExclusive); // trigger on unpressed exclusive
                        interactEventReadyState[interactEvent.OnUnpressedExclusive] = false; //make sure it doesn't get triggered again.
                    } // (interactEventReadyState[interactEvent.OnUnpressedExclusive])

                    if (pressHistory.Get(0).time - pressHistory.Get(1).time < tapThreshold) //was the most recent press a tap
                    { // get when it was pressed - when it was unpressed
                        if (pressHistory.Get(1).time - pressHistory.Get(2).time < tapThreshold) // has too much time passed since the last tap for it to be a double tap candidate?
                        { // okay it's recent enough, but was it actually a tap?
                            if (pressHistory.Get(2).time - pressHistory.Get(3).time < tapThreshold) // was the previous press a tap?
                            {
                                if (interactEventReadyState[interactEvent.OnDoubleTappedExclusive]) // is double tap exclusive ready?
                                {
                                    TriggerEvent(interactEvent.OnDoubleTappedExclusive); // trigger double tap exclusive event
                                    interactEventReadyState[interactEvent.OnDoubleTappedExclusive] = false; // make sure it doesn't trigger again
                                }
                            }
                        }
                        else if (interactEventReadyState[interactEvent.OnTappedExclusive]) // it wasn't a double tap, is on tap exclusive ready to be triggered?
                        {
                            TriggerEvent(interactEvent.OnTappedExclusive); // trigger the event.
                            interactEventReadyState[interactEvent.OnTappedExclusive] = false; // make sure it does not get triggered again.
                        } // (interactEventReadyState[interactEvent.OnTappedExclusive])
                    } // (pressHistory.Get(1).time - pressHistory.Get(0).time < tapThreshold)
                } // (pressHistory.Get(0).time < time - tapThreshold)
            } // (!keyHeld)
        } // update method
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
using BotControl.SmartSelect;
using System.Collections.Generic;
using UnityEngine;

namespace SlideDrum.sInputSystem
{
    public class sKeyPress
    {
        public sInputSystem inputSystem;
        public enum InputEdge
        {
            Down,
            Up
        }
        public enum PressType
        {
            tap,
            hold,
        }
        public class InputEvent
        {
            public float time;
            public InputEdge edge;
            //public KeyCode key;
            public InputEvent(float time, InputEdge edge)
            {
                this.time = time;
                this.edge = edge;
            }
        }
        public static sKeyPress lastPress;
        public sKeySequence Sequence;
        private float tapThreshold => inputSystem.TapThreshold;
        private readonly InputEvent KeyDown;
        private InputEvent KeyUp;
        public sKeyPress PreviousKeyPress;
        public bool IsPressedDown => KeyUp == null;
        public float DownTimestamp => KeyDown.time;
        public float UpTimestamp => KeyUp?.time ?? Time.time;
        public float HeldTime => UpTimestamp - DownTimestamp;
        public float UnheldTime => PreviousKeyPress == null ? float.MaxValue : DownTimestamp - PreviousKeyPress.UpTimestamp;
        public float TimeSinceReleased => TimeSince(UpTimestamp);
        private bool StartNewSequence => UnheldTime > tapThreshold;
        private bool IsTap => HeldTime < tapThreshold;
        public PressType pressType => IsTap ? PressType.tap : PressType.hold;
        public sKeyPress(sInputSystem inputSystem)
        {
            if (lastPress != null && lastPress.KeyUp == null)
            {
                Debug.LogWarning("New key press created before last key press was released. This may cause unexpected behavior.");
                lastPress.SetUp();
            }
            this.inputSystem = inputSystem;
            KeyDown = new InputEvent(Time.time, InputEdge.Down);
            if (lastPress != null)
                PreviousKeyPress = lastPress;
            lastPress = this;
            if (StartNewSequence || PreviousKeyPress == null)
                Sequence = new sKeySequence(this);
            else
            {
                Sequence = PreviousKeyPress.Sequence;
                Sequence.Add(this);
            }
        }
        public static void SetUpLast()
        {
            lastPress.SetUp();
        }
        public void SetUp()
        {
            KeyUp = new InputEvent(Time.time, InputEdge.Up); ;
        }
        private static float TimeSince(float startTime, float? endTime = null)
        {
            if (endTime == null) endTime = Time.time;
            return (float)endTime - startTime;
        }
    }
}

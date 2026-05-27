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
        public readonly KeyCode Key;
        public sKeyPress PreviousKeyPress;
        public bool IsPressedDown => KeyUp == null;
        public float DownTimestamp => KeyDown.time;
        public float UpTimestamp => KeyUp?.time ?? Time.time;
        public float HeldDurration => UpTimestamp - DownTimestamp;
        public float UnheldDurration => PreviousKeyPress == null ? float.MaxValue : DownTimestamp - PreviousKeyPress.UpTimestamp;
        public float UnheldFor => TimeSince(UpTimestamp);
        private bool StartNewSequence => UnheldDurration > tapThreshold;
        public sKeyPress(KeyCode key, sInputSystem inputSystem)
        {
            if (lastPress != null && lastPress.KeyUp == null)
            {
                Debug.LogWarning("New key press created before last key press was released. This may cause unexpected behavior.");
                lastPress.SetUp();
            }
            this.inputSystem = inputSystem;
            this.Key = key;
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
        public bool Matches(sKeyPressDefinition definition)
        {
            if (this.Key != definition.Key)
                return false;

            if (this.UnheldDurration < definition.MinUnheldDurration ||
                this.UnheldDurration > definition.MaxUnheldDurration)
                return false;

            if (this.HeldDurration < definition.MinHoldDurration ||
                this.HeldDurration > definition.MaxHoldDurration)
                return false;

            return true;
        }
        //public bool Matches(sKeyPressDefinition definition)
        //{
        //    bool ret = true;
        //    if (this.Key != definition.Key)
        //        ret = false;

        //    if (definition.InvertedInput)
        //    {
        //        if (this.IsPressedDown)
        //            ret = false;
        //    }
        //    else
        //    {
        //        if (!this.IsPressedDown)
        //            ret = false;
        //    }

        //    if (this.UnheldDurration < definition.MinUnheldDurration ||
        //        this.UnheldDurration > definition.MaxUnheldDurration)
        //        ret = false;

        //    if (this.HeldDurration < definition.MinHoldDurration ||
        //        this.HeldDurration > definition.MaxHoldDurration)
        //        ret = false;

        //    if (definition.InvertedOutput)
        //        ret = !ret;

        //    return ret;
        //}
    }
}

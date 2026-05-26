using System.Collections.Generic;
using UnityEngine;

namespace BotControl.SmartSelect
{
    public class KeyPress
    {
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
            public InputEvent(float time, InputEdge edge)
            {
                this.time = time;
                this.edge = edge;
            }
        }
        public static KeyPress? lastPress;
        private static RollingBuffer<KeyPress> _pressHistory;
        private static RollingBuffer<KeyPress> pressHistory { get 
            {
                if (_pressHistory == null)
                    _pressHistory = new(HistoryLenth);
                return _pressHistory;
            } 
        }
        private static float tapThreshold = ssInputHandler.tapThreshold;
        private static int HistoryLenth = ssInputHandler.maxTapCount;
        private readonly InputEvent KeyDown;
        private InputEvent? KeyUp;
        public KeyPress? PreviousKeyPress;
        public float DownTimestamp => KeyDown.time;
        public float UpTimestamp => KeyUp?.time ?? Time.time;
        public float HeldTime => UpTimestamp - DownTimestamp;
        public bool IsDown => KeyUp == null;
        public float UnheldTime => PreviousKeyPress == null ? float.MaxValue : DownTimestamp - PreviousKeyPress.UpTimestamp;
        public bool IsNewSequence => UnheldTime > tapThreshold;
        public float TimeSinceReleased => TimeSince(UpTimestamp);
        public bool IsSequenceOngoing => TimeSinceReleased > tapThreshold;
        public bool IsTap => HeldTime < tapThreshold;
        public PressType pressType => IsTap ? PressType.tap : PressType.hold;
        public float UnpressedTimeBetweenPreviousPress => PreviousKeyPress == null ? tapThreshold + 1 : DownTimestamp - PreviousKeyPress.UpTimestamp;
        public bool IsDoublePress => PreviousKeyPress == null ? false : PreviousKeyPress.IsTap && UnpressedTimeBetweenPreviousPress < tapThreshold;
        public KeyPress()
        {
            KeyDown = new InputEvent(Time.time, InputEdge.Down);
            if (lastPress != null)
                PreviousKeyPress = lastPress;
            lastPress = this;
            if (pressHistory.full)
                pressHistory.Get(pressHistory.Capacity - 2).PreviousKeyPress = null; // Remove the last refrence to the key press that's no longer needed so it gets garbage collected.
            pressHistory.Add(this);
        }
        public int SequenceLength()
        {
            if (IsNewSequence || PreviousKeyPress == null)
                return 1;
            return PreviousKeyPress.SequenceLength() + 1;
        }
        public KeyPress GetSequenceStart()
        {
            if (IsNewSequence || PreviousKeyPress == null)
                return this;
            return PreviousKeyPress.GetSequenceStart();
        }

        public List<PressType> GetSequence()
        {
            List<PressType> ret;
            if (IsNewSequence || PreviousKeyPress == null)
                ret = new List<PressType>();
            else
                ret = PreviousKeyPress.GetSequence();
            ret.Add(pressType);
            return ret;
        }
        public bool MatchesSequence(List<PressType> sequence, bool strict = true)
        {
            List<PressType> currentSequence = GetSequence();

            if (sequence.Count > currentSequence.Count)
                return false;

            int start = strict ? 0 : currentSequence.Count - sequence.Count;

            if (strict && currentSequence.Count != sequence.Count)
                return false;

            for (int i = 0; i < sequence.Count; i++)
            {
                if (currentSequence[i + start] != sequence[i])
                    return false;
            }

            return true;
        }
        public static void SetUp()
        {
            lastPress._SetUp();
        }
        private void _SetUp()
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

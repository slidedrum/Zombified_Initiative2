using UnityEngine;
using static BotControl.SmartSelect.ssInputHandler;

namespace BotControl.SmartSelect
{
    public class KeyPress
    {
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
}

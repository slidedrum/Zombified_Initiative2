using BotControl;
using SlideMenu;
using System.Collections.Generic;
using UnityEngine;

namespace SlideDrum.sInputSystem
{
    public struct sKeySequenceDefinition
    {
        public enum TriggerPoint
        {
            SequenceComplete,
            Pressed,
            Unpressed,
        }
        public sKeyPressDefinition[] Sequence;
        public TriggerPoint Trigger;
        public FlexibleMethodDefinition callback;
        public bool strict;
        public bool RisingEdgeOnly;
        public string Key => $"{string.Join(",", Sequence)}_{Trigger}_{strict}_{RisingEdgeOnly}";
        private uint _id;
        public uint Id
        {
            get
            {
                if (_id == 0)
                {
                    _id = zHelpers.HashString(Key.ToString());
                }
                return _id;
            }
        }
        public void SetKeyCode(KeyCode key)
        {
            for (int i = 0; i < Sequence.Length; i++)
            {
                var keyPress = Sequence[i];
                keyPress.Key = key;
                Sequence[i] = keyPress;
            }
        }

        internal HashSet<KeyCode> GetKeyCodes()
        {
            HashSet<KeyCode> ret = new();
            foreach (var keyPress in Sequence)
                if (keyPress.Key != null)
                    ret.Add((KeyCode)keyPress.Key);
            return ret;
        }

        public sKeySequenceDefinition(sKeyPressDefinition[] Sequence, TriggerPoint Trigger, FlexibleMethodDefinition callback, bool strict = false, bool RisingEdgeOnly = true, KeyCode? Key = null)
        {
            this.Sequence = Sequence;
            this.Trigger = Trigger;
            this.callback = callback;
            this.strict = strict;
            this.RisingEdgeOnly = RisingEdgeOnly;
            for (int i = 0; i < Sequence.Length; i++)
            {
                var keyPress = Sequence[i];
                if (Key != null)
                {
                    keyPress.Key = Key;
                    Sequence[i] = keyPress;
                    continue;
                }
                //if (keyPress.Key == null)
                //    throw new ArgumentException("All key presses must have a key defined either in the sequence or in the constructor override.");
            }
        }
    }
}

using BotControl;
using SlideMenu;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SlideDrum.sInputSystem
{
    public class sKeySequenceDefinition
    {
        public enum TriggerPoint
        {
            SequenceComplete,
            Pressed,
            Unpressed,
        }
        public sKeyPressDefinition[] Sequence;
        public TriggerPoint Trigger;
        public FlexibleMethodDefinition Callback;
        public bool Strict;
        public bool RisingEdgeOnly;
        public bool Inverted;
        public string Key => $"{string.Join(",", Sequence.Select(x => x.ToString()))}_{Trigger}_{Strict}_{RisingEdgeOnly}_{Callback?.method?.Target}_{Callback?.args}";
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

        public sKeySequenceDefinition(sKeyPressDefinition[] Sequence, TriggerPoint Trigger, FlexibleMethodDefinition Callback, bool Strict = false, bool RisingEdgeOnly = true, KeyCode? Key = null, bool Inverted = false)
        {
            this.Sequence = Sequence;
            this.Trigger = Trigger;
            this.Callback = Callback;
            this.Strict = Strict;
            this.Inverted = Inverted;
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
            }
        }
    }
}

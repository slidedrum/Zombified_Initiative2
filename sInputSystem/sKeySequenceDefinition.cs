using BotControl;
using InControl;
using SlideMenu;

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
        public enum EdgeType
        {
            Rising,
            Falling,
        }
        public sKeyPress.PressType[] Sequence;
        public TriggerPoint Trigger;
        public FlexibleMethodDefinition callback;
        public bool strict;
        public bool RisingEdgeOnly;
        private string Key => $"{string.Join(",", Sequence)}_{Trigger}_{strict}_{RisingEdgeOnly}";
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
        public sKeySequenceDefinition(sKeyPress.PressType[] Sequence, TriggerPoint Trigger, FlexibleMethodDefinition callback, bool strict = false, bool RisingEdgeOnly = true)
        {
            this.Sequence = Sequence;
            this.Trigger = Trigger;
            this.callback = callback;
            this.strict = strict;
            this.RisingEdgeOnly = RisingEdgeOnly;
        }
    }
}

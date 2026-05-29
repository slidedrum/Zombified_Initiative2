using UnityEngine;

namespace SlideDrum.sInputSystem
{
    public class sKeyPressRefrence
    {
        public readonly KeyCode Key;
        public bool Pressed;
        public float Start;
        private float? _End = null;
        public float End
        {
            get
            {
                return _End != null ? (float)_End : Time.time;
            }
            set
            {
                _End = value;
            }
        }
        public bool Completed => _End != null;
        public float Durration => End - Start;
        public sKeyPressRefrence(KeyCode Key, bool Pressed, float Start, float? End = null)
        {
            this.Key = Key;
            this.Start = Start;
            this._End = End;
            this.Pressed = Pressed;
        }
        public sKeyPressRefrence(sInputEvent Start, sInputEvent End)
        {
            if (Start.Key != End.Key)
                throw new System.ArgumentException("Start and end events must have the same key.");
            if (Start.Pressed == End.Pressed)
                throw new System.ArgumentException("Start and end events must have different pressed states.");
            this.Key = Start.Key;
            this.Start = Start.Time;
            this.End = End.Time;
            this.Pressed = Start.Pressed;
        }
        public sKeyPressRefrence(sInputEvent Start, float EndTime)
        {
            this.Key = Start.Key;
            this.Start = Start.Time;
            this.End = EndTime;
            this.Pressed = Start.Pressed;
        }
        public bool ContainsEvent(sInputEvent evnt)
        {
            if (evnt.Key != Key)
                return false;

            if (evnt.Time == Start)
                return evnt.Pressed == Pressed;

            if (evnt.Time == End)
                return evnt.Pressed != Pressed;

            return false;
        }
        public override string ToString()
        {
            string pressedString = Pressed ? "Pressed" : "Unpressed";
            return $"{Key} {pressedString} for {Durration} at {Start}";
        }
    }
}
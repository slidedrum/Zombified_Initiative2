using UnityEngine;

namespace SlideDrum.sInputSystem
{
    public class sInputEvent
    {
        public float Time;
        public bool Pressed;
        public KeyCode Key;
        public sInputEvent(KeyCode Key, float Time, bool Pressed)
        {
            this.Key = Key;
            this.Time = Time;
            this.Pressed = Pressed;
        }
        public override string ToString()
        {
            string pressedString = Pressed ? "Pressed" : "Unpressed";
            return $"{Key} {pressedString} at {Time}";
        }
    }
}

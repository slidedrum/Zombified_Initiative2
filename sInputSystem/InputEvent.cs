using UnityEngine;

namespace SlideDrum.sInputSystem
{
    public class InputEvent
    {
        public float Time;
        public bool Pressed;
        public KeyCode Key;
        public InputEvent(KeyCode Key, float Time, bool Pressed)
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

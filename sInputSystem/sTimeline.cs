using BotControl;
using System.Collections.Generic;
using System.Reflection.Metadata;
using UnityEngine;

namespace SlideDrum.sInputSystem
{
    public static class sTimeline
    {
        private const int buffersize = 16;
        public static sRollingBuffer<InputEvent> Sequence { get; private set; } = new(buffersize);
        public static List<sKeyPressRefrence> KeyPressRefrences
        {
            get
            {
                Dictionary<KeyCode, InputEvent> PreviousEvent = new();
                List<sKeyPressRefrence> _KeyPressRefrences = new();
                for (int i = 0; i < Sequence.Count; i++) // this goes BACAKWORDS in time through the sequence. 0 is most recent.
                {
                    var SequenceEvent = Sequence[i];
                    if (PreviousEvent.TryGetValue(SequenceEvent.Key, out var pairedEvent))
                    {
                        if (pairedEvent.Pressed != SequenceEvent.Pressed)
                        {
                            var newEvent = new sKeyPressRefrence(SequenceEvent, pairedEvent);
                            _KeyPressRefrences.Add(newEvent);
                            PreviousEvent[SequenceEvent.Key] = SequenceEvent;
                        }
                        else
                        {
                            //something very bad happend!
                            throw new System.ArgumentException("Two presses or two unpresses in a row should never happen!");
                        }
                    }
                    else
                    {
                        var newEvent = new sKeyPressRefrence(SequenceEvent, Time.time);
                        _KeyPressRefrences.Add(newEvent); // if there is no previous event to match it to, we pair it with the current time.
                        PreviousEvent[SequenceEvent.Key] = SequenceEvent;
                    }
                }
                return _KeyPressRefrences;
            }
        }

        public static void Add(InputEvent Evnt)
        {
            Sequence.Add(Evnt);
        }
        private static void DebugPrint()
        {
            string output = "Buffer: ";
            for (int i = 0; i < Sequence.Count; i++)
            {
                output += Sequence[i].Time + ",";
            }
            ZiMain.log.LogDebug(output);
        }
    }
}
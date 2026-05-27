using System;
using System.Collections.Generic;
using System.Linq;

namespace SlideDrum.sInputSystem
{
    public struct sKeySequence
    {
        private List<sKeyPress> _sequence;
        public bool IsNewSequence => _sequence.Count == 1;
        public bool IsSequenceOngoing => Recent.TimeSinceReleased < First.inputSystem.TapThreshold;
        public int Count => _sequence.Count;
        public sKeyPress First => _sequence[0];
        public sKeyPress Recent => _sequence[_sequence.Count - 1];
        public sKeySequence(sKeyPress firstPress)
        {
            _sequence = new() { firstPress };
        }
        public void Add(sKeyPress press)
        {
            if (press.inputSystem != First.inputSystem)
                throw new InvalidOperationException("Cannot add a key press from a different input system to this sequence.");
            _sequence.Add(press);
        }
        public bool MatchesSequence(sKeySequenceDefinition sequenceDefinition)
        {
            if (sequenceDefinition.Sequence.Length > Count)
                return false;

            int start = sequenceDefinition.strict ? 0 : Count - sequenceDefinition.Sequence.Length;

            if (sequenceDefinition.strict && Count != sequenceDefinition.Sequence.Length)
                return false;

            for (int i = 0; i < sequenceDefinition.Sequence.Length; i++)
            {
                if (_sequence[i + start].pressType != sequenceDefinition.Sequence[i])
                    return false;
            }
            return true;
        }
    }
}

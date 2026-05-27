using BotControl;
using InControl;
using SlideMenu;
using System.Collections.Generic;
using UnityEngine;

namespace SlideDrum.sInputSystem
{
    public class sInputSystem
    {
        //TODO remove tap threshold, causes bugs tied to when should a new sequence start.
        //TODO handle overlapping key sequences from multiple buttons.
        public class KeyState
        {
            public bool WasKeyHeldOnThePreviousFrame = false;
            public sKeyPress RecentPress;
            public KeyCode Key;
            public Dictionary<sKeySequenceDefinition.TriggerPoint, bool> StatesResetStatus = new();
            public Dictionary<uint, bool> definitionStates = new();
            public bool TryGetSequence(out sKeySequence sequence)
            {
                if (RecentPress == null)
                {
                    sequence = default;
                    return false;
                }
                sequence = RecentPress.Sequence;
                return true;
            }
            public KeyState(KeyCode Key)
            {
                this.Key = Key;
            }
        }
        private static Dictionary<KeyCode, KeyState> KeyStates = new();
        public KeyState this[KeyCode Key]
        {
            get
            {
                if (!KeyStates.TryGetValue(Key, out var state))
                {
                    state = new KeyState(Key);
                    KeyStates[Key] = state;
                }
                return state;
            }
            set
            {
                KeyStates[Key] = value;
            }
        }
        internal float TapThreshold;  // TODO remove.  Tied to starting new sequences, might need rolling buffer after all?
        private HashSet<KeyCode> KeyCodes => GetKeyCodes();
        private List<sKeySequenceDefinition> SequenceDefinitions = new();
        public sInputSystem(float TapThreshold = sInputSystemDefaults.TapThreshold)
        {
            this.TapThreshold = TapThreshold;
        }
        public void AddListener(sKeySequenceDefinition newSequenceDefinition, FlexibleMethodDefinition callback = null, KeyCode? Key = null)
        {
            if (callback != null)
                newSequenceDefinition.Callback = callback;
            if (Key != null)
                newSequenceDefinition.SetKeyCode((KeyCode)Key);
            SequenceDefinitions.Add(newSequenceDefinition);
            if (newSequenceDefinition.Callback == null)
                ZiMain.log.LogWarning($"Created a keypress sequence listener with no callback! \"{newSequenceDefinition.Key}\" Nothing will happen!");
        }
        private HashSet<KeyCode> GetKeyCodes()
        {
            HashSet<KeyCode> ret = new();
            foreach (var sequence in SequenceDefinitions)
                ret.UnionWith(sequence.GetKeyCodes());
            return ret;
        }
        public void Update()
        {
            float time = Time.time;
            foreach (KeyCode Key in KeyCodes)
            {
                var KeyState = this[Key];
                bool keyPressed = Input.GetKey(Key);
                bool keyDown = keyPressed && !KeyState.WasKeyHeldOnThePreviousFrame; // the button was not pressed last frame but is now
                bool keyUp = !keyPressed && KeyState.WasKeyHeldOnThePreviousFrame; // the button was pressed last frame but is not now
                KeyState.WasKeyHeldOnThePreviousFrame = keyPressed;
                if (keyDown)
                {
                    KeyState.RecentPress = new sKeyPress(Key, this);
                }
                else if (keyUp)
                {
                    if (KeyState.RecentPress == null)
                        throw new System.InvalidOperationException("RecetPress was null on key up");
                    KeyState.RecentPress.SetUp();
                }
                if (keyPressed)
                {
                    Trigger(KeyState, sKeySequenceDefinition.TriggerPoint.Pressed);
                    KeyState.StatesResetStatus[sKeySequenceDefinition.TriggerPoint.Pressed] = false;
                    Reset(KeyState, sKeySequenceDefinition.TriggerPoint.Unpressed);
                }
                else
                {
                    Trigger(KeyState, sKeySequenceDefinition.TriggerPoint.Unpressed);
                    KeyState.StatesResetStatus[sKeySequenceDefinition.TriggerPoint.Unpressed] = false;
                    Reset(KeyState, sKeySequenceDefinition.TriggerPoint.Pressed);
                }
                sKeySequence sequence;
                if (KeyState.TryGetSequence(out sequence))
                {
                    if (sequence.IsSequenceOngoing)
                        Reset(KeyState, sKeySequenceDefinition.TriggerPoint.SequenceComplete);
                    else
                    {
                        Trigger(KeyState, sKeySequenceDefinition.TriggerPoint.SequenceComplete);
                        KeyState.StatesResetStatus[sKeySequenceDefinition.TriggerPoint.SequenceComplete] = false;
                    }
                }
            }
        }
        private void Reset(KeyState KeyState, sKeySequenceDefinition.TriggerPoint trigger)
        {
            if (!KeyState.StatesResetStatus.TryGetValue(trigger, out var reset) || !reset)
            {
                for (int i = 0; i < SequenceDefinitions.Count; i++)
                {
                    var definition = SequenceDefinitions[i];
                    if (definition.Trigger == trigger)
                    {
                        KeyState.definitionStates[definition.Id] = false;
                    }
                }
                KeyState.StatesResetStatus[trigger] = true;
            }
        }
        private void Trigger(KeyState KeyState, sKeySequenceDefinition.TriggerPoint trigger)
        {
            sKeySequence sequence;
            if (!KeyState.TryGetSequence(out sequence))
                return;
            for (int i = 0; i < SequenceDefinitions.Count; i++)
            {
                var definition = SequenceDefinitions[i];

                if (definition.Trigger != trigger)
                    continue;
                bool state = sequence.MatchesSequence(definition);
                if (state && (!definition.RisingEdgeOnly || !KeyState.definitionStates.ContainsKey(definition.Id) || !KeyState.definitionStates[definition.Id]))
                    definition.Callback.Invoke();
                KeyState.definitionStates[definition.Id] = state;
            }
        }
    }
}
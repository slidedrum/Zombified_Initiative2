using BotControl;
using SlideMenu;
using System.Collections.Generic;
using UnityEngine;

namespace SlideDrum.sInputSystem
{
    public class sInputSystem
    {
        internal float TapThreshold;
        private KeyCode Key;
        private bool WasKeyHeldOnThePreviousFrame;
        private sKeyPress RecetPress;
        private bool CurrentSequenceExists => RecetPress != null;
        private sKeySequence CurrentSequence => RecetPress.Sequence;
        private Dictionary<sKeySequenceDefinition.TriggerPoint, bool> StatesResetStatus = new();

        private List<sKeySequenceDefinition> SequenceDefinitions = new();
        private Dictionary<uint, bool> definitionStates = new();
        public sInputSystem(KeyCode Key, float TapThreshold = sInputSystemDefaults.TapThreshold)
        {
            this.Key = Key;
            this.TapThreshold = TapThreshold;
        }
        public void AddListener(sKeySequenceDefinition newSequenceDefinition, FlexibleMethodDefinition callback = null, KeyCode? Key = null)
        {
            if (callback != null)
                newSequenceDefinition.callback = callback;
            if (Key != null)
                newSequenceDefinition.SetKeyCode((KeyCode)Key);
            SequenceDefinitions.Add(newSequenceDefinition);
            if (newSequenceDefinition.callback == null)
                ZiMain.log.LogWarning($"Created a keypress sequence listener with no callback! \"{newSequenceDefinition.Key}\" Nothing will happen!");
        }
        public void Update()
        {
            float time = Time.time;
            bool keyPressed = Input.GetKey(Key);
            bool keyDown = keyPressed && !WasKeyHeldOnThePreviousFrame; // the button was not pressed last frame but is now
            bool keyUp = !keyPressed && WasKeyHeldOnThePreviousFrame; // the button was pressed last frame but is not now

            WasKeyHeldOnThePreviousFrame = keyPressed;
            if (keyDown)
            {
                RecetPress = new sKeyPress(Key, this);
            }
            else if (keyUp)
            {
                RecetPress.SetUp();
            }
            if (keyPressed)
            {
                Trigger(sKeySequenceDefinition.TriggerPoint.Pressed);
                StatesResetStatus[sKeySequenceDefinition.TriggerPoint.Pressed] = false;
                Reset(sKeySequenceDefinition.TriggerPoint.Unpressed);
            }
            else
            {
                Trigger(sKeySequenceDefinition.TriggerPoint.Unpressed);
                StatesResetStatus[sKeySequenceDefinition.TriggerPoint.Unpressed] = false;
                Reset(sKeySequenceDefinition.TriggerPoint.Pressed);
            }
            if (CurrentSequenceExists)
            {
                if (CurrentSequence.IsSequenceOngoing)
                    Reset(sKeySequenceDefinition.TriggerPoint.SequenceComplete);
                else
                {
                    Trigger(sKeySequenceDefinition.TriggerPoint.SequenceComplete);
                    StatesResetStatus[sKeySequenceDefinition.TriggerPoint.SequenceComplete] = false;
                }
            }
        }
        private void Reset(sKeySequenceDefinition.TriggerPoint trigger)
        {
            if (!StatesResetStatus.ContainsKey(trigger) || !StatesResetStatus[trigger])
            {
                for (int i = 0; i < SequenceDefinitions.Count; i++)
                {
                    var definition = SequenceDefinitions[i];
                    if (definition.Trigger == trigger)
                    {
                        definitionStates[definition.Id] = false;
                    }
                }
                StatesResetStatus[trigger] = true;
            }
        }
        private void Trigger(sKeySequenceDefinition.TriggerPoint trigger)
        {
            if (!CurrentSequenceExists)
                return;
            for (int i = 0; i < SequenceDefinitions.Count; i++)
            {
                var definition = SequenceDefinitions[i];

                if (definition.Trigger != trigger)
                    continue;
                bool state = CurrentSequence.MatchesSequence(definition);
                if (state && (!definition.RisingEdgeOnly || !definitionStates.ContainsKey(definition.Id) || !definitionStates[definition.Id]))
                    definition.callback.Invoke();
                definitionStates[definition.Id] = state;
            }
        }
    }
}
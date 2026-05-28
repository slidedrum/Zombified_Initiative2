using BotControl;
using SlideMenu;
using System.Collections.Generic;
using UnityEngine;

namespace SlideDrum.sInputSystem
{
    public static class sInputSystem
    {
        private static Dictionary<KeyCode, bool> WasKeyHeldOnThePreviousFrame = new();
        private static HashSet<KeyCode> KeyCodes 
        { 
            get 
            {
                HashSet<KeyCode> ret = new();
                foreach (sSequenceDefinition sequence in SequenceDefinitions)
                    ret.UnionWith(sequence.KeyCodes);
                return ret;
            } 
        }
        private static List<sSequenceDefinition> SequenceDefinitions = new();
        public static void AddListener(sSequenceDefinition newSequenceDefinition, FlexibleMethodDefinition callback = null, KeyCode? Key = null)
        {
            if (callback != null)
                newSequenceDefinition.Callback = callback;
            if (Key != null)
                newSequenceDefinition.SetKeyCode((KeyCode)Key);
            SequenceDefinitions.Add(newSequenceDefinition);
            if (newSequenceDefinition.Callback == null)
                ZiMain.log.LogWarning($"Created a keypress sequence listener with no callback! \"{newSequenceDefinition}\" Nothing will happen!");
        }
        public static void Update()
        {
            float time = Time.time;

            foreach (KeyCode Key in KeyCodes)
            {
                bool keyPressed = Input.GetKey(Key);

                WasKeyHeldOnThePreviousFrame.TryGetValue(Key, out bool wasHeld);

                bool keyDown = keyPressed && !wasHeld;
                bool keyUp = !keyPressed && wasHeld;

                if (keyDown)
                    sTimeline.Add(new InputEvent(Key, time, true));

                if (keyUp)
                    sTimeline.Add(new InputEvent(Key, time, false));

                WasKeyHeldOnThePreviousFrame[Key] = keyPressed;
            }

            EvaluateDefinitions();
        }
        private static void EvaluateDefinitions()
        {
            for (int i = 0; i < SequenceDefinitions.Count; i++)
            {
                sSequenceDefinition definition = SequenceDefinitions[i];

                if (definition.Matches())
                    definition.Callback.Invoke();
            }
        }
    }
}
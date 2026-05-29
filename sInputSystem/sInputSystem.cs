using BotControl;
using SlideMenu;
using System.Collections.Generic;
using UnityEngine;

namespace SlideDrum.sInputSystem
{
    public static class sInputSystem
    {
        private static Dictionary<KeyCode, bool> WasKeyHeldOnThePreviousFrame = new();
        private static int LenthOfSequenceDefinitionsWhenKeyCodesWasLastUpdates = 0;
        private static HashSet<KeyCode> _KeyCodes;
        private static HashSet<KeyCode> KeyCodes
        {
            get
            {
                if (_KeyCodes == null || LenthOfSequenceDefinitionsWhenKeyCodesWasLastUpdates != SequenceDefinitions.Count)
                {
                    if (_KeyCodes == null)
                        _KeyCodes = new();
                    else
                        _KeyCodes.Clear();
                    foreach (sSequenceDefinition sequence in SequenceDefinitions)
                        _KeyCodes.UnionWith(sequence.KeyCodes);
                    LenthOfSequenceDefinitionsWhenKeyCodesWasLastUpdates = SequenceDefinitions.Count;
                }
                return _KeyCodes;
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
                bool NeverPressed = !WasKeyHeldOnThePreviousFrame.TryGetValue(Key, out bool wasHeld);

                bool keyDown = keyPressed && (!wasHeld || NeverPressed);
                bool keyUp = !keyPressed && (wasHeld || NeverPressed);

                if (keyDown)
                    sTimeline.Add(new sInputEvent(Key, time, true));

                if (keyUp)
                    sTimeline.Add(new sInputEvent(Key, time, false));

                WasKeyHeldOnThePreviousFrame[Key] = keyPressed;
            }
            List<sKeyPressRefrence> Refrences = new(sTimeline.KeyPressRefrences); // we want to make our own clone so we don't rebuild the list every time we check it.
            EvaluateDefinitions(Refrences);
        }
        private static void EvaluateDefinitions(List<sKeyPressRefrence> Refrences)
        {
            for (int i = 0; i < SequenceDefinitions.Count; i++)
            {
                sSequenceDefinition definition = SequenceDefinitions[i];

                if (definition.Matches(Refrences))
                    definition.Callback.Invoke();
            }
        }
    }
}
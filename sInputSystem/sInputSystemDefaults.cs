using SlideDrum.sInputSystem;
using SlideMenu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SlideDrum.sInputSystem
{
    public static class sInputSystemDefaults
    {
        public const float TapThreshold = 0.25f;
        private static sKeyPressDefinition? _Tap;
        public static sKeyPressDefinition Tap 
        {
            get
            {
                if (_Tap == null)
                {
                    _Tap = new(
                        MaxHoldDurration: TapThreshold
                        );
                }
                return new sKeyPressDefinition(_Tap);
            }
        }
        private static sKeyPressDefinition? _Hold;
        public static sKeyPressDefinition Hold
        {
            get
            {
                if (_Hold == null)
                {
                    _Hold = new(
                        MinHoldDurration: TapThreshold
                        );
                }
                return new sKeyPressDefinition(_Hold);
            }
        }
        private static sKeyPressDefinition? _Press;
        public static sKeyPressDefinition Press
        {
            get
            {
                if (_Press == null)
                {
                    _Press = new(
                        );
                }
                return new sKeyPressDefinition(_Press);
            }
        }
        private static sKeySequenceDefinition? _OnPressed;
        public static sKeySequenceDefinition OnPressed
        {
            get
            {
                if (_OnPressed == null)
                {
                    _OnPressed = new sKeySequenceDefinition(
                        [Press],
                        sKeySequenceDefinition.TriggerPoint.Pressed,
                        null,
                        Strict: false,
                        RisingEdgeOnly: true
                    );
                }
                return (sKeySequenceDefinition)_OnPressed;
            }
        }
        private static sKeySequenceDefinition? _WhilePressed;
        public static sKeySequenceDefinition WhilePressed
        {
            get
            {
                if (_WhilePressed == null)
                {
                    _WhilePressed = new sKeySequenceDefinition(
                        [Press],
                        sKeySequenceDefinition.TriggerPoint.Pressed,
                        null,
                        Strict: false,
                        RisingEdgeOnly: false
                    );
                }
                return (sKeySequenceDefinition)_WhilePressed;
            }
        }
        private static sKeySequenceDefinition? _WhileUnpressed;
        public static sKeySequenceDefinition WhileUnpressed
        {
            get
            {
                if (_WhileUnpressed == null)
                {
                    _WhileUnpressed = new sKeySequenceDefinition(
                        [Press],
                        sKeySequenceDefinition.TriggerPoint.Unpressed,
                        null,
                        Strict: false,
                        RisingEdgeOnly: false
                    );
                }
                return (sKeySequenceDefinition)_WhileUnpressed;
            }
        }
        private static sKeySequenceDefinition? _OnUnpressed;
        public static sKeySequenceDefinition OnUnpressed
        {
            get
            {
                if (_OnUnpressed == null)
                {
                    _OnUnpressed = new sKeySequenceDefinition(
                        [Press],
                        sKeySequenceDefinition.TriggerPoint.Unpressed,
                        null,
                        Strict: false,
                        RisingEdgeOnly: true
                    );
                }
                return (sKeySequenceDefinition)_OnUnpressed;
            }
        }
        private static sKeySequenceDefinition? _OnTap;
        public static sKeySequenceDefinition OnTap 
        {
            get
            {
                if (_OnTap == null)
                {
                    _OnTap = new sKeySequenceDefinition(
                        [Tap],
                        sKeySequenceDefinition.TriggerPoint.Unpressed,
                        null,
                        Strict: false,
                        RisingEdgeOnly: true
                    );
                }
                return (sKeySequenceDefinition)_OnTap;
            }
        }
        private static sKeySequenceDefinition? _OnHold;
        public static sKeySequenceDefinition OnHold
        {
            get
            {
                if (_OnHold == null)
                {
                    _OnHold = new sKeySequenceDefinition(
                        [Hold],
                        sKeySequenceDefinition.TriggerPoint.Unpressed,
                        null,
                        Strict: false,
                        RisingEdgeOnly: true
                    );
                }
                return (sKeySequenceDefinition)_OnHold;
            }
        }
        private static sKeySequenceDefinition? _OnHoldImmediate;
        public static sKeySequenceDefinition OnHoldImmediate
        {
            get
            {
                if (_OnHoldImmediate == null)
                {
                    _OnHoldImmediate = new sKeySequenceDefinition(
                        [Hold],
                        sKeySequenceDefinition.TriggerPoint.Pressed,
                        null,
                        Strict: false,
                        RisingEdgeOnly: true
                    );
                }
                return (sKeySequenceDefinition)_OnHoldImmediate;
            }
        }
        private static sKeySequenceDefinition? _OnDoubleTap;
        public static sKeySequenceDefinition OnDoubleTap
        {
            get
            {
                if (_OnDoubleTap == null)
                {
                    _OnDoubleTap = new sKeySequenceDefinition(
                        [Tap, Tap],
                        sKeySequenceDefinition.TriggerPoint.Unpressed,
                        null,
                        Strict: false,
                        RisingEdgeOnly: true
                    );
                }
                return (sKeySequenceDefinition)_OnDoubleTap;
            }
        }


    }
}

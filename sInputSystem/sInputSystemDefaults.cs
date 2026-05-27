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
                return (sKeyPressDefinition)_Tap;
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
                return (sKeyPressDefinition)_Hold;
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
                        strict: false,
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
                        strict: false,
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
                        strict: false,
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
                        [Tap,Tap],
                        sKeySequenceDefinition.TriggerPoint.Unpressed,
                        null,
                        strict: false,
                        RisingEdgeOnly: true
                    );
                }
                return (sKeySequenceDefinition)_OnDoubleTap;
            }
        }
    }
}

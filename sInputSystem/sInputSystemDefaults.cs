namespace SlideDrum.sInputSystem
{
    public static class sInputSystemDefaults
    {
        public const float TapThreshold = 0.25f;
        public static sKeyPressDefinition Press
        {
            get
            {
                sKeyPressDefinition press = new(
                        Key: null,
                        Pressed: true,
                        Identifier: "Press"
                        );
                return press;
            }
        }
        public static sKeyPressDefinition ShortPress 
        {
            get
            {
                sKeyPressDefinition press = new(
                        Key: null,
                        Pressed: true,
                        MaxDuration: TapThreshold,
                        Identifier: "ShortPress"
                        );
                return press;
            }
        }
        public static sKeyPressDefinition LongPress
        {
            get
            {
                sKeyPressDefinition press = new(
                        Key: null,
                        Pressed: true,
                        MinDuration: TapThreshold,
                        Identifier: "LongPress"
                        );
                return press;
            }
        }
        public static sKeyPressDefinition Unpressed
        {
            get
            {
                sKeyPressDefinition press = new(
                        Key: null,
                        Pressed: false,
                        Identifier: "Unpressed"
                        );
                return press;
            }
        }
        public static sKeyPressDefinition LongUnpressed
        {
            get
            {
                sKeyPressDefinition press = new(
                        Key: null,
                        Pressed: false,
                        MinDuration: TapThreshold,
                        Identifier: "LongUnpressed"
                        );
                return press;
            }
        }
        public static sSequenceDefinition OnTapped
        {
            get
            {
                sKeyPressDefinition[] Presses = [ShortPress, Unpressed];
                sSequenceDefinition sequence = new(
                    Presses: Presses,
                    Callback: null,
                    Identifier: "OnTapped"
                    );
                return sequence;
            }
        }
        public static sSequenceDefinition OnHold
        {
            get
            {
                sKeyPressDefinition[] Presses = [LongPress, Unpressed];
                sSequenceDefinition sequence = new(
                    Presses: Presses,
                    Callback: null,
                    Identifier: "OnHold"
                    );
                return sequence;
            }
        }
        public static sSequenceDefinition OnHoldImmediate
        {
            get
            {
                sKeyPressDefinition[] Presses = [LongPress];
                sSequenceDefinition sequence = new(
                    Presses: Presses,
                    Callback: null,
                    Identifier: "OnHoldImmediate"
                    );
                return sequence;
            }
        }
        public static sSequenceDefinition OnDoubleTapped
        {
            get
            {
                sKeyPressDefinition[] Presses = [ShortPress, ShortPress, Unpressed];
                sSequenceDefinition sequence = new(
                    Presses: Presses,
                    Callback: null,
                    Identifier: "OnDoubleTapped"
                    );
                return sequence;
            }
        }
        public static sSequenceDefinition OnTapAndHold
        {
            get
            {
                sKeyPressDefinition[] Presses = [ShortPress, LongPress, Unpressed];
                sSequenceDefinition sequence = new(
                    Presses: Presses,
                    Callback: null,
                    Identifier: "OnTapAndHold"
                    );
                return sequence;
            }
        }
        public static sSequenceDefinition OnTapAndHoldExclusive
        {
            get
            {
                sKeyPressDefinition[] Presses = [LongUnpressed, ShortPress, LongPress, Unpressed];
                sSequenceDefinition sequence = new(
                    Presses: Presses,
                    Callback: null,
                    Identifier: "OnTapAndHoldExclusive"
                    );
                return sequence;
            }
        }
        public static sSequenceDefinition OnTapAndHoldImmediateExclusive
        {
            get
            {
                sKeyPressDefinition[] Presses = [LongUnpressed, ShortPress, LongPress];
                sSequenceDefinition sequence = new(
                    Presses: Presses,
                    Callback: null,
                    Identifier: "OnTapAndHoldImmediateExclusive"
                    );
                return sequence;
            }
        }
        public static sSequenceDefinition WhileHeld
        {
            get
            {
                sKeyPressDefinition[] Presses = [LongPress];
                sSequenceDefinition sequence = new(
                    Presses: Presses,
                    Callback: null,
                    RisingEdgeOnly: false,
                    Identifier: "WhileHeld"
                    );
                return sequence;
            }
        }
    }
}

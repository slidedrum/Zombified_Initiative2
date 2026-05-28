namespace SlideDrum.sInputSystem
{
    public static class sInputSystemDefaults
    {
        public const float TapThreshold = 0.25f;
        public static sKeyPressDefinition ShortPress 
        {
            get
            {
                sKeyPressDefinition ShortPress = new(
                        Key: null,
                        Pressed: true,
                        MaxDuration: TapThreshold,
                        Identifier: "ShortPress"
                        );
                return ShortPress;
            }
        }
        public static sKeyPressDefinition LongPress
        {
            get
            {
                sKeyPressDefinition LongPress = new(
                        Key: null,
                        Pressed: true,
                        MinDuration: TapThreshold,
                        Identifier: "LongPress"
                        );
                return LongPress;
            }
        }
        public static sKeyPressDefinition Unpressed
        {
            get
            {
                sKeyPressDefinition Unpressed = new(
                        Key: null,
                        Pressed: false,
                        Identifier: "Unpressed"
                        );
                return Unpressed;
            }
        }
        public static sSequenceDefinition OnTapped
        {
            get
            {
                var shortPress = ShortPress;
                var unpressed = Unpressed;
                sKeyPressDefinition[] Presses = [shortPress, unpressed];
                unpressed.AddAnchor(shortPress);
                sSequenceDefinition OnTapped = new(
                    Presses: Presses,
                    Callback: null,
                    Identifier: "OnTapped"
                    );
                return OnTapped;
            }
        }
        public static sSequenceDefinition OnHold
        {
            get
            {
                var longPress = LongPress;
                var unpressed = Unpressed;
                sKeyPressDefinition[] Presses = [longPress, unpressed];
                unpressed.AddAnchor(longPress);
                sSequenceDefinition OnHold = new(
                    Presses: Presses,
                    Callback: null,
                    Identifier: "OnHold"
                    );
                return OnHold;
            }
        }
    }
}

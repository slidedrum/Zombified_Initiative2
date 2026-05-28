namespace SlideDrum.sInputSystem
{
    public static class sInputSystemDefaults
    {
        public const float TapThreshold = 0.25f;
        private static sKeyPressDefinition? _ShortPress;
        public static sKeyPressDefinition ShortPress 
        {
            get
            {
                sKeyPressDefinition _ShortPress = new(
                        Key: null,
                        Pressed: true,
                        MaxDuration: TapThreshold
                        );
                return _ShortPress;
            }
        }
        public static sKeyPressDefinition Unpressed
        {
            get
            {
                sKeyPressDefinition _Unpressed = new(
                        Key: null,
                        Pressed: false
                        );
                return _Unpressed;
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
                sSequenceDefinition _OnTapped = new(
                    Presses: Presses,
                    Callback: null
                    );
                return _OnTapped;
            }
        }
    }
}

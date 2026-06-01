using BotControl.SmartSelect.PressTypes;
using Player;
using SlideDrum.sInputSystem;
using SlideMenu;
using System.Linq;
using UnityEngine;

namespace BotControl.SmartSelect
{
    public static class zSmartSelect
    {
        // This class handles everything with the smart select button (V)
        // Tapping and holding the button will tell the bot to move there.
        // You can also do a bunch of other actions:
        //
        //            (  TAP   /    HOLD     /  DOUBLE TAP  )
        //            ( ----------------------------------- )
        // Player/Bot (*Select*/ --*Share*-- / --*Follow*-- ) PlayerAgent
        //       Item ( ------ / -*Pickup*-- / ------------ ) ItemInLevel
        //     Sentry (*Pickup*/ --Refill--- / *Pickup all* ) SentryGunInstance
        //  Container ( ------ / Open,unlock / ---Place?--- ) LG_WeakResourceContainer
        // Floor/Wall ( ------ / Consumable- / -Equipment*- ) Raycast normal
        //    Holding ( ------ / -Drop Here- / --Drop Now-- ) Raycast normal
        //       Door ( -Open- / Throw cFoam / ---Break?--- ) LG_WeakDoor
        //      Enemy ( ------ / --Attack--- / -Countdown-- ) EnemyAgent //use voiceline PLAY_CL_THREETWOONEGO
        //  Generator ( ------ / Place cell- / ------------ ) LG_PowerGenerator_Core 

        public static Selection MainSelection = new();
        private static bool IsSetUp = false;
        public static uint InvalidSound = AK.EVENTS.MENU_HOST_EXPEDITION_BUTTON_RELEASE;
        public static uint CorrectSound = AK.EVENTS.MENU_HOST_EXPEDITION_BUTTON_FULL;
        private static float lastSlowUpdateTime = 0;
        private static float SelectionAngle = 30f;
        public static bool FallbackToClosest = true;
        private static float now => Time.time;
        private static float roundedTime => now - (now % slowupdateinterval);
        private const float slowupdateinterval = 0.25f;
        public enum PressTypes
        {
            Tap,
            Hold,
            DoubleTap,
            TapAndHold,
        }
        public enum ActionTypes
        {
            Agent,
            Item,
            Sentry,
            Container,
            Nothing,
            Door,
            Enemy,
            Generator,
        }
        public static pTypeTapPress TapPress = new();
        public static pTypeHoldPress HoldPress = new();
        public static pTypeDoublePress DoubleTapPress = new();
        public static pTypeTapAndHoldPress TapAndHoldPress = new();
        public static void Update()
        {
            bool ready = FocusStateManager.CurrentState == eFocusState.FPS || FocusStateManager.CurrentState == eFocusState.Dead;
            if (!ready) return;
            if (!IsSetUp) SetUp();
            sInputSystem.Update();
            if (roundedTime > lastSlowUpdateTime)
                SlowUpdate();
        }
        public static void SlowUpdate()
        {
            TapPress.Update();
            HoldPress.Update();
            DoubleTapPress.Update();
            TapAndHoldPress.Update();

            zSmartSelectHud.Update();
            lastSlowUpdateTime = roundedTime;
        }
        private static void SetUp()
        {
            sInputSystem.AddListener(sInputSystemDefaults.OnTappedExclusive, new FlexibleMethodDefinition(OnKeyTap), KeyCode.V);
            sInputSystem.AddListener(sInputSystemDefaults.OnHoldImmediateExclusive, new FlexibleMethodDefinition(OnKeyHold), KeyCode.V);
            sInputSystem.AddListener(sInputSystemDefaults.OnDoubleTappedExclusive, new FlexibleMethodDefinition(OnKeyDoubleTap), KeyCode.V);
            sInputSystem.AddListener(sInputSystemDefaults.OnTapAndHoldImmediateExclusive, new FlexibleMethodDefinition(OnTapAndHold), KeyCode.V);
            
            IsSetUp = true;
        }
        public static uint GetVoiceId(PlayerAIBot bot)
        {
            var Agent = bot.Agent;
            var botName = Agent.PlayerName;
            var botId = Agent.CharacterID;
            uint voiceID = 0u;

            if (botName.ToUpper().Contains("BISHOP"))
                voiceID = AK.EVENTS.PLAY_ADDRESSBISHOPIRRITATED01;
            if (botName.ToUpper().Contains("DAUDA"))
                voiceID = AK.EVENTS.PLAY_ADDRESSDAUDAIRRITATED01;
            if (botName.ToUpper().Contains("HACKET"))
                voiceID = AK.EVENTS.PLAY_ADDRESSHACKETTIRRITATED01;
            if (botName.ToUpper().Contains("WOODS"))
                voiceID = AK.EVENTS.PLAY_ADDRESSWOODSIRRITATED01;
            return voiceID;
        }
        public static PlayerAIBot GetBotLookingAt()
        {
            PlayerAIBot bot = zSearch.FindBestAligned(zStaticRefrences.CameraTransform, zStaticRefrences.AllBotObjects, SelectionAngle)?.GetComponent<PlayerAIBot>();
            return bot;
        }
        public static void OnKeyTap()
        {
            if (TapPress.Invoke())
                ZiMain.PlayUiSound(CorrectSound);
            else
                ZiMain.PlayUiSound(InvalidSound);
        }
        public static void OnKeyHold()
        {
            if (HoldPress.Invoke())
                ZiMain.PlayUiSound(CorrectSound);
            else
                ZiMain.PlayUiSound(InvalidSound);
        }
        public static void OnKeyDoubleTap()
        {
            if (DoubleTapPress.Invoke())
                ZiMain.PlayUiSound(CorrectSound);
            else
                ZiMain.PlayUiSound(InvalidSound);
        }
        public static void OnTapAndHold()
        {
            if (TapAndHoldPress.Invoke())
                ZiMain.PlayUiSound(CorrectSound);
            else
                ZiMain.PlayUiSound(InvalidSound);
        }
    }
}

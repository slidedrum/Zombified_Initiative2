using Player;
using SlideMenu;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BotControl.SmartSelect.PressActions
{
    public class pActionPickupSingleSentry : PressAction
    {
        public override string FriendlyName => "Pickup Sentry";
        public override string FriendlyNameShort => "Pickup";
        public override bool Invoke(Component BestComponent)
        {
            SentryGunInstance sentry = BestComponent.Cast<SentryGunInstance>();
            PlayerAIBot bot = sentry?.Owner?.GetComponent<PlayerAIBot>();
            if (bot == null)
                return false;
            PlayerVoiceManager.WantToSay(zStaticRefrences.LocalPlayer.CharacterID, AK.EVENTS.PLAY_CL_PICKUPYOURDEPLOYABLES);
            zStaticRefrences.Subtitles.ShowSingleLineSubtitle($"Pick up your deployables.", 1);
            zBotActions.SendBotToPickUpSentry(bot, zStaticRefrences.LocalPlayer);
            return true;
        }
    }
}

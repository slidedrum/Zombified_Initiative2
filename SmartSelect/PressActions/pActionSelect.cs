using Player;
using SlideMenu;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BotControl.SmartSelect.PressActions
{
    public class pActionSelect : PressAction
    {
        public override string FriendlyName => "Select Bot";
        public override string FriendlyNameShort => "Select";
        public override bool Invoke(Component BestComponent)
        {
            PlayerAIBot Bot = BestComponent.Cast<PlayerAIBot>();
            if (Bot == null)
                return false;
            zSmartSelect.MainSelection.Select(Bot);
            var Agent = Bot.Agent;
            var botName = Agent.PlayerName;
            var botId = Agent.CharacterID;
            var voiceID = zSmartSelect.GetVoiceId(Bot);

            PlayerVoiceManager.WantToSay(zStaticRefrences.LocalPlayer.CharacterID, voiceID);
            zStaticRefrences.Subtitles.ShowSingleLineSubtitle($"Hey {botName}!", 1);
            ZiMain.BotBarkBack(botId, AK.EVENTS.PLAY_CL_YES, "Yes?");
            if ((bool)zSlideComputer.ActionPermissions.ValueAt("Notify smart selected"))
                ZiMain.sendChatMessage("I'm ready", Agent, zStaticRefrences.LocalPlayer);
            return true;
        }
    }
}

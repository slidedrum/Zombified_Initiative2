using BepInEx.Unity.IL2CPP.Utils;
using Player;
using System.Collections;
using UnityEngine;

namespace BotControl.SmartSelect.PressActions
{
    public class pActionFollow : PressAction
    {
        public override string FriendlyName => "Follow Me";
        public override string FriendlyNameShort => "Follow";
        public override bool Invoke(Component BestComponent)
        {
            PlayerAgent Agent = BestComponent.Cast<PlayerAgent>();
            PlayerAIBot bot = Agent?.GetComponent<PlayerAIBot>();
            if (bot == null)
                return false;
            zUpdater.Instance.StartCoroutine(CallBotToFollow(bot));
            return true;
        }
        public static IEnumerator CallBotToFollow(PlayerAIBot Bot)
        {
            ZiMain.sendChatMessage($"On the way.", Bot.Agent, zStaticRefrences.LocalPlayer);
            uint voidID = zSmartSelect.GetVoiceId(Bot);
            string botname = Bot.Agent.PlayerName;
            zStaticRefrences.Subtitles.ShowSingleLineSubtitle($"Hey {botname}, Follow me!", 2);
            PlayerVoiceManager.WantToSay(zStaticRefrences.LocalPlayer.CharacterID, voidID);
            yield return new WaitForSeconds(1f);
            PlayerVoiceManager.WantToSay(zStaticRefrences.LocalPlayer.CharacterID, AK.EVENTS.PLAY_CL_FOLLOWME);
            zStaticRefrences.CommsMenu.OnButtonPressedCall(null, Bot.Agent);
        }
    }
}

using Player;
using SlideMenu;
using System.Collections.Generic;
using UnityEngine;

namespace BotControl.SmartSelect.PressActions
{
    public class pActionDeselect : PressAction
    {
        public override string FriendlyName => "Deselect All Bots";
        public override string FriendlyNameShort => "Deselect";
        public override bool Invoke(Component BestComponent)
        {
            if (!zSmartSelect.MainSelection.Selected<PlayerAIBot>())
                return false;
            PlayerVoiceManager.WantToSay(zStaticRefrences.LocalPlayer.CharacterID, AK.EVENTS.PLAY_CL_CANCELTHAT);
            zStaticRefrences.Subtitles.ShowSingleLineSubtitle("Cancel that.", 1);
            if ((bool)zSlideComputer.ActionPermissions.ValueAt("Notify smart selected"))
            {
                HashSet<PlayerAIBot> selectedBots = zSmartSelect.MainSelection.GetSelected<PlayerAIBot>();
                foreach (PlayerAIBot selectedBot in selectedBots)
                {
                    ZiMain.sendChatMessage("Nevermind.", selectedBot.Agent, zStaticRefrences.LocalPlayer);
                }
            }
            zSmartSelect.MainSelection.Deselect<PlayerAIBot>();
            return true;
        }
    }
}

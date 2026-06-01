using BepInEx.Unity.IL2CPP.Utils;
using Player;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BotControl.SmartSelect.PressActions
{
    public class pActionPickupAllSentries : PressAction
    {
        public override string FriendlyName => "Pickup All Sentries";
        public override string FriendlyNameShort => "Pickup";
        public override bool Invoke(Component BestComponent)
        {
            var Botlist = ZiMain.GetBotList();
            List<PlayerAIBot> BotsWithTurretsOut = new();
            foreach (var Bot in Botlist)
            {
                ItemEquippable[] deployedItems = Bot.GetDeployedItems().ToArray();
                if (deployedItems.Length > 0)
                    BotsWithTurretsOut.Add(Bot);
            }
            if (BotsWithTurretsOut.Count > 0)
            {
                PlayerVoiceManager.WantToSay(zStaticRefrences.LocalPlayer.CharacterID, AK.EVENTS.PLAY_CL_PICKUPYOURDEPLOYABLES);
                zStaticRefrences.Subtitles.ShowSingleLineSubtitle($"Pick up your deployables.", 1);
                zUpdater.Instance.StartCoroutine(BotsPickupTurrets(BotsWithTurretsOut));
                return true;
            }
            return false;
        }
        public static IEnumerator BotsPickupTurrets(List<PlayerAIBot> Bots)
        {
            foreach (var bot in Bots)
            {
                zBotActions.SendBotToPickUpSentry(bot, zStaticRefrences.LocalPlayer);
                yield return new WaitForSeconds(0.25f);
            }
        }
    }
}

using Agents;
using BotControl.Patches;
using Enemies;
using LevelGeneration;
using Player;
using UnityEngine;

namespace BotControl.SmartSelect.PressActions
{
    public class pActionShareResource : PressAction
    {
        public override string FriendlyName => "Share Resource";
        public override string FriendlyNameShort => "Share";
        public override bool Invoke(Component BestComponent)
        {
            bool sucsess = false;
            PlayerAgent Agent = BestComponent.Cast<PlayerAgent>();
            float offset = 0;
            foreach (PlayerAIBot selectedBot in zSmartSelect.MainSelection.GetSelected<PlayerAIBot>())
            {
                uint resourcePackID = zHelpers.GetAgentBackpackItemId(selectedBot.Agent, InventorySlot.ResourcePack);
                bool needsResourceIhave = false;
                switch (resourcePackID)
                {
                    case (uint)ShareActionPatch.ResourceIDs.MediPack:
                        needsResourceIhave = Agent.NeedHealth();
                        break;
                    case (uint)ShareActionPatch.ResourceIDs.AmmoPack:
                        needsResourceIhave = Agent.NeedWeaponAmmo();
                        break;
                    case (uint)ShareActionPatch.ResourceIDs.ToolPack:
                        needsResourceIhave = Agent.NeedToolAmmo();
                        break;
                    case (uint)ShareActionPatch.ResourceIDs.DisinfectPack:
                        needsResourceIhave = Agent.NeedDisinfection();
                        break;
                }
                if (!needsResourceIhave)
                    continue;
                sucsess = true;
                zBotActions.SendBotToShareResourcePack(selectedBot, Agent, zStaticRefrences.LocalPlayer);
                PlayerVoiceManager.WantToSay(zStaticRefrences.LocalPlayer.CharacterID, AK.EVENTS.PLAY_CL_PLEASE);
                zStaticRefrences.Subtitles.ShowSingleLineSubtitle($"Please", 1);
                ZiMain.BotBarkBack(selectedBot.Agent.CharacterID, AK.EVENTS.PLAY_CL_WILLDO, "Will Do.", 1f + offset);
                offset += 0.25f;
            }
            return sucsess;
        }
    }
}

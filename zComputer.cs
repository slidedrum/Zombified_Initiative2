using GameData;
using Localization;
using Player;
using SNetwork;
using System.Collections.Generic;
using UnityEngine;

namespace Zombified_Initiative
{
    public class zComputer : MonoBehaviour
    {
        bool menusetup = false;
        TextDataBlock textmenuroot;
        TextDataBlock textallowedpickups;
        TextDataBlock textallowedshare;
        TextDataBlock textstopcommand;
        TextDataBlock textattack;
        TextDataBlock textpickup;
        TextDataBlock textsupply;
        TextDataBlock textsentry;
        public CommunicationNode mymenu;

        public bool allowedpickups = true;
        public bool allowedshare = true;
        public bool allowedmove = true;
        public bool started = false;
        List<PlayerBotActionBase> actionsToRemove = new();
        PlayerAgent myself = null;
        PlayerAIBot myAI = null;

        public PlayerBotActionBase pickupaction;
        public PlayerBotActionBase shareaction;
        public PlayerBotActionBase followaction;
        public PlayerBotActionBase travelaction;


        public void Initialize()
        {
            this.myself = this.gameObject.GetComponent<PlayerAgent>();
            if (this.myself == null) return;
            if (!this.myself.Owner.IsBot) { Destroy(this); return; }
            this.myAI = this.gameObject.GetComponent<PlayerAIBot>();
            Zi.log.LogInfo($"initializing zombified comp on {myself.PlayerName} slot {myself.PlayerSlotIndex}..");
            var localizationService = Text.TextLocalizationService.TryCast<GameDataTextLocalizationService>();
            try
            {
                textmenuroot = TextDataBlock.AddBlock(new() { persistentID = 0, internalEnabled = true, SkipLocalization = true, name = this.myself.PlayerName + "menuroot", English = this.myself.PlayerName });
                localizationService.m_texts.Add(textmenuroot.persistentID, textmenuroot.GetText(localizationService.CurrentLanguage));

                textallowedpickups = TextDataBlock.AddBlock(new() { persistentID = 0, internalEnabled = true, SkipLocalization = true, name = this.myself.PlayerName + "pickupperm", English = this.myself.PlayerName + $" toggle pickup permission" });
                localizationService.m_texts.Add(textallowedpickups.persistentID, textallowedpickups.GetText(localizationService.CurrentLanguage));

                textallowedshare = TextDataBlock.AddBlock(new() { persistentID = 0, internalEnabled = true, SkipLocalization = true, name = this.myself.PlayerName + "shareperm", English = this.myself.PlayerName + " toggle share permission" });
                localizationService.m_texts.Add(textallowedshare.persistentID, textallowedshare.GetText(localizationService.CurrentLanguage));

                textstopcommand = TextDataBlock.AddBlock(new() { persistentID = 0, internalEnabled = true, SkipLocalization = true, name = this.myself.PlayerName + "stopcommand", English = this.myself.PlayerName + " stop what you are doing" });
                localizationService.m_texts.Add(textstopcommand.persistentID, textstopcommand.GetText(localizationService.CurrentLanguage));

                textattack = TextDataBlock.AddBlock(new() { persistentID = 0, internalEnabled = true, SkipLocalization = true, name = this.myself.PlayerName + "attack", English = this.myself.PlayerName + " attack my target" });
                localizationService.m_texts.Add(textattack.persistentID, textattack.GetText(localizationService.CurrentLanguage));

                textpickup = TextDataBlock.AddBlock(new() { persistentID = 0, internalEnabled = true, SkipLocalization = true, name = this.myself.PlayerName + "pickup", English = this.myself.PlayerName + " pickup resource under my aim" });
                localizationService.m_texts.Add(textpickup.persistentID, textpickup.GetText(localizationService.CurrentLanguage));

                textsupply = TextDataBlock.AddBlock(new() { persistentID = 0, internalEnabled = true, SkipLocalization = true, name = this.myself.PlayerName + "supply", English = this.myself.PlayerName + " supply resource (aimed or me)" });
                localizationService.m_texts.Add(textsupply.persistentID, textsupply.GetText(localizationService.CurrentLanguage));

                textsentry = TextDataBlock.AddBlock(new() { persistentID = 0, internalEnabled = true, SkipLocalization = true, name = this.myself.PlayerName + "sentry", English = this.myself.PlayerName + " toggle sentry mode" });
                localizationService.m_texts.Add(textsentry.persistentID, textsentry.GetText(localizationService.CurrentLanguage));

                this.mymenu = new(this.textmenuroot.persistentID, CommunicationNode.ScriptType.None);
                mymenu.IsLastNode = false;
                mymenu.m_ChildNodes.Add(new CommunicationNode(textallowedpickups.persistentID, CommunicationNode.ScriptType.None));
                mymenu.m_ChildNodes.Add(new CommunicationNode(textallowedshare.persistentID, CommunicationNode.ScriptType.None));
                mymenu.m_ChildNodes.Add(new CommunicationNode(textstopcommand.persistentID, CommunicationNode.ScriptType.None));
                mymenu.m_ChildNodes.Add(new CommunicationNode(textattack.persistentID, CommunicationNode.ScriptType.None));
                mymenu.m_ChildNodes.Add(new CommunicationNode(textpickup.persistentID, CommunicationNode.ScriptType.None));
                mymenu.m_ChildNodes.Add(new CommunicationNode(textsupply.persistentID, CommunicationNode.ScriptType.None));
                mymenu.m_ChildNodes.Add(new CommunicationNode(textsentry.persistentID, CommunicationNode.ScriptType.None));

                mymenu.m_ChildNodes[0].DialogID = 314;
                mymenu.m_ChildNodes[1].DialogID = 314;
                mymenu.m_ChildNodes[2].DialogID = 314;
                mymenu.m_ChildNodes[3].DialogID = 314;
                mymenu.m_ChildNodes[4].DialogID = 314;
                mymenu.m_ChildNodes[5].DialogID = 314;
                mymenu.m_ChildNodes[6].DialogID = 314;

            }
            catch { }
            if (Zi.BotTable.Count == 0) Zi.BotTable.Add(myself.PlayerName, myAI);
            if (!Zi.BotTable.ContainsKey(myself.PlayerName)) Zi.BotTable.Add(myself.PlayerName, myAI);
            this.started = true;
        }

        public void OnDestroy()
        {
            if (Zi.BotTable.ContainsKey(myself.PlayerName)) Zi.BotTable.Remove(myself.PlayerName);
            mymenu.IsLastNode = true;
        }

        void Update()
        {
            if (!this.started) return;
            if (!this.menusetup && Zi.rootmenusetup)
            {
                int menunumber = 0;
                bool flag = false;
                // get index of zombified
                for (int num = 0; num < Zi._menu.m_menu.CurrentNode.ChildNodes[5].m_ChildNodes.Count; num++)
                    if (TextDataBlock.GetBlock(Zi._menu.m_menu.CurrentNode.ChildNodes[5].m_ChildNodes[num].TextId).English == "Zombified Initiative")
                        menunumber = num;

                // not readding bot if its already somehow in
                for (int num = 0; num < Zi._menu.m_menu.CurrentNode.ChildNodes[5].m_ChildNodes[menunumber].m_ChildNodes.Count; num++)
                    if (TextDataBlock.GetBlock(Zi._menu.m_menu.CurrentNode.ChildNodes[5].m_ChildNodes[menunumber].m_ChildNodes[num].TextId).English == myself.PlayerName)
                    {
                        flag = true;
                        Zi._menu.m_menu.CurrentNode.ChildNodes[5].m_ChildNodes[menunumber].m_ChildNodes[num].IsLastNode = false;
                    }

                if (!flag)
                {
                    Zi._menu.m_menu.CurrentNode.ChildNodes[5].m_ChildNodes[menunumber].m_ChildNodes.Add(mymenu);
                    for (int num = 0; num < Zi._menu.m_menu.CurrentNode.ChildNodes[5].m_ChildNodes[menunumber].m_ChildNodes.Count; num++)
                        if (TextDataBlock.GetBlock(Zi._menu.m_menu.CurrentNode.ChildNodes[5].m_ChildNodes[menunumber].m_ChildNodes[num].TextId).English == myself.PlayerName)
                            Zi._menu.m_menu.CurrentNode.ChildNodes[5].m_ChildNodes[menunumber].m_ChildNodes[num].IsLastNode = false;
                }
                this.menusetup = true;
            }

            if (!SNet.IsMaster) return;
            if (this.myAI.Actions.Count == 0) return;
            actionsToRemove.Clear();
            foreach (var action in this.myAI.Actions)
            {
                // sentry?
                if (action.GetIl2CppType().Name == "PlayerBotActionFollow") followaction = action;
                if (action.GetIl2CppType().Name == "PlayerBotActionTravel") travelaction = action;

                if (!allowedmove && action.GetIl2CppType().Name == "PlayerBotActionFollow") action.DescBase.Status = PlayerBotActionBase.Descriptor.StatusType.Queued;
                if (!allowedmove && action.GetIl2CppType().Name == "PlayerBotActionTravel") action.DescBase.Status = PlayerBotActionBase.Descriptor.StatusType.Queued;

                // pickups?
                if (!allowedpickups && action.GetIl2CppType().Name == "PlayerBotActionCollectItem")
                {
                    var descriptor = action.DescBase.Cast<PlayerBotActionCollectItem.Descriptor>();
                    var itemIsDesinfectionPack = descriptor.TargetItem.PublicName == "Disinfection Pack";
                    var itemIsMediPack = descriptor.TargetItem.PublicName == "MediPack";
                    var itemIsAmmoPack = descriptor.TargetItem.PublicName == "Ammo Pack";
                    var itemIsToolRefillPack = descriptor.TargetItem.PublicName == "Tool Refill Pack";

                    var itemIsPack = itemIsToolRefillPack || itemIsAmmoPack || itemIsMediPack || itemIsDesinfectionPack;
                    if (descriptor.Haste < Zi._manualActionsHaste && itemIsPack)
                    {
                        pickupaction = action;
                        actionsToRemove.Add(action);
                    }
                } // pickups

                // sharing?
                if (!allowedshare && action.GetIl2CppType().Name == "PlayerBotActionShareResourcePack")
                {
                    var descriptor = action.DescBase.Cast<PlayerBotActionShareResourcePack.Descriptor>();
                    if (descriptor.Haste < Zi._manualActionsHaste)
                    {
                        shareaction = action;
                        actionsToRemove.Add(action);
                    }
                } // share
            } // foreach action

            if (actionsToRemove.Count == 0) return;
            foreach (var action in actionsToRemove)
            {
                this.myAI.Actions.Remove(action);
                Zi.log.LogInfo($"{this.myself.PlayerName} action {action.GetIl2CppType().Name} was cancelled");
            }
            actionsToRemove.Clear();
        } // slowUpdate

        public void PreventManualActions()
        {
            if (!this.started) return;
            if (this.myAI.Actions.Count == 0) return;
            if (this.pickupaction != null) this.pickupaction.DescBase.SetCompletionStatus(PlayerBotActionBase.Descriptor.StatusType.Failed);
            if (this.shareaction != null) this.shareaction.DescBase.SetCompletionStatus(PlayerBotActionBase.Descriptor.StatusType.Failed);
            this.pickupaction = null;
            this.shareaction = null;

            var actionsToRemove = new List<PlayerBotActionBase>();
            var haste = Zi._manualActionsHaste - 0.01f;

            foreach (var action in this.myAI.Actions)
            {
                if (action.GetIl2CppType().Name == "PlayerBotActionAttack")
                {
                    var descriptor = action.DescBase.Cast<PlayerBotActionAttack.Descriptor>();
                    if (descriptor.Haste > haste)
                    {
                        actionsToRemove.Add(action);
                        continue;
                    }
                }
                if (action.GetIl2CppType().Name == "PlayerBotActionCollectItem")
                {
                    var descriptor = action.DescBase.Cast<PlayerBotActionCollectItem.Descriptor>();
                    if (descriptor.Haste > haste)
                    {
                        actionsToRemove.Add(action);
                        continue;
                    }
                }

                if (action.GetIl2CppType().Name == "PlayerBotActionShareResourcePack")
                {
                    var descriptor = action.DescBase.Cast<PlayerBotActionShareResourcePack.Descriptor>();
                    if (descriptor.Haste > haste)
                    {
                        actionsToRemove.Add(action);
                        continue;
                    }
                }
            }

            foreach (var action in actionsToRemove)
            {
                myAI.Actions.Remove(action); // Queued stop
                // this.myAI.StopAction(action.DescBase); // Instant stop
                Zi.log.LogInfo($"{this.myself.PlayerName}'s manual actions were cancelled");
            }
            actionsToRemove.Clear();
        } // preventmanual
        public void updateExtraInfo()
        {
            PlayerAgent agent = this.gameObject.GetComponent<PlayerAgent>();
            var nav = agent.NavMarker;
            nav.UpdateExtraInfo();
        }
        public void togglePickupPermission()
        {
            this.allowedpickups = !this.allowedpickups;
            updateExtraInfo();
        }
        public void toggleSharePermission()
        {
            this.allowedpickups = !this.allowedpickups;
            updateExtraInfo();
        }
        public void ToggleSentryMode()
        {
            this.allowedmove = !this.allowedmove;
            if (!this.allowedmove)
            {
                if (this.followaction != null) this.followaction.DescBase.SetCompletionStatus(PlayerBotActionBase.Descriptor.StatusType.Failed);
                if (this.travelaction != null) this.travelaction.DescBase.SetCompletionStatus(PlayerBotActionBase.Descriptor.StatusType.Failed);
            }
            updateExtraInfo();
        }
        public void ExecuteBotAction(PlayerBotActionBase.Descriptor descriptor, string message)
        {
            this.myAI.StartAction(descriptor);
            Zi.log.LogInfo(message);
        }
    }
}
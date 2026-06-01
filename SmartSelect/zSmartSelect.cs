using BepInEx.Unity.IL2CPP.Utils;
using BotControl.Patches;
using BotControl.SmartSelect.PressTypes;
using Enemies;
using Il2CppInterop.Runtime;
using LevelGeneration;
using Player;
using SlideDrum.sInputSystem;
using SlideMenu;
using System;
using System.Collections;
using System.Collections.Generic;
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
        public static Dictionary<PressTypes, HashSet<Il2CppSystem.Type>> PressTypeMap = new();
        public static Dictionary<PressTypes, Component> CurrentActionComponenet = new();
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
        public static bool SelectBot(PlayerAIBot bot)
        {
            if (bot == null)
                return false;
            MainSelection.Select(bot);
            var Agent = bot.Agent;
            var botName = Agent.PlayerName;
            var botId = Agent.CharacterID;
            var voiceID = GetVoiceId(bot);

            PlayerVoiceManager.WantToSay(zStaticRefrences.LocalPlayer.CharacterID, voiceID);
            zStaticRefrences.Subtitles.ShowSingleLineSubtitle($"Hey {botName}!", 1);
            ZiMain.BotBarkBack(botId, AK.EVENTS.PLAY_CL_YES, "Yes?");
            if ((bool)zSlideComputer.ActionPermissions.ValueAt("Notify smart selected"))
                ZiMain.sendChatMessage("I'm ready", Agent, zStaticRefrences.LocalPlayer);
            return true;
        }
        public static bool SelectBotInView()
        {
            PlayerAIBot bot = GetBotLookingAt();
            return SelectBot(bot);
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
        //public static bool _OnKeyTap() // TODO make this a thing that returns the action that you would do if you tapped.  and have another method to actually do that.
        //{
        //    Component BestComponent = CurrentActionComponenet[PressTypes.Tap];
        //    Il2CppSystem.Type type = BestComponent?.GetIl2CppType();
        //    PlayerAIBot bot;
        //    if (type == null)
        //    {
        //        bool facingUp = Vector3.Angle(zStaticRefrences.CameraTransform.forward, Vector3.up) < 15f;
        //        if (facingUp && MainSelection.Selected<PlayerAIBot>())
        //        {
        //            DeselectAllBots();
        //            return true;
        //        }
        //    }
        //    else if (zHelpers.IsOfType<PlayerAIBot>(type))
        //    {
        //        bot = BestComponent.Cast<PlayerAIBot>();
        //        if (SelectBot(bot))
        //            return true;
        //    }
        //    else if (zHelpers.IsOfType<SentryGunInstance>(type))
        //    {
        //        SentryGunInstance sentry = BestComponent.Cast<SentryGunInstance>();
        //        if (TapPressSentry(sentry))
        //            return true;
        //    }
        //    else if (zHelpers.IsOfType<LG_WeakDoor>(type))
        //    {
        //        LG_WeakDoor Door = BestComponent.Cast<LG_WeakDoor>();
        //        if (TapPressDoor(Door))
        //            return true;
        //    }
        //    bot = GetBotLookingAt();
        //    if (bot != null)
        //    {
        //        SelectBot(bot);
        //        return true;
        //    }
        //    return false;
        //}
        public static void OnKeyHold()
        {
            if (HoldPress.Invoke())
                ZiMain.PlayUiSound(CorrectSound);
            else
                ZiMain.PlayUiSound(InvalidSound);
        }
        public static bool _OnKeyHold()
        {
            if (!MainSelection.Selected<PlayerAIBot>())
            {
                ZiMain.PlayUiSound(InvalidSound);
                return false;
            }
            Component BestComponent = CurrentActionComponenet[PressTypes.Hold];
            Il2CppSystem.Type type = BestComponent?.GetIl2CppType();
            if (type == null)
            {
                if (HoldPressNothing(zStaticRefrences.CameraTransform))
                    return true;
            }
            else if (zHelpers.IsOfType<PlayerAgent>(type))
            {
                PlayerAgent agent = BestComponent.Cast<PlayerAgent>();
                if (HoldPressPlayerAgent(agent))
                    return true;
            }
            else if (zHelpers.IsOfType<ItemInLevel>(type))
            {
                ItemInLevel item = BestComponent.Cast<ItemInLevel>();
                if (HoldPressItemInLevel(item))
                    return true;
            }
            else if (zHelpers.IsOfType<SentryGunInstance>(type))
            {
                SentryGunInstance sentry = BestComponent.Cast<SentryGunInstance>();
                if (HoldPressSentryGrun(sentry))
                    return true;
            }
            else if (zHelpers.IsOfType<LG_WeakResourceContainer>(type)) // Might need to deprioritize this if an item is in the way somehow.
            {
                LG_WeakResourceContainer container = BestComponent.Cast<LG_WeakResourceContainer>();
                if (HoldPressContainer(container))
                    return true;
            }
            else if (zHelpers.IsOfType<LG_WeakDoor>(type))
            {
                LG_WeakDoor door = BestComponent.Cast<LG_WeakDoor>();
                if (HoldPressDoor(door))
                    return true;
            }
            else if (zHelpers.IsOfType<EnemyAgent>(type))
            {
                EnemyAgent enemy = BestComponent.Cast<EnemyAgent>();
                if (HoldPressEnemy(enemy))
                    return true;
            }
            else if (zHelpers.IsOfType<LG_PowerGenerator_Core>(type))
            {
                LG_PowerGenerator_Core generator = BestComponent.Cast<LG_PowerGenerator_Core>();
                if (HoldPressGenerator(generator))
                    return true;
            }
            return false;
        }
        public static void OnKeyDoubleTap()
        {
            if (DoubleTapPress.Invoke())
                ZiMain.PlayUiSound(CorrectSound);
            else
                ZiMain.PlayUiSound(InvalidSound);
        }
        public static bool _OnKeyDoubleTap()
        {
            bool hasSelectedBot = MainSelection.Selected<PlayerAIBot>();
            Component BestComponent = CurrentActionComponenet[PressTypes.DoubleTap];
            Il2CppSystem.Type type = BestComponent?.GetIl2CppType();
            if (type == null)
            {
                if (!hasSelectedBot)
                    return false;
                if (TurretHandler.TryPlaceTurret())
                    return true;
            }
            else if (zHelpers.IsOfType<PlayerAgent>(type))
            {
                PlayerAgent Agent = BestComponent.Cast<PlayerAgent>();
                if (DoublePressAgent(Agent))
                    return true;
            }
            else if (zHelpers.IsOfType<SentryGunInstance>(type))
            {
                SentryGunInstance Sentry = BestComponent.Cast<SentryGunInstance>();
                if (DoublePressSentry(Sentry))
                    return true;
            }
            else if (zHelpers.IsOfType<LG_WeakResourceContainer>(type))
            {
                if (!hasSelectedBot)
                    return false;
                LG_WeakResourceContainer Container = BestComponent.Cast<LG_WeakResourceContainer>();
                if (DoublePressContainer(Container))
                    return true;
            }
            else if (zHelpers.IsOfType<LG_WeakDoor>(type))
            {
                if (!hasSelectedBot)
                    return false;
                LG_WeakDoor Door = BestComponent.Cast<LG_WeakDoor>();
                if (DoublePressDoor(Door))
                    return true;
            }
            else if (zHelpers.IsOfType<EnemyAgent>(type))
            {
                if (!hasSelectedBot)
                    return false;
                EnemyAgent Enemy = BestComponent.Cast<EnemyAgent>();
                if (DoublePressEnemy(Enemy))
                    return true;
            }
            var bot = GetBotLookingAt();
            if (bot != null)
            {
                zUpdater.Instance.StartCoroutine(CallBotToFollow(bot));
                return true;
            }
                return false;
        }
        public static void OnTapAndHold()
        {
            if (TapAndHoldPress.Invoke())
                ZiMain.PlayUiSound(CorrectSound);
            else
                ZiMain.PlayUiSound(InvalidSound);
        }
        public static string WhatHappensIfYouTapAndHold()
        {
            PlayerAIBot BestBot = MainSelection.GetSelected<PlayerAIBot>().FirstOrDefault();
            if (BestBot == null)
            {
                return " - ";
            }
            var destinationPosition = zStaticRefrences.LocalPlayer.FPSCamera.CameraRayPos;
            if (zHelpers.PositionIsValidForAgent(BestBot.Agent, ref destinationPosition))
            {
                return "Move";
            }
            return " - ";
        }
        public static bool _OnTapAndHold()
        {
            PlayerAIBot BestBot = MainSelection.GetSelected<PlayerAIBot>().FirstOrDefault();
            if (BestBot == null)
            {
                return false;
            }
            var destinationPosition = zStaticRefrences.LocalPlayer.FPSCamera.CameraRayPos;
            if (zHelpers.PositionIsValidForAgent(BestBot.Agent, ref destinationPosition))
            {
                ZiMain.sendChatMessage("I would have moved to that locataion, but I'm stupid!", BestBot.Agent, zStaticRefrences.LocalPlayer);
                return true;
            }
            return false;
        }
        public static bool TapPressSentry(SentryGunInstance sentry)
        {
            PlayerAIBot bot = sentry?.Owner?.GetComponent<PlayerAIBot>();
            if (bot == null)
                return false;
            PlayerVoiceManager.WantToSay(zStaticRefrences.LocalPlayer.CharacterID, AK.EVENTS.PLAY_CL_PICKUPYOURDEPLOYABLES);
            zStaticRefrences.Subtitles.ShowSingleLineSubtitle($"Pick up your deployables.", 1);
            zBotActions.SendBotToPickUpSentry(bot, zStaticRefrences.LocalPlayer);
            return true;
        }
        public static bool TapPressDoor(LG_WeakDoor Door)
        {
            PlayerAIBot BestBot = MainSelection.GetSelected<PlayerAIBot>().FirstOrDefault();
            if (BestBot == null)
                return false;
            ZiMain.sendChatMessage($"I would have openend/closed the door, but I'm stupid.", BestBot.Agent, zStaticRefrences.LocalPlayer);
            return true;
        }
        public static bool HoldPressNothing(Transform transform)
        {
            if (Vector3.Angle(transform.forward, Vector3.down) < 15f) // are we looking down?  if so, consider us interacting with our player agent.
            {
                HoldPressPlayerAgent(zStaticRefrences.LocalPlayer);
                return true;
            }
            return false;
            // try to have them throw/place their consumable!
        }
        public static string GetHoldPressPlayerAgentAction(PlayerAgent Agent)
        {
            bool sucsess = false;
            if (Agent.Alive)
            {
                float offset = 0;
                foreach (PlayerAIBot selectedBot in MainSelection.GetSelected<PlayerAIBot>())
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
                    return "Share";
                }
            }
            else // Agent is dead
                return "Revive";
            return " - ";
        }
        public static bool HoldPressPlayerAgent(PlayerAgent Agent)
        {
            bool sucsess = false;
            if (Agent.Alive)
            {
                float offset = 0;
                foreach (PlayerAIBot selectedBot in MainSelection.GetSelected<PlayerAIBot>())
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
            }
            else // Agent is dead
            {
                PlayerAIBot BestBot = MainSelection.GetSelected<PlayerAIBot>().First();
                ZiMain.sendChatMessage($"I would have revived {Agent.PlayerName}, but I'm stupid.", BestBot.Agent, zStaticRefrences.LocalPlayer);
            }
            if (!sucsess)
                ZiMain.PlayUiSound(InvalidSound);
            return sucsess;
        }
        public static bool HoldPressItemInLevel(ItemInLevel item)
        {
            PlayerAIBot BestBot = MainSelection.GetSelected<PlayerAIBot>().FirstOrDefault();
            if (BestBot == null)
                return false;
            zBotActions.SendBotToPickupItem(BestBot, item, zStaticRefrences.LocalPlayer);
            return true;
        }
        public static string GetHoldPressSentryGunAction(SentryGunInstance sentry)
        {
            HashSet<PlayerAIBot> selection = MainSelection.GetSelected<PlayerAIBot>();
            foreach (PlayerAIBot bot in selection)
            {
                bool owned = sentry.Owner == bot.Agent;
                bool haveTool = (zHelpers.GetAgentBackpackItemId(bot.Agent, InventorySlot.ResourcePack) == (uint)ShareActionPatch.ResourceIDs.ToolPack);
                if (haveTool)
                {
                    return "Refill";
                }
            }
            return " - ";
        }
        public static bool HoldPressSentryGrun(SentryGunInstance sentry)
        {
            HashSet<PlayerAIBot> selection = MainSelection.GetSelected<PlayerAIBot>();
            bool sucsess = false;
            foreach (PlayerAIBot bot in selection)
            {
                // do you have tool resources to share?
                // are you the owner of the sentry?
                bool owned = sentry.Owner == bot.Agent;
                bool haveTool = (zHelpers.GetAgentBackpackItemId(bot.Agent, InventorySlot.ResourcePack) == (uint)ShareActionPatch.ResourceIDs.ToolPack);
                if (haveTool)
                {
                    ZiMain.sendChatMessage($"I would have refilled the sentry, but I'm stupid.", bot.Agent, zStaticRefrences.LocalPlayer);
                    sucsess = true;
                    // TODO send them to refill the sentry
                    // Seems like this is not a vanilla behavior I can hook into.
                    // This will have to wait untill I attempt custom actions again.
                }
            }
            return sucsess;
        }
        public static bool HoldPressContainer(LG_WeakResourceContainer container)
        {
            var BestBot = MainSelection.GetSelected<PlayerAIBot>().First();
            if (BestBot == null)
                return false;
            ZiMain.sendChatMessage("I would have opend up the container, but I'm stupid!", BestBot.Agent, zStaticRefrences.LocalPlayer);
            return true;
        }
        public static bool HoldPressDoor(LG_WeakDoor door)
        {
            var BestBot = MainSelection.GetSelected<PlayerAIBot>().First();
            if (BestBot == null)
                return false;
            ZiMain.sendChatMessage("I would have secured the door, but I'm stupid!", BestBot.Agent, zStaticRefrences.LocalPlayer);
            return true;
        }
        public static bool HoldPressEnemy(EnemyAgent enemy)
        {
            var BestBot = MainSelection.GetSelected<PlayerAIBot>().First();
            if (BestBot == null)
                return false;
            ZiMain.sendChatMessage("I would have attacked the enemy, but I'm stupid!", BestBot.Agent, zStaticRefrences.LocalPlayer);
            return true;
        }
        public static bool HoldPressGenerator(LG_PowerGenerator_Core generator)
        {
            var BestBot = MainSelection.GetSelected<PlayerAIBot>().First();
            if (BestBot == null)
                return false;
            ZiMain.sendChatMessage("I would have tried to incert a cell, but I'm stupid!", BestBot.Agent, zStaticRefrences.LocalPlayer);
            return true;
        }
        public static bool DoublePressAgent(PlayerAgent Agent)
        {
            PlayerAIBot bot = Agent?.GetComponent<PlayerAIBot>();
            if (bot == null)
                return false;
            zUpdater.Instance.StartCoroutine(CallBotToFollow(bot));
            return true;
        }
        public static bool DoublePressSentry(SentryGunInstance Sentry)
        {
            var Botlist = ZiMain.GetBotList();
            List<PlayerAIBot> BotsWithTurretsOut = new();
            foreach (var bot in Botlist)
            {
                ItemEquippable[] deployedItems = bot.GetDeployedItems().ToArray();
                if (deployedItems.Length > 0)
                    BotsWithTurretsOut.Add(bot);
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
        public static bool DoublePressContainer(LG_WeakResourceContainer Container)
        {
            PlayerAIBot BestBot = MainSelection.GetSelected<PlayerAIBot>().FirstOrDefault();
            if (BestBot == null)
                return false;
            ZiMain.sendChatMessage("I would have tried to place my item in the container, but I'm stupid!", BestBot.Agent, zStaticRefrences.LocalPlayer);
            return true;
        }
        public static bool DoublePressDoor(LG_WeakDoor Door)
        {
            PlayerAIBot BestBot = MainSelection.GetSelected<PlayerAIBot>().FirstOrDefault();
            if (BestBot == null)
                return false;
            ZiMain.sendChatMessage("I would have tried to break the door, but I might be stupid!", BestBot.Agent, zStaticRefrences.LocalPlayer);
            return true;
        }
        public static bool DoublePressEnemy(EnemyAgent Enemy)
        {
            PlayerAIBot BestBot = MainSelection.GetSelected<PlayerAIBot>().FirstOrDefault();
            if (BestBot == null)
                return false;
            PlayerVoiceManager.WantToSay(BestBot.Agent.CharacterID, AK.EVENTS.PLAY_CL_THREETWOONEGO);
            zStaticRefrences.Subtitles.ShowSingleLineSubtitle("Three, two, one, GO!"); // TODO split this out
            ZiMain.sendChatMessage("I would have attacked the enemy at the end of the countdown, but I'm stupid!", BestBot.Agent, zStaticRefrences.LocalPlayer);
            return true;
        }
        public static IEnumerator CallBotToFollow(PlayerAIBot Bot)
        {
            ZiMain.sendChatMessage($"On the way.", Bot.Agent, zStaticRefrences.LocalPlayer);
            uint voidID = GetVoiceId(Bot);
            string botname = Bot.Agent.PlayerName;
            zStaticRefrences.Subtitles.ShowSingleLineSubtitle($"Hey {botname}, Follow me!", 2);
            PlayerVoiceManager.WantToSay(zStaticRefrences.LocalPlayer.CharacterID, voidID);
            yield return new WaitForSeconds(1f);
            PlayerVoiceManager.WantToSay(zStaticRefrences.LocalPlayer.CharacterID, AK.EVENTS.PLAY_CL_FOLLOWME);
            zStaticRefrences.CommsMenu.OnButtonPressedCall(null, Bot.Agent);            
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

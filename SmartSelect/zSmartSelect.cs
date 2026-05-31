using BepInEx.Unity.IL2CPP.Utils;
using BotControl.Patches;
using Enemies;
using Il2CppInterop.Runtime;
using LevelGeneration;
using Player;
using SlideDrum.sInputSystem;
using SlideMenu;
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
        // Floor/Wall ( ------ / Consumable- / -*Equipment* ) Raycast normal
        //    Holding ( ------ / -Drop Here- / --Drop Now-- ) Raycast normal
        //       Door ( -Open- / Throw cFoam / ---Break?--- ) LG_WeakDoor
        //      Enemy ( ------ / --Attack--- / -Countdown-- ) EnemyAgent //use voiceline PLAY_CL_THREETWOONEGO
        //  Generator ( ------ / Place cell- / ------------ ) LG_PowerGenerator_Core 

        public static Selection MainSelection = new();
        private static bool IsSetUp = false;
        public static readonly uint InvalidSound = AK.EVENTS.MENU_HOST_EXPEDITION_BUTTON_RELEASE;
        internal static void Update()
        {
            bool ready = FocusStateManager.CurrentState == eFocusState.FPS || FocusStateManager.CurrentState == eFocusState.Dead;
            if (!ready) return;
            if (!IsSetUp) SetUp();
            sInputSystem.Update();
        }
        private static void SetUp()
        {
            sInputSystem.AddListener(sInputSystemDefaults.OnTappedExclusive, new FlexibleMethodDefinition(onKeyTap), KeyCode.V);
            sInputSystem.AddListener(sInputSystemDefaults.OnHoldImmediateExclusive, new FlexibleMethodDefinition(onKeyHeld), KeyCode.V);
            sInputSystem.AddListener(sInputSystemDefaults.OnDoubleTapped, new FlexibleMethodDefinition(onKeyDoubleTap), KeyCode.V);
            sInputSystem.AddListener(sInputSystemDefaults.OnTapAndHoldImmediateExclusive, new FlexibleMethodDefinition(OnTapAndHold), KeyCode.V);
            IsSetUp = true;
        }
        public static PlayerAIBot GetBotLookingAt()
        {
            PlayerAIBot bot = zSearch.FindBestAligned(zStaticRefrences.CameraTransform, zStaticRefrences.AllBotObjects, 30f)?.GetComponent<PlayerAIBot>();
            return bot;
        }
        private static void DeselectAllBots()
        {
            PlayerVoiceManager.WantToSay(zStaticRefrences.LocalPlayer.CharacterID, AK.EVENTS.PLAY_CL_CANCELTHAT);
            zStaticRefrences.Subtitles.ShowSingleLineSubtitle("Cancel that.", 1);
            if ((bool)zSlideComputer.ActionPermissions.ValueAt("Notify smart selected"))
            {
                HashSet<PlayerAIBot> selectedBots = MainSelection.GetSelected<PlayerAIBot>();
                foreach (PlayerAIBot selectedBot in selectedBots)
                {
                    ZiMain.sendChatMessage("Nevermind.", selectedBot.Agent, zStaticRefrences.LocalPlayer);
                }
            }
            MainSelection.Deselect<PlayerAIBot>();
        }
        private static uint GetVoiceId(PlayerAIBot bot)
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
        private static bool SelectBotInView()
        {
            bool facingUp = Vector3.Angle(zStaticRefrences.CameraTransform.forward, Vector3.up) < 15f;
            if (facingUp && MainSelection.Selected<PlayerAIBot>())
            {
                DeselectAllBots();
                return true;
            }

            PlayerAIBot bot = GetBotLookingAt();
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
        private static bool IsOfType<T>(Il2CppSystem.Type type)
        {
            Il2CppSystem.Type target = Il2CppType.Of<T>();
            return type == target || type.IsSubclassOf(target);
        }
        private static void onKeyTap()
        {
            HashSet<Il2CppSystem.Type> InteractableTypes = new()
            {
                Il2CppType.Of<PlayerAIBot>(), //bot
                //Il2CppType.Of<ItemInLevel>(), // item - pickup
                Il2CppType.Of<SentryGunInstance>(), // turret - refill/pickup
                //Il2CppType.Of<LG_WeakResourceContainer>(), // container - open/unlock
                Il2CppType.Of<LG_WeakDoor>(), // door - open/throw cfoam
                //Il2CppType.Of<EnemyAgent>(), // enemy - attack / Big enemy - attack countdown
                //Il2CppType.Of<LG_PowerGenerator_Core>(), // Generator - Place Cell
            };
            Component BestComponent = zSearch.FindBestInView(zStaticRefrences.CameraTransform, InteractableTypes, MaxAngle: 30f);
            Il2CppSystem.Type type = BestComponent?.GetIl2CppType();
            if (type == null)
            {
                SelectBotInView();
                return;
            }
            if (IsOfType<PlayerAIBot>(type))
            {
                SelectBotInView();
            }
            else if (IsOfType<SentryGunInstance>(type))
            {
                SentryGunInstance sentry = BestComponent.Cast<SentryGunInstance>();
                PlayerAIBot bot = sentry?.Owner?.GetComponent<PlayerAIBot>();
                if (bot != null)
                {
                    PlayerVoiceManager.WantToSay(zStaticRefrences.LocalPlayer.CharacterID, AK.EVENTS.PLAY_CL_PICKUPYOURDEPLOYABLES);
                    zStaticRefrences.Subtitles.ShowSingleLineSubtitle($"Pick up your deployables.", 1);
                    zBotActions.SendBotToPickUpSentry(bot, zStaticRefrences.LocalPlayer);
                }

            }
            else if (IsOfType<LG_WeakDoor>(type))
            {
                // TODO open / close door
            }
            else
            {
                SelectBotInView();
            }
        }
        private static void onKeyHeld()
        {
            if (!MainSelection.Selected<PlayerAIBot>())
                return;
            HashSet<Il2CppSystem.Type> InteractableTypes = new()
            {
                Il2CppType.Of<PlayerAgent>(),
                Il2CppType.Of<ItemInLevel>(),
                Il2CppType.Of<SentryGunInstance>(),
                Il2CppType.Of<LG_WeakResourceContainer>(),
                Il2CppType.Of<LG_WeakDoor>(),
                Il2CppType.Of<EnemyAgent>(),
                Il2CppType.Of<LG_PowerGenerator_Core>(),
            };
            Component BestComponent = zSearch.FindBestInView(zStaticRefrences.CameraTransform, InteractableTypes, MaxAngle: 30f);
            Il2CppSystem.Type type = BestComponent?.GetIl2CppType();
            if (type == null)
            {
                HoldPressNothing(zStaticRefrences.CameraTransform);
                return;
            }
            else if (IsOfType<PlayerAgent>(type))
            {
                PlayerAgent agent = BestComponent.Cast<PlayerAgent>();
                HoldPressPlayerAgent(agent);
                return;
            }
            else if (IsOfType<ItemInLevel>(type))
            {
                ItemInLevel item = BestComponent.Cast<ItemInLevel>();
                HoldPressItemInLevel(item);
                return;
            }
            else if (IsOfType<SentryGunInstance>(type))
            {
                SentryGunInstance sentry = BestComponent.Cast<SentryGunInstance>();
                HoldPressSentryGrun(sentry);
                return;
            }
            else if (IsOfType<LG_WeakResourceContainer>(type))
            {
                LG_WeakResourceContainer container = BestComponent.Cast<LG_WeakResourceContainer>();
                HoldPressContainer(container);
                return;
            }
            else if (IsOfType<LG_WeakDoor>(type))
            {
                LG_WeakDoor door = BestComponent.Cast<LG_WeakDoor>();
                HoldPressDoor(door);
                return;
            }
            else if (IsOfType<EnemyAgent>(type))
            {
                EnemyAgent enemy = BestComponent.Cast<EnemyAgent>();
                HoldPressEnemy(enemy);
                return;
            }
            else if (IsOfType<LG_PowerGenerator_Core>(type))
            {
                LG_PowerGenerator_Core generator = BestComponent.Cast<LG_PowerGenerator_Core>();
                HoldPressGenerator(generator);
                return;
            }
            ZiMain.PlayUiSound(InvalidSound);
        }
        public static void onKeyDoubleTap()
        {
            HashSet<Il2CppSystem.Type> InteractableTypes = new()
            {
                Il2CppType.Of<PlayerAgent>(),
                //Il2CppType.Of<ItemInLevel>(),
                Il2CppType.Of<SentryGunInstance>(),
                Il2CppType.Of<LG_WeakResourceContainer>(),
                Il2CppType.Of<LG_WeakDoor>(),
                Il2CppType.Of<EnemyAgent>(),
                //Il2CppType.Of<LG_PowerGenerator_Core>(),
            };
            Component BestComponent = zSearch.FindBestInView(zStaticRefrences.CameraTransform, InteractableTypes, MaxAngle: 30f);
            Il2CppSystem.Type type = BestComponent?.GetIl2CppType();
            PlayerAIBot bestbot = MainSelection.GetSelected<PlayerAIBot>().FirstOrDefault();
            if (type == null)
            {
                if (TurretHandler.TryPlaceTurret())
                    return;
            }
            else if (IsOfType<PlayerAgent>(type))
            {
                PlayerAgent Agent = BestComponent.Cast<PlayerAgent>();
                PlayerAIBot bot = Agent.GetComponent<PlayerAIBot>();
                zUpdater.Instance.StartCoroutine(CallBotToFollow(bot));
                return;
            }
            else if (IsOfType<SentryGunInstance>(type))
            {
                SentryGunInstance Sentry = BestComponent.Cast<SentryGunInstance>();
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
                    return;
                }
            }
            else if (IsOfType<LG_WeakResourceContainer>(type))
            {
                LG_WeakResourceContainer Container = BestComponent.Cast<LG_WeakResourceContainer>();
                PlayerAIBot BestBot = MainSelection.GetSelected<PlayerAIBot>().FirstOrDefault();
                if (BestBot != null)
                {
                    ZiMain.sendChatMessage("I would have tried to place my item in the container, but I'm stupid!", BestBot.Agent, zStaticRefrences.LocalPlayer);
                    return;
                }
            }
            else if (IsOfType<LG_WeakDoor>(type))
            {
                LG_WeakDoor Door = BestComponent.Cast<LG_WeakDoor>();
                PlayerAIBot BestBot = MainSelection.GetSelected<PlayerAIBot>().FirstOrDefault();
                if (BestBot != null)
                {
                    ZiMain.sendChatMessage("I would have tried to break the door, but I might be stupid!", BestBot.Agent, zStaticRefrences.LocalPlayer);
                    return;
                }
            }
            else if (IsOfType<EnemyAgent>(type))
            {
                EnemyAgent Door = BestComponent.Cast<EnemyAgent>();
                PlayerAIBot BestBot = MainSelection.GetSelected<PlayerAIBot>().FirstOrDefault();
                if (BestBot != null)
                {
                    PlayerVoiceManager.WantToSay(BestBot.Agent.CharacterID, AK.EVENTS.PLAY_CL_THREETWOONEGO);
                    ZiMain.sendChatMessage("I would have attacked the enemy at the end of the countdown, but I'm stupid!", BestBot.Agent, zStaticRefrences.LocalPlayer);
                    return;
                }
            }
            var Bot = GetBotLookingAt();
            if (Bot != null)
            {
                zUpdater.Instance.StartCoroutine(CallBotToFollow(Bot));
                return;
            }
            ZiMain.PlayUiSound(InvalidSound);
        }
        private static void OnTapAndHold()
        {
            PlayerAIBot BestBot = MainSelection.GetSelected<PlayerAIBot>().FirstOrDefault();
            if (BestBot != null)
            {
                ZiMain.sendChatMessage("I would have moved to that locataion, but I'm stupid!", BestBot.Agent, zStaticRefrences.LocalPlayer);
                return;
            }
            ZiMain.PlayUiSound(InvalidSound);
        }
        public static void HoldPressNothing(Transform transform)
        {
            if (Vector3.Angle(transform.forward, Vector3.down) < 15f) // are we looking down?  if so, consider us interacting with our player agent.
            {
                HoldPressPlayerAgent(zStaticRefrences.LocalPlayer);
                return;
            }
            // try to have them throw/place their consumable!
            ZiMain.PlayUiSound(InvalidSound);
        }
        public static bool HoldPressPlayerAgent(PlayerAgent Agent)
        {
            bool sucsess = false;
            if (Agent.Alive)
            {
                float offset = 0;
                foreach (PlayerAIBot selectedBot in MainSelection.GetSelected<PlayerAIBot>())
                {
                    uint resourcePackID = GetAgentResoucePack(zStaticRefrences.LocalPlayer);
                    bool needsResourceIhave = false;
                    switch (resourcePackID)
                    {
                        case (uint)ShareActionPatch.ResourceIDs.MediPack:
                            needsResourceIhave = Agent.NeedHealth();
                            continue;
                        case (uint)ShareActionPatch.ResourceIDs.AmmoPack:
                            needsResourceIhave = Agent.NeedWeaponAmmo();
                            continue;
                        case (uint)ShareActionPatch.ResourceIDs.ToolPack:
                            needsResourceIhave = Agent.NeedToolAmmo();
                            continue;
                        case (uint)ShareActionPatch.ResourceIDs.DisinfectPack:
                            needsResourceIhave = Agent.NeedDisinfection();
                            continue;
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
        public static void HoldPressItemInLevel(ItemInLevel item)
        {
            PlayerAIBot BestBot = MainSelection.GetSelected<PlayerAIBot>().FirstOrDefault();
            zBotActions.SendBotToPickupItem(BestBot, item, zStaticRefrences.LocalPlayer);
        }
        public static void HoldPressSentryGrun(SentryGunInstance sentry)
        {
            HashSet<PlayerAIBot> selection = MainSelection.GetSelected<PlayerAIBot>();
            foreach (PlayerAIBot bot in selection)
            {
                // do you have tool resources to share?
                // are you the owner of the sentry?
                bool owned = sentry.Owner == bot.Agent;
                bool haveTool = (GetAgentResoucePack(bot.Agent) == (uint)ShareActionPatch.ResourceIDs.ToolPack);
                if (haveTool)
                {
                    ZiMain.sendChatMessage($"I would have refilled the sentry, but I'm stupid.", bot.Agent, zStaticRefrences.LocalPlayer);
                    // TODO send them to refill the sentry
                    // Seems like this is not a vanilla behavior I can hook into.
                    // This will have to wait untill I attempt custom actions again.
                }
            }
        }
        public static void HoldPressContainer(LG_WeakResourceContainer container)
        {
            var BestBot = MainSelection.GetSelected<PlayerAIBot>().First();
            ZiMain.sendChatMessage("I would have opend up the container, but I'm stupid!", BestBot.Agent, zStaticRefrences.LocalPlayer);
        }
        public static void HoldPressDoor(LG_WeakDoor door)
        {
            var BestBot = MainSelection.GetSelected<PlayerAIBot>().First();
            ZiMain.sendChatMessage("I would have opend or closed the door, but I'm stupid!", BestBot.Agent, zStaticRefrences.LocalPlayer);
        }
        public static void HoldPressEnemy(EnemyAgent enemy)
        {
            var BestBot = MainSelection.GetSelected<PlayerAIBot>().First();
            ZiMain.sendChatMessage("I would have attacked the enemy, but I'm stupid!", BestBot.Agent, zStaticRefrences.LocalPlayer);
        }
        public static void HoldPressGenerator(LG_PowerGenerator_Core generator)
        {
            var BestBot = MainSelection.GetSelected<PlayerAIBot>().First();
            ZiMain.sendChatMessage("I would have tried to incert a cell, but I'm stupid!", BestBot.Agent, zStaticRefrences.LocalPlayer);
        }
        public static uint GetAgentResoucePack(PlayerAgent agent)
        {
            PlayerBackpack backpack = PlayerBackpackManager.GetBackpack(agent.Owner);
            if (backpack.TryGetBackpackItem(InventorySlot.ResourcePack, out BackpackItem backpackItem))
                return backpackItem.ItemID;
            return 0;
        }
        public static IEnumerator CallBotToFollow(PlayerAIBot Bot)
        {
            ZiMain.sendChatMessage($"On the way.", Bot.Agent, zStaticRefrences.LocalPlayer);
            uint voidID = GetVoiceId(Bot);
            string botname = Bot.Agent.PlayerName;
            zStaticRefrences.Subtitles.ShowSingleLineSubtitle($"Hey {botname}, Follow me!", 2);
            PlayerVoiceManager.WantToSay(zStaticRefrences.LocalPlayer.CharacterID, voidID);
            PlayerVoiceManager.WantToSay(zStaticRefrences.LocalPlayer.CharacterID, AK.EVENTS.PLAY_CL_FOLLOWME);
            yield return new WaitForSeconds(1f);
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

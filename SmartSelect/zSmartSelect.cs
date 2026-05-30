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
        //This class handles everything with the smart select button (V)
        public static Selection MainSelection = new();
        private static bool IsSetUp = false;
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
                return true ;
            }

            PlayerAIBot bot = GetBotLookingAt();
            if (bot == null)
                return false ;
            MainSelection.Select(bot);
            var Agent = bot.Agent;
            var botName = Agent.PlayerName;
            var botId = Agent.CharacterID;
            var voiceID = GetVoiceId(bot);

            PlayerVoiceManager.WantToSay(zStaticRefrences.LocalPlayer.CharacterID, voiceID);
            zStaticRefrences.Subtitles.ShowSingleLineSubtitle($"Hey {botName}!", 1);
            BotBarkBack(botId, AK.EVENTS.PLAY_CL_YES, "Yes?");
            if ((bool)zSlideComputer.ActionPermissions.ValueAt("Notify smart selected"))
                ZiMain.sendChatMessage("I'm ready", Agent, zStaticRefrences.LocalPlayer);
            return true;
        }
        public static void BotBarkBack(int botId, uint voiceID, string subtitle, float delay = 1f)
        {
            FlexibleMethodDefinition barkback = new FlexibleMethodDefinition(BotBark, [botId, voiceID, subtitle]);
            zUpdater.InvokeStatic(barkback, delay);
            
        }
        internal static void BotBark(int botId, uint voiceID, string subtitle = "")
        {
            PlayerVoiceManager.WantToSay(botId, voiceID);
            if (subtitle != "")
                zStaticRefrences.Subtitles.ShowSingleLineSubtitle(subtitle, 1);
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
                //Il2CppType.Of<LG_WeakDoor>(), // door - open/throw cfoam
                //Il2CppType.Of<EnemyAgent>(), // enemy - attack / Big enemy - attack countdown
                //Il2CppType.Of<LG_PowerGenerator_Core>(), // Generator - Place Cell
            };
            Component BestComponent = zSearch.FindBestInView(zStaticRefrences.CameraTransform, InteractableTypes, MaxAngle: 30f);
            if (BestComponent == null)
            {
                SelectBotInView();
                return;
            }
            Il2CppSystem.Type type = BestComponent?.GetIl2CppType();
            if (IsOfType<SentryGunInstance>(type))
            {
                SentryGunInstance sentry = BestComponent.Cast<SentryGunInstance>();
                PlayerAIBot bot = sentry?.Owner?.GetComponent<PlayerAIBot>();
                if (bot != null)
                    SendBotToPickUpSentry(bot);
            }
            else if (IsOfType<PlayerAIBot>(type))
            {
                //PlayerAIBot bot = BestComponent.Cast<PlayerAIBot>();
                //MainSelection.Select(bot);
                SelectBotInView();
            }
            else
            {
                SelectBotInView();
            }
        }

        private static void OnTapAndHold()
        {
            if (TurretHandler.TryPlaceTurret())
                return;

            ZiMain.PlayUiSound(AK.EVENTS.MENU_HOST_EXPEDITION_BUTTON_RELEASE);
        }
        private static void onKeyHeld()
        {
            // figure out what we are looking at, if anything, do a context action with it.
            // could be looking at:
            // playeragent  (share resource item)   PlayerAgent
            // item         (pickup)                ItemInLevel
            // turret       (refill/pickup)         SentryGunInstance
            // container    (open/unlock)           LG_WeakResourceContainer
            // floor        (place sentry, throw)   ?BoxCollider?
            // wall         (place mine)            ?BoxCollider?
            // door         (open/throw cfoam)      LG_WeakDoor
            // enemy        (attack)                EnemyAgent
            // big enemy    (attack countdown)      EnemyAgent?
            //              use voiceline PLAY_CL_THREETWOONEGO
            // Generator    (Place cell)            LG_PowerGenerator_Core 
            if (!MainSelection.Selected<PlayerAIBot>())
            {
                return;
                ZiMain.PlayUiSound(AK.EVENTS.MENU_HOST_EXPEDITION_BUTTON_RELEASE);
            }
                

            HashSet<Il2CppSystem.Type> InteractableTypes = new()
            {
                Il2CppType.Of<PlayerAgent>(), //bot/player - share resource item
                Il2CppType.Of<ItemInLevel>(), // item - pickup
                Il2CppType.Of<SentryGunInstance>(), // turret - refill/pickup
                Il2CppType.Of<LG_WeakResourceContainer>(), // container - open/unlock
                Il2CppType.Of<LG_WeakDoor>(), // door - open/throw cfoam
                Il2CppType.Of<EnemyAgent>(), // enemy - attack / Big enemy - attack countdown
                Il2CppType.Of<LG_PowerGenerator_Core>(), // Generator - Place Cell
            };
            Component BestComponent = zSearch.FindBestInView(zStaticRefrences.CameraTransform, InteractableTypes, MaxAngle: 30f);
            Il2CppSystem.Type type = BestComponent?.GetIl2CppType();
            if (type == null)
            {
                InteractWithNothing(zStaticRefrences.CameraTransform);
                return;
            }
            else if (IsOfType<PlayerAgent>(type))
            {
                PlayerAgent agent = BestComponent.Cast<PlayerAgent>();
                InteractWithPlayerAgent(agent);
                return;
            }
            else if (IsOfType<ItemInLevel>(type))
            {
                ItemInLevel item = BestComponent.Cast<ItemInLevel>();
                InteractWithItemInLevel(item);
                return;
            }
            else if (IsOfType<SentryGunInstance>(type))
            {
                SentryGunInstance sentry = BestComponent.Cast<SentryGunInstance>();
                RefillSentryGrun(sentry);
                return;
            }
            else if (IsOfType<LG_WeakResourceContainer>(type))
            {
                LG_WeakResourceContainer container = BestComponent.Cast<LG_WeakResourceContainer>();
                InteractWithContainer(container);
                return;
            }
            else if (IsOfType<LG_WeakDoor>(type))
            {
                LG_WeakDoor door = BestComponent.Cast<LG_WeakDoor>();
                InteractWithDoor(door);
                return;
            }
            else if (IsOfType<EnemyAgent>(type))
            {
                EnemyAgent enemy = BestComponent.Cast<EnemyAgent>();
                InteractWithEnemy(enemy);
                return;
            }
            else if (IsOfType<LG_PowerGenerator_Core>(type))
            {
                LG_PowerGenerator_Core generator = BestComponent.Cast<LG_PowerGenerator_Core>();
                InteractWithGenerator(generator);
                return;
            }
            ZiMain.PlayUiSound(AK.EVENTS.MENU_HOST_EXPEDITION_BUTTON_RELEASE);
        }
        public static void InteractWithNothing(Transform transform)
        {
            if (Vector3.Angle(transform.forward, Vector3.down) < 15f) // are we looking down?  if so, consider us interacting with our player agent.
            {
                InteractWithPlayerAgent(zStaticRefrences.LocalPlayer);
                return;
            }
            ZiMain.PlayUiSound(AK.EVENTS.MENU_HOST_EXPEDITION_BUTTON_RELEASE);
        }
        public static bool InteractWithPlayerAgent(PlayerAgent Agent)
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
                    BotBarkBack(selectedBot.Agent.CharacterID, AK.EVENTS.PLAY_CL_WILLDO, "Will Do.", 1f + offset);
                    offset += 0.25f;
                }
            }
            else
            {
                PlayerAIBot BestBot = MainSelection.GetSelected<PlayerAIBot>().First();
                ZiMain.sendChatMessage($"I would have revived {Agent.PlayerName}, but I'm stupid.", BestBot.Agent, zStaticRefrences.LocalPlayer);
            }
            if (sucsess)
            {
                PlayerVoiceManager.WantToSay(zStaticRefrences.LocalPlayer.CharacterID, AK.EVENTS.PLAY_CL_PLEASE);
                zStaticRefrences.Subtitles.ShowSingleLineSubtitle($"Please", 1);
            }
            else
            {
                ZiMain.PlayUiSound(AK.EVENTS.MENU_HOST_EXPEDITION_BUTTON_RELEASE);
            }
                
            return sucsess;
        }
        public static void InteractWithItemInLevel(ItemInLevel item)
        {
            PlayerAIBot BestBot = MainSelection.GetSelected<PlayerAIBot>().FirstOrDefault();
            PlayerVoiceManager.WantToSay(zStaticRefrences.LocalPlayer.CharacterID, AK.EVENTS.PLAY_CL_PLEASE);
            zStaticRefrences.Subtitles.ShowSingleLineSubtitle($"Please", 1);
            zBotActions.SendBotToPickupItem(BestBot, item, zStaticRefrences.LocalPlayer);
            BotBarkBack(BestBot.Agent.CharacterID, AK.EVENTS.PLAY_CL_WILLDO, "Will Do.");
        }
        public static void RefillSentryGrun(SentryGunInstance sentry)
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
        public static void SendBotToPickUpSentry(PlayerAIBot bot)
        {
            PlayerVoiceManager.WantToSay(zStaticRefrences.LocalPlayer.CharacterID, AK.EVENTS.PLAY_CL_PICKUPYOURDEPLOYABLES);
            zStaticRefrences.Subtitles.ShowSingleLineSubtitle($"Pick up your deployables.", 1);
            zBotActions.SendBotToPickUpSentry(bot);
            BotBarkBack(bot.Agent.CharacterID, AK.EVENTS.PLAY_CL_WILLDO, "Will Do.", 2f);
        }
        public static void InteractWithContainer(LG_WeakResourceContainer container)
        {

        }
        public static void InteractWithDoor(LG_WeakDoor door)
        {

        }
        public static void InteractWithEnemy(EnemyAgent enemy)
        {

        }
        public static void InteractWithGenerator(LG_PowerGenerator_Core generator)
        {

        }
        public static uint GetAgentResoucePack(PlayerAgent agent)
        {
            PlayerBackpack backpack = PlayerBackpackManager.GetBackpack(agent.Owner);
            if (backpack.TryGetBackpackItem(InventorySlot.ResourcePack, out BackpackItem backpackItem))
                return backpackItem.ItemID;
            return 0;
        }
        public static void onKeyDoubleTap()
        {
            var Bot = GetBotLookingAt();
            if (Bot == null)
                return;
            
            zUpdater.Instance.StartCoroutine(CallBotToFollow(Bot));
            ZiMain.sendChatMessage($"On the way.", Bot.Agent, zStaticRefrences.LocalPlayer);
        }

        public static IEnumerator CallBotToFollow(PlayerAIBot Bot)
        {
            uint voidID = GetVoiceId(Bot);
            string botname = Bot.Agent.PlayerName;
            zStaticRefrences.Subtitles.ShowSingleLineSubtitle($"Hey {botname}, Follow me!", 2);
            PlayerVoiceManager.WantToSay(zStaticRefrences.LocalPlayer.CharacterID, voidID);
            yield return new WaitForSeconds(1f);
            zStaticRefrences.CommsMenu.ExecuteCmdCall(zStaticRefrences.LocalPlayer, Bot.GetComponent<PlayerAgent>());
            PlayerVoiceManager.WantToSay(zStaticRefrences.LocalPlayer.CharacterID, AK.EVENTS.PLAY_CL_FOLLOWME);

            //yield return new WaitForSeconds(1f);

            //PlayerVoiceManager.WantToSay(Bot.Agent.CharacterID, AK.EVENTS.PLAY_CL_IMONMYWAY);
            
        }
    }
}

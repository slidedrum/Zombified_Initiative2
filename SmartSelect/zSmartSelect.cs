using Agents;
using BotControl.Patches;
using Enemies;
using Il2CppInterop.Runtime;
using LevelGeneration;
using Player;
using PlayFab.AdminModels;
using SlideDrum.sInputSystem;
using SlideMenu;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static UnityEngine.UI.GridLayoutGroup;

namespace BotControl.SmartSelect
{
    public static class zSmartSelect
    {
        //This class handles everything with the smart select button (V)
        private static Selection MainSelection = new();
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
            uint GetVoiceId(string botName)
            {

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
            var voiceID = GetVoiceId(botName);

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
        private static Vector3 FlatForward(Transform transform)
        {
            Vector3 dir = transform.forward;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
                return Vector3.forward;
            return dir.normalized;
        }
        private static void OnTapAndHold()
        {
            var selection = MainSelection.GetSelected<PlayerAIBot>();
            foreach (var bot in selection)
            {
                var backpack = bot.Backpack;
                if (!backpack.TryGetBackpackItem(InventorySlot.GearClass, out BackpackItem backpackItem))
                    continue;
                bool isSentry = backpackItem.Instance.ArchetypeName == "Sentry Gun";
                bool isDeployed = backpackItem.Status == eInventoryItemStatus.Deployed;
                if (!isSentry || isDeployed)
                    continue;
  
                //raycast from camera to find hit position and normal,
                //place sentry at hit position, oriented based on normal.
                Vector3 origin = zStaticRefrences.CameraTransform.position;
                Vector3 direction = zStaticRefrences.CameraTransform.forward;
                if (!Physics.Raycast(origin, direction, out RaycastHit hit, 100f))
                    continue;
                Vector3 placePosition = hit.point;
                Quaternion placeRotation = Quaternion.LookRotation(FlatForward(zStaticRefrences.CameraTransform));
                Pose sentryPose = new Pose(placePosition, placeRotation);
                if (!CanPlaceTurret(sentryPose))
                    continue;
                zBotActions.SendBotToPlaceSentry(bot, sentryPose, zStaticRefrences.LocalPlayer);
                PlayerVoiceManager.WantToSay(zStaticRefrences.LocalPlayer.CharacterID, AK.EVENTS.PLAY_CL_PUTASENTRYGUNHERE);
                zStaticRefrences.Subtitles.ShowSingleLineSubtitle($"Put a sentry here.", 1);
                BotBarkBack(bot.Agent.CharacterID, AK.EVENTS.PLAY_CL_WILLDO, "Will Do.", 2f);
                break;
            }
        }
        public static bool CanPlaceTurret(Pose pose)
        {
            bool hasRayHit = false;
            Vector3 origin = pose.position + Vector3.up * 0.1f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 3f, LayerManager.MASK_SENTRYGUN_CAMERARAY_MOVERHELPER))
            {
                float angle = Vector3.Dot(hit.normal, Vector3.up);
                if (angle > 0.9f)
                {
                    hasRayHit = true;
                }
                else if (angle > 0.7f && Physics.Raycast(origin, Vector3.down, out RaycastHit hit2, 3f, LayerManager.MASK_SENTRYGUN_CAMERARAY))
                {
                    hasRayHit = true;
                }
            }
            if (!hasRayHit)
                return false;
            Bounds localBounds = new Bounds();

            for (int i = 0; i < zStaticRefrences.SentryRaycastCorners.Length; i++)
            {
                Vector3 local = zStaticRefrences.SentryRaycastCorners[i].localPosition;
                localBounds.Encapsulate(local);
            }
            Vector3 halfExtents = localBounds.size * 0.5f;

            Collider[] hits = Physics.OverlapBox(
                pose.position,
                halfExtents,
                pose.rotation,
                LayerManager.MASK_SENTRYGUN_CAMERARAY_MOVERHELPER
            );

            return hits.Length == 0;
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
                return;

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
            }
            else if (IsOfType<PlayerAgent>(type))
            {
                PlayerAgent agent = BestComponent.Cast<PlayerAgent>();
                InteractWithPlayerAgent(agent);
            }
            else if (IsOfType<ItemInLevel>(type))
            {
                ItemInLevel item = BestComponent.Cast<ItemInLevel>();
                InteractWithItemInLevel(item);
            }
            else if (IsOfType<SentryGunInstance>(type))
            {
                SentryGunInstance sentry = BestComponent.Cast<SentryGunInstance>();
                RefillSentryGrun(sentry);
            }
            else if (IsOfType<LG_WeakResourceContainer>(type))
            {
                LG_WeakResourceContainer container = BestComponent.Cast<LG_WeakResourceContainer>();
                InteractWithContainer(container);
            }
            else if (IsOfType<LG_WeakDoor>(type))
            {
                LG_WeakDoor door = BestComponent.Cast<LG_WeakDoor>();
                InteractWithDoor(door);
            }
            else if (IsOfType<EnemyAgent>(type))
            {
                EnemyAgent enemy = BestComponent.Cast<EnemyAgent>();
                InteractWithEnemy(enemy);
            }
            else if (IsOfType<LG_PowerGenerator_Core>(type))
            {
                LG_PowerGenerator_Core generator = BestComponent.Cast<LG_PowerGenerator_Core>();
                InteractWithGenerator(generator);
            }
        }
        public static void InteractWithNothing(Transform transform)
        {
            if (Vector3.Angle(transform.forward, Vector3.down) < 15f) // are we looking down?  if so, consider us interacting with our player agent.
            {
                InteractWithPlayerAgent(zStaticRefrences.LocalPlayer);
            }
        }
        public static void InteractWithPlayerAgent(PlayerAgent Agent)
        {
            if (Agent.Alive)
            {
                PlayerVoiceManager.WantToSay(zStaticRefrences.LocalPlayer.CharacterID, AK.EVENTS.PLAY_CL_PLEASE);
                zStaticRefrences.Subtitles.ShowSingleLineSubtitle($"Please", 1);
                float offset = 0;
                foreach (PlayerAIBot selectedBot in MainSelection.GetSelected<PlayerAIBot>())
                {
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
                bool haveTool = (GetAgentResoucePack(bot) == (uint)ShareActionPatch.ResourceIDs.ToolPack);
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
        public static uint GetAgentResoucePack(PlayerAIBot bot)
        {
            PlayerBackpack backpack = PlayerBackpackManager.GetBackpack(bot.Agent.Owner);
            if (backpack.TryGetBackpackItem(InventorySlot.ResourcePack, out BackpackItem backpackItem))
                return backpackItem.ItemID;
            return 0;
        }
        public static void onKeyDoubleTap()
        {
            //var Bot = selection.getBotGobject();
            //if (Bot == null)
            //    return;
            //var localPlayer = PlayerManager.GetLocalPlayerAgent();
            //PlayerVoiceManager.WantToSay(localPlayer.CharacterID, AK.EVENTS.PLAY_CL_FOLLOWME);
            //zStaticRefrences.Subtitles.ShowSingleLineSubtitle($"Follow me!", 1);
            //zStaticRefrences.CommsMenu.ExecuteCmdCall(localPlayer, Bot.GetComponent<PlayerAgent>());
        }

    }
}

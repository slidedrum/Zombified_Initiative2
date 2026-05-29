using Enemies;
using LevelGeneration;
using Player;
using SlideMenu;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SlideDrum.sInputSystem;
using System;

namespace BotControl.SmartSelect
{
    public static class zSmartSelect
    {
        //This class handles everything with the smart select button (V)


        //public static float interactionHeldStart = Time.time;
        //public static bool interactionHeld = false;
        //public static bool interactable = true; //Do I need this anymore?
        //public static float heldDuration = 0f;
        //public static KeyCode key = KeyCode.V;
        public static Selection selection = new();
        private const float vertHeadOffset = 1.75f;
        private static bool IsSetUp = false;
        private static HashSet<Type> _types;
        public static HashSet<Il2CppSystem.Type> Types
        {
            get
            {
                if (_types != null)
                    return _types;

                _types = new HashSet<Type>();

                var baseType = typeof(zSelectableObject);

                foreach (var type in AppDomain.CurrentDomain.GetAssemblies()
                             .SelectMany(a => a.GetTypes()))
                {
                    if (type.IsAbstract)
                        continue;

                    if (baseType.IsAssignableFrom(type))
                    {
                        _types.Add(type);
                    }
                }
                return _types;
            }
        }
        private static List<zSelectableObject> SelectedObjects;
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
            sInputSystem.AddListener(sInputSystemDefaults.OnHoldImmediate, new FlexibleMethodDefinition(onKeyHeld), KeyCode.V);
            sInputSystem.AddListener(sInputSystemDefaults.OnDoubleTapped, new FlexibleMethodDefinition(onKeyDoubleTap), KeyCode.V);
            IsSetUp = true;
        }
        public static void onKeyHeld()
        {
            if (selection.getBotGobject() == null)
                return;
            var localPlayer = PlayerManager.GetLocalPlayerAgent();
            var cameraTransform = localPlayer.FPSCamera.transform;
            lookingObject lookingAt = GetFilteredObjectLookingAt();
            switch (lookingAt.type)
            {
                case objectType.None:
                    {
                        if (Vector3.Angle(cameraTransform.forward, Vector3.down) < 15f)
                        {
                            //this might not be needed at all? Since your game object is below you, so it triggers agentImLookingAt without having to do anything special.
                            PlayerAgent receiver = PlayerManager.GetLocalPlayerAgent();
                            ZiMain.log.LogInfo($"Looking at new self {receiver.PlayerName}");
                            GameObject selectedBotObject = selection.getBotGobject();
                            PlayerAIBot selectedBot = selectedBotObject.GetComponent<PlayerAIBot>();;
                            ZiMain.SendBotToShareResourcePack(selectedBot, receiver);
                        }
                        else
                        {
                            ZiMain.log.LogInfo("Looking at nothing");
                        }
                        break;
                    }
                case objectType.PlayerAgent:
                    {
                        //todo refactor this to be more consistant and easy to read
                        ZiMain.log.LogInfo($"Looking at new agent {lookingAt.gobject.name}");
                        GameObject selectedBotObject = selection.getBotGobject();
                        PlayerAgent receiver = lookingAt.gobject.GetComponent<PlayerAgent>();
                        PlayerAIBot selectedBot = selectedBotObject.GetComponent<PlayerAIBot>();
                        ZiMain.SendBotToShareResourcePack(selectedBot, receiver,localPlayer);
                        break;
                    }
                case objectType.EnemyAgent:
                    {
                        ZiMain.log.LogInfo($"Looking at new enemy {lookingAt.gobject.name}");
                        GameObject selectedBotObject = selection.getBotGobject();
                        PlayerAgent bot = selectedBotObject.GetComponent<PlayerAgent>();
                        PlayerAIBot aiBot = selectedBotObject.GetComponent<PlayerAIBot>();
                        ZiMain.SendBotToKillEnemy(aiBot, lookingAt.gobject.GetComponent<EnemyAgent>(),localPlayer);
                        break;
                    }
                case objectType.Item:
                    {
                        ZiMain.log.LogInfo($"Looking at new item {lookingAt.gobject.name}");
                        GameObject selectedBotObject = selection.getBotGobject();
                        PlayerAgent agent = selectedBotObject.GetComponent<PlayerAgent>();
                        PlayerAIBot aiBot = selectedBotObject.GetComponent<PlayerAIBot>();
                        ItemInLevel pickup = lookingAt.gobject.GetComponent<ItemInLevel>();
                        ZiMain.SendBotToPickupItem(aiBot, pickup,localPlayer);
                        break;
                    }
                case objectType.Other:
                    { 
                        ZiMain.log.LogWarning($"Looking at something weird {lookingAt.gobject.name} at {lookingAt.gobject.transform.position}");
                        break; 
                    }
                default:
                    {
                        ZiMain.log.LogError($"Looking at something VERY weird {lookingAt.gobject.name} at {lookingAt.gobject.transform.position}");
                        break; 
                    }
            }
        }
        public static void onKeyDoubleTap()
        {
            var Bot = selection.getBotGobject();
            if (Bot == null)
                return;
            var localPlayer = PlayerManager.GetLocalPlayerAgent();
            PlayerVoiceManager.WantToSay(localPlayer.CharacterID, AK.EVENTS.PLAY_CL_FOLLOWME);
            zStaticRefrences.Subtitles.ShowSingleLineSubtitle($"Follow me!", 1);
            zStaticRefrences.CommsMenu.ExecuteCmdCall(localPlayer, Bot.GetComponent<PlayerAgent>());
        }
        public static void onKeyTap()
        {
            var localPlayer = PlayerManager.GetLocalPlayerAgent();
            var cameraTransform = localPlayer.FPSCamera.transform;
            lookingObject lookingAt = GetFilteredObjectLookingAt();
            if (lookingAt.type == objectType.None && Vector3.Angle(cameraTransform.forward, Vector3.up) < 15f && selection.getBotGobject() != null)
            {
                if ((bool)zSlideComputer.ActionPermissions.ValueAt("Notify smart selected"))
                    ZiMain.sendChatMessage("Nevermind.", selection.getBotGobject().GetComponent<PlayerAgent>(), localPlayer);
                selection.setBot(null);
                return;
            }
            if (lookingAt.type != objectType.PlayerAgent || lookingAt.type == objectType.None)
                return;
            PlayerAgent agent = lookingAt.gobject.GetComponent<PlayerAgent>();

            if (lookingAt.gobject != null)
            {
                var botName = agent.PlayerName;
                var botId = agent.CharacterID;
                uint voiceID = 0u;
                if (botName.ToUpper().Contains("BISHOP")) 
                {
                    voiceID = AK.EVENTS.PLAY_ADDRESSBISHOPIRRITATED01;
                }
                if (botName.ToUpper().Contains("DAUDA"))
                {
                    voiceID = AK.EVENTS.PLAY_ADDRESSDAUDAIRRITATED01;
                }
                if (botName.ToUpper().Contains("HACKET"))
                {
                    voiceID = AK.EVENTS.PLAY_ADDRESSHACKETTIRRITATED01;
                }
                if (botName.ToUpper().Contains("WOODS"))
                {
                    voiceID = AK.EVENTS.PLAY_ADDRESSWOODSIRRITATED01;
                }
                PlayerVoiceManager.WantToSay(localPlayer.CharacterID, voiceID);
                zStaticRefrences.Subtitles.ShowSingleLineSubtitle($"Hey {botName}!", 1);
                if (!agent.Owner.IsBot)
                    return;
                FlexibleMethodDefinition barkback = new FlexibleMethodDefinition(BotBarkBack, [botId]); //yes
                zUpdater.InvokeStatic(barkback, 1f);
                if ((bool)zSlideComputer.ActionPermissions.ValueAt("Notify smart selected"))
                    ZiMain.sendChatMessage("I'm ready", agent, localPlayer);
                selection.setBot(lookingAt.gobject);
            }
        }
        public static void BotBarkBack(int botId)
        {
            PlayerVoiceManager.WantToSay(botId, AK.EVENTS.PLAY_CL_YES);
            zStaticRefrences.Subtitles.ShowSingleLineSubtitle($"Yes?", 1);
        }
    }
}

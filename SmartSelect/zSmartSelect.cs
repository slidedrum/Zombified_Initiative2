using Enemies;
using LevelGeneration;
using Player;
using SlideMenu;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SlideDrum.sInputSystem;

namespace BotControl.SmartSelect
{
    //PUI_CommunicationMenu.ExecuteCmdCall(PlayerAgent, PlayerAgent) 
    // to call a bot to follow you.
    // Don't know if that's synced.
    public class Selection
    {
        //This class handles a selection instance
        

        private GameObject item = null;
        private GameObject bot = null;
        private GameObject enemy = null;
        public void setItem(GameObject newItem) { item = newItem; }
        public GameObject getItem() { 
            if (item != null && !item.activeInHierarchy)
            {
                item = null;
            }
            return item; 
        }
        public void setBot(GameObject newBot) { bot = newBot; }
        public GameObject getBotGobject() { 
            if (bot != null && !bot.activeInHierarchy)
            {
                bot = null;
            }
            return bot; 
        }
        public void setEnemy(GameObject newEnemy) { enemy = newEnemy; }
        public GameObject getEnemy() { 
            if (enemy != null && !enemy.activeInHierarchy)
            {
                enemy = null;
            }
            return enemy; 
        }
    }
    public static class zSmartSelect
    {
        //This class handles everything with the smart select button (V)
        public static PUI_CommunicationMenu _CommsMenu = null;
        public static PUI_CommunicationMenu CommsMenu { 
            get 
            { 
                if (_CommsMenu == null)
                {
                    _CommsMenu = GameObject.Find("GUI/CellUI_Camera(Clone)/PlayerLayer/MovementRoot/PUI_CommunicationMenu(Clone)").GetComponent<PUI_CommunicationMenu>();
                }
                return _CommsMenu;
            } 
        }
        public static PUI_Subtitles _Subtitles = null;
        public static PUI_Subtitles Subtitles
        {
            get
            {
                if (_Subtitles == null)
                    _Subtitles = GameObject.Find("GUI/CellUI_Camera(Clone)/PlayerLayer/MovementRoot/PUI_Subtitles_CellUI(Clone)").GetComponent<PUI_Subtitles>();
                return _Subtitles;
            }
        }

        public static float interactionHeldStart = Time.time;
        public static bool interactionHeld = false;
        //public static bool interactable = true; //Do I need this anymore?
        public static float heldDuration = 0f;
        public static KeyCode key = KeyCode.V;
        public static Selection selection = new();
        private const float vertHeadOffset = 1.75f;
        private static bool IsSetUp = false;
        public struct lookingObject
        {
            public objectType type;
            public GameObject gobject;
        }
        public enum objectType
        {
            None,
            PlayerAgent,
            EnemyAgent,
            Item,
            Other,
        }
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
        public static void DebugTrigger(string messge)
        {
            ZiMain.log.LogDebug(messge);
        }
        public static lookingObject GetFilteredObjectLookingAt()
        {
            var localPlayer = PlayerManager.GetLocalPlayerAgent();
            var cameraTransform = localPlayer.FPSCamera.transform;
            List<GameObject> agentGameObjects = PlayerManager.PlayerAgentsInLevel
                .ToArray()
                .Where(agent => agent != null)
                .Select(agent => agent.gameObject)
                .ToList();
            foreach (var agent in PlayerManager.PlayerAgentsInLevel)
            {
                if (agent != null)
                {
                    agentGameObjects.Add(agent.gameObject);
                }
            }
            float angle = 15f;
            GameObject agentImLookingAt = zSearch.GetClosestObjectInLookDirection(cameraTransform, agentGameObjects, angle,new Vector3(0f, vertHeadOffset, 0f)); //Vert offset to make selection point closer to head.
            GameObject enemyImLookingAt = zSearch.GetClosestObjectInLookDirection(cameraTransform, zSearch.GetGameObjectsWithLookDirection<EnemyAgent>(cameraTransform), angle);
            GameObject itemImLookingAt = zSearch.GetClosestObjectInLookDirection(cameraTransform, zSearch.GetGameObjectsWithLookDirection<ItemInLevel>(cameraTransform), angle);
            GameObject lookingAt = zSearch.GetClosestObjectInLookDirection(cameraTransform, [agentImLookingAt, enemyImLookingAt, itemImLookingAt]);
            lookingObject ret = new lookingObject();
            ret.gobject = lookingAt;
            switch (lookingAt)
            {
                case null:
                    ret.type = objectType.None;
                    break;
                case var go when go == agentImLookingAt:
                    ret.type = objectType.PlayerAgent;
                    break;
                case var go when go == enemyImLookingAt:
                    ret.type = objectType.EnemyAgent;
                    break;
                case var go when go == itemImLookingAt:
                    ret.type = objectType.Item;
                    break;
                default:
                    ret.type = objectType.Other;
                    break;
            }
            return ret;
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
            Subtitles.ShowSingleLineSubtitle($"Follow me!", 1);
            CommsMenu.ExecuteCmdCall(localPlayer, Bot.GetComponent<PlayerAgent>());
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
                Subtitles.ShowSingleLineSubtitle($"Hey {botName}!", 1);
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
            Subtitles.ShowSingleLineSubtitle($"Yes?", 1);
        }
    }
}

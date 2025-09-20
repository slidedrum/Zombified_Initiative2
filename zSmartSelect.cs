using Enemies;
using LevelGeneration;
using Player;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Zombified_Initiative
{
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

        public static float interactionHeldStart = Time.time;
        public static bool interactionHeld = false;
        //public static bool interactable = true; //Do I need this anymore?
        public static float heldDuration = 0f;
        public static KeyCode key = KeyCode.V;
        public static Selection selection = new();
        const float holdThreshold = 0.1f;
        private const float vertHeadOffset = 1.75f;
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
        internal static void update()
        {
            bool ready = (FocusStateManager.CurrentState == eFocusState.FPS || FocusStateManager.CurrentState == eFocusState.Dead);
            if (!ready) return;
            if (Input.GetKeyDown(key))
            {
                onKeyDown();
            }
            if (Input.GetKeyUp(key))
            {
                onKeyUp();
            }
            if (Input.GetKey(key))
            {
                onKey();
            }
        }
        public static void onKeyDown()
        {
            interactionHeldStart = Time.time;
            interactionHeld = true;
        }
        public static void onKeyUp()
        {
            if (heldDuration < holdThreshold)
                onKeyTap();
            interactionHeld = false;
        }
        public static void onKey()
        {
            heldDuration = Time.time - interactionHeldStart;
            if (heldDuration > holdThreshold && heldDuration - Time.deltaTime <= holdThreshold)
                onKeyHeld();
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
                case var go when go == agentImLookingAt:
                    ret.type = objectType.PlayerAgent;
                    break;
                case var go when go == enemyImLookingAt:
                    ret.type = objectType.EnemyAgent;
                    break;
                case var go when go == itemImLookingAt:
                    ret.type = objectType.Item;
                    break;
                case null:
                    ret.type = objectType.None;
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
                            ZiMain.SendBotToShareResourcePackNew(selectedBot, receiver);
                        }
                        else
                        {
                            ZiMain.log.LogInfo("Looking at nothing");
                        }
                        break;
                    }
                case objectType.PlayerAgent:
                    {
                        ZiMain.log.LogInfo($"Looking at new agent {lookingAt.gobject.name}");
                        GameObject selectedBotObject = selection.getBotGobject();
                        PlayerAIBot selectedBot = selectedBotObject.GetComponent<PlayerAIBot>();
                        PlayerAgent receiver = lookingAt.gobject.GetComponent<PlayerAgent>();
                        ZiMain.SendBotToShareResourcePackNew(selectedBot, receiver);
                        break;
                    }
                case objectType.EnemyAgent:
                    {
                        ZiMain.log.LogInfo($"Looking at new enemy {lookingAt.gobject.name}");
                        PlayerAgent bot = selection.getBotGobject().GetComponent<PlayerAgent>();
                        ZiMain.sendChatMessage("Attacking enemy", bot, localPlayer);
                        ZiMain.SendBotToKillEnemyOld(bot.PlayerName, lookingAt.gobject.GetComponent<EnemyAgent>());
                        break;
                    }
                case objectType.Item:
                    {
                        ZiMain.log.LogInfo($"Looking at new item {lookingAt.gobject.name}");
                        PlayerAgent bot = selection.getBotGobject().GetComponent<PlayerAgent>();
                        ItemInLevel pickup = lookingAt.gobject.GetComponent<ItemInLevel>();
                        ZiMain.sendChatMessage("Picking up item: " + pickup.PublicName, bot, localPlayer);
                        ZiMain.SendBotToPickupItemOld(bot.PlayerName, pickup);
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
        public static void onKeyTap()
        {
            var localPlayer = PlayerManager.GetLocalPlayerAgent();
            var cameraTransform = localPlayer.FPSCamera.transform;
            lookingObject lookingAt = GetFilteredObjectLookingAt();
            if (lookingAt.type == objectType.None && Vector3.Angle(cameraTransform.forward, Vector3.up) < 15f)
            {
                ZiMain.sendChatMessage("Nevermind.", selection.getBotGobject().GetComponent<PlayerAgent>(), localPlayer);
                selection.setBot(null);
                //interactable = false; //I don't think I need this anymore?
                return;
            }
            if (lookingAt.type != objectType.PlayerAgent)
                return;
            if (lookingAt.gobject != null)
            {
                ZiMain.sendChatMessage("I'm ready", lookingAt.gobject.GetComponent<PlayerAgent>(), localPlayer);
                selection.setBot(lookingAt.gobject);
                //interactable = false; //I don't think I need this anymore?
            }
        }
    }
}

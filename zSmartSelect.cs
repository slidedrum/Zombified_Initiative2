using Enemies;
using LevelGeneration;
using Player;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        public GameObject getBot() { 
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
        public static bool interactable = true;
        public static float heldDuration = 0f;
        public static KeyCode key = KeyCode.V;
        public static Selection selection = new();
        const float holdThreshold = 0.1f;

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
            {
                onKeyTap();
            }
            interactionHeld = false;
        }
        public static void onKey()
        {
            heldDuration = Time.time - interactionHeldStart;
            //if smoothTime held is larger than threshold and less than last deltatime
            if (heldDuration > holdThreshold && heldDuration - Time.deltaTime <= holdThreshold)
            {
                ZiMain.log.LogInfo("held duration: " + heldDuration);
                onKeyHeld();
            }
        }
        public static void onKeyHeld()
        {
            var localPlayer = PlayerManager.GetLocalPlayerAgent();
            var cameraTransform = localPlayer.FPSCamera.transform;
            List<GameObject> agentGameObjects = new List<GameObject>();
            foreach (var agent in PlayerManager.PlayerAgentsInLevel)
            {
                if (agent != null) // optional null check
                    agentGameObjects.Add(agent.gameObject);
            }
            GameObject agentImLookingAt = zSearch.GetClosestObjectInLookDirection(cameraTransform, agentGameObjects, 30f);
            GameObject enemyImLookingAt = zSearch.GetClosestObjectInLookDirection(cameraTransform, zSearch.GetGameObjectsWithLookDirection<EnemyAgent>(cameraTransform));
            GameObject itemImLookingAt = zSearch.GetClosestObjectInLookDirection(cameraTransform, zSearch.GetGameObjectsWithLookDirection<ItemInLevel>(cameraTransform), 180f);
            GameObject lookingAt = zSearch.GetClosestObjectInLookDirection(cameraTransform, new List<GameObject>() { agentImLookingAt, enemyImLookingAt, itemImLookingAt } );
            switch (lookingAt)
            {
                case null:
                    {
                        if (Vector3.Angle(cameraTransform.forward, Vector3.down) < 15f)
                        {
                            PlayerAgent agent = selection.getBot().GetComponent<PlayerAgent>();
                            ZiMain.SendBotToShareResourcePackOld(agent.PlayerName, lookingAt.GetComponent<PlayerAgent>());
                        }
                        else
                        {
                            ZiMain.log.LogInfo("Looking at nothing");
                        }
                        break;
                    }
                case var go when go == agentImLookingAt:
                    {
                        ZiMain.log.LogInfo($"Looking at new agent {lookingAt.name}");
                        if (selection.getBot() != null)
                        {
                            PlayerAgent agent = selection.getBot().GetComponent<PlayerAgent>();
                            ZiMain.SendBotToShareResourcePackOld(agent.PlayerName, lookingAt.GetComponent<PlayerAgent>());
                        }
                        break;
                    }
                case var go when go == enemyImLookingAt:
                    {
                        ZiMain.log.LogInfo($"Looking at new enemy {lookingAt.name}");
                        if (selection.getBot() != null)
                        {
                            PlayerAgent bot = selection.getBot().GetComponent<PlayerAgent>();
                            ZiMain.sendChatMessage("Attacking enemy", bot, localPlayer);
                            ZiMain.SendBotToKillEnemyOld(bot.PlayerName, lookingAt.GetComponent<EnemyAgent>());
                        }
                        break;
                    }
                case var go when go == itemImLookingAt:
                    {
                        ZiMain.log.LogInfo($"Looking at new item {lookingAt.name}");
                        if (selection.getBot() != null)
                        {
                            PlayerAgent bot = selection.getBot().GetComponent<PlayerAgent>();
                            ItemInLevel pickup = lookingAt.GetComponent<ItemInLevel>();
                            ZiMain.sendChatMessage("Picking up item: " + pickup.PublicName, bot, localPlayer);
                            ZiMain.SendBotToPickupItemOld(bot.PlayerName, pickup);
                        }
                        break;
                    }
                default:
                    { 
                        ZiMain.log.LogError($"Looking at something weird {lookingAt.name} at {lookingAt.transform.position}");
                        break; 
                    }
            }
        }
        public static void onKeyTap()
        {
            var localPlayer = PlayerManager.GetLocalPlayerAgent();
            var cameraTransform = localPlayer.FPSCamera.transform;
            GameObject botImLookingAt = zSearch.GetClosestObjectInLookDirection(cameraTransform, ZiMain.BotTable.Values.Select(b => b.gameObject).ToList(), 30f);
            if (botImLookingAt != null)
            {
                ZiMain.sendChatMessage("I'm ready", botImLookingAt.GetComponent<PlayerAgent>(), localPlayer);
                selection.setBot(botImLookingAt);
                interactable = false;
            }
            //if looking within 15 degrees of straight up deselect agent
            else if (Vector3.Angle(cameraTransform.forward, Vector3.up) < 15f)
            {
                ZiMain.sendChatMessage("Nevermind.", selection.getBot().GetComponent<PlayerAgent>(), localPlayer);
                selection.setBot(null);
                interactable = false;
            }
            ZiMain.log.LogInfo("looking for items...");
            var items = zSearch.GetGameObjectsWithLookDirection<ItemInLevel>(cameraTransform, 3);
            ZiMain.log.LogInfo("found " + items.Count + " items");
            if (items.Count > 0)
            {
                selection.setItem(zSearch.GetClosestObjectInLookDirection(cameraTransform, items, 180f));
                if (selection.getItem() != null)
                    ZiMain.log.LogInfo("closest item: " + selection.getItem().name);
            }
        }
    }
}

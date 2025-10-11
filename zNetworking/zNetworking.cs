using Enemies;
using LevelGeneration;
using Player;
using SNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ZombieTweak2.zMenu;
using Zombified_Initiative;

namespace ZombieTweak2.zNetworking
{
    public class zNetworking
    { //This class will handle all incoming and outgoing network requests.
        //todo only Update values every 100ms.  
        //if not host ask for host's value after change
        internal static Dictionary<int, bool> botSelections = new Dictionary<int, bool>();
        internal static long EncodeBotSelectionForNetwork(Dictionary<int, bool> botSelection)
        {
            //this method is AI generated, but I fully understand what it's doing.  I just didn't know how to do bitwise ops.
            const int MaxBots = 21; // 64 bits / 3 bits per bot
            long result = 0;
            int bitPos = 0;

            if (botSelection.Keys.Any(k => k < 0 || k >= MaxBots))
                throw new ArgumentOutOfRangeException(nameof(botSelection), $"All bot keys must be between 0 and {MaxBots - 1}.");

            var keys = botSelection.Keys.OrderBy(k => k).ToList();
            int lastKey = keys.Last();

            for (int i = 0; i < MaxBots; i++)
            {
                bool hasData = botSelection.ContainsKey(i);
                bool data = hasData && botSelection[i];
                bool end = i == lastKey;
                if (hasData) result |= (1L << bitPos);
                bitPos++;
                if (data) result |= (1L << bitPos);
                bitPos++;
                if (end) result |= (1L << bitPos);
                bitPos++;
            }

            return result;
        }
        internal static Dictionary<int, bool> DecodeBotSelectionFromNetwork(long encoded)
        {
            const int MaxBots = 21;
            var botSelection = new Dictionary<int, bool>();
            int bitPos = 0;

            for (int i = 0; i < MaxBots; i++)
            {
                bool hasData = (encoded & (1L << bitPos)) != 0;
                bitPos++;
                bool data = (encoded & (1L << bitPos)) != 0;
                bitPos++;
                bool end = (encoded & (1L << bitPos)) != 0;
                bitPos++;

                if (hasData)
                {
                    botSelection[i] = data;
                }

                if (end)
                {
                    break;
                }
            }
            return botSelection;
        }
        internal static void reciveTogglePickupPermission(ulong sender, pStructs.pBotSelections info)
        {
            ZiMain.log.LogInfo($"Recived toggle perm wiht pbotsends {info.data}");
            botSelections = DecodeBotSelectionFromNetwork(info.data);
            zSlideComputer.TogglePickupPermission(botSelections); //this works, it's downstream.
        }
        internal static void ReciveSetItemPrioDisable(ulong sender, pStructs.pItemPrioDisable info)
        {
            ZiMain.log.LogInfo($"Recived set item prio disabled from network!");
            uint id = info.id;
            bool allowed = info.allowed;
            ZiMain.log.LogInfo($"id:{id}, allowed:{allowed}");
            if (!AutomaticActionMenuClass.PickupMenuClass.prioNodesByID.ContainsKey(id))
            {
                ZiMain.log.LogError("Unknown id recived!");
                return;
            }
            zSlideComputer.SetItemPrioDisabled(id, allowed, sender);
            AutomaticActionMenuClass.PickupMenuClass.updateNodePriorityDisplay(AutomaticActionMenuClass.PickupMenuClass.prioNodesByID[id], id);
        }
        internal static void ReciveSetItemPrio(ulong sender, pStructs.pItemPrio info)
        {
            ZiMain.log.LogInfo($"Recived set item prio value from network!");
            uint id = info.id;
            float prio = info.prio;
            ZiMain.log.LogInfo($"id:{id}, prio:{prio}");
            if (!AutomaticActionMenuClass.PickupMenuClass.prioNodesByID.ContainsKey(id))
            {
                ZiMain.log.LogError("Unknown id recived!");
                return;
            }
            zSlideComputer.SetBotItemPriority(id, prio, sender);
            AutomaticActionMenuClass.PickupMenuClass.updateNodePriorityDisplay(AutomaticActionMenuClass.PickupMenuClass.prioNodesByID[id], id);
        }
        internal static void reciveSetResourceThreshold(ulong sender, pStructs.pResourceThreshold info)
        {
            ZiMain.log.LogInfo($"Recived set resource threshold value from network!");
            uint id = info.id;
            int threshold = info.threshold;
            ZiMain.log.LogInfo($"id:{id}, threshold:{threshold}");
            if (!AutomaticActionMenuClass.ShareMenuClass.packNodesByID.ContainsKey(id))
            {
                ZiMain.log.LogError("Unknown id recived!");
                return;
            }
            zSlideComputer.SetResourceThreshold(id, threshold, sender);
            AutomaticActionMenuClass.ShareMenuClass.updateNodeThresholdDisplay(AutomaticActionMenuClass.ShareMenuClass.packNodesByID[id], id);
        }
        internal static void ReciveSetResourceThresholdDisable(ulong sender, pStructs.pResourceThresholdDisable info)
        {
            ZiMain.log.LogInfo($"Recived set resource disable value from network!");
            uint id = info.id;
            bool allowed = info.allowed;
            ZiMain.log.LogInfo($"id:{id}, allowed:{allowed}");
            if (!AutomaticActionMenuClass.ShareMenuClass.packNodesByID.ContainsKey(id))
            {
                ZiMain.log.LogError("Unknown id recived!");
                return;
            }
            zSlideComputer.SetResourceSharePermission(id, allowed, sender);
            AutomaticActionMenuClass.ShareMenuClass.updateNodeThresholdDisplay(AutomaticActionMenuClass.ShareMenuClass.packNodesByID[id], id);
        }
        internal static void ReciveSetPickupPermission(ulong sender, pStructs.pPickupPermission info)
        {
            ZiMain.log.LogInfo($"Recived set pickup permision value from network!");
            int playerID = info.playerID;
            bool allowed = info.allowed;
            ZiMain.log.LogInfo($"id:{playerID}, allowed:{allowed}");
            if (!zSlideComputer.PickUpPerms.ContainsKey(playerID))
            {
                ZiMain.log.LogError("Unknown id recived!");
                return;
            }
            zSlideComputer.SetPickupPermission(playerID, allowed, sender);
            //zMenus.UpdateIndicatorForNode(zMenus.permissionMenu.GetNode("Pickups"), zSlideComputer.PickUpPerms);
            var node = AutomaticActionMenuClass.PickupMenuClass.pickupNode;
            var menu = AutomaticActionMenuClass.PickupMenuClass.pickupMenu;
            if (allowed)
            {
                node.SetColor(zMenuManager.defaultColor);
                menu.centerNode.SetColor(zMenuManager.defaultColor);
            }
            else
            {
                node.SetColor(new Color(0.25f, 0f, 0f));
                menu.centerNode.SetColor(new Color(0.25f, 0f, 0f));
            }
        }
        internal static void ReciveSetSharePermission(ulong sender, pStructs.pSharePermission info)
        {
            ZiMain.log.LogInfo($"Recived set share permision value from network!");
            int playerID = info.playerID;
            bool allowed = info.allowed;
            ZiMain.log.LogInfo($"id:{playerID}, allowed:{allowed}");
            if (!zSlideComputer.SharePerms.ContainsKey(playerID))
            {
                ZiMain.log.LogError("Unknown id recived!");
                return;
            }
            zSlideComputer.SetSharePermission(playerID, allowed, sender);
            //zMenus.UpdateIndicatorForNode(zMenus.permissionMenu.GetNode("Share"), zSlideComputer.SharePerms);
            var node = AutomaticActionMenuClass.ShareMenuClass.shareNode;
            var menu = AutomaticActionMenuClass.ShareMenuClass.shareMenu;
            if (allowed)
            {
                node.SetColor(zMenuManager.defaultColor);
                menu.centerNode.SetColor(zMenuManager.defaultColor);
            }
            else
            {
                node.SetColor(new Color(0.25f, 0f, 0f));
                menu.centerNode.SetColor(new Color(0.25f, 0f, 0f));
            }
        }
        internal static void ReciveRequestToPickupItem(ulong sender, pStructs.pPickupItemInfo info)
        {
            ZiMain.log.LogInfo("Recived request to pickup item!");
            if (!SNet.IsMaster)
                return;
            PlayerAgent agent = pStructs.Get_RefFrom_pStruct(info.playerAgent);
            PlayerAgent commander = pStructs.Get_RefFrom_pStruct(info.commander);
            GameObject itemGobject = pStructs.Get_RefFrom_pStruct(info.item);
            if (itemGobject != null)
                ZiMain.log.LogInfo($"Gobject name: {itemGobject.name}");
            ItemInLevel item = itemGobject.GetComponent<ItemInLevel>();
            if (item == null)
                ZiMain.log.LogInfo($"Item in level is null");
            if (item == null || agent == null || commander == null)
            {
                ZiMain.log.LogError("Invalid request to pickup item: agent, item or commander is null.");
                return;
            }
            ZiMain.log.LogInfo($"{commander.PlayerName} wants to tell {agent.PlayerName} to pickup a {item.PublicName}");
            if (!agent.Owner.IsBot)
            {
                ZiMain.log.LogWarning("Invalid request to pickup item, You can't tell a player what to do.");
                return;
            }
            PlayerAIBot aiBot = agent.gameObject.GetComponent<PlayerAIBot>();
            ZiMain.SendBotToPickupItem(aiBot, item, commander, sender);
        }
        internal static void ReciveRequestToShareResource(ulong netSender, pStructs.pShareResourceInfo info) 
        {
            ZiMain.log.LogInfo("Recived request to share resoruce!");
            if (!SNet.IsMaster)
                return;
            PlayerAgent sender = pStructs.Get_RefFrom_pStruct(info.sender);
            PlayerAgent receiver = pStructs.Get_RefFrom_pStruct(info.receiver);
            PlayerAgent commander = pStructs.Get_RefFrom_pStruct(info.commander);
            
            if (sender == null || receiver == null || commander == null)
            {
                ZiMain.log.LogError("Invalid request to share resource: sender, reciver or commander is null.");
                return;
            }
            ZiMain.log.LogInfo($"{commander.PlayerName} wants to tell {sender.PlayerName} to share resoruces with {receiver.PlayerName}");
            if (!sender.Owner.IsBot)
            {
                ZiMain.log.LogWarning("Invalid request to pickup item, You can't tell a player what to do.");
                return;
            }
            PlayerAIBot aiBot = sender.gameObject.GetComponent<PlayerAIBot>();
            ZiMain.SendBotToShareResourcePack(aiBot, receiver, commander, netSender);
        }

        internal static void ReciveRequestToKillEnemy(ulong netSender, pStructs.pAttackEnemyInfo info)
        {
            ZiMain.log.LogInfo("Recived request to kill enemy!");
            if (!SNet.IsMaster)
                return;
            PlayerAgent aiBotAgent = pStructs.Get_RefFrom_pStruct(info.aiBot);
            PlayerAgent commander = pStructs.Get_RefFrom_pStruct(info.commander);
            EnemyAgent enemy = pStructs.Get_RefFrom_pStruct(info.enemy);

            if (aiBotAgent == null || enemy == null || commander == null)
            {
                ZiMain.log.LogError("Invalid request to share resource: aiBot, reciver or enemy is null.");
                return;
            }
            ZiMain.log.LogInfo($"{commander.PlayerName} wants to tell {aiBotAgent.PlayerName} to kill an enemy.");
            if (!aiBotAgent.Owner.IsBot)
            {
                ZiMain.log.LogWarning("Invalid request to pickup item, You can't tell a player what to do.");
                return;
            }
            PlayerAIBot aiBot = aiBotAgent.gameObject.GetComponent<PlayerAIBot>();
            ZiMain.SendBotToKillEnemy(aiBot, enemy, commander, netSender);
        }
    }
}

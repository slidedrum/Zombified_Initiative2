using BotControl.Menus;
using BotControl.Patches;
using Enemies;
using LevelGeneration;
using Player;
using SlideMenu;
using SNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BotControl.Networking
{
    public class zNetworking
    { //This class will handle all incoming and outgoing network requests.
      //todo only Update values every 100ms.  
      //if not host ask for host's value after change
        internal static void ReciveSetBoolOverideTree(ulong netSender, pStructs.pBoolOverideTreeInfo info)
        {
            ZiMain.log.LogDebug("Recived request to update bool override tree!");
            ZiMain.log.LogDebug($"treeID:{info.treeID}, keyId:{info.keyId}, isNull:{info.isNull}, value:{info.value}");
            uint treeID = info.treeID;
            uint keyId = info.keyId;
            bool isNull = info.isNull;
            bool? value;
            if (isNull)
                value = null;
            else
                value = info.value;
            OverrideTree<bool?> tree = OverrideTree<bool?>.GetTreeFromID(info.treeID);
            tree.SetValue(keyId, value, netSender);
        }
        internal static void ReciveSetIntOverideTree(ulong netSender, pStructs.pIntOverideTreeInfo info)
        {
            ZiMain.log.LogDebug("Recived request to update int override tree!");
            ZiMain.log.LogDebug($"treeID:{info.treeID}, keyId:{info.keyId}, isNull:{info.isNull}, value:{info.value}");
            uint treeID = info.treeID;
            uint keyId = info.keyId;
            bool isNull = info.isNull;
            int? value;
            if (isNull)
                value = null;
            else
                value = info.value;
            OverrideTree<int?> tree = OverrideTree<int?>.GetTreeFromID(info.treeID);
            tree.SetValue(keyId, value, netSender);
        }
        internal static void ReciveSetFloatOverideTree(ulong netSender, pStructs.pFloatOverideTreeInfo info)
        {
            ZiMain.log.LogDebug("Recived request to update float override tree!");
            ZiMain.log.LogDebug($"treeID:{info.treeID}, keyId:{info.keyId}, isNull:{info.isNull}, value:{info.value}");
            uint treeID = info.treeID;
            uint keyId = info.keyId;
            bool isNull = info.isNull;
            float? value;
            if (isNull)
                value = null;
            else
                value = info.value;
            OverrideTree<float?> tree = OverrideTree<float?>.GetTreeFromID(info.treeID);
            tree.SetValue(keyId, value, netSender);
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
            zBotActions.SendBotToPickupItem(aiBot, item, commander, sender);
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
            zBotActions.SendBotToShareResourcePack(aiBot, receiver, commander, netSender);
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
            zBotActions.SendBotToKillEnemy(aiBot, enemy, commander, netSender);
        }
        internal static void ReciveRequestToPickupSentry(ulong netSender, pStructs.pPickupSentryInfo info)
        {
            ZiMain.log.LogInfo("Recived request to pick up sentry!");
            if (!SNet.IsMaster)
                return;
            PlayerAgent agent = pStructs.Get_RefFrom_pStruct(info.playerAgent);
            PlayerAgent commander = pStructs.Get_RefFrom_pStruct(info.commander);

            if (agent == null || commander == null)
            {
                ZiMain.log.LogError("Invalid request to pick up sentry: agent or commander is null.");
                return;
            }
            ZiMain.log.LogInfo($"{commander.PlayerName} wants to tell {agent.PlayerName} to pick up their turret!");
            if (!agent.Owner.IsBot)
            {
                ZiMain.log.LogWarning("Invalid request to pickup sentry, You can't tell a player what to do.");
                return;
            }
            PlayerAIBot aiBot = agent.gameObject.GetComponent<PlayerAIBot>();
            PlayerVoiceManager.WantToSay(commander.CharacterID, AK.EVENTS.PLAY_CL_PICKUPYOURDEPLOYABLES);
            zStaticRefrences.Subtitles.ShowSingleLineSubtitle($"Pick up your deployables.", 1);
            zBotActions.SendBotToPickUpSentry(aiBot, commander, netSender);
        }
        internal static void ReciveRequestToPlaceSentry(ulong netSender, pStructs.pPlaceSentryInfo info)
        {
            ZiMain.log.LogInfo("Recived request to place a sentry!");
            if (!SNet.IsMaster)
                return;
            PlayerAgent agent = pStructs.Get_RefFrom_pStruct(info.playerAgent);
            PlayerAgent commander = pStructs.Get_RefFrom_pStruct(info.commander);
            Pose pose = info.Pose;

            if (agent == null || commander == null || pose == null)
            {
                ZiMain.log.LogError("Invalid request to place turret: agent, commander or pose is null.");
                return;
            }

            ZiMain.log.LogInfo($"{commander.PlayerName} wants to tell {agent.PlayerName} to place their turret!");
            if (!agent.Owner.IsBot)
            {
                ZiMain.log.LogWarning("Invalid request to place turret, You can't tell a player what to do.");
                return;
            }

            PlayerAIBot aiBot = agent.gameObject.GetComponent<PlayerAIBot>();
            zBotActions.SendBotToPlaceSentry(aiBot, pose, commander, netSender);
        }
        internal static void ReciveRequestToThrowItem(ulong netSender, pStructs.pThrowDataInfo info)
        {
            ZiMain.log.LogInfo("Recived request to throw item!");
            if (!SNet.IsMaster)
                return;
            PlayerAgent Commander = pStructs.Get_RefFrom_pStruct(info.Commander);
            PlayerAgent BotAgent = pStructs.Get_RefFrom_pStruct(info.Agent);
            pStructs.pThrowType ThrowType = info.ThrowType;
            Vector3 MovePostion = info.MovePosition;
            Vector3 TargetPosition = info.TargetPosition;
            ZiMain.log.LogInfo($"{Commander.PlayerName} wants to tell {BotAgent.PlayerName} to throw a {ThrowType} from {MovePostion} to {TargetPosition}");
            if (!BotAgent.Owner.IsBot)
            {
                ZiMain.log.LogWarning("Invalid request to throw item, You can't tell a player what to do.");
                return;
            }
            zBotActions.SendBotToThrowItem(Commander, BotAgent, ThrowType, MovePostion, TargetPosition, netSender);
        }
    }
}

using BotControl.Networking;
using BotControl.Patches;
using BotControl.zRootBotPlayerAction;
using Dissonance.Networking.Client;
using Enemies;
using GTFO.API;
using LevelGeneration;
using Player;
using SlideMenu;
using SNetwork;
using System;
using UnityEngine;
using UnityEngine.UIElements;
using static BotControl.Networking.pStructs;

namespace BotControl
{
    public static class zBotActions
    {
        public static void SendBotToPickUpSentry(PlayerAIBot aiBot, PlayerAgent commander = null, ulong netsender = 0)
        {

            ZiMain.BotBarkBack(aiBot.Agent.CharacterID, AK.EVENTS.PLAY_CL_WILLDO, "Will Do.", 2f);
            if (!SNet.IsMaster) //Are we a client?
            {
                if (netsender != 0) //Is this request coming from a different client?
                    return;
                pPickupSentryInfo info = new pPickupSentryInfo();                                                                             //info.item = pStructs.Get_pStructFromRefrence(item);
                info.playerAgent = pStructs.Get_pStructFromRefrence(aiBot.Agent);
                info.commander = pStructs.Get_pStructFromRefrence(commander); //This might be a problem in commander is null?  Not sure. TODO look into it.
                NetworkAPI.InvokeEvent<pPickupSentryInfo>("RequestToPickupSentry", info);
                return;
            }
            // todo check if the sentry is even deployed first.
            // Though it should never get called if it's not deployed already.
            PlayerBotActionDeploySentryGun.Descriptor desc = new(aiBot) 
            {
                Prio = 15f,
            };
            zActions.manualActions.Add(desc);
            aiBot.StartAction(desc);
        }
        public static void SendBotToPlaceSentry(PlayerAIBot aiBot, Pose sentryPose, PlayerAgent commander = null, ulong netsender = 0)
        {
            if (!SNet.IsMaster) //Are we a client?
            {
                if (netsender != 0) //Is this request coming from a different client?
                    return;
                pPlaceSentryInfo info = new pPlaceSentryInfo();                                                                             //info.item = pStructs.Get_pStructFromRefrence(item);
                info.playerAgent = pStructs.Get_pStructFromRefrence(aiBot.Agent);
                info.commander = pStructs.Get_pStructFromRefrence(commander); //This might be a problem in commander is null?  Not sure. TODO look into it.
                info.Pose = sentryPose;
                NetworkAPI.InvokeEvent<pPlaceSentryInfo>("RequestToPlaceSentry", info);
                return;
            }
            // todo check if the sentry is deployed first.
            // Though it should never get called if it's not deployed already.
            PlayerBotActionDeploySentryGun.Descriptor desc = new(aiBot)
            {
                Prio = 15f,
                InstallationPose = sentryPose
            };
            zActions.manualActions.Add(desc);
            aiBot.StartAction(desc);
        }
        public static void SendBotToPickupItem(PlayerAIBot aiBot, ItemInLevel item, PlayerAgent commander = null, ulong netsender = 0)
        {
            //todo add to manual action list for refrence later.
            if (!SNet.IsMaster) //Are we a client?
            {
                if (netsender != 0) //Is this request coming from a different client?
                    return;
                //request host
                pPickupItemInfo info = new pPickupItemInfo();
                info.item.replicatorRef.SetID(item.GetComponent<LG_PickupItem_Sync>().m_stateReplicator.Replicator); //TODO Nullcheck?
                                                                                                                     //info.item = pStructs.Get_pStructFromRefrence(item);
                info.playerAgent = pStructs.Get_pStructFromRefrence(aiBot.Agent);
                info.commander = pStructs.Get_pStructFromRefrence(commander); //This might be a problem in commander is null?  Not sure. TODO look into it.
                NetworkAPI.InvokeEvent<pPickupItemInfo>("RequestToPickupItem", info);
                return;
            }
            //Is this an item we should carry?
            var carrycore = item.gameObject.GetComponent<CarryItemPickup_Core>();
            if (carrycore != null)
            {
                SendBotToCarryItem(aiBot, carrycore, commander, netsender);
                return;
            }
            ZiMain.log.LogInfo($"{commander.PlayerName} is sending {aiBot.Agent.PlayerName} to pick up {item.PublicName}");
            float prio = 15f;
            float haste = 1f;
            PlayerBotActionCollectItem.Descriptor desc = new(aiBot)
            {
                TargetItem = item,
                TargetContainer = item.container,
                TargetPosition = item.transform.position,
                Prio = prio,
                Haste = haste,
            };
            PlayerVoiceManager.WantToSay(commander.CharacterID, AK.EVENTS.PLAY_CL_GRABTHEITEM);
            zStaticRefrences.Subtitles.ShowSingleLineSubtitle($"Grab the item.", 1);
            FlexibleMethodDefinition barkback = new FlexibleMethodDefinition(PlayerVoiceManager.WantToSay, [aiBot.Agent.CharacterID, AK.EVENTS.PLAY_CL_WILLDO]);
            zUpdater.InvokeStatic(barkback, 1f);
            if ((bool)zSlideComputer.ActionPermissions.ValueAt("Notify confirm action"))
                ZiMain.sendChatMessage($"Picking up {item.PublicName}", aiBot.Agent, commander);
            zActions.manualActions.Add(desc);
            aiBot.StartAction(desc);
        }
        public static void SendBotToCarryItem(PlayerAIBot aiBot, CarryItemPickup_Core item, PlayerAgent commander = null, ulong netsender = 0)
        {
            //todo add to manual action list for refrence later.
            //TODO split this up into it's own netaction instead of piggybacking on sendbottopickupitem.
            if (!SNet.IsMaster) //Are we a client?
            {
                if (netsender != 0) //Is this request coming from a different client?
                    return;
                //request host
                pPickupItemInfo info = new pPickupItemInfo();
                info.item.replicatorRef.SetID(item.GetComponent<LG_PickupItem_Sync>().m_stateReplicator.Replicator); //TODO Nullcheck?
                                                                                                                     //info.item = pStructs.Get_pStructFromRefrence(item);
                info.playerAgent = pStructs.Get_pStructFromRefrence(aiBot.Agent);
                info.commander = pStructs.Get_pStructFromRefrence(commander); //This might be a problem in commander is null?  Not sure. TODO look into it.
                NetworkAPI.InvokeEvent<pPickupItemInfo>("RequestToPickupItem", info);
                return;
            }
            ZiMain.log.LogInfo($"{commander.PlayerName} is sending {aiBot.Agent.PlayerName} to carry {item._PublicName_k__BackingField} with the new method");
            float prio = 14f;
            PlayerBotActionCarryExpeditionItem.Descriptor desc = new(aiBot)
            {
                TargetItem = item,
                Prio = prio,
            };
            if ((bool)zSlideComputer.ActionPermissions.ValueAt("Notify confirm action"))
                ZiMain.sendChatMessage($"Carrying {item._PublicName_k__BackingField}", aiBot.Agent, commander);
            PlayerVoiceManager.WantToSay(aiBot.Agent.CharacterID, AK.EVENTS.PLAY_CL_WILLDO); //will do
            zActions.manualActions.Add(desc);
            aiBot.StartAction(desc);
        }
        public static void SendBotToShareResourcePack(PlayerAIBot aiBot, PlayerAgent receiver, PlayerAgent commander = null, ulong netsender = 0)
        {
            //todo add to manual action list for refrence later.
            if (!SNet.IsMaster)//Are we a client?
            {
                if (netsender != 0)//Is this request coming from a different client?
                    return;
                //request host
                pStructs.pShareResourceInfo info = new pStructs.pShareResourceInfo();
                info.sender = pStructs.Get_pStructFromRefrence(aiBot.Agent);
                info.receiver = pStructs.Get_pStructFromRefrence(receiver);
                info.commander = pStructs.Get_pStructFromRefrence(commander); //This might be a problem in commander is null?  Not sure. TODO look into it.
                NetworkAPI.InvokeEvent<pStructs.pShareResourceInfo>("RequestToShareResourcePack", info);
                return;
            }
            float prio = 15f;
            float haste = 1f;
            BackpackItem backpackItem = null;
            ZiMain.log.LogInfo($"{aiBot.Agent.PlayerName} was told by {commander?.PlayerName ?? "someone"} with netid {netsender} to try to share resource pack to {receiver.PlayerName}");
            //var gotBackpackItem = aiBot.Backpack.HasBackpackItem(InventorySlot.ResourcePack) &&
            //                      aiBot.Backpack.TryGetBackpackItem(InventorySlot.ResourcePack, out backpackItem);
            bool gotBackpackItem = aiBot.Backpack.TryGetBackpackItem(InventorySlot.ResourcePack, out backpackItem);
            if (!gotBackpackItem)
                return;
            ItemEquippable resourcePack = backpackItem.Instance.Cast<ItemEquippable>();
            aiBot.Inventory.DoEquipItem(resourcePack);//is this needed?  Does the action not handle this?
            PlayerBotActionShareResourcePack.Descriptor desc = new(aiBot)
            {
                Receiver = receiver,
                Item = resourcePack,
                Prio = prio,
                Haste = haste,
            };
            float ammoLeft = aiBot.Backpack.AmmoStorage.GetAmmoInPack(AmmoType.ResourcePackRel);
            ZiMain.sendChatMessage($"Sharing my {resourcePack.PublicName} ({ammoLeft}%) with {receiver.PlayerName}.", aiBot.Agent, commander);
            PlayerVoiceManager.WantToSay(aiBot.Agent.CharacterID, AK.EVENTS.PLAY_CL_WILLDO);
            aiBot.StartAction(desc);
        }
        public static void SendBotToClearCurrentRoom(PlayerAIBot aiBot = null, PlayerAgent commander = null, ulong netsender = 0, PlayerBotActionBase.Descriptor arg_descriptor = null)
        {

            if (arg_descriptor != null && arg_descriptor.Status != PlayerBotActionBase.Descriptor.StatusType.Successful)
            {
                ZiMain.log.LogInfo($"Unsucsefull last kill {arg_descriptor.Status}");
                return;
            }
            if (commander == null)
                commander = PlayerManager.GetLocalPlayerAgent();
            var allEnemies = commander.CourseNode.m_enemiesInNode;
            if (aiBot == null)
            {
                PlayerAgent closestBot = null;
                float closestBotDistnace = float.MaxValue;
                foreach (var botCandidate in PlayerManager.PlayerAgentsInLevel)
                {
                    if (!botCandidate.Owner.IsBot)
                        continue;
                    float distanceToBot = (commander.gameObject.transform.position - botCandidate.gameObject.transform.position).sqrMagnitude;
                    if (distanceToBot < closestBotDistnace)
                    {
                        closestBotDistnace = distanceToBot;
                        closestBot = botCandidate;
                    }
                }
                if (closestBot == null)
                    return;
                aiBot = closestBot.gameObject.GetComponent<PlayerAIBot>();
            }
            if (allEnemies.Count <= 0)
            {
                ZiMain.sendChatMessage("I have killed all enemies in the room", aiBot.gameObject.GetComponent<PlayerAgent>(), commander);
                return;
            }

            EnemyAgent closestEnemy = null;
            float closestEnemyDistnace = float.MaxValue;
            foreach (var enemy in allEnemies)
            {
                float distanceToEnemy = (aiBot.gameObject.transform.position - enemy.gameObject.transform.position).sqrMagnitude;
                if (distanceToEnemy < closestEnemyDistnace)
                {
                    closestEnemyDistnace = distanceToEnemy;
                    closestEnemy = enemy;
                }
            }
            var descriptor = SendBotToKillEnemy(aiBot, closestEnemy, commander);
            FlexibleMethodDefinition callback = new(SendBotToClearCurrentRoom, [aiBot, commander, netsender]);
            zActionSub.addOnTerminated(descriptor, callback);
        }
        public static PlayerBotActionAttack.Descriptor SendBotToKillEnemy(PlayerAIBot aiBot, EnemyAgent enemy, PlayerAgent commander = null, ulong netsender = 0, PlayerBotActionAttack.StanceEnum stance = PlayerBotActionAttack.StanceEnum.All, PlayerBotActionAttack.AttackMeansEnum means = PlayerBotActionAttack.AttackMeansEnum.Melee, PlayerBotActionWalk.Descriptor.PostureEnum posture = PlayerBotActionWalk.Descriptor.PostureEnum.Crouch)
        {
            float attackPrio = 5f;
            float attackHaste = 0.5f;
            if (!SNet.IsMaster) //Are we a client?
            {
                if (netsender != 0) //Is this request coming from a different client?
                    return null;
                //request host
                pAttackEnemyInfo info = new pAttackEnemyInfo();
                info.enemy = pStructs.Get_pStructFromRefrence(enemy);
                info.aiBot = pStructs.Get_pStructFromRefrence(aiBot.Agent);
                info.commander = pStructs.Get_pStructFromRefrence(commander); //This might be a problem in commander is null?  Not sure. TODO look into it.
                if ((bool)zSlideComputer.ActionPermissions.ValueAt("Notify confirm action"))
                    NetworkAPI.InvokeEvent<pAttackEnemyInfo>("RequestToKillEnemy", info);
                return null;
            }
            var descriptor = new PlayerBotActionAttack.Descriptor(aiBot)
            {
                Stance = stance,
                Means = means,
                Posture = posture,
                TargetAgent = enemy,
                Prio = attackPrio,
                Haste = attackHaste,
            };
            aiBot.Actions[0].Cast<RootPlayerBotAction>().m_followLeaderAction.Prio = attackPrio - 1;
            zActions.manualActions.Add(descriptor);
            ZiMain.sendChatMessage($"Killing the {enemy.EnemyData.name}.", aiBot.Agent, commander);
            //TODO figure out how to make them crouch instead of stand.
            PlayerVoiceManager.WantToSay(aiBot.Agent.CharacterID, AK.EVENTS.PLAY_CL_WILLDO);
            aiBot.StartAction(descriptor);
            return descriptor;
        }
        public static bool SendBotToThrowItem(PlayerAgent Commander, PlayerAgent botAgent, pStructs.pThrowType ThrowType, Vector3 MovePosition, Vector3 TargetPosition, ulong netSender = 0)
        {
            if (!SNet.Master)
                return false;

            PlayerAIBot aiBot = botAgent.GetComponent<PlayerAIBot>();
            var backpack = aiBot.Backpack;
            backpack.TryGetBackpackItem(InventorySlot.Consumable, out var item);
            if (item == null)
            {
                ZiMain.log.LogWarning($"Wanted to throw {ThrowType} but found nothing.");
                return false;
            }
            if (item.Name != ThrowItemPatch.ThrowMappings[ThrowType])
            {
                ZiMain.log.LogWarning($"Invalid throw item to throw.  Wanted to throw {ThrowType} but found {item.Name}");
                return false;
            }

            PlayerBotActionThrowItem.Descriptor desc = new(aiBot)
            {
                Prio = 15f,
                Haste = 0.8f,
                TargetPosition = TargetPosition,
                TargetObject = Commander.transform,
                TargetType = PlayerBotActionThrowItem.TargetTypeEnum.Position,
                Item = item.Instance.Cast<ItemEquippable>(),
                MovementAllowed = true
            };
            aiBot.StartAction(desc);
            return false;
        }
    }
}

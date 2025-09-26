using Agents;
using AIGraph;
using Enemies;
using GameData;
using Il2CppInterop.Runtime;
using LevelGeneration;
using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ZombieTweak2.zMenu;
using ZombieTweak2.zNetworking;
using Zombified_Initiative;
using static ZombieTweak2.zNetworking.pStructs;

namespace ZombieTweak2
{
    public static class zDebug
    {
        //This class is unused, but it's where i put all the stuff I need for debugging.
        private static GameObject debugSphere;
        public static Agent nofindagent;
        private static bool checkVis = false;
        internal static void ShowDebugSphere(Vector3 position, float radius)
        {
            if (debugSphere == null)
            {
                debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                debugSphere.name = "LookDirectionDebugSphere";

                // Remove collider so it doesn’t interfere with physics
                UnityEngine.Object.Destroy(debugSphere.GetComponent<Collider>());

                // Make it semi-transparent
                var renderer = debugSphere.GetComponent<Renderer>();
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = new Color(0f, 1f, 0f, 0.3f); // green, 30% opacity
                renderer.material.SetFloat("_Mode", 3); // Transparent mode
                renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                renderer.material.SetInt("_ZWrite", 0);
                renderer.material.DisableKeyword("_ALPHATEST_ON");
                renderer.material.EnableKeyword("_ALPHABLEND_ON");
                renderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                renderer.material.renderQueue = 3000;
            }

            debugSphere.transform.position = position;
            debugSphere.transform.localScale = Vector3.one * (radius * 2f); // scale to match search radius
        }
        private static void printallbotactionpriorities()
        {
            var allAgents = PlayerManager.PlayerAgentsInLevel;
            foreach (var agent in allAgents)
            {
                if (!agent.Owner.IsBot)
                    continue;

                var aiBot = agent.gameObject.GetComponent<PlayerAIBot>();
                if (aiBot == null)
                    continue;

                ZiMain.log.LogInfo(agent.PlayerName);

                var rootAction = aiBot.Actions[0].Cast<RootPlayerBotAction>(); ;
                if (rootAction == null)
                    continue;

                Type currentType = rootAction.GetType();

                // Walk up the inheritance chain to get all fields and properties
                while (currentType != null && currentType != typeof(object))
                {
                    // Fields
                    var fields = currentType.GetFields(
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.DeclaredOnly);

                    foreach (var field in fields)
                    {
                        if (typeof(PlayerBotActionBase.Descriptor).IsAssignableFrom(field.FieldType))
                        {
                            var descriptor = field.GetValue(rootAction) as PlayerBotActionBase.Descriptor;
                            if (descriptor != null)
                                ZiMain.log.LogInfo($"\t - {field.Name}: {descriptor.Prio}");
                        }
                    }

                    // Properties
                    var properties = currentType.GetProperties(
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.DeclaredOnly);

                    foreach (var prop in properties)
                    {
                        if (typeof(PlayerBotActionBase.Descriptor).IsAssignableFrom(prop.PropertyType))
                        {
                            try
                            {
                                var descriptor = prop.GetValue(rootAction, null) as PlayerBotActionBase.Descriptor;
                                if (descriptor != null)
                                    ZiMain.log.LogInfo($"\t - {prop.Name}: {descriptor.Prio}");
                            }
                            catch
                            {
                                // Some IL2CPP properties may throw when accessed via reflection
                            }
                        }
                    }

                    currentType = currentType.BaseType;
                }
            }
        }
        private static void printAllNames()
        {
            System.Collections.Generic.HashSet<string> ArchetypeNames = new System.Collections.Generic.HashSet<string>();
            System.Collections.Generic.HashSet<string> terminalItemShortNames = new System.Collections.Generic.HashSet<string>();
            System.Collections.Generic.HashSet<string> terminalItemLongNames = new System.Collections.Generic.HashSet<string>();
            System.Collections.Generic.HashSet<string> PublicNames = new System.Collections.Generic.HashSet<string>();
            System.Collections.Generic.HashSet<string> DataBlockPublicNames = new System.Collections.Generic.HashSet<string>();
            System.Collections.Generic.List<Item> allItems = GameObject.FindObjectsOfType<Item>().ToList();
            foreach (Item item in allItems) 
            {
                PublicNames.Add(item?.PublicName ?? "");
                DataBlockPublicNames.Add(item?.ItemDataBlock?.publicName ?? "");
                ArchetypeNames.Add(item?.ArchetypeName ?? "");
                terminalItemShortNames.Add(item?.ItemDataBlock?.terminalItemShortName ?? "");
                terminalItemLongNames.Add(item?.ItemDataBlock?.terminalItemLongName ?? "");
            }
            ZiMain.log.LogInfo("PublicNames:");
            ZiMain.log.LogInfo(string.Join("\n", PublicNames));
            ZiMain.log.LogInfo("DataBlockPublicNames:");
            ZiMain.log.LogInfo(string.Join("\n", DataBlockPublicNames));
            ZiMain.log.LogInfo("ArchetypeNames:");
            ZiMain.log.LogInfo(string.Join("\n", ArchetypeNames));
            ZiMain.log.LogInfo("terminalItemShortNames:");
            ZiMain.log.LogInfo(string.Join("\n", terminalItemShortNames));
            ZiMain.log.LogInfo("terminalItemLongNames:");
            ZiMain.log.LogInfo(string.Join("\n", terminalItemLongNames));
        }
        private static void SetToolThreshold(uint id, int threshold)
        {
            zSlideComputer.resourceThresholds[id] = threshold;
        }
        private static void printAllInventoryItems()
        {
            var allItemTypes = ItemSpawnManager.m_itemDataPerInventorySlot;
            for (int i = 0; i < allItemTypes.Count; i++)
            {
                var slotName = ((InventorySlot)i).ToString();
                var items = allItemTypes[i];
                ZiMain.log.LogInfo($"{slotName} ({items.Count}):");
                foreach (var item in items)
                {
                    ZiMain.log.LogInfo($"\t - ({ItemDataBlock.s_blockIDByName[item.name]}){item.publicName}"); //there's got to be a better way to get the id.
                }
            }
        }
        private static void printWhatBotsCanPickUp()
        {
            //ItemDataBlock.s_blockIDByName has all ids
            //RootPlayerBotAction.s_itemBasePrios has what bots can pick up
            foreach (Il2CppSystem.Collections.Generic.KeyValuePair<uint,float> item in RootPlayerBotAction.s_itemBasePrios)
            {
                uint id = item.Key;
                float priority = item.Value;
                ItemDataBlock block = ItemDataBlock.s_blockByID[id];
                var name = block.name;
                ZiMain.log.LogMessage($"{name}:{priority}");
            }
        }
        private static string printNameFromID(uint id)
        {
            if (ItemDataBlock.s_blockByID.ContainsKey(id))
            {
                string name = ItemDataBlock.s_blockByID[id].name + " - " + ItemDataBlock.s_blockByID[id].publicName;
                ZiMain.log.LogInfo($"{id}: {name}");
                return name;
            }
            return "";
        }
        private static ItemDataBlock getDatablockFromId(uint id)
        {
            if (ItemDataBlock.s_blockByID.ContainsKey(id))
            {
                return ItemDataBlock.s_blockByID[id];
            }
            return null;
        }
        private static pStructs.pBotSelections testSend = new pStructs.pBotSelections();
        private static void setuppBotSelectionsForTest()
        {
            testSend.data = zNetworking.zNetworking.EncodeBotSelectionForNetwork(SelectionMenuClass.botSelection);
        }
        private static void TestReciveTogglePickupPermission()
        {
            zNetworking.zNetworking.reciveTogglePickupPermission(0, testSend);
        }
        private static bool OriginalUpdateActionShareResoursePack(RootPlayerBotAction __instance, ref PlayerBotActionBase.Descriptor bestAction)
        {
            //This is based on the original mono decomp.  Not a re-creation.
            if (!__instance.m_shareResourceAction.IsTerminated())
            {
                //is there already share resource action?
                return false;
            }
            __instance.m_shareResourceAction.Prio = RootPlayerBotAction.m_prioSettings.ShareResource;
            if (!RootPlayerBotAction.CompareActionPrios(__instance.m_shareResourceAction, bestAction))
            {
                //Do we already have a more important action?
                return false;
            }
            if (__instance.m_bot.IsActionForbidden(__instance.m_shareResourceAction))
            {
                //Are we allowed to do this action?
                return false;
            }
            PlayerBackpack backpack = PlayerBackpackManager.GetBackpack(__instance.m_agent.Owner);
            BackpackItem backpackItem;
            if (!backpack.TryGetBackpackItem(InventorySlot.ResourcePack, out backpackItem))
            {
                //Do we have any resources to share?
                return false;
            }

            PlayerAmmoStorage ammoStorage = backpack.AmmoStorage;
            float shareUnit = ammoStorage.ResourcePackAmmo.CostOfBullet / ammoStorage.ResourcePackAmmo.AmmoMaxCap;

            PlayerAgent chosenAgent = null;
            float topCandidateScore = 0f;

            for (int i = 0; i < PlayerManager.PlayerAgentsInLevel.Count; i++)
            {
                PlayerAgent candidateAgent = PlayerManager.PlayerAgentsInLevel[i];

                if (candidateAgent == null || !candidateAgent.Alive)
                {
                    //if the agent is null, or they're dead ignore them.
                    continue;
                }
                if (candidateAgent != __instance.m_agent)
                {
                    float prio = __instance.m_shareResourceAction.Prio;
                    Vector3 position = candidateAgent.Position;

                    if (__instance.m_bot.ApplyRestrictionsToRootPosition(ref position, ref prio))
                    {
                        //would moving to the candidate cause problems?
                        continue;
                    }
                }

                PlayerAmmoStorage candidateAmmoStorage = PlayerBackpackManager.GetBackpack(candidateAgent.Owner).AmmoStorage;
                float candidateScore = 0f;

                if (__instance.m_gearAvailability.Has(RootPlayerBotAction.GearAvailability.GearFlags.Medipack))
                {
                    shareUnit = AgentModifierManager.ApplyModifier(__instance.m_agent, AgentModifier.HealSupport, shareUnit);//This is artifacts!
                    if (candidateAgent.Damage.GetHealthRel() + shareUnit < 0.98f)//Would you have less than 98% health even after heals?
                    {
                        candidateScore = 0.98f - candidateAgent.Damage.GetHealthRel();//the more damaged they are, they higher the score
                    }
                }
                else if (__instance.m_gearAvailability.Has(RootPlayerBotAction.GearAvailability.GearFlags.AmmoPackWeapon))
                {
                    shareUnit = AgentModifierManager.ApplyModifier(__instance.m_agent, AgentModifier.AmmoSupport, shareUnit);//This is artifacts!
                    if (candidateAmmoStorage.StandardAmmo.RelInPack + shareUnit < 0.98f &&//Would we overflow primary or secondary ammo?
                        candidateAmmoStorage.SpecialAmmo.RelInPack + shareUnit < 0.98f)
                    {
                        candidateScore = 0.98f - (candidateAmmoStorage.StandardAmmo.RelInPack + candidateAmmoStorage.SpecialAmmo.RelInPack) / 2f;//score is average of both ammo pools
                    }
                }
                else if (__instance.m_gearAvailability.Has(RootPlayerBotAction.GearAvailability.GearFlags.DisinfectionPack))
                {
                    if (candidateAgent.Damage.Infection * 0.98f > shareUnit)//Would we overheal?
                    {
                        candidateScore = candidateAgent.Damage.Infection * 0.98f;//our score is how infected they are
                    }
                }
                else if (__instance.m_gearAvailability.Has(RootPlayerBotAction.GearAvailability.GearFlags.AmmoPackTool))
                {
                    shareUnit = AgentModifierManager.ApplyModifier(__instance.m_agent, AgentModifier.AmmoSupport, shareUnit);//This is artifacts!
                    if (candidateAgent.NeedToolAmmo() && candidateAmmoStorage.ClassAmmo.RelInPack + shareUnit < 0.98f)//would we overflow tool ammo?
                    {
                        candidateScore = 0.98f - candidateAmmoStorage.ClassAmmo.RelInPack;//how much ammo are they missing?
                    }
                }
                if (candidateScore > 0f)//did they score at all?
                {
                    candidateScore = Mathf.Clamp01(candidateScore / 0.98f);//make sure the score is not over 1 and reduce it slightly.
                    if (!candidateAgent.Owner.IsBot)
                    {
                        candidateScore = Mathf.Lerp(candidateScore, 1f, 0.5f);//deprioritize bots
                    }

                    if (candidateScore > topCandidateScore)
                    {
                        chosenAgent = candidateAgent;
                        topCandidateScore = candidateScore;
                    }
                }
            }
            if (chosenAgent != null)
            {
                __instance.m_shareResourceAction.Item = backpackItem.Instance.Cast<ItemEquippable>();
                __instance.m_shareResourceAction.Receiver = chosenAgent;
                __instance.m_shareResourceAction.Haste = 0.8f;
                bestAction = __instance.m_shareResourceAction;
            }
            return false;
        }
        private static void TestReciveSetItemPrioDisableNetwork(ulong sender, uint id, bool allowed)
        {
            pStructs.pItemPrioDisable info = new pStructs.pItemPrioDisable();
            info.allowed = allowed;
            info.id = id;
            zNetworking.zNetworking.ReciveSetItemPrioDisable(sender, info);
        }
        private static void TestReciveSetItemPrioNetwork(ulong sender, uint id, float prio)
        {
            pStructs.pItemPrio info = new pStructs.pItemPrio();
            info.prio = prio;
            info.id = id;
            zNetworking.zNetworking.ReciveSetItemPrio(sender, info);
        }
        private static void TestReciveSetResourceThresholdNetwork(ulong sender, uint id, int threshold)
        {
            pStructs.pResourceThreshold info = new pStructs.pResourceThreshold();
            info.threshold = threshold;
            info.id = id;
            zNetworking.zNetworking.reciveSetResourceThreshold(sender, info);
        }
        private static void TestReciveSetResourceThresholdDisableNetwork(ulong sender, uint id, bool allowed)
        {
            pStructs.pResourceThresholdDisable info = new pStructs.pResourceThresholdDisable();
            info.allowed = allowed;
            info.id = id;
            zNetworking.zNetworking.ReciveSetResourceThresholdDisable(sender, info);
        }
        private static void TestReciveSetPickupPermissionNetwork(ulong sender, int playerID, bool allowed)
        {
            pStructs.pPickupPermission info = new pStructs.pPickupPermission();
            info.allowed = allowed;
            info.playerID = playerID;
            zNetworking.zNetworking.ReciveSetPickupPermission(sender, info);
        }
        private static void TestReciveSetSharePermissionNetwork(ulong sender, int playerID, bool allowed)
        {
            pStructs.pSharePermission info = new pStructs.pSharePermission();
            info.allowed = allowed;
            info.playerID = playerID;
            zNetworking.zNetworking.ReciveSetSharePermission(sender, info);
        }
        private static void TestReciveRequestToPickupItemNetwork(ulong sender, PlayerAgent bot, ItemInLevel item)
        {
            pPickupItemInfo info = new pPickupItemInfo();
            info.item = pStructs.Get_pStructFromRefrence(item);
            info.playerAgent = pStructs.Get_pStructFromRefrence(bot);
            info.commander = pStructs.Get_pStructFromRefrence(PlayerManager.GetLocalPlayerAgent());
            zNetworking.zNetworking.ReciveRequestToPickupItem(sender, info);
        }
        private static void TestReciveRequestToShareResourcePackNetwork(ulong netSender, PlayerAgent sender, PlayerAgent receiver)
        {
            pStructs.pShareResourceInfo info = new pStructs.pShareResourceInfo();
            info.sender = pStructs.Get_pStructFromRefrence(sender);
            info.receiver = pStructs.Get_pStructFromRefrence(receiver);
            info.commander = pStructs.Get_pStructFromRefrence(PlayerManager.GetLocalPlayerAgent());
            zNetworking.zNetworking.ReciveRequestToShareResource(netSender, info);
        }
        private static void TestReciveRequestSendBotToKillEnemyNetwork(ulong netSender, PlayerAgent aiBot, EnemyAgent enemy)
        {
            pStructs.pAttackEnemyInfo info = new pStructs.pAttackEnemyInfo();
            info.aiBot = pStructs.Get_pStructFromRefrence(aiBot);
            info.enemy = pStructs.Get_pStructFromRefrence(enemy);
            info.commander = pStructs.Get_pStructFromRefrence(PlayerManager.GetLocalPlayerAgent());
            zNetworking.zNetworking.ReciveRequestToKillEnemy(netSender, info);
        }
        internal static void debugCheckViz()
        {
            GameObject observer = PlayerManager.GetLocalPlayerAgent().FPSCamera.gameObject;
            Transform menuTransform = zMenuManager.mainMenu.gameObject.transform;
            GameObject target = zSearch.GetClosestObjectInLookDirection(menuTransform, zSearch.GetGameObjectsWithLookDirection<EnemyAgent>(menuTransform), 180f);
            zVisiblityManager2.CheckForObject(observer, target);
        }
        internal static void toggleVisCheck()
        {
            setVisCheck(!checkVis);
        }
        internal static void setVisCheck(bool set)
        {
            checkVis = set;
        }
        internal static void MarkUnexploredArea(PlayerAgent playerAgent = null)
        {
            if (playerAgent == null)
                playerAgent = PlayerManager.GetLocalPlayerAgent();
            var Unexplored = VisitNode.getUnexploredLocation(playerAgent.Position);
            if (Unexplored == playerAgent.Position || Unexplored == Vector3.zero)
                return;
            CreatePing(Unexplored);
        }
        internal static void CreatePing(Vector3 pos)
        {
            var localPlayer = PlayerManager.GetLocalPlayerAgent();
            GuiManager.AttemptSetPlayerPingStatus(localPlayer, true, pos);
        }
        internal static void debugUpdate() 
        {
            if (checkVis)
            {
                GameObject observer = PlayerManager.GetLocalPlayerAgent().FPSCamera.gameObject;
                GameObject target = zSearch.GetClosestObjectInLookDirection(observer.transform, zSearch.GetGameObjectsWithLookDirection<EnemyAgent>(observer.transform), 180f);
                zVisiblityManager2.CheckForObject(observer, target);
            }
        }
        internal static void SendClosestBotToExplore()
        {
            PlayerAgent playerAgent = PlayerManager.GetLocalPlayerAgent();
            PlayerAIBot bot = null;
            PlayerAgent botAgent = null;
            float closestDistance = float.MaxValue;
            foreach (var agent in PlayerManager.PlayerAgentsInLevel)
            {
                if (!agent.Owner.IsBot || !agent.Alive)
                    continue;
                float distance = Vector3.Distance(agent.Position, playerAgent.Position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    botAgent = agent;
                    bot = agent.gameObject.GetComponent<PlayerAIBot>();
                }
            }
            if (bot == null)
                return;
            ZiMain.sendChatMessage("Okay here I go exploring!", botAgent, playerAgent);
            SendBotToExplore(bot);
        }
        internal static void SendBotToExplore(PlayerAIBot bot, PlayerBotActionTravel.Descriptor desc = null)
        {
            if (desc != null && desc.Status != PlayerBotActionBase.Descriptor.StatusType.Successful)
                return;
            PlayerAgent agent = bot.Agent;
            var Unexplored = VisitNode.getUnexploredLocation(agent.Position);
            if (Unexplored == agent.Position || Unexplored == Vector3.zero)
                return;
            CreatePing(Unexplored);
            PlayerBotActionTravel.Descriptor descriptor = new(bot)
            {
                DestinationPos = Unexplored,
                Haste = 0.5f,
                WalkPosture = PlayerBotActionWalk.Descriptor.PostureEnum.None,
                Radius = 0.1f,
                DestinationType = PlayerBotActionTravel.Descriptor.DestinationEnum.Position,
                Persistent = false,
                Prio = 15,
            };
            bot.StartAction(descriptor);
            FlexibleMethodDefinition callback = new (SendBotToExplore, [bot]);
            zActionSub.addOnTerminated(descriptor, callback);
        }
    }
}

using Agents;
using Dissonance;
using GameData;
using GTFO.API;
using HarmonyLib;
using Player;
using SNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ZombieTweak2.zMenu;
using ZombieTweak2.zNetworking;
using Zombified_Initiative;

namespace ZombieTweak2
{
    public static class zDebug
    {//This class is unused, but it's where i put all the stuff I need for debugging.
        private static GameObject debugSphere;
        public static Agent nofindagent;
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
        private static void OLDsetPickupPermission(string bot, bool allowed)
        { //TODO - figure out network stuff and refactor this. Right now I'm a little scared to touch this because I can't test it.
            foreach (KeyValuePair<String, PlayerAIBot> iBotTable in ZiMain.BotTable)
            {
                string botName = iBotTable.Key;
                PlayerAIBot playerAIBot = iBotTable.Value;
                if (bot == botName)
                {
                    ZiMain.log.LogInfo($"{botName} pickup perm set to {allowed}");
                    if (!SNet.IsMaster) NetworkAPI.InvokeEvent<ZiMain.ZINetInfo>("ZINetInfo", new ZiMain.ZINetInfo(2, playerAIBot.m_playerAgent.PlayerSlotIndex, 0, 0, 0));
                    if (SNet.IsMaster)
                    {
                        zComputer botComp = playerAIBot.GetComponent<zComputer>();
                        if (botComp.pickupaction != null) botComp.pickupaction.DescBase.SetCompletionStatus(PlayerBotActionBase.Descriptor.StatusType.Failed);
                        botComp.allowedpickups = allowed;
                    }
                }
            }
        }
        private static void OLDtogglePickupPermission(string bot, bool everyone = false)
        { //TODO - rework so it doesn't need a for loop
          //TODO - remove everyone arg
            foreach (KeyValuePair<String, PlayerAIBot> iBotTable in ZiMain.BotTable)
            {
                string botName = iBotTable.Key;
                PlayerAIBot playerAIBot = iBotTable.Value;
                if (everyone || bot == botName)
                {
                    ZiMain.log.LogInfo($"{botName} toggle resource pickups");
                    if (!SNet.IsMaster) NetworkAPI.InvokeEvent<ZiMain.ZINetInfo>("ZINetInfo", new ZiMain.ZINetInfo(2, playerAIBot.m_playerAgent.PlayerSlotIndex, 0, 0, 0));
                    if (SNet.IsMaster)
                    {
                        zComputer botComp = playerAIBot.GetComponent<zComputer>();
                        if (botComp.pickupaction != null) botComp.pickupaction.DescBase.SetCompletionStatus(PlayerBotActionBase.Descriptor.StatusType.Failed);
                        botComp.allowedpickups = !botComp.allowedpickups;
                    }
                }
            }
        }
        private static void TestSetItemPrioDissableNetwork(ulong sender, uint id, bool allowed)
        {
            pStructs.pItemPrioDisable info = new pStructs.pItemPrioDisable();
            info.allowed = allowed;
            info.id = id;
            zNetworking.zNetworking.reciveSetItemPrioDissable(sender, info);
        }
        private static void TestSetBotItemPrioNetwork(ulong sender, uint id, float prio)
        {
            pStructs.pItemPrio info = new pStructs.pItemPrio();
            info.prio = prio;
            info.id = id;
            zNetworking.zNetworking.reciveSetItemPrio(sender, info);
        }
    }
}

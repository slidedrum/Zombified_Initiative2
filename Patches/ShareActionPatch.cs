using BetterBots.Components;
using GameData;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Zombified_Initiative;

namespace ZombieTweak2.Patches
{
    [HarmonyPatch]
    internal class ShareActionPatch
    {
        public static Il2CppReferenceArray<ItemDataBlock> rawResourcePackItems
        {
            get
            {
                return ItemSpawnManager.m_itemDataPerInventorySlot?[(int)InventorySlot.ResourcePack];
            }
        }
        public static Il2CppReferenceArray<ItemDataBlock> resourcePackItems
        {
            get
            {
                var source = rawResourcePackItems;

                if (ReferenceEquals(source, _rawResourcePackItems))
                    return _resourcePackItems;

                _rawResourcePackItems = source;

                _resourcePackItems = new Il2CppReferenceArray<ItemDataBlock>(
                    source
                        .Where(item =>
                            item != null)
                        .ToArray());

                return _resourcePackItems;
            }
        }
        private static Il2CppReferenceArray<ItemDataBlock> _rawResourcePackItems;
        private static Il2CppReferenceArray<ItemDataBlock> _resourcePackItems;
        public static List<string> resourcePackItemPublicNames
        {
            get
            {
                var items = resourcePackItems;

                if (ReferenceEquals(items, _resourcePackItems) &&
                    _resourcePackItemPublicNames != null)
                    return _resourcePackItemPublicNames;

                _resourcePackItemPublicNames = items
                    .Select(item => item.publicName)
                    .ToList();

                return _resourcePackItemPublicNames;
            }
        }
        private static List<string> _resourcePackItemPublicNames;
        public static List<string> resourcePackItemNames
        {
            get
            {
                var items = resourcePackItems;

                if (ReferenceEquals(items, _resourcePackItems) &&
                    _resourcePackItemNames != null)
                    return _resourcePackItemNames;

                _resourcePackItemNames = items
                    .Select(item => item.name)
                    .ToList();

                return _resourcePackItemNames;
            }
        }
        private static List<string> _resourcePackItemNames;
        [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.UpdateActionShareResoursePack))]
        [HarmonyPrefix]
        public static bool UpdateActionShareResoursePack(RootPlayerBotAction __instance, ref PlayerBotActionBase.Descriptor bestAction)
        {
            //This is based on the original mono decomp. Not a re-creation.  Still quite modified though.
            //Todo have a custom threshold per type - DONE
            //Todo Block some types outright. - DONE
            //Todo if someone else is already giving the visTarget the same item, don't double up.
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
                //Do we have any resourcesActions to share?
                return false;
            }
            int agentID = __instance.m_agent.Owner.PlayerSlotIndex();
            var itemID = backpackItem.ItemID;
            var itemName = backpackItem.Name;
            var block = ItemDataBlock.GetBlock(itemID);
            var itemInternalName = block.name;
            var actionKey = "Share" + itemInternalName;
            //if (!(bool)zSlideComputer.ActionPermissions.ValueAt("Share"))
            //{ 
            //    //Do we have share perms?
            //    return false;
            //}
            if (!(bool)zSlideComputer.ActionPermissions.ValueAt(actionKey))
            {
                //Is this item dissabled?
                return false;
            }


            if (ZiMain.HasBetterBots && CheckBetterBotsDanger(__instance.m_agent.gameObject))
            {
                return false; // skip sharing if bot is in danger
            }



            PlayerAmmoStorage ammoStorage = backpack.AmmoStorage;
            float ammoutCanGivePercent = ammoStorage.ResourcePackAmmo.CostOfBullet / ammoStorage.ResourcePackAmmo.AmmoMaxCap;

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
                switch (itemID)
                {
                    case 102: //medpack
                        ammoutCanGivePercent = AgentModifierManager.ApplyModifier(__instance.m_agent, AgentModifier.HealSupport, ammoutCanGivePercent);//This is artifacts!;
                        break;
                    case 127: //toolPack
                    case 101: //Ammopack
                        ammoutCanGivePercent = AgentModifierManager.ApplyModifier(__instance.m_agent, AgentModifier.AmmoSupport, ammoutCanGivePercent);
                        break;
                    case 132: //Disinfect
                    default: //something weird
                        break;
                }
                float threshHold = 0.98f; //not needed fallback 
                if (zSlideComputer.ActionPriorities.HasKey(actionKey))
                {
                    int baseThreshold = (int)zSlideComputer.ActionPriorities.ValueAt(actionKey);
                    foreach (var agent in PlayerManager.PlayerAgentsInLevel)
                    {
                        //Check if any other bots are giving the same visTarget the same resource
                        if (!agent.Owner.IsBot)
                            continue;
                        PlayerAIBot aiBot = agent.gameObject.GetComponent<PlayerAIBot>();
                        foreach (var action in aiBot.Actions)
                        {
                            if (action.GetIl2CppType().Name == "PlayerBotActionShareResourcePack")
                            {
                                var descriptor = action.DescBase.Cast<PlayerBotActionShareResourcePack.Descriptor>();
                                if (descriptor.Item.ItemDataBlock.persistentID == itemID
                                    && descriptor.Receiver.CharacterID == candidateAgent.CharacterID)
                                {
                                    //if other bots are sharing that resource, reduce threshold so nothing is wasted
                                    baseThreshold -= (int)ammoStorage.ResourcePackAmmo.CostOfBullet;
                                }
                            }
                        }
                    }
                    float clampedThreshold = Mathf.Clamp01(baseThreshold / 100f);
                    float lowerThreshold = Mathf.Lerp(0f, 0.98f, clampedThreshold);
                    threshHold = Math.Min(lowerThreshold + ammoutCanGivePercent, 0.98f);
                }
                else
                {
                    ZiMain.log.LogWarning($"Tried share unknown resoruce pack ID {itemID}");
                    return false;
                }
                PlayerAmmoStorage candidateAmmoStorage = PlayerBackpackManager.GetBackpack(candidateAgent.Owner).AmmoStorage;
                float candidateScore = 0f;
                //I'm not sure if m_gearAvailability is the best way to go here, Switching to itemID.  Hopefully that doesn't cause any unexpected issues.
                switch (itemID)
                {
                    case 102:
                        if (candidateAgent.Damage.GetHealthRel() + ammoutCanGivePercent < threshHold) // Would you have less than threshHold after heals?
                        {
                            candidateScore = threshHold - candidateAgent.Damage.GetHealthRel(); // more damaged -> higher score
                        }
                        break;
                    case 101:
                        if (candidateAmmoStorage.StandardAmmo.RelInPack + ammoutCanGivePercent < threshHold &&
                            candidateAmmoStorage.SpecialAmmo.RelInPack + ammoutCanGivePercent < threshHold) // Would we overflow primary or secondary ammo?
                        {
                            candidateScore = threshHold - (candidateAmmoStorage.StandardAmmo.RelInPack + candidateAmmoStorage.SpecialAmmo.RelInPack) / 2f; // average of both ammo pools
                        }
                        break;
                    case 132:
                        if (candidateAgent.Damage.Infection * threshHold > ammoutCanGivePercent) // Would we overheal?
                        {
                            candidateScore = candidateAgent.Damage.Infection * threshHold; // score is how infected they are
                        }
                        break;
                    case 127:
                        if (candidateAgent.NeedToolAmmo() && candidateAmmoStorage.ClassAmmo.RelInPack + ammoutCanGivePercent < threshHold) // would we overflow tool ammo?
                        {
                            candidateScore = threshHold - candidateAmmoStorage.ClassAmmo.RelInPack; // how much ammo are they missing?
                        }
                        break;
                    default:
                        break;
                }
                if (candidateScore > 0f)//did they score at all?
                {
                    candidateScore = Mathf.Clamp01(candidateScore / threshHold);//make sure the score is not over 1 and reduce it slightly.
                    if (!candidateAgent.Owner.IsBot)
                    {
                        candidateScore = Mathf.Lerp(candidateScore, 1f, 0.5f);//prioritize players.
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
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool CheckBetterBotsDanger(GameObject agentGO)
        {
            BotRecorder component = agentGO.GetComponent<BotRecorder>();
            if (component != null)
            {
                return component.IsInDangerousSituation();
            }
            return false;
        }
    }
}

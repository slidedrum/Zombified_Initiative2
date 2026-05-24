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
using BotControl;
using ZombieTweak2;

namespace BotControl.Patches
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
        private enum ResourceIDs : uint
        {
            MediPack = 102,
            ToolPack = 127,
            AmmoPack = 101,
            DisinfectPack = 132,
        }
        [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.UpdateActionShareResoursePack))]
        [HarmonyPrefix]
        public static bool UpdateActionShareResoursePack(RootPlayerBotAction __instance, ref PlayerBotActionBase.Descriptor bestAction)
        {
            //This is based on the original mono decomp. Not a re-creation.  Still quite modified though.
            if (!__instance.m_shareResourceAction.IsTerminated())
                return false; //is there already share resource action?

            __instance.m_shareResourceAction.Prio = RootPlayerBotAction.m_prioSettings.ShareResource;

            if (!RootPlayerBotAction.CompareActionPrios(__instance.m_shareResourceAction, bestAction))
                return false; //Do we already have a more important action?
            
            if (__instance.m_bot.IsActionForbidden(__instance.m_shareResourceAction))
                return false; //Are we allowed to do this action?
            
            PlayerBackpack backpack = PlayerBackpackManager.GetBackpack(__instance.m_agent.Owner);
            BackpackItem backpackItem;

            if (!backpack.TryGetBackpackItem(InventorySlot.ResourcePack, out backpackItem))
                return false; //Do we have any resourcesActions to share?

            var itemID = backpackItem.ItemID;
            var block = ItemDataBlock.GetBlock(itemID);
            var itemInternalName = block.name;
            var actionKey = "Share" + itemInternalName;

            if (!(bool)zSlideComputer.ActionPermissions.ValueAt(actionKey))
                return false; //Do we have permission to share this item?

            if (ZiMain.HasBetterBots && BBCompat.CheckDanger(__instance.m_agent))
                return false; // skip sharing if bot is in danger

            PlayerAmmoStorage ammoStorage = backpack.AmmoStorage;
            float ammoutCanGivePercent = ammoStorage.ResourcePackAmmo.CostOfBullet / ammoStorage.ResourcePackAmmo.AmmoMaxCap;
            switch (itemID)
            {
                case (uint)ResourceIDs.MediPack: //medpack
                    ammoutCanGivePercent = AgentModifierManager.ApplyModifier(__instance.m_agent, AgentModifier.HealSupport, ammoutCanGivePercent);//This is artifacts!;
                    break;
                case (uint)ResourceIDs.ToolPack: //toolPack
                case (uint)ResourceIDs.AmmoPack: //Ammopack
                    ammoutCanGivePercent = AgentModifierManager.ApplyModifier(__instance.m_agent, AgentModifier.AmmoSupport, ammoutCanGivePercent);
                    break;
                case (uint)ResourceIDs.DisinfectPack: //Disinfect
                default: //something weird
                    break;
            }

            PlayerAgent chosenAgent = null;
            float topCandidateScore = 0f;
            foreach (PlayerAgent candidateAgent in PlayerManager.PlayerAgentsInLevel)
            {
                if (candidateAgent == null || !candidateAgent.Alive)
                    continue; //if the agent is null, or they're dead ignore them.
                if (candidateAgent != __instance.m_agent)
                {
                    float prio = __instance.m_shareResourceAction.Prio;
                    Vector3 position = candidateAgent.Position;

                    if (__instance.m_bot.ApplyRestrictionsToRootPosition(ref position, ref prio))
                        continue; //would moving to the candidate cause problems?
                }

                //float threshHold = 0.98f; //not needed fallback 
                if (!zSlideComputer.ActionPriorities.HasKey(actionKey))
                {
                    ZiMain.log.LogWarning($"Tried share unknown resoruce pack ID {itemID}");
                    return false;
                }
                int baseThreshold = (int)zSlideComputer.ActionPriorities.ValueAt(actionKey);
                foreach (var agent in PlayerManager.PlayerAgentsInLevel)
                {
                    //Check if any other bots are giving the same agent the same resource
                    if (!agent.Owner.IsBot)
                        continue;
                    PlayerAIBot aiBot = agent.gameObject.GetComponent<PlayerAIBot>();
                    var descriptor = aiBot?.m_rootAction?.ActionBase?.Cast<RootPlayerBotAction>()?.m_shareResourceAction;
                    if (descriptor == null || descriptor.Item == null)
                        continue;
                    if (descriptor.Item.ItemDataBlock.persistentID == itemID
                        && descriptor.Receiver.CharacterID == candidateAgent.CharacterID)
                    {
                        //if other bots are sharing that resource with the same target, reduce threshold so nothing is wasted
                        baseThreshold -= (int)ammoStorage.ResourcePackAmmo.CostOfBullet;
                    }
                }
                float clampedThreshold = Mathf.Clamp01(baseThreshold / 100f);
                float lowerThreshold = Mathf.Lerp(0f, 0.98f, clampedThreshold);
                float threshold = Math.Min(lowerThreshold + ammoutCanGivePercent, 0.98f);

                PlayerAmmoStorage candidateAmmoStorage = PlayerBackpackManager.GetBackpack(candidateAgent.Owner).AmmoStorage;
                float candidateScore = 0f;
                //I'm not sure if m_gearAvailability is the best way to go here, Switching to itemID.  Hopefully that doesn't cause any unexpected issues.
                switch (itemID)
                {
                    case (uint)ResourceIDs.MediPack:
                        if (candidateAgent.Damage.GetHealthRel() + ammoutCanGivePercent < threshold) // Would you have less than threshHold after heals?
                        {
                            candidateScore = threshold - candidateAgent.Damage.GetHealthRel(); // more damaged -> higher score
                        }
                        break;
                    case (uint)ResourceIDs.AmmoPack:
                        if (candidateAmmoStorage.StandardAmmo.RelInPack + ammoutCanGivePercent < threshold &&
                            candidateAmmoStorage.SpecialAmmo.RelInPack + ammoutCanGivePercent < threshold) // Would we overflow primary or secondary ammo?
                        {
                            candidateScore = threshold - (candidateAmmoStorage.StandardAmmo.RelInPack + candidateAmmoStorage.SpecialAmmo.RelInPack) / 2f; // average of both ammo pools
                        }
                        break;
                    case (uint)ResourceIDs.DisinfectPack:
                        //if (candidateAgent.Damage.Infection * threshold > ammoutCanGivePercent)
                        if (__instance.m_bot.IsPositionInfected(candidateAgent.Position) > 0.001f) //Are they taking infection damage?
                            break;
                        if (candidateAgent.Damage.Infection > lowerThreshold) // Would we overheal?
                            candidateScore = candidateAgent.Damage.Infection - lowerThreshold;
                            //candidateScore = candidateAgent.Damage.Infection * threshold; // score is how infected they are
                        break;
                    case (uint)ResourceIDs.ToolPack:
                        if (candidateAgent.NeedToolAmmo() && candidateAmmoStorage.ClassAmmo.RelInPack + ammoutCanGivePercent < threshold) // would we overflow tool ammo?
                        {
                            candidateScore = threshold - candidateAmmoStorage.ClassAmmo.RelInPack; // how much ammo are they missing?
                        }
                        break;
                    default:
                        break;
                }
                if (candidateScore > 0f)//did they score at all?
                {
                    candidateScore = Mathf.Clamp01(candidateScore / threshold);//make sure the score is not over 1 and reduce it slightly.
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

    }
}

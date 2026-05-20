using AIGraph;
using GameData;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using LevelGeneration;
using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zombified_Initiative;

namespace ZombieTweak2.Patches
{
    [HarmonyPatch]
    internal class PickupActionPatch
    {
        public static Il2CppReferenceArray<ItemDataBlock> rawConsumableItems
        {
            get
            {
                return ItemSpawnManager.m_itemDataPerInventorySlot?[(int)InventorySlot.Consumable];
            }
        }
        public static Il2CppReferenceArray<ItemDataBlock> consumableItems
        {
            get
            {
                var source = rawConsumableItems;

                if (ReferenceEquals(source, _rawConsumableItems))
                    return _consumableItems;

                _rawConsumableItems = source;

                _consumableItems = new Il2CppReferenceArray<ItemDataBlock>(
                    source
                        .Where(item =>
                            item != null &&
                            !unusedItems.Contains(item.name))
                        .ToArray());

                return _consumableItems;
            }
        }
        private static Il2CppReferenceArray<ItemDataBlock> _rawConsumableItems;
        private static Il2CppReferenceArray<ItemDataBlock> _consumableItems;
        public static List<string> consumableItemPublicNames
        {
            get
            {
                var items = consumableItems;

                if (ReferenceEquals(items, _consumableItems) &&
                    _consumableItemPublicNames != null)
                    return _consumableItemPublicNames;

                _consumableItemPublicNames = items
                    .Select(item => item.publicName)
                    .ToList();

                return _consumableItemPublicNames;
            }
        }
        private static List<string> _consumableItemPublicNames;
        public static List<string> consumableItemNames
        {
            get
            {
                var items = consumableItems;

                if (ReferenceEquals(items, _consumableItems) &&
                    _consumableItemNames != null)
                    return _consumableItemNames;

                _consumableItemNames = items
                    .Select(item => item.name)
                    .ToList();

                return _consumableItemNames;
            }
        }
        private static List<string> _consumableItemNames;

        private static List<ItemDataBlock> _fullItemList;
        private static Il2CppReferenceArray<ItemDataBlock> _lastConsumables;
        private static Il2CppReferenceArray<ItemDataBlock> _lastResourcePacks;
        public static List<ItemDataBlock> fullItemList
        {
            get
            {
                var consumables = consumableItems;
                var resourcePacks = ShareActionPatch.resourcePackItems;

                bool changed =
                    !ReferenceEquals(consumables, _lastConsumables) ||
                    !ReferenceEquals(resourcePacks, _lastResourcePacks);

                if (!changed && _fullItemList != null)
                    return _fullItemList;

                _lastConsumables = consumables;
                _lastResourcePacks = resourcePacks;

                _fullItemList = consumables
                    .Concat(resourcePacks)
                    .ToList();

                return _fullItemList;
            }
        }
        private static List<string> _fullItemNameList;
        public static List<string> fullItemNameList
        {
            get
            {
                var items = fullItemList;

                if (_fullItemNameList != null)
                    return _fullItemNameList;

                _fullItemNameList = items
                    .Select(x => x.name)
                    .ToList();

                return _fullItemNameList;
            }
        }
        private static List<string> _fullItemPublicNameList;
        public static List<string> fullItemPublicNameList
        {
            get
            {
                var items = fullItemList;

                if (_fullItemPublicNameList != null)
                    return _fullItemPublicNameList;

                _fullItemPublicNameList = items
                    .Select(x => x.publicName)
                    .ToList();

                return _fullItemPublicNameList;
            }
        }

        private static bool hasSetup = false;
        public static List<string> fullGlowStickNames = new() { "CONSUMABLE_GlowStick", "CONSUMABLE_GlowStick_Christmas", "CONSUMABLE_GlowStick_Halloween", "CONSUMABLE_GlowStick_Yellow" };
        public static List<string> shortGlowStickNames = new() { "Glow Stick", "Red Glow Stick", "Glow Stick Orange", "Glow Stick Yellow" };
        public static List<string> unusedItems = new() { "CONSUMABLE_Flare", "CONSUMABLE_Grenade", "CONSUMABLE_Portable_Barrier" , "CONSUMABLE_ShapedMine_Explosive" };
        //public static List<uint> GlowStickIds { get; internal set; }

        internal static void SetUp()
        {


            //foreach (var kvp in RootPlayerBotAction.s_itemBasePrios)
            //{
            //    uint id = kvp.Key;
            //    float priority = kvp.Value;
            //    string name = ItemDataBlock.GetBlockName(id);
            //    if (fullGlowStickNames.Contains(name))
            //        continue;
            //    zSlideComputer.ActionPriorities.AddNode(name, priority, "Pickup", defaultValue: priority).onChanged.Listen(SetInternalPriority, args: [name]);
            //    zSlideComputer.ActionPermissions.AddNode(name, null, "Pickup", defaultValue: null).onChanged.Listen(SetInternalPriority, args: [name]);
            //}
        }
        [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.GetItemPrio))]
        [HarmonyPrefix]
        public static bool GetItemPrio(RootPlayerBotAction __instance, InventorySlot itemSlot, uint itemID, ref float __result)
        {
            //This is a full re-implentation of the original method.  But without the hard coded values.
            //This approach allows me to support arbitrary item pickups not normally in the list, without breaking the logic.
            //Theoretically if there are a bunch of new items in the list, they could get into a "hot potato" loop.  but I'm calling that a "known shippable" for now.

            //var originalResult = __result;
            __result = 0f;
            //if (!(bool)zSlideComputer.ActionPermissions.ValueAt("Pickup"))
            //    return false;
            //if ((float)zSlideComputer.ActionPriorities.ValueAt("Pickup") == 0f)
            //    return false;
            string itemName = ItemDataBlock.GetBlockName(itemID);
            string actionKey = "Pickup" + itemName;
            if (zSlideComputer.ActionPermissions.HasKey(actionKey) && !(bool)zSlideComputer.ActionPermissions.ValueAt(actionKey)) // if there is a specific setting for this item, and it's false, don't pick it up at all (prio 0)
                return false;
            if (zSlideComputer.ActionPriorities.HasKey(actionKey) && 0 == (int)zSlideComputer.ActionPriorities.ValueAt(actionKey)) // if there is a specific setting for this item, and it's false, don't pick it up at all (prio 0)
                return false;
            //if (!zSlideComputer.enabledItemPrios.ContainsKey(itemID) || !zSlideComputer.enabledItemPrios[itemID])
            //    return false;
            ItemDataBlock itemDataBlock;
            if (ItemDataBlock.s_blockByID.ContainsKey(itemID))
            {
                itemDataBlock = ItemDataBlock.s_blockByID[itemID];
            }
            else
            {
                ZiMain.log.LogError($"Tried to get priority for unknow id: {itemID}");
                return false;
            }
            __result = 0f;
            float basePriority = (float)zSlideComputer.ActionPriorities.ValueAt(actionKey);
            //float basePriority = RootPlayerBotAction.s_itemBasePrios[itemID];

            List<PlayerAgent> playerAgentsList = PlayerManager.PlayerAgentsInLevel.ToArray().ToList();

            float highestAmmoCap = 0f;
            float currentTotalAmmoOfOtherBotsNotThisBot = 0f;
            float priorityFloor = 0.15f;
            int foundMe = 0;
            int otherAgents = 0;
            foreach (PlayerAgent agent in playerAgentsList)
            {
                if (agent.CharacterID == __instance.m_agent.CharacterID)
                {
                    foundMe++;
                    continue;
                }
                otherAgents++;
                PlayerBackpack agentBackpack = PlayerBackpackManager.GetBackpack(agent.Owner);
                BackpackItem agentItem = null;
                if (agentBackpack != null)
                    if (!agentBackpack.TryGetBackpackItem(itemSlot, itemID, out agentItem))
                        continue;
                if (agentItem == null)
                    continue;
                float ammoCap = 1;
                float agentAmmo = 0;
                InventorySlotAmmo pack = null;
                switch (itemSlot)
                {
                    case InventorySlot.ResourcePack:
                        pack = agentBackpack?.AmmoStorage?.ResourcePackAmmo;
                        priorityFloor = 0.5f;
                        break;
                    case InventorySlot.Consumable:
                        pack = agentBackpack?.AmmoStorage?.ConsumableAmmo;
                        priorityFloor = 0.25f;
                        break;
                    default:
                        break;
                }
                if (pack != null)
                {
                    ammoCap = pack.AmmoMaxCap;
                    agentAmmo = pack.AmmoInPack;
                }
                else
                {
                    ammoCap = 1;
                    agentAmmo = 1;
                }
                highestAmmoCap = Math.Max(ammoCap, highestAmmoCap);
                currentTotalAmmoOfOtherBotsNotThisBot += agentAmmo;
            }
            if (foundMe == 0)
            {
                var alive = __instance.m_agent is UnityEngine.Object uObj && uObj;
                ZiMain.log.LogError($"Could not find this agent! This will probably cause issues. {alive}");
            }
            float maxOtherTotal = -1f;
            float fillFactor = -1f;
            float minPriority = -1f;
            if (highestAmmoCap > 0.0f && otherAgents > 0)
            {
                maxOtherTotal = playerAgentsList.Count * highestAmmoCap;
                fillFactor = currentTotalAmmoOfOtherBotsNotThisBot / maxOtherTotal;
                minPriority = basePriority * priorityFloor;

                __result = Mathf.Lerp(basePriority, minPriority, fillFactor);
            }
            else
            {
                __result = basePriority;
            }
            return false;
        }
        [HarmonyPatch(typeof(PlayerBotActionCollectItem), nameof(PlayerBotActionCollectItem.OnTravelActionEvent))]
        [HarmonyPrefix]
        public static bool OnTravelActionEventPatch(PlayerBotActionCollectItem __instance, PlayerBotActionBase.Descriptor descBase)
        { //This is supposed to fix bots not always being able to pick up items, but I don't really remember how it works.
            if (descBase != __instance.m_travelAction)
            {
                __instance.PrintError("Rogue action.");
            }
            if (descBase.IsTerminated())
            {
                __instance.m_travelAction = null;
                if (descBase.Status == PlayerBotActionBase.Descriptor.StatusType.Successful)
                {
                    __instance.StartTransfer();
                    return false;
                }
                if (__instance.VerifyCurrentPosition())
                {
                    ZiMain.log.LogWarning("VerifyCurrentPosition - travel event failed, but within range anyway!");
                    __instance.StartTransfer();
                    return false;
                }
                ZiMain.log.LogWarning("VerifyCurrentPosition - travel event failed");
                __instance.m_desc.SetCompletionStatus(PlayerBotActionBase.Descriptor.StatusType.Failed);
            }
            return false;
        }
        [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.UpdateActionCollectItem))]
        [HarmonyPrefix]
        public static bool UpdateActionCollectItemReCreation(RootPlayerBotAction __instance, ref PlayerBotActionBase.Descriptor bestAction)
        {
            // TODO cut this out completely?
            // TODO check why there are some unused vars in here, is this actually accurate to the real game?
            // all this does is change it so it checks against agent position not "epicenter" position.
            // Local temporaries that match responsibilities seen in the decompiled code

            //bool allowed = (bool)zSlideComputer.ActionPermissions.ValueAt("Pickup"); //This gets commented out because we may have an idividual item override.  that's handed under get item priority.
            //if (!allowed)
            //    return false;

            PlayerBotActionCollectItem.Descriptor collectItemActionDescriptor = __instance.m_collectItemAction;
            Item chosenItem = null;
            float bestItemPrio = 0.0f;
            AIG_CourseNode activityNode = null;
            Vector3 activityEpicenter = Vector3.zero;
            BackpackItem foundBackpackItem = null;

            // If there is an active collect action and it's NOT terminated -> do nothing
            if (collectItemActionDescriptor != null)
            {
                if (!collectItemActionDescriptor.IsTerminated())
                    return false;
                collectItemActionDescriptor.Prio = (float)zSlideComputer.ActionPriorities.ValueAt("Pickup");
            }

            // If we already have a best action and the collect action is present:
            // if the collect action's priority is less-or-equal to the current best, don't consider it.
            if (bestAction != null)
            {
                if (collectItemActionDescriptor == null)
                {
                    // decompiled flow jumped to end in this case; just return (no change to bestAction)
                    return false;
                }

                if (collectItemActionDescriptor.Prio <= bestAction.Prio)
                    return false;
            }

            // If we have a backpack, and it already has slot 8 occupied -> don't pick up anything.
            if (__instance.m_backpack != null)
            {
                if (__instance.m_backpack.HasBackpackItem(InventorySlot.InLevelCarry))
                    return false;
            }

            // If the bot exists and the action is forbidden for that bot -> don't proceed
            if (__instance.m_bot != null && __instance.m_bot.IsActionForbidden(collectItemActionDescriptor))
                return false;

            // Get the activity epicenter and a course node (area) where we should search
            if (!__instance.GetActivityEpicenter(out activityNode, out activityEpicenter))
                return false;
            activityEpicenter = __instance.m_bot.transform.position; //TODO patch GetActivityEpicenter
            activityNode = __instance.m_bot.Agent.CourseNode; // This is what's different
            // Prepare temporary reservations (object + position) using static temp objects on RootPlayerBotAction
            if (__instance.m_agent != null && RootPlayerBotAction.s_tempObjReservation != null)
            {
                RootPlayerBotAction.s_tempObjReservation.CharacterID = __instance.m_agent.CharacterID;
            }

            if (collectItemActionDescriptor != null && RootPlayerBotAction.s_tempObjReservation != null)
            {
                RootPlayerBotAction.s_tempObjReservation.Prio = collectItemActionDescriptor.Prio;
            }

            if (__instance.m_agent != null && RootPlayerBotAction.s_tempPosReservation != null)
            {
                RootPlayerBotAction.s_tempPosReservation.CharacterID = __instance.m_agent.CharacterID;
            }

            if (collectItemActionDescriptor != null && RootPlayerBotAction.s_tempPosReservation != null)
            {
                RootPlayerBotAction.s_tempPosReservation.Prio = collectItemActionDescriptor.Prio;
            }

            // We'll search storage containers that are attached to the discovered course node
            int containerIndex = 0;
            int chosenContainerIndex = -1;
            LG_ResourceContainer_Storage chosenContainer = null;
            Vector3 candidateRootPos = Vector3.zero;
            float candidateRadius = 0f;

            // Prepare some static values
            float collectSearchDistance = RootPlayerBotAction.s_collectItemSearchDistance;
            float collectStandDistance = RootPlayerBotAction.s_collectItemStandDistance;
            float collectSearchDistanceSqr = collectSearchDistance * collectSearchDistance;

            // If the area node is null we won't find containers to search
            if (activityNode != null && activityNode.MetaData?.StorageContainers != null)
            {
                List<LG_ResourceContainer_Storage> storageList = activityNode.MetaData.StorageContainers.ToArray().ToList();
                // iterate through storage containers
                for (containerIndex = 0; containerIndex < storageList.Count; containerIndex++)
                {
                    var container = storageList[containerIndex];
                    if (container == null)
                        continue;

                    // container may have an internal items array/list; iterate items in the container
                    // The decompiled code walked a specific internal array; here we use the container's public API
                    // to find candidate item GameObjects and Items.
                    // First: check the container GameObject / weak resource container to see if it is "active" / valid
                    var containerGO = container.gameObject;
                    if (containerGO == null)
                        continue;

                    var weakContainerComp = containerGO.GetComponent<LG_WeakResourceContainer>();
                    if (weakContainerComp == null)
                        continue;

                    // In the decompiled code there was a check on a char/flag inside the component.
                    // We preserve the logic: if the weak container is not "enabled/open" skip.
                    if (!weakContainerComp.ISOpen) // <-- IsOpen is a guessed property name mapping to that char flag
                        continue;

                    // Reserve this container temporarily and check whether the reservation collides with existing reservations
                    if (RootPlayerBotAction.s_tempObjReservation == null)
                        continue;

                    RootPlayerBotAction.s_tempObjReservation.Object = containerGO;

                    // If the object/container is reserved by PlayerManager, skip it
                    if (PlayerManager.Current == null)
                        continue;

                    if (PlayerManager.Current.IsObjectReserved(RootPlayerBotAction.s_tempObjReservation))
                        continue;

                    // set the collect action's target container tentatively
                    if (collectItemActionDescriptor == null)
                        continue;

                    collectItemActionDescriptor.TargetContainer = container;

                    // If bot.TestFailureRetry returns false, skip container
                    if (__instance.m_bot == null)
                        continue;

                    if (!__instance.m_bot.TestFailureRetry(collectItemActionDescriptor))
                        continue;

                    // get container transform position
                    Transform containerTransform = container.GetComponent<Transform>();
                    if (containerTransform == null)
                        continue;

                    Vector3 containerPos = containerTransform.position;

                    // compute squared distance from activity epicenter to container position
                    Vector3 diff = containerPos - activityEpicenter;
                    float sqrDist = diff.sqrMagnitude;

                    // Only consider containers within the search radius
                    if (sqrDist >= collectSearchDistanceSqr)
                        continue;

                    // Check whether any human players are near the container (don't pick up if humans nearby)
                    if (__instance.IsAnyHumanPlayerNear(containerPos, 4.0f))
                        continue;

                    // Compute a stand position in front of the container using container up vector and stand distance
                    Vector3 itemPos = containerTransform.position;
                    Vector3 up = containerTransform.up;
                    Vector3 standOffset = up * collectStandDistance;
                    Vector3 standCandidate = itemPos - standOffset;

                    // Snap to nav and get radius (SnapPositionToNav will write rootPos and radius)
                    Vector3 rootPos;
                    float radius;
                    if (!__instance.SnapPositionToNav(standCandidate, out rootPos))
                        continue;

                    // populate the temp position reservation
                    if (RootPlayerBotAction.s_tempPosReservation == null)
                        continue;

                    RootPlayerBotAction.s_tempPosReservation.Position = rootPos; // note: decomp used field shuffling
                                                                                 //RootPlayerBotAction.s_tempPosReservation.Radius = radius;

                    // If position is reserved, skip
                    if (PlayerManager.Current == null)
                        continue;
                    if (PlayerManager.Current.IsPositionReserved(RootPlayerBotAction.s_tempPosReservation))
                        continue;

                    // Ask AIBot to apply restrictions to root position (may change priority)
                    if (__instance.m_bot == null)
                        continue;

                    float prioRef = collectItemActionDescriptor.Prio;
                    if (__instance.m_bot.ApplyRestrictionsToRootPosition(ref rootPos, ref prioRef))
                    {
                        // ApplyRestrictionsToRootPosition returning true in our naming means "rejected" in the decompiled control flow
                        // (decompiled returned early if it returned true), so skip this container if it returns true.
                        continue;
                    }

                    // Test danger zones around candidate root position
                    Vector3 posForDanger = new Vector3(rootPos.x, rootPos.y, rootPos.z);
                    if (!__instance.m_bot.TestDangerZones(posForDanger, AIDangerZone.SeverityEnum.CertainDeath, out AIDangerZone.SeverityEnum danger))
                        continue;

                    // At this point the container is valid. Iterate the items inside the container and evaluate each Item.
                    // The original iterated over a coroutine list of easeLocalScaleRoutine entries (child gameobjects).
                    // We'll traverse game objects / child items: find components of type Item in container children.
                    // This mirrors the decompiled behavior of scanning children and calling GetComponentInParent<Item>().
                    Component[] potentialItems = container.GetComponentsInChildren<Component>(true);
                    if (potentialItems == null)
                        continue;

                    // iterate potentialItems like the decompiled code scanned coroutines entries
                    int counter = 0;
                    foreach (var comp in potentialItems)
                    {
                        // get actual UnityEngine.Object and check not null
                        if (comp == null)
                        {
                            counter++;
                            continue;
                        }

                        // In the decomp they did a series of type checks / generic checks; here we attempt to find Item in parent
                        Item itemComponent = comp.GetComponentInParent<Item>();
                        if (itemComponent == null)
                        {
                            counter++;
                            continue;
                        }

                        if (itemComponent.ItemDataBlock == null)
                        {
                            counter++;
                            continue;
                        }

                        // Check whether the bot knows how to use this item (by gear CRC / id)
                        uint gearCRC = itemComponent.pItemData.itemID_gearCRC;
                        if (!PlayerAIBot.KnowsHowToUseItem(gearCRC))
                        {
                            counter++;
                            continue;
                        }

                        // compute the item priority for picking it up
                        ItemDataBlock dataBlock = itemComponent.ItemDataBlock;
                        if (dataBlock == null)
                        {
                            counter++;
                            continue;
                        }

                        float itemPrio = __instance.GetItemPrio(dataBlock.inventorySlot, itemComponent.pItemData.itemID_gearCRC);

                        // if this item's prio is not greater than the currently best, skip
                        if (itemPrio <= bestItemPrio)
                        {
                            counter++;
                            continue;
                        }

                        // If item type exists and we have a backpack, compare against an existing backpack item slot
                        if (__instance.m_backpack != null)
                        {
                            BackpackItem existing;
                            if (__instance.m_backpack.TryGetBackpackItem(dataBlock.inventorySlot, out existing))
                            {
                                // If the existing backpack item has equal-or-better priority, skip this item
                                float existingPrio = __instance.GetItemPrio(dataBlock.inventorySlot, existing.ItemID);
                                if (itemPrio <= existingPrio)
                                {
                                    counter++;
                                    continue;
                                }
                            }
                        }

                        // Found a better item candidate — remember it
                        chosenItem = itemComponent;
                        bestItemPrio = itemPrio;
                        chosenContainer = container;
                        // save final root pos/radius to assign to action if accepted
                        candidateRootPos = rootPos;
                        //candidateRadius = radius;

                        counter++;
                    } // end foreach comp

                    // If we found a chosenItem in this container, assign to the collect action and finish
                    if (chosenItem != null)
                    {
                        // assign the target item and container to the collect descriptor
                        collectItemActionDescriptor.TargetItem = chosenItem;
                        collectItemActionDescriptor.TargetContainer = chosenContainer;

                        // store candidateRootPos into collect.TargetPosition (match decompiled axis mapping)
                        collectItemActionDescriptor.TargetPosition = candidateRootPos;
                        //collect.TargetPosition.fields.y = candidateRootPos.y;
                        //collect.TargetPosition.fields.z = candidateRootPos.z;

                        collectItemActionDescriptor.Haste = 0.5f;

                        // set the bestAction reference to the collect action we built
                        bestAction = collectItemActionDescriptor;
                        return false;
                    }
                    // else: continue to next container
                } // end for each container
            } // end if activityNode != null

            // No valid pick found -> nothing to set; just return
            return false;
        }
    }
}

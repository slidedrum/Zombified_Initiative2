using AIGraph;
using Enemies;
using FluffyUnderware.Curvy.Generator;
using Il2CppSystem.Linq.Expressions;
using LevelGeneration;
using Player;
using PlayFab.AdminModels;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ZombieTweak2;
using ZombieTweak2.zMenu;

namespace Zombified_Initiative
{
    public static class zSearch
    {
        //this class is for finding stuff inside the world.
        public static Dictionary<int,FindableObject> FindableObjects = new();
        public static float foundDistance = 10f;

        public static GameObject GetClosestObjectInLookDirection(Transform baseTransform,List<GameObject> candidates,float maxAngle = 180f, Vector3? candidateOffset = null, Vector3? baseOffset = null)
        {
            //TODO add some optional leeway for very close objects
            candidateOffset = candidateOffset ?? Vector3.zero;
            baseOffset = baseOffset ?? Vector3.zero;
            if (baseTransform == null || candidates == null || candidates.Count == 0)
                return null;

            Vector3 basePosition = (Vector3)(baseTransform.position + baseOffset);
            Vector3 lookDirection = baseTransform.forward;

            GameObject bestCanidate = null;
            float bestAngle = maxAngle;

            foreach (GameObject candidate in candidates)
            {
                if (candidate == null) continue;
                Vector3 candidatePosition = (Vector3)(candidate.transform.position + candidateOffset);
                Vector3 targetDirection = (candidatePosition - basePosition).normalized;
                float canidateAngle = Vector3.Angle(lookDirection, targetDirection);

                if (canidateAngle < bestAngle)
                {
                    bestAngle = canidateAngle;
                    bestCanidate = candidate;
                }
            }

            // enforce canidateAngle cutoff
            if (bestCanidate != null && bestAngle <= maxAngle)
                return bestCanidate;

            return null;
        }
        public static List<GameObject> GetObjectsWithComponentInRadius<T>(Vector3 position,float searchRadius) where T : Component
        {
            List<GameObject> results = new List<GameObject>();

            Collider[] nearby = Physics.OverlapSphere(position, searchRadius);
            foreach (var col in nearby)
            {
                T comp = col.GetComponentInParent<T>();
                if (comp != null)
                {
                    results.Add(comp.gameObject);
                }
            }
            return results;
        }
        public static List<GameObject> GetGameObjectsWithLookDirection<T>(Transform source,float searchRadius = 3,float rayDistance = 10000f) where T : Component
        {
            Ray ray = new Ray(source.position, source.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
            {
                //zDebug.ShowDebugSphere(hit.point, searchRadius);
                return GetObjectsWithComponentInRadius<T>(hit.point, searchRadius);
            }
            return new List<GameObject>();
        }
        public static void Update() 
        {
            foreach(var agent in PlayerManager.PlayerAgentsInLevel)
            {
                UpdateFindables(agent);
                Updatefinds(agent);
                CleanFindables(agent);
            }
        }
        public static List<ItemInLevel> GetItemsFromLocker(LG_ResourceContainer_Storage storage)
        {
            List<ItemInLevel> ret = new();
            var interactions = storage.PickupInteractions;
            foreach (Interact_Base interaction in interactions)
            {
                Transform current = interaction.gameObject.transform;
                int depth = 0;
                ItemInLevel item = null;
                while (current != null && depth < 7)
                {
                    item = current.GetComponent<ItemInLevel>();
                    if (item != null)
                        break;
                    current = current.parent;
                    depth++;
                }
                if (item == null)
                    continue;
                ret.Add(item);
            }
            return ret;
        }
        public static void UpdateFindables(PlayerAgent agent)
        {
            //update enemy findables
            AIG_CourseNode node = agent.CourseNode;
            if (node == null)
                return;
            Il2CppSystem.Collections.Generic.List<EnemyAgent> enemyAgents = new();
            AIG_CourseNode.GetEnemiesInNodes(node, 2, enemyAgents);
            foreach (var enemy in enemyAgents)
            {
                int instanceId = enemy.gameObject.GetInstanceID();
                if (FindableObjects.ContainsKey(instanceId))
                    continue;
                FindableObject findable = new FindableObject { gameObject = enemy.gameObject, courseNode = node, type = typeof(EnemyAgent), found = false};
                FindableObjects[instanceId] = findable;
            }
            //update locker findables
            Il2CppSystem.Collections.Generic.List<LG_ResourceContainer_Storage> lockers = new();
            lockers = node.MetaData.StorageContainers;
            foreach (var locker in lockers)
            {
                int instanceId = locker.gameObject.GetInstanceID();
                if (!FindableObjects.ContainsKey(instanceId))
                {
                    FindableObject findable = new FindableObject { gameObject = locker.gameObject, courseNode = node, type = typeof(LG_ResourceContainer_Storage), found = false };
                    FindableObjects[instanceId] = findable;
                }
                //update item findables
                LG_WeakResourceContainer Container = locker.gameObject.GetComponent<LG_WeakResourceContainer>();
                if (Container != null && Container.ISOpen)
                {
                    LG_ResourceContainer_Storage storage = Container.gameObject.GetComponent<LG_ResourceContainer_Storage>();
                    if (storage == null)
                        continue;
                    var items = GetItemsFromLocker(storage);
                    foreach (ItemInLevel item in items)
                    {
                        
                        if (item == null)
                            continue;
                        instanceId = item.GetInstanceID();
                        if (FindableObjects.ContainsKey(instanceId))
                        {
                            uint currentId = FindableObjects[instanceId].gameObject.GetComponent<ItemInLevel>().ItemDataBlock.persistentID;
                            uint newId = item.ItemDataBlock.persistentID;
                            if (currentId == newId)
                            {
                                continue;
                            }
                        }
                        FindableObject itemFindable = new FindableObject { gameObject = item.gameObject, courseNode = node, type = typeof(ItemInLevel), found = false};
                        FindableObjects[instanceId] = itemFindable;
                    }
                }
            }
        }
        private static int totalFound = 0;
        public static void Updatefinds(PlayerAgent agent)
        {
            totalFound = 0;
            AIG_CourseNode node = agent.CourseNode;
            if (node == null)
                return;
            foreach (var kvp in FindableObjects)
            {
                int instanceId = kvp.Key;
                FindableObject findable = kvp.Value;
                if (findable.found == true)
                {
                    totalFound++;
                    continue;
                }
                GameObject gameObject = findable.gameObject;
                if (Vector3.Distance(findable.gameObject.transform.position, agent.Position) < foundDistance)
                {
                    findable.found = true;
                    totalFound++;
                    ZiMain.log.LogInfo($"Found object {findable.type}! {gameObject.name}");
                }
            }
        }
        public static void CleanFindables(PlayerAgent agent)
        {
            List<int> objectToRemove = new List<int>();
            foreach (var obj in FindableObjects)
                if (!obj.Value.gameObject.activeInHierarchy)
                    objectToRemove.Add(obj.Key);
            foreach (var index in objectToRemove)
                FindableObjects.Remove(index);
        }
    }
    public class FindableObject
    {
        public GameObject gameObject;
        public AIG_CourseNode courseNode;
        public Type type;
        public bool found;
    }

}

using AIGraph;
using Il2CppInterop.Runtime;
using LevelGeneration;
using Player;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using ZombieTweak2;
using static BoundingBox;
using static PUI_CommunicationButton;

namespace Zombified_Initiative
{
    public static class zSearch
    {
        //this class is for finding stuff inside the world.
        public static Dictionary<int,FindableObject> FindableObjects = new();
        public static HashSet<AIG_CourseNode> CheckedNodes = new();
        public static float foundDistance = 10f;
        private static PlayerAgent localPlayer = null;
        private static PlayerPingTarget[] allPingTargets;
        private static List<PingTargetParent> pingTargetGroups;
        private static int pingMapCellSise = 5;
        private static Dictionary<Vector2, List<FindableObject>> findbleObjectMap = new();
        private static Dictionary<eNavMarkerStyle, List<PlayerPingTarget>> pingGroups = new();
        private static List<Transform> pingTransforms = new();

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
        public static List<GameObject> GetObjectsWithComponentInRadius(Vector3 position, float searchRadius, List<Il2CppSystem.Type> types)
        {
            List<GameObject> results = new List<GameObject>();

            Collider[] nearby = Physics.OverlapSphere(position, searchRadius);
            foreach (var col in nearby)
            {
                foreach (var type in types) 
                {
                    Component comp = col.GetComponentInParent(type);
                    if (comp != null)
                    {
                        results.Add(comp.gameObject);
                    } 
                }
            }
            return results;
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
        public static List<GameObject> GetGameObjectsWithLookDirection(Transform source, List<Il2CppSystem.Type> types, float searchRadius = 3, float rayDistance = 10000f)
        {
            Ray ray = new Ray(source.position, source.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
            {
                //zDebug.ShowDebugSphere(hit.point, searchRadius);
                return GetObjectsWithComponentInRadius(hit.point, searchRadius, types);
            }
            return new List<GameObject>();
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
            if (localPlayer == null)
                localPlayer = PlayerManager.GetLocalPlayerAgent();
            if (localPlayer == null)
                return;
            if (localPlayer.Owner.refSessionMode != SNetwork.eReplicationMode.Playing)
            {
                if (FindableObjects.Count > 0)
                    FindableObjects.Clear();
                return;
            }
            foreach (var agent in PlayerManager.PlayerAgentsInLevel)
            {
                UpdateFindables(agent);
                Updatefinds(agent);
            }
        }
        public class PingTargetParent
        {
            public GameObject baseGo;
            public eNavMarkerStyle PingTargetStyle;
            public List<PlayerPingTarget> pings = new();
        }
        public static void OnFactoryBuildDone()
        {
            allPingTargets = UnityEngine.Object.FindObjectsOfType<PlayerPingTarget>();  //Find all ping targets in the level.
            pingTransforms = new();
            foreach (PlayerPingTarget ping in allPingTargets)
            {
                if (!pingGroups.TryGetValue(ping.PingTargetStyle, out var list))
                {
                    list = new List<PlayerPingTarget>();
                    pingGroups[ping.PingTargetStyle] = list;
                }
                list.Add(ping);
                var parent = ping.transform;
                Type type = null;
                switch (ping.PingTargetStyle)
                {
                    case eNavMarkerStyle.LocationBeacon:
                        break;

                    case eNavMarkerStyle.PlayerPingLookat:
                        break;

                    case eNavMarkerStyle.PlayerPingEnemy:
                        break;

                    case eNavMarkerStyle.PlayerPingAmmo:
                        type = typeof(LG_GenericTerminalItem);
                        break;

                    case eNavMarkerStyle.PlayerPingHealth:
                        type = typeof(LG_GenericTerminalItem);
                        break;

                    case eNavMarkerStyle.PlayerPingLoot:
                        type = typeof(LG_PickupItem_Sync);
                        break;

                    case eNavMarkerStyle.PlayerPingTerminal:
                        type = typeof(LG_GenericTerminalItem);
                        break;

                    case eNavMarkerStyle.PlayerPingCaution:
                        break;

                    case eNavMarkerStyle.PlayerPingHSU: //
                        break;

                    case eNavMarkerStyle.PlayerPingDoor:
                        type = typeof(LG_GenericTerminalItem);
                        break;

                    case eNavMarkerStyle.PlayerPingResourceLocker:
                        type = typeof(LG_GenericTerminalItem);
                        break;

                    case eNavMarkerStyle.PlayerPingResourceBox:
                        type = typeof(LG_GenericTerminalItem);
                        break;

                    case eNavMarkerStyle.PlayerInCompass:
                        break;

                    case eNavMarkerStyle.PlayerPingSign:
                        break;

                    case eNavMarkerStyle.LocationBeaconNoText:
                        break;

                    case eNavMarkerStyle.TerminalPing:
                        break;

                    case eNavMarkerStyle.PlayerPingGenerator:
                        type = typeof(LG_GenericTerminalItem);
                        break;

                    case eNavMarkerStyle.PlayerPingDisinfection:
                        type = typeof(LG_GenericTerminalItem);
                        break;

                    case eNavMarkerStyle.PlayerPingCarryItem:
                        type = typeof(LG_GenericTerminalItem);
                        break;

                    case eNavMarkerStyle.PlayerPingConsumable:
                        type = typeof(LG_PickupItem_Sync);
                        break;

                    case eNavMarkerStyle.PlayerPingPickupObjectiveItem:
                        type = typeof(LG_GenericTerminalItem);
                        break;

                    case eNavMarkerStyle.PlayerPingToolRefill:
                        type = typeof(LG_GenericTerminalItem);
                        break;

                    case eNavMarkerStyle.PlayerPingSecurityDoor:
                        type = typeof(LG_GenericTerminalItem);
                        break;

                    case eNavMarkerStyle.PlayerPingBulkheadDoor:
                        type = typeof(LG_GenericTerminalItem);
                        break;

                    case eNavMarkerStyle.PlayerPingApexDoor:
                        type = typeof(LG_GenericTerminalItem);
                        break;

                    case eNavMarkerStyle.PlayerPingBloodDoor:
                        type = typeof(LG_GenericTerminalItem);
                        break;

                    case eNavMarkerStyle.PlayerPingSecurityCheckpointDoor:
                        type = typeof(LG_GenericTerminalItem);
                        break;

                    case eNavMarkerStyle.PlayerPingBulkheadCheckpointDoor:
                        type = typeof(LG_GenericTerminalItem);
                        break;

                    case eNavMarkerStyle.PlayerPingApexCheckpointDoor:
                        type = typeof(LG_GenericTerminalItem);
                        break;

                    case eNavMarkerStyle.PlayerPingBloodCheckpointDoor:
                        type = typeof(LG_GenericTerminalItem);
                        break;

                    case eNavMarkerStyle.PlayerPingBulkheadDC:
                        type = typeof(LG_GenericTerminalItem);
                        break;

                    default:
                        break;
                }

                if (type != null)
                {
                    var originalParrent = parent;
                    while(parent.GetComponent(Il2CppType.From(type)) == null)
                    {
                        if (parent.parent == null)
                        {
                            parent = originalParrent;
                            break;
                        }
                        parent = parent.parent;
                    }
                    if (!pingTransforms.Contains(parent))
                    {
                        bool found = false;
                        for (int i = 0; i < pingTransforms.Count; i++)
                        {
                            if (pingTransforms[i].IsChildOf(parent))
                            {
                                pingTransforms[i] = parent;
                                found = true;
                                break;
                            }
                            if (parent.IsChildOf(pingTransforms[i]))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            pingTransforms.Add(parent);
                        }
                    }
                }
            }
            foreach (var p in pingTransforms)
            {
                BoundingBox box = new(p.gameObject, BoundingSource.Renderers);
                box.ShowDebug();
            }
        }
        //public static void OnFactoryBuildDone()
        //{
        //    allPingTargets = UnityEngine.Object.FindObjectsOfType<PlayerPingTarget>();  //Find all ping targets in the level.
        //    pingTargetGroups = new();
        //    foreach (var ping in allPingTargets) //Loop through all of them to group them together into each item.
        //    {
        //        if (ping == null || ping.gameObject == null) continue;

        //        eNavMarkerStyle style = ping.PingTargetStyle;
        //        GameObject currentGO = ping.gameObject;
        //        bool foundParrent = false;
        //        foreach(var pingTarget in pingTargetGroups) //Figure out if they are part of an existing group.
        //        {
        //            if (pingTarget.PingTargetStyle != style) continue; //not the same type, can't be in the same group.
        //            if (ping.gameObject.transform.IsChildOf(pingTarget.baseGo.transform)) //Part of an existing group.
        //            {
        //                pingTarget.pings.Add(ping);
        //                foundParrent = true;
        //                break;
        //            }
        //            else if (pingTarget.baseGo.transform.IsChildOf(ping.gameObject.transform.parent)) // An existing group is part of this ping
        //            {
        //                pingTarget.pings.Add(ping);
        //                pingTarget.baseGo = ping.gameObject.transform.parent.gameObject;
        //                foundParrent = true;
        //                break;
        //            }
        //        }
        //        if (!foundParrent) // Not part of an existing group.
        //        {
        //            pingTargetGroups.Add(new PingTargetParent()
        //            {
        //                baseGo = ping.gameObject.transform.parent.gameObject,
        //                PingTargetStyle = style,
        //                pings = new List<PlayerPingTarget> { ping }
        //            });
        //        }
        //    }
        //    foreach (var group in pingTargetGroups) // Find the highest level game object.  might not be needed?
        //    {
        //        if (group.pings == null || group.pings.Count == 0)
        //            continue;

        //        // Start from the first ping’s transform
        //        Transform common = group.pings[0].transform;

        //        // Iterate upward until this transform contains ALL ping objects
        //        bool foundCommon = false;
        //        while (common != null)
        //        {
        //            bool allContained = true;
        //            foreach (var ping in group.pings)
        //            {
        //                if (!ping.transform.IsChildOf(common))
        //                {
        //                    allContained = false;
        //                    break;
        //                }
        //            }

        //            if (allContained)
        //            {
        //                foundCommon = true;
        //                break;
        //            }

        //            common = common.parent;
        //        }

        //        if (foundCommon && common != null)
        //            group.baseGo = common.gameObject;
        //    }
        //    foreach (var group in pingTargetGroups) //Add the ping target to the map.
        //    {
        //        Vector3 centerpoint = Vector3.zero;
        //        List<Vector3> points = new List<Vector3>();
        //        foreach (var ping in group.pings) 
        //        {
        //            BoundingBox box = new(ping.gameObject,BoundingSource.Renderers);
        //            //box.ShowDebug();
        //            foreach(var point in box.GetCorners())
        //            {
        //                points.Add(point);
        //            }
        //        }
        //        if (points.Count == 0)
        //        {
        //            continue;
        //        }
        //        else if (points.Count == 1)
        //        {
        //            centerpoint = points[0];
        //        }
        //        else
        //        {
        //            Bounds bounds = new Bounds(points[0], points[1]);
        //            foreach (var  point in points)
        //            {
        //                bounds.Encapsulate(point);
        //            }
        //            centerpoint = bounds.center;
        //        }
        //        var cell = new Vector2Int(
        //            Mathf.FloorToInt(centerpoint.x / pingMapCellSise),
        //            Mathf.FloorToInt(centerpoint.z / pingMapCellSise)
        //        );
        //        if (!findbleObjectMap.ContainsKey(cell))
        //        {
        //            findbleObjectMap[cell] = new List<FindableObject>();
        //        }
        //        findbleObjectMap[cell].Add(new FindableObject()
        //        {
        //            gameObjects = group.pings.Select(p => p.gameObject).ToList(),
        //            type = typeof(PlayerPingTarget),
        //            found = false,
        //            centerPoint = centerpoint,
        //        });
        //    }
        //    //The base game object bounding box for some reason is offset for lockers, no idea why.  Actually ping target game objects are not.
        //}
        private static PingTargetParent FindPingTargetByname(string name)
        {
            foreach (var ping in pingTargetGroups)
            { 
                if (ping.baseGo.name.Contains(name))
                {
                    return ping;
                }
            }
            return null;
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
            //AIG_CourseNode node = agent.CourseNode;
            ////if (node == null)
            ////    return;
            ////if (CheckedNodes.Contains(node))
            ////{
            ////    //return;
            ////}
            ////else 
            ////    CheckedNodes.Add(node);
            //////update enemy findables
            ////Il2CppSystem.Collections.Generic.List<EnemyAgent> enemyAgents = new();
            ////AIG_CourseNode.GetEnemiesInNodes(node, 2, enemyAgents);
            ////foreach (var enemy in enemyAgents)
            ////{
            ////    int instanceId = enemy.gameObject.GetInstanceID();
            ////    if (FindableObjects.ContainsKey(instanceId))
            ////        continue;
            ////    FindableObject findable = new FindableObject { gameObject = enemy.gameObject, courseNode = node, type = typeof(EnemyAgent), found = false};
            ////    FindableObjects[instanceId] = findable;
            ////}
            ////Update pingTargetFindalbes
            //Vector3 pos = agent.transform.position;

            //int cellX = Mathf.FloorToInt(pos.x / pingMapCellSise);
            //int cellZ = Mathf.FloorToInt(pos.z / pingMapCellSise);

            //int range = Mathf.CeilToInt(foundDistance / pingMapCellSise);

            //for (int dx = -range; dx <= range; dx++)
            //{
            //    for (int dz = -range; dz <= range; dz++)
            //    {
            //        Vector2 neighborCell = new Vector2(cellX + dx, cellZ + dz);

            //        if (!findbleObjectMap.TryGetValue(neighborCell, out List<PlayerPingTarget> pingTargets))
            //            continue;

            //        foreach (var ping in pingTargets)
            //        {
            //            if (ping == null) continue;

            //            Vector3 pingPos = ping.transform.position;
            //            float distance = Vector3.Distance(pos, pingPos);

            //            // Double-check that it's truly within foundDistance
            //            if (distance > foundDistance)
            //                continue;

            //            int instanceId = ping.gameObject.GetInstanceID();
            //            if (FindableObjects.ContainsKey(instanceId))
            //                continue;

            //            FindableObject findable = new FindableObject
            //            {
            //                gameObject = ping.gameObject,
            //                courseNode = node,
            //                type = typeof(PlayerPingTarget),
            //                found = false
            //            };

            //            FindableObjects[instanceId] = findable;
            //        }
            //    }
            //}


            ////update locker findables
            //Il2CppSystem.Collections.Generic.List<LG_ResourceContainer_Storage> lockers = new();
            //lockers = node.MetaData.StorageContainers;
            //foreach (var locker in lockers)
            //{
            //    int instanceId = locker.gameObject.GetInstanceID();
            //    if (!FindableObjects.ContainsKey(instanceId))
            //    {
            //        FindableObject findable = new FindableObject { gameObject = locker.gameObject, courseNode = node, type = typeof(LG_ResourceContainer_Storage), found = false};
            //        FindableObjects[instanceId] = findable;
            //    }
            //    //update item findables
            //    LG_WeakResourceContainer Container = locker.gameObject.GetComponent<LG_WeakResourceContainer>();
            //    if (Container != null && Container.ISOpen)
            //    {
            //        LG_ResourceContainer_Storage storage = Container.gameObject.GetComponent<LG_ResourceContainer_Storage>();
            //        if (storage == null)
            //            continue;
            //        var items = GetItemsFromLocker(storage);
            //        foreach (ItemInLevel item in items)
            //        {

            //            if (item == null)
            //                continue;
            //            instanceId = item.GetInstanceID();
            //            if (FindableObjects.ContainsKey(instanceId))
            //            {
            //                uint currentId = FindableObjects[instanceId].gameObject.GetComponent<ItemInLevel>().ItemDataBlock.persistentID;
            //                uint newId = item.ItemDataBlock.persistentID;
            //                if (currentId == newId)
            //                {
            //                    continue;
            //                }
            //            }
            //            FindableObject itemFindable = new FindableObject { gameObject = item.gameObject, courseNode = node, type = typeof(ItemInLevel), found = false};
            //            FindableObjects[instanceId] = itemFindable;
            //        }
            //    }
            //}
        }

        public static void Updatefinds(PlayerAgent agent)
        {
            Vector3 pos = agent.transform.position;
            int cellX = Mathf.FloorToInt(pos.x / pingMapCellSise);
            int cellZ = Mathf.FloorToInt(pos.z / pingMapCellSise);

            // Number of cells to cover foundDistance
            int range = Mathf.CeilToInt(foundDistance / pingMapCellSise);

            for (int dx = -range; dx <= range; dx++)
            {
                for (int dz = -range; dz <= range; dz++)
                {
                    Vector2 cell = new Vector2(cellX + dx, cellZ + dz);

                    if (!findbleObjectMap.TryGetValue(cell, out var findables))
                        continue;

                    foreach (var findable in findables)
                    {
                        foreach (var gameObject in findable.gameObjects)
                        {
                            if (findable == null)
                                continue;
                            if (gameObject == null)
                                continue;
                            if (!gameObject.activeInHierarchy)
                                continue;
                            if (findable.found)
                                continue;
                            if (!findable.lastCheckedVis.TryGetValue(agent.Owner.PlayerSlotIndex(), out float lastChecked))
                                lastChecked = 0f;
                            if (Time.time - lastChecked < 0.1f)
                                continue;

                            findable.lastCheckedVis[agent.Owner.PlayerSlotIndex()] = Time.time;

                            // Check visibility / distance
                            Vector3 dir = findable.centerPoint - agent.Position + Vector3.up * 1.5f; ;
                            int layerMask =
                                (1 << 0) | //Default
                                (1 << 11) | //Dynamic
                                (1 << 15) | // Glue Gun proj
                                (1 << 16) | //Enemy
                                (1 << 17) | // Enemy dead
                                (1 << 18) | // Debris 
                                (1 << 19); // Denemy Damageble
                            if (zVisibilityManager.CheckObjectVisiblity(gameObject, agent.gameObject, foundDistance) > 0.01f)
                            {
                                findable.found = true;
                                GuiManager.AttemptSetPlayerPingStatus(agent, true, findable.centerPoint, style : findable.gameObjects[0].GetComponent<PlayerPingTarget>().PingTargetStyle);
                                ZiMain.log.LogInfo($"Found object {findable.type}! {gameObject.name}");
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
    public class FindableObject
    {
        public List<GameObject> gameObjects;
        public AIG_CourseNode courseNode;
        public Type type;
        public bool found;
        public Dictionary<int,float> lastCheckedVis = new();
        public Vector3 centerPoint;
    }
    
    public class VisitNode
    {
        public static float VisitNodeDistance = 2f;
        private static float FuzzyVisitNodeDistance = (VisitNodeDistance * 1.4f);
        public static HashSet<VisitNode> AllNodes = new ();
        public static Dictionary<Vector2Int, HashSet<VisitNode>> nodeGrid = new();
        public static HashSet<GameObject> debugCubes = new();
        public Vector3 UnexploredLocation = Vector3.zero;
        public GameObject debugCube;
        public HashSet<GameObject> SampleDebugCubes = new();
        public Vector3 position;
        public bool explored = false;
        public int propigated = 0;
        public HashSet<VisitNode> conntectedNodes = new ();
        public NavMeshHit hit = new();
        private Vector2Int cell;
        public static int propigationAmmount = 30;
        public static Vector3 getUnexploredLocation(Vector3 position)
        {
            VisitNode startingNode = GetNearestNode(position);
            if (startingNode == null)
                return position;

            return startingNode.getUnexploredLocation();
        }
        public Vector3 getUnexploredLocation()
        {
            HashSet<VisitNode> visited = new HashSet<VisitNode>();
            return getUnexploredLocation(ref visited);
        }
        public Vector3 getUnexploredLocation(ref HashSet<VisitNode> visited)
        {
            if (UnexploredLocation != Vector3.zero)
                return UnexploredLocation;

            if (visited == null)
                visited = new HashSet<VisitNode>();
            visited.Add(this);
            if (visited.Count > 100) // arbitrary large number
            {
                ZiMain.log.LogWarning("Everything nearby has been explored");
                return Vector3.zero;
            }


            // Shuffle connected nodes
            List<VisitNode> shuffledNodes = new(conntectedNodes);
            for (int i = shuffledNodes.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                var temp = shuffledNodes[i];
                shuffledNodes[i] = shuffledNodes[j];
                shuffledNodes[j] = temp;
            }


            foreach (VisitNode node in shuffledNodes)
            {
                if (visited.Contains(node) || node == this)
                    continue;

                Vector3 ret = node.getUnexploredLocation(ref visited);
                if (ret != Vector3.zero)
                    return ret;
            }

            return Vector3.zero;
        }
        public static void Update()
        {
            foreach (PlayerAgent agent in PlayerManager.PlayerAgentsInLevel)
            {
                if (agent == null)
                    continue;
                if (agent.Owner.refSessionMode != SNetwork.eReplicationMode.Playing)
                    continue;
                int charId = agent.CharacterID;
                var NearbyNodes = GetNearByNodes(agent.Position, VisitNodeDistance);
                if (NearbyNodes.Count == 0)
                {
                    new VisitNode(agent.Position, propigationAmmount);
                }
                NearbyNodes = GetNearByNodes(agent.Position, VisitNodeDistance * 10);
                foreach (var node in NearbyNodes)
                {
                    if (!node.explored)
                    {
                        node.explored = true;
                        node.propigate(propigationAmmount);
                        node.UpdateDebugCube(false);
                    }
                }
            }
        }
        public static HashSet<VisitNode> GetNearByNodes(Vector3 position, float searchRadius = -1)
        {
            var ret = new HashSet<VisitNode>();

            // central cell for the query position
            var cell = new Vector2Int(
                Mathf.FloorToInt(position.x / VisitNodeDistance),
                Mathf.FloorToInt(position.z / VisitNodeDistance)
            );

            // compute how many cells we must check to cover the radius
            if (searchRadius <= 0)
                searchRadius = FuzzyVisitNodeDistance;
            int cellRadius = Mathf.CeilToInt(searchRadius / VisitNodeDistance);
            if (cellRadius < 1) cellRadius = 1; // at least check neighbors

            // collect candidates from the necessary grid cells
            var nearbyCandidates = new HashSet<VisitNode>();
            for (int dx = -cellRadius; dx <= cellRadius; dx++)
            {
                for (int dy = -cellRadius; dy <= cellRadius; dy++)
                {
                    var neighborCell = new Vector2Int(cell.x + dx, cell.y + dy);
                    if (nodeGrid.TryGetValue(neighborCell, out var nodes) && nodes != null)
                        nearbyCandidates.UnionWith(nodes);
                }
            }

            // filter by true linear distance
            foreach (var node in nearbyCandidates)
            {
                if (node == null) continue;
                if (Vector3.Distance(node.position, position) <= searchRadius)
                    ret.Add(node);
            }

            return ret;
        }
        public HashSet<VisitNode> GetNearByNodes()
        {
            return GetNearByNodes(position);
        }
        public static VisitNode GetNearestNode(Vector3 position)
        {
            HashSet<VisitNode> nearbyNodes = GetNearByNodes(position);
            VisitNode nearestNode = null;
            float nearestDistance = float.MaxValue;

            foreach (var node in nearbyNodes)
            {
                if (node == null)
                    continue;

                float distance = Vector3.Distance(node.position, position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestNode = node;
                }
            }

            return nearestNode;
        }
        public VisitNode(Vector3 Position, int proigate = 0)
        {
            
            hit = new NavMeshHit();
            NavMesh.SamplePosition(Position, out hit, 3f, -1);
            position = Position;
            AllNodes.Add(this);

            cell = new Vector2Int(
                Mathf.FloorToInt(position.x / VisitNodeDistance),
                Mathf.FloorToInt(position.z / VisitNodeDistance)
            );

            // ensure cell exists and add this node
            if (!nodeGrid.TryGetValue(cell, out var set))
            {
                set = new HashSet<VisitNode>();
                nodeGrid[cell] = set;
            }
            set.Add(this);

            // find neighbors (query uses the snapped position)
            var nearby = GetNearByNodes(position, FuzzyVisitNodeDistance * 2);
            // be sure we don't treat ourselves as a neighbor
            nearby.Remove(this);

            foreach (var node in nearby)
            {
                node.UpdateDebugCube();
                if (node == null || !CanNavigateBetween(node.position, position))
                    continue;
                conntectedNodes.Add(node);
                node.conntectedNodes.Add(this);
            }
            CheckSamplesNearby(proigate);
            propigated = Math.Max(proigate, propigated);
            addDebugCube();
        }
        public static int areaMask = 1 << 0; // Default walkable area
        public static bool CanNavigateBetween(Vector3 start, Vector3 end)
        {
            if (NavMesh.SamplePosition(start, out NavMeshHit startHit, 1.0f, areaMask) &&
                NavMesh.SamplePosition(end, out NavMeshHit endHit, 1.0f, areaMask))
            {
                NavMeshPath path = new NavMeshPath();
                bool pathFound = NavMesh.CalculatePath(startHit.position, endHit.position, areaMask, path);
                return pathFound && path.status == NavMeshPathStatus.PathComplete;
            }

            // Either start or end is not on the NavMesh
            return false;
        }
        public void UpdateDebugCube(bool explore = true)
        {
            if (debugCube == null)
                return;
            Renderer rend = debugCube.GetComponent<Renderer>();
            if (rend == null)
                return;
            if (explore)
                CheckSamplesNearby();
            // Green if there's unexplored nearby, red if not
            if (explored)
                rend.material.color = Color.green;
            else
                rend.material.color = Color.red;
            // Update propigated text
            Transform textTransform = debugCube.transform.Find("PropigatedText");
            if (textTransform != null)
            {
                TextMesh textMesh = textTransform.GetComponent<TextMesh>();
                if (textMesh != null)
                {
                    textMesh.text = propigated.ToString();
                }
                else 
                {                                    
                    ZiMain.log.LogWarning("UpdateDebugCube: TextMesh component not found on PropigatedText.");
                }
            }
            else
            {
                ZiMain.log.LogWarning("UpdateDebugCube: PropigatedText not found.");
            }
        }
        public void addDebugCube()
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject.Destroy(cube.GetComponent<Collider>());
            cube.transform.localScale = Vector3.one * 0.1f;
            cube.transform.position = position + Vector3.up * 1f;

            Renderer cubeRend = cube.GetComponent<Renderer>();
            if (cubeRend != null)
                cubeRend.material.color = Color.green;

            debugCubes.Add(cube);
            debugCube = cube;

            // Add a TextMesh above the cube
            GameObject textObj = new GameObject("PropigatedText");
            textObj.transform.SetParent(debugCube.transform);
            textObj.transform.localPosition = Vector3.up * 0.25f;

            TextMesh textMesh = textObj.AddComponent<TextMesh>();
            textMesh.characterSize = 0.1f;
            textMesh.anchor = TextAnchor.LowerCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = new Color(0.2f, 0.2f, 0.2f);
            textMesh.text = propigated.ToString();

            UpdateDebugCube(); // Make sure color and text are correct
        }
        public void addSampleDebugCube(Vector3 pos, HashSet<VisitNode> nearbyNodes, Color color)
        {
            // Create a small cube for the sample
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject.Destroy(cube.GetComponent<Collider>());
            cube.transform.localScale = Vector3.one * 0.05f;
            Vector3 sampleWorldPos = pos + Vector3.up * 0.5f;
            cube.transform.position = sampleWorldPos;
            Renderer cubeRend = cube.GetComponent<Renderer>();
            if (cubeRend != null)
                cubeRend.material.color = color;

            // CreateLine helper uses actual world positions and parents the line to the sample cube
            void CreateLine(Vector3 startWorld, Vector3 endWorld, Color baseColor)
            {
                GameObject lineObj = new GameObject("DebugLine");
                lineObj.transform.SetParent(cube.transform, worldPositionStays: true); // parent to cube

                LineRenderer lr = lineObj.AddComponent<LineRenderer>();
                lr.useWorldSpace = true; // we will set world-space positions
                lr.positionCount = 2;
                lr.SetPosition(0, startWorld);
                lr.SetPosition(1, endWorld);

                // Very thin line
                lr.startWidth = 0.01f;
                lr.endWidth = 0.01f;

                // Simple transparent material
                lr.material = new Material(Shader.Find("Sprites/Default"));
                Color c = baseColor;
                c.a = 0.2f;               // 0.2 transparency
                lr.startColor = c;
                lr.endColor = c;

                // Optional: smooth caps
                lr.numCapVertices = 2;
            }

            // Line to base node (white)
            Vector3 baseNodeLineEnd = (debugCube != null) ? debugCube.transform.position : (this.position + Vector3.up * 1f);
            CreateLine(sampleWorldPos, baseNodeLineEnd, Color.white);

            // Lines to nearby nodes (brown)
            Color brown = new Color(0.6f, 0.3f, 0.1f);
            foreach (var node in nearbyNodes)
            {
                if (node == null) continue;
                Vector3 nodeLineEnd = (node.debugCube != null) ? node.debugCube.transform.position : (node.position + Vector3.up * 1f);
                CreateLine(sampleWorldPos, nodeLineEnd, brown);
            }

            // (optional) keep track of sample cube if you need to destroy it later:
            SampleDebugCubes.Add(cube);
        }
        public void propigate(int depth, HashSet<VisitNode> visited = null)
        {
            if (depth <= 0)
                return;

            if (visited == null)
                visited = new HashSet<VisitNode>();

            // Skip nodes we've already processed this cycle
            if (visited.Contains(this))
                return;

            visited.Add(this);

            // Update this node's depth
            propigated = Math.Max(depth, propigated);
            UpdateDebugCube();

            // Check samples around this node — this will handle new node creation
            CheckSamplesNearby(depth - 1);

            // Propagate to connected nodes
            HashSet<VisitNode> nearbynodes = GetNearByNodes();
            foreach (var node in nearbynodes)
            {
                node.propigate(depth - 1, visited);
            }
        }
        public bool CheckSamplesNearby(int propigate = 0)
        {
            foreach (var cube in SampleDebugCubes)
            {
                GameObject.Destroy(cube);
            }
            //if (propigate < propigated)
            //    return UnexploredLocation != Vector3.zero;
            int sampleCount = 8;
            float radius = VisitNodeDistance;
            int indexOffset = UnityEngine.Random.Range(0, sampleCount);
            UnexploredLocation = Vector3.zero;
            for (int i = 0; i < sampleCount; i++)
            {
                int rotatedIndex = (i + indexOffset) % sampleCount;
                // Angle around the circle
                float angle = rotatedIndex * Mathf.PI * 2f / sampleCount;
                Vector3 samplePos = position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;

                if (!NavMesh.SamplePosition(samplePos, out NavMeshHit hit, 3f, -1))
                    continue;

                bool AdjustPositionUp(Vector3 pos, out Vector3 result, float maxLift = 1.5f, float step = 0.05f)
                {
                    // Attempt to get player capsule
                    var controller = PlayerManager.GetLocalPlayerAgent()?.PlayerCharacterController?.m_characterController;

                    if (controller == null)
                    {
                        ZiMain.log.LogWarning("AdjustPositionUp: PlayerCharacterController not found, falling back to sphere check.");
                        float lifted = 0f;
                        while (lifted <= maxLift)
                        {
                            if (!Physics.CheckSphere(pos, 0.1f))
                            {
                                result = pos;
                                return true;
                            }
                            pos.y += step;
                            lifted += step;
                        }

                        result = pos;
                        return false;
                    }

                    float radius = controller.radius;
                    float height = controller.height;
                    Vector3 centerOffset = controller.center;

                    var playerCollider = controller; // the capsule to ignore
                    float liftedCapsule = 0f;

                    while (liftedCapsule <= maxLift)
                    {
                        Vector3 capsuleBottom = pos + centerOffset - Vector3.up * (height / 2f - radius);
                        Vector3 capsuleTop = pos + centerOffset + Vector3.up * (height / 2f - radius);

                        // Check if capsule overlaps anything excluding the player
                        Collider[] hits = Physics.OverlapCapsule(capsuleBottom, capsuleTop, radius, ~0, QueryTriggerInteraction.Ignore);
                        bool blocked = hits.Any(c => c != playerCollider);

                        if (!blocked)
                        {
                            result = pos;
                            return true;
                        }

                        pos.y += step;
                        liftedCapsule += step;
                    }

                    result = pos;
                    return false;
                }

                // Lift hit position and node position
                Vector3 liftedHitPos = new();
                if (!AdjustPositionUp(hit.position, out liftedHitPos))
                    continue;
                Vector3 liftedNodePos = new();
                if (!AdjustPositionUp(position, out liftedNodePos))
                    continue;
                var nearbyNodes = GetNearByNodes(liftedHitPos, VisitNodeDistance);
                float rayDistance = Vector3.Distance(liftedHitPos, liftedNodePos);
                Vector3 direction = (liftedNodePos - liftedHitPos).normalized;
                //!Physics.Raycast(liftedHitPos, direction, rayDistance)
                if (CanNavigateBetween(liftedHitPos, liftedNodePos))
                {
                    // No obstruction between lifted positions
                    //addSampleDebugCube(hit.position, nearbyNodes, Color.black);
                    UnexploredLocation = liftedHitPos;
                    if (nearbyNodes.Count == 0 && propigate > 0)
                    {
                        new VisitNode(liftedHitPos, propigate - 1);
                    }
                }
            }
            return UnexploredLocation != Vector3.zero;
        }
    }
    public class ComponentFinder : MonoBehaviour
    {
        public IEnumerator FindAllWithComponentSlow<T>(Action<List<GameObject>> onComplete, int itemsPerFrame = 100)
            where T : Component
        {
            List<GameObject> results = new List<GameObject>();

            // Get all root objects in all loaded scenes
            List<GameObject> roots = new List<GameObject>();
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                scene.GetRootGameObjects(roots.ToIl2CppList());
            }

            int processed = 0;

            foreach (var root in roots)
            {
                // Walk all children manually instead of using FindObjectsOfType
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.TryGetComponent<T>(out _))
                        results.Add(t.gameObject);

                    processed++;

                    // Yield every N objects to spread over frames
                    if (processed % itemsPerFrame == 0)
                        yield return null;
                }
            }

            onComplete?.Invoke(results);
        }
    }
    public static class ListConverter
    {
        public static Il2CppSystem.Collections.Generic.List<T> ToIl2CppList<T>(this IEnumerable<T> source)
        {
            var list = new Il2CppSystem.Collections.Generic.List<T>();
            foreach (var item in source)
                list.Add(item);
            return list;
        }
    }
}

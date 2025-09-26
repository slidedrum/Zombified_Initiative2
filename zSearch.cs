using Agents;
using AIGraph;
using Enemies;
using FluffyUnderware.Curvy.Generator;
using FluffyUnderware.DevTools.Extensions;
using Il2CppSystem.Linq.Expressions;
using LevelGeneration;
using Player;
using PlayFab.AdminModels;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using ZombieTweak2;
using ZombieTweak2.zMenu;
using static RootMotion.FinalIK.AimPoser;

namespace Zombified_Initiative
{
    public static class zSearch
    {
        //this class is for finding stuff inside the world.
        public static Dictionary<int,FindableObject> FindableObjects = new();
        public static float foundDistance = 10f;
        private static PlayerAgent localPlayer = null;

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

}

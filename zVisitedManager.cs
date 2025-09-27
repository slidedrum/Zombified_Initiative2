using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Zombified_Initiative;

namespace ZombieTweak2
{
    public static class zVisitedManager
    {
        public static int NodeMapGridSize = 10;
        public static float NodeGridSize = 2.5f;
        public static float NodeVisitDistance = 10f;
        public static Dictionary<Vector3Int, HashSet<VisitNode>> NodeMap = new();
        private static bool setup = false;
        private static List<PlayerAgent> agents = new();
        private static List<PlayerAgent> botAgents = new();
        private const int areaMask = 1 << 0;
        internal static OrderedSet<VisitNode> nodesThatNeedConnectionChecks = new();
        internal static OrderedSet<nodeToCreate> nodesToCreate = new();
        private static int conectionCheckIndex = 0;
        internal static int connectionChecksPerFrame = 2;
        internal static int nodesCreatedPerFrame = 5;
        public static int propigationAmmount = 3;
        public static int propigationSampleCount = 32;
        internal static bool debugCube = false;
        internal static bool debugText = false;
        internal static bool debugLines = false;
        internal static int unexploredMaxDepth = 50;
        private static PlayerAgent localPlayer;
        public static Vector3[] CapsuleCorners;
        public static HashSet<VisitNode> allnodes = new();
        public static VisitNode GetUnexploredLocation(Vector3 position, int depth = 0,int maxDepth = 0, OrderedSet<VisitNode> searched = null)
        {
            if (maxDepth == 0)
                maxDepth = unexploredMaxDepth;
            if (searched == null)
                searched = new();
            var node = GetNearestNode(position);
            if (node == null)
                return null;
            return node.FindUnexplored(depth,maxDepth,searched);
        }

        public static void SetNodeMapGridSize(int size)
        {
            if (size < 1) size = 1;
            NodeMapGridSize = size;
            setup = false;
            Setup(true);
        }
        public static void SetNodeGridSize(float size)
        {
            if (size < 0.1f) size = 0.1f;
            NodeGridSize = (float)Math.Round(size, 1); ;
            setup = false;
            Setup(true);
        }
        public static void SetNodeVisitDistance(float size)
        {
            if (size < 0.1f) size = 0.1f;
            NodeVisitDistance = (float)Math.Round(size, 1);
            setup = false;
            Setup(true);
        }
        public static void SetPropigationAmmount(int distance)
        {
            if (distance < 0) distance = 0;
            propigationAmmount = distance;
            setup = false;
            Setup(true);
        }
        public static void SetPropigationSampleCount(int count)
        {
            if (count < 4) count = 4;
            propigationSampleCount = count;
            setup = false;
            Setup(true);
        }
        public static void Setup(bool instantNodePropigation = false)
        {
            if (setup) 
                return;

            if (localPlayer.Owner.refSessionMode != SNetwork.eReplicationMode.Playing)
            {
                setup = false;
                return;
            }
            CapsuleCorners = GetCapsuleCorners();
            NodeMap.Clear();
            var _debug = debugCube;
            var _debugText = debugText;
            var _debugLines = debugLines;
            debugCube = false;
            debugText = false;
            debugLines = false;
            foreach (var node in allnodes)
                node.UpdateDebugVisuals();
            allnodes.Clear();
            debugCube = _debug;
            debugText = _debugText;
            debugLines = _debugLines;
            nodesThatNeedConnectionChecks.Clear();
            nodesToCreate.Clear();
            agents = PlayerManager.PlayerAgentsInLevel.ToArray().ToList();
            botAgents.Clear();
            foreach (PlayerAgent agent in agents)
            {
                if (agent.Owner.IsBot)
                {
                    botAgents.Add(agent);
                }
            }
            setup = true;
            if (instantNodePropigation)
            {
                foreach (var agent in agents)
                {
                    var node = CreateNodeOnNavMesh(agent.Position);
                    if (node != null)
                    {
                        node.Propigate(propigationAmmount);
                        node.UpdateDebugVisuals();
                    }
                }
            }
        }
        public static void SetDebug(bool? debug = null, bool? text = null, bool? lines = null)
        {
            if (debug.HasValue)
                zVisitedManager.debugCube = debug.Value;

            if (text.HasValue)
                zVisitedManager.debugText = text.Value;

            if (lines.HasValue)
                zVisitedManager.debugLines = lines.Value;

            foreach (var node in allnodes)
                node.UpdateDebugVisuals();
        }
        public class nodeToCreate
        {
            public Vector3 position;
            public bool garenteedNoNodesNearby;
            public int depth;
            public HashSet<VisitNode> CheckedNodes;
            public override bool Equals(object obj)
            {
                if (obj is nodeToCreate other)
                {
                    float tolerance = zVisitedManager.NodeGridSize / 2f;
                    return Vector3.Distance(position, other.position) <= tolerance;
                }
                return false;
            }

            public override int GetHashCode()
            {
                // Hash codes and tolerance don't mix well. A safe trick:
                // Quantize the position to the tolerance grid so "close" positions hash the same.
                float tolerance = zVisitedManager.NodeGridSize / 2f;

                int x = Mathf.RoundToInt(position.x / tolerance);
                int y = Mathf.RoundToInt(position.y / tolerance);
                int z = Mathf.RoundToInt(position.z / tolerance);

                return (x * 73856093) ^ (y * 19349663) ^ (z * 83492791);
                // using big primes to spread values out
            }

            public static bool operator ==(nodeToCreate left, nodeToCreate right)
            {
                if (ReferenceEquals(left, right)) return true;
                if (left is null || right is null) return false;
                return left.Equals(right);
            }

            public static bool operator !=(nodeToCreate left, nodeToCreate right)
            {
                return !(left == right);
            }
        }
        public static void MapNode(VisitNode node)
        {
            allnodes.Add(node);
            var gridPos = node.GetGridPosition();
            if (!NodeMap.ContainsKey(gridPos))
                NodeMap[gridPos] = new();
            NodeMap[gridPos].Add(node);
        }
        public static void Update()
        {
            if (localPlayer == null)
                localPlayer = PlayerManager.GetLocalPlayerAgent(); //might want to delay this.
            if (localPlayer == null)
                return;
            if (localPlayer.Owner.refSessionMode != SNetwork.eReplicationMode.Playing)
            {
                setup = false;
                return;
            }
            if (!setup)
                Setup();
            if (!setup)
                return;
            foreach(PlayerAgent agent in agents)
            {
                HashSet<VisitNode> visitableNodes = GetNearByNodes(agent.transform.position, NodeVisitDistance);
                bool nodesNearby = HasNodesnearby(agent.Position, NodeGridSize);
                if (!nodesNearby)
                {
                    //create a new node
                    var node = CreateNodeOnNavMesh(agent.Position);
                    if (node != null)
                        visitableNodes.Add(node);
                }
                foreach (VisitNode node in visitableNodes)
                {
                    node.Discover();
                    node.Propigate(propigationAmmount);
                    node.UpdateDebugVisuals();
                }
            }
            for (int i = 0; i < nodesCreatedPerFrame && i < nodesToCreate.Count; i++)
            {
                var nodeInfo = nodesToCreate.FirstOrDefault();
                nodesToCreate.Remove(nodeInfo);
                var node = CreateNodeOnNavMesh(nodeInfo.position);
                if (node != null)
                    node.Propigate(nodeInfo.depth, nodeInfo.CheckedNodes);
            }
            for (int i = 0; i < connectionChecksPerFrame; i++)
            {
                if (nodesThatNeedConnectionChecks.Count > 0)
                {
                    if (conectionCheckIndex >= nodesThatNeedConnectionChecks.Count)
                        conectionCheckIndex = 0;
                    var node1ToCheck = nodesThatNeedConnectionChecks[conectionCheckIndex];
                    var node2ToCheck = node1ToCheck.nearbyNodesToCheckIfConnected.FirstOrDefault();
                    if (node2ToCheck != null)
                    {
                        node1ToCheck.nearbyNodesToCheckIfConnected.Remove(node2ToCheck);
                        node2ToCheck.nearbyNodesToCheckIfConnected.Remove(node1ToCheck);

                        if (node1ToCheck.nearbyNodesToCheckIfConnected.Count == 0)
                        {
                            nodesThatNeedConnectionChecks.Remove(node1ToCheck);
                        }
                        if (node2ToCheck.nearbyNodesToCheckIfConnected.Count == 0)
                        {
                            nodesThatNeedConnectionChecks.Remove(node2ToCheck);
                        }
                        bool navicable = false;
                        if (node1ToCheck.connectedNodes.Contains(node2ToCheck) ^ node2ToCheck.connectedNodes.Contains(node1ToCheck))
                        {
                            navicable = true;
                        }
                        else if (!node1ToCheck.connectedNodes.Contains(node2ToCheck) &&
                                !node2ToCheck.connectedNodes.Contains(node1ToCheck) &&
                                CanNavigateBetween(node1ToCheck.position, node2ToCheck.position))
                        {
                            navicable = true;
                        }
                        if (navicable && CanNodeSeeEachOther(node1ToCheck, node2ToCheck))
                            node1ToCheck.ConnectNode(node2ToCheck);
                    }
                    conectionCheckIndex++;

                }
                else
                    break;
            }
        }
        public static bool CanNodeSeeEachOther(VisitNode node1, VisitNode node2)
        {
            // Calculate rotation of node1 looking at node2
            Vector3 direction = (node2.position - node1.position).normalized;
            Quaternion rotation1 = Quaternion.LookRotation(direction);

            // Get world-space corners
            Vector3[] node1Corners = GetWorldColliderCorners(node1.position, rotation1);
            Vector3[] node2Corners = GetWorldColliderCorners(node2.position, rotation1);

            // Use player's layer mask or a default mask
            int layerMask = ~0; // all layers
            QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

            // Check each corner-to-corner ray
            foreach (var corner1 in node1Corners)
            {
                foreach (var corner2 in node2Corners)
                {
                    Vector3 dir = corner2 - corner1;
                    float dist = dir.magnitude;
                    dir /= dist; // normalize

                    if (!Physics.Raycast(corner1, dir, dist, layerMask, triggerInteraction))
                    {
                        // At least one clear line of sight
                        return true;
                    }
                }
            }

            return false; // all rays blocked
        }
        public static Vector3[] GetWorldColliderCorners(Vector3 position, Quaternion rotation)
        {
            Vector3[] worldPoints = new Vector3[CapsuleCorners.Length];
            for (int i = 0; i < CapsuleCorners.Length; i++)
            {
                worldPoints[i] = position + rotation * CapsuleCorners[i];
            }
            return worldPoints;
        }
        public static Vector3[] GetCapsuleCorners()
        {
            Vector3[] RelativePoints;
            var controller = PlayerManager.GetLocalPlayerAgent()?.PlayerCharacterController?.m_characterController;
            if (controller == null)
            {
                // fallback: just center
                ZiMain.log.LogWarning("GetCapsuleCorners: PlayerCharacterController not found, falling back to center point.");
                RelativePoints = new Vector3[1] { Vector3.zero };
                return RelativePoints;
            }

            float radius = controller.radius;
            float height = controller.height;

            float topOffset = (height / 2f - radius) * 0.9f;    // lower top corners by 10%
            float bottomOffset = (height / 2f - radius) * 0.9f; // raise bottom corners by 10%

            Vector3 center = Vector3.zero;
            Vector3 top = Vector3.up * topOffset;
            Vector3 bottom = Vector3.down * bottomOffset;

            RelativePoints = new Vector3[9];
            RelativePoints[0] = center; // center

            // Top layer
            RelativePoints[1] = top + new Vector3(radius, 0, radius);   // front right
            RelativePoints[2] = top + new Vector3(-radius, 0, radius);  // front left
            RelativePoints[3] = top + new Vector3(radius, 0, -radius);  // back right
            RelativePoints[4] = top + new Vector3(-radius, 0, -radius); // back left

            // Bottom layer
            RelativePoints[5] = bottom + new Vector3(radius, 0, radius);   // front right
            RelativePoints[6] = bottom + new Vector3(-radius, 0, radius);  // front left
            RelativePoints[7] = bottom + new Vector3(radius, 0, -radius);  // back right
            RelativePoints[8] = bottom + new Vector3(-radius, 0, -radius); // back left
            return RelativePoints;
        }
        public static VisitNode CreateNodeOnNavMesh(Vector3 pos)
        {
            if (!NavMesh.SamplePosition(pos, out NavMeshHit hit, 3f, -1)) //can we find a spot on the nav mesh.
                return null;
            if (!IsPositionFree(hit.position)) //can the player fit on that spot in the nav mesh.
                return null;
            var nodesNearby = HasNodesnearby(hit.position, NodeGridSize);
            var NearbyNodes = GetNearByNodes(hit.position, NodeVisitDistance);
            if (nodesNearby)
            { //do we already have a node in this spot.
                return null;
            }
            if (NearbyNodes.Count > 0) 
            {
                bool navicable = false;
                foreach (var nearbyNode in NearbyNodes)
                {
                    if (CanNavigateBetween(hit.position, nearbyNode.position))
                    {
                        navicable = true;
                        break;
                    }
                }
                if (navicable == false)
                {
                    //return null;
                }
            }
            //else
            //{ 
            //    //This might be a problem, not sure.  this means we need to be navicable to the grid to create a new node.  Is that a good thing?  
            //    return null;
            //}
            var node = new VisitNode(hit.position, true);
            node.UpdateDebugVisuals();
            return node;
        }
        public static Vector3Int GetGridPosition(Vector3 pos)
        {
            return new Vector3Int(Mathf.FloorToInt(pos.x / NodeMapGridSize), Mathf.FloorToInt(pos.y / NodeMapGridSize), Mathf.FloorToInt(pos.z / NodeMapGridSize));
        }
        public static bool HasNodesnearby(Vector3 position, float searchRadius = 0)
        {
            HashSet<VisitNode> nearbyNodes = new();
            Vector3Int gridPosition = GetGridPosition(position);

            if (searchRadius <= 0)
                searchRadius = NodeVisitDistance;

            int cellRadius = Mathf.CeilToInt(searchRadius / NodeMapGridSize);

            for (int x = -cellRadius; x <= cellRadius; x++)
            {
                for (int y = -cellRadius; y <= cellRadius; y++)
                {
                    for (int z = -cellRadius; z <= cellRadius; z++)
                    {
                        Vector3Int checkGridPosition = new Vector3Int(gridPosition.x + x, gridPosition.y + y, gridPosition.z + z);

                        if (NodeMap.TryGetValue(checkGridPosition, out var nodes))
                        {
                            foreach (VisitNode node in nodes)
                            {
                                if (Vector3.Distance(node.position, position) <= searchRadius)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }
        public static VisitNode GetNearestNode(Vector3 position, float searchRadius = 0)
        {
            var NearbyNodes = GetNearByNodes(position, searchRadius);
            if (NearbyNodes.Count == 0)
                return null;
            float closestDistance = float.MaxValue;
            VisitNode nearestNode = null;
            foreach(var node in  NearbyNodes)
            {
                var distance = Vector3.Distance(node.position, position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    nearestNode = node;
                }
                if (distance < NodeGridSize)
                {
                    break;
                }
            }
            return nearestNode;
        }
        public static HashSet<VisitNode> GetNearByNodes(Vector3 position, float searchRadius = 0)
        {
            HashSet<VisitNode> nearbyNodes = new();
            Vector3Int gridPosition = GetGridPosition(position);

            if (searchRadius <= 0)
                searchRadius = NodeVisitDistance;

            int cellRadius = Mathf.CeilToInt(searchRadius / NodeMapGridSize);

            for (int x = -cellRadius; x <= cellRadius; x++)
            {
                for (int y = -cellRadius; y <= cellRadius; y++)
                {
                    for (int z = -cellRadius; z <= cellRadius; z++)
                    {
                        Vector3Int checkGridPosition = new Vector3Int(gridPosition.x + x, gridPosition.y + y, gridPosition.z + z);

                        if (NodeMap.TryGetValue(checkGridPosition, out var nodes))
                        {
                            foreach (VisitNode node in nodes)
                            {
                                if (Vector3.Distance(node.position, position) <= searchRadius)
                                {
                                    nearbyNodes.Add(node);
                                }
                            }
                        }
                    }
                }
            }
            return nearbyNodes;
        }
        public static bool CanNavigateBetween(Vector3 start, Vector3 end, float distanceMult = 2f)
        {
            if (NavMesh.SamplePosition(start, out NavMeshHit startHit, 1.0f, areaMask) &&
                NavMesh.SamplePosition(end, out NavMeshHit endHit, 1.0f, areaMask))
            {
                NavMeshPath path = new NavMeshPath();
                bool pathFound = NavMesh.CalculatePath(startHit.position, endHit.position, areaMask, path);
                if (!pathFound || path.status != NavMeshPathStatus.PathComplete)
                    return false;

                // Calculate the total path length
                float totalLength = 0f;
                for (int i = 1; i < path.corners.Length; i++)
                {
                    totalLength += Vector3.Distance(path.corners[i - 1], path.corners[i]);
                }

                float directDistance = Vector3.Distance(startHit.position, endHit.position);

                return totalLength <= distanceMult * directDistance;
            }
            return false;
        }
        public static bool IsPositionFree(Vector3 pos)
        {
            var controller = PlayerManager.GetLocalPlayerAgent()?.PlayerCharacterController?.m_characterController;

            if (controller == null)
            {
                // fallback: simple sphere check at the current position
                ZiMain.log.LogWarning("AdjustPositionUp: PlayerCharacterController not found, falling back to sphere check.");
                bool free = !Physics.CheckSphere(pos, 0.1f, ~0, QueryTriggerInteraction.Ignore);
                return free;
            }

            float radius = controller.radius;
            float height = controller.height;
            Vector3 centerOffset = controller.center;

            Vector3 capsuleBottom = pos + centerOffset - Vector3.up * (height / 2f - radius);
            Vector3 capsuleTop = pos + centerOffset + Vector3.up * (height / 2f - radius);

            // check only colliders the player can collide with, ignoring triggers
            Collider[] hits = Physics.OverlapCapsule(capsuleBottom, capsuleTop, radius, 1 << 0, QueryTriggerInteraction.Ignore);
            bool blocked = hits.Any(c => c != controller);

            return !blocked;
        }
    }

    public class VisitNode
    {
        public GameObject DebugObject;
        public Vector3 position;
        public bool discovered = false;
        public int propigated = 0;
        public HashSet<VisitNode> nearbyNodes = new();
        public OrderedSet<VisitNode> connectedNodes = new();
        public HashSet<VisitNode> nearbyNodesToCheckIfConnected = new();
        private Dictionary<VisitNode, LineRenderer> connectionLines = new();

        public VisitNode(Vector3 pos, bool garenteedNoNodesNearby)
        {
            position = pos;
            zVisitedManager.MapNode(this);
            if (!garenteedNoNodesNearby || true)
            {
                nearbyNodes = GetNearByNodes(zVisitedManager.NodeGridSize * 1.75f);
                foreach (var node in nearbyNodes)
                {
                    node.ConnectNearbyNode(this);
                }
                if (nearbyNodes.Count > 0)
                {
                    nearbyNodesToCheckIfConnected = new(nearbyNodes);
                    zVisitedManager.nodesThatNeedConnectionChecks.Add(this);
                }
            }

        }
        public OrderedSet<VisitNode> getUnexploredNodes()
        {
            OrderedSet<VisitNode> unexploredNodes = new();
            foreach (var node in connectedNodes)
            {
                if (!node.discovered)
                {
                    unexploredNodes.Add(node);
                }
            }
            return unexploredNodes;
        }
        public  Vector3Int GetGridPosition()
        {
            return zVisitedManager.GetGridPosition(position);
        }
        public void ConnectNearbyNode(VisitNode node)
        {
            if (node == this) return;
            if (!nearbyNodes.Contains(node))
                nearbyNodes.Add(node);
            if (!node.nearbyNodes.Contains(this))
                node.nearbyNodes.Add(this);
        }
        public void ConnectNode(VisitNode node)
        {
            if (node == this) return;
            if (!connectedNodes.Contains(node))
                connectedNodes.Add(node);
            if (!node.connectedNodes.Contains(this))
                node.connectedNodes.Add(this);
        }
        public HashSet<VisitNode> GetNearByNodes(float searchRadius = 0)
        {
            var nearybyNodes = zVisitedManager.GetNearByNodes(position, searchRadius);
            nearybyNodes.Remove(this);
            return nearybyNodes;
        }
        public void Discover()
        {
            if (discovered)
                return;
            discovered = true;
        }
        public void Propigate(int depth, HashSet<VisitNode> CheckedNodes = null)
        {
            //TODO figure out why it doesn't always work in some edge cases.  What are those edge cases?
            if (depth <= 0)
                return;
            if (depth <= propigated)
                return;
            propigated = depth;
            UpdateDebugVisuals();
            if (CheckedNodes == null)
                CheckedNodes = new();
            if (CheckedNodes.Contains(this))
                return;
            CheckedNodes.Add(this);
            
            float radius = zVisitedManager.NodeGridSize;
            for (int i = 0; i < zVisitedManager.propigationSampleCount; i++)
            {
                // Angle around the circle
                float angle = i * Mathf.PI * 2f / zVisitedManager.propigationSampleCount;
                Vector3 samplePos = position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                if (!NavMesh.SamplePosition(samplePos, out NavMeshHit hit, 3f, -1)) //can we find a spot on the nav mesh.
                    continue;
                if (!zVisitedManager.IsPositionFree(hit.position)) //can the player fit on that spot in the nav mesh.
                    continue;
                if (!zVisitedManager.CanNavigateBetween(position, hit.position)) //can we navigate to that spot.
                    continue;
                var nearbyNodes = zVisitedManager.GetNearByNodes(hit.position, zVisitedManager.NodeGridSize);
                if (nearbyNodes.Count > 0)
                { //do we already have a node in this spot.
                    foreach (var nearbyNode in nearbyNodes)
                    {
                        nearbyNode.Propigate(depth - 1, CheckedNodes);
                    }
                    continue;
                }
                zVisitedManager.nodeToCreate node = new()
                {
                    position = hit.position,
                    garenteedNoNodesNearby = true,
                    depth = depth - 1,
                    CheckedNodes = CheckedNodes,
                };
                if (!zVisitedManager.nodesToCreate.Contains(node))
                    zVisitedManager.nodesToCreate.Add(node);
                //var node = new VisitNode(hit.position, true);
                //node.Propigate(depth - 1, CheckedNodes);
            }
        }

        public void UpdateDebugVisuals()
        {
            // Early out if all debug options are off
            if (!zVisitedManager.debugCube && !zVisitedManager.debugText && !zVisitedManager.debugLines)
            {
                if (DebugObject != null)
                {
                    UnityEngine.Object.Destroy(DebugObject);
                    DebugObject = null;
                }
                return;
            }

            // Ensure DebugObject exists
            if (DebugObject == null)
            {
                DebugObject = new GameObject($"VisitNode_Debug_{GetHashCode()}");
                DebugObject.transform.position = position + Vector3.up * 0.25f;
            }

            // --- Cube ---
            var cubeObj = DebugObject.transform.Find("Cube")?.gameObject;
            if (zVisitedManager.debugCube)
            {
                if (cubeObj == null)
                {
                    cubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cubeObj.name = "Cube";
                    cubeObj.transform.SetParent(DebugObject.transform, false);
                    cubeObj.transform.localPosition = Vector3.zero;
                    cubeObj.transform.localScale = Vector3.one * 0.1f;

                    // Disable cube physics
                    var col = cubeObj.GetComponent<Collider>();
                    if (col != null)
                        UnityEngine.Object.Destroy(col);
                }

                var cubeRenderer = cubeObj.GetComponent<MeshRenderer>();
                if (cubeRenderer != null)
                {
                    if (cubeRenderer.material == null)
                        cubeRenderer.material = new Material(Shader.Find("Standard"));
                    cubeRenderer.material.color = discovered ? Color.green : Color.red;
                }
            }
            else
            {
                if (cubeObj != null)
                    UnityEngine.Object.Destroy(cubeObj);
            }

            // --- Text ---
            var textObj = DebugObject.transform.Find("PropigatedText")?.gameObject;
            if (zVisitedManager.debugText)
            {
                TextMesh textMesh;
                if (textObj == null)
                {
                    textObj = new GameObject("PropigatedText");
                    textObj.transform.SetParent(DebugObject.transform, false);
                    textObj.transform.localPosition = Vector3.up * 0.15f;

                    textMesh = textObj.AddComponent<TextMesh>();
                    textMesh.fontSize = 32;
                    textMesh.characterSize = 0.05f;
                    textMesh.anchor = TextAnchor.MiddleCenter;
                    textMesh.alignment = TextAlignment.Center;
                    textMesh.color = Color.white;
                }
                else
                {
                    textMesh = textObj.GetComponent<TextMesh>();
                }

                if (textMesh != null)
                    textMesh.text = propigated.ToString();
            }
            else
            {
                if (textObj != null)
                    UnityEngine.Object.Destroy(textObj);
            }

            // --- Lines ---
            if (zVisitedManager.debugLines)
            {
                foreach (var connectedNode in connectedNodes)
                {
                    if (connectedNode == null) continue;

                    if (connectedNode.connectionLines != null &&
                        connectedNode.connectionLines.ContainsKey(this)) continue;

                    if (!connectionLines.TryGetValue(connectedNode, out var lr) || lr == null)
                    {
                        GameObject lineObj = new GameObject($"Line_{GetHashCode()}_{connectedNode.GetHashCode()}");
                        lineObj.transform.SetParent(DebugObject.transform, false);

                        lr = lineObj.AddComponent<LineRenderer>();
                        lr.positionCount = 2;
                        lr.startWidth = 0.05f;
                        lr.endWidth = 0.05f;
                        lr.material = new Material(Shader.Find("Unlit/Color"));
                        lr.material.color = new Color(0.1f, 0.1f, 0f, 0.2f);
                        lr.useWorldSpace = true;

                        connectionLines[connectedNode] = lr;
                    }

                    lr.SetPosition(0, position + Vector3.up * 0.25f);
                    lr.SetPosition(1, connectedNode.position + Vector3.up * 0.25f);
                }

                // Remove lines for nodes no longer connected
                var toRemove = connectionLines.Keys.Where(n => !connectedNodes.Contains(n)).ToList();
                foreach (var key in toRemove)
                {
                    if (connectionLines[key] != null)
                        UnityEngine.Object.Destroy(connectionLines[key].gameObject);
                    connectionLines.Remove(key);
                }
            }
            else
            {
                foreach (var lr in connectionLines.Values)
                {
                    if (lr != null)
                        UnityEngine.Object.Destroy(lr.gameObject);
                }
                connectionLines.Clear();
            }
        }

        internal VisitNode FindUnexplored(int depth = 0, int maxDepth = 0, OrderedSet<VisitNode> searched = null)
        {
            if (maxDepth == 0)
                maxDepth = zVisitedManager.unexploredMaxDepth;
            if (searched == null)
                searched = new();
            OrderedSet<VisitNode> unexploredConnections = getUnexploredNodes();
            if (unexploredConnections.Count > 0)
                return unexploredConnections[UnityEngine.Random.Range(0, unexploredConnections.Count)];
            else if (depth < maxDepth)
            {
                var ViableNodes = connectedNodes.Except(searched).ToList();
                if (ViableNodes.Count() == 0)
                    return null;
                List<VisitNode> shuffledNodes = new(ViableNodes);
                for (int i = shuffledNodes.Count - 1; i > 0; i--)
                {
                    int j = UnityEngine.Random.Range(0, i + 1);
                    var temp = shuffledNodes[i];
                    shuffledNodes[i] = shuffledNodes[j];
                    shuffledNodes[j] = temp;
                }
                foreach (var node in shuffledNodes)
                {
                    searched.Add(node);
                    var result = node.FindUnexplored(depth + 1, maxDepth, searched);
                    if (result != null)
                        return result;
                }
            }
            return null;
        }
    }
}

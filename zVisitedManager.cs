using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using Zombified_Initiative;
using static AIDangerZone;
using static RootMotion.FinalIK.AimPoser;
using static ZombieTweak2.zVisitedManager;

namespace ZombieTweak2
{
    public static class zVisitedManager
    {
        public const int NodeMapGridSize = 4;
        public const float NodeGridSize = 1f;
        public const float NodeVisitDistance = 10f;
        public static Dictionary<Vector3Int, HashSet<VisitNode>> NodeMap = new();
        private static bool setup = false;
        private static List<PlayerAgent> agents;
        private static List<PlayerAgent> botAgents;
        private const int areaMask = 1 << 0;
        internal static OrderedSet<VisitNode> nodesThatNeedConnectionChecks = new();
        internal static OrderedSet<nodeToCreate> nodesToCreate = new();
        private static int conectionCheckIndex = 0;
        private const int connectionChecksPerFrame = 5;
        private const int nodesCreatedPerFrame = 5;
        private static bool debug = true;
        private static PlayerAgent localPlayer;
        public static Vector3[] CapsuleCorners;
        public static int propigationSampleCount = 16;

        public static void Setup()
        {
            if (setup) 
                return;
            if (localPlayer == null)
                localPlayer = PlayerManager.GetLocalPlayerAgent();
            if (localPlayer == null) 
                return;
            if (localPlayer.Owner.refSessionMode != SNetwork.eReplicationMode.Playing)
            {
                setup = false;
                return;
            }
            CapsuleCorners = GetCapsuleCorners();
            NodeMap.Clear();
            agents = PlayerManager.PlayerAgentsInLevel.ToArray().ToList();
            botAgents = new();
            foreach (PlayerAgent agent in agents)
            {
                if (agent.Owner.IsBot)
                {
                    botAgents.Add(agent);
                }
            }
            setup = true;
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
            var gridPos = node.GetGridPosition();
            if (!NodeMap.ContainsKey(gridPos))
                NodeMap[gridPos] = new();
            NodeMap[gridPos].Add(node);
        }
        public static void Update()
        {
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
                    node.Propigate(5);
                    node.UpdateDebugCube();
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
                    if (conectionCheckIndex >= nodesThatNeedConnectionChecks.Count)
                        conectionCheckIndex = 0;
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
            node.UpdateDebugCube();
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
        public HashSet<VisitNode> connectedNodes = new();
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
        public HashSet<VisitNode> getUnexploredNodes()
        {
            HashSet<VisitNode> unexploredNodes = new();
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
            UpdateDebugCube();
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

        public void CreateDebugCube()
        {
            if (DebugObject != null)
                return;

            DebugObject = new GameObject($"VisitNode_Debug_{GetHashCode()}");
            DebugObject.transform.position = position + Vector3.up * 0.25f;

            // Cube visual
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(DebugObject.transform, false);
            cube.transform.localPosition = Vector3.zero;
            cube.transform.localScale = Vector3.one * 0.1f;

            // Disable cube physics
            var col = cube.GetComponent<Collider>();
            if (col != null)
                UnityEngine.Object.Destroy(col);

            // TextMesh on top
            var textObj = new GameObject("PropigatedText");
            textObj.transform.SetParent(DebugObject.transform, false);
            textObj.transform.localPosition = Vector3.up * 0.15f;

            var textMesh = textObj.AddComponent<TextMesh>();
            textMesh.text = propigated.ToString();
            textMesh.fontSize = 32;
            textMesh.characterSize = 0.05f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.white;
        }

        public void UpdateDebugCube()
        {
            if (DebugObject == null)
                CreateDebugCube();

            // Update cube color
            var cube = DebugObject.GetComponentInChildren<MeshRenderer>();
            if (cube != null)
            {
                if (cube.material == null)
                    cube.material = new Material(Shader.Find("Standard"));
                cube.material.color = discovered ? Color.green : Color.red;
            }

            // Update text
            var textMesh = DebugObject.GetComponentInChildren<TextMesh>();
            if (textMesh != null)
            {
                textMesh.text = propigated.ToString();
            }

            foreach (var connectedNode in connectedNodes)
            {
                if (connectedNode == null)
                    continue;

                // Check if the connected node already has a line to this node
                if (connectedNode.connectionLines != null && connectedNode.connectionLines.ContainsKey(this))
                    continue; // Skip, line already exists

                // Check if line already exists from this node to connectedNode
                if (!connectionLines.TryGetValue(connectedNode, out var lr) || lr == null)
                {
                    // Create a new GameObject to hold the LineRenderer
                    GameObject lineObj = new GameObject($"Line_{GetHashCode()}_{connectedNode.GetHashCode()}");
                    lineObj.transform.SetParent(DebugObject.transform, false);

                    lr = lineObj.AddComponent<LineRenderer>();
                    lr.positionCount = 2;
                    lr.startWidth = 0.05f;
                    lr.endWidth = 0.05f;
                    lr.material = new Material(Shader.Find("Unlit/Color"));
                    lr.material.color = new Color(0.2f,0.2f,0f);
                    lr.useWorldSpace = true;

                    connectionLines[connectedNode] = lr;
                }

                // Update positions every frame
                lr.SetPosition(0, position + Vector3.up * 0.25f);
                lr.SetPosition(1, connectedNode.position + Vector3.up * 0.25f);
            }

            // Optionally: remove any lines for nodes no longer navicable
            var toRemove = connectionLines.Keys.Where(n => !connectedNodes.Contains(n)).ToList();
            foreach (var key in toRemove)
            {
                if (connectionLines[key] != null)
                    UnityEngine.Object.Destroy(connectionLines[key].gameObject);
                connectionLines.Remove(key);
            }
        }
    }
}

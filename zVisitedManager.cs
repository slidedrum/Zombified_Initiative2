using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ZombieTweak2
{
    public static class zVisitedManager
    {
        public const int NodeMapGridSize = 10;
        public const float NodeGridSize = 2.5f;
        public const float NodeVisitDistance = 5f;
        public static Dictionary<Vector3Int, HashSet<VisitNode>> NodeMap = new();
        private static bool setup = false;
        private static List<PlayerAgent> agents;
        private static List<PlayerAgent> botAgents;

        public static void Setup()
        {
            if (setup) return;
            setup = true;
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
        }
        public static void Update()
        {
            if (!setup)
                Setup();
            foreach(PlayerAgent botAgent in botAgents)
            {
                HashSet<VisitNode> nearbyNodes = GetNearByNodes(botAgent.transform.position, NodeVisitDistance);
                foreach(VisitNode node in nearbyNodes)
                {

                }
            }
        }
        public static Vector3Int GetGridPosition(Vector3 pos)
        {
            return new Vector3Int(Mathf.FloorToInt(pos.x / NodeMapGridSize), Mathf.FloorToInt(pos.y / NodeMapGridSize), Mathf.FloorToInt(pos.z / NodeMapGridSize));
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
    }

    public class VisitNode
    {
        public GameObject DebugObject;
        public Vector3 position;
        public bool visited = false;
        public HashSet<VisitNode> connectedNodes = new();

        public VisitNode(Vector3 pos)
        {
            position = pos;
            connectedNodes = GetNearByNodes();
            foreach (var node in connectedNodes)
            {

            }
        }
        public HashSet<VisitNode> GetNearByNodes(float searchRadius = 0)
        {
            var nearybyNodes = zVisitedManager.GetNearByNodes(position, searchRadius);
            nearybyNodes.Remove(this);
            return nearybyNodes;
        }
    }
}

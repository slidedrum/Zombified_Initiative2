using System.Collections.Generic;
using UnityEngine;

namespace BotControl
{
    public class zSearch
    {
        public static Component FindBestAligned(Transform Look, HashSet<Il2CppSystem.Type> Types, float MaxDistance = 1000f, float Radius = 3, float MaxAngle = 180f)
        {
            HashSet<Component> candidates = FindAllInView(Look, Types, MaxDistance, Radius);
            return FindBestAligned(Look, candidates, MaxAngle);
        }
        public static GameObject FindBestAligned(Transform Look, HashSet<GameObject> Candidates, float MaxAngle = 180f)
        {
            return FindBestAligned(Look, Candidates, x => x.transform, MaxAngle);
        }
        public static Component FindBestAligned(Transform Look, HashSet<Component> Candidates, float MaxAngle = 180f)
        {
            return FindBestAligned(Look, Candidates, x => x.transform, MaxAngle);
        }
        public static T FindBestAligned<T>(Transform Look, HashSet<T> Candidates, System.Func<T, Transform> GetTransform, float MaxAngle = 180f)
            where T : UnityEngine.Object
        {
            Vector3 lookDirection = Look.forward;

            T bestCandidate = null;
            float bestAngle = MaxAngle;

            foreach (T candidate in Candidates)
            {
                Vector3 targetDirection =
                    (GetTransform(candidate).position - Look.position).normalized;

                float candidateAngle =
                    Vector3.Angle(lookDirection, targetDirection);

                if (candidateAngle < bestAngle)
                {
                    bestAngle = candidateAngle;
                    bestCandidate = candidate;
                }
            }

            return bestCandidate;
        }
        public static Component FindBestInView(Transform Look, Il2CppSystem.Type Type, float MaxDistance = 10000f, float radius = 3, float MaxAngle = 180f)
        {
            return FindBestInView(Look, new HashSet<Il2CppSystem.Type>() { Type }, out _, MaxDistance, radius, MaxAngle);
        }
        public static Component FindBestInView(Transform Look, HashSet<Il2CppSystem.Type> Types, float MaxDistance = 10000f, float Radius = 3, float MaxAngle = 180f)
        {
            return FindBestInView(Look, Types, out _, MaxDistance, Radius, MaxAngle);
        }
        public static Component FindBestInView(Transform Look, HashSet<Il2CppSystem.Type> types, out Il2CppSystem.Type Type, float MaxDistance = 10000f, float radius = 3, float MaxAngle = 180f)
        {
            Dictionary<Il2CppSystem.Type, HashSet<Component>> TypeLists = new();
            HashSet<Component> BigCandidateList = new();
            foreach (var type in types)
            {
                HashSet<Component> CandidateList = FindAllInView(Look, type, MaxDistance, radius);
                TypeLists[type] = CandidateList;
                BigCandidateList.UnionWith(CandidateList);
            }
            Component selected = FindBestAligned(Look, BigCandidateList, MaxAngle);
            Type = null;
            foreach(var kvp in TypeLists)
            {
                if (kvp.Value.Contains(selected))
                {
                    Type = kvp.Key;
                    break;
                }
            }
            return selected;
        }
        public static HashSet<Component> FindAllInView(Transform Look, Il2CppSystem.Type Type, float MaxDistance = 10000f, float radius = 3)
        {
            return FindAllInView(Look, new HashSet<Il2CppSystem.Type>() { Type }, MaxDistance, radius);
        }
        public static HashSet<Component> FindAllInView(Transform Look, HashSet<Il2CppSystem.Type> types, float MaxDistance = 10000f, float radius = 3)
        {
            Ray ray = new Ray(Look.position, Look.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, MaxDistance))
                return new();
            HashSet<Component> Candidates = FindAllNearby(hit.point, types, radius);
            return Candidates;
        }
        public static Component FindNearest(Vector3 Position, Il2CppSystem.Type Type, float Radius = 3)
        {
            return FindNearest(Position, new HashSet<Il2CppSystem.Type>() { Type }, Radius);
        }
        public static Component FindNearest(Vector3 Position, HashSet<Il2CppSystem.Type> Types, float Radius = 3)
        { 
            return FindNearest(Position, out _, Types , Radius);
        }
        public static Component FindNearest(Vector3 Position, out Il2CppSystem.Type Type, HashSet<Il2CppSystem.Type> Types, float Radius = 3)
        {
            HashSet<Component> Candidates = FindAllNearby(Position, Types, Radius);
            float minDistance = float.MaxValue;
            Component BestCandidate = null;
            Type = null;
            foreach (var candidate in Candidates)
            {
                float distance = Vector3.Distance(Position, candidate.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    BestCandidate = candidate;
                    foreach (var candidateType in Types)
                    {
                        if (candidateType.IsAssignableFrom(candidate.GetIl2CppType()))
                        {
                            Type = candidateType;
                            break;
                        }
                    }
                }
            }
            return BestCandidate;
        }
        public static HashSet<Component> FindAllNearby(Vector3 Position, Il2CppSystem.Type Type, float Radius = 3)
        {
            return FindAllNearby(Position, new HashSet<Il2CppSystem.Type>() { Type }, Radius);
        }
        public static HashSet<Component> FindAllNearby(Vector3 Position, HashSet<Il2CppSystem.Type> Types, float Radius = 3)
        {
            HashSet<Component> Candidates = new();
            Collider[] Nearby = Physics.OverlapSphere(Position, Radius);
            foreach (Collider collider in Nearby)
            {
                foreach (Il2CppSystem.Type Type in Types)
                {
                    Component componenet = collider.GetComponentInParent(Type);
                    if (componenet != null)
                    {
                        Candidates.Add(componenet);
                    }
                }
            }
            return Candidates;
        }
    }
}

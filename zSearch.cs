using AIGraph;
using LevelGeneration;
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
        public static OrderedSet<FoundObject> FoundObjects;

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
            
        }
    }
    public struct FoundObject 
    {
        public GameObject gameObject;
        public AIG_CourseNode courseNode;
        public LG_Zone zone;
        public LG_Area area;
    }
}

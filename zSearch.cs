using Enemies;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using LevelGeneration;
using Player;
using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieTweak2;

namespace Zombified_Initiative
{
    public static class zSearch
    {
        //this class is for finding stuff inside the world.
        
        #region obsolete
        [Obsolete]
        public static RaycastHit? RaycastHit()
        {
            if (Physics.Raycast(Camera.current.ScreenPointToRay(Input.mousePosition), out var hitInfo))
                return hitInfo;
            return null;
        }

        [Obsolete]
        public static Il2CppStructArray<RaycastHit> RaycastHits()
        {
            return Physics.RaycastAll(Camera.current.ScreenPointToRay(Input.mousePosition));
        }
        [Obsolete]
        public static ItemInLevel GetItemUnderPlayerAim()
        {
            return GetComponentUnderPlayerAim<ItemInLevel>
                (item => "Found item: " + item.PublicName);
        }
        [Obsolete]
        public static PlayerAgent GetHumanUnderPlayerAim()
        {
            var playerAIBot = GetComponentUnderPlayerAim<PlayerAIBot>
                (bot => "Found bot: " + bot.Agent.PlayerName);
            if (playerAIBot != null)
                return playerAIBot.Agent;

            var otherPlayerAgent = GetComponentUnderPlayerAim<PlayerAgent>
                (player => "Found other player: " + player.PlayerName);
            if (otherPlayerAgent != null)
                return otherPlayerAgent;

            var localPlayerAgent = PlayerManager.GetLocalPlayerAgent();
            ZiMain.log.LogInfo("Found local player: " + localPlayerAgent.PlayerName);
            return localPlayerAgent;
        }
        [Obsolete]
        public static EnemyAgent GetMonsterUnderPlayerAim()
        {
            return GetComponentUnderPlayerAim<EnemyAgent>
                (enemy => "Found monster: " + enemy.EnemyData.name, false);
        }
        [Obsolete]
        public static T GetComponentUnderPlayerAim<T>(System.Func<T, string> message, bool raycastAll = true) where T : class
        {
            if (raycastAll)
            {
                foreach (var raycastHit in RaycastHits())
                {
                    var component = raycastHit.collider.GetComponentInParent<T>();
                    if (component == null)
                        continue;

                    ZiMain.log.LogInfo(message(component));
                    return component;
                }
            }
            else
            {
                var raycastHit = RaycastHit();
                if (raycastHit.HasValue)
                {
                    var component = raycastHit.Value.collider.GetComponentInParent<T>();
                    if (component == null)
                        return null;

                    ZiMain.log.LogInfo(message(component));
                    return component;
                }
            }

            return null;
        }
        #endregion
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
    }
}

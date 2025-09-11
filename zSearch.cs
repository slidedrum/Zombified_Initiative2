using Dissonance;
using Enemies;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using LevelGeneration;
using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Zombified_Initiative;

namespace ZombieTweak2
{
    public static class zSearch
    {
        public static RaycastHit? RaycastHit()
        {
            if (Physics.Raycast(Camera.current.ScreenPointToRay(Input.mousePosition), out var hitInfo))
                return hitInfo;
            return null;
        }


        public static Il2CppStructArray<RaycastHit> RaycastHits()
        {
            return Physics.RaycastAll(Camera.current.ScreenPointToRay(Input.mousePosition));
        }
        public static ItemInLevel GetItemUnderPlayerAim()
        {
            return GetComponentUnderPlayerAim<ItemInLevel>
                (item => "Found item: " + item.PublicName);
        }

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
            Zi.log.LogInfo("Found local player: " + localPlayerAgent.PlayerName);
            return localPlayerAgent;
        }
        public static EnemyAgent GetMonsterUnderPlayerAim()
        {
            return GetComponentUnderPlayerAim<EnemyAgent>
                (enemy => "Found monster: " + enemy.EnemyData.name, false);
        }
        public static T GetComponentUnderPlayerAim<T>(System.Func<T, string> message, bool raycastAll = true) where T : class
        {
            if (raycastAll)
            {
                foreach (var raycastHit in RaycastHits())
                {
                    var component = raycastHit.collider.GetComponentInParent<T>();
                    if (component == null)
                        continue;

                    Zi.log.LogInfo(message(component));
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

                    Zi.log.LogInfo(message(component));
                    return component;
                }
            }

            return null;
        }
        public static GameObject GetClosestInLookDirection(Transform baseTransform,List<GameObject> candidates,float maxAngle = 180f)
        {
            if (baseTransform == null || candidates == null || candidates.Count == 0)
                return null;

            Vector3 basePos = baseTransform.position;
            Vector3 lookDir = baseTransform.forward;

            GameObject best = null;
            float bestAngle = float.MaxValue;

            foreach (var obj in candidates)
            {
                if (obj == null) continue;

                Vector3 toTarget = (obj.transform.position - basePos).normalized;
                float angle = Vector3.Angle(lookDir, toTarget);

                if (angle < bestAngle)
                {
                    bestAngle = angle;
                    best = obj;
                }
            }

            // enforce angle cutoff
            if (best != null && bestAngle <= maxAngle)
                return best;

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
        private static GameObject debugSphere;
        private static void ShowDebugSphere(Vector3 position, float radius)
        {
            if (debugSphere == null)
            {
                debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                debugSphere.name = "LookDirectionDebugSphere";

                // Remove collider so it doesn’t interfere with physics
                UnityEngine.Object.Destroy(debugSphere.GetComponent<Collider>());

                // Make it semi-transparent
                var renderer = debugSphere.GetComponent<Renderer>();
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = new Color(0f, 1f, 0f, 0.3f); // green, 30% opacity
                renderer.material.SetFloat("_Mode", 3); // Transparent mode
                renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                renderer.material.SetInt("_ZWrite", 0);
                renderer.material.DisableKeyword("_ALPHATEST_ON");
                renderer.material.EnableKeyword("_ALPHABLEND_ON");
                renderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                renderer.material.renderQueue = 3000;
            }

            debugSphere.transform.position = position;
            debugSphere.transform.localScale = Vector3.one * (radius * 2f); // scale to match search radius
        }
        public static List<GameObject> GetGameObjectsWithLookDirection<T>(Transform source,float searchRadius = 3,float rayDistance = 10000f) where T : Component
        {
            Ray ray = new Ray(source.position, source.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
            {
                ShowDebugSphere(hit.point, searchRadius);
                return GetObjectsWithComponentInRadius<T>(hit.point, searchRadius);
            }
            return new List<GameObject>();
        }
    }
}

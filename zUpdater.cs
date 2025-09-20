using BepInEx.Unity.IL2CPP.Utils;
using System.Collections;
using UnityEngine;
using Zombified_Initiative;
using HarmonyLib;
using Player;

namespace ZombieTweak2
{
    public class zUpdater : MonoBehaviour
    {   
        //This class handles stuff that would normally be handled by making things a monobehavior
        //For whatever reason I can't use custom types as args in methods in Il2cpp (╯°□°)╯︵ ┻━┻
        //So i'm doing this instead.  If there's a better way, lmk.  Maybe I'll refactor again.

        public static FlexibleEvent onUpdate = new();
        public static FlexibleEvent onLateUpdate = new();
        public static zUpdater Instance = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                ZiMain.log.LogWarning("Multiple updater instances created");
                return;
            }
            Instance = this;
        }
        public static void CreateInstance()
        {
            if (Instance == null)
            {
                GameObject go = new GameObject("zUpdater");
                Instance = go.AddComponent<zUpdater>();
            }
        }
        private void Update()
        {
            onUpdate?.Invoke();
        }

        private void LateUpdate()
        {
            onLateUpdate?.Invoke();
        }

        public static void InvokeStatic(FlexibleMethodDefinition method, float time)
        {
            if (Instance == null)
                CreateInstance();

            Instance.StartCoroutine(Instance.InvokeDelayed(method, time));
        }

        private IEnumerator InvokeDelayed(FlexibleMethodDefinition method, float time)
        {
            yield return new WaitForSeconds(time);
            method.method.DynamicInvoke(method.args);
        }
    }

    public class zCameraEvents : MonoBehaviour
    {
        // This class handles update loops tied to the camera, specifically the Unity callbacks: OnPreRender, OnPreCull, etc...
        // This behaviour is separate to zUpdater as it must be added to the FPSCamera which isn't available immediately on game load.

        public FlexibleEvent onPreRender = new();

        private void OnPreRender()
        {
            onPreRender?.Invoke();
        }
    }
}

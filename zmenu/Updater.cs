//using BepInEx.Unity.IL2CPP.Utils;
//using System.Collections;
//using UnityEngine;
//using UnityEngine.Rendering;

//namespace SlideMenu
//{
//    public class slideMenuUpdater : MonoBehaviour
//    {
//        //This class handles stuff that would normally be handled by making things a monobehavior
//        //For whatever reason I can't use custom types as args in methods in Il2cpp (?°?°)?? ???
//        //So i'm doing this instead.  If there's a better way, lmk.  Maybe I'll refactor again.

//        public static FlexibleEvent onUpdate = new();
//        public static FlexibleEvent onLateUpdate = new();
//        public static slideMenuUpdater Instance;

//        private void Awake()
//        {
//            if (Instance != null && Instance != this)
//            {
//                Destroy(this);
//                return;
//            }
//            //RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
//            Instance = this;
//        }
//        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
//        {
//            OnPreRender();
//        }
//        public static void CreateInstance()
//        {
//            if (Instance == null)
//            {
//                var cam = Camera.main;
//                if (cam == null)
//                {
//                    Debug.LogError("No main camera found for slideMenuUpdater!");
//                    return;
//                }
//                Instance = cam.gameObject.AddComponent<slideMenuUpdater>();
//            }
//        }
//        private void Update()
//        {
//            sMenuManager.Update();
//            onUpdate?.Invoke();
//        }
//        private void LateUpdate()
//        {
//            sMenuManager.LateUpdate();
//            onLateUpdate?.Invoke();
//        }
//        public static void InvokeStatic(FlexibleMethodDefinition method, float time)
//        {
//            if (Instance == null)
//                CreateInstance();

//            Instance.StartCoroutine(Instance.InvokeDelayed(method, time));
//        }

//        private IEnumerator InvokeDelayed(FlexibleMethodDefinition method, float time)
//        {
//            yield return new WaitForSeconds(time);
//            method.method.DynamicInvoke(method.args);
//        }
//        public FlexibleEvent onPreRender = new();
//        private void OnPreRender()
//        {
//            sMenuManager.PreRender();
//            onPreRender?.Invoke();
//        }
//    }
//}
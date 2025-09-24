using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ZombieTweak2.zMenu;

namespace ZombieTweak2
{
    public static class VisibilityVisualizer
    {
        private static Camera _observerCam;
        private static RenderTexture _rt;
        private static Texture2D _tex;
        private static Material _solidGreen;
        private static Material _litGreen; // reacts to lighting

        // Quads for visualization
        private static GameObject _fullQuad;
        private static GameObject _visibleQuad;
        private static Material _quadMat;



        static VisibilityVisualizer()
        {
            // Create hidden observer camera
            GameObject camObj = new GameObject("ObserverCam");
            camObj.hideFlags = HideFlags.HideAndDontSave;
            _observerCam = camObj.AddComponent<Camera>();
            _observerCam.enabled = false;
            _observerCam.clearFlags = CameraClearFlags.SolidColor;
            _observerCam.backgroundColor = Color.black;
            _observerCam.orthographic = false;
            _observerCam.nearClipPlane = 0.01f;
            _observerCam.farClipPlane = 1000f;

            // Low-res RT
            _rt = new RenderTexture(50, 50, 16, RenderTextureFormat.ARGB32);
            _observerCam.targetTexture = _rt;
            _tex = new Texture2D(50, 50, TextureFormat.RGB24, false);

            // Solid white material for rendering target
            _solidGreen = new Material(Shader.Find("Unlit/Color"));
            _solidGreen.color = Color.white;

            _litGreen = new Material(Shader.Find("Standard"));
            _litGreen.color = Color.white;
            _litGreen.SetFloat("_Glossiness", 0f); // optional: remove specular/shininess
            _litGreen.SetFloat("_Metallic", 0f);   // optional: keep fully diffuse

            // Material for quads
            _quadMat = new Material(Shader.Find("Unlit/Texture"));

            // Create floating quads (one for each pass)
            _fullQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _fullQuad.name = "FullSilhouetteQuad";
            _fullQuad.transform.localScale = Vector3.one * 0.5f;
            _fullQuad.GetComponent<MeshRenderer>().material = _quadMat;
            UnityEngine.Object.DontDestroyOnLoad(_fullQuad);

            _visibleQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _visibleQuad.name = "VisibleFractionQuad";
            _visibleQuad.transform.localScale = Vector3.one * 0.5f;
            _visibleQuad.GetComponent<MeshRenderer>().material = _quadMat;
            UnityEngine.Object.DontDestroyOnLoad(_visibleQuad);
        }

        public static float checkForObject(GameObject observer, GameObject target)
        {
            zMenuManager.CloseAllMenues();
            if (target == null) return 0f;

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return 0f;

            // Backup original materials
            List<Material[]> oldMats = new List<Material[]>();
            foreach (var renderer in renderers)
                oldMats.Add(renderer.materials.ToArray());

            // Swap to unlit white
            foreach (var renderer in renderers)
            {
                Material[] mats = new Material[renderer.materials.Length];
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = _solidGreen;
                renderer.materials = mats; // use instance materials
            }

            _visibleQuad.gameObject.SetActive(false);
            _fullQuad.gameObject.SetActive(false);

            // Position observer camera
            _observerCam.transform.position = observer.transform.position;
            _observerCam.transform.rotation = observer.transform.rotation;
            //_observerCam.transform.LookAt(target.transform.position);



            // ---- PASS 1: Target only ----
            var originalLayers = SaveOriginalLayers(target);
            int tempLayer = 31; // choose an unused layer
            SetLayerRecursively(target, tempLayer);
            _observerCam.cullingMask = 1 << tempLayer;
            _observerCam.clearFlags = CameraClearFlags.SolidColor;
            _observerCam.backgroundColor = Color.black;
            _observerCam.allowHDR = false;
            _observerCam.Render();
            Texture2D fullTex = new Texture2D(_rt.width, _rt.height, TextureFormat.RGB24, false);
            RenderTexture.active = _rt;
            fullTex.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0);
            fullTex.Apply();

            // Filter first texture
            FilterFullTexture(fullTex);

            int totalPixels = CountWhitePixels(fullTex); // modified CountWhitePixels to accept a texture

            // Restore original layers
            RestoreOriginalLayers(originalLayers);

            // ---- PASS 2: Target + Environment ----

            // Swap to lit white material for second pass
            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] mats = new Material[renderers[i].materials.Length];
                for (int j = 0; j < mats.Length; j++)
                    mats[j] = _litGreen;
                renderers[i].materials = mats;
            }

            _observerCam.cullingMask = ~0; // everything
            _observerCam.clearFlags = CameraClearFlags.Skybox; // or keep original
            _observerCam.allowHDR = false;
            _observerCam.Render();
            Texture2D visibleTex = new Texture2D(_rt.width, _rt.height, TextureFormat.RGB24, false);
            RenderTexture.active = _rt;
            visibleTex.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0);
            visibleTex.Apply();



            int visiblePixels = CountWhitePixels(visibleTex);

            RenderTexture.active = _rt;
            visibleTex.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0);
            visibleTex.Apply();
            RenderTexture.active = null;

            // Restore materials
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].materials = oldMats[i];

            // Apply first pass as mask
            ApplyMask(visibleTex, fullTex);

            _visibleQuad.gameObject.SetActive(true);
            _fullQuad.gameObject.SetActive(true);



            //Make color darker so no bloom.
            Color32[] pixels = fullTex.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i].r = (byte)(pixels[i].r * 0.25f);
                pixels[i].g = (byte)(pixels[i].g * 0.25f);
                pixels[i].b = (byte)(pixels[i].b * 0.25f);
            }
            fullTex.SetPixels32(pixels);
            fullTex.Apply();

            //pixels = visibleTex.GetPixels32();
            //for (int i = 0; i < pixels.Length; i++)
            //{
            //    pixels[i].r = (byte)(pixels[i].r * 0.25f);
            //    pixels[i].g = (byte)(pixels[i].g * 0.25f);
            //    pixels[i].b = (byte)(pixels[i].b * 0.25f);
            //}
            //visibleTex.SetPixels32(pixels);
            //visibleTex.Apply();

            // Display quads in front of target (billboard toward observer)
            Vector3 offset = observer.transform.forward * 0.5f; // distance in front of observer

            // Full silhouette quad (left)
            _fullQuad.transform.position = observer.transform.position + offset + observer.transform.right * -0.3f;
            _fullQuad.transform.rotation = Quaternion.LookRotation(_fullQuad.transform.position - observer.transform.position);
            _fullQuad.GetComponent<MeshRenderer>().material.mainTexture = fullTex;

            // Visible fraction quad (right)
            _visibleQuad.transform.position = observer.transform.position + offset + observer.transform.right * 0.3f;
            _visibleQuad.transform.rotation = Quaternion.LookRotation(_visibleQuad.transform.position - observer.transform.position);
            _visibleQuad.GetComponent<MeshRenderer>().material.mainTexture = visibleTex;

            // Remove colliders if they exist
            UnityEngine.Object.Destroy(_fullQuad.GetComponent<Collider>());
            UnityEngine.Object.Destroy(_visibleQuad.GetComponent<Collider>());

            // Return visible fraction
            return totalPixels > 0 ? (float)visiblePixels / totalPixels : 0f;
        }
        // Filter the first texture: pure white for target, pure black for everything else
        public static void FilterFullTexture(Texture2D tex)
        {
            Color32[] pixels = tex.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].r > 200 && pixels[i].g > 200 && pixels[i].b > 200)
                    pixels[i] = Color.white; // target pixel
                else
                    pixels[i] = Color.black; // everything else
            }
            tex.SetPixels32(pixels);
            tex.Apply();
        }
        public static Dictionary<GameObject, int> SaveOriginalLayers(GameObject obj)
        {
            var layers = new Dictionary<GameObject, int>();
            void Recurse(GameObject go)
            {
                if (go == null) return;
                layers[go] = go.layer;

                // For IL2CPP, get children safely
                var trans = go.transform;
                for (int i = 0; i < trans.childCount; i++)
                {
                    var childGo = trans.GetChild(i).gameObject;
                    Recurse(childGo);
                }
            }
            Recurse(obj);
            return layers;
        }

        public static void SetLayerRecursively(GameObject obj, int layer)
        {
            void Recurse(GameObject go)
            {
                if (go == null) return;
                go.layer = layer;
                var trans = go.transform;
                for (int i = 0; i < trans.childCount; i++)
                    Recurse(trans.GetChild(i).gameObject);
            }
            Recurse(obj);
        }

        private static void RestoreOriginalLayers(Dictionary<GameObject, int> layers)
        {
            foreach (var kvp in layers)
                kvp.Key.layer = kvp.Value;
        }
        // Filter the second texture: mask with first texture
        private static void ApplyMask(Texture2D visibleTex, Texture2D fullTex)
        {
            Color32[] visPixels = visibleTex.GetPixels32();
            Color32[] maskPixels = fullTex.GetPixels32();

            for (int i = 0; i < visPixels.Length; i++)
            {
                if (maskPixels[i] != Color.white) // if first texture is not white
                    visPixels[i] = Color.black;
            }

            visibleTex.SetPixels32(visPixels);
            visibleTex.Apply();
        }

        private static int CountWhitePixels(Texture2D tex)
        {
            Color32[] pixels = tex.GetPixels32();
            int count = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].r > 200 && pixels[i].g > 200 && pixels[i].b > 200)
                    count++;
            }
            return count;
        }
    }
}

using InControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ZombieTweak2.zMenu;

namespace ZombieTweak2
{
    internal class zVisiblityManager2
    {
        private static Camera _observerCam;
        private static RenderTexture _renderTexture;
        private static Texture2D _tex;
        private static Material _solidWhite;
        private static List<Color> colorsToCheck = new();
        private static Renderer[] targetRenderers;
        private static Texture2D visiblityTexture;


        // Quads for visualization
        private static GameObject _fullQuad;
        private static Material _quadMat;
        private static readonly Dictionary<Color, GameObject> _quads = new Dictionary<Color, GameObject>();
        static zVisiblityManager2()
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
            _renderTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
            _observerCam.targetTexture = _renderTexture;
            _tex = new Texture2D(512, 512, TextureFormat.RGB24, false);

            // Solid white material for rendering target
            _solidWhite = new Material(Shader.Find("Unlit/Color"));
            _solidWhite.color = Color.white;

            // Lit materials
            colorsToCheck.Add(Color.red);
            colorsToCheck.Add(Color.green);
            colorsToCheck.Add(Color.blue);

            // Material for quads
            _quadMat = new Material(Shader.Find("Unlit/Texture"));

            // Create floating quads (one for each pass)
            _fullQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _fullQuad.name = "FullSilhouetteQuad";
            _fullQuad.transform.localScale = Vector3.one * 0.5f;
            _fullQuad.GetComponent<MeshRenderer>().material = _quadMat;
            UnityEngine.Object.DontDestroyOnLoad(_fullQuad);
        }
        private static void CreateQuad(Transform position, Texture2D texture)
        {
            Vector3 offset = position.forward * 0.5f;
            _fullQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _fullQuad.name = "textureQuad";
            _fullQuad.transform.localScale = Vector3.one * 0.5f;
            _fullQuad.GetComponent<MeshRenderer>().material = _quadMat;
            _fullQuad.transform.position = position.position;
            _fullQuad.transform.rotation = position.rotation;
            _fullQuad.GetComponent<MeshRenderer>().material.mainTexture = texture;
        }

        public static float CheckForObject(GameObject observer, GameObject target)
        {
            zMenuManager.CloseAllMenues();
            if (target == null) return 0f;

            targetRenderers = target.GetComponentsInChildren<Renderer>();
            if (targetRenderers.Length == 0) return 0f;

            // Backup original materials
            List<Material[]> oldMats = new List<Material[]>();
            foreach (var renderer in targetRenderers)
                oldMats.Add(renderer.sharedMaterials.ToArray());

            // Swap to unlit white
            foreach (var renderer in targetRenderers)
            {
                Material[] mats = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = _solidWhite;
                renderer.sharedMaterials = mats;
            }

            _fullQuad.gameObject.SetActive(false);

            // Position observer camera
            _observerCam.transform.position = observer.transform.position;
            _observerCam.transform.rotation = observer.transform.rotation;

            // ---- PASS 1: Target only ----
            var originalLayers = SaveOriginalLayers(target);
            int tempLayer = 31; // choose an unused layer
            SetLayerRecursively(target, tempLayer);
            _observerCam.cullingMask = 1 << tempLayer;
            _observerCam.clearFlags = CameraClearFlags.SolidColor;
            _observerCam.backgroundColor = Color.black;
            _observerCam.allowHDR = false;
            _observerCam.Render();
            visiblityTexture = new Texture2D(_renderTexture.width, _renderTexture.height, TextureFormat.RGB24, false);
            RenderTexture.active = _renderTexture;
            visiblityTexture.ReadPixels(new Rect(0, 0, _renderTexture.width, _renderTexture.height), 0, 0);
            visiblityTexture.Apply();
            RestoreOriginalLayers(originalLayers);  // <- restore layers
            FilterTexture(visiblityTexture, Color.white);

            int totalVisblePixels = CountColorPixels(visiblityTexture, Color.white);

            //CREATE QUAD FOR visiblityTexture INFRONT OF AND TO THE LEFT OF observer

            _observerCam.cullingMask = ~0; // everything
            _observerCam.clearFlags = CameraClearFlags.Skybox; // or keep original
            _observerCam.Render();
            _observerCam.allowHDR = true;
            int numQuads = 0;
            foreach (Color color in colorsToCheck)
            {
                // 2. Run pass with colors
                Texture2D texture;
                int colorVisiblePixels = RunVisibilityPass(observer, target, color, out texture);
                GetOrCreateQuad(color, observer, texture, numQuads);
                numQuads++;
                //TODO calcuate visible.  Will do myself later.
            }

            // Restore materials
            for (int i = 0; i < targetRenderers.Length; i++)
                targetRenderers[i].sharedMaterials = oldMats[i];
            return 0f;
        }
        private static int RunVisibilityPass(GameObject observer, GameObject target, Color color, out Texture2D colorVisibleTexture)
        {
            Material litMat = CreatelitMaterial(color);

            // swap to colored mat
            foreach (var renderer in targetRenderers)
            {
                Material[] mats = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = litMat;
                renderer.sharedMaterials = mats;
            }
            _observerCam.Render();
            colorVisibleTexture = new Texture2D(_renderTexture.width, _renderTexture.height, TextureFormat.RGB24, false);
            RenderTexture.active = _renderTexture;
            colorVisibleTexture.ReadPixels(new Rect(0, 0, _renderTexture.width, _renderTexture.height), 0, 0);
            colorVisibleTexture.Apply();
            //ApplyMask(colorVisibleTexture, visiblityTexture);
            return CountColorPixels(colorVisibleTexture, color);
        }
        private static void FilterTexture(Texture2D tex, Color color, int tolerance = 5)
        {
            Color32[] pixels = tex.GetPixels32();
            byte kr = (byte)(color.r * 255);
            byte kg = (byte)(color.g * 255);
            byte kb = (byte)(color.b * 255);

            for (int i = 0; i < pixels.Length; i++)
            {
                int dr = pixels[i].r - kr;
                int dg = pixels[i].g - kg;
                int db = pixels[i].b - kb;
                int distSq = dr * dr + dg * dg + db * db;
                if (distSq > tolerance * tolerance)
                    pixels[i] = Color.black;
            }
            tex.SetPixels32(pixels);
            tex.Apply();
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

        private static Material CreatelitMaterial(Color color)
        {
            var mat = new Material(Shader.Find("Standard"));
            float mult = 1f;
            mat.color = new Color(color.r * mult, color.g * mult, color.b * mult);
            mat.SetFloat("_Glossiness", 0f);
            mat.SetFloat("_Metallic", 0f);
            return mat;
        }
        private static void ApplyMask(Texture2D targetTex, Texture2D maskTex, int tolerance = 5)
        {
            Color32[] targetPixels = targetTex.GetPixels32();
            Color32[] maskPixels = maskTex.GetPixels32();

            for (int i = 0; i < targetPixels.Length; i++)
            {
                var m = maskPixels[i];
                // If mask pixel is not (almost) white, black out the target pixel
                if (m.r < 255 - tolerance || m.g < 255 - tolerance || m.b < 255 - tolerance)
                    targetPixels[i] = Color.black;
            }

            targetTex.SetPixels32(targetPixels);
            targetTex.Apply();
        }
        private static int CountColorPixels(Texture2D tex, Color color, int tolerance = 20)
        {
            Color32[] pixels = tex.GetPixels32();
            byte kr = (byte)(color.r * 255);
            byte kg = (byte)(color.g * 255);
            byte kb = (byte)(color.b * 255);

            int count = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                int dr = pixels[i].r - kr;
                int dg = pixels[i].g - kg;
                int db = pixels[i].b - kb;
                int distSq = dr * dr + dg * dg + db * db;
                if (distSq < tolerance * tolerance)
                    count++;
            }
            return count;
        }
        private static GameObject GetOrCreateQuad(Color key, GameObject observer, Texture2D texture, int stackIndex)
        {
            if (!_quads.TryGetValue(key, out GameObject quad) || quad == null)
            {
                quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = $"Quad_{key}";
                quad.GetComponent<MeshRenderer>().material = new Material(_quadMat);
                UnityEngine.Object.DontDestroyOnLoad(quad);
                _quads[key] = quad;
            }

            // Position offset
            float forwardDist = 1.0f;   // in front of observer
            float sideOffset = 0.6f;    // to the right
            float verticalSpacing = 0.6f;

            Transform obsT = observer.transform;
            Vector3 basePos = obsT.position + obsT.forward * forwardDist;
            Vector3 side = obsT.right * sideOffset;                     // always right side
            Vector3 vertical = -obsT.up * (stackIndex * verticalSpacing); // stack downward

            quad.transform.position = basePos + side + vertical;
            quad.transform.rotation = obsT.rotation;
            quad.transform.localScale = Vector3.one * 0.5f;

            // Assign texture
            quad.GetComponent<MeshRenderer>().material.mainTexture = texture;
            UnityEngine.Object.Destroy(quad.GetComponent<Collider>());
            return quad;
        }
        private static void RestoreOriginalLayers(Dictionary<GameObject, int> layers)
        {
            foreach (var kvp in layers)
                kvp.Key.layer = kvp.Value;
        }
    }
}

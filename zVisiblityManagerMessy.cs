using CullingSystem;
using Enemies;
using ExteriorRendering;
using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ZombieTweak2
{
    internal class zVisiblityManagerMessy
    {
        private static Camera _observerCam;
        private static RenderTexture _renderTexture;
        private static Texture2D _tex;
        private static Material _solidWhite;
        private static List<Color> colorsToCheck = new();
        private static Renderer[] targetRenderers;
        private static Texture2D visiblityTexture;
        //Might be useful
        //	ExteriorLight	public static void UpdateShadows(Camera camera, float nearClip, float bias, float thickness, int blur)

        // Quads for visualization
        private static GameObject _fullQuad;
        private static Material _quadMat;
        private static readonly Dictionary<int, GameObject> _quads = new Dictionary<int, GameObject>();

        private static bool applyMask = true;
        private static float toleranceFloor = 0.1f;
        private static float toleranceCeling = 0.3f;
        public static float emmisivenessOn = 0.75f;
        public static float emmisivenessOff = 0.1f;
        public static bool alwaysEmit = false;
        public static Color AverageColor = Color.black;
        public static Color invertedColor = Color.white;
        static zVisiblityManagerMessy()
        {
            // Create hidden head camera
            GameObject camObj = new GameObject("ObserverCam");
            camObj.hideFlags = HideFlags.HideAndDontSave;
            _observerCam = camObj.AddComponent<Camera>();
            //_observerCam.CopyFrom(Camera.main);
            _observerCam.enabled = false;
            _observerCam.allowMSAA = false;
            //_observerCam.allowHDR = false;
            _observerCam.cullingMask = (1 << LayerMask.NameToLayer("Default"))
                                     | (1 << LayerMask.NameToLayer("Enemy"));
            _observerCam.useOcclusionCulling = false;
            _observerCam.farClipPlane = 20f;
            camObj.AddComponent<ExteriorCamera>();

            // Low-res RT
            _renderTexture = new RenderTexture(128, 128, 1, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                autoGenerateMips = false,
                antiAliasing = 1
            }; 
            _observerCam.targetTexture = _renderTexture;

            // Solid white material for rendering visTarget
            _solidWhite = new Material(Shader.Find("Unlit/Color"));
            _solidWhite.color = Color.white;

            // Lit materials
            colorsToCheck.Add(Color.red);
            colorsToCheck.Add(Color.green);
            colorsToCheck.Add(Color.blue);

            // Material for quads
            _quadMat = new Material(Shader.Find("Unlit/Texture"));
            _quadMat.mainTexture = _renderTexture;
            _quadMat.mainTexture.filterMode = FilterMode.Point;

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
        public static float CalculateVerticalFov(Vector3 observerPos, Bounds targetBounds)
        {
            float targetHeight = targetBounds.size.y;

            // Distance from camera to the bounds center
            float distance = Vector3.Distance(observerPos, targetBounds.center);

            if (distance < 1e-4f || targetHeight <= 0f)
                return 1f; // avoid divide-by-zero

            // Formula: fov = 2 * atan( h / (2d) )
            float fovRad = 2f * Mathf.Atan(targetHeight / (2f * distance));
            float fovDeg = fovRad * Mathf.Rad2Deg;

            return Mathf.Clamp(fovDeg, 0.0001f, 179f);
        }
        public static Bounds GetMaxBounds(GameObject go)
        {
            var bounds = GetRendererMaxBounds(go);
            bounds.Encapsulate(GetColliderMaxBounds(go));
            return bounds;
        }

        public static Bounds GetRendererMaxBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();

            if (renderers.Length == 0)
                return new Bounds(go.transform.position, Vector3.zero);

            var bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        public static Bounds GetColliderMaxBounds(GameObject go)
        {
            var colliders = go.GetComponentsInChildren<Collider>();

            if (colliders.Length == 0)
                return new Bounds(go.transform.position, Vector3.zero);

            var bounds = colliders[0].bounds;
            foreach (var collider in colliders)
            {
                bounds.Encapsulate(collider.bounds);
            }
            return bounds;
        }
        public static float CheckForObject(GameObject observer, GameObject target, PlayerAgent agent = null, GameObject debug = null)
        {
            //zMenuManager.CloseAllMenues();
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

            // Position head camera
            _observerCam.transform.position = observer.transform.position;
            var bounds = GetMaxBounds(target);
             _observerCam.transform.LookAt(bounds.center);
            _observerCam.fieldOfView = CalculateVerticalFov(_observerCam.transform.position, bounds);

            // ---- PASS 1: Target only ----
            var realcam = C_Camera.Current.m_camera;
            var realagent = C_Camera.Current.m_cullAgent;
            C_Camera.Current.m_camera = _observerCam;
            C_Camera.Current.m_cullAgent = agent?.gameObject?.GetComponent<C_MovingCuller>() ?? C_Camera.Current.m_cullAgent;
            C_Camera.Current.RunVisibility();
            _observerCam.Render();
            visiblityTexture = new Texture2D(_renderTexture.width, _renderTexture.height, TextureFormat.RGB24, false);
            visiblityTexture.filterMode = FilterMode.Point;
            visiblityTexture.wrapMode = TextureWrapMode.Clamp;
            RenderTexture.active = _renderTexture;
            visiblityTexture.ReadPixels(new Rect(0, 0, _renderTexture.width, _renderTexture.height), 0, 0);
            visiblityTexture.Apply();
            AverageColor = GetAverageColor(visiblityTexture, Color.white);
            invertedColor = InvertColor(AverageColor);
            GetOrCreateQuad(-1, debug ?? observer, visiblityTexture, -1);
            FilterTexture(visiblityTexture, Color.white);
            float totalVisblePixels = CountColorPixels(visiblityTexture, Color.white,0.4f,0.6f);
            // 2. Run pass with colors
            Texture2D texture;
            float colorVisiblePixels = RunVisibilityPass(observer, target, invertedColor, out texture);
            float visPercent = colorVisiblePixels / Math.Max(float.Epsilon,totalVisblePixels);
            C_Camera.Current.m_cullAgent = realagent;
            C_Camera.Current.m_camera = realcam;
            GetOrCreateQuad(1, debug ?? observer, texture, 1);
            //TODO calcuate visible.  Will do myself later.
            // Restore materials
            for (int i = 0; i < targetRenderers.Length; i++)
                targetRenderers[i].sharedMaterials = oldMats[i];
            ShowVisibilityNumber(visPercent, invertedColor);
            return visPercent;
        }

        private static float RunVisibilityPass(GameObject observer, GameObject target, Color color, out Texture2D colorVisibleTexture)
        {
            Material litMat = CreatelitMaterial(color, target.GetComponent<EnemyAgent>());

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
            if (applyMask)
                ApplyMask(colorVisibleTexture, visiblityTexture);
            return CountColorPixels(colorVisibleTexture, color, toleranceFloor, toleranceCeling);
        }
        private static void FilterTexture(Texture2D tex, Color color, int tolerance = 0)
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
        private static Color NormalizeColor(Color c)
        {
            float maxVal = Mathf.Max(c.r, c.g, c.b);
            if (maxVal > 0f)
                return new Color(c.r / maxVal, c.g / maxVal, c.b / maxVal, c.a);
            return c; // if all components are 0, return the original color
        }
        public static Color GetAverageColor(Texture2D tex, Color? ignoreColor = null, float tolerance = 0.01f)
        {
            Color32[] pixels = tex.GetPixels32();
            long r = 0, g = 0, b = 0, a = 0;
            int count = 0;

            foreach (Color32 c in pixels)
            {
                if (ignoreColor.HasValue)
                {
                    Color32 ic = ignoreColor.Value;
                    float dr = Mathf.Abs(c.r / 255f - ic.r);
                    float dg = Mathf.Abs(c.g / 255f - ic.g);
                    float db = Mathf.Abs(c.b / 255f - ic.b);
                    if (dr <= tolerance && dg <= tolerance && db <= tolerance)
                        continue; // skip this pixel
                }

                r += c.r;
                g += c.g;
                b += c.b;
                a += c.a;
                count++;
            }

            if (count == 0)
                return Color.clear;

            return new Color(r / (float)(count * 255), g / (float)(count * 255), b / (float)(count * 255), a / (float)(count * 255));
        }
        private static Color InvertColor(Color c)
        {
            Color.RGBToHSV(c, out float h, out float s, out float v);
            h = (h + 0.5f) % 1f; // shift hue 180 degrees
            //v = 1f - v;           // invert brightness
            Color result = Color.HSVToRGB(h, s, v);
            result.a = c.a;       // keep alpha
            return result;
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

        private static Material CreatelitMaterial(Color color, EnemyAgent agent = null)
        {
            var mat = new Material(Shader.Find("Standard"));
            float mult = 1f;
            mat.color = new Color(color.r * mult, color.g * mult, color.b * mult);
            // Enable emission keyword so Unity actually uses it
            mat.EnableKeyword("_EMISSION");
            // Set emission color slightly brighter than base
            mat.SetColor("_EmissionColor", color * (((agent?.AI?.m_detection?.m_noiseDetectionOn ?? false) || alwaysEmit) ? emmisivenessOn : emmisivenessOff));
            //mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            return mat;
        }
        private static void ApplyMask(Texture2D targetTex, Texture2D maskTex, int tolerance = 5)
        {
            Color32[] targetPixels = targetTex.GetPixels32();
            Color32[] maskPixels = maskTex.GetPixels32();

            for (int i = 0; i < targetPixels.Length; i++)
            {
                var m = maskPixels[i];
                // If mask pixel is not (almost) white, black out the visTarget pixel
                if (m.r < 255 - tolerance || m.g < 255 - tolerance || m.b < 255 - tolerance)
                    targetPixels[i] = Color.black;
            }

            targetTex.SetPixels32(targetPixels);
            targetTex.Apply();
        }

        private static void ShowVisibilityNumber(float visPercent, Color color, int stackIndex = 0)
        {
            color.r *= 0.2f;
            color.g *= 0.2f;
            color.b *= 0.2f;
            const int textKey = 999; // unique key for the TextMesh
            if (!_quads.TryGetValue(textKey, out GameObject textObj) || textObj == null)
            {
                textObj = new GameObject("VisibilityText");
                TextMesh tm = textObj.AddComponent<TextMesh>();
                tm.fontSize = 48;
                tm.characterSize = 0.02f;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.color = color;
                _quads[textKey] = textObj;
                UnityEngine.Object.DontDestroyOnLoad(textObj);
            }

            // Position to the left of the camera at same offset as quads
            Transform obsT = PlayerManager.GetLocalPlayerAgent().transform;
            float forwardDist = 1.0f;
            float sideOffset = -0.6f; // left side
            float verticalSpacing = 0.6f;

            Vector3 basePos = obsT.position + obsT.forward * forwardDist + obsT.up * 1f;
            Vector3 side = obsT.right * -sideOffset;
            Vector3 vertical = -obsT.up * (stackIndex * verticalSpacing); // stack downward if needed
            textObj.transform.position = basePos + side + vertical;
            textObj.transform.rotation = obsT.rotation;

            // Update the text
            TextMesh textMesh = textObj.GetComponent<TextMesh>();
            textMesh.text = (visPercent * 100f).ToString("F1") + "%";
        }
        private static float CountColorPixels(Texture2D tex, Color color, float minTolerance = 0.1f, float maxTolerance = 0.3f)
        {
            
            Color32[] pixels = tex.GetPixels32();
            float total = 0f;
            float maxDistnace = 2f;
            minTolerance = minTolerance * maxDistnace;
            maxTolerance = maxTolerance * maxDistnace;
            Vector3 targetVector = new Vector3(color.r, color.g, color.b).normalized;

            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i] == Color.black)
                    continue;
                Vector3 pixelVector = new Vector3(pixels[i].r, pixels[i].g, pixels[i].b).normalized;
                float dist = Vector3.Distance(pixelVector, targetVector);
                float contribution;
                if (dist < minTolerance)
                {
                    contribution = 1f;
                }
                else if (dist > maxTolerance)
                {
                    contribution = 0f;
                }
                else
                {
                    contribution = 1 - ((dist - minTolerance) / (maxTolerance - minTolerance));
                }
                total += contribution;
            }
            return total;
        }
        private static GameObject GetOrCreateQuad(int key, GameObject observer, Texture2D texture, int stackIndex)
        {
            if (!_quads.TryGetValue(key, out GameObject quad) || quad == null)
            {
                quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = $"Quad_{key}";
                quad.GetComponent<MeshRenderer>().material = new Material(_quadMat);
                //quad.GetComponent<MeshRenderer>().material.color = new Color(0.2f, 0.2f, 0.2f);
                UnityEngine.Object.DontDestroyOnLoad(quad);
                _quads[key] = quad;
            }

            // Position offset
            float forwardDist = 1.0f;   // in front of head
            float sideOffset = 0.6f;    // to the right
            float verticalSpacing = 0.6f;

            Transform obsT = PlayerManager.GetLocalPlayerAgent().transform;
            Vector3 basePos = obsT.position + obsT.forward * forwardDist + obsT.up * 1f;
            Vector3 side = obsT.right * sideOffset;                     // always right side
            Vector3 vertical = -obsT.up * (stackIndex * verticalSpacing); // stack downward

            quad.transform.position = basePos + side + vertical;
            quad.transform.rotation = obsT.rotation;
            quad.transform.localScale = Vector3.one * 0.5f;
            quad.ChangeLayerRecursive(LayerMask.NameToLayer("Ignore Raycast"));

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

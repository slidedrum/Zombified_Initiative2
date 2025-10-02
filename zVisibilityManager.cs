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
    public static class zVisibilityManager
    {
        private static Camera observationCamera;
        private static GameObject observercamGobject;
        private static ExteriorCamera observerExteriroCamera;
        private static RenderTexture renderTextureAtlas;
        private static RenderTexture renderTexture;
        private static Texture2D textureAtlas;
        private static Texture2D scratchBoard;

        private static Material unlitMat;
        private static Material litMat;

        private static List<Material[]> originalMats = new();
        private static Renderer[] rendererCache;

        private static bool cullingCamInOriginalState 
        { 
            get 
            { 
                return real_C_Cam_cam   == C_Camera.Current.m_camera 
                    && real_C_Cam_agent == C_Camera.Current.m_cullAgent;
            } 
        }
        private static Camera real_C_Cam_cam = C_Camera.Current.m_camera;
        private static C_MovingCuller real_C_Cam_agent = C_Camera.Current.m_cullAgent;

        public enum visMethods
        {
            VeryFancy,  //Render textures
            Fancy,      //Bounds corner raycasts
            Basic,      //BasicRaycasts
            VeryBasic,  //Single raycast
        }
        public struct visSettings
        {
            public float maxDistance;
            public visMethods visMethod;
            public bool resetCullingCam;
            public visSettings()
            {
                maxDistance = 30f;
                visMethod = visMethods.VeryFancy;
                resetCullingCam = true;
            }
        }
        static zVisibilityManager()
        {
            observercamGobject = new GameObject("ObserverCam");
            observerExteriroCamera = observercamGobject.AddComponent<ExteriorCamera>();
            renderTexture = new RenderTexture(Settings.resolution.x, Settings.resolution.y, 1);
            renderTextureAtlas = new RenderTexture(Settings.resolution.x, Settings.resolution.y*3, 1);
            Setup.SetUpObservationCamera();
            Setup.SetUpMaterals();
            textureAtlas = new(Settings.resolution.x, Settings.resolution.y * 3, TextureFormat.RGBA32, false);
            scratchBoard = new(Settings.resolution.x, Settings.resolution.y, TextureFormat.RGBA32, false);

        }
        private static class Setup
        {
            public static void SetUpObservationCamera()
            {
                observationCamera = observercamGobject.AddComponent<Camera>();
                observationCamera.enabled = false;
                observationCamera.allowMSAA = false;
                observationCamera.useOcclusionCulling = false;
                observationCamera.farClipPlane = 20f;
                observationCamera.targetTexture = renderTexture;
                observationCamera.cullingMask = (1 << LayerMask.NameToLayer("Default"))
                                              | (1 << LayerMask.NameToLayer("Enemy"))
                                              | (1 << LayerMask.NameToLayer("Dynamic"));
            }
            public static void SetUpMaterals()
            {
                unlitMat = new Material(Shader.Find("Unlit/Color"));
                unlitMat.color = Color.white;
                litMat = new Material(Shader.Find("Standard"));
                litMat.color = Color.white;
                litMat.EnableKeyword("_EMISSION");
            }
        }
        public static class Settings
        {
            public static Vector2Int resolution;
            static Settings()
            {
                resolution = new Vector2Int(32, 32);
            }
        }
        public static class HelperMethods
        {
            public static void CopyTextureAtlasToCpu()
            {
                RenderTexture prev = RenderTexture.active;
                RenderTexture.active = renderTextureAtlas;
                textureAtlas.ReadPixels(new Rect(0, 0, renderTextureAtlas.width, renderTextureAtlas.height), 0, 0, false);
                textureAtlas.Apply(false, false);
                RenderTexture.active = prev;
            }
            public static bool IsClose(Color32 c, Color target, float tol)
            {
                float r = c.r / 255f;
                float g = c.g / 255f;
                float b = c.b / 255f;

                return Mathf.Abs(r - target.r) <= tol &&
                       Mathf.Abs(g - target.g) <= tol &&
                       Mathf.Abs(b - target.b) <= tol;
            }
            public static Color GetAverageColor(RenderTexture renderTexture, Color ignoreColor, float tolerance = 0.01f)
            {
                if (renderTexture == null) return Color.clear;

                // Create a temporary Texture2D to read the RenderTexture
                RenderTexture prev = RenderTexture.active;
                RenderTexture.active = renderTexture;

                scratchBoard.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                scratchBoard.Apply();
                RenderTexture.active = prev;

                Color32[] pixels = scratchBoard.GetPixels32();
                long rSum = 0, gSum = 0, bSum = 0, count = 0;

                foreach (var color in pixels)
                {
                    // Skip dynamic ignore color1
                    if (IsClose(color, ignoreColor, tolerance)) continue;

                    // Skip black
                    if (color.r == 0 && color.g == 0 && color.b == 0) continue;

                    rSum += color.r;
                    gSum += color.g;
                    bSum += color.b;
                    count++;
                }
                if (count == 0) return Color.clear;
                return new Color(
                    rSum / (count * 255f),
                    gSum / (count * 255f),
                    bSum / (count * 255f),
                    1f
                );
            }
            public static void StoreMaterals(GameObject target)
            {
                originalMats.Clear();
                rendererCache = target.GetComponentsInChildren<Renderer>();
                foreach (var renderer in rendererCache)
                    originalMats.Add(renderer.sharedMaterials.ToArray());
            }
            public static void RestoreMaterals()
            {
                for (int i = 0; i < rendererCache.Length; i++)
                    rendererCache[i].sharedMaterials = originalMats[i];
                originalMats.Clear();
            }
            public static void SetMateral(Material materal)
            {
                foreach (var renderer in rendererCache)
                {
                    Material[] mats = new Material[renderer.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++)
                        mats[i] = materal;  // Not sure if this is the best way to do this.
                    renderer.sharedMaterials = mats;
                }
            }
            public static void CopyToAtlas(RenderTexture renderTexture, RenderTexture atlas, int index)
            {
                if (renderTexture == null || atlas == null)
                    throw new ArgumentNullException("RenderTexture or atlas is null.");

                if (index < 0 || index >= atlas.height / renderTexture.height)
                    throw new ArgumentOutOfRangeException(nameof(index), "Index out of bounds for atlas bands.");

                int bandHeight = renderTexture.height;
                int dstY = index * bandHeight;
                Graphics.CopyTexture(renderTexture, 0, 0, 0, 0, renderTexture.width, bandHeight,
                                     atlas, 0, 0, 0, dstY);
            }
            public static float CalculateVerticalFov(Vector3 observerPos, Bounds targetBounds)
            {
                float targetHeight = targetBounds.size.y;
                float distance = Vector3.Distance(observerPos, targetBounds.center);

                if (distance < 1e-4f || targetHeight <= 0f)
                    return 1f;

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

            public static Color HueShift(Color color)
            {
                Color.RGBToHSV(color, out float hue, out float sat, out float val);
                hue = (hue + 0.5f) % 1f;
                sat = sat > 0.001f ? sat : 0f;
                return Color.HSVToRGB(hue, sat, val, true);
            }
        }
        private static class VisDebug
        {
            public static bool debugMode = true;
            public static GameObject debugParrent;
            public static bool applyMask = true;
            public static float maxVisibilityforMaxScore = 0.3f;
            public static float minVisiblityForMinScore = 0.1f;
            public static float emmisivenessOn = 0.75f;
            public static float emmisivenessOff = 0.1f;
            public static bool alwaysEmit = false;
            public static Color AverageColor = Color.black;
            public static Color invertedColor = Color.white;
            public static readonly Dictionary<int, GameObject> debugQuads;
            static VisDebug()
            {
                debugParrent = new("zVisDebug");
                VisDebug.debugQuads = new();
            }
        }

        public static float CheckObjectVisiblity(GameObject target, GameObject observer)
        {
            visSettings settings = new visSettings();
            return CheckObjectVisiblity(target, observer, settings);
        }
        public static float CheckObjectVisiblity(GameObject target, GameObject observer, visMethods vismethod)
        {
            visSettings settings = new visSettings();
            settings.visMethod = vismethod;
            return CheckObjectVisiblity(target, observer, settings);
        }
        public static float CheckObjectVisiblity(GameObject target, GameObject observer, float maxDistance)
        {
            visSettings settings = new visSettings();
            settings.maxDistance = maxDistance;
            return CheckObjectVisiblity(target, observer, settings);
        }
        public static float CheckObjectVisiblity(GameObject target, GameObject observer, visSettings settings)
        {
            float ret = 0f;
            switch (settings.visMethod)
            {
                case visMethods.VeryFancy:
                    ret = VeryFancyObjectVisilityCheck(target, observer, settings);
                    break;
                case visMethods.Fancy:
                    ret= FancyObjectVisibilityChec(target, observer, settings);
                    break;
                case visMethods.Basic:
                    ret = BasicObjectVisibilityChec(target, observer, settings);
                    break;
                case visMethods.VeryBasic:
                    ret = VeryBasicObjectVisibilityChec(target, observer, settings);
                    break;
            }
            return ret;
        }
        private static float VeryFancyObjectVisilityCheck(GameObject target, GameObject observer, visSettings settings)
        {
            float ret = 0f;
            // Step 0: Move camera
            observercamGobject.gameObject.transform.position = observer.transform.position;
            var bounds = HelperMethods.GetMaxBounds(target);
            observercamGobject.transform.LookAt(bounds.center);
            observationCamera.fieldOfView = HelperMethods.CalculateVerticalFov(observer.transform.position, bounds);

            
            if (cullingCamInOriginalState) // Save culling camera settings
            {
                PlayerAgent agent = observer.GetComponent<PlayerAgent>(); // Set temp culling camera settings
                C_Camera.Current.m_camera = observationCamera; // This causes flicker, TODO
                C_Camera.Current.m_cullAgent = agent?.gameObject?.GetComponent<C_MovingCuller>() ?? C_Camera.Current.m_cullAgent; //If agent exists upate cullagent, if not don't touch it.
                C_Camera.Current.RunVisibility(); // Must recalculate culling with new position.
            }

            // Step 1: First pass render, whiteMat enemy only

            // Setup camera for render.
            HelperMethods.StoreMaterals(target); // Backup original materials
            HelperMethods.SetMateral(unlitMat);
            observationCamera.cullingMask = (1 << LayerMask.NameToLayer("Enemy"));
            observationCamera.Render(); // observationCamera.targetTexture = renderTexture;
            HelperMethods.CopyToAtlas(renderTexture, renderTextureAtlas,0);

            // Setp 2: Second pass render, whiteMat enemy + world
            observationCamera.cullingMask = (1 << LayerMask.NameToLayer("Default"))
                                          | (1 << LayerMask.NameToLayer("Enemy"))
                                          | (1 << LayerMask.NameToLayer("Dynamic"));
            litMat.color = Color.white;
            HelperMethods.SetMateral(litMat);
            observationCamera.Render(); // observationCamera.targetTexture = renderTexture;
            HelperMethods.CopyToAtlas(renderTexture, renderTextureAtlas, 1);

            // Step 3: Get color1 from second pass render
            var averageColor = HelperMethods.GetAverageColor(renderTexture, Color.white);
            var hueShiftColor = HelperMethods.HueShift(averageColor);
            if (VisDebug.debugMode)
            {
                VisDebug.AverageColor = averageColor;
                VisDebug.invertedColor = hueShiftColor;
            }
            // Setp 4: Third pass render, litcolor mat enemy + world
            litMat.color = averageColor;
            var enemyAgent = target.GetComponent<EnemyAgent>();
            bool shouldGlow = enemyAgent?.AI?.m_detection?.m_noiseDetectionOn ?? false;
            if (VisDebug.debugMode)
                shouldGlow = shouldGlow || VisDebug.alwaysEmit;
            litMat.SetColor("_EmissionColor", averageColor * (shouldGlow ? VisDebug.emmisivenessOn : VisDebug.emmisivenessOff));
            HelperMethods.SetMateral(litMat);
            observationCamera.Render(); // observationCamera.targetTexture = renderTexture;
            HelperMethods.CopyToAtlas(renderTexture, renderTextureAtlas, 2);
            HelperMethods.RestoreMaterals();
            // Restore culling camera
            if (settings.resetCullingCam && !cullingCamInOriginalState)
            {
                C_Camera.Current.m_cullAgent = real_C_Cam_agent;
                C_Camera.Current.m_camera = real_C_Cam_cam;
            }

            // Step 5: Move renderTextureAtlas to CPU mem,

            HelperMethods.CopyTextureAtlasToCpu();
            int bandHeight = Settings.resolution.y;
            int pass1offset = 0;
            int pass2offset = bandHeight;
            int pass3offset = bandHeight * 2;
            int totalPixels = 0;
            int width = textureAtlas.width;
            float totalValue = 0f;
            Color32[] pixels = textureAtlas.GetPixels32();
            Color GetPixel(int x, int y, int i)
            {
                return pixels[(i * bandHeight + y) * width + x];
            }
            void SetPixel(int x, int y, int i, Color color)
            {
                pixels[(i * bandHeight + y) * width + x] = color;
            }
            for (int y = 0; y < bandHeight; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color color1 = GetPixel(x, y, 0);
                    Color color2 = GetPixel(x, y, 1);
                    Color color3 = GetPixel(x, y, 2);
                    if (color1 == Color.black)
                        continue;
                    if (color1 == Color.white) // Step 6: Hue shift 1st pass render.
                    {
                        if (VisDebug.debugMode)
                            SetPixel(x, y, 0, hueShiftColor);
                        color1 = hueShiftColor;
                        totalPixels++;
                    }
                    if (color2 != Color.white) // Step 7: Mask 3rd pass render with 2nd pass render.
                    {
                        if (VisDebug.debugMode)
                            SetPixel(x, y, 2, Color.black);
                        color3 = Color.black;
                        continue;
                    }
                    // Step 8: Compare 1st pass and 3rd pass render to get final value
                    float contribution = 0f;
                    Color.RGBToHSV(color1, out float hue1, out float sat1, out float val1);
                    Color.RGBToHSV(color3, out float hue3, out float sat3, out float val3);
                    float hueDiff = Mathf.Abs(hue1 - hue3);
                    hueDiff = Mathf.Min(hueDiff, 1f - hueDiff);
                    float satDiff = Mathf.Abs(sat1 - sat3);
                    float hueSatDiff = Mathf.Sqrt(hueDiff * hueDiff + satDiff * satDiff);
                    float score = Mathf.Clamp01(1f - hueSatDiff); // [0,1], higher = more similar
                    contribution = score * val3; // darker pixels reduce contribution
                    totalValue += contribution;
                }
            }
            if (VisDebug.debugMode)
            {
                textureAtlas.SetPixels32(pixels);
                textureAtlas.Apply();
            }
            
            return totalPixels == 0 ? 0 : totalValue / totalPixels;
        }
        private static float FancyObjectVisibilityChec(GameObject target, GameObject observer, visSettings settings)
        {
            throw new NotImplementedException();
        }
        private static float BasicObjectVisibilityChec(GameObject target, GameObject observer, visSettings settings)
        {
            throw new NotImplementedException();
        }
        private static float VeryBasicObjectVisibilityChec(GameObject target, GameObject observer, visSettings settings)
        {
            throw new NotImplementedException();
        }
    }
}

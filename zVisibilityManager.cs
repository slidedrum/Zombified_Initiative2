using CullingSystem;
using Enemies;
using ExteriorRendering;
using FluffyUnderware.DevTools.Extensions;
using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieTweak2
{
    public static class zVisibilityManager
    {
        private static Camera _observationCamera;
        public static Camera observationCamera
        {
            get
            {
                if (_observationCamera == null)
                    Setup.SetUpObservationCamera();
                return _observationCamera;
            }
        }
        private static GameObject observercamGobject;
        private static ExteriorCamera observerExteriroCamera;
        private static FPSCamera fpsCamera;
        private static PreLitVolume PreLitVolume;
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
        private static Camera r_camera = new();
        private static Transform r_transform = new();

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
                maxDistance = 150f;
                visMethod = visMethods.VeryFancy;
                resetCullingCam = true;
            }
        }
        static zVisibilityManager()
        {
            observercamGobject = new GameObject("ObserverCam");
            observerExteriroCamera = observercamGobject.AddComponent<ExteriorCamera>();
            fpsCamera = observercamGobject.AddComponent<FPSCamera>();
            PreLitVolume = observercamGobject.AddComponent<PreLitVolume>();
            renderTexture = new RenderTexture(Settings.resolution.x, Settings.resolution.y, 16, RenderTextureFormat.ARGB32);
            renderTextureAtlas = new RenderTexture(Settings.resolution.x, Settings.resolution.y*3, 16, RenderTextureFormat.ARGB32);
            //Setup.SetUpObservationCamera();
            Setup.SetUpMaterals();
            textureAtlas = new(Settings.resolution.x, Settings.resolution.y * 3, TextureFormat.RGB24, false);
            scratchBoard = new(Settings.resolution.x, Settings.resolution.y, TextureFormat.RGB24, false);

        }
        private static class Setup
        {
            public static void SetUpObservationCamera()
            {
                _observationCamera = observercamGobject.GetComponent<Camera>();
                if (_observationCamera == null )
                    _observationCamera = observercamGobject.AddComponent<Camera>();
                observationCamera.enabled = false;
                observationCamera.allowMSAA = false;
                observationCamera.useOcclusionCulling = false;
                observationCamera.farClipPlane = 150f;
                observationCamera.targetTexture = renderTexture;
                observationCamera.clearFlags = CameraClearFlags.SolidColor;
                observationCamera.backgroundColor = Color.black;
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
                resolution = new Vector2Int(64, 64);
            }
        }
        public static class HelperMethods
        {

            public static void QuaternionToYawPitch(Quaternion q, out float yaw, out float pitch)
            {
                Vector3 f = q * Vector3.forward;
                yaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
                pitch = -Mathf.Asin(Mathf.Clamp(f.y, -1f, 1f)) * Mathf.Rad2Deg;
            }
            public struct CameraSettings
            {
                public float fieldOfView;
                public float nearClipPlane;
                public float farClipPlane;
                public bool allowMSAA;
                public bool useOcclusionCulling;
                public CameraClearFlags clearFlags;
                public Color backgroundColor;
                public LayerMask cullingMask;
                public Vector3 agentPosition;
                public Quaternion agentRotation;
                public Vector3 camPosition;
                public Quaternion camRotation;
                public RenderTexture targetTexture;
            }
            public static CameraSettings SaveSettings()
            {
                Camera cam = Camera.main;
                FPSCamera fps = cam.GetComponent<FPSCamera>();
                return new CameraSettings
                {
                    fieldOfView = cam.fieldOfView,
                    nearClipPlane = cam.nearClipPlane,
                    farClipPlane = cam.farClipPlane,
                    allowMSAA = cam.allowMSAA,
                    useOcclusionCulling = cam.useOcclusionCulling,
                    clearFlags = cam.clearFlags,
                    backgroundColor = cam.backgroundColor,
                    cullingMask = cam.cullingMask,
                    camPosition = cam.transform.position,
                    camRotation = cam.transform.rotation,
                    targetTexture = cam.targetTexture,
                    agentPosition = fps.Position,
                    agentRotation = fps.Rotation,

                };
            }
            public static void SetSettings(Camera source)
            {
                Camera cam = Camera.main;
                FPSCamera fps = cam.gameObject.GetComponent<FPSCamera>();
                cam.fieldOfView = source.fieldOfView;
                cam.nearClipPlane = source.nearClipPlane;
                cam.farClipPlane = source.farClipPlane;
                cam.allowMSAA = source.allowMSAA;
                cam.useOcclusionCulling = source.useOcclusionCulling;
                cam.clearFlags = source.clearFlags;
                cam.backgroundColor = source.backgroundColor;
                cam.cullingMask = source.cullingMask;
                cam.transform.position = source.transform.position;
                cam.transform.rotation = source.transform.rotation;
                cam.targetTexture = source.targetTexture;
                fps.Position = source.transform.position;
                fps.Rotation = source.transform.rotation;
                fps.m_owner.Position = source.transform.position;
                fps.m_owner.transform.rotation = source.transform.rotation;
                var yaw = fps.m_yaw;
                var pitch = fps.m_pitch;
                QuaternionToYawPitch(source.transform.rotation, out yaw, out pitch);
                fps.m_yaw = yaw;
                fps.m_pitch = pitch;
            }
            public static void SetSettings(CameraSettings settings)
            {
                Camera cam = Camera.main;
                FPSCamera fps = cam.GetComponent<FPSCamera>();
                cam.fieldOfView = settings.fieldOfView;
                cam.nearClipPlane = settings.nearClipPlane;
                cam.farClipPlane = settings.farClipPlane;
                cam.allowMSAA = settings.allowMSAA;
                cam.useOcclusionCulling = settings.useOcclusionCulling;
                cam.clearFlags = settings.clearFlags;
                cam.backgroundColor = settings.backgroundColor;
                cam.cullingMask = settings.cullingMask;
                cam.transform.position = settings.camPosition;
                cam.transform.rotation = settings.camRotation;
                cam.targetTexture = settings.targetTexture;
                fps.Position = settings.agentPosition;
                fps.Rotation = settings.agentRotation;
                fps.m_owner.Position = settings.agentPosition;
                fps.m_owner.transform.rotation = settings.agentRotation;
                var yaw = fps.m_yaw;
                var pitch = fps.m_pitch;
                QuaternionToYawPitch(settings.agentRotation, out yaw, out pitch);
                fps.m_yaw = yaw;
                fps.m_pitch = pitch;
            }
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
                return Color.red;
                if (renderTexture == null) return Color.clear;

                // Create a temporary Texture2D destination read the RenderTexture
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
                        mats[i] = materal;  // Not sure if this is the best way destination do this.
                    renderer.sharedMaterials = mats;
                }
            }
            public static void CopyToAtlas(int index)
            {
                if (renderTexture == null || renderTextureAtlas == null)
                    throw new ArgumentNullException("RenderTexture or atlas is null.");

                if (index < 0 || index >= renderTextureAtlas.height / renderTexture.height)
                    throw new ArgumentOutOfRangeException(nameof(index), "Index out of bounds for atlas bands.");

                int bandHeight = renderTexture.height;
                int dstY = index * bandHeight;
                Graphics.CopyTexture(renderTexture, 0, 0, 0, 0, renderTexture.width, bandHeight,
                                     renderTextureAtlas, 0, 0, 0, dstY);
            }
            public static float CalculateVerticalFov(Vector3 observerPos, Bounds targetBounds)
            {
                float targetHeight = targetBounds.size.y;
                float distance = Vector3.Distance(observerPos, targetBounds.center);

                if (distance < 1e-4f || targetHeight <= 0f)
                    return 1f;

                float fovRad = 2f * Mathf.Atan(targetHeight / (2f * distance));
                float fovDeg = fovRad * Mathf.Rad2Deg;

                return Mathf.Clamp(fovDeg + VisDebug.fovOffset, 0.0001f, 179f);
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
            internal static float fovOffset = 5f;
            public static readonly Dictionary<int, GameObject> debugQuads;
            static VisDebug()
            {
                debugParrent = new("zVisDebug");
                VisDebug.debugQuads = new();
            }

            internal static void CreateDebugHud(int index)
            {
                if (debugParrent == null)
                    debugParrent = new GameObject("zVisDebug");

                string hudName = $"DebugHudTexture_{index}";

                // Try destination find an existing HUD quad for this index
                if (!debugQuads.TryGetValue(index, out GameObject debugHud))
                {
                    debugHud = new GameObject(hudName);
                    debugHud.transform.SetParent(debugParrent.transform, false);

                    // Add Canvas if needed
                    Canvas canvas = debugHud.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 9999;

                    CanvasScaler scaler = debugHud.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);

                    debugHud.AddComponent<GraphicRaycaster>();

                    // Add RawImage destination show textureAtlas
                    GameObject rawImageObj = new GameObject("AtlasImage");
                    rawImageObj.transform.SetParent(debugHud.transform, false);
                    RawImage rawImage = rawImageObj.AddComponent<RawImage>();

                    // Set size and anchor destination bottom-right
                    RectTransform rt = rawImage.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(1f, 0f);
                    rt.anchorMax = new Vector2(1f, 0f);
                    rt.pivot = new Vector2(1f, 0f);

                    // Each index shifts left by atlas width + 60px padding
                    float xOffset = -70f - (index * (textureAtlas.width * 4 + 60f));
                    rt.anchoredPosition = new Vector2(xOffset, 10f);
                    rt.sizeDelta = new Vector2(textureAtlas.width * 4, textureAtlas.height * 4);

                    debugQuads[index] = debugHud;

                    // === Add labels for each band (stay on the right edge of atlas) ===
                    int bandHeight = Settings.resolution.y * 4; // scaled size per band
                    int numBands = textureAtlas.height / Settings.resolution.y;

                    for (int i = 0; i < numBands; i++)
                    {
                        GameObject labelObj = new GameObject($"Label_{i}");
                        labelObj.transform.SetParent(rawImageObj.transform, false);
                        Text label = labelObj.AddComponent<Text>();
                        label.text = i.ToString();
                        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                        label.fontSize = 20;
                        label.color = Color.white;
                        label.alignment = TextAnchor.MiddleLeft;

                        RectTransform lrt = label.GetComponent<RectTransform>();
                        lrt.anchorMin = new Vector2(1f, 0f);
                        lrt.anchorMax = new Vector2(1f, 0f);
                        lrt.pivot = new Vector2(0f, 0.5f);
                        lrt.anchoredPosition = new Vector2(5f, i * bandHeight + bandHeight / 2f);
                        lrt.sizeDelta = new Vector2(40f, 30f);
                    }

                    // === Add color swatches above the atlas ===
                    float swatchWidth = rt.sizeDelta.x / 2f;
                    float swatchHeight = 30f;

                    GameObject avgSwatchObj = new GameObject("AverageColorSwatch");
                    avgSwatchObj.transform.SetParent(rawImageObj.transform, false);
                    Image avgSwatch = avgSwatchObj.AddComponent<Image>();
                    avgSwatch.color = AverageColor;

                    RectTransform avgRt = avgSwatch.GetComponent<RectTransform>();
                    avgRt.anchorMin = new Vector2(0f, 1f);
                    avgRt.anchorMax = new Vector2(0f, 1f);
                    avgRt.pivot = new Vector2(0f, 0f);
                    avgRt.anchoredPosition = new Vector2(0f, 10f);
                    avgRt.sizeDelta = new Vector2(swatchWidth, swatchHeight);

                    GameObject hueSwatchObj = new GameObject("HueShiftColorSwatch");
                    hueSwatchObj.transform.SetParent(rawImageObj.transform, false);
                    Image hueSwatch = hueSwatchObj.AddComponent<Image>();
                    hueSwatch.color = invertedColor;

                    RectTransform hueRt = hueSwatch.GetComponent<RectTransform>();
                    hueRt.anchorMin = new Vector2(0f, 1f);
                    hueRt.anchorMax = new Vector2(0f, 1f);
                    hueRt.pivot = new Vector2(0f, 0f);
                    hueRt.anchoredPosition = new Vector2(swatchWidth, 10f);
                    hueRt.sizeDelta = new Vector2(swatchWidth, swatchHeight);
                }

                // Update atlas texture (source instead of live reference)
                RawImage img = debugHud.GetComponentInChildren<RawImage>();

                // Always create or update the source for this index
                Texture2D snapshot = new Texture2D(textureAtlas.width, textureAtlas.height, TextureFormat.RGBA32, false);
                snapshot.SetPixels(textureAtlas.GetPixels());
                snapshot.Apply();
                img.texture = snapshot;

                // Update swatch colors each call
                foreach (var image in debugHud.GetComponentsInChildren<Image>())
                {
                    if (image.name == "AverageColorSwatch") image.color = AverageColor;
                    if (image.name == "HueShiftColorSwatch") image.color = invertedColor;
                }
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
            if (target == null || observer == null) return 0f;

            //if (cullingCamInOriginalState) // Save culling camera source
            //{
            //    FPSCameraHolder c_holder = observer.GetComponent<FPSCameraHolder>(); // Set temp culling camera source
            //    PlayerAgent c_agent = c_holder.m_owner;
            //    C_Camera.Current.m_camera = observationCamera; // This causes flicker, TODO
            //    C_Camera.Current.m_cullAgent = c_agent?.gameObject?.GetComponent<C_MovingCuller>() ?? C_Camera.Current.m_cullAgent; //If c_agent exists upate cullagent, if not don't touch it.
            //    C_Camera.Current.RunVisibility(); // Must recalculate culling with new agentPosition.
            //}

            // Step 0: Move camera
            //PreLitVolume.Current.UpdateFogVolume = false;
            //PreLitVolume.Current.UpdateLitVolume = false;
            //PreLitVolume.Current.UpdateIndirectVolume = false;
            observercamGobject.gameObject.transform.position = observer.transform.position;
            var bounds = HelperMethods.GetMaxBounds(target);
            observercamGobject.transform.LookAt(bounds.center);
            observationCamera.fieldOfView = HelperMethods.CalculateVerticalFov(observer.transform.position, bounds);
            observationCamera.farClipPlane = settings.maxDistance;
            
            //This might be very cursed, but what if I move camera.main too?
            //var settingsbackup = HelperMethods.SaveSettings();
            //HelperMethods.SetSettings(observationCamera);
            //C_Camera.Current.GetComponent<FPSCamera>().OnPreCull();

            // Backup source materials
            HelperMethods.StoreMaterals(target); 

            // Step 1: First pass render, whiteMat enemy only
            HelperMethods.SetMateral(unlitMat);
            observationCamera.cullingMask = (1 << LayerMask.NameToLayer("Enemy"));
            observationCamera.Render(); // observationCamera.targetTexture = renderTexture;
            HelperMethods.CopyToAtlas(0);

            // Setp 2: Second pass render, whiteMat enemy + world
            observationCamera.cullingMask = -1;
            HelperMethods.SetMateral(unlitMat);
            observationCamera.Render(); // observationCamera.targetTexture = renderTexture;
            HelperMethods.CopyToAtlas(1);

            // Step 3: Get color1 source second pass render
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
            HelperMethods.CopyToAtlas(2);
            HelperMethods.RestoreMaterals();
            // Restore culling camera
            //if (settings.resetCullingCam && !cullingCamInOriginalState)
            //{
            //    C_Camera.Current.m_cullAgent = real_C_Cam_agent;
            //    C_Camera.Current.m_camera = real_C_Cam_cam;
            //}
            // Restore camera source.
            //HelperMethods.SetSettings(settingsbackup);
            //PreLitVolume.Current.UpdateFogVolume = true;
            //PreLitVolume.Current.UpdateLitVolume = true;
            //PreLitVolume.Current.UpdateIndirectVolume = true;
            // Step 5: Move renderTextureAtlas destination CPU mem,

            HelperMethods.CopyTextureAtlasToCpu();
            if (VisDebug.debugMode)
            {
                VisDebug.CreateDebugHud(0);
            }
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
                    pixels[(0 * bandHeight + y) * width + x].a = 255;
                    pixels[(1 * bandHeight + y) * width + x].a = 255;
                    pixels[(2 * bandHeight + y) * width + x].a = 255;
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
                    // Step 8: Compare 1st pass and 3rd pass render destination get final value
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
                VisDebug.CreateDebugHud(1);
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

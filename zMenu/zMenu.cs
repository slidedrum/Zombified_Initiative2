using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zombified_Initiative;

namespace ZombieTweak2.zMenu
{

    public partial class zMenu
    {
        // This class is the menu instance.

        private string name;
        public IEnumerable<zMenuNode> allNodes
        {
            get 
            {
                if (centerNode == null)
                    return nodes;
                return new[] { centerNode }.Concat(nodes.Where(n => n != centerNode));
            }
        }
        public OrderedSet<zMenuNode> nodes { get; private set; }
        public OrderedSet<zMenuNode> disabledNodes { get; private set; }
        public  zMenuNode centerNode { get; private set; }
        private zMenu _parrentMenu;
        internal int frameOpenedAt = Time.frameCount;
        internal float timeOpenedAt = Time.time;
        public zMenu parrentMenu { 
            get => _parrentMenu; 
            private set 
            {
                _parrentMenu = value;
                if (value != null)
                {
                    centerNode.ClearListeners(zMenuManager.nodeEvent.OnUnpressedSelected);
                    centerNode.AddListener(zMenuManager.nodeEvent.OnUnpressedSelected, _parrentMenu.Open);
                }
            } 
        }
        public GameObject gameObject;
        private Canvas canvas;
        public Vector3 RelativePosition = Vector3.zero;

        private Dictionary<zMenuManager.menuEvent, FlexibleEvent> eventMap;

        private FlexibleEvent OnOpened = new();
        private FlexibleEvent WhileOpened = new();
        private FlexibleEvent OnClosed = new();
        private FlexibleEvent WhileClosed = new();
        private FlexibleEvent OnSelected = new();
        private FlexibleEvent WhileSelected = new();
        private FlexibleEvent OnDeselected = new();
        private FlexibleEvent WhileDeselected = new();

        //settings
        private Vector3 canvasScale = new Vector3(0.02f, 0.02f, 0.02f);
        public float radius = 20f;
        private Color textColor = zMenuManager.defaultColor;
        private zMenuNode selectedNode;

        public zMenu(string arg_Name, zMenu arg_ParrentMenu = null)
        {
            nodes = new OrderedSet<zMenuNode>();
            name = arg_Name;
            gameObject = new GameObject($"zMenu {name}");
            gameObject.transform.SetParent(zMenuManager.menuParrent.transform);
            setupCanvas();
            FlexibleMethodDefinition onClose;
            if (arg_ParrentMenu != null)
                onClose = new FlexibleMethodDefinition(arg_ParrentMenu.Open);
            else
                onClose = new FlexibleMethodDefinition(Close);
            centerNode = new zMenuNode(name, this, onClose).SetTitle(arg_ParrentMenu != null ? arg_ParrentMenu.name : "Close");
            parrentMenu = arg_ParrentMenu;
            Close();
            eventMap = new Dictionary<zMenuManager.menuEvent, FlexibleEvent>(){ //I think all invokes are covered?  Might be missing one.
                                    { zMenuManager.menuEvent.OnOpened, OnOpened },
                                    { zMenuManager.menuEvent.WhileOpened, WhileOpened },
                                    { zMenuManager.menuEvent.OnClosed, OnClosed },
                                    { zMenuManager.menuEvent.WhileClosed, WhileClosed },
                                    { zMenuManager.menuEvent.OnSelected, OnSelected },
                                    { zMenuManager.menuEvent.WhileSelected, WhileSelected },
                                    { zMenuManager.menuEvent.OnDeselected, OnDeselected },
                                    { zMenuManager.menuEvent.WhileDeselected, WhileDeselected }};
        }
        public void UpdatePosition()
        {
            Vector3 newpos = zMenuManager.mainCamera.Position - RelativePosition;
            setPosition(newpos);
        }
        public void Update()
        {
        }
        public void Lateupdate()
        {
        }
        public void PreRender()
        {
            UpdatePosition();
            FaceCamera();
            if (gameObject.activeInHierarchy)
                WhileOpened.Invoke();
            else
                WhileClosed.Invoke();
            selectedNode = null;
            foreach (var node in allNodes)
            {
                node.Update();
            }
            if (selectedNode != null)
                WhileSelected.Invoke();
            else
                WhileDeselected.Invoke();
        }
        public zMenu Close()
        {
            setVisiblity(false);
            if (zMenuManager.currentMenu == this)
                zMenuManager.currentMenu = null;
            OnClosed.Invoke();
            return this; 
        }
        public zMenu Open()
        {
            FocusStateManager.ChangeState(eFocusState.FPS_CommunicationDialog);
            timeOpenedAt = Time.time;
            frameOpenedAt = Time.frameCount;
            if (!zMenuManager.menues.Contains(this))
                ZiMain.log.LogWarning($"Unregestered menu opened! ({name}) It may not close properly.");
            if (RelativePosition == Vector3.zero)
            {
                if (parrentMenu == null)
                { 
                    MoveInfrontOfCamera();
                }
                else
                { //TODO move SetRelativePosition into a listener so it can be disabled.
                    var node = parrentMenu.GetNode(name);
                    SetRelativePosition(zMenuManager.mainCamera.Position - node.gameObject.transform.position);
                }
            }
            ArrangeNodes();
            FaceCamera();
            setVisiblity(true);
            zMenu oldMenu = zMenuManager.currentMenu;
            zMenuManager.currentMenu = this;
            if (oldMenu != null && oldMenu != this)
                oldMenu.Close();
            OnOpened.Invoke();
            return this;
        }
        public zMenu SetRelativePosition(float x, float y, float z)
        {
            return SetRelativePosition(new Vector3 (x, y, z));
        }
        public zMenu SetRelativePosition(Vector3 relativePosition)
        {
            RelativePosition = relativePosition;
            return setPosition(zMenuManager.mainCamera.Position - RelativePosition);
        }
        public zMenu ResetRelativePosition(bool setPos = true)
        {
            RelativePosition = Vector3.zero;
            if (setPos)
                return setPosition(zMenuManager.mainCamera.Position - RelativePosition);
            return this;
        }
        private void AddDebugVisuals()
        {
            if (canvas == null)
            {
                Debug.LogWarning("zMenu: Tried to add debug visuals, but canvas is null!");
                return;
            }

            GameObject bgGO = new GameObject("DebugBackground");
            bgGO.transform.SetParent(canvas.transform, false);

            Image bgImage = bgGO.AddComponent<Image>();
            bgImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // 50% grey

            RectTransform bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;  // bottom-left
            bgRect.anchorMax = Vector2.one;   // top-right
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            GameObject textGO = new GameObject("DebugText");
            textGO.transform.SetParent(canvas.transform, false);

            TextMeshPro tmp = textGO.AddComponent<TextMeshPro>();
            tmp.text = $"Hello from {name}";
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            RectTransform textRect = tmp.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }
        private void setupCanvas()
        {
            if (canvas) return;
            GameObject canvasGO = new GameObject("Canvas");
            canvasGO.transform.SetParent(gameObject.transform, false);

            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            RectTransform rect = canvasGO.GetComponent<RectTransform>();
            rect.localPosition = Vector3.zero;      // center inside menu
            rect.localScale = canvasScale;  // scale down so it’s not huge
            gameObject.transform.position = zMenuManager.mainCamera.Position + zMenuManager.mainCamera.transform.forward * 1f;
            gameObject.transform.rotation = Quaternion.LookRotation(gameObject.transform.position - zMenuManager.mainCamera.Position);
        }
        public zMenu ArrangeNodes()
        {
            int count = nodes.Count;
            for (int i = 0; i < count; i++)
            {
                // Calculate angle for this node (start at top, go clockwise)
                float angle = 2 * Mathf.PI / count * i - Mathf.PI / 2; // subtract PI/2 = top start

                // Compute x and y positions (flip Y for clockwise)
                float x = radius * Mathf.Cos(angle);
                float y = -radius * Mathf.Sin(angle); // flip sign for clockwise

                // Set node position
                nodes[i].SetPosition(x, y);
            }
            return this;
        }
        public zMenu MoveInfrontOfCamera()
        {
            Vector3 position = zMenuManager.mainCamera.Position + zMenuManager.mainCamera.transform.forward * 1f;
            return SetRelativePosition(zMenuManager.mainCamera.Position - position);
        }
        public zMenu FaceCamera(bool menuOnly = false)
        {
            Quaternion rotation = Quaternion.LookRotation(gameObject.transform.position - zMenuManager.mainCamera.Position);
            if (!menuOnly)
                foreach (var node in nodes)
                    node.FaceCamera();
            return setRotation(rotation); 
        }
        public zMenu setPosition(float x, float y, float z)
        {
            return setPosition(new Vector3(x, y, z));
        }
        public zMenu setPosition(Vector3 pos)
        {
            gameObject.transform.position = pos;
            return this;
        }
        public zMenu setLocalPosition(float x, float y, float z)
        {
            return setLocalPosition(new Vector3(x, y, z));
        }
        public zMenu setLocalPosition(Vector3 pos)
        {
            gameObject.transform.localPosition = pos;
            return this;
        }
        public zMenu setRotation(Quaternion rot)
        {
            gameObject.transform.rotation = rot;
            return this;
        }
        public zMenu AddListener(zMenuManager.menuEvent arg_event, Action arg_method)
        {
            return AddListener(arg_event, (FlexibleMethodDefinition)arg_method);
        }
        public zMenu AddListener(zMenuManager.menuEvent arg_event, Delegate method, params object[] args)
        {
            var flex = new FlexibleMethodDefinition(method, args);
            return AddListener(arg_event, flex);
        }
        public zMenu AddListener(zMenuManager.menuEvent arg_event, FlexibleMethodDefinition arg_method)
        {
            if (eventMap.TryGetValue(arg_event, out var flexEvent))
            {
                flexEvent.Listen(arg_method);
            }
            return this;
        }
        public zMenu RemoveListener(zMenuManager.menuEvent arg_event, Action arg_method)
        {
            if (eventMap.TryGetValue(arg_event, out var flexEvent))
            {
                flexEvent.Unlisten(arg_method);
            }
            return this;
        }
        public zMenu RemoveListener(zMenuManager.menuEvent arg_event, FlexibleMethodDefinition arg_method)
        {
            if (eventMap.TryGetValue(arg_event, out var flexEvent))
            {
                flexEvent.Unlisten(arg_method);
            }
            return this;
        }
        public zMenu ClearListeners(zMenuManager.menuEvent arg_event)
        {
            if (eventMap.TryGetValue(arg_event, out var flexEvent))
            {
                flexEvent.ClearListeners();
            }
            return this;
        }
        public void DisableNode(zMenuNode node)
        {
            if (nodes.Contains(node))
            {
                nodes.Remove(node);
                node.gameObject.SetActive(false);
            }
            else
                ZiMain.log.LogWarning($"Could not find node {node.text} to disable from {name} menu");
        }
        public void DisableNode(string nodeText)
        {
            var nodeToDisable = nodes.FirstOrDefault(n => n.text == nodeText);
            if (nodeToDisable != null)
            {
                nodes.Remove(nodeToDisable);
                nodeToDisable.gameObject.SetActive(false);
            }
            else
                ZiMain.log.LogWarning($"Could not find node {nodeText} to disable from {name} menu");
        }
        public void EnableNode(string nodeText)
        {
            var nodeToEnable = disabledNodes.FirstOrDefault(n => n.text == nodeText);
            if (nodeToEnable != null)
            {
                disabledNodes.Add(nodeToEnable);
                nodeToEnable.gameObject.SetActive(true);
            }
            else
                ZiMain.log.LogWarning($"Could not find node {nodeText} to enable from {name} menu");
        }
        public zMenuNode AddNode(zMenu menu)
        {
            FlexibleMethodDefinition callback = new FlexibleMethodDefinition(menu.Open);
            menu.parrentMenu = this;
            return AddNode(menu.name, callback);
        }
        public zMenuNode AddNode(string arg_Name) 
        {
            Action callback = null;
            return AddNode(arg_Name, callback);
        }
        public zMenuNode AddNode(string arg_Name, Delegate method, params object[] args)
        {
            FlexibleMethodDefinition callback = new FlexibleMethodDefinition(method, args);
            return AddNode(arg_Name, callback);
        }
        public zMenuNode AddNode(string arg_Name, Action arg_callback)
        {
            FlexibleMethodDefinition callback = new(arg_callback);
            return AddNode(arg_Name,callback);
        }
        public zMenuNode AddNode(string arg_Name, FlexibleMethodDefinition callback)
        {
            zMenuNode node = new zMenuNode(arg_Name,this,callback);
            RegisterNode(node);
            return node;
        }
        public zMenuNode GetNode(string nodeName)
        {
            foreach (zMenuNode node in allNodes)
            {
                if (node.text == nodeName)
                {
                    return node;
                }
            }
            ZiMain.log.LogError($"Node {nodeName} not found in menu {name}");
            return null;
        }
        public zMenu RegisterNode(zMenuNode node)
        {
            nodes.Add(node);
            return this;
        }
        public zMenu setVisiblity(bool visible)
        {
            gameObject.SetActive(visible);
            return this;
        }
        public Color getTextColor()
        {
            return textColor;
        }
        public Canvas getCanvas()
        {
            return canvas;
        }
    }
}

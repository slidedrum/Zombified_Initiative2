using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlideMenu
{

    public partial class sMenu
    {
        // This class is the menu instance.

        private string name;
        public IEnumerable<sMenuNode> allNodes
        {
            get 
            {
                if (centerNode == null)
                    return nodes;
                return new[] { centerNode }.Concat(nodes.Where(n => n != centerNode));
            }
        }
        public OrderedSet<sMenuNode> nodes { get; private set; } = new();
        public OrderedSet<sMenuNode> disabledNodes { get; private set; } = new();
        public  sMenuNode centerNode { get; private set; }
        private sMenu _parrentMenu;
        internal int frameOpenedAt = Time.frameCount;
        internal float timeOpenedAt = Time.time;
        public sMenu parrentMenu { 
            get => _parrentMenu; 
            private set 
            {
                _parrentMenu = value;
                if (value != null)
                {
                    centerNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
                    centerNode.AddListener(sMenuManager.nodeEvent.OnUnpressedSelected, _parrentMenu.Open);
                }
            } 
        }
        public GameObject gameObject;
        private Canvas canvas;
        public Vector3 RelativePosition = Vector3.zero;

        private Dictionary<sMenuManager.menuEvent, FlexibleEvent> eventMap;
        public Dictionary<string, List<sMenuNode>> catagories = new();
        public List<sMenuNode> currentCatagory 
        { 
            get 
            {
                return catagories[currentCatagoryName];
            } 
        }
        private int catagoryIndex = 0;
        public string currentCatagoryName
        { 
            get 
            {
                return catagories.Keys.ToArray().ElementAt(catagoryIndex);
            }
        }

        private FlexibleEvent OnOpened = new();
        private FlexibleEvent WhileOpened = new();
        private FlexibleEvent OnClosed = new();
        private FlexibleEvent WhileClosed = new();
        private FlexibleEvent OnSelected = new();
        private FlexibleEvent WhileSelected = new();
        private FlexibleEvent OnDeselected = new();
        private FlexibleEvent WhileDeselected = new();
        private FlexibleEvent OnCatagoryChanged = new();

        private int pannelPositionWorkaround = 0; //TODO fix this properly

        //settings
        private Vector3 canvasScale = new Vector3(0.003f, 0.003f, 0.003f); //Really small because it's really close to the camera.
        public float radius;
        public float rotationalOffset = 0f;
        private Color textColor = sMenuManager.defaultColor;
        private sMenuNode selectedNode;
        private RectTransform rect;
        Dictionary<sMenuPannel.Side, sMenuPannel> pannels = new();

        public sMenu(string arg_Name, sMenu arg_ParrentMenu = null)
        {
            eventMap = new Dictionary<sMenuManager.menuEvent, FlexibleEvent>(){ //I think all invokes are covered?  Might be missing one.
                                    { sMenuManager.menuEvent.OnOpened, OnOpened },
                                    { sMenuManager.menuEvent.WhileOpened, WhileOpened },
                                    { sMenuManager.menuEvent.OnClosed, OnClosed },
                                    { sMenuManager.menuEvent.WhileClosed, WhileClosed },
                                    { sMenuManager.menuEvent.OnSelected, OnSelected },
                                    { sMenuManager.menuEvent.WhileSelected, WhileSelected },
                                    { sMenuManager.menuEvent.OnDeselected, OnDeselected },
                                    { sMenuManager.menuEvent.WhileDeselected, WhileDeselected },
                                    { sMenuManager.menuEvent.OnCatagoryChanged, OnCatagoryChanged }};
            radius = sMenuManager.defaultRadius;
            name = arg_Name;
            gameObject = new GameObject($"sMenu {name}");
            gameObject.transform.SetParent(sMenuManager.menuParrent.transform);
            setupCanvas();
            FlexibleMethodDefinition onClose;
            if (arg_ParrentMenu != null)
                onClose = new FlexibleMethodDefinition(arg_ParrentMenu.Open);
            else
                onClose = new FlexibleMethodDefinition(Close);
            centerNode = new sMenuNode(name, this, onClose).SetTitle(arg_ParrentMenu != null ? arg_ParrentMenu.name : "Close");
            parrentMenu = arg_ParrentMenu;
            //Open();
            //ArrangeNodes();
            Close();
        }
        internal void UpdateCatagoryByScroll()
        {
            if (catagories.Count() == 0)
                return;
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            int normalizedScroll = (int)Mathf.Sign(scroll);
            if (scroll == 0f)
                return;
            catagoryIndex += (int)normalizedScroll;
            if (catagoryIndex >= catagories.Count())
                catagoryIndex = 0;
            if (catagoryIndex < 0)
                catagoryIndex = catagories.Count() - 1;
            SetCatagory(catagories.Keys.ElementAt(catagoryIndex));
        }
        public void UpdateCatagoryNodes()
        {
            if (catagoryIndex >= catagories.Count())
                return;
            string catagory = catagories.Keys.ElementAt(catagoryIndex);
            centerNode.SetSubtitle($"<color=#00CC8466>[ </color>{catagory}<color=#00CC8466> ]</color>");
            if (catagory.ToLower() == "all")
            {
                foreach (var node in nodes)
                {
                    EnableNode(node);
                }
                return;
            }
            foreach (var node in nodes)
            {
                if (catagories[catagory].Contains(node))
                    EnableNode(node);
                else
                    DisableNode(node);
            }
        }
        public void SetCatagory(string catagory)
        {
            if (!catagories.ContainsKey(catagory))
            {
                return;
            }
            centerNode.SetSubtitle($"<color=#00CC8466>[ </color>{catagory}<color=#00CC8466> ]</color>");
            if (catagory.ToLower() == "all")
            {
                foreach (var node in nodes)
                {
                    EnableNode(node);
                }
                return;
            }
            foreach (var node in nodes)
            {
                if (catagories[catagory].Contains(node))
                    EnableNode(node);
                else
                    DisableNode(node);
            }
            
            UpdatePannelPositions();
            OnCatagoryChanged.Invoke();
        }
        public void AddCatagory(string catagory)
        {
            if (!catagories.ContainsKey(catagory))
                catagories[catagory] = new();
        }
        public void AddNodeToCatagory(string catagory, sMenuNode node)
        {
            if (node == null)
            {
                return;
            }
            if (!catagories.ContainsKey(catagory))
                AddCatagory(catagory);
            catagories[catagory].Add(node);
        }
        public void AddNodeToCatagory(string catagory, string nodeName)
        {
            var node = GetNode(nodeName);
            if (node == null)
            {
                return;
            }
            AddNodeToCatagory(catagory, node);
        }
        public void UpdatePosition()
        {
            Vector3 newpos = sMenuManager.mainCamera.transform.position - RelativePosition;
            setPosition(newpos);
        }
        public void Update()
        {
            if (gameObject.activeInHierarchy)
                WhileOpened.Invoke();
            else
                WhileClosed.Invoke();
            selectedNode = null;
            foreach (var node in allNodes.Where(n => n.gameObject.activeInHierarchy).ToList())
            {
                node.Update();
            }
            if (selectedNode != null)
                WhileSelected.Invoke();
            else
                WhileDeselected.Invoke();
            if (pannelPositionWorkaround < 2)
                _UpdatePannelPositions();
        }
        public void Lateupdate()
        {
            UpdatePosition();
            FaceCamera();
        }
        public void PreRender()
        {

        }
        public sMenu Close()
        {
            setVisiblity(false);
            if (sMenuManager.currentMenu == this)
                sMenuManager.currentMenu = null;
            OnClosed.Invoke();
            return this; 
        }
        public void Open()
        {
            //FocusStateManager.ChangeState(eFocusState.FPS_CommunicationDialog);
            timeOpenedAt = Time.time;
            frameOpenedAt = Time.frameCount;
            if (parrentMenu == null)
            { 
                if (RelativePosition == Vector3.zero)
                    MoveInfrontOfCamera();
            }
            else
            { 
                //TODO move SetRelativePosition into a listener so it can be disabled.
                var node = parrentMenu.GetNode(name);
                SetRelativePosition(sMenuManager.mainCamera.transform.position - node.gameObject.transform.position);
            }
            FaceCamera();
            setVisiblity(true);
            ArrangeNodes();
            sMenuManager.previousMenu = sMenuManager.currentMenu;
            sMenuManager.currentMenu = this;
            if (sMenuManager.previousMenu != null && sMenuManager.previousMenu != this)
                sMenuManager.previousMenu.Close();
            OnOpened.Invoke();
        }
        public sMenuPannel AddPannel(sMenuPannel.Side side, string initialText = "")
        {
            sMenuPannel newPannel = new sMenuPannel(side, this);
            if (pannels.ContainsKey(side))
            {
                if (initialText != "")
                    pannels[side].addLine(initialText);
                return pannels[side];
            }
            if (initialText != "")
                newPannel.addLine(initialText);
            pannels[side] = newPannel;
            return newPannel;
        }
        public sMenu SetRelativePosition(float x, float y, float z)
        {
            return SetRelativePosition(new Vector3 (x, y, z));
        }
        public sMenu SetRelativePosition(Vector3 relativePosition)
        {
            RelativePosition = relativePosition;
            return setPosition(sMenuManager.mainCamera.transform.position - RelativePosition);
        }
        public sMenu ResetRelativePosition(bool setPos = true)
        {
            RelativePosition = Vector3.zero;
            if (setPos)
                return setPosition(sMenuManager.mainCamera.transform.position - RelativePosition);
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

            rect = canvasGO.GetComponent<RectTransform>();
            rect.localPosition = Vector3.zero;      // center inside menu
            rect.localScale = canvasScale;  // scale down so it’s not huge
            gameObject.transform.position = sMenuManager.mainCamera.transform.position + sMenuManager.mainCamera.transform.forward * 1f;
            gameObject.transform.rotation = Quaternion.LookRotation(gameObject.transform.position - sMenuManager.mainCamera.transform.position);
        }
        internal void Setsize(float scale)
        {
            rect.localScale = canvasScale * scale;
        }
        public sMenu ArrangeNodes()
        {
            var activeNodes = nodes.Where(n => n.gameObject.activeInHierarchy).ToList();
            float additionalOffset = 0f;
            if (rotationalOffset == 0f && activeNodes.Count() == 4)
                additionalOffset = 45f;
            if (rotationalOffset == 0f && activeNodes.Count() == 2)
                additionalOffset = 90f;
            float offsetRadians = (rotationalOffset + additionalOffset) * Mathf.Deg2Rad;

            int count = activeNodes.Count;

            if (count != 0)
            {
                for (int i = 0; i < count; i++)
                {
                    // Calculate angle for this node (start at top, go clockwise)
                    float angle = 2 * Mathf.PI / count * i - Mathf.PI / 2 + offsetRadians;

                    // Compute x and y positions (flip Y for clockwise)
                    float x = radius * Mathf.Cos(angle);
                    float y = -radius * Mathf.Sin(angle); // flip sign for clockwise

                    // Set node position
                    activeNodes[i].SetPosition(x, y);
                }
            }
            UpdatePannelPositions();
            return this;
        }
        public sMenu UpdatePannelPositions()
        {
            pannelPositionWorkaround = 0;
            return _UpdatePannelPositions();
        }
        private sMenu _UpdatePannelPositions()
        {
            pannelPositionWorkaround++;
            foreach (var pannel in pannels.Values)
            {
                pannel.UpdatePosition();
            }
            return this;
        }
        public sMenu MoveInfrontOfCamera()
        {
            Vector3 position = sMenuManager.mainCamera.transform.position + sMenuManager.mainCamera.transform.forward * 1f;
            return SetRelativePosition(sMenuManager.mainCamera.transform.position - position);
        }
        public sMenu FaceCamera(bool menuOnly = false)
        {
            
            Quaternion rotation = Quaternion.LookRotation(gameObject.transform.position - sMenuManager.mainCamera.transform.position);
            setRotation(rotation);
            if (!menuOnly)
                foreach (var node in allNodes)
                    node.FaceCamera();
            return this;
        }
        public sMenu setPosition(float x, float y, float z)
        {
            return setPosition(new Vector3(x, y, z));
        }
        public sMenu setPosition(Vector3 pos)
        {
            gameObject.transform.position = pos;
            return this;
        }
        public sMenu setLocalPosition(float x, float y, float z)
        {
            return setLocalPosition(new Vector3(x, y, z));
        }
        public sMenu setLocalPosition(Vector3 pos)
        {
            gameObject.transform.localPosition = pos;
            return this;
        }
        public sMenu setRotation(Quaternion rot)
        {
            gameObject.transform.rotation = rot;
            return this;
        }
        public sMenu AddListener(sMenuManager.menuEvent arg_event, Action arg_method)
        {
            return AddListener(arg_event, (FlexibleMethodDefinition)arg_method);
        }
        public sMenu AddListener(sMenuManager.menuEvent arg_event, Delegate method, params object[] args)
        {
            var flex = new FlexibleMethodDefinition(method, args);
            return AddListener(arg_event, flex);
        }
        public sMenu AddListener(sMenuManager.menuEvent arg_event, FlexibleMethodDefinition arg_method)
        {
            if (eventMap.TryGetValue(arg_event, out var flexEvent))
            {
                flexEvent.Listen(arg_method);
            }
            return this;
        }
        public sMenu RemoveListener(sMenuManager.menuEvent arg_event, Action arg_method)
        {
            if (eventMap.TryGetValue(arg_event, out var flexEvent))
            {
                flexEvent.Unlisten(arg_method);
            }
            return this;
        }
        public sMenu RemoveListener(sMenuManager.menuEvent arg_event, FlexibleMethodDefinition arg_method)
        {
            if (eventMap.TryGetValue(arg_event, out var flexEvent))
            {
                flexEvent.Unlisten(arg_method);
            }
            return this;
        }
        public sMenu ClearListeners(sMenuManager.menuEvent arg_event)
        {
            if (eventMap.TryGetValue(arg_event, out var flexEvent))
            {
                flexEvent.ClearListeners();
            }
            return this;
        }
        public void DisableNode(sMenuNode node)
        {
            if (!disabledNodes.Contains(node))
            {
                disabledNodes.Add(node);
                node.gameObject.SetActive(false);
                ArrangeNodes();
            }
            //else
            //    ZiMain.log.LogWarning($"Could not find node {node.text} to disable from {name} menu");
        }
        public void DisableNode(sMenu menu)
        {
            var nodeToDisable = nodes.FirstOrDefault(n => n.text == menu.centerNode.text);
            if (nodeToDisable != null)
            {
                DisableNode(nodeToDisable);
            }
        }
        public void DisableNode(string nodeText)
        {
            var nodeToDisable = nodes.FirstOrDefault(n => n.text == nodeText);
            if (nodeToDisable != null)
            {
                DisableNode(nodeToDisable);
            }
        }
        public void EnableNode(sMenuNode node)
        {
            if (disabledNodes.Contains(node))
            {
                disabledNodes.Remove(node);
                node.gameObject.SetActive(true);
                ArrangeNodes();
            }
        }
        public void EnableNode(string nodeText)
        {
            var nodeToEnable = disabledNodes.FirstOrDefault(n => n.text == nodeText);
            if (nodeToEnable != null)
            {
                EnableNode(nodeToEnable);
            }
        }
        public void EnableNode(sMenu menu)
        {
            var nodeToEnable = disabledNodes.FirstOrDefault(n => n.text == menu.centerNode.text);
            if (nodeToEnable != null)
            {
                EnableNode(nodeToEnable);
            }
            //else
            //    ZiMain.log.LogWarning($"Could not find node {menu.centerNode.text} to enable from {name} menu");
        }
        public sMenuNode AddNode(sMenu menu)
        {
            FlexibleMethodDefinition callback = new FlexibleMethodDefinition(menu.Open);
            menu.parrentMenu = this;
            return AddNode(menu.name, callback);
        }
        public sMenuNode AddNode(string arg_Name) 
        {
            Action callback = null;
            return AddNode(arg_Name, callback);
        }
        public sMenuNode AddNode(string arg_Name, Delegate method, params object[] args)
        {
            FlexibleMethodDefinition callback = new FlexibleMethodDefinition(method, args);
            return AddNode(arg_Name, callback);
        }
        public sMenuNode AddNode(string arg_Name, Action arg_callback)
        {
            FlexibleMethodDefinition callback = new(arg_callback);
            return AddNode(arg_Name,callback);
        }
        public sMenuNode AddNode(string arg_Name, FlexibleMethodDefinition callback)
        {
            sMenuNode node = new sMenuNode(arg_Name,this,callback);
            RegisterNode(node);
            ArrangeNodes();
            return node;
        }
        public sMenuNode GetNode()
        {
            return parrentMenu.GetNode(this);
        }
        public sMenuNode GetNode(sMenu menu)
        {
            if (menu.parrentMenu != this)
            {
                return null;
            }
            return GetNode(menu.centerNode.text);
        }
        public sMenuNode GetNode(string nodeName)
        {
            foreach (sMenuNode node in allNodes)
            {
                if (node.text == nodeName)
                {
                    return node;
                }
            }
            return null;
        }
        public sMenu RegisterNode(sMenuNode node)
        {
            nodes.Add(node);
            return this;
        }
        public sMenu setVisiblity(bool visible)
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
        public void setNodeSize(float size)
        {
            foreach (sMenuNode node in nodes)
            {
                node.SetSize(size);
            }
        }
        public Rect GetNodeBounds()
        {
            float xmin = float.MaxValue;
            float ymin = float.MaxValue;
            float xmax = float.MinValue;
            float ymax = float.MinValue;

            // Save the parent scale and temporarily reset it
            foreach (var node in allNodes)
            {
                var nodeTransform = node.gameObject.transform;

                // Temporarily normalize node scale to make rect math consistent
                var actualNodeScale = nodeTransform.localScale;
                nodeTransform.localScale = Vector3.one * sMenuManager.selectedNodeSizeMultiplier;

                // Get all rects under this node
                var rects = node.gameObject.GetComponentsInChildren<RectTransform>();
                foreach (var rectTransform in rects)
                {
                    // Get the rect in the rectTransform's local space
                    Rect rect = rectTransform.rect;

                    // Compute the 4 local corners
                    Vector3[] corners = new Vector3[4];
                    corners[0] = new Vector3(rect.xMin, rect.yMin, 0);
                    corners[1] = new Vector3(rect.xMax, rect.yMin, 0);
                    corners[2] = new Vector3(rect.xMax, rect.yMax, 0);
                    corners[3] = new Vector3(rect.xMin, rect.yMax, 0);

                    // Transform each corner into the coordinate space of the main gameObject
                    for (int i = 0; i < 4; i++)
                    {
                        Vector3 world = rectTransform.TransformPoint(corners[i]);
                        Vector3 localToParent = gameObject.transform.InverseTransformPoint(world);
                        localToParent.x /= canvas.transform.localScale.x;
                        localToParent.y /= canvas.transform.localScale.y;
                        localToParent.z /= canvas.transform.localScale.z;

                        xmin = Mathf.Min(xmin, localToParent.x);
                        xmax = Mathf.Max(xmax, localToParent.x);
                        ymin = Mathf.Min(ymin, localToParent.y);
                        ymax = Mathf.Max(ymax, localToParent.y);
                    }
                }

                // Restore node scale
                nodeTransform.localScale = actualNodeScale;
            }
            return Rect.MinMaxRect(xmin, ymin, xmax, ymax);
        }
    }
}

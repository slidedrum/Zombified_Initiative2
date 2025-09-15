using GTFO.API;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zombified_Initiative;

namespace ZombieTweak2.zMenu
{
    public static class zMenuManager
    {
        public static HashSet<zMenu> menues { get; private set; } = new();
        public static GameObject menuParrent;
        private static bool playerInControll = false;
        public  static zMenu mainMenu { get; private set; }
        public static zMenu currentMenu { get; internal set; }
        private static zMenu.zMenuNode selectedNode;

        public static Color defaultColor { get; private set; } = new Color(0.25f, 0.25f, 0.25f, 1f);
        public enum nodeEvent
        {
            OnPressed,
            WhilePressed,
            OnUnpressed,
            WhileUnpressed,
            OnSelected,
            WhileSelected,
            OnDeselected,
            WhileDeselected,
        }
        public enum menuEvent
        {
            OnOpened,
            WhileOpened,
            OnClosed,
            WhileClosed,
            OnSelected,
            WhileSelected,
            OnDeselected,
            WhileDeselected,
        }
        //static settings
        public static string menuParrentPath = "GUI/CellUI_Camera(Clone)/NavMarkerLayer";

        public static zMenu createMenu(string name, zMenu parrentMenu = null)
        {
            zMenu newMenu = new zMenu(name, parrentMenu);
            if (parrentMenu == null)
            {
                newMenu.centerNode.ClearListeners(zMenuManager.nodeEvent.OnPressed);
                newMenu.centerNode.AddListener(zMenuManager.nodeEvent.OnPressed, CloseAllMenues);
            }
            else
            {
                parrentMenu.AddNode(newMenu);
            }
            registerMenu(newMenu);
            return newMenu;
        }
        public static zMenu registerMenu(zMenu menu)
        {
            menues.Add(menu);
            return menu;
        }
        public static void Update()
        {
            playerInControll = FocusStateManager.CurrentState == eFocusState.FPS || FocusStateManager.CurrentState == eFocusState.Dead;
            if (playerInControll) 
            {
                bool menuOpen = currentMenu != null;
                bool nodeSelected = false;
                if (menuOpen) {

                    Dictionary<GameObject, zMenu.zMenuNode>  nodeDict = new(currentMenu.nodes.Count+1);
                    foreach (var node in currentMenu.allNodes)
                    {
                        nodeDict[node.gameObject] = node;
                    }
                    List<GameObject>  nodeList = nodeDict.Keys.ToList();
                    GameObject selectedNodeObject = zSearch.GetClosestInLookDirection(Camera.current.transform, nodeList, 10f);
                    selectedNode = null;
                    if (selectedNodeObject != null)
                        nodeDict.TryGetValue(selectedNodeObject, out selectedNode);
                    nodeSelected = selectedNode != null;
                    foreach (zMenu.zMenuNode node in nodeDict.Values)
                    {
                        if (node == selectedNode)
                        {
                            node.Select();
                        }
                        else
                        {
                            node.Deselect();
                        }
                    }
                    currentMenu.Update();
                }
                if (Input.GetKeyDown(KeyCode.M))
                {
                    if (menuOpen)
                    {
                        if (nodeSelected)
                        {
                            selectedNode.Press();
                        }
                        else
                        {
                            CloseAllMenues();
                        }
                    }
                    else
                    {
                        mainMenu.Open();
                    } 
                }
            }
        }
        public static void CloseAllMenues()
        {
            foreach (zMenu menu in menues)
            {
                menu.ResetRelativePosition();
                menu.Close();
            }
        }
        public static void LateUpdate()
        {
            if (playerInControll)
            {
                if (currentMenu != null)
                {
                    currentMenu.Lateupdate();
                }
            }
        }
        public static void OnFactoryBuildDone()
        {
            menuParrent = GameObject.Find(menuParrentPath);
            mainMenu = new zMenu("Main");
            mainMenu.centerNode.AddListener(zMenuManager.nodeEvent.OnPressed, CloseAllMenues);
            registerMenu(mainMenu);
        }
    }
    public partial class zMenu
    {
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
        public  zMenuNode centerNode { get; private set; }
        private zMenu _parrentMenu;
        private zMenu parrentMenu { 
            get => _parrentMenu; 
            set 
            {
                _parrentMenu = value;
                if (value != null)
                {
                    centerNode.ClearListeners(zMenuManager.nodeEvent.OnPressed);
                    centerNode.AddListener(zMenuManager.nodeEvent.OnPressed, _parrentMenu.Open);
                }
            } 
        }
        private GameObject gameObject;
        private Canvas canvas;
        private RectTransform rectTransform;
        private Vector3 RelativePosition = Vector3.zero;

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
        private Vector2 canvasSize = new Vector2(1000, 1000);
        private Vector3 canvasScale = new Vector3(0.002f, 0.002f, 0.002f);
        private float radius = 125f;
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
            centerNode = new zMenuNode(arg_ParrentMenu != null ? arg_ParrentMenu.name : "Close", this, onClose).SetTitle(name);
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
        public void Update()
        {
            Vector3 newpos = Camera.main.transform.position - RelativePosition;
            setPosition(newpos);
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
        public void Lateupdate()
        {

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
            if (!zMenuManager.menues.Contains(this))
                Zi.log.LogWarning($"Unregestered menu opened! ({name}) It may not clsoe properly.");
            setVisiblity(true);
            FaceCamera();
            ArrangeNodes();
            if (RelativePosition == Vector3.zero)
                MoveInfrontOfCamera();

            zMenu oldMenu = zMenuManager.currentMenu;
            zMenuManager.currentMenu = this;
            if (oldMenu != null && oldMenu != this)
                oldMenu.Close();
            OnOpened.Invoke();
            return this;
        }
        public zMenu ResetRelativePosition()
        {
            RelativePosition = Vector3.zero;
            return this;
        }
        public zMenu SetRelativePostion(Vector3 position)
        { 
            RelativePosition = position;
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

            TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
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
        public void setupCanvas()
        {
            if (canvas) return;
            GameObject canvasGO = new GameObject("Canvas");
            canvasGO.transform.SetParent(gameObject.transform, false);

            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10000f;

            RectTransform rect = canvasGO.GetComponent<RectTransform>();
            rect.sizeDelta = canvasSize; // size in "pixels"
            rect.localPosition = Vector3.zero;      // center inside menu
            rect.localScale = canvasScale;  // scale down so it’s not huge
            gameObject.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 1f;
            gameObject.transform.rotation = Quaternion.LookRotation(gameObject.transform.position - Camera.main.transform.position);
        }
        public void ArrangeNodes()
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
        }
        public zMenu MoveInfrontOfCamera()
        {
            Camera cam = Camera.main;
            Vector3 position = cam.transform.position + cam.transform.forward * 1f;
            setPosition(position);
            RelativePosition = cam.transform.position - gameObject.transform.position;
            return this;
        }
        public zMenu FaceCamera()
        {
            Quaternion rotation = Quaternion.LookRotation(gameObject.transform.position - Camera.main.transform.position);
            setRotation(rotation); 
            return this;
        }
        public zMenu setPosition(float x, float y, float z)
        {
            setPosition(new Vector3(x, y, z));
            return this;
        }
        public zMenu setPosition(Vector3 pos)
        {
            gameObject.transform.position = pos;
            return this;
        }
        public zMenu setLocalPosition(float x, float y, float z)
        {
            setLocalPosition(new Vector3(x, y, z));
            return this;
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
            zMenuNode node = new zMenuNode(arg_Name,this,callback).SetPosition(0,nodes.Count+1*100);
            RegisterNode(node);
            return node;
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
    public class OrderedSet<T> : IEnumerable<T>, IEnumerable
    {
        private readonly List<T> _list = new();
        private readonly Dictionary<T, int> _dict = new();

        public int Count => _list.Count;

        public bool Add(T item)
        {
            if (_dict.ContainsKey(item))
                return false;

            _list.Add(item);
            _dict[item] = _list.Count - 1;
            return true;
        }

        public bool Remove(T item)
        {
            if (!_dict.TryGetValue(item, out int index))
                return false;

            _dict.Remove(item);

            int lastIndex = _list.Count - 1;
            if (index != lastIndex)
            {
                T lastItem = _list[lastIndex];
                _list[index] = lastItem;
                _dict[lastItem] = index;
            }

            _list.RemoveAt(lastIndex);
            return true;
        }

        public void Clear()
        {
            _list.Clear();
            _dict.Clear();
        }
        public bool Contains(T item) => _dict.ContainsKey(item);

        public T this[int index] => _list[index];

        // Generic enumerator
        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();

        // Non-generic enumerator
        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

        public List<T> ToList() => new(_list);
    }
}

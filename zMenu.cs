using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zombified_Initiative;
using static ZombieTweak2.zMenu.zMenuNode;

namespace ZombieTweak2
{
    public static class zMenuManager
    {
        public static HashSet<zMenu> menues { get; private set; } = new();
        public static GameObject menuParrent;
        private static bool playerInControll = false;
        public  static zMenu mainMenu { get; private set; }
        public static zMenu currentMenu { get; internal set; }
        private static zMenu.zMenuNode selectedNode;

        //static settings
        public static string menuParrentPath = "GUI/CellUI_Camera(Clone)/NavMarkerLayer";

        public static zMenu createMenu(string name, zMenu parrentMenu = null)
        {
            zMenu newMenu = new zMenu(name, parrentMenu);
            if (parrentMenu == null)
            {
                newMenu.centerNode.ClearListeners(nodeEvent.OnPressed);
                newMenu.centerNode.AddListener(nodeEvent.OnPressed, CloseAllMenues);
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
            playerInControll = (FocusStateManager.CurrentState == eFocusState.FPS || FocusStateManager.CurrentState == eFocusState.Dead);
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
            mainMenu.centerNode.AddListener(nodeEvent.OnPressed, CloseAllMenues);
            registerMenu(mainMenu);
        }
    }
    public class zMenu
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
        private zMenu parrentMenu;
        private GameObject gameObject;
        private Canvas canvas;
        private RectTransform rectTransform;
        private Vector3 RelativePosition = Vector3.zero;
        //settings
        private Vector2 canvasSize = new Vector2(1000, 1000);
        private Vector3 canvasScale = new Vector3(0.002f, 0.002f, 0.002f);
        private float radius = 200f;
        private Color textColor = new Color(0.25f, 0.25f, 0.25f, 1f);

        public zMenu(string arg_Name, zMenu arg_ParrentMenu = null)
        {
            nodes = new OrderedSet<zMenuNode>();
            name = arg_Name;
            parrentMenu = arg_ParrentMenu;
            gameObject = new GameObject($"zMenu {name}");
            gameObject.transform.SetParent(zMenuManager.menuParrent.transform);
            setupCanvas();
            FlexibleMethodDefinition onClose;
            if (parrentMenu != null)
                onClose = new FlexibleMethodDefinition(parrentMenu.Open);
            else
                onClose = new FlexibleMethodDefinition(Close);
            centerNode = new zMenuNode(arg_Name, this, onClose);
            Close();
        }
        public zMenu Close()
        {
            setVisiblity(false);
            if (zMenuManager.currentMenu == this)
                zMenuManager.currentMenu = null;
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
                float angle = (2 * Mathf.PI / count) * i - Mathf.PI / 2; // subtract PI/2 = top start

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
            this.gameObject.transform.position = pos;
            return this;
        }
        public zMenu setLocalPosition(float x, float y, float z)
        {
            setLocalPosition(new Vector3(x, y, z));
            return this;
        }
        public zMenu setLocalPosition(Vector3 pos)
        {
            this.gameObject.transform.localPosition = pos;
            return this;
        }
        public zMenu setRotation(Quaternion rot)
        {
            gameObject.transform.rotation = rot;
            return this;
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
        public void Update()
        {
            Vector3 newpos = Camera.main.transform.position - RelativePosition;
            setPosition(newpos);
            FaceCamera();
            foreach (var node in allNodes)
            {
                node.Update();
            }
        }
        internal void Lateupdate()
        {

        }
        public class zMenuNode
        {
            public string text;
            public string prefix;
            public string suffix;
            public string fullText;
            public string title;
            public string subtitle;
            public string description;
            public bool selected = false;
            public bool pressed = false;
            public int pressedAt = Time.frameCount;
            public zMenu parrentMenu;
            private FlexibleEvent OnPressed = new();
            private FlexibleEvent WhilePressed = new();
            private FlexibleEvent OnUnpressed = new();
            private FlexibleEvent WhileUnpressed = new();
            private FlexibleEvent OnSelected = new();
            private FlexibleEvent WhileSelected = new();
            private FlexibleEvent OnDeselected = new();
            private FlexibleEvent WhileDeselected = new();
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
            private Dictionary<nodeEvent, FlexibleEvent> eventMap;
            public TextPart fullTextPart;
            public TextPart prefixPart;
            public TextPart suffixPart;
            public TextPart titlePart;
            public TextPart subtitlePart;
            public TextPart descriptionPart;
            public RectTransform rect;
            public GameObject gameObject;
            public Color color;

            //settings

            public zMenuNode(string arg_Name, zMenu arg_Menu, FlexibleMethodDefinition arg_Callback)
            {
                text = arg_Name;
                parrentMenu = arg_Menu;
                if (arg_Callback != null) OnPressed.Listen(arg_Callback);

                // Create node container
                gameObject = new GameObject($"zMenuNode {text}");
                gameObject.transform.SetParent(parrentMenu.canvas.transform, false);

                rect = gameObject.AddComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.localScale = Vector3.one;
                rect.sizeDelta = new Vector2(300, 0);

                // Node color for child TextParts
                color = parrentMenu.textColor;


                // Vertical layout for stacking text parts
                VerticalLayoutGroup layout = gameObject.AddComponent<VerticalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleCenter;  // stack from top
                layout.spacing = 2f;
                layout.childControlWidth = true;   // children will stretch horizontally
                layout.childControlHeight = false; // children keep their own height
                layout.childForceExpandWidth = true;  // stretch width to match container
                layout.childForceExpandHeight = false; // keep height per child

                // ContentSizeFitter makes the node expand vertically based on children
                ContentSizeFitter fitter = gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

                // Create the text in a TextPart
                titlePart = new TextPart(this, $"title {title}").SetScale(0.75f, 0.75f);
                fullTextPart = new TextPart(this, $"Hello from {text}");
                subtitlePart = new TextPart(this, $"subtitle {title}").SetScale(0.75f,0.75f);
                eventMap = new Dictionary<nodeEvent, FlexibleEvent>(){
                    { nodeEvent.OnPressed, OnPressed },
                    { nodeEvent.WhilePressed, WhilePressed },
                    { nodeEvent.OnUnpressed, OnUnpressed },
                    { nodeEvent.WhileUnpressed, WhileUnpressed },
                    { nodeEvent.OnSelected, OnSelected },
                    { nodeEvent.WhileSelected, WhileSelected },
                    { nodeEvent.OnDeselected, OnDeselected },
                    { nodeEvent.WhileDeselected, WhileDeselected }};
            }
            public zMenuNode Update()
            {
                if (selected)
                    WhileSelected.Invoke();
                else
                    WhileDeselected.Invoke();
                if (pressed)
                {
                    WhilePressed.Invoke();
                    if (Time.frameCount - pressedAt > 2)
                    {
                        Unpress();
                    }
                }
                else
                    WhileUnpressed.Invoke();
                FaceCamera();
                return this;
            }
            public zMenuNode SetPosition(float x, float y)
            {
                rect.anchoredPosition = new Vector2(x, y);
                return this;
            }
            public zMenuNode SetSize(float scale)
            {
                return SetSize(new Vector3(scale, scale, scale));
            }
            public zMenuNode SetSize(Vector3 scale)
            {
                rect.localScale = scale;
                return this;
            }
            public zMenuNode FaceCamera()
            {
                Quaternion rotation = Quaternion.LookRotation(gameObject.transform.position - Camera.main.transform.position);
                setRotation(rotation);
                return this;
            }
            public zMenuNode setRotation(Quaternion rot)
            {
                gameObject.transform.rotation = rot;
                return this;
            }
            public zMenuNode Deselect()
            {
                selected = false;
                SetSize(1f);
                OnDeselected.Invoke();
                return this;
            }
            public zMenuNode Select()
            {
                selected = true;
                SetSize(1.5f);
                OnSelected.Invoke();
                return this;
            }
            public zMenuNode Press()
            {
                if (!pressed)
                {
                    pressedAt = Time.frameCount;
                    OnPressed.Invoke();
                    pressed = true;
                }
                return this;
            }
            public zMenuNode Unpress()
            {
                if (pressed)
                {
                    OnUnpressed.Invoke();
                }
                pressed = false;
                return this;
            }
            public zMenuNode AddListener(nodeEvent arg_event, Action arg_method)
            {
                if (eventMap.TryGetValue(arg_event, out var flexEvent))
                {
                    flexEvent.Listen(arg_method);
                }
                return this;
            }
            public zMenuNode AddListener(nodeEvent arg_event, FlexibleMethodDefinition arg_method)
            {
                if (eventMap.TryGetValue(arg_event, out var flexEvent))
                {
                    flexEvent.Listen(arg_method);
                }
                return this;
            }
            public zMenuNode RemoveListener(nodeEvent arg_event, FlexibleMethodDefinition arg_method)
            {
                if (eventMap.TryGetValue(arg_event, out var flexEvent))
                {
                    flexEvent.Unlisten(arg_method);
                }
                return this;
            }
            public zMenuNode ClearListeners(nodeEvent arg_event)
            {
                if (eventMap.TryGetValue(arg_event, out var flexEvent))
                {
                    flexEvent.ClearListeners();
                }
                return this;
            }
            public class TextPart
            {
                public string text;
                public GameObject gameObject;
                public TextMeshProUGUI textMesh;
                public RectTransform rect;

                public TextPart(zMenuNode parent, string arg_Text)
                {
                    text = arg_Text;

                    // Child object under the node
                    gameObject = new GameObject($"TextPart {text}");
                    rect = gameObject.AddComponent<RectTransform>();
                    rect.SetParent(parent.rect, false);

                    // Add TMP
                    textMesh = gameObject.AddComponent<TextMeshProUGUI>();
                    textMesh.text = arg_Text;
                    textMesh.fontSize = 24;
                    textMesh.alignment = TextAlignmentOptions.Center;
                    textMesh.color = parent.color;

                    rect.anchoredPosition = Vector2.zero; // center inside node
                    rect.localScale = Vector3.one;


                    // Fixed width, height auto
                    rect.sizeDelta = new Vector2(300, 0);

                    // Make height auto-adjust to text
                    ContentSizeFitter fitter = gameObject.AddComponent<ContentSizeFitter>();
                    fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

                    TMP_FontAsset font = Resources.Load<TMP_FontAsset>("ShareTechMono-Regular_TMPro");
                    textMesh.font = font;

                }
                public TextPart SetPosition(Vector2 pos)
                {
                    SetPosition(pos.x, pos.y);
                    return this;
                }
                public TextPart SetPosition(float x, float y)
                {
                    rect.anchoredPosition = new Vector2(x, y);
                    return this;
                }
                public TextPart SetScale(Vector2 scale)
                {
                    SetScale (scale.x, scale.y,1);
                    return this;
                }
                public TextPart SetScale(Vector3 scale)
                {
                    SetScale(scale.x, scale.y, scale.z);
                    return this;
                }
                public TextPart SetScale(float x, float y)
                {
                    SetScale(x,y,1);
                    return this;
                }
                public TextPart SetScale(float x, float y, float z)
                {
                    rect.localScale = new Vector3(x, y,z);
                    return this;
                }
            }
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

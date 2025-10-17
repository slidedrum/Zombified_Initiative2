using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using ZombieTweak2;

namespace SlideMenu
{
    public partial class sMenu 
    {
        public partial class sMenuNode
        {
            //This is a menu node instance, there if you want to create one, call the parrent menu.addnode not this.

            private string _title = "";
            public string title { 
                get => _title.Trim(); 
                set 
                {
                    _title = value;
                    UpdateTitle();
                } 
            }
            private string _subtitle = "";
            public string subtitle { 
                get => _subtitle.Trim();
                set 
                { 
                    _subtitle = value;
                    UpdateSubtitle();
                }
            }
            private string _description = "";
            public string description {
                get => _description.Trim(); 
                set 
                { 
                    _description = value;
                    UpdateDescription();
                } 
            }
            private string _text = "";
            public string text
            {
                get => _text.Trim();
                set
                {
                    _text = value;
                    UpdateText();
                }
            }
            private string _prefix = "";
            public string prefix 
            {
                get => _prefix.Trim();
                set
                {
                    _prefix = value;
                    UpdateText();
                }
            }
            private string _suffix = "";
            public string suffix
            {
                get => _suffix.Trim();
                set
                {
                    _suffix = value;
                    UpdateText();
                }
            }
            public string fullText 
            {
                get => string.Join(" ", prefix, text, suffix).Trim();
                [Obsolete("\nDon't assign to fullText. Use vars \"prefix\", \"text\" and \"suffix\" instead.\nYou probably want var \"text\".", error: true)]
                set { }
            }
            public bool closeOnPress { get; private set; }

            public bool selected = false;
            public bool pressed = false;
            public bool held = false;
            public int frameFirstPressedAt = Time.frameCount;
            public float timeFirstPressedAt = Time.time;
            public int frameLastPressedAt = Time.frameCount;
            public float timeLastPressedAt = Time.time;
            public float lastTapTime = Time.time;
            public sMenu parrentMenu;

            private readonly FlexibleEvent OnPressed = new();
            private readonly FlexibleEvent WhilePressed = new();
            private readonly FlexibleEvent OnUnpressed = new();
            private readonly FlexibleEvent OnUnpressedSelected = new();
            private readonly FlexibleEvent WhileUnpressed = new();
            private readonly FlexibleEvent OnSelected = new();
            private readonly FlexibleEvent WhileSelected = new();
            private readonly FlexibleEvent OnDeselected = new();
            private readonly FlexibleEvent WhileDeselected = new();
            private readonly FlexibleEvent OnDoubleTapped = new();
            private readonly FlexibleEvent OnTappedExclusive = new();
            private readonly FlexibleEvent OnTappedThenHeld = new(); //TODO?
            private readonly FlexibleEvent OnTapped = new();
            private readonly FlexibleEvent OnHeld = new();
            private readonly FlexibleEvent WhileHeld = new();
            private readonly FlexibleEvent OnHeldSelected = new();
            private readonly FlexibleEvent WhileHeldSelected = new();
            private readonly FlexibleEvent OnHeldImmediate = new();
            private readonly FlexibleEvent OnHeldImmediateSelected = new();

            private Dictionary<sMenuManager.nodeEvent, FlexibleEvent> eventMap;
            public TextPart fullTextPart;//todo make these private and add setters and getters for font stuff.
            public TextPart titlePart;
            public TextPart subtitlePart;
            public TextPart descriptionPart;
            public GameObject gameObject;
            private GameObject TextPartGameObject;
            private RectTransform rect;
            internal GameObject backgroundObject;
            internal RawImage backgroundImage;
            private SelectionColorHandler selectionColorHandler;
            private bool hasHoverText = false;
            private Color _color;
            public Color color
            {
                get => _color;
                set
                {
                    var oldValue = _color;
                    _color = value;
                    if (oldValue != value)
                        SetColor(_color);
                }
            }

            private Color _colorOffset;
            public Color ColorOffset
            {
                get => _colorOffset;
                set
                {
                    var oldValue = _colorOffset;
                    _colorOffset = value;
                    if (oldValue != value)
                        SetColor(_color);
                }
            }



            //settings
            private float holdThreshold = 0.2f;
            private float doubleTapThreshold = 0.3f;

            public sMenuNode(string arg_Name, sMenu arg_parrentMenu, FlexibleMethodDefinition arg_Callback)
            {

                eventMap = new Dictionary<sMenuManager.nodeEvent, FlexibleEvent>(){
                    { sMenuManager.nodeEvent.OnPressed, OnPressed },
                    { sMenuManager.nodeEvent.WhilePressed, WhilePressed },
                    { sMenuManager.nodeEvent.OnUnpressed, OnUnpressed },
                    { sMenuManager.nodeEvent.OnUnpressedSelected, OnUnpressedSelected },
                    { sMenuManager.nodeEvent.WhileUnpressed, WhileUnpressed },
                    { sMenuManager.nodeEvent.OnSelected, OnSelected },
                    { sMenuManager.nodeEvent.WhileSelected, WhileSelected },
                    { sMenuManager.nodeEvent.OnDeselected, OnDeselected },
                    { sMenuManager.nodeEvent.WhileDeselected, WhileDeselected },
                    { sMenuManager.nodeEvent.OnDoubleTapped, OnDoubleTapped },
                    { sMenuManager.nodeEvent.OnTapped, OnTapped },
                    { sMenuManager.nodeEvent.OnHeld, OnHeld },
                    { sMenuManager.nodeEvent.WhileHeld, WhileHeld },
                    { sMenuManager.nodeEvent.OnHeldSelected, OnHeldSelected },
                    { sMenuManager.nodeEvent.WhileHeldSelected, WhileHeldSelected },
                    { sMenuManager.nodeEvent.OnHeldImmediate, OnHeldImmediate },
                    { sMenuManager.nodeEvent.OnHeldImmediateSelected, OnHeldImmediateSelected },
                    { sMenuManager.nodeEvent.OnTappedExclusive, OnTappedExclusive },
                };

                gameObject = new($"sMenuNode {arg_Name}");
                gameObject.transform.SetParent(arg_parrentMenu.getCanvas().transform, false);
                backgroundObject = new GameObject("Background");
                TextPartGameObject = new GameObject($"TextParts");
                TextPartGameObject.transform.SetParent(gameObject.transform, false);
                backgroundObject.transform.SetParent(gameObject.transform, false);
                backgroundObject.transform.localPosition = new Vector3(0,0,30f); //Move background behind text
                backgroundImage = backgroundObject.AddComponent<RawImage>();
                backgroundImage.texture = sMenuManager.DefaultBackgroundImage;


                rect = gameObject.AddComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.localScale = Vector3.one;

                //rect.sizeDelta = new Vector2(300, 0);

                parrentMenu = arg_parrentMenu;
                if (arg_Callback != null) OnUnpressedSelected.Listen(arg_Callback);
                color = parrentMenu.getTextColor();

                VerticalLayoutGroup layout = TextPartGameObject.AddComponent<VerticalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.spacing = 2f;
                layout.childControlWidth = true;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;

                ContentSizeFitter fitter = TextPartGameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

                (titlePart = new TextPart(TextPartGameObject, $"{title}", new Color(0.2f, 0.2f, 0.2f, 1f)).SetScale(0.75f, 0.75f)).gameObject.name = "Title";
                (fullTextPart = new TextPart(TextPartGameObject, $"{text}", parrentMenu.textColor)).gameObject.name = "Text";
                (subtitlePart = new TextPart(TextPartGameObject, $"{title}", new Color(0.1f, 0.1f, 0.1f, 1f)).SetScale(0.75f, 0.75f)).gameObject.name = "Subtitle";
                (descriptionPart = new TextPart(TextPartGameObject, description, parrentMenu.textColor)).gameObject.name = "Description";

                text = arg_Name;

                selectionColorHandler = new SelectionColorHandler(this);
                AddListener(sMenuManager.nodeEvent.OnSelected, selectionColorHandler.onSelected);
                AddListener(sMenuManager.nodeEvent.OnDeselected, selectionColorHandler.OnDeselected);
                AddListener(sMenuManager.nodeEvent.OnPressed, selectionColorHandler.OnPressed);
                AddListener(sMenuManager.nodeEvent.OnUnpressed, selectionColorHandler.OnUnpressed);
                parrentMenu.AddListener(sMenuManager.menuEvent.OnClosed, selectionColorHandler.onClosed);
            }
            public sMenuNode Update()
            {
                if (selected)
                {
                    WhileSelected.Invoke();
                    parrentMenu.selectedNode = this;
                }
                else
                    WhileDeselected.Invoke();
                if (!pressed)
                    WhileUnpressed.Invoke();
                //if (pressed && Time.frameCount - frameLastPressedAt > 2)
                //    Unpress();
                //FaceCamera();
                return this;
            }
            public Vector3 GetRelativePosition()
            {
                // local agentPosition in menu space, accounting for canvas scale
                Vector3 localPos = new Vector3(rect.anchoredPosition.x, rect.anchoredPosition.y, 0f);
                Vector3 scaledLocalPos = Vector3.Scale(localPos, parrentMenu.getCanvas().transform.localScale);

                // world agentPosition
                Vector3 worldPos = parrentMenu.gameObject.transform.TransformPoint(scaledLocalPos);

                // relative to camera
                return sMenuManager.mainCamera.transform.position - worldPos;
            }
            public sMenuNode SetPosition(float x, float y)
            {
                SetPosition(new Vector2(x, y));
                return this;
            }
            public sMenuNode SetPosition(Vector2 pos)
            {
                rect.anchoredPosition = pos;
                return this;
            }
            public sMenuNode SetSize(float scale)
            {
                return SetSize(new Vector3(scale, scale, scale));
            }
            public sMenuNode SetSize(float x, float y)
            {
                SetSize(new Vector2(x, y));
                return this;
            }
            public sMenuNode SetSize(Vector2 scale)
            {
                SetSize(new Vector3(scale.x, scale.y, rect.localScale.z));
                return this;
            }
            public sMenuNode SetSize(float x, float y, float z)
            {
                SetSize(new Vector3(x,y,z));
                return this;
            }
            public sMenuNode SetSize(Vector3 scale) //main passthrough
            {
                rect.localScale = scale;
                return this;
            }
            public sMenuNode SetCloseMenuOnPress(bool close)
            {
                closeOnPress = close;
                return this;
            }
            public sMenuNode FaceCamera()
            {
                Quaternion rotation = Quaternion.LookRotation(gameObject.transform.position - sMenuManager.mainCamera.transform.position);
                setRotation(rotation);
                return this;
            }
            public sMenuNode setRotation(Quaternion rot)
            {
                gameObject.transform.rotation = rot;
                return this;
            }
            public sMenuNode Select()
            {

                if (selected) return this;
                selected = true;
                SetSize(sMenuManager.selectedNodeSizeMultiplier);
                OnSelected.Invoke();
                parrentMenu.OnSelected.Invoke();
                return this;
            }
            public sMenuNode Deselect()
            {
                if (!selected) return this;
                selected = false;
                SetSize(1f);
                OnDeselected.Invoke();
                parrentMenu.OnDeselected.Invoke();
                return this;
            }
            public sMenuNode Pressing()
            {
                frameLastPressedAt = Time.frameCount;
                timeLastPressedAt = Time.time;
                WhilePressed.Invoke();
                float heldTime = Time.time - timeFirstPressedAt;
                if (heldTime > holdThreshold)
                {
                    if (held)
                    {
                        if (selected)
                            WhileHeldSelected.Invoke();
                        else
                            WhileHeld.Invoke();
                    }
                    else
                    {
                        held = true;
                        if (selected)
                            OnHeldImmediateSelected.Invoke();
                        OnHeldImmediate.Invoke();
                    }
                }
                return this;
            }
            public sMenuNode Press()
            {
                frameFirstPressedAt = Time.frameCount;
                timeFirstPressedAt = Time.time;
                pressed = true;
                OnPressed.Invoke();
                if (closeOnPress)
                {
                    sMenuManager.CloseAllMenues();
                    return this;
                }
                return this;
            }
            public sMenuNode Unpress()
            {

                if (pressed)
                {
                    OnUnpressed.Invoke();
                    if (held)
                        OnHeld.Invoke();
                    if (selected)
                        OnUnpressedSelected.Invoke();
                    if (held && selected)
                        OnHeldSelected.Invoke();

                    // If released quickly enough = tap
                    if (Time.time - timeFirstPressedAt < holdThreshold)
                    {
                        OnTapped.Invoke();
                        if (Time.time - lastTapTime <= doubleTapThreshold)
                        {
                            OnDoubleTapped.Invoke();
                        }
                        else
                        {
                            zUpdater.InvokeStatic(new ZombieTweak2.FlexibleMethodDefinition(InvokeTappedExclusive), doubleTapThreshold - (Time.deltaTime / 2));
                        }
                        lastTapTime = Time.time;
                    }
                }
                held = false;
                pressed = false;
                return this;
            }
            private void InvokeTappedExclusive()
            {
                // If no second tap happened (lastTapTime is still within threshold window)
                if (Time.time - lastTapTime >= doubleTapThreshold - Time.deltaTime && !pressed)
                {
                    OnTappedExclusive.Invoke();
                }
            }
            //public sMenuNode AddListener<T>(sMenuManager.nodeEvent arg_event, Action<T> method, T arg)
            //{
            //    AddListener(arg_event, method, arg);
            //    return this;
            //}
            public sMenuNode AddListener(sMenuManager.nodeEvent arg_event, Action arg_method)
            {
                return AddListener(arg_event, (FlexibleMethodDefinition)arg_method);
            }
            public sMenuNode AddListener(sMenuManager.nodeEvent arg_event, Delegate method, params object[] args)
            {
                var flex = new FlexibleMethodDefinition(method, args);
                return AddListener(arg_event, flex);
            }
            public sMenuNode AddListener(sMenuManager.nodeEvent arg_event, FlexibleMethodDefinition arg_method)
            {
                if (eventMap.TryGetValue(arg_event, out var flexEvent))
                {
                    flexEvent.Listen(arg_method);
                }
                return this;
            }
            public sMenuNode RemoveListener(sMenuManager.nodeEvent arg_event, Action arg_method)
            {
                if (eventMap.TryGetValue(arg_event, out var flexEvent))
                {
                    flexEvent.Unlisten(arg_method);
                }
                return this;
            }
            public sMenuNode RemoveListener(sMenuManager.nodeEvent arg_event, FlexibleMethodDefinition arg_method)
            {
                if (eventMap.TryGetValue(arg_event, out var flexEvent))
                {
                    flexEvent.Unlisten(arg_method);
                }
                return this;
            }
            public sMenuNode ClearListeners(sMenuManager.nodeEvent arg_event)
            {
                if (eventMap.TryGetValue(arg_event, out var flexEvent))
                {
                    flexEvent.ClearListeners();
                }
                return this;
            }
            public sMenuNode SetTitle(string arg_Title)
            {
                title = arg_Title ?? string.Empty;
                return this;
            }
            public sMenuNode SetSubtitle(string arg_Subtitle)
            {
                subtitle = arg_Subtitle ?? string.Empty;
                return this;
            }
            public sMenuNode SetDescription(string arg_Description)
            {
                description = arg_Description ?? string.Empty;
                return this;
            }
            public sMenuNode SetText(string arg_Text)
            {
                text = arg_Text ?? string.Empty;
                return this;
            }
            public sMenuNode SetPrefix(string arg_Prefix)
            {
                prefix = arg_Prefix ?? string.Empty;
                return this;
            }
            public sMenuNode SetSuffix(string arg_Suffix)
            {
                suffix = arg_Suffix ?? string.Empty;
                return this;
            }
            public sMenuNode SetColor(Color arg_Color)
            {
                color = arg_Color;
                List<TextPart> textParts = GetTextParts();
                foreach (TextPart part in textParts) 
                {
                    Color final = arg_Color;
                    final.r += ColorOffset.r;
                    final.g += ColorOffset.g;
                    final.b += ColorOffset.b;
                    final.a = arg_Color.a;
                    part.SetColor(final);
                }
                return this;
            }
            public sMenuNode AddHoverText(sMenuPannel.Side side, string[] textList)
            {
                foreach (var text in textList)
                {
                    AddHoverText(side, text);
                }
                return this;
            }
            public sMenuNode AddHoverText(sMenuPannel.Side side, string text)
            {
                var pannel = parrentMenu.AddPannel(side);
                string key = gameObject.GetInstanceID().ToString();
                pannel.addLine(text, key);
                pannel.SetLineVisible(false, text, key);
                if (!hasHoverText)
                {
                    AddListener(sMenuManager.nodeEvent.OnSelected, () => pannel.SetKeyVisible(true, key));
                    AddListener(sMenuManager.nodeEvent.OnDeselected, () => pannel.SetKeyVisible(false, key));
                    hasHoverText = true;
                }
                return this;
            }
            public List<TextPart> GetTextParts()
            {
                return this.GetType()
                           .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                           .Where(f => f.FieldType == typeof(TextPart))
                           .Select(f => f.GetValue(this) as TextPart)
                           .Where(tp => tp != null)
                           .ToList();
            }
            private void UpdateTitle()
            {
                if (titlePart != null)
                    titlePart.SetText(title);
            }
            private void UpdateSubtitle()
            {
                if (subtitlePart != null)
                    subtitlePart.SetText(subtitle);
            }
            private void UpdateDescription()
            {
                if (descriptionPart != null)
                    descriptionPart.SetText(description);
            }
            private void UpdateText()
            {
                if (fullTextPart != null)
                    fullTextPart.SetText(fullText);
            }
            private class SelectionColorHandler
            {
                private sMenuNode node;
                private bool selected = false;
                private bool pressed = false;
                private static Color selectedOffset = new Color(0.1f, 0.1f, 0.1f);
                private static Color pressedOffset = new Color(0.3f, 0.2f, 0.5f);

                public SelectionColorHandler(sMenuNode Node)
                {
                    node = Node;
                }
                internal void onClosed()
                {
                    selected = false;
                    pressed = false;
                    UpdateOffset();
                }

                internal void onSelected()
                {
                    selected = true;
                    UpdateOffset();
                }

                internal void OnDeselected()
                {
                    selected = false;
                    UpdateOffset();
                }

                internal void OnPressed()
                {
                    pressed = true;
                    UpdateOffset();
                }

                internal void OnUnpressed()
                {
                    pressed = false;
                    UpdateOffset();
                }
                private void UpdateOffset()
                {
                    node.ColorOffset = new Color(0,0,0);
                    if (selected) node.ColorOffset += selectedOffset;
                    if (pressed) node.ColorOffset += pressedOffset;
                }
            }
        }
    }
}

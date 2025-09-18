using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Zombified_Initiative;

namespace ZombieTweak2.zMenu
{
    public partial class zMenu 
    {
        public partial class zMenuNode
        {
            //This is a menu node instance, there if you want to create one, call the parrent menu.addnode not this.

            private string _title;
            public string title { 
                get => _title; 
                set 
                {
                    _title = value;
                    UpdateTitle();
                } 
            }
            private string _subtitle;
            public string subtitle { 
                get => _subtitle;
                set 
                { 
                    _subtitle = value;
                    UpdateSubtitle();
                }
            }
            private string _description;
            public string description {
                get => _description; 
                set 
                { 
                    _description = value;
                    UpdateDescription();
                } 
            }
            private string _text;
            public string text
            {
                get => _text;
                set
                {
                    _text = value;
                    UpdateText();
                }
            }
            private string _prefix;
            public string prefix 
            {
                get => _prefix;
                set
                {
                    _prefix = value;
                    UpdateText();
                }
            }
            private string _suffix;
            public string suffix
            {
                get => _suffix;
                set
                {
                    _suffix = value;
                    UpdateText();
                }
            }
            public string fullText 
            {
                get => prefix + " " + text + " " + suffix;
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
            public zMenu parrentMenu;
            private FlexibleEvent OnPressed = new();
            private FlexibleEvent WhilePressed = new();
            private FlexibleEvent OnUnpressed = new();
            private FlexibleEvent OnUnpressedSelected = new();
            private FlexibleEvent WhileUnpressed = new();
            private FlexibleEvent OnSelected = new();
            private FlexibleEvent WhileSelected = new();
            private FlexibleEvent OnDeselected = new();
            private FlexibleEvent WhileDeselected = new();
            private FlexibleEvent OnDoublePressed = new(); //TODO
            private FlexibleEvent OnTapped = new();
            private FlexibleEvent OnHeld = new();
            private FlexibleEvent WhileHeld = new();
            private FlexibleEvent OnHeldSelected = new();
            private FlexibleEvent WhileHeldSelected = new();
            private FlexibleEvent OnHeldImmediate = new();
            private FlexibleEvent OnHeldImmediateSelected = new();

            private Dictionary<zMenuManager.nodeEvent, FlexibleEvent> eventMap;
            public TextPart fullTextPart;
            public TextPart titlePart;
            public TextPart subtitlePart;
            public TextPart descriptionPart;
            public RectTransform rect;
            public GameObject gameObject;
            public UnityEngine.Color color;

            //settings
            private float holdThreshold = 0.2f;

            public zMenuNode(string arg_Name, zMenu arg_parrentMenu, FlexibleMethodDefinition arg_Callback)
            {
                gameObject = new GameObject($"zMenuNode {arg_Name}");
                gameObject.transform.SetParent(arg_parrentMenu.getCanvas().transform, false);

                rect = gameObject.AddComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.localScale = Vector3.one;
                rect.sizeDelta = new Vector2(300, 0);

                parrentMenu = arg_parrentMenu;
                if (arg_Callback != null) OnUnpressedSelected.Listen(arg_Callback);
                color = parrentMenu.getTextColor();

                VerticalLayoutGroup layout = gameObject.AddComponent<VerticalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.spacing = 2f;
                layout.childControlWidth = true;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;

                ContentSizeFitter fitter = gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

                titlePart = new TextPart(this, $"{title}").SetScale(0.50f, 0.50f).SetColor(new Color(0.2f, 0.2f, 0.2f, 1f));
                fullTextPart = new TextPart(this, $"{text}");
                subtitlePart = new TextPart(this, $"{title}").SetScale(0.25f, 0.25f).SetColor(new Color(0.1f, 0.1f, 0.1f, 1f));
                descriptionPart = new TextPart(this, description);

                text = arg_Name;

                eventMap = new Dictionary<zMenuManager.nodeEvent, FlexibleEvent>(){
                    { zMenuManager.nodeEvent.OnPressed, OnPressed },
                    { zMenuManager.nodeEvent.WhilePressed, WhilePressed },
                    { zMenuManager.nodeEvent.OnUnpressed, OnUnpressed },
                    { zMenuManager.nodeEvent.OnUnpressedSelected, OnUnpressedSelected },
                    { zMenuManager.nodeEvent.WhileUnpressed, WhileUnpressed },
                    { zMenuManager.nodeEvent.OnSelected, OnSelected },
                    { zMenuManager.nodeEvent.WhileSelected, WhileSelected },
                    { zMenuManager.nodeEvent.OnDeselected, OnDeselected },
                    { zMenuManager.nodeEvent.WhileDeselected, WhileDeselected },
                    { zMenuManager.nodeEvent.OnTapped, OnTapped },
                    { zMenuManager.nodeEvent.OnHeld, OnHeld },
                    { zMenuManager.nodeEvent.WhileHeld, WhileHeld },
                    { zMenuManager.nodeEvent.OnHeldSelected, OnHeldSelected },
                    { zMenuManager.nodeEvent.WhileHeldSelected, WhileHeldSelected },
                    { zMenuManager.nodeEvent.OnHeldImmediate, OnHeldImmediate },
                    { zMenuManager.nodeEvent.OnHeldImmediateSelected, OnHeldImmediateSelected },
                };
            }
            public zMenuNode Update()
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
                FaceCamera();
                return this;
            }
            public zMenuNode SetPosition(float x, float y)
            {
                SetPosition(new Vector2(x, y));
                return this;
            }
            public zMenuNode SetPosition(Vector2 pos)
            {
                rect.anchoredPosition = pos;
                return this;
            }
            public zMenuNode SetSize(float scale)
            {
                return SetSize(new Vector3(scale, scale, scale));
            }
            public zMenuNode SetSize(float x, float y)
            {
                SetSize(new Vector2(x, y));
                return this;
            }
            public zMenuNode SetSize(Vector2 scale)
            {
                SetSize(new Vector3(scale.x, scale.y, rect.localScale.z));
                return this;
            }
            public zMenuNode SetSize(float x, float y, float z)
            {
                SetSize(new Vector3(x,y,z));
                return this;
            }
            public zMenuNode SetSize(Vector3 scale) //main passthrough
            {
                rect.localScale = scale;
                return this;
            }
            public zMenuNode SetCloseMenuOnPress(bool close)
            {
                closeOnPress = close;
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
            public zMenuNode Select()
            {
                selected = true;
                SetSize(1.5f);
                OnSelected.Invoke();
                parrentMenu.OnSelected.Invoke();
                return this;
            }
            public zMenuNode Deselect()
            {
                selected = false;
                SetSize(1f);
                OnDeselected.Invoke();
                parrentMenu.OnDeselected.Invoke();
                return this;
            }
            public zMenuNode Pressing()
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
                        ZiMain.log.LogInfo(fullText + " was held");
                        held = true;
                        if (selected)
                            OnHeldImmediateSelected.Invoke();
                        else
                            OnHeldImmediate.Invoke();
                    }
                }
                return this;
            }
            public zMenuNode Press()
            {
                frameFirstPressedAt = Time.frameCount;
                timeFirstPressedAt = Time.time;
                pressed = true;
                ZiMain.log.LogInfo(fullText + "was pressed");
                OnPressed.Invoke();
                if (closeOnPress)
                {
                    zMenuManager.CloseAllMenues();
                    return this;
                }
                return this;
            }
            public zMenuNode Unpress()
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
                    if (Time.time - timeFirstPressedAt < holdThreshold)
                    {
                        ZiMain.log.LogInfo(fullText + "was tapped");
                        OnTapped.Invoke();
                    }
                }
                held = false;
                pressed = false;
                return this;
            }
            public zMenuNode AddListener(zMenuManager.nodeEvent arg_event, Action arg_method)
            {
                return AddListener(arg_event, (FlexibleMethodDefinition)arg_method);
            }
            public zMenuNode AddListener(zMenuManager.nodeEvent arg_event, Delegate method, params object[] args)
            {
                var flex = new FlexibleMethodDefinition(method, args);
                return AddListener(arg_event, flex);
            }
            public zMenuNode AddListener(zMenuManager.nodeEvent arg_event, FlexibleMethodDefinition arg_method)
            {
                if (eventMap.TryGetValue(arg_event, out var flexEvent))
                {
                    flexEvent.Listen(arg_method);
                }
                return this;
            }
            public zMenuNode RemoveListener(zMenuManager.nodeEvent arg_event, Action arg_method)
            {
                if (eventMap.TryGetValue(arg_event, out var flexEvent))
                {
                    flexEvent.Unlisten(arg_method);
                }
                return this;
            }
            public zMenuNode RemoveListener(zMenuManager.nodeEvent arg_event, FlexibleMethodDefinition arg_method)
            {
                if (eventMap.TryGetValue(arg_event, out var flexEvent))
                {
                    flexEvent.Unlisten(arg_method);
                }
                return this;
            }
            public zMenuNode ClearListeners(zMenuManager.nodeEvent arg_event)
            {
                if (eventMap.TryGetValue(arg_event, out var flexEvent))
                {
                    flexEvent.ClearListeners();
                }
                return this;
            }
            public zMenuNode SetTitle(string arg_Title)
            {
                title = arg_Title ?? string.Empty;
                return this;
            }
            public zMenuNode SetSubtitle(string arg_Subtitle)
            {
                subtitle = arg_Subtitle ?? string.Empty;
                return this;
            }
            public zMenuNode SetDescription(string arg_Description)
            {
                description = arg_Description ?? string.Empty;
                return this;
            }
            public zMenuNode SetText(string arg_Text)
            {
                text = arg_Text ?? string.Empty;
                return this;
            }
            public zMenuNode SetPrefix(string arg_Prefix)
            {
                prefix = arg_Prefix ?? string.Empty;
                return this;
            }
            public zMenuNode SetSuffix(string arg_Suffix)
            {
                suffix = arg_Suffix ?? string.Empty;
                return this;
            }
            public zMenuNode SetColor(UnityEngine.Color arg_Color)
            {
                List<TextPart> textParts = GetTextParts();
                foreach (TextPart part in textParts) 
                { 
                    part.SetColor(arg_Color);
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
                titlePart.SetText(title);
            }
            private void UpdateSubtitle()
            {
                subtitlePart.SetText(subtitle);
            }
            private void UpdateDescription()
            {
                descriptionPart.SetText(description);
            }
            private void UpdateText()
            {
                fullTextPart.SetText(fullText);
            }
        }
    }
}

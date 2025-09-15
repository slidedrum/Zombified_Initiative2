using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieTweak2.zMenu
{
    public partial class zMenu {
        public partial class zMenuNode
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
                gameObject.transform.SetParent(parrentMenu.getCanvas().transform, false);

                rect = gameObject.AddComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.localScale = Vector3.one;
                rect.sizeDelta = new Vector2(300, 0);

                // Node color for child TextParts
                color = parrentMenu.getTextColor();


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
                subtitlePart = new TextPart(this, $"subtitle {title}").SetScale(0.75f, 0.75f);
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
        }
    }
}

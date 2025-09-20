using FluffyUnderware.Curvy.Generator;
using FluffyUnderware.DevTools.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieTweak2.zMenu
{
    public partial class zMenu
    {
        public partial class zMenuNode
        {
            public class TextPart
            {
                //This is my text handler.  It's not pretty, but it's mine.
                private string _text;
                public string text 
                { 
                    get => _text; 
                    set 
                    { 
                        _text = value;
                        textMesh.text = value;
                    } 
                }
                private Color _textColor;
                public Color textColor
                {
                    get => _textColor;
                    set
                    {
                        var oldValue = _textColor;
                        _textColor = value;
                        if (oldValue != value)
                            I_SetColor(_textColor);
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
                            I_SetColor(_textColor); // combine base + offset
                    }
                }
                public GameObject gameObject { get; private set; }
                private TextMeshPro textMesh;

                public RectTransform rect { get; private set; }
                public TextPart(zMenuNode parent, string arg_Text)
                {
                    // Steal TextMeshPro from a reference object
                    GameObject reference = GameObject.Find("GUI/CellUI_Camera(Clone)/NavMarkerLayer/NavMarkerGeneric(Clone)/IconHolder/Title");
                    gameObject = Object.Instantiate(reference);
                    gameObject.GetComponent<NavMarkerComponent>().Destroy();
                    gameObject.transform.position = Vector3.zero;
                    gameObject.transform.rotation = Quaternion.identity;
                    gameObject.transform.localScale = Vector3.one;
                    gameObject.layer = 0;
                    gameObject.tag = "Untagged";
                    gameObject.SetActive(true);

                    gameObject.name = $"TextPart {arg_Text}";
                    rect = gameObject.GetComponent<RectTransform>();
                    rect.SetParent(parent.rect.transform, false);

                    textMesh = gameObject.GetComponent<TextMeshPro>();
                    textMesh.enableAutoSizing = false;
                    textMesh.fontSize = 3;
                    textMesh.alignment = TextAlignmentOptions.Center;
                    textMesh.color = parent.color;
                    textMesh.text = arg_Text;

                    rect.anchoredPosition = Vector2.zero; // center inside node
                    rect.localScale = Vector3.one;

                    rect.sizeDelta = new Vector2(300, 0);

                    ContentSizeFitter fitter = gameObject.AddComponent<ContentSizeFitter>();
                    fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                }

                //TODO
                //set font
                //set alignment
                //set font size
                public TextPart SetText(string newText)
                {
                    text = newText;
                    return this;
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
                    SetScale(scale.x, scale.y, 1);
                    return this;
                }
                public TextPart SetScale(Vector3 scale)
                {
                    SetScale(scale.x, scale.y, scale.z);
                    return this;
                }
                public TextPart SetScale(float x, float y)
                {
                    SetScale(x, y, 1);
                    return this;
                }
                public TextPart SetScale(float x, float y, float z)
                {
                    rect.localScale = new Vector3(x, y, z);
                    return this;
                }
                private TextPart I_SetColor(Color color)
                {
                    textMesh.color = textColor + ColorOffset;
                    return this;
                }
                public TextPart SetColor(Color color)
                {
                    textColor = color;
                    textMesh.color = textColor + ColorOffset;
                    return this;
                }
                public Color GetColor()
                {
                    return textColor;
                }
            }
        }
    }
}

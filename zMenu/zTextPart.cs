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
                private TextMeshProUGUI textMesh;

                public RectTransform rect { get; private set; }
                public TextPart(zMenuNode parent, string arg_Text)
                {
                    gameObject = new GameObject($"TextPart {arg_Text}");
                    rect = gameObject.AddComponent<RectTransform>();
                    rect.SetParent(parent.rect.transform, false);

                    textMesh = gameObject.AddComponent<TextMeshProUGUI>();
                    textMesh.text = arg_Text;
                    textMesh.fontSize = 24;
                    textMesh.alignment = TextAlignmentOptions.Center;
                    textMesh.color = parent.color;

                    rect.anchoredPosition = Vector2.zero; // center inside node
                    rect.localScale = Vector3.one;

                    rect.sizeDelta = new Vector2(300, 0);

                    ContentSizeFitter fitter = gameObject.AddComponent<ContentSizeFitter>();
                    fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

                    TMP_FontAsset font = GameObject.Find("GUI/CellUI_Camera(Clone)/WatermarkLayer/MovementRoot/PUI_Watermark(Clone)/Text").GetComponent<TextMeshPro>().font;
                    textMesh.font = font;
                    text = arg_Text;
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

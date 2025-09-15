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
    public partial class zMenu
    {
        public partial class zMenuNode
        {
            public class TextPart
            {
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
                public GameObject gameObject { get; private set; }
                private TextMeshProUGUI textMesh;
                public RectTransform rect { get; private set; }

                public TextPart(zMenuNode parent, string arg_Text)
                {
                    // Child object under the node
                    gameObject = new GameObject($"TextPart {arg_Text}");
                    rect = gameObject.AddComponent<RectTransform>();
                    rect.SetParent(parent.rect.transform, false);

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
                    text = arg_Text;
                }

                //set color
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
            }
        }
    }
}

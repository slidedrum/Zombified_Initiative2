using Iced.Intel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
namespace ZombieTweak2.zMenu
{
    public partial class zMenu
    {
        public partial class zMenuPannel
        {
            public enum Side
            {
                left,
                right,
                top,
                bottom,
            }
            public Side side;
            public zMenu parrentMenu;
            public GameObject gameObject;
            public RectTransform rect;
            public Color color;
            public List<TextPart> lines;

            public zMenuPannel(Side side, zMenu parrentMenu)
            {
                this.side = side;

                gameObject = new GameObject($"zMenuPannel {side}");
                gameObject.transform.SetParent(parrentMenu.getCanvas().transform, false);

                rect = gameObject.AddComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero + Vector2.right * parrentMenu.radius;
                rect.localScale = Vector3.one;
                rect.sizeDelta = new Vector2(300, 0);

                this.parrentMenu = parrentMenu;
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
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            public void addLine(string lineText) 
            {
                TextPart newLine = new TextPart(gameObject, lineText, parrentMenu.textColor);
                lines.Add(newLine);
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SlideMenu
{
    public partial class sMenu
    {
        public partial class sMenuPannel
        {
            public enum Side
            {
                left,
                right,
                top,
                bottom,
            }
            public Side side;
            public sMenu parrentMenu;
            public GameObject gameObject;
            public RectTransform rect;
            public Color color;
            public Dictionary<string, List<TextPart>> lines = new();

            public sMenuPannel(Side side, sMenu parrentMenu)
            {
                this.side = side;

                gameObject = new GameObject($"zMenuPannel {side}");
                gameObject.transform.SetParent(parrentMenu.getCanvas().transform, false);
                this.parrentMenu = parrentMenu;
                color = parrentMenu.getTextColor();

                rect = gameObject.GetComponent<RectTransform>();
                if (rect == null)
                    rect = gameObject.AddComponent<RectTransform>();
                //rect.anchorMax = new Vector2(0.5f, 0.5f);
                //rect.anchoredPosition = Vector2.zero + Vector2.right * parrentMenu.radius;
                rect.localScale = Vector3.one;
                rect.sizeDelta = new Vector2(300, 0);

                VerticalLayoutGroup layout = gameObject.AddComponent<VerticalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.spacing = 2f;
                layout.childControlWidth = (side == Side.top || side == Side.bottom);
                layout.childControlHeight = false;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;

                ContentSizeFitter fitter = gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                UpdatePosition();
            }
            public sMenuPannel UpdatePosition(float margin = 0f)
            {
                Rect bounds = parrentMenu.GetNodeBounds();

                Vector2 pos = Vector2.zero;

                switch (side)
                {
                    case Side.right:
                        rect.anchorMax = new Vector2(0f, 0.5f);
                        rect.anchorMin = new Vector2(0f, 0.5f);
                        rect.pivot = new Vector2(0f, 0.5f);

                        pos = new Vector2(bounds.xMax + margin, 0);
                        break;
                    case Side.left:
                        rect.anchorMax = new Vector2(1f, 0.5f);
                        rect.anchorMin = new Vector2(1f, 0.5f);
                        rect.pivot = new Vector2(1f, 0.5f);
                        pos = new Vector2(bounds.xMin - margin, 0);
                        break;
                    case Side.top:
                        rect.anchorMax = new Vector2(0.5f, 0f);
                        rect.anchorMin = new Vector2(0.5f, 0f);
                        rect.pivot = new Vector2(0.5f, 0f);
                        pos = new Vector2(0, bounds.yMax + margin);
                        break;
                    case Side.bottom:
                        rect.anchorMax = new Vector2(0.5f, 1f);
                        rect.anchorMin = new Vector2(0.5f, 1f);
                        rect.pivot = new Vector2(0.5f, 1f);
                        pos = new Vector2(0, bounds.yMin - margin);
                        break;
                }
                gameObject.transform.localPosition = pos;
                FaceCamera();
                return this;
            }
            public TextPart addLine(string lineText, string key = "Default")
            {
                TextPart newLine = new TextPart(gameObject, lineText, parrentMenu.textColor);
                newLine.gameObject.transform.SetParent(gameObject.transform, false);
                if (!lines.ContainsKey(key))
                    lines[key] = new();
                lines[key].Add(newLine);
                return newLine;
            }
            public sMenuPannel FaceCamera()
            {
                Quaternion rotation = Quaternion.LookRotation(gameObject.transform.position - sMenuManager.mainCamera.transform.position);
                setRotation(rotation);
                return this;
            }
            public sMenuPannel setRotation(Quaternion rot)
            {
                gameObject.transform.rotation = rot;
                return this;
            }
            public TextPart GetLine(string lineText, string key)
            {
                TextPart textPart = null;
                foreach (var candidateLine in lines[key])
                {
                    if (candidateLine.text.Contains(lineText))
                    {
                        textPart = candidateLine;
                        break;
                    }
                }
                return textPart;
            }
            public sMenuPannel SetLineVisible(bool visible, string line, string key = "")
            {
                TextPart textPart = null;
                if (key == "")
                {
                    foreach (var candidateKey in lines.Keys)
                    {
                        textPart = GetLine(line, candidateKey);
                        if (textPart != null)
                            break;
                    }
                }
                else if (lines.ContainsKey(key))
                {
                    textPart = GetLine(line, key);
                }
                textPart.gameObject.SetActive(visible);
                return this;
            }
            public sMenuPannel SetKeyVisible(bool visible, string key)
            {
                if (!lines.ContainsKey(key))
                    return this;
                foreach (var line in lines[key])
                {
                    line.gameObject.SetActive(visible);
                }
                return this;
            }
            public bool IsLineVisible(string line, string key = "")
            {
                TextPart textPart = GetLine(line, key);
                if (textPart != null)
                    return textPart.gameObject.activeInHierarchy;
                return false;
            }
            public bool IsKeyVisible(string key)
            {
                if (!lines.ContainsKey(key))
                    return false;
                int visibleLines = 0;
                foreach(var line in lines[key])
                {
                    if (line.gameObject.activeInHierarchy)
                        visibleLines++;
                    else
                        visibleLines--;
                }
                return visibleLines > 0;
            }
            public sMenuPannel ToggleLineVisiblity(string line, string key = "")
            {
                if (!lines.ContainsKey(key))
                    return this;
                bool currentVisibility = IsLineVisible(line, key);
                SetLineVisible(!currentVisibility, line, key);
                return this;
            }
            public sMenuPannel ToggleKeyVisiblity(string key)
            {
                if (!lines.ContainsKey(key)) 
                    return this;
                bool currentVisibility = IsKeyVisible(key);
                SetKeyVisible(!currentVisibility, key);
                return this;
            }
        }
    }
}

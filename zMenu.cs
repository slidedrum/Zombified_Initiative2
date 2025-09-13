using GuiMenu;
using Il2CppSystem.Data;
using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Zombified_Initiative;

namespace ZombieTweak2
{


    public static class ZMenuManger 
    {

        public static ZMenu mainMenu;
        public static List<ZMenu> menuList = new();
        public static ZMenu currentMenu = null;

        public static class StaticSettings
        {
            //public static Material standardMat = new Material(Shader.Find("Standard"));
            //public static Material selectedMat = new Material(Shader.Find("Unlit/Color"));
            public static int closeAngle = 30;
            public static float selectAngle = 3f;
            public static float menuDistance = 2f;
            public static float verticalOffset = 0.2f;
            public static float roationalOffset = 5f;
        }
        public static void Init()
        {
            mainMenu = new ZMenu("Main", null);
            menuList = new List<ZMenu>() { mainMenu };
        }
        public static void Update()
        {
            bool ready = FocusStateManager.CurrentState == eFocusState.FPS || FocusStateManager.CurrentState == eFocusState.Dead;
            if (ready)
            {
                if (mainMenu == null)
                {
                    Init();
                }
                if (Input.GetKeyDown(KeyCode.M))
                {
                    if (currentMenu == null)
                    {
                        mainMenu.Show();
                    }
                    else if (currentMenu.selectedNode == null && zSearch.GetClosestInLookDirection(Camera.main.transform, currentMenu.nodes.Select(node => node.gameObject).ToList(), StaticSettings.selectAngle * 2) == null)
                    {
                        closeAllMenues();
                    }
                    else if (currentMenu.selectedNode != null)
                    {
                        currentMenu.selectedNode.Press();
                    }
                }
                if (currentMenu != null)
                {
                    Vector3 menuDirection = (currentMenu.centerNode.transform.position - Camera.main.transform.position).normalized;
                    float menuAngle = Vector3.Angle(Camera.main.transform.forward, menuDirection);
                    if (menuAngle > StaticSettings.closeAngle)
                    {
                        closeAllMenues();
                        return;
                    }
                    currentMenu.Update();
                }
                //foreach (var menu in ZMenuManger.menuList)
                //{
                //    if (menu != null)
                //    {
                //        menu.Update();
                //    }
                //}
            }
        }
        public static ZMenu addMenu(string name, ZMenu parrentMenu)
        {
            ZMenu newMenu = new ZMenu(name, parrentMenu);
            addMenu(newMenu);
            return newMenu;
        }
        public static void addMenu(ZMenu newMenu)
        {
            menuList.Add(newMenu);
        }
        public static void closeAllMenues(ZMenuNode none = null)
        {
            currentMenu.Hide();
            ZMenuManger.currentMenu = null;
            foreach (ZMenu menu in menuList) 
            {
                menu.positionSet = false;
            }
        }
        public static void SetCurrentMenu(ZMenu menu)
        {
            currentMenu = menu;
        }
    }
    public class ZMenu
    {
        public ZMenuNode centerNode;
        public GameObject menuGO;
        public bool visible = false;
        public List<ZMenuNode> nodes = new();
        public float radius = 0.4f;
        public bool created = false;
        public ZMenuNode selectedNode;
        public string menuName = string.Empty;
        public bool positionSet = false;
        private bool warned = false;
        public ZMenu(string name, ZMenu parrentMenu)
        {
            menuGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            UnityEngine.Object.Destroy(menuGO.GetComponent<Collider>());
            menuName = name;
            centerNode = menuGO.AddComponent<ZMenuNode>();
            Action<ZMenuNode> callback;
            if (parrentMenu != null) 
                callback = parrentMenu.Show;
            else
                callback = ZMenuManger.closeAllMenues;
            string centerNodeText = parrentMenu?.menuName ?? "Close";
            centerNode.Init(this, centerNodeText, callback);
            nodes.Add(centerNode);
        }

        internal void Update()
        {
            if (!ZMenuManger.menuList.Contains(this) && !warned)
            {
                Zi.log.LogWarning($"Menu {menuName} has not been registered!");
                warned = true;
            }
            if (!visible) return;

            centerNode.gameObject.transform.position =
                Camera.main.transform.position + centerNode.gameObject.transform.forward * ZMenuManger.StaticSettings.menuDistance;

            int count = nodes.Count;
            if (count == 0) return;

            selectedNode = zSearch.GetClosestInLookDirection(
                Camera.main.transform, 
                nodes.Select(node => node.gameObject).ToList(), 
                ZMenuManger.StaticSettings.selectAngle,
                new Vector3(0f,ZMenuManger.StaticSettings.verticalOffset, 0f))
                    ?.GetComponent<ZMenuNode>();
            if (selectedNode != null)
                selectedNode.GetComponent<ZMenuNode>().SetSelected(true);

            Transform centerTransform = centerNode.gameObject.transform;

            for (int i = 0; i < count; i++)
            {
                var node  = nodes[i];
                if (selectedNode == null || node != selectedNode)
                {
                    node.SetSelected(false);
                }
                if (node != centerNode)
                {
                    float angle = i * Mathf.PI * 2f / (count - 1);//not counting center node.
                    Vector3 localOffset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
                    Vector3 worldOffset = centerTransform.rotation * localOffset;
                    node.gameObject.transform.position = centerTransform.position + worldOffset;
                }
            }
        }
        public void AddNode(string name, Action<ZMenuNode> callback)
        {
            GameObject nodeGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            UnityEngine.Object.Destroy(nodeGO.GetComponent<Collider>());

            ZMenuNode nodeComponent = nodeGO.AddComponent<ZMenuNode>();
            nodeComponent.Init(this, name, callback);

            nodes.Add(nodeComponent);
        }
        public void Show(ZMenuNode menuNode = null)
        {
            ZMenuManger.currentMenu?.Hide();
            centerNode.Show();
            if (!positionSet)
            {
                centerNode.transform.SetPositionAndRotation(Camera.main.transform.position, Camera.main.transform.rotation * Quaternion.AngleAxis(ZMenuManger.StaticSettings.roationalOffset, Vector3.right));
                centerNode.transform.localPosition += centerNode.transform.forward * 2;
                positionSet = true;
            }
            foreach (var node in nodes) 
            {
                node.Show();
            }
            visible = true;
            ZMenuManger.SetCurrentMenu(this);
        }
        public void Hide()
        {
            centerNode.Hide();
            foreach (var node in nodes)
            {
                node.Hide();
            }
            visible = false;
        }
    }
    public class ZMenuNode : MonoBehaviour
    {
        public ZMenu menuParrent;
        public string text;
        public NavMarker marker;
        private readonly float bigSize = 2;
        private readonly float smallSize = 1;
        public Color color = Color.grey;
        public bool selected = false;
        public Action<ZMenuNode> callback;

        public void Init(ZMenu parentMenu, string Name, Action<ZMenuNode> Callback)
        {
            menuParrent = parentMenu;
            text = Name;
            callback = Callback;
            // Destroy collider
            var col = gameObject.GetComponent<Collider>();
            if (col != null) Destroy(col);
            gameObject.GetComponent<Renderer>().enabled = false;
            marker = GuiManager.NavMarkerLayer.PlaceCustomMarker(NavMarkerOption.Sign,gameObject,text);
            marker.SetSignInfo(text);
            marker.SetPinEnabled(false);
            gameObject.name = "MenuNode";
            transform.localScale = Vector3.one * 0.1f;
        }
        public void Show()
        {
            gameObject.SetActive(true);
            if (marker == null)
            {
                marker = GuiManager.NavMarkerLayer.PlaceCustomMarker(NavMarkerOption.Sign, gameObject, text);
                marker.SetSignInfo(text);
                marker.SetPinEnabled(false);
            }
            marker.SetVisible(true);
            var sign = marker.m_sign;
            marker.Scale(sign, new Vector3(0, 0, 0), new Vector3(smallSize, smallSize, smallSize), color, color, 0.5f);
            //gameObject.transform.SetPositionAndRotation(transform.position, transform.rotation);
            //gameObject.transform.localPosition += gameObject.transform.forward * 2;
        }
        void HideMarker()
        {
            if (marker != null) return;
            if (!marker.IsVisible) return;
            marker.SetVisible(false);
        }
        void ShowMarker()
        {
            if (marker != null) return;
            if (marker.IsVisible) return;
            marker.SetVisible(true);
        }
        public void Hide()
        {
            if (gameObject != null)
            {
                gameObject.SetActive(false);
                var sign = marker.m_sign;
                marker.Scale(sign, sign.transform.localScale, new Vector3(0, 0, 0), color, color, 0.5f);
                Invoke(nameof(HideMarker), 0.5f);
            }
        }
        public void SetBig(bool Big)
        {
            Zi.log.LogInfo($"Setting {text} big {Big}");
            var sign = marker.m_sign;
            if (Big)
            {
                marker.Scale(sign, sign.transform.localScale, new Vector3(bigSize, bigSize, bigSize), color, color, 0.5f);
            }
            else
            {
                marker.Scale(sign, sign.transform.localScale, new Vector3(smallSize, smallSize, smallSize), color, color, 0.5f);
            }
        }
        internal void SetSelected(bool newSelected)
        {
            if (newSelected != selected)
            {
                Zi.log.LogInfo($"Selected {text} {newSelected}");
                selected = newSelected;
                SetBig(newSelected);
            }
        }

        internal void Press()
        {
            callback?.Invoke(this);
        }
    }
}

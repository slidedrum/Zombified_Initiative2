using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SlideMenu
{
    public static class sMenuManager
    {
        //This is the big custom menu manager.  Handles all menu creation and shit.  More work to do, but pretty good so far.
        public static HashSet<sMenu> menues { get; private set; } = new();
        public static GameObject menuParrent;
        private static bool playerInControll = false;
        private static Texture2D _defaultBackgroundImage;
        public static Texture2D DefaultBackgroundImage { 
            get 
            {
                if (_defaultBackgroundImage == null)
                    _defaultBackgroundImage = CreateBackgroundImage();
                return _defaultBackgroundImage;
            } 
            set
            {
                _defaultBackgroundImage = value;
            }
        }
        public static sMenu mainMenu { get; private set; }
        public static sMenu currentMenu { get; internal set; }
        public static sMenu previousMenu { get; internal set; }
        private static sMenu.sMenuNode selectedNode;
        public static Camera mainCamera;
        public static Color defaultColor { get; private set; } = new Color(0.2f, 0.2f, 0.2f, 1f);
        public static float defaultFontSize { get; private set; } = 100f;
        public static float defaultRadius { get; private set; } = 100f;

        public enum nodeEvent
        {
            // To add an event it must be added here, a flexible event must be created, it must be added in the eventMap, and given a place where it is invoked.
            OnPressed,
            WhilePressed,
            OnUnpressed,
            WhileUnpressed,
            OnSelected,
            WhileSelected,
            OnDeselected,
            WhileDeselected,
            OnDoubleTapped,
            OnTapped,
            OnHeld,
            WhileHeld,
            OnHeldImmediate,
            OnUnpressedSelected,
            OnHeldSelected,
            WhileHeldSelected,
            OnHeldImmediateSelected,
            OnTappedExclusive,
        }
        public enum menuEvent
        {
            OnOpened,
            WhileOpened,
            OnClosed,
            WhileClosed,
            OnSelected,
            WhileSelected,
            OnDeselected,
            WhileDeselected,
            OnCatagoryChanged,
        }
        //static settings
        internal static bool pressable;
        internal static sMenu.sMenuNode pressedNode;
        internal static float selectedNodeSizeMultiplier = 1.5f;
        internal static float menuSizeScaler = 1.0f;
        private static float angleTollerance = 45f;
        private static float nodeAngleTollerance = 10f;
        internal static float pannelBuffer = 1f;
        

        private static bool menuOpen { get { return currentMenu != null; } }
        //static sMenuManager()
        //{
        //    if (_defaultBackgroundImage == null)
        //        _defaultBackgroundImage = CreateBackgroundImage();
        //}
        public static sMenu createMenu(string name, sMenu parrentMenu = null, bool autoAddNode = true)
        {
            sMenu newMenu = new sMenu(name, parrentMenu);
            if (parrentMenu == null )
            {
                newMenu.centerNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
                newMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnUnpressedSelected, CloseAllMenues);
            }
            else if (autoAddNode)
            {
                    parrentMenu.AddNode(newMenu);
            }
            newMenu.centerNode.AddListener(nodeEvent.WhileSelected, newMenu.UpdateCatagoryByScroll);
            registerMenu(newMenu);
            return newMenu;
        }
        public static sMenu registerMenu(sMenu menu)
        {
            menues.Add(menu);
            return menu;
        }
        public static void Update()
        {
            playerInControll = FocusStateManager.CurrentState == eFocusState.FPS || FocusStateManager.CurrentState == eFocusState.Dead || FocusStateManager.CurrentState == eFocusState.FPS_CommunicationDialog;
            //playerInControll = true;
            if (playerInControll)
            {
                if (mainCamera == null)
                {
                    ClearAllMenus();
                    sMenus.CreateMenus();
                }
                if (menuOpen)
                {
                    currentMenu.Update();

                }
                if (Input.GetKey(KeyCode.M))
                {
                    if (pressable) //is this the first frame of holding the button?
                    {
                        if (!menuOpen) //is the menu closed?
                        {
                            mainMenu.Open();
                        } //open main menu
                        else if (selectedNode != null) //Are we hovering over a button?
                        {
                            pressedNode = selectedNode; //save the node we have selected
                            pressedNode.Press(); //let the button know it's been pressed
                        }
                    }
                    else //this is not the first frame of holding the button.
                    {    //We are holding the button.
                        if (pressedNode != null) //did we start pressing a node on the first frame?
                        {
                            pressedNode.Pressing(); //let the node know.
                        }
                    }
                    pressable = false;
                }
                else //we are not holding the button
                {
                    pressable = true; //we can have a first frame again
                    if (pressedNode != null)
                        pressedNode.Unpress();
                    pressedNode = null; //stop holding onto the node.
                }
            }
        }
        public static void CloseAllMenues()
        {
            pressedNode = null;
            foreach (sMenu menu in menues)
            {
                menu.ResetRelativePosition();
                menu.Close();
            }
        }
        public static void LateUpdate()
        {
            if (playerInControll)
            {
                if (menuOpen)
                {
                    currentMenu.Lateupdate();
                    Dictionary<GameObject, sMenu.sMenuNode> nodeDict = new(currentMenu.nodes.Count + 1);
                    foreach (var node in currentMenu.allNodes.Where(n => n.gameObject.activeInHierarchy).ToList())
                    {
                        nodeDict[node.gameObject] = node;
                    }
                    List<GameObject> nodeList = nodeDict.Keys.ToList();
                    GameObject selectedNodeObject = GetClosestObjectInLookDirection(mainCamera.transform, nodeList, nodeAngleTollerance * menuSizeScaler);
                    selectedNode = null;
                    if (selectedNodeObject != null)
                    {
                        nodeDict.TryGetValue(selectedNodeObject, out selectedNode);
                    }
                    foreach (sMenu.sMenuNode node in nodeDict.Values)
                    {
                        if (node == selectedNode && !node.selected)
                        {
                            node.Select();
                        }
                        if (node != selectedNode && node.selected)
                        {
                            node.Deselect();
                        }
                    }

                    if (selectedNodeObject == null)
                    {
                        Vector3 angleToTarget = (currentMenu.gameObject.transform.position - mainCamera.transform.position).normalized;
                        Vector3 cameraAngle = mainCamera.transform.forward;
                        float angleDelta = Vector3.Angle(cameraAngle, angleToTarget);
                        if (angleDelta > angleTollerance * menuSizeScaler) //close if we're looking too far away. 
                            CloseAllMenues();
                        else if (GetClosestObjectInLookDirection(mainCamera.transform, nodeList, nodeAngleTollerance * menuSizeScaler * 2) == null) //close if we're not looking near a node with a wider tolerance.
                            CloseAllMenues();
                    }
                }
                
            }
        }
        public static void PreRender()
        {
            if (playerInControll && menuOpen)
            {
                currentMenu.PreRender();

            }
        }
        public static void SetupCamera() {
            mainCamera = Camera.main;
            menuParrent = new GameObject("menus");
            mainMenu = new sMenu("Main");
            mainMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnUnpressedSelected, CloseAllMenues);
            registerMenu(mainMenu);
        }
        public static void ClearAllMenus()
        {
            menues.Clear();
            mainCamera = null;
            GameObject.Destroy(menuParrent);
            SetupCamera();
        }
        public static sMenu GetMenu(string menuName)
        {
            foreach (var menu in menues)
            {
                if (menu.centerNode.text.Contains(menuName))
                    return menu;
            }
            return null;
        }
        public static void SetMenusScale(float scale)
        {
            foreach(var menu in menues)
            {
                menu.Setsize(scale);
            }
        }
        public static GameObject GetClosestObjectInLookDirection(Transform baseTransform, List<GameObject> candidates, float maxAngle = 180f, Vector3? candidateOffset = null, Vector3? baseOffset = null)
        {
            //TODO add some optional leeway for very close objects
            candidateOffset = candidateOffset ?? Vector3.zero;
            baseOffset = baseOffset ?? Vector3.zero;
            if (baseTransform == null || candidates == null || candidates.Count == 0)
                return null;

            Vector3 basePosition = (Vector3)(baseTransform.position + baseOffset);
            Vector3 lookDirection = baseTransform.forward;

            GameObject bestCanidate = null;
            float bestAngle = maxAngle;

            foreach (GameObject candidate in candidates)
            {
                if (candidate == null) continue;
                Vector3 candidatePosition = (Vector3)(candidate.transform.position + candidateOffset);
                Vector3 targetDirection = (candidatePosition - basePosition).normalized;
                float canidateAngle = Vector3.Angle(lookDirection, targetDirection);

                if (canidateAngle < bestAngle)
                {
                    bestAngle = canidateAngle;
                    bestCanidate = candidate;
                }
            }
            // enforce canidateAngle cutoff
            if (bestCanidate != null && bestAngle <= maxAngle)
                return bestCanidate;

            return null;
        }
        private static Texture2D CreateBackgroundImage(int size = 512, Color? color = null)
        {
            if (color == null)
                color = Color.black;

            // Interior alpha values
            float alphaCenter = 0.9f;  // alpha at the very center
            float alphaEdge = 0.0f;    // alpha at the edge of inner circle

            // Border color (75% alpha)
            Color borderColor = new(color.Value.r, color.Value.g, color.Value.b, 0.75f);

            Texture2D backgroundImage = new(size, size, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[size * size];

            float radius = size / 2f;
            float rSquared = radius * radius;
            float borderWidth = 5f;
            float innerRadius = radius - borderWidth;
            float innerRSquared = innerRadius * innerRadius;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - radius + 0.5f;
                    float dy = y - radius + 0.5f;
                    float distSquared = dx * dx + dy * dy;

                    if (distSquared <= innerRSquared)
                    {
                        // Compute distance from center
                        float dist = Mathf.Sqrt(distSquared);
                        // Lerp alpha from center to edge
                        float alpha = Mathf.Lerp(alphaCenter, alphaEdge, dist / innerRadius);
                        Color innerColor = new Color(color.Value.r, color.Value.g, color.Value.b, alpha);
                        pixels[y * size + x] = (Color32)innerColor;
                    }
                    //else if (distSquared <= rSquared)
                    //{
                    //    pixels[y * size + x] = (Color32)borderColor; // border
                    //}
                    else
                    {
                        pixels[y * size + x] = new Color(0, 0, 0, 0); // transparent outside
                    }
                }
            }

            backgroundImage.SetPixels32(pixels);
            backgroundImage.Apply();
            return backgroundImage;
        }
    }
}

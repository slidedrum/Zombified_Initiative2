using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Zombified_Initiative;

namespace ZombieTweak2.zMenu
{
    public static class zMenuManager
    {
        //This is the big custom menu manager.  Handles all menu creation and shit.  More work to do, but pretty good so far.
        public static HashSet<zMenu> menues { get; private set; } = new();
        public static GameObject menuParrent;
        private static bool playerInControll = false;
        public static zMenu mainMenu { get; private set; }
        public static zMenu currentMenu { get; internal set; }
        private static zMenu.zMenuNode selectedNode;
        public static Color defaultColor { get; private set; } = new Color(0.25f, 0.25f, 0.25f, 1f);
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
            OnDoubleTapped, //TODO
            OnTapped,
            OnHeld,
            WhileHeld,
            OnHeldImmediate,
            OnUnpressedSelected,
            OnHeldSelected,
            WhileHeldSelected,
            OnHeldImmediateSelected,
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
        }
        //static settings
        public static string menuParrentPath = "GUI/CellUI_Camera(Clone)/NavMarkerLayer";
        internal static bool pressable;
        internal static zMenu.zMenuNode pressedNode;

        public static zMenu createMenu(string name, zMenu parrentMenu = null)
        {
            zMenu newMenu = new zMenu(name, parrentMenu);
            if (parrentMenu == null)
            {
                newMenu.centerNode.ClearListeners(zMenuManager.nodeEvent.OnUnpressedSelected);
                newMenu.centerNode.AddListener(zMenuManager.nodeEvent.OnUnpressedSelected, CloseAllMenues);
            }
            else
            {
                parrentMenu.AddNode(newMenu);
            }
            registerMenu(newMenu);
            return newMenu;
        }
        public static zMenu registerMenu(zMenu menu)
        {
            menues.Add(menu);
            return menu;
        }
        public static void Update()
        {
            playerInControll = FocusStateManager.CurrentState == eFocusState.FPS || FocusStateManager.CurrentState == eFocusState.Dead;
            if (playerInControll)
            {

                bool menuOpen = currentMenu != null;
                if (menuOpen)
                {
                    Dictionary<GameObject, zMenu.zMenuNode> nodeDict = new(currentMenu.nodes.Count + 1);
                    foreach (var node in currentMenu.allNodes)
                    {
                        nodeDict[node.gameObject] = node;
                    }
                    List<GameObject> nodeList = nodeDict.Keys.ToList();
                    GameObject selectedNodeObject = zSearch.GetClosestObjectInLookDirection(Camera.main.transform, nodeList, 10f);
                    selectedNode = null;
                    if (selectedNodeObject != null)
                    {
                        nodeDict.TryGetValue(selectedNodeObject, out selectedNode);
                    }
                    foreach (zMenu.zMenuNode node in nodeDict.Values)
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
                    currentMenu.Update();
                    if (selectedNodeObject == null)
                    {
                        Vector3 angleToTarget = (currentMenu.gameObject.transform.position - Camera.main.transform.position).normalized;
                        Vector3 cameraAngle = Camera.main.transform.forward;
                        float angleDelta = Vector3.Angle(cameraAngle, angleToTarget);
                        if (angleDelta > 45) //close if we're looking too far away.
                            CloseAllMenues();
                        else if (zSearch.GetClosestObjectInLookDirection(Camera.main.transform, nodeList, 20f) == null) //close if we're not looking near a node with a wider tolerance.
                            CloseAllMenues();
                    }
                }
                if (Input.GetKey(KeyCode.M))
                {
                    if (pressable) //is this the first frame of holding the button?
                    {
                        if (!menuOpen) //is the menu closed?
                        {
                            mainMenu.Open();
                            ZiMain.log.LogInfo($"Main menu opened");
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
            foreach (zMenu menu in menues)
            {
                menu.ResetRelativePosition();
                menu.Close();
            }
        }
        public static void LateUpdate()
        {
            if (playerInControll)
            {
                if (currentMenu != null)
                {
                    currentMenu.Lateupdate();
                }
            }
        }
        public static void OnFactoryBuildDone()
        {
            menuParrent = GameObject.Find(menuParrentPath);
            mainMenu = new zMenu("Main");
            mainMenu.centerNode.AddListener(zMenuManager.nodeEvent.OnUnpressedSelected, CloseAllMenues);
            registerMenu(mainMenu);
        }
    }
}

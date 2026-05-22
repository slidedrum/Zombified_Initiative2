using CollisionRundown.Features.HUDs;
using SlideMenu;
using System;
using UnityEngine;
using BotControl.Patches;
using BotControl;

namespace BotControl.Menus
{
    public static class DebugMenuClass
    {
        public static sMenu debugMenu;
        public static sMenu debugNodeMenu;
        public static sMenu debugNodeSettingsMenu;
        public static sMenu debugCameraCullingMenu;
        public static sMenu debugHooksEnabled;

        public static void Setup(sMenu menu)
        {
            //debugMenu = sMenuManager.createMenu("debug", sMenuManager.mainMenu);
            debugMenu = menu;
            debugNodeMenu = sMenuManager.createMenu("Nodes", debugMenu);
            debugNodeSettingsMenu = sMenuManager.createMenu("Settings", debugNodeMenu);
            debugCameraCullingMenu = sMenuManager.createMenu("Camera culling", debugMenu);
            debugHooksEnabled = sMenuManager.createMenu("UseHooks", debugMenu);
            debugMenu.AddNode("Show title prompt", InGameTitle.DisplayDefault).AddListener(sMenuManager.nodeEvent.OnUnpressedSelected, debugMenu.Close); ;
            
            debugMenu.AddNode("ChecVis")
                .AddListener(sMenuManager.nodeEvent.OnUnpressedSelected, zDebug.setCheckVizTarget)
                .AddListener(sMenuManager.nodeEvent.OnUnpressedSelected, zDebug.debugCheckViz)
                .AddListener(sMenuManager.nodeEvent.OnHeldImmediate, zDebug.toggleVisCheck)
                .AddListener(sMenuManager.nodeEvent.OnHeldImmediate, sMenuManager.CloseAllMenus)
                .AddListener(sMenuManager.nodeEvent.OnTappedExclusive, sMenuManager.CloseAllMenus)
                .AddListener(sMenuManager.nodeEvent.OnDoubleTapped, zDebug.setVisCheck, false)
                .AddListener(sMenuManager.nodeEvent.OnDoubleTapped, sMenuManager.CloseAllMenus)
            ;
            debugMenu.AddNode("Find unexplored", zDebug.MarkUnexploredArea);
            debugMenu.AddNode("SendBotToExplore", zDebug.SendClosestBotToExplore);
            debugMenu.AddNode("Show corners", zDebug.debugCorners);
            //debugMenu.AddNode("Toggle explore",ExploreAction.ToggleCanExplore);
            debugNodeMenu.AddNode("Node I'm looking at", zDebug.GetNodeImLookingAT, [sMenuManager.mainMenu.gameObject.transform]);
            debugNodeMenu.AddNode("Toggle Nodes", zDebug.ToggleNodes);
            debugNodeMenu.AddNode("Toggle Connections", zDebug.ToggleConnections);
            debugNodeMenu.AddNode("Toggle Node Info", zDebug.ToggleNodeInfo);
            debugNodeSettingsMenu.radius = 130;
            debugHooksEnabled.AddNode("Use get item priority", toggleUseItemPrio);
            debugHooksEnabled.AddNode("Use updatePickupAction", toggleUsePickupAction);
            var gridSizeNode = debugNodeSettingsMenu.AddNode("Grid Size");
            gridSizeNode.AddListener(sMenuManager.nodeEvent.WhileSelected, DebugMenuClass.ChangeValueBasedOnMouseWheel, [DebugValueToChange.NodeGridSize, gridSizeNode, 0.1f]);
            gridSizeNode.SetSubtitle($"{zVisitedManager.NodeGridSize}");
            var mapGridSizeNode = debugNodeSettingsMenu.AddNode("Map Grid Size");
            mapGridSizeNode.AddListener(sMenuManager.nodeEvent.WhileSelected, DebugMenuClass.ChangeValueBasedOnMouseWheel, [DebugValueToChange.NodeMapSize, mapGridSizeNode, 1f]);
            mapGridSizeNode.SetSubtitle($"{zVisitedManager.NodeMapGridSize}");
            var visitDistanceNode = debugNodeSettingsMenu.AddNode("Visit distnace");
            visitDistanceNode.AddListener(sMenuManager.nodeEvent.WhileSelected, DebugMenuClass.ChangeValueBasedOnMouseWheel, [DebugValueToChange.NodeVisitDistance, visitDistanceNode, 0.5f]);
            visitDistanceNode.SetSubtitle($"{zVisitedManager.NodeVisitDistance}");
            var propigationAmmountNode = debugNodeSettingsMenu.AddNode("Propigation ammount");
            propigationAmmountNode.AddListener(sMenuManager.nodeEvent.WhileSelected, DebugMenuClass.ChangeValueBasedOnMouseWheel, [DebugValueToChange.PropigationAmmount, propigationAmmountNode, 1f]);
            propigationAmmountNode.SetSubtitle($"{zVisitedManager.propigationAmmount}");
            var propigationSameCountNode = debugNodeSettingsMenu.AddNode("Propigation sample count");
            propigationSameCountNode.AddListener(sMenuManager.nodeEvent.WhileSelected, DebugMenuClass.ChangeValueBasedOnMouseWheel, [DebugValueToChange.PropigationSampleCount, propigationSameCountNode, 1f]);
            propigationSameCountNode.SetSubtitle($"{zVisitedManager.propigationSampleCount}");
            var nodesPerFrameNode = debugNodeSettingsMenu.AddNode("Nodes per frame");
            nodesPerFrameNode.AddListener(sMenuManager.nodeEvent.WhileSelected, DebugMenuClass.ChangeValueBasedOnMouseWheel, [DebugValueToChange.NodesCreatedPerFrame, nodesPerFrameNode, 1f]);
            nodesPerFrameNode.SetSubtitle($"{zVisitedManager.nodesCreatedPerFrame}");
            var connectionChecksPerFrameNode = debugNodeSettingsMenu.AddNode("Connections per frame");
            connectionChecksPerFrameNode.AddListener(sMenuManager.nodeEvent.WhileSelected, DebugMenuClass.ChangeValueBasedOnMouseWheel, [DebugValueToChange.connectionChecksPerFrame, connectionChecksPerFrameNode, 1f]);
            connectionChecksPerFrameNode.SetSubtitle($"{zVisitedManager.connectionChecksPerFrame}");
            CullingMenuClass.setupCullingMenu(debugCameraCullingMenu);
            debugCameraCullingMenu.radius = 140;
            debugCameraCullingMenu.setNodeSize(0.5f);
        }

        private static void toggleUsePickupAction()
        {
            PickupActionPatch.useUpdateActionCollectItem = !PickupActionPatch.useUpdateActionCollectItem;
            ZiMain.log.LogInfo($"Toggled pickup action hook: {PickupActionPatch.useUpdateActionCollectItem}");
        }

        private static void toggleUseItemPrio()
        {
            PickupActionPatch.useGetItemPrio = !PickupActionPatch.useGetItemPrio;
            ZiMain.log.LogInfo($"Toggled item prio hook: {PickupActionPatch.useGetItemPrio}");
        }

        public enum DebugValueToChange
        {
            NodeGridSize,
            NodeMapSize,
            NodeVisitDistance,
            PropigationAmmount,
            PropigationSampleCount,
            NodesCreatedPerFrame,
            connectionChecksPerFrame,
        }
        public static void ChangeValueBasedOnMouseWheel(DebugValueToChange valueToChange, sMenu.sMenuNode node, float increment = 0.1f)
        {
            if (node == null || !node.gameObject.activeInHierarchy)
                return;
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            int normalizedScroll = (int)Mathf.Sign(scroll);
            if (scroll == 0f)
                return;
            float offset = normalizedScroll * increment;
            float value = 0;
            switch (valueToChange)
            {
                case DebugValueToChange.NodeMapSize:
                    zVisitedManager.SetNodeMapGridSize((int)offset + zVisitedManager.NodeMapGridSize);
                    value = zVisitedManager.NodeMapGridSize;
                    break;
                case DebugValueToChange.NodeGridSize:
                    zVisitedManager.SetNodeGridSize(offset + zVisitedManager.NodeGridSize);
                    value = zVisitedManager.NodeGridSize;
                    break;
                case DebugValueToChange.NodeVisitDistance:
                    zVisitedManager.SetNodeVisitDistance(offset + zVisitedManager.NodeVisitDistance);
                    value = zVisitedManager.NodeVisitDistance;
                    break;
                case DebugValueToChange.PropigationAmmount:
                    zVisitedManager.SetPropigationAmmount((int)offset + zVisitedManager.propigationAmmount);
                    value = zVisitedManager.propigationAmmount;
                    break;
                case DebugValueToChange.PropigationSampleCount:
                    zVisitedManager.SetPropigationSampleCount((int)offset + zVisitedManager.propigationSampleCount);
                    value = zVisitedManager.propigationSampleCount;
                    break;
                case DebugValueToChange.NodesCreatedPerFrame:
                    zVisitedManager.nodesCreatedPerFrame = Math.Max((int)offset + zVisitedManager.nodesCreatedPerFrame, 1);
                    value = zVisitedManager.nodesCreatedPerFrame;
                    break;
                case DebugValueToChange.connectionChecksPerFrame:
                    zVisitedManager.connectionChecksPerFrame = Math.Max((int)offset + zVisitedManager.connectionChecksPerFrame, 1);
                    value = zVisitedManager.connectionChecksPerFrame;
                    break;
                default:
                    Debug.LogWarning("Unknown DebugValueToChange: " + valueToChange);
                    break;
            }
            node.SetSubtitle($"{value}");
        }
        public static class CullingMenuClass
        {
            public static void setupCullingMenu(sMenu menu)
            {
                for (int i = 0; i < 32; i++)
                {
                    string name = LayerMask.LayerToName(i);
                    if (string.IsNullOrEmpty(name))
                        continue;
                    Camera camera = Camera.main;
                    var node = menu.AddNode(name);
                    node.AddListener(sMenuManager.nodeEvent.OnUnpressedSelected, ToggleLayer, camera, i, node);
                }
            }
            public static void ToggleLayer(Camera camera, int layer, sMenu.sMenuNode node)
            {
                if (camera == null)
                {
                    Debug.LogWarning("Camera is null!");
                    return;
                }

                if (layer < 0 || layer > 31)
                {
                    Debug.LogWarning("Layer index out of range (0-31)!");
                    return;
                }

                // XOR the bit for the layer to toggle it
                camera.cullingMask ^= 1 << layer;
                bool isVisible = (camera.cullingMask & 1 << layer) != 0;
                if (isVisible)
                    node.SetColor(new Color(0, 0.2f, 0));
                else
                    node.SetColor(new Color(0.2f, 0, 0));
            }
        }
    }
}

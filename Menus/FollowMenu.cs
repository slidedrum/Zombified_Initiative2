using Il2CppSystem.Xml.Schema;
using Player;
using SlideMenu;
using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieTweak2.Patches;
using ZombieTweak2.zRootBotPlayerAction;
using Zombified_Initiative;

namespace ZombieTweak2.Menus
{
    public static class FollowMenuClass
    {
        private static sMenu followMenu;
        private static sMenu.sMenuNode followMenuNode;
        private static Dictionary<DRAMA_State, sMenu.sMenuNode> stateNodes;
        private static Dictionary<string, sMenu.sMenuNode> catagoryNodes;
        public static DRAMA_State previousState;
        public static Color currentStateColor;
        public static Color defaultColor;
        //private static OverrideTree<float?> prio;
        public static OverrideTree<int?> followRadius;
        public static OverrideTree<int?> maxDistance;
        private static List<DRAMA_State> fightingStates;
        private static List<DRAMA_State> ignoredStates;

        internal static void Setup(sMenu menu)
        {
            defaultColor = menu.getTextColor();
            currentStateColor = new(0f, 0.2f, 0f);
            followMenu = menu;
            followMenuNode = followMenu.GetNode();
            previousState = DramaManager.CurrentStateEnum;
            FollowActionPatch.Setup();
            fightingStates = new();
            ignoredStates = new();
            stateNodes = new();
            catagoryNodes = new();
            followRadius = new(7, "followRadius");
            maxDistance = new(10, "maxDistance");
            fightingStates.Add(DRAMA_State.Combat);
            fightingStates.Add(DRAMA_State.Encounter);
            fightingStates.Add(DRAMA_State.IntentionalCombat);
            fightingStates.Add(DRAMA_State.Survival);
            fightingStates.Add(DRAMA_State.Alert);
            ignoredStates.Add(DRAMA_State.ElevatorGoingDown);
            ignoredStates.Add(DRAMA_State.ElevatorIdle);

            zSlideComputer.ActionPriorities.AddNode("Fighting", null, "Follow", defaultValue: null, hasDefaultValue: true, condition: () => { return fightingStates.Contains(DramaManager.CurrentStateEnum); });
            zSlideComputer.ActionPriorities.AddNode("Stealth", null, "Follow", defaultValue: null, hasDefaultValue: true, condition: () => { return DramaManager.CurrentStateEnum == DRAMA_State.Sneaking; });
            zSlideComputer.ActionPriorities.AddNode("Explore", null, "Follow", defaultValue: null, hasDefaultValue: true, condition: () => { return DramaManager.CurrentStateEnum == DRAMA_State.Exploration; });

            followRadius.AddNode("Follow", null, (string?)null);
            followRadius.AddNode("Fighting", null, "Follow", defaultValue: null, hasDefaultValue: true, condition: () => { return fightingStates.Contains(DramaManager.CurrentStateEnum); });
            followRadius.AddNode("Stealth", null, "Follow", defaultValue: null, hasDefaultValue: true, condition: () => { return DramaManager.CurrentStateEnum == DRAMA_State.Sneaking; });
            followRadius.AddNode("Explore", null, "Follow", defaultValue: null, hasDefaultValue: true, condition: () => { return DramaManager.CurrentStateEnum == DRAMA_State.Exploration; });
            
            maxDistance.AddNode("Follow", null, (string?)null);
            maxDistance.AddNode("Fighting", null, "Follow", defaultValue: null, hasDefaultValue: true, condition: () => { return fightingStates.Contains(DramaManager.CurrentStateEnum); });
            maxDistance.AddNode("Stealth", null, "Follow", defaultValue: null, hasDefaultValue: true, condition: () => { return DramaManager.CurrentStateEnum == DRAMA_State.Sneaking; });
            maxDistance.AddNode("Explore", null, "Follow", defaultValue: null, hasDefaultValue: true, condition: () => { return DramaManager.CurrentStateEnum == DRAMA_State.Exploration; });

            followRadius.nodes["Follow"].onChanged.Listen(UpdateNodeSettingsDisplay, args: [followMenuNode]);
            maxDistance.nodes["Follow"].onChanged.Listen(UpdateNodeSettingsDisplay, args: [followMenuNode]);

            catagoryNodes["Fighting"] = AddCatagoryNode("Fighting");
            catagoryNodes["Stealth"] = AddCatagoryNode("Stealth");
            catagoryNodes["Explore"] = AddCatagoryNode("Explore");

            followMenu.AddCatagory("Basic");
            followMenu.AddNodeToCatagory("Basic", "Fighting");
            followMenu.AddNodeToCatagory("Basic", "Stealth");
            followMenu.AddNodeToCatagory("Basic", "Explore");
            followMenu.AddCatagory("Advanced");

            followMenu.AddPannel(sMenu.sMenuPannel.Side.top, "Controlls when and how closely the bots follow their leader.");
            followMenu.AddPannel(sMenu.sMenuPannel.Side.bottom, "Top: Priority, how important is staying in range?");
            followMenu.AddPannel(sMenu.sMenuPannel.Side.bottom, "Bottom left: Range, how close should the bots be?");
            followMenu.AddPannel(sMenu.sMenuPannel.Side.bottom, "Bottom right: Max distance, When should bots sprint?");
            followMenu.AddPannel(sMenu.sMenuPannel.Side.right, "Scroll => change setting");
            followMenu.AddPannel(sMenu.sMenuPannel.Side.right, "Hold => reset");


            foreach (DRAMA_State state in Enum.GetValues(typeof(DRAMA_State)))
            {
                if (ignoredStates.Contains(state))
                    continue;
                string parentNode = "Follow";
                switch (state)
                {
                    case DRAMA_State.Sneaking:
                        parentNode = "Stealth";
                        break;
                    case DRAMA_State.Exploration:
                        parentNode = "Explore";
                        break;
                    case var s when fightingStates.Contains(s):
                        parentNode = "Fighting";
                        break;
                }

                var stateNode = followMenu.AddNode(state.ToString());
                followRadius.AddNode(state.ToString(), null, parentNode, () => { return DramaManager.CurrentStateEnum == state; });
                maxDistance.AddNode(state.ToString(), null, parentNode, () => { return DramaManager.CurrentStateEnum == state; });
                followRadius.nodes[state.ToString()].onChanged.Listen(UpdateNodeSettingsDisplay, args: [stateNode]);
                maxDistance.nodes[state.ToString()].onChanged.Listen(UpdateNodeSettingsDisplay, args: [stateNode]);

                //TODO these lines are horendious omg.
                zSlideComputer.ActionPriorities.AddNode(state.ToString(), null, parentNode, () => { return DramaManager.CurrentStateEnum == state; }, defaultValue: null, hasDefaultValue: true).onChanged.Listen(UpdateNodeSettingsDisplay, args: [stateNode]);
                zSlideComputer.ActionPermissions.AddNode(state.ToString(), null, parentNode, defaultValue: null, hasDefaultValue: true).onChanged.Listen(UpdateNodeSettingsDisplay, args: [stateNode, state.ToString()]);
                //zSlideComputer.actionNameToMenuNodes[state.ToString()] = stateNode;
                
                stateNodes[state] = stateNode;
                UpdateNodeSettingsDisplay(stateNode);
                stateNode.titlePart.SetScale(0.5f);
                stateNode.subtitlePart.SetScale(0.5f);
                stateNode.AddListener(sMenuManager.nodeEvent.WhileSelected, UpdateNodeBasedOnScroll, stateNode);
                stateNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, ResetSettings, stateNode);    
                stateNode.AddListener(sMenuManager.nodeEvent.OnTapped, zSlideComputer.GenericToggleAllowed, stateNode.text, stateNode);
                followMenu.AddNodeToCatagory("Advanced", stateNode);
            }

            followMenu.AddListener(sMenuManager.menuEvent.WhileOpened, UpdateHighlightedState);
            followMenu.AddListener(sMenuManager.menuEvent.OnOpened, UpdateAllNodes);
            followMenu.AddListener(sMenuManager.menuEvent.OnCatagoryChanged, UpdateAllNodes);

            followMenu.centerNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
            followMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, ResetAllLocalSettings);
            followMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnTapped, followMenu.parrentMenu.Open);

            followMenuNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
            followMenuNode.ClearListeners(sMenuManager.nodeEvent.WhileSelected);

            followMenuNode.AddListener(sMenuManager.nodeEvent.WhileSelected, UpdateNodeBasedOnScroll, followMenuNode);
            followMenuNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediateSelected, ResetSettings, followMenuNode);
            followMenuNode.AddListener(sMenuManager.nodeEvent.OnDoubleTapped, followMenu.Open);

            followMenu.radius = 130f;
            AutomaticActionMenuClass.AutoActionMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediateSelected, FollowMenuClass.ResetSettings, followMenuNode);
            //AutomaticActionMenuClass.AutoActionMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediateSelected, FollowMenuClass.setAllowed, true);
            //ActionPriorities.Add("Follow", new OverrideTree<float?>(zSlideComputer.PermissionDefinitions.GetDefaultPriority("Follow")).AddNode("Follow", null).Tree);
            UpdateNodeSettingsDisplay(followMenuNode);
            followMenu.SetCatagory("Basic");
        }
        //public static void setAllowed(bool allowed, bool allowDissabled = false)
        //{
        //    bool current = (bool)AutomaticActionMenuClass.ActionPermissions.ValueAt("Follow");

        //    // Already in desired state
        //    if (current == allowed)
        //        return;
        //    if (!followMenuNode.gameObject.activeInHierarchy && !allowDissabled)
        //        return;
        //    AutomaticActionMenuClass.ActionPermissions.SetValue("Follow", allowed);
        //    var allbots = ZiMain.GetBotList();

        //    foreach (var bot in allbots)
        //    {
        //        var data = zActions.GetOrCreateData(bot);

        //        if (allowed)
        //        {
        //            // Restore original leader
        //            if (bot.SyncValues.Leader != bot.Agent)
        //            {
        //                // Leader was externally changed, preserve it
        //                data.actualLeader = bot.SyncValues.Leader;
        //                continue;
        //            }

        //            if (data.actualLeader == bot.Agent)
        //            {
        //                ZiMain.log.LogWarning(
        //                    $"Actual leader for {bot.Agent.PlayerName} got lost. Resetting to local player");

        //                data.actualLeader = PlayerManager.GetLocalPlayerAgent();
        //            }

        //            bot.SyncValues.Leader = data.actualLeader;
        //        }
        //        else
        //        {
        //            // Disable following by setting self as leader
        //            if (bot.SyncValues.Leader == bot.Agent)
        //            {
        //                ZiMain.log.LogWarning(
        //                    $"Follow leader for {bot.Agent.PlayerName} got lost. Resetting to local player");

        //                data.actualLeader = PlayerManager.GetLocalPlayerAgent();
        //            }
        //            else
        //            {
        //                // Backup current real leader
        //                data.actualLeader = bot.SyncValues.Leader;
        //            }

        //            bot.SyncValues.Leader = bot.Agent;
        //        }
        //    }

        //    UpdateToggleStateColors();
        //}
        
        private static void UpdateToggleStateColors()
        {
            if ((bool)zSlideComputer.ActionPermissions.ValueAt("Follow"))
            {
                followMenuNode.SetColor(sMenuManager.defaultColor);
                followMenu.centerNode.SetColor(sMenuManager.defaultColor);
            }
            else
            {
                followMenuNode.SetColor(new Color(0.25f, 0f, 0f));
                followMenu.centerNode.SetColor(new Color(0.25f, 0f, 0f));
            }
        }
        private static sMenu.sMenuNode AddCatagoryNode(string catagory)
        {
            var catagoryNode = followMenu.AddNode(catagory);
            zSlideComputer.ActionPriorities.nodes[catagory].onChanged.Listen(UpdateNodeSettingsDisplay, args: [catagoryNode]);
            followRadius.nodes[catagory].onChanged.Listen(UpdateNodeSettingsDisplay, args: [catagoryNode]);
            maxDistance.nodes[catagory].onChanged.Listen(UpdateNodeSettingsDisplay, args: [catagoryNode]);
            catagoryNode.titlePart.SetScale(0.5f);
            catagoryNode.subtitlePart.SetScale(0.5f);
            catagoryNode.AddListener(sMenuManager.nodeEvent.WhileSelected, UpdateNodeBasedOnScroll, catagoryNode);
            catagoryNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, ResetSettings, catagoryNode);
            catagoryNode.AddListener(sMenuManager.nodeEvent.OnTapped, zSlideComputer.GenericToggleAllowed, catagory, catagoryNode);
            //zSlideComputer.actionNameToMenuNodes[catagory] = catagoryNode;
            zSlideComputer.ActionPermissions.AddNode(catagory, null, "Follow", defaultValue: null, hasDefaultValue: true).onChanged.Listen(UpdateNodeSettingsDisplay, args: [catagoryNode, catagory]);
            return catagoryNode;
        }
        internal static void ResetSettings(sMenu.sMenuNode node)
        {
            string text = node.text;
            zSlideComputer.ActionPermissions.SetValue(text, null);
            zSlideComputer.ActionPriorities.SetValue(text, null);
            followRadius.SetValue(text, null);
            maxDistance.SetValue(text, null);
            UpdateNodeSettingsDisplay(node);
        }
        private static void ResetAllLocalSettings()
        {
            foreach (var Node in followMenu.currentCatagory)
                ResetSettings(Node);
        }
        private static void UpdateHighlightedState(bool breakOnSameState = true)
        {
            if (breakOnSameState && previousState == DramaManager.CurrentStateEnum)
                return;
            if (followMenu.currentCatagoryName == "Advanced")
            {
                if (stateNodes.ContainsKey(DramaManager.CurrentStateEnum))
                {
                    if (stateNodes.ContainsKey(previousState))
                        stateNodes[previousState].SetColor(defaultColor);
                    stateNodes[DramaManager.CurrentStateEnum].SetColor(currentStateColor);
                }
                else if (stateNodes.ContainsKey(previousState))
                {
                    stateNodes[previousState].SetColor(defaultColor);
                }
            }
            if (followMenu.currentCatagoryName == "Advanced")
            {
                switch (DramaManager.CurrentStateEnum)
                {
                    case DRAMA_State.Sneaking:
                        catagoryNodes["Stealth"].SetColor(currentStateColor);
                        catagoryNodes["Fighting"].SetColor(defaultColor);
                        catagoryNodes["Explore"].SetColor(defaultColor);
                        break;
                    case DRAMA_State.Exploration:
                        catagoryNodes["Stealth"].SetColor(defaultColor);
                        catagoryNodes["Fighting"].SetColor(defaultColor);
                        catagoryNodes["Explore"].SetColor(currentStateColor);
                        break;
                    case var s when fightingStates.Contains(s):
                        catagoryNodes["Stealth"].SetColor(defaultColor);
                        catagoryNodes["Fighting"].SetColor(currentStateColor);
                        catagoryNodes["Explore"].SetColor(defaultColor);
                        break;
                }
            }
            previousState = DramaManager.CurrentStateEnum;
        }


        private static void UpdateNodeBasedOnScroll(sMenu.sMenuNode node)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            float normalizedScroll = (int)Mathf.Sign(scroll);
            if (scroll == 0f)
                return;
            string text = node.text;
            var pos = Camera.main.WorldToViewportPoint(node.gameObject.transform.position);
            pos = new Vector2(pos.x - 0.5f, pos.y - 0.5f) * -1;
            if (pos.y > Math.Abs(pos.x)) // TOP
            {
                normalizedScroll = normalizedScroll * 0.1f;
                float newValue = (float)zSlideComputer.ActionPriorities.ValueAt(text) + normalizedScroll;
                newValue = (float)Math.Round(newValue, 1);
                zSlideComputer.ActionPriorities.SetValue(text, Math.Clamp(newValue, 1, 15));
            }
            else if (pos.x > 0) // RIGHT
            {
                maxDistance.SetValue(text, Math.Clamp((int)maxDistance.ValueAt(text) + (int)normalizedScroll, (int)followRadius.ValueAt(text), 60));
            }
            else // LEFT
            {
                followRadius.SetValue(text, Math.Clamp((int)followRadius.ValueAt(text) + (int)normalizedScroll, 1, (int)maxDistance.ValueAt(text)));
            }
            UpdateNodeSettingsDisplay(node);
        }
        private static void UpdateAllNodes()
        {
            foreach (var Node in followMenu.nodes)
            {
                UpdateNodeSettingsDisplay(Node);
            }
        }

        private static void UpdateNodeSettingsDisplay(sMenu.sMenuNode node, string actionKey = null)
        {

            //List<IOverrideTree> defaultTrees = new()
            //{
            //    zSlideComputer.ActionPriorities,
            //    followRadius,
            //    maxDistance,
            //};
            if (actionKey == null)
                actionKey = node.text;
            List<IOverrideTree> extraTrees = new()
            {
                followRadius,
                maxDistance,
            };
            //bool italic = AutomaticActionMenuClass.AnyTreeOverridesNullDefault(trees, actionKey);
            //bool star = !AutomaticActionMenuClass.AllMatchingDefaultValue(trees, actionKey);
            //AutomaticActionMenuClass.ApplyTextEffectToNode(node, AutomaticActionMenuClass.textEffect.Star, star);
            //AutomaticActionMenuClass.ApplyTextEffectToNode(node, AutomaticActionMenuClass.textEffect.Italic, italic);
            AutomaticActionMenuClass.GenericUpdateNodePrioDisplay(node);
            AutomaticActionMenuClass.GenericUpdateNodeAllowedDisplay(actionKey, node);
            AutomaticActionMenuClass.ApplyTextEffects(node, actionKey, extraTrees);
            //node.SetTitle($"Prio <color=#CC840066>[</color>{zSlideComputer.ActionPriorities.ValueAt(text)}<color=#CC840066>]</color>");
            node.SetSubtitle($"Range <color=#CC840066>[</color>{followRadius.ValueAt(actionKey)}/{maxDistance.ValueAt(actionKey)}<color=#CC840066>]</color>");
        }
    }
}

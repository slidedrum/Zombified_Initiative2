using SlideMenu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ZombieTweak2.CustomActions.Patches;
using ZombieTweak2.zRootBotPlayerAction;
using Zombified_Initiative;
using Player;

namespace ZombieTweak2.Menus
{
    public static class FollowMenuClass
    {
        private static sMenu followMenu;
        private static sMenu.sMenuNode followMenuNode;
        private static Dictionary<DRAMA_State, sMenu.sMenuNode> stateNodes = new();
        private static Dictionary<string, sMenu.sMenuNode> catagoryNodes = new();
        public static DRAMA_State previousState;
        public static Color currentStateColor;
        public static Color defaultColor;
        public static OverrideTree<float?> prio = AutomaticActionMenuClass.ActionPriorities["Follow"];
        public static OverrideTree<int?> followRadius = new(7);
        public static OverrideTree<int?> maxDistance = new(10);
        public static OverrideTree<bool?> followEnabled = new(true);
        private static List<DRAMA_State> fightingStates = new();
        private static List<DRAMA_State> ignoredStates = new();

        internal static void Setup(sMenu menu)
        {
            defaultColor = menu.getTextColor();
            currentStateColor = new(0f, 0.2f, 0f);
            followMenu = menu;
            followMenuNode = followMenu.GetNode();
            previousState = DramaManager.CurrentStateEnum;
            FollowActionPatch.Setup();

            fightingStates.Add(DRAMA_State.Combat);
            fightingStates.Add(DRAMA_State.Encounter);
            fightingStates.Add(DRAMA_State.IntentionalCombat);
            fightingStates.Add(DRAMA_State.Alert);
            ignoredStates.Add(DRAMA_State.ElevatorGoingDown);
            ignoredStates.Add(DRAMA_State.ElevatorIdle);

            //prio = new(14);
            followRadius = new(7);
            maxDistance = new(10);
            followEnabled = new(true);
            followEnabled.AddNode("Main", true);
            //prio.AddNode("Follow", null);
            prio.AddNode("Fighting", null, "Follow", condition: () => { return fightingStates.Contains(DramaManager.CurrentStateEnum); });
            prio.AddNode("Stealth", null, "Follow", condition: () => { return DramaManager.CurrentStateEnum == DRAMA_State.Sneaking; });
            prio.AddNode("Explore", null, "Follow", condition: () => { return DramaManager.CurrentStateEnum == DRAMA_State.Exploration; });
            followRadius.AddNode("Follow", null);
            followRadius.AddNode("Fighting", null, "Follow", condition: () => { return fightingStates.Contains(DramaManager.CurrentStateEnum); });
            followRadius.AddNode("Stealth", null, "Follow", condition: () => { return DramaManager.CurrentStateEnum == DRAMA_State.Sneaking; });
            followRadius.AddNode("Explore", null, "Follow", condition: () => { return DramaManager.CurrentStateEnum == DRAMA_State.Exploration; });
            maxDistance.AddNode("Follow", null);
            maxDistance.AddNode("Fighting", null, "Follow", condition: () => { return fightingStates.Contains(DramaManager.CurrentStateEnum); });
            maxDistance.AddNode("Stealth", null, "Follow", condition: () => { return DramaManager.CurrentStateEnum == DRAMA_State.Sneaking; });
            maxDistance.AddNode("Explore", null, "Follow", condition: () => { return DramaManager.CurrentStateEnum == DRAMA_State.Exploration; });

            catagoryNodes["Fighting"] = AddCatagoryNode("Fighting");
            catagoryNodes["Stealth"] = AddCatagoryNode("Stealth");
            catagoryNodes["Explore"] = AddCatagoryNode("Explore");

            followMenu.AddCatagory("Basic");
            followMenu.AddNodeToCatagory("Basic", "Fighting");
            followMenu.AddNodeToCatagory("Basic", "Stealth");
            followMenu.AddNodeToCatagory("Basic", "Explore");
            followMenu.AddCatagory("Advanced");

            followMenu.AddPannel(sMenu.sMenuPannel.Side.bottom, "Top: Priority, how important is staing in range?");
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
                prio.AddNode(state.ToString(), null, parentNode, () => { return DramaManager.CurrentStateEnum == state; });
                followRadius.AddNode(state.ToString(), null, parentNode, () => { return DramaManager.CurrentStateEnum == state; });
                maxDistance.AddNode(state.ToString(), null, parentNode, () => { return DramaManager.CurrentStateEnum == state; });

                var stateNode = followMenu.AddNode(state.ToString());
                stateNodes[state] = stateNode;
                UpdateNodeSettingsDisplay(stateNode);
                stateNode.titlePart.SetScale(0.5f);
                stateNode.subtitlePart.SetScale(0.5f);
                stateNode.AddListener(sMenuManager.nodeEvent.WhileSelected, UpdateNodeBasedOnScroll, stateNode);
                stateNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, ResetSettings, stateNode);
                followMenu.AddNodeToCatagory("Advanced", stateNode);
            }
            followMenu.AddListener(sMenuManager.menuEvent.WhileOpened, UpdateHighlightedState);
            followMenu.AddListener(sMenuManager.menuEvent.OnOpened, UpdateAllNodes);
            followMenu.AddListener(sMenuManager.menuEvent.OnCatagoryChanged, UpdateAllNodes);
            followMenu.centerNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
            followMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, ResetAllLocalSettings);
            followMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnTapped, followMenu.parrentMenu.Open);
            followMenuNode.ClearListeners(sMenuManager.nodeEvent.OnUnpressedSelected);
            followMenuNode.AddListener(sMenuManager.nodeEvent.WhileSelected, UpdateNodeBasedOnScroll, followMenuNode);
            followMenuNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediateSelected, ResetSettings, followMenuNode);
            followMenuNode.AddListener(sMenuManager.nodeEvent.OnDoubleTapped, followMenu.Open);
            followMenu.radius = 130f;
            AutomaticActionMenuClass.AutoActionMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediateSelected, FollowMenuClass.ResetSettings, followMenuNode);
            AutomaticActionMenuClass.AutoActionMenu.centerNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediateSelected, FollowMenuClass.setAllowed, true);
            //ActionPriorities.Add("Follow", new OverrideTree<float?>(zSlideComputer.PermissionDefinitions.GetDefaultPriority("Follow")).AddNode("Follow", null).Tree);
            UpdateNodeSettingsDisplay(followMenuNode);
            followMenu.SetCatagory("Basic");
        }
        public static void setAllowed(bool enabled, bool allowDissabled = false)
        {
            bool current = (bool)followEnabled.GetValue();

            // Already in desired state
            if (current == enabled)
                return;
            if (!followMenuNode.gameObject.activeInHierarchy && !allowDissabled)
                return;
            followEnabled.SetValue("Main", enabled);

            var allbots = ZiMain.GetBotList();

            foreach (var bot in allbots)
            {
                var data = zActions.GetOrCreateData(bot);

                if (enabled)
                {
                    // Restore original leader
                    if (bot.SyncValues.Leader != bot.Agent)
                    {
                        // Leader was externally changed, preserve it
                        data.actualLeader = bot.SyncValues.Leader;
                        continue;
                    }

                    if (data.actualLeader == bot.Agent)
                    {
                        ZiMain.log.LogWarning(
                            $"Actual leader for {bot.Agent.PlayerName} got lost. Resetting to local player");

                        data.actualLeader = PlayerManager.GetLocalPlayerAgent();
                    }

                    bot.SyncValues.Leader = data.actualLeader;
                }
                else
                {
                    // Disable following by setting self as leader
                    if (bot.SyncValues.Leader == bot.Agent)
                    {
                        ZiMain.log.LogWarning(
                            $"Follow leader for {bot.Agent.PlayerName} got lost. Resetting to local player");

                        data.actualLeader = PlayerManager.GetLocalPlayerAgent();
                    }
                    else
                    {
                        // Backup current real leader
                        data.actualLeader = bot.SyncValues.Leader;
                    }

                    bot.SyncValues.Leader = bot.Agent;
                }
            }

            UpdateToggleStateColors();
        }
        private static void TogglePerms()
        {
            zSlideComputer.PermissionDefinitions.SetAllowed("Follow", !zSlideComputer.PermissionDefinitions.GetAllowed("Follow", 1));
        }
        private static void OldToggleFollowEnabled()
        {
            bool allowed = (bool)followEnabled.SetValue("Main", !(bool)followEnabled.GetValue()); // Invert value
            var allbots = ZiMain.GetBotList();
            foreach (var bot in allbots)
            {
                var data = zActions.GetOrCreateData(bot);
                if (allowed)
                {
                    if (bot.SyncValues.Leader != bot.Agent)//Leader was changed to something else, don't revert
                    {
                        data.actualLeader = bot.SyncValues.Leader;
                        continue;
                    }
                    if (data.actualLeader == bot.Agent)
                    {
                        ZiMain.log.LogWarning($"Actual leader for {bot.Agent.PlayerName} got lost.  Reseting to local player");
                        data.actualLeader = PlayerManager.GetLocalPlayerAgent();
                    }
                    bot.SyncValues.Leader = data.actualLeader;

                }
                else
                {
                    if (bot.SyncValues.Leader == bot.Agent)
                    {
                        ZiMain.log.LogWarning($"Follow leader for {bot.Agent.PlayerName} got lost.  Reseting to local player");
                        data.actualLeader = PlayerManager.GetLocalPlayerAgent();
                    }
                    else
                    {
                        data.actualLeader = bot.SyncValues.Leader; // Backup the real leader
                    }
                    bot.SyncValues.Leader = bot.Agent; // Set leader to itself
                }
            }
            UpdateToggleStateColors();
        }
        private static void UpdateToggleStateColors()
        {
            if ((bool)followEnabled.GetValue())
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
            catagoryNode.titlePart.SetScale(0.5f);
            catagoryNode.subtitlePart.SetScale(0.5f);
            catagoryNode.AddListener(sMenuManager.nodeEvent.WhileSelected, UpdateNodeBasedOnScroll, catagoryNode);
            catagoryNode.AddListener(sMenuManager.nodeEvent.OnHeldImmediate, ResetSettings, catagoryNode);
            return catagoryNode;
        }
        internal static void ResetSettings(sMenu.sMenuNode node)
        {
            string text = node.text;
            prio.SetValue(text, null);
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
                float newValue = (float)prio.ValueAt(text) + normalizedScroll;
                newValue = (float)Math.Round(newValue, 1);
                prio.SetValue(text, Math.Clamp(newValue, 1, 15));
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
        private static void UpdateNodeSettingsDisplay(sMenu.sMenuNode node)
        {
            string text = node.text;
            if (prio.nodes[text].IsDefaultValue() && followRadius.nodes[text].IsDefaultValue() && maxDistance.nodes[text].IsDefaultValue())
            {
                node.SetPrefix("");
                node.SetSuffix("");
            }
            else
            {
                node.SetPrefix("* ");
                node.SetSuffix(" *");
            }
            node.SetTitle($"Prio <color=#CC840066>[</color>{prio.ValueAt(text)}<color=#CC840066>]</color>");
            node.SetSubtitle($"Range <color=#CC840066>[</color>{followRadius.ValueAt(text)}/{maxDistance.ValueAt(text)}<color=#CC840066>]</color>");
        }
    }
}

using Agents;
using BetterBots.Data;
using Enemies;
using Il2CppInterop.Runtime.Injection;
using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Zombified_Initiative;

namespace ZombieTweak2.zRootBotPlayerAction.CustomActions
{
    internal class ExploreAction : CustomActionBase
    {
        private StateEnum state = StateEnum.None;
        VisitNode UnexploredNode = null;
        public static Dictionary<int, bool> ExplorePerms = new(); //bot.Agent.Owner.PlayerSlotIndex()
        public static new bool Setup()
        {
            return true;
        }
        public static bool ToggleExplorePerm(int index)
        {
            SetExplorePerm(index, !ExplorePerms[index]);
            return GetExplorePerm(index);
        }
        public static bool ToggleExplorePerm(PlayerAgent agent)
        {
            return ToggleExplorePerm(agent.Owner.PlayerSlotIndex());
        }
        public static bool ToggleExplorePerm(PlayerAIBot bot)
        {
            return ToggleExplorePerm(bot.Agent);
        }
        public static void SetExplorePerm(int index, bool allowed)
        {
            ExplorePerms[index] = allowed;
        }
        public static void SetExplorePerm(PlayerAgent agent, bool allowed)
        {
            SetExplorePerm(agent.Owner.PlayerSlotIndex(), allowed);
        }
        public static void SetExplorePerm(PlayerAIBot bot, bool allowed)
        {
            SetExplorePerm(bot.Agent, allowed);
        }
        public static bool GetExplorePerm(PlayerAIBot bot)
        {
            return GetExplorePerm(bot.Agent);
        }
        public static bool GetExplorePerm(PlayerAgent agent)
        {
            return GetExplorePerm(agent.Owner.PlayerSlotIndex());
        }
        public static bool GetExplorePerm(int index)
        {
            if (ExplorePerms.TryGetValue(index, out var perm)) { return perm; }
            return ExplorePerms[index] = true;
        }
        public PlayerBotActionTravel.Descriptor travelAction = null;
        public new class Descriptor : CustomActionBase.Descriptor
        {
            public new float Prio = 3;
            float lastLooked = 0;
            public bool canExplore = true;
            float lookCooldown = 5;
            List<string> typeIgnoreList = [
                typeof(RootPlayerBotAction).FullName,
                typeof(PlayerBotActionFollow).FullName,
                typeof(PlayerBotActionIdle).FullName,
                typeof(PlayerBotActionLook).FullName,
            ];
            List<string> typeBlackList = [
                typeof(PlayerBotActionCollectItem).FullName,
                typeof(PlayerBotActionAttack).FullName,
                typeof(PlayerBotActionRevive).FullName,
                typeof(PlayerBotActionHighlight).FullName,
                typeof(PlayerBotActionShareResourcePack).FullName,
            ];
            public Descriptor() : base(ClassInjector.DerivedConstructorPointer<Descriptor>())
            {
                ClassInjector.DerivedConstructorBody(this);
                //Don't use.  This is needed for Il2cpp nonsnse.
            }
            public Descriptor(IntPtr ptr) : base(ptr)
            {
                ClassInjector.DerivedConstructorBody(this);
                //Don't use.  This is needed for Il2cpp nonsnse.
            }
            public Descriptor(PlayerAIBot bot) : base(bot)
            {
                //Use this
            }
            private bool HasFoundEnemies()
            {
                Vector3 pos1 = Bot.transform.position;
                Vector3 pos2 = Bot.m_rootAction.Cast<RootPlayerBotAction.Descriptor>().ActionBase.Cast<RootPlayerBotAction>().m_followLeaderAction.Cast<PlayerBotActionFollow.Descriptor>().Client.transform.position; //holy shit this is dumb.
                if (ExploreAction.HasFoundEnemies(pos1) || ExploreAction.HasFoundEnemies(pos2))
                    return true;
                return false;
            }
            public override void compareAction(ref PlayerBotActionBase.Descriptor bestAction)
            {
                if (!GetExplorePerm(Bot))
                    return;
                if (lastLooked == 0)
                    lastLooked = Time.time;
                if (DramaManager.CurrentStateEnum != DRAMA_State.Exploration && DramaManager.CurrentStateEnum != DRAMA_State.Sneaking)
                    return;
                //if (DramaManager.EnemiesAreClose)
                //    return;
                if (Time.time - lastLooked < lookCooldown)
                    return;
                if (!IsTerminated())
                    return;
                if (HasFoundEnemies())
                    return;
                float maxprio = 0f;
                foreach (var act in Bot.Actions)
                {
                    if (typeBlackList.Contains(act.GetIl2CppType().FullName))
                        return;
                    if (typeIgnoreList.Contains(act.GetIl2CppType().FullName))
                        continue;
                    var desc = act.DescBase;
                    maxprio = Math.Max(desc.Prio, maxprio);
                }
                if (maxprio > Prio)
                    return;
                lastLooked = Time.time;
                if (zVisitedManager.GetUnexploredLocation(Bot.Agent.Position, 0, 30) == null) //TODO this is very perf intensive when not finding anything.
                    return;
                if (bestAction == null || Prio > bestAction.Prio)
                {
                    bestAction = this;
                }
            }
            public override void OnQueued()
            {
                ZiMain.log.LogWarning("Hello Explore has been queued." + Bot.Agent.PlayerName);
                base.OnQueued();
            }
            public override PlayerBotActionBase CreateAction()
            {
                return new ExploreAction(this);
            }

        }
        public ExploreAction() : base(ClassInjector.DerivedConstructorPointer<CustomActionBase>())
        {//Don't use!
            ClassInjector.DerivedConstructorBody(this);
            
        }
        public ExploreAction(IntPtr ptr) : base(ptr)
        {//Don't use!
            ClassInjector.DerivedConstructorBody(this);
        }
        public ExploreAction(Descriptor desc) : base(desc)
        {// Use this.
            //ZiMain.sendChatMessage("Here I go exploring because I feel like it.",m_bot.Agent);
            state = StateEnum.lookingForUnexplored;
        }
        public override bool Update()
        {
            base.Update();
            if (!GetExplorePerm(m_bot) && !DescBase.IsCompleted())
            {
                Stop();
            }
            if (state == StateEnum.lookingForUnexplored)
            {
                if (UnexploredNode == null)
                {
                    UnexploredNode = zVisitedManager.GetUnexploredLocation(m_bot.Agent.Position);
                    if (UnexploredNode == null)
                    {
                        DescBase.SetCompletionStatus(PlayerBotActionBase.Descriptor.StatusType.Successful);
                        state = StateEnum.Finished;
                        return false;
                    }
                    state = StateEnum.Idle;
                    return false;
                }
                state = StateEnum.Idle;
                return false;
            }
            else if (state == StateEnum.Idle)
            {
                if (travelAction == null || travelAction.IsTerminated())
                {
                    PlayerAgent agent = m_bot.Agent;
                    Vector3 Unexplored = UnexploredNode.position;
                    travelAction = new(m_bot)
                    {
                        DestinationPos = Unexplored,
                        Haste = 0.5f,
                        WalkPosture = PlayerBotActionWalk.Descriptor.PostureEnum.None,
                        Radius = 0.5f,
                        DestinationType = PlayerBotActionTravel.Descriptor.DestinationEnum.Position,
                        Persistent = false,
                        ParentActionBase = this,
                        Prio = 3,
                    };
                    m_bot.StartAction(travelAction);
                    FlexibleMethodDefinition callback = new(OnTravelActionEvent, [travelAction]);
                    zActionSub.addOnTerminated(travelAction, callback);
                    state = StateEnum.Moving;
                    return false;
                }
                state = StateEnum.Moving;
                return !IsActive(); //Waiting for travel action to finish.
            }
            else if (state == StateEnum.Finished)
            {
                if (travelAction.Status == PlayerBotActionBase.Descriptor.StatusType.Successful)
                    ZiMain.sendChatMessage("I have looked everywhere!");
                if (travelAction.Status == PlayerBotActionBase.Descriptor.StatusType.Active)
                    ZiMain.log.LogWarning("Travel action still active somehow.");
                DescBase.SetCompletionStatus(travelAction.Status);
                Stop();
                return true;
            }
            else if (state == StateEnum.Moving) 
            {
                if (HasFoundEnemies())
                {
                    m_bot.StopAction(travelAction);
                    state = StateEnum.Finished;
                    return false;
                }
                if (UnexploredNode != null && UnexploredNode.discovered)
                {
                    m_bot.StopAction(travelAction);
                    state = StateEnum.lookingForUnexplored;
                    return false;
                }
            }
            return !IsActive();
        }
        private bool HasFoundEnemies()
        {
            Vector3 pos1 = m_bot.transform.position;
            Vector3 pos2 = m_bot.m_rootAction.Cast<RootPlayerBotAction.Descriptor>().ActionBase.Cast<RootPlayerBotAction>().m_followLeaderAction.Cast<PlayerBotActionFollow.Descriptor>().Client.transform.position;
            if (HasFoundEnemies(pos1) || HasFoundEnemies(pos2))
                return true;
            return false;
        }
        private static bool HasFoundEnemies(Vector3 pos)
        {
            int cellX = Mathf.FloorToInt(pos.x / zSearch.MapCellSise);
            int cellZ = Mathf.FloorToInt(pos.z / zSearch.MapCellSise);

            // Number of cells to cover foundDistance
            int range = Mathf.CeilToInt(zSearch.foundDistance * 5 / zSearch.MapCellSise);

            for (int dx = -range; dx <= range; dx++)
            {
                for (int dz = -range; dz <= range; dz++)
                {
                    Vector2 cell = new Vector2(cellX + dx, cellZ + dz);

                    if (!zSearch.findbleObjectMap.TryGetValue(cell, out var findables))
                        continue;

                    foreach (var findable in findables)
                    {
                        if (findable.found == true && findable.type == typeof(EnemyAgent))
                            return true;
                    }
                }
            }
            return false;
        }
        public void OnTravelActionEvent(PlayerBotActionBase.Descriptor descBase)
        {
            travelAction = (PlayerBotActionTravel.Descriptor)descBase;
            UnexploredNode = null;
            if (travelAction.Status == PlayerBotActionBase.Descriptor.StatusType.Successful)
            {
                state = StateEnum.lookingForUnexplored;
            }
            else if (travelAction.IsTerminated())
            {
                state = StateEnum.Finished;
            }
        }
        public override void Stop()
        {
            base.Stop();
            if (travelAction != null && !travelAction.IsTerminated())
                m_bot.StopAction(travelAction);
            state = StateEnum.Finished;
            DescBase.SetCompletionStatus(PlayerBotActionBase.Descriptor.StatusType.Stopped);
        }
        public enum StateEnum
        {
            None,
            Finished,
            lookingForUnexplored,
            Moving,
            Idle,
        }
    }
}

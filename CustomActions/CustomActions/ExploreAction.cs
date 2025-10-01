using Il2CppInterop.Runtime.Injection;
using Player;
using System;
using UnityEngine;
using Zombified_Initiative;

namespace ZombieTweak2.zRootBotPlayerAction.CustomActions
{
    internal class ExploreAction : CustomActionBase
    {
        private StateEnum state = StateEnum.None;
        VisitNode UnexploredNode = null;
        public PlayerBotActionTravel.Descriptor travelAction = null;
        public new class Descriptor : CustomActionBase.Descriptor
        {
            public new int Prio = 5;
            float lastLooked = 0;
            float lookCooldown = 5;
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
            public override void compareAction(ref PlayerBotActionBase.Descriptor bestAction)
            {
                if (lastLooked == 0)
                    lastLooked = Time.time;
                if (DramaManager.CurrentStateEnum != DRAMA_State.Exploration)
                    return;
                if (Time.time - lastLooked < lookCooldown)
                    return;
                if (zVisitedManager.GetUnexploredLocation(Bot.Agent.Position, 0, 5) != null && IsTerminated())
                {
                    if (bestAction == null || Prio > bestAction.Prio)
                    {
                        bestAction = this;
                        lastLooked = Time.time;
                    }
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
            ZiMain.sendChatMessage("Here I go exploring because I feel like it.",m_bot.Agent);
            m_bot.Actions[0].Cast<RootPlayerBotAction>().m_followLeaderAction.Prio = 4;
            state = StateEnum.lookingForUnexplored;
        }
        public override bool Update()
        {
            base.Update();
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
                        Prio = 5,
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
            else if (state == StateEnum.Moving)
            {
                if (UnexploredNode != null && UnexploredNode.discovered)
                {
                    m_bot.StopAction(travelAction);
                    state = StateEnum.lookingForUnexplored;
                    return false;
                }
            }
            else if (state == StateEnum.Finished)
            {
                if (travelAction.Status == PlayerBotActionBase.Descriptor.StatusType.Successful)
                    ZiMain.sendChatMessage("I have looked everywhere!");
                DescBase.SetCompletionStatus(travelAction.Status);
                Stop();
                return true;
            }
            return !IsActive(); //state moving
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
            m_bot.Actions[0].Cast<RootPlayerBotAction>().m_followLeaderAction.Prio = 14;
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

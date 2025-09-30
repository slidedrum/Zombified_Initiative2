using Il2CppInterop.Runtime.Injection;
using Player;
using System;
using UnityEngine;
using UnityEngine.UIElements;
using Zombified_Initiative;

namespace ZombieTweak2.zRootBotPlayerAction.CustomActions
{
    internal class zPlayerBotActionExplore : CustomActionBase
    {
        public new class Descriptor : CustomActionBase.Descriptor
        {
            public int Prio = 5;
            VisitNode unexploredNode = null;
            float timeStarted = 0;
            float ignoreTime = 5;
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
            public new void compareAction(ref PlayerBotActionBase.Descriptor bestAction)
            {
                if (timeStarted == 0)
                    timeStarted = Time.time;
                if (DramaManager.CurrentStateEnum != DRAMA_State.Exploration)
                    return;
                if (Time.time - timeStarted < ignoreTime)
                    return;
                unexploredNode = zVisitedManager.GetUnexploredLocation(Bot.Agent.Position);

                if (unexploredNode != null && IsTerminated())
                //if (IsTerminated())
                {
                    if (bestAction == null || Prio > bestAction.Prio)
                    {
                        var thisAction = this;
                        bestAction = thisAction;
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
                return new zPlayerBotActionExplore(this);
            }
        }
        public zPlayerBotActionExplore() : base(ClassInjector.DerivedConstructorPointer<CustomActionBase>())
        {
            ClassInjector.DerivedConstructorBody(this);
            //Don't use
        }
        public zPlayerBotActionExplore(IntPtr ptr) : base(ptr)
        {
            ClassInjector.DerivedConstructorBody(this);
            //Don't use
        }
        public zPlayerBotActionExplore(Descriptor desc) : base(desc)
        {
            //Use this
        }
        public override bool Update()
        {
            PrintLog($"Hello I am about to explore! {m_bot.Agent.PlayerName}");
            ZiMain.log.LogWarning("Hello I am also about to explore! " + m_bot.Agent.PlayerName);
            base.Update();
            return !IsActive();
        }
        public override void Stop()
        {
            base.Stop();
            this.m_bot.Actions[0].Cast<RootPlayerBotAction>().m_followLeaderAction.Prio = 14;
        }
        public enum StateEnum
        {
            Idle,
            lookingForUnexplored,
            Moving,
        }
    }
}

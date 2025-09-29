using Player;
using UnityEngine;
using Zombified_Initiative;

namespace ZombieTweak2.zRootBotPlayerAction.CustomActions
{
    internal class zPlayerBotActionExplore : CustomBotAction, ICustomPlayerBotActionBase
    {
        // You might want to keep a refrence to any potential sub actions here.
        // private PlayerBotActionTravel.Descriptor m_travelAction;
        public new class Descriptor : CustomBotAction.Descriptor, ICustomPlayerBotActionBase.IDescriptor
        {
            public int Prio = 5;
            //VisitNode unexploredNode = null;
            float timeStarted = 0;
            float ignoreTime = 5;
            public Descriptor(PlayerAIBot bot) : base(bot) 
            {
            }
            public override void compareAction(RootPlayerBotAction root, ref PlayerBotActionBase.Descriptor bestAction)
            {
                if (timeStarted == 0)
                    timeStarted = Time.time;
                if (DramaManager.CurrentStateEnum != DRAMA_State.Exploration)
                    return;
                if (Time.time - timeStarted < ignoreTime)
                    return;
                //unexploredNode = zVisitedManager.GetUnexploredLocation(Bot.Agent.Position);

                //if (unexploredNode != null && IsTerminated())
                if (IsTerminated())
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
        public zPlayerBotActionExplore(Descriptor desc) : base(desc)
        {
            desc.m_customBase = this;
            m_customDesc = desc;
            // Initialize your custom action here
        }
        public override bool Update()
        {
            PrintLog($"Hello I am about to explore! {m_bot.Agent.PlayerName}");
            ZiMain.log.LogWarning("Hello I am also about to explore! " + m_bot.Agent.PlayerName);
            base.Update();
            // Implement your custom update logic here
            // Usualy we verify the state of the action, like the target is still valid, etc.
            // Then we run a switch statement based on StateEnum, and call a different update method for each state.
            // Be sure to call this.m_desc.SetCompletionStatus when you're done, or if anything goes wrong.
            // You can call sub actions if you want like PlayerBotActionLook or PlayerBotActionTravel.
            // Almost all actions call sub actions.  Just create a descriptor for them and call this.m_bot.RequestAction(descriptor).
            return !IsActive();
        }

        // If you need any cleanup be sure to overide Stop, OnAborted, OnStopped, OnExpired and OnInterrupted.
         
        public override void Stop()
        {
            base.Stop();
            this.m_bot.Actions[0].Cast<RootPlayerBotAction>().m_followLeaderAction.Prio = 14;
            // Be sure to call SafeStop here on any sub actions you're running.
        }

        // You probably want a state enum like this
        public enum StateEnum
        {
            Idle,
            lookingForUnexplored,
            Moving,
        }

    }
}

using MS.Internal.Xml.XPath;
using Player;
using System.Collections.Generic;
using Zombified_Initiative;

namespace ZombieTweak2.zRootBotPlayerAction.CustomActions
{
    internal class zPlayerBotActionExplore : CustomBotAction, ICustomPlayerBotActionBase
    {
        // You might want to keep a refrence to any potential sub actions here.
        // private PlayerBotActionTravel.Descriptor m_travelAction;
        public new class Descriptor(PlayerAIBot bot) : CustomBotAction.Descriptor(bot), ICustomPlayerBotActionBase.IDescriptor
        {
            public override ICustomPlayerBotActionBase m_customBase { get ; set ; } // this is a typted refrnece to the base action
            public int prio = 5;

            public override void compareAction(RootPlayerBotAction root, ref PlayerBotActionBase.Descriptor bestAction)
            {
                if (DramaManager.CurrentStateEnum != DRAMA_State.Exploration)
                    return;

                VisitNode unexplored = null;
                unexplored = zVisitedManager.GetUnexploredLocation(Bot.Agent.Position);
                
                if (unexplored != null)
                {
                    root.m_followLeaderAction.Prio = 4;
                    if (bestAction == null || prio > bestAction.Prio)
                        bestAction = this;
                    root.m_followLeaderAction.Prio = 14;
                }
                
            }

            public override CustomBotAction CreateAction()
            {
                return new zPlayerBotActionExplore(this);
            }
            public override void Register(OrderedSet<ICustomPlayerBotActionBase.IDescriptor> allActions)
            {
                allActions.Add(this);
            }
            public new void OnStarted()
            {
                ZiMain.log.LogWarning("Hello I am starting to explore! " + Bot.Agent.PlayerName);
                this.Bot.Actions[0].Cast<RootPlayerBotAction>().m_followLeaderAction.Prio = 4;
            }
        }
        public zPlayerBotActionExplore(Descriptor desc) : base(desc)
        {
            desc.m_customBase = this;
            // Initialize your custom action here
        }
        public override bool Update()
        {
            base.Update();
            PrintLog($"Hello I am about to explore! {descriptor.Bot.Agent.PlayerName}");
            ZiMain.log.LogWarning("Hello I am also about to explore! " + descriptor.Bot.Agent.PlayerName);
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

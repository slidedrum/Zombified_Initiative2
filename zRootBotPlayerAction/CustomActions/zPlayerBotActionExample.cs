using Player;
using System.Collections.Generic;
using Zombified_Initiative;

namespace ZombieTweak2.zRootBotPlayerAction.CustomActions
{
    internal class zPlayerBotActionExample : CustomBotAction//, ICustomPlayerBotActionBase
    {
        // You might want to keep a refrence to any potential sub actions here.
        // private PlayerBotActionTravel.Descriptor m_travelAction;
        public new class Descriptor(PlayerAIBot bot) : CustomBotAction.Descriptor(bot)
        {
            public override void compareAction(ref PlayerBotActionBase.Descriptor bestAction)
            {
                // When should this action run?
                // Implement your custom comparison logic here
                // This should check if your action should be activated.  if it should, set bestAction to this.
                // Be sure to check the prio of the current best action before overwriting it.
            }

            public override CustomBotAction CreateAction()
            {
                // this must return an instance of your custom action
                return new zPlayerBotActionExample(this);
            }
        }
        public zPlayerBotActionExample(Descriptor desc) : base(desc)
        {
            // Initialize your custom action here
        }
        public override bool Update()
        {
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
            // Be sure to call SafeStop here on any sub actions you're running.
        }

        // You probably want a state enum like this
        public enum StateEnum
        {
            Idle,
            Running,
            Finishing,
        }

    }
}

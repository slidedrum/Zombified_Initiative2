using Player;

namespace ZombieTweak2.zRootBotPlayerAction.BaseActionWrappers
{
    internal class zPlayerBotActionUnlock : PlayerBotActionUnlock, ICustomPlayerBotActionBase
    {
        public Descriptor descriptor;
        public new class Descriptor : PlayerBotActionUnlock.Descriptor, ICustomPlayerBotActionBase.IDescriptor
        {
            public Descriptor(PlayerAIBot bot) : base(bot)
            {
                m_customBase = new zPlayerBotActionUnlock(this);
            }
            public ICustomPlayerBotActionBase m_customBase { get; set; }
            public override PlayerBotActionBase CreateAction()
            {
                var action = new zPlayerBotActionUnlock(this);
                m_customBase = action;
                return action;
            }
        }
        public zPlayerBotActionUnlock(Descriptor desc) : base(desc)
        {
            desc.m_customBase = this;
            descriptor = desc;
            m_desc = m_descBase as Descriptor;
        }
        public void compareAction(RootPlayerBotAction root, ref PlayerBotActionBase.Descriptor bestAction)
        {
            root.UpdateActionUnlock(ref bestAction);
        }
    }
}

using Player;

namespace ZombieTweak2.zRootBotPlayerAction.BaseActionWrappers
{
    internal class zPlayerBotActionUnlock : PlayerBotActionUnlock//, ICustomPlayerBotActionBase
    {
        public Descriptor m_customDesc { get; set; }

        public new class Descriptor : PlayerBotActionUnlock.Descriptor//, ICustomPlayerBotActionBase.IDescriptor
        {
            public Descriptor(PlayerAIBot bot) : base(bot)
            {
                //m_customBase = new zPlayerBotActionUnlock(this);
            }
            public zPlayerBotActionUnlock m_customBase { get; set; }

            public override zPlayerBotActionUnlock CreateAction()
            {
                return new zPlayerBotActionUnlock(this);
            }
            public void compareAction(RootPlayerBotAction root, ref PlayerBotActionBase.Descriptor bestAction)
            {
                root.UpdateActionUnlock(ref bestAction);
            }
        }
        public zPlayerBotActionUnlock(Descriptor desc) : base(desc)
        {
            desc.m_customBase = this;
            m_customDesc = desc;
        }

    }
}

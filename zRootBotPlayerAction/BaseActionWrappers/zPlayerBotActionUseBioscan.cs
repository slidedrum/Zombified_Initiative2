using Player;

namespace ZombieTweak2.zRootBotPlayerAction.BaseActionWrappers
{
    internal class zPlayerBotActionUseBioscan : PlayerBotActionUseBioscan, ICustomPlayerBotActionBase
    {
        public ICustomPlayerBotActionBase.IDescriptor m_customDesc { get; set; }
        public new class Descriptor : PlayerBotActionUseBioscan.Descriptor, ICustomPlayerBotActionBase.IDescriptor
        {
            public Descriptor(PlayerAIBot bot) : base(bot)
            {
                //m_customBase = new zPlayerBotActionUseBioscan(this);
            }
            public ICustomPlayerBotActionBase m_customBase { get; set; }
            public override PlayerBotActionBase CreateAction()
            {
                return new zPlayerBotActionUseBioscan(this);
            }
            public void compareAction(RootPlayerBotAction root, ref PlayerBotActionBase.Descriptor bestAction)
            {
                root.UpdateActionUseBioscan(ref bestAction);
            }

        }
        public zPlayerBotActionUseBioscan(Descriptor desc) : base(desc)
        {
            desc.m_customBase = this;
            m_customDesc = desc;
        }

    }
}

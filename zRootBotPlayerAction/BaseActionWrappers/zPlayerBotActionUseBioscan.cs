using Player;

namespace ZombieTweak2.zRootBotPlayerAction.BaseActionWrappers
{
    internal class zPlayerBotActionUseBioscan : PlayerBotActionUseBioscan, ICustomPlayerBotActionBase
    {
        public Descriptor descriptor;
        public new class Descriptor : PlayerBotActionUseBioscan.Descriptor, ICustomPlayerBotActionBase.IDescriptor
        {
            public Descriptor(PlayerAIBot bot) : base(bot)
            {
                //m_customBase = new zPlayerBotActionUseBioscan(this);
            }
            public ICustomPlayerBotActionBase m_customBase { get; set; }
            public override zPlayerBotActionUseBioscan CreateAction()
            {
                return new zPlayerBotActionUseBioscan(this);
            }
        }
        public zPlayerBotActionUseBioscan(Descriptor desc) : base(desc)
        {
            desc.m_customBase = this;
            descriptor = desc;
            m_desc = m_descBase as Descriptor;
        }
        public void compareAction(RootPlayerBotAction root, ref PlayerBotActionBase.Descriptor bestAction)
        {
            root.UpdateActionUseBioscan(ref bestAction);
        }
    }
}

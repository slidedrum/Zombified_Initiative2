using Player;

namespace ZombieTweak2.zRootBotPlayerAction.BaseActionWrappers
{
    internal class zPlayerBotActionHighlight : PlayerBotActionHighlight, ICustomPlayerBotActionBase
    {
        public Descriptor m_customDesc { get; set; }
        public new class Descriptor : PlayerBotActionHighlight.Descriptor, ICustomPlayerBotActionBase.IDescriptor
        {
            public Descriptor(PlayerAIBot bot) : base(bot)
            {
                //m_customBase = new zPlayerBotActionHighlight(this);
            }
            public zPlayerBotActionHighlight m_customBase { get; set; }
            public override zPlayerBotActionHighlight CreateAction()
            {
                return new zPlayerBotActionHighlight(this); ;
            }
            public void compareAction(RootPlayerBotAction root, ref PlayerBotActionBase.Descriptor bestAction)
            {
                root.UpdateActionHighlight(ref bestAction);
            }
        }
        public zPlayerBotActionHighlight(Descriptor desc) : base(desc)
        {
            desc.m_customBase = this;
            m_customDesc = desc;
        }


    }
}

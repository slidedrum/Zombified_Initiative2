using Player;

namespace ZombieTweak2
{
    internal class zPlayerBotActionIdle : PlayerBotActionIdle, ICustomPlayerBotActionBase
    {
        public Descriptor descriptor;
        public new class Descriptor : PlayerBotActionIdle.Descriptor, ICustomPlayerBotActionBase.IDescriptor
        {
            public Descriptor(PlayerAIBot bot) : base(bot) 
            {
                //m_customBase = new zPlayerBotActionIdle(this);
            }
            public ICustomPlayerBotActionBase m_customBase { get; set; }
            public override PlayerBotActionBase CreateAction()
            {
                var action = new zPlayerBotActionIdle(this);
                m_customBase = action;
                return action;
            }
        }
        public zPlayerBotActionIdle(Descriptor desc) : base(desc)
        {
            desc.m_customBase = this;
            descriptor = desc;
            m_desc = m_descBase as Descriptor;
        }

        public void compareAction(RootPlayerBotAction root, ref PlayerBotActionBase.Descriptor bestAction)
        {
            root.UpdateActionIdle(ref bestAction);
        }
    }

}

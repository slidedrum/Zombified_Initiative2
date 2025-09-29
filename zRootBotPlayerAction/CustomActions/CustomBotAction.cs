using Player;

namespace ZombieTweak2.zRootBotPlayerAction.CustomActions
{
    public abstract class CustomBotAction : PlayerBotActionBase, ICustomPlayerBotActionBase
    {
        public Descriptor m_customDesc { get ; set; }

        public new abstract class Descriptor(PlayerAIBot bot) : PlayerBotActionBase.Descriptor(bot), ICustomPlayerBotActionBase.IDescriptor
        {
            public CustomBotAction m_customBase { get; set; }
            public abstract void compareAction(RootPlayerBotAction root, ref PlayerBotActionBase.Descriptor bestAction);
            public abstract override PlayerBotActionBase CreateAction();
        }
        public CustomBotAction(Descriptor desc) : base(desc)
        {
            desc.m_customBase = this;
            m_customDesc = desc;
        }
    }
}

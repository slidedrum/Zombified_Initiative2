using Player;

namespace ZombieTweak2.zRootBotPlayerAction.BaseActionWrappers
{
    internal class zPlayerBotActionRevive : PlayerBotActionRevive, ICustomPlayerBotActionBase
    {
        public Descriptor descriptor;
        public new class Descriptor : PlayerBotActionRevive.Descriptor, ICustomPlayerBotActionBase.IDescriptor
        {
            public Descriptor(PlayerAIBot bot) : base(bot)
            {
                m_customBase = new zPlayerBotActionRevive(this);
            }
            public ICustomPlayerBotActionBase m_customBase { get; set; }
            public override PlayerBotActionBase CreateAction()
            {
                var action = new zPlayerBotActionRevive(this);
                m_customBase = action;
                return action;
            }
        }
        public zPlayerBotActionRevive(Descriptor desc) : base(desc)
        {
            desc.m_customBase = this;
            descriptor = desc;
            m_desc = m_descBase as Descriptor;
        }

        public void compareAction(RootPlayerBotAction root, ref PlayerBotActionBase.Descriptor bestAction)
        {
            root.UpdateActionReviveTeammate(ref bestAction);
        }
    }
}

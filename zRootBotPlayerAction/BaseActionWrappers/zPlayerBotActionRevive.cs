using Player;

namespace ZombieTweak2.zRootBotPlayerAction.BaseActionWrappers
{
    internal class zPlayerBotActionRevive : PlayerBotActionRevive, ICustomPlayerBotActionBase
    {
        public Descriptor m_customDesc { get; set; }
        public new class Descriptor : PlayerBotActionRevive.Descriptor, ICustomPlayerBotActionBase.IDescriptor
        {
            public Descriptor(PlayerAIBot bot) : base(bot)
            {
                //m_customBase = new zPlayerBotActionRevive(this);
            }
            public zPlayerBotActionRevive m_customBase { get; set; }
            public override zPlayerBotActionRevive CreateAction()
            {
                return new zPlayerBotActionRevive(this);
            }
            public void compareAction(RootPlayerBotAction root, ref PlayerBotActionBase.Descriptor bestAction)
            {
                root.UpdateActionReviveTeammate(ref bestAction);
            }
        }
        public zPlayerBotActionRevive(Descriptor desc) : base(desc)
        {
            desc.m_customBase = this;
            m_customDesc = desc;
        }


    }
}

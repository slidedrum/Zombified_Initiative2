using Player;

namespace ZombieTweak2
{
    internal class zPlayerBotActionFollow : PlayerBotActionFollow, ICustomPlayerBotActionBase
    {
        public ICustomPlayerBotActionBase.IDescriptor m_customDesc { get; set; }

        public new class Descriptor : PlayerBotActionFollow.Descriptor, ICustomPlayerBotActionBase.IDescriptor
        {
            public ICustomPlayerBotActionBase m_customBase { get; set; }
            public Descriptor(PlayerAIBot bot) : base(bot)
            {
                
            }
            public override zPlayerBotActionFollow CreateAction()
            {
                return new zPlayerBotActionFollow(this);
            }
            public void compareAction(RootPlayerBotAction root, ref PlayerBotActionBase.Descriptor bestAction)
            {
                root.UpdateActionFollowPlayer(ref bestAction);
            }
        }
        public zPlayerBotActionFollow(Descriptor desc) : base(desc)
        {
            desc.m_customBase = this;
            m_customDesc = desc;
        }


    }

}

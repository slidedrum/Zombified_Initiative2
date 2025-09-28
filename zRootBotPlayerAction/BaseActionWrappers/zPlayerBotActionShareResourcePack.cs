using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZombieTweak2.zRootBotPlayerAction.BaseActionWrappers
{
    internal class zPlayerBotActionShareResourcePack : PlayerBotActionShareResourcePack, ICustomPlayerBotActionBase
    {
        public ICustomPlayerBotActionBase.IDescriptor m_customDesc { get; set; }
        public new class Descriptor : PlayerBotActionShareResourcePack.Descriptor, ICustomPlayerBotActionBase.IDescriptor
        {
            public Descriptor(PlayerAIBot bot) : base(bot)
            {
                //m_customBase = new zPlayerBotActionShareResourcePack(this);
            }
            public ICustomPlayerBotActionBase m_customBase { get; set; }
            public override zPlayerBotActionShareResourcePack CreateAction()
            {
                return new zPlayerBotActionShareResourcePack(this);
            }
            public void compareAction(RootPlayerBotAction root, ref PlayerBotActionBase.Descriptor bestAction)
            {
                root.UpdateActionShareResoursePack(ref bestAction);
            }
        }
        public zPlayerBotActionShareResourcePack(Descriptor desc) : base(desc)
        {
            desc.m_customBase = this;
            m_customDesc = desc;
        }


    }
}

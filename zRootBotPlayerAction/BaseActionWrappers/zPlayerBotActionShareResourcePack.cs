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
        public Descriptor descriptor;
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
        }
        public zPlayerBotActionShareResourcePack(Descriptor desc) : base(desc)
        {
            desc.m_customBase = this;
            descriptor = desc;
            m_desc = m_descBase as Descriptor;
        }

        public void compareAction(RootPlayerBotAction root, ref PlayerBotActionBase.Descriptor bestAction)
        {
            root.UpdateActionShareResoursePack(ref bestAction);
        }
    }
}

using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZombieTweak2.zRootBotPlayerAction.BaseActionWrappers
{
    internal class zPlayerBotActionAttack : PlayerBotActionAttack//, ICustomPlayerBotActionBase
    {
        public Descriptor m_customDesc { get ; set; }

        public new class Descriptor : PlayerBotActionAttack.Descriptor//, ICustomPlayerBotActionBase.IDescriptor
        {
            public zPlayerBotActionAttack m_customBase { get; set; }
            public Descriptor(PlayerAIBot bot) : base(bot)
            {
                //m_customBase = new zPlayerBotActionAttack(this);
            }
            public override zPlayerBotActionAttack CreateAction()
            {
                return new zPlayerBotActionAttack(this);
            }
            public void compareAction(RootPlayerBotAction root, ref PlayerBotActionBase.Descriptor bestAction)
            {
                root.UpdateActionAttack(ref bestAction);
            }
        }
        public zPlayerBotActionAttack(Descriptor desc) : base(desc)
        {
            desc.m_customBase = this;
            m_customDesc = desc;
        }
    }
}

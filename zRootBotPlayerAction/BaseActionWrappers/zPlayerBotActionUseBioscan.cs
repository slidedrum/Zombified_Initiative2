using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZombieTweak2.zRootBotPlayerAction.BaseActionWrappers
{
    internal class zPlayerBotActionUseBioscan : PlayerBotActionUseBioscan, ICustomPlayerBotActionBase
    {
        public new class Descriptor : PlayerBotActionUseBioscan.Descriptor, ICustomPlayerBotActionBase.IDescriptor
        {
            public Descriptor(PlayerAIBot bot) : base(bot)
            {
                m_customBase = new zPlayerBotActionUseBioscan(this);
            }
            public ICustomPlayerBotActionBase m_customBase { get; set; }
            public override PlayerBotActionBase CreateAction()
            {
                var action = new zPlayerBotActionUseBioscan(this);
                m_customBase = action;
                return action;
            }
        }
        public zPlayerBotActionUseBioscan(Descriptor desc) : base(desc)
        {
            desc.m_customBase = this;
            m_desc = m_descBase as PlayerBotActionUseBioscan.Descriptor;
        }
        public void compareAction(RootPlayerBotAction root, ref PlayerBotActionBase.Descriptor bestAction)
        {
            root.UpdateActionUseBioscan(ref bestAction);
        }
    }
}

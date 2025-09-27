using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZombieTweak2.zRootBotPlayerAction.BaseActionWrappers
{
    internal class zPlayerBotActionCollectItem : PlayerBotActionCollectItem, ICustomPlayerBotActionBase
    {
        public Descriptor descriptor;
        public new class Descriptor : PlayerBotActionCollectItem.Descriptor, ICustomPlayerBotActionBase.IDescriptor
        {
            public ICustomPlayerBotActionBase m_customBase { get; set; }
            public Descriptor(PlayerAIBot bot) : base(bot)
            {
                m_customBase = new zPlayerBotActionCollectItem(this);
            }
            public override PlayerBotActionBase CreateAction()
            {
                return new zPlayerBotActionCollectItem(this);
            }
        }
        public zPlayerBotActionCollectItem(Descriptor desc) : base(desc)
        {
            desc.m_customBase = this;
            descriptor = desc;
            m_desc = m_descBase as Descriptor;
        }

        public void compareAction(RootPlayerBotAction root, ref PlayerBotActionBase.Descriptor bestAction)
        {
            root.UpdateActionCollectItem(ref bestAction);
        }
    }
}

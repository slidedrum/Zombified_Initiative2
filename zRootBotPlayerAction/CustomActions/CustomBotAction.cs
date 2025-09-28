using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZombieTweak2.zRootBotPlayerAction.CustomActions
{
    public abstract class CustomBotAction : PlayerBotActionBase, ICustomPlayerBotActionBase
    {
        public Descriptor descriptor;
        protected Descriptor m_desc;
        public new abstract class Descriptor(PlayerAIBot bot) : PlayerBotActionBase.Descriptor(bot), ICustomPlayerBotActionBase.IDescriptor
        {
            public abstract ICustomPlayerBotActionBase m_customBase { get; set; }

            public abstract void compareAction(RootPlayerBotAction root, ref PlayerBotActionBase.Descriptor bestAction);

            public abstract override PlayerBotActionBase CreateAction();
            public abstract void Register(OrderedSet<ICustomPlayerBotActionBase.IDescriptor> allActions);
        }
        public CustomBotAction(Descriptor desc) : base(desc)
        {
        }
    }
}

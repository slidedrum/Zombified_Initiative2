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
        public Descriptor m_desc; // Not sure if this is needed, but the base game has this.

        public new abstract class Descriptor(PlayerAIBot bot) : PlayerBotActionBase.Descriptor(bot), ICustomPlayerBotActionBase.IDescriptor
        {
            public abstract ICustomPlayerBotActionBase m_customBase { get; set; }

            public abstract void compareAction(RootPlayerBotAction root, ref PlayerBotActionBase.Descriptor bestAction);
            public abstract override CustomBotAction CreateAction();
            public abstract void Register(OrderedSet<ICustomPlayerBotActionBase.IDescriptor> allActions);
        }
        public CustomBotAction(Descriptor desc) : base(desc)
        {
            desc.ActionBase = this;
            m_bot = desc.Bot;
            m_agent = m_bot.Agent;
            m_loco = m_agent.Locomotion;
            descriptor = desc;
            m_desc = m_descBase as Descriptor;
        }
    }
}

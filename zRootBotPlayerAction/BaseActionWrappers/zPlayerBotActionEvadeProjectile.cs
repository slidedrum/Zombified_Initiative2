using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZombieTweak2.zRootBotPlayerAction.BaseActionWrappers
{
    internal class zPlayerBotActionEvadeProjectile : PlayerBotActionEvadeProjectile
    {
        public Descriptor m_customDesc { get; set; }
        public new class Descriptor : PlayerBotActionEvadeProjectile.Descriptor, ICustomPlayerBotActionBase.IDescriptor
        {
            public zPlayerBotActionEvadeProjectile m_customBase { get; set; }
            public Descriptor(PlayerAIBot bot) : base(bot)
            {
                //m_customBase = new zPlayerBotActionEvadeProjectile(this);
            }
            public override zPlayerBotActionEvadeProjectile CreateAction()
            {
                return new zPlayerBotActionEvadeProjectile(this);
            }

            public void compareAction(RootPlayerBotAction root, ref PlayerBotActionBase.Descriptor bestAction)
            {
                root.UpdateActionFollowPlayer(ref bestAction);
            }
        }
        public zPlayerBotActionEvadeProjectile(Descriptor desc) : base(desc)
        {
            desc.m_customBase = this;
            m_customDesc = desc;
        }

    }
}

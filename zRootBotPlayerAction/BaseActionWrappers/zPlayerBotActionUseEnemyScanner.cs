using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZombieTweak2.zRootBotPlayerAction.BaseActionWrappers
{
    internal class zPlayerBotActionUseEnemyScanner : PlayerBotActionUseEnemyScanner//, ICustomPlayerBotActionBase
    {
        public Descriptor m_customDesc { get; set; }
        public new class Descriptor : PlayerBotActionUseEnemyScanner.Descriptor//, ICustomPlayerBotActionBase.IDescriptor
        {
            public Descriptor(PlayerAIBot bot) : base(bot)
            {
                //m_customBase = new zPlayerBotActionUseEnemyScanner(this);
            }
            public zPlayerBotActionUseEnemyScanner m_customBase { get; set; }
            public override zPlayerBotActionUseEnemyScanner CreateAction()
            {
                return new zPlayerBotActionUseEnemyScanner(this);
            }
            public void compareAction(RootPlayerBotAction root, ref PlayerBotActionBase.Descriptor bestAction)
            {
                root.UpdateActionUseEnemyScanner(ref bestAction);
            }
        }
        public zPlayerBotActionUseEnemyScanner(Descriptor desc) : base(desc)
        {
            desc.m_customBase = this;
            m_customDesc = desc;
        }

    }
}

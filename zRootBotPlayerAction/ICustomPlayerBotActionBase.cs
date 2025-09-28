using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZombieTweak2.zRootBotPlayerAction.CustomActions;

namespace ZombieTweak2
{
    public interface ICustomPlayerBotActionBase
    {
        public interface IDescriptor
        {
            ICustomPlayerBotActionBase m_customBase { get; set; }
            void compareAction(RootPlayerBotAction root, ref PlayerBotActionBase.Descriptor bestAction);
            PlayerBotActionBase CreateAction();
            void OnStarted();
        }
    }
}

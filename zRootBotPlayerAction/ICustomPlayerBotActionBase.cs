using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZombieTweak2.zRootBotPlayerAction;

namespace ZombieTweak2
{
    public interface ICustomPlayerBotActionBase
    {
        IDescriptor m_customDesc { get; set; }
        public interface IDescriptor
        {
            // You must also overide CreateAction in your descriptor to return your custom action type
            ICustomPlayerBotActionBase m_customBase { get; set; }
            void compareAction(RootPlayerBotAction root, ref PlayerBotActionBase.Descriptor bestAction);
            PlayerBotActionBase CreateAction();
            void OnStarted();
        }
        
    }
}

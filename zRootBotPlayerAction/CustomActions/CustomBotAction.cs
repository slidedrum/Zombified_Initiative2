//using Il2CppInterop.Runtime;
//using Il2CppInterop.Runtime.Injection;
//using Il2CppSystem;
//using Player;
//using UnityEngine;

//namespace ZombieTweak2.zRootBotPlayerAction.CustomActions
//{
//    public class CustomBotAction : PlayerBotActionBase//, ICustomPlayerBotActionBase
//    {
//        public new class Descriptor : PlayerBotActionBase.Descriptor//, ICustomPlayerBotActionBase.IDescriptor
//        {
//            public Descriptor() : base(ClassInjector.DerivedConstructorPointer<CustomActionBase>())
//            {
//                ClassInjector.DerivedConstructorBody(this);
//            }
//            public Descriptor(IntPtr ptr) : base(ptr)
//            {
//                ClassInjector.DerivedConstructorBody(this);
//            }
//            public Descriptor(PlayerAIBot bot) : base(bot)
//            {
//            }
//            public Descriptor() : base(new PlayerAIBot()) { }
//            public CustomBotAction m_customBase { get; set; }
//            public void compareAction(ref PlayerBotActionBase.Descriptor bestAction)
//            {

//            }
//            public override PlayerBotActionBase CreateAction()
//            {
//                return null;
//            }
//        }
//        public Descriptor m_customDesc { get; set; }
//        public CustomBotAction(Descriptor desc) : base(desc)
//        {
//            desc.m_customBase = this;
//            m_customDesc = desc;
//        }
//        public CustomBotAction() : base(new Descriptor())
//        {
//        }

//    }

//}

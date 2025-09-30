using Il2CppInterop.Runtime.Injection;
using Il2CppSystem.Security.Cryptography;
using Player;
using System;
using UnityEngine;
using ZombieTweak2;
using Zombified_Initiative;

public class TestAction : PlayerBotActionBase
{
    CustomBase _Base;
    public new class Descriptor : PlayerBotActionBase.Descriptor
    {
        CustomDescBase _Base = null;
        public Descriptor() : base(ClassInjector.DerivedConstructorPointer<Descriptor>())
        {
            ClassInjector.DerivedConstructorBody(this);
            //Don't use
        }
        public Descriptor(IntPtr ptr) : base(ptr)
        {
            ClassInjector.DerivedConstructorBody(this);
            //Don't use
        }
        public Descriptor(PlayerAIBot bot) : base(ClassInjector.DerivedConstructorPointer<Descriptor>())
        {
            ClassInjector.DerivedConstructorBody(this);
            _Base = new CustomDescBase(this);
            Bot = bot;
        }
        public override PlayerBotActionBase CreateAction()
        {
            return new TestAction(this);
        }
        public override bool IsActionAllowed(PlayerBotActionBase.Descriptor desc)
        {
            return _Base.IsActionAllowed(desc);
        }
        public override bool CheckCollision(PlayerBotActionBase.Descriptor desc)
        {
            Debug.Log("boop");
            return _Base.CheckCollision(desc);
        }
        public override void OnQueued()
        {
            _Base.OnQueued();
        }
        public override AccessLayers GetAccessLayersRuntime()
        {
            return _Base.GetAccessLayersRuntime();
        }
        public override void InternalOnTerminated()
        {
            _Base.InternalOnTerminated();
        }
        internal void compareAction(ref PlayerBotActionBase.Descriptor bestAction)
        {
        }

    }
    public TestAction() : base(ClassInjector.DerivedConstructorPointer<CustomActionBase>())
    {
        ClassInjector.DerivedConstructorBody(this);
        //Don't use
    }
    public TestAction(IntPtr ptr) : base(ptr) 
    {
        ClassInjector.DerivedConstructorBody(this);
        //Don't use
    }
    public TestAction(Descriptor desc) : base(ClassInjector.DerivedConstructorPointer<CustomActionBase>())
    {
        ClassInjector.DerivedConstructorBody(this);
        _Base = new CustomBase(this);
        desc.ActionBase = this;
        m_bot = desc.Bot;
        m_agent = m_bot.Agent;
        m_loco = m_agent.Locomotion;
        m_descBase = desc;
    }
    public override void Stop()
    {
        _Base.Stop();
    }
    public override bool Update()
    {
        return _Base.Update();
    }
    public override bool IsActionAllowed(PlayerBotActionBase.Descriptor desc)
    {
        return _Base.IsActionAllowed(desc);
    }
    public override bool CheckCollision(PlayerBotActionBase.Descriptor desc)
    {
        return _Base.CheckCollision(desc);
    }
    public override AccessLayers GetAccessLayersRuntime()
    {
        return _Base.GetAccessLayersRuntime();
    }
    public override void OnWarped(Vector3 position)
    {
        _Base.OnWarped(position);
    }
}

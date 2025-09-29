using Player;
using Il2CppInterop.Runtime.Injection;
using System;
using UnityEngine;

public class CustomActionBase : PlayerBotActionBase
{
    public new class Descriptor : PlayerBotActionBase.Descriptor
    {
        public int testInt = 7;
        public bool best = false;
        public Descriptor() : base(ClassInjector.DerivedConstructorPointer<Descriptor>())
        {
            ClassInjector.DerivedConstructorBody(this);
        }
        public Descriptor(IntPtr ptr) : base(ptr)
        {
            ClassInjector.DerivedConstructorBody(this);
        }
        public Descriptor(PlayerAIBot bot) : base(ClassInjector.DerivedConstructorPointer<Descriptor>())
        {
            ClassInjector.DerivedConstructorBody(this);
            Bot = bot;
        }
        public override PlayerBotActionBase CreateAction()
        {
            return new CustomActionBase(this);
        }
        public override bool IsActionAllowed(PlayerBotActionBase.Descriptor desc)
        {
            return false;
        }
        public override bool CheckCollision(PlayerBotActionBase.Descriptor desc)
        {
            return false;
        }
        public override void OnQueued()
        {

        }
        public override AccessLayers GetAccessLayersRuntime()
        {
            return AccessLayers.None;
        }
        public override void InternalOnTerminated()
        {

        }

        internal void compareAction(ref PlayerBotActionBase.Descriptor bestAction)
        {
            if (best)
                bestAction = this;
            return;
        }
    }
    public CustomActionBase() : base(ClassInjector.DerivedConstructorPointer<CustomActionBase>())
    {
        ClassInjector.DerivedConstructorBody(this);
    }
    public CustomActionBase(IntPtr ptr) : base(ptr) 
    {
        ClassInjector.DerivedConstructorBody(this);
    }
    public CustomActionBase(Descriptor desc) : base(ClassInjector.DerivedConstructorPointer<CustomActionBase>())
    {
        ClassInjector.DerivedConstructorBody(this);
        desc.ActionBase = this;
        m_bot = desc.Bot;
        m_agent = m_bot.Agent;
        m_loco = m_agent.Locomotion;
        m_descBase = desc;
    }
    public override void Stop()
    {

    }
    public override bool Update()
    {
        return false;
    }
    public override bool IsActionAllowed(PlayerBotActionBase.Descriptor desc)
    {
        return false;
    }
    public override bool CheckCollision(PlayerBotActionBase.Descriptor desc)
    {
        return false;
    }
    public override AccessLayers GetAccessLayersRuntime()
    {
        return AccessLayers.None;
    }
    public override void OnWarped(Vector3 position)
    {

    }
}

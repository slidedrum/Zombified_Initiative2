using Player;
using Il2CppInterop.Runtime.Injection;
using System;
using UnityEngine;
using Zombified_Initiative;

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
            return base.IsActionAllowed(desc);
        }
        public override bool CheckCollision(PlayerBotActionBase.Descriptor desc)
        {
            return base.CheckCollision(desc);
        }
        public override void OnQueued()
        {
            ZiMain.log.LogInfo("Holy shit custom action was queued!");
        }
        public override AccessLayers GetAccessLayersRuntime()
        {
            return base.GetAccessLayersRuntime();
        }
        public override void InternalOnTerminated()
        {
            base.InternalOnTerminated();
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
        base.Stop();
    }
    public override bool Update()
    {
        ZiMain.log.LogInfo("Holy shit custom action is updating!");
        return base.Update();
    }
    public override bool IsActionAllowed(PlayerBotActionBase.Descriptor desc)
    {
        return base.IsActionAllowed(desc);
    }
    public override bool CheckCollision(PlayerBotActionBase.Descriptor desc)
    {
        return base.CheckCollision(desc);
    }
    public override AccessLayers GetAccessLayersRuntime()
    {
        return base.GetAccessLayersRuntime();
    }
    public override void OnWarped(Vector3 position)
    {
        base.OnWarped(position);
    }
}

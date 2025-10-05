using Il2CppInterop.Runtime.Injection;
using Player;
using System;
using UnityEngine;

public class TemplateAction : CustomActionBase
{
    //This is an example of how you can set up your own custom action!
    public static new bool Setup() //This will be called when your class is regestered, it should return true if your action will even activate on it's own, or false if it's an exclusively manual action.
    {
        return true;
    }
    public new class Descriptor : CustomActionBase.Descriptor
    {
        //This is an example of how you can set up your own custom descriptor!
        public Descriptor() : base(ClassInjector.DerivedConstructorPointer<Descriptor>()) // Don't use this!  Needed for il2cpp nonsense.
        {
            ClassInjector.DerivedConstructorBody(this);
        } // Don't use this!  Needed for il2cpp nonsense.
        public Descriptor(IntPtr ptr) : base(ptr) // Don't use this!  Needed for il2cpp nonsense.
        {
            ClassInjector.DerivedConstructorBody(this);
        }  // Don't use this!  Needed for il2cpp nonsense.
        public Descriptor(PlayerAIBot bot) : base(bot)
        {
            //Use this is your descriptor constructor.
            //The descriptor is used to describe everything about your action.
            //Any paramaters are set up by the calling class.  
            //Be sure to add any you need to this class.
            //Some paramaters are inhareted, like Prio (priority). 
        }
        public override PlayerBotActionBase CreateAction()
        {
            //This converts your descriptor into an action instance.
            //This means your action is starting!
            //You probably won't need to do anything else here.
            return new CustomActionBase(this);
        }
        public override bool IsActionAllowed(PlayerBotActionBase.Descriptor desc)
        {
            //Does your action play nice with desc?
            return base.IsActionAllowed(desc);
        }
        public override bool CheckCollision(PlayerBotActionBase.Descriptor desc)
        {
            //Should this action abort if desc is active?
            return base.CheckCollision(desc);
        }
        public override void OnQueued()
        {
            //This gets called when your action is added to the que.
            base.OnQueued();
        }
        public override AccessLayers GetAccessLayersRuntime()
        {
            //A mostly simple getter method, tbh I don't really understand access layers yet.
            return base.GetAccessLayersRuntime();
        }
        public override void InternalOnTerminated()
        {
            //This gets called when your action is getting terminated.
            //This includes any form of interuption, but does not include finishing the action.
            base.InternalOnTerminated();
        }
        public virtual void CompareAction(ref PlayerBotActionBase.Descriptor bestAction)
        {
            //Should your action be queued?
            //This gets called every frame
            //Be sure to compare priority against the current best action.
            //Best action inludes vanilla actions.
            //Be sure to not set this to best action if it's already active.
        }

    }
    public TemplateAction() : base(ClassInjector.DerivedConstructorPointer<CustomActionBase>())// Don't use this!  Needed for il2cpp nonsense.
    {
        ClassInjector.DerivedConstructorBody(this);
        
    }// Don't use this!  Needed for il2cpp nonsense.
    public TemplateAction(IntPtr ptr) : base(ptr) // Don't use this!  Needed for il2cpp nonsense.
    {
        ClassInjector.DerivedConstructorBody(this);

    }// Don't use this!  Needed for il2cpp nonsense.
    public TemplateAction(Descriptor desc) : base(desc)
    {
        //Use this constructor.
        //This means your action is starting!
    }
    public override void Stop()
    {
        //This is called when your action is told to stop.
        //Be sure to do any cleanup if you need to.
        base.Stop();
    }
    public override bool Update()
    {
        //This is called every frame when your action is active.
        return base.Update();
    }
    public override bool IsActionAllowed(PlayerBotActionBase.Descriptor desc)
    {
        //This just calls the descriptor version of this method.
        //Not sure why this is virtual, but it is.
        return base.IsActionAllowed(desc);
    }
    public override bool CheckCollision(PlayerBotActionBase.Descriptor desc)
    {
        //This does NOT call the descriptor version of this method
        //This re-implements the exact same thing as the descriptor version.
        //Not sure why this is virtual, but it is.
        return base.CheckCollision(desc);
    }
    public override AccessLayers GetAccessLayersRuntime()
    {
        //This tries to call the descriptor version of this method.
        //falls back to RequiredLayers
        return base.GetAccessLayersRuntime();
    }
    public override void OnWarped(Vector3 position)
    {
        //Called when the bot is warped, duh.
        //This will set completion status to failed by deafult.
        base.OnWarped(position);
    }
}

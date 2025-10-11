using AIGraph;
using Enemies;
using Il2CppInterop.Runtime.Injection;
using System.Collections.Generic;
using Player;
using System;
using System.Linq;
using UnityEngine;
using ZombieTweak2;
using ZombieTweak2.zRootBotPlayerAction;
using ZombieTweak2.zRootBotPlayerAction.CustomActions;
using Zombified_Initiative;
using static ZombieTweak2.zVisibilityManager;
using Discord;

public class ClearRoomAction : CustomActionBase
{
    //This is an example of how you can set up your own custom action!
    public State state;
    private ExploreAction.Descriptor exploreAction;
    private Descriptor customDesc;
    private AIG_CourseNode courseNode;
    private List<EnemyAgent> allEnemiesInNode;
    private EnemyAgent target;
    private PlayerBotActionTravel.Descriptor travelAction;
    private PlayerBotActionAttack.Descriptor attackAction;

    public static new bool Setup() //This will be called when your class is regestered, it should return true if your action will even activate on it's own, or false if it's an exclusively manual action.
    {
        return false;
    }
    public new class Descriptor : CustomActionBase.Descriptor
    {
        public AIG_CourseNode courseNode;
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
            return new ClearRoomAction(this);
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
    public enum State
    {
        idle,
        lookingForEnemy,
        needsToExplore,
        Exploring,
        NeedsToMove,
        Moving,
        Killing,
        Finished,
    }
    public ClearRoomAction() : base(ClassInjector.DerivedConstructorPointer<CustomActionBase>())// Don't use this!  Needed for il2cpp nonsense.
    {
        ClassInjector.DerivedConstructorBody(this);
        
    }// Don't use this!  Needed for il2cpp nonsense.
    public ClearRoomAction(IntPtr ptr) : base(ptr) // Don't use this!  Needed for il2cpp nonsense.
    {
        ClassInjector.DerivedConstructorBody(this);

    }// Don't use this!  Needed for il2cpp nonsense.
    public ClearRoomAction(Descriptor desc) : base(desc)
    {
        zActions.manualActions.Add(desc);
        customDesc = desc;
        courseNode = customDesc.courseNode ?? m_bot.Agent.CourseNode;
        state = State.lookingForEnemy;
        //Use this constructor.
        //This means your action is starting!
    }
    public override void Stop()
    {
        //This is called when your action is told to stop.
        //Be sure to do any cleanup if you need to.
        base.Stop();
        m_bot.StopAction(attackAction);
        m_bot.StopAction(travelAction);
        m_bot.StopAction(exploreAction);
    }
    public override bool Update()
    {
        base.Update();
        //This is called every frame when your action is active.
        List<FindableObject> foundEnemiesInNode = new();
        if (zSearch.courseNodeFindableObjectCache.ContainsKey(courseNode.Name))
            foundEnemiesInNode = zSearch.courseNodeFindableObjectCache[courseNode.Name].Where(findable => findable.type == typeof(EnemyAgent) && findable.found == true).ToList(); //This might be expensive?
        switch (state)
        {
            case State.idle:
                break;
            case State.lookingForEnemy:
                if(foundEnemiesInNode.Count == 0) // We don't know about any enemies in this node
                {
                    if (zVisitedManager.HasCourseNodeBeenFullyExplored(courseNode)) // we have found everything in this coursenode
                    {
                        state = State.Finished; //We are done
                        break;
                    }
                    state = State.needsToExplore;
                    break;
                }
                FindableObject closestEnemy = null;
                float closestDistance = float.MaxValue;
                foreach (var enemy in foundEnemiesInNode)
                {
                    var distance = Vector3.Distance(enemy.gameObject.transform.position, m_bot.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestEnemy = enemy;
                    }
                }
                if (closestEnemy == null)
                {
                    ZiMain.log.LogError("Something went horribly wrong while clear room action was looking for an enemy.");
                }
                target = closestEnemy.gameObject.GetComponent<EnemyAgent>();
                state = State.NeedsToMove;
                break;
            case State.needsToExplore:
                exploreAction = new ExploreAction.Descriptor(m_bot)
                {
                    limiationsCheck = new(IsUnexploredNodeWithinCourseNode),
                    respectFollowDistance = false,
                    stopOnEnemyFound = false,
                };
                m_bot.StartAction(exploreAction);
                state = State.Exploring;
                break;
            case State.Exploring:
                if (foundEnemiesInNode.Count > 0)
                {
                    state = State.lookingForEnemy;
                    m_bot.StopAction(exploreAction);
                    break;
                }
                if (exploreAction.IsTerminated())
                {
                    if (!zVisitedManager.HasCourseNodeBeenFullyExplored(courseNode))
                    {
                        exploreAction = new ExploreAction.Descriptor(m_bot)
                        {
                            limiationsCheck = new(IsUnexploredNodeWithinCourseNode),
                            respectFollowDistance = false,
                            stopOnEnemyFound = false,
                        };
                        m_bot.StartAction(exploreAction);
                    }
                    else
                        state = State.Finished;
                }
                break;
            case State.NeedsToMove:
                if (target == null || target.gameObject == null)
                {
                    state = State.lookingForEnemy;
                    break;
                }
                travelAction = new PlayerBotActionTravel.Descriptor(m_bot)
                {
                    DestinationObject = target.gameObject,
                    DestinationType = PlayerBotActionTravel.Descriptor.DestinationEnum.GameObject,
                    Haste = 0.5f,
                    WalkPosture = PlayerBotActionWalk.Descriptor.PostureEnum.None,
                    Radius = 2f,
                    Persistent = false,
                    ParentActionBase = this,
                    Prio = DescBase.Prio,
                };
                m_bot.StartAction(travelAction);
                state = State.Moving;
                break;
            case State.Moving:
                if (target == null)
                {
                    state = State.lookingForEnemy;
                    break;
                }
                var distanceToTarget = Vector3.Distance(m_bot.transform.position, target.transform.position);
                if (distanceToTarget < 2f)
                {
                    m_bot.StopAction(travelAction);
                    attackAction = new PlayerBotActionAttack.Descriptor(m_bot)
                    {
                        Stance = PlayerBotActionAttack.StanceEnum.All,
                        Means = PlayerBotActionAttack.AttackMeansEnum.Melee,
                        Posture = PlayerBotActionWalk.Descriptor.PostureEnum.Crouch,
                        TargetAgent = target,
                        Prio = DescBase.Prio,
                        Haste = 1,
                    };
                    m_bot.StartAction(attackAction);
                    state = State.Killing;
                }
                break;
            case State.Killing:
                if (!attackAction.IsTerminated())
                    break;
                state = State.lookingForEnemy;
                break;
            case State.Finished:
                ZiMain.sendChatMessage("The room is clear!", m_bot.Agent);
                Stop();
                break;
            default:
                break;
        }
        return !IsActive(); 
    }
    public void IsUnexploredNodeWithinCourseNode(ExploreAction.Descriptor desc)
    {
        var actionBase = desc.exploreBase;
        var courseNode = actionBase.UnexploredNode.courseNode;
        var nodeCache = zVisitedManager.VisitNodeCourseNodeCache;
        actionBase.positionAllowed = false;
        if (nodeCache.ContainsKey(courseNode) && nodeCache[courseNode].Contains(actionBase.UnexploredNode))
        {
            actionBase.positionAllowed = true;
        }
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

using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ZombieTweak2.zRootBotPlayerAction.BaseActionWrappers
{
    internal class zRootPlayerBotAction : RootPlayerBotAction
    {
        public zRootPlayerBotAction(Descriptor desc) : base(desc)
        {
            var data = zActions.GetOrCreateData(desc);
            //hurt action is handled somewhere else?  Might need to find it?  Or maybe it's unused now?  Who knows.

            var m_idleAction = new PlayerBotActionIdle.Descriptor(this.m_bot); // Create the action wrapper
            m_idleAction.PrioFreezeForTwitcher = RootPlayerBotAction.m_prioSettings.IdleFreezeForTwitcher;
            m_idleAction.PrioFreezeForInteraction = RootPlayerBotAction.m_prioSettings.IdleFreezeForInteraction;
            m_idleAction.PrioLook = RootPlayerBotAction.m_prioSettings.IdleLook;
            m_idleAction.PrioPrepareAction = RootPlayerBotAction.m_prioSettings.IdlePrepare;
            this.m_idleAction = m_idleAction; // Assign it to the instance
            FlexibleMethodDefinition m_idleAction_act = new(this.UpdateActionIdle);
            data.comparisonMap[m_idleAction.Pointer] = m_idleAction_act;
            data.allActions.Add(m_idleAction); // Add it to our custom list
            // Repeat
            var m_followLeaderAction = new PlayerBotActionFollow.Descriptor(this.m_bot);
            this.m_followLeaderAction = m_followLeaderAction;
            FlexibleMethodDefinition m_followLeaderAction_act = new(this.UpdateActionFollowPlayer);
            data.comparisonMap[m_idleAction.Pointer] = m_followLeaderAction_act;
            data.allActions.Add(m_followLeaderAction);

            var m_useBioscanAction = new PlayerBotActionUseBioscan.Descriptor(this.m_bot);
            this.m_useBioscanAction = m_useBioscanAction;
            FlexibleMethodDefinition m_useBioscanAction_act = new(this.UpdateActionUseBioscan);
            data.comparisonMap[m_idleAction.Pointer] = m_useBioscanAction_act;
            data.allActions.Add(m_useBioscanAction);

            var m_attackAction = new PlayerBotActionAttack.Descriptor(this.m_bot);
            this.m_attackAction = m_attackAction;
            FlexibleMethodDefinition m_attackAction_act = new(this.UpdateActionAttack);
            data.comparisonMap[m_idleAction.Pointer] = m_attackAction_act;
            data.allActions.Add(m_attackAction);

            var m_reviveAction = new PlayerBotActionRevive.Descriptor(this.m_bot);
            this.m_reviveAction = m_reviveAction;
            FlexibleMethodDefinition m_reviveAction_act = new(this.UpdateActionReviveTeammate);
            data.comparisonMap[m_idleAction.Pointer] = m_reviveAction_act;
            data.allActions.Add(m_reviveAction);

            var m_unlockAction = new PlayerBotActionUnlock.Descriptor(this.m_bot);
            this.m_unlockAction = m_unlockAction;
            FlexibleMethodDefinition m_unlockAction_act = new(this.UpdateActionUnlock);
            data.comparisonMap[m_idleAction.Pointer] = m_unlockAction_act;
            data.allActions.Add(m_unlockAction);

            var m_highlightAction = new PlayerBotActionHighlight.Descriptor(this.m_bot);
            this.m_highlightAction = m_highlightAction;
            FlexibleMethodDefinition m_highlightAction_act = new(this.UpdateActionHighlight);
            data.comparisonMap[m_idleAction.Pointer] = m_highlightAction_act;
            data.allActions.Add(m_highlightAction);

            var m_useEnemyScannerAction = new PlayerBotActionUseEnemyScanner.Descriptor(this.m_bot);
            this.m_useEnemyScannerAction = m_useEnemyScannerAction;
            FlexibleMethodDefinition m_useEnemyScannerAction_act = new(this.UpdateActionUseEnemyScanner);
            data.comparisonMap[m_idleAction.Pointer] = m_useEnemyScannerAction_act;
            data.allActions.Add(m_useEnemyScannerAction);

            var m_tagEnemiesAction = new PlayerBotActionUseEnemyScanner.Descriptor(this.m_bot); // resued same class for some reason.  Create another one?
            this.m_tagEnemiesAction = m_tagEnemiesAction;
            FlexibleMethodDefinition m_tagEnemiesAction_act = new(this.UpdateActionTagEnemies);
            data.comparisonMap[m_idleAction.Pointer] = m_tagEnemiesAction_act;
            data.allActions.Add(m_tagEnemiesAction);

            var m_collectItemAction = new PlayerBotActionCollectItem.Descriptor(this.m_bot);
            this.m_collectItemAction = m_collectItemAction;
            FlexibleMethodDefinition m_collectItemAction_act = new(this.UpdateActionCollectItem);
            data.comparisonMap[m_idleAction.Pointer] = m_collectItemAction_act;
            data.allActions.Add(m_collectItemAction);

            var m_shareResourceAction = new PlayerBotActionShareResourcePack.Descriptor(this.m_bot);
            this.m_shareResourceAction = m_shareResourceAction;
            FlexibleMethodDefinition m_shareResourceAction_act = new(this.UpdateActionShareResoursePack);
            data.comparisonMap[m_idleAction.Pointer] = m_shareResourceAction_act;
            data.allActions.Add(m_shareResourceAction);

            var m_evadeAction = new PlayerBotActionEvadeProjectile.Descriptor(this.m_bot);
            m_evadeAction.PrioLook = m_prioSettings.EvadeProjectileLook;
            m_evadeAction.PrioPrecaution = m_prioSettings.EvadeProjectilePrecaution;
            this.m_evadeAction = m_evadeAction;
            FlexibleMethodDefinition m_evadeAction_act = new(this.UpdateActionEvadeProjectiles);
            data.comparisonMap[m_idleAction.Pointer] = m_evadeAction_act;
            data.allActions.Add(m_evadeAction);

            var m_exploreAction = new CustomActions.zPlayerBotActionExplore.Descriptor(this.m_bot);
            FlexibleMethodDefinition m_exploreAction_act = new(m_exploreAction.compareAction);
            data.comparisonMap[m_idleAction.Pointer] = m_exploreAction_act;
            data.allActions.Add(m_exploreAction);

            //foreach (var action in data.allActions) 
            //{ 
            //    zActions.RegisterStrictTypeInstanceDescriptor(action);
            //}
        }
    }   
}

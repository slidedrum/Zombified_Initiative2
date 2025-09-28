using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZombieTweak2.zRootBotPlayerAction.BaseActionWrappers
{
    internal class zRootPlayerBotAction : RootPlayerBotAction
    {
        public zRootPlayerBotAction(Descriptor desc) : base(desc)
        {
            var data = zActions.GetOrCreateData(this);
            //hurt action is handled somewhere else?  Might need to find it?  Or maybe it's unused now?  Who knows.

            var m_idleAction = new zPlayerBotActionIdle.Descriptor(this.m_bot); // Create the action wrapper
            m_idleAction.PrioFreezeForTwitcher = RootPlayerBotAction.m_prioSettings.IdleFreezeForTwitcher;
            m_idleAction.PrioFreezeForInteraction = RootPlayerBotAction.m_prioSettings.IdleFreezeForInteraction;
            m_idleAction.PrioLook = RootPlayerBotAction.m_prioSettings.IdleLook;
            m_idleAction.PrioPrepareAction = RootPlayerBotAction.m_prioSettings.IdlePrepare;
            this.m_idleAction = m_idleAction; // Assign it to the instance
            data.allActions.Add(m_idleAction); // Add it to our custom list
            // Repeat
            var m_followLeaderAction = new zPlayerBotActionFollow.Descriptor(this.m_bot);
            this.m_followLeaderAction = m_followLeaderAction;
            data.allActions.Add(m_followLeaderAction);

            var m_useBioscanAction = new zPlayerBotActionUseBioscan.Descriptor(this.m_bot);
            this.m_useBioscanAction = m_useBioscanAction;
            data.allActions.Add(m_useBioscanAction);

            var m_attackAction = new zPlayerBotActionAttack.Descriptor(this.m_bot);
            this.m_attackAction = m_attackAction;
            data.allActions.Add(m_attackAction);

            var m_reviveAction = new zPlayerBotActionRevive.Descriptor(this.m_bot);
            this.m_reviveAction = m_reviveAction;
            data.allActions.Add(m_reviveAction);

            var m_unlockAction = new zPlayerBotActionUnlock.Descriptor(this.m_bot);
            this.m_unlockAction = m_unlockAction;
            data.allActions.Add(m_unlockAction);

            var m_highlightAction = new zPlayerBotActionHighlight.Descriptor(this.m_bot);
            this.m_highlightAction = m_highlightAction;
            data.allActions.Add(m_highlightAction);

            var m_useEnemyScannerAction = new zPlayerBotActionUseEnemyScanner.Descriptor(this.m_bot);
            this.m_useEnemyScannerAction = m_useEnemyScannerAction;
            data.allActions.Add(m_useEnemyScannerAction);

            var m_tagEnemiesAction = new zPlayerBotActionUseEnemyScanner.Descriptor(this.m_bot); // resued same class for some reason.  Create another one?
            this.m_tagEnemiesAction = m_tagEnemiesAction;
            data.allActions.Add(m_tagEnemiesAction);

            var m_collectItemAction = new zPlayerBotActionCollectItem.Descriptor(this.m_bot);
            this.m_collectItemAction = m_collectItemAction;
            data.allActions.Add(m_collectItemAction);

            var m_shareResourceAction = new zPlayerBotActionShareResourcePack.Descriptor(this.m_bot);
            this.m_shareResourceAction = m_shareResourceAction;
            data.allActions.Add(m_shareResourceAction);

            var m_evadeAction = new zPlayerBotActionEvadeProjectile.Descriptor(this.m_bot);
            m_evadeAction.PrioLook = RootPlayerBotAction.m_prioSettings.EvadeProjectileLook;
            m_evadeAction.PrioPrecaution = RootPlayerBotAction.m_prioSettings.EvadeProjectilePrecaution;
            this.m_evadeAction = m_evadeAction;
            data.allActions.Add(m_evadeAction);

            foreach(var action in data.allActions) 
            { 
                zActions.RegisterStrictTypeInstance(action);
            }
        }
    }
}

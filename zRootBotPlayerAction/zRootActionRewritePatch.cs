using HarmonyLib;
using Il2CppSystem.Data.Common;
using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ZombieTweak2.zRootBotPlayerAction.BaseActionWrappers;
using Zombified_Initiative;

namespace ZombieTweak2.zRootBotPlayerAction
{
    public class RootPlayerBotActionData
    {
        public List<ICustomPlayerBotActionBase.IDescriptor> allActions = new();
    }
    [HarmonyPatch]
    public class RootActionRewritePatch
    {
        private static readonly ConditionalWeakTable<RootPlayerBotAction, RootPlayerBotActionData> RootActionDataStore = new();

        [HarmonyPatch(typeof(RootPlayerBotAction), MethodType.Constructor, new Type[] { typeof(RootPlayerBotAction.Descriptor) })]
        [HarmonyPrefix]
        public static bool RootCtor(RootPlayerBotAction __instance, RootPlayerBotAction.Descriptor desc)
        {
            if (!ZiMain.newRootBotPlayerAction)
                return true;
            // Might need some curvy fluffy underwear or something?
            // initalize our custom data
            RootActionDataStore.Add(__instance, new RootPlayerBotActionData());
            if (!RootActionDataStore.TryGetValue(__instance, out var data))
                return true; // fallback if something went wrong

            // Call the base constructor manually
            var baseCtor = AccessTools.Constructor(
                typeof(PlayerBotActionBase),
                new Type[] { typeof(PlayerBotActionBase.Descriptor) }
            );
            baseCtor.Invoke(__instance, new object[] { desc });

            __instance.m_gearAvailability = new RootPlayerBotAction.GearAvailability();
            __instance.m_desc = __instance.m_descBase as RootPlayerBotAction.Descriptor;

            data.allActions.Clear();

            //hurt action is handled somewhere else?  Might need to find it?  Or maybe it's unused now?  Who knows.

            var m_idleAction = new zPlayerBotActionIdle.Descriptor(__instance.m_bot); // Create the action wrapper
            m_idleAction.PrioFreezeForTwitcher = RootPlayerBotAction.m_prioSettings.IdleFreezeForTwitcher;
            m_idleAction.PrioFreezeForInteraction = RootPlayerBotAction.m_prioSettings.IdleFreezeForInteraction;
            m_idleAction.PrioLook = RootPlayerBotAction.m_prioSettings.IdleLook;
            m_idleAction.PrioPrepareAction = RootPlayerBotAction.m_prioSettings.IdlePrepare;
            __instance.m_idleAction = m_idleAction; // Assign it to the instance
            data.allActions.Add(m_idleAction); // Add it to our custom list
            // Repeat
            var m_followLeaderAction = new zPlayerBotActionFollow.Descriptor(__instance.m_bot);
            __instance.m_followLeaderAction = m_followLeaderAction;
            data.allActions.Add(m_followLeaderAction);

            var m_useBioscanAction = new zPlayerBotActionUseBioscan.Descriptor(__instance.m_bot);
            __instance.m_useBioscanAction = m_useBioscanAction;
            data.allActions.Add(m_useBioscanAction);

            var m_attackAction = new zPlayerBotActionAttack.Descriptor(__instance.m_bot);
            __instance.m_attackAction = m_attackAction;
            data.allActions.Add(m_attackAction);

            var m_reviveAction = new zPlayerBotActionRevive.Descriptor(__instance.m_bot);
            __instance.m_reviveAction = m_reviveAction;
            data.allActions.Add(m_reviveAction);

            var m_unlockAction = new zPlayerBotActionUnlock.Descriptor(__instance.m_bot);
            __instance.m_unlockAction = m_unlockAction;
            data.allActions.Add(m_unlockAction);

            var m_highlightAction = new zPlayerBotActionHighlight.Descriptor(__instance.m_bot);
            __instance.m_highlightAction = m_highlightAction;
            data.allActions.Add(m_highlightAction);

            var m_useEnemyScannerAction = new zPlayerBotActionUseEnemyScanner.Descriptor(__instance.m_bot);
            __instance.m_useEnemyScannerAction = m_useEnemyScannerAction;
            data.allActions.Add(m_useEnemyScannerAction);

            var m_tagEnemiesAction = new zPlayerBotActionUseEnemyScanner.Descriptor(__instance.m_bot); // resued same class for some reason.  Create another one?
            __instance.m_tagEnemiesAction = m_tagEnemiesAction;
            data.allActions.Add(m_tagEnemiesAction);

            var m_collectItemAction = new zPlayerBotActionCollectItem.Descriptor(__instance.m_bot);
            __instance.m_collectItemAction = m_collectItemAction;
            data.allActions.Add(m_collectItemAction);

            var m_shareResourceAction = new zPlayerBotActionShareResourcePack.Descriptor(__instance.m_bot);
            __instance.m_shareResourceAction = m_shareResourceAction;
            data.allActions.Add(m_shareResourceAction);

            var m_evadeAction = new zPlayerBotActionEvadeProjectile.Descriptor(__instance.m_bot);
            m_evadeAction.PrioLook = RootPlayerBotAction.m_prioSettings.EvadeProjectileLook;
            m_evadeAction.PrioPrecaution = RootPlayerBotAction.m_prioSettings.EvadeProjectilePrecaution;
            __instance.m_evadeAction = m_evadeAction;
            data.allActions.Add(m_evadeAction);

            __instance.RefreshGearAvailability();
            
            return false;
        }

        [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.Update))]
        [HarmonyPrefix]
        public static bool Update(RootPlayerBotAction __instance, ref bool __result)
        {
            return true;
            if (!ZiMain.newRootBotPlayerAction)
                return true;
            RootActionDataStore.TryGetValue(__instance, out var __data); //Get our custom data

            __result = __instance.IsActive();
            //if (base.Update())
            var baseUpdate = AccessTools.Method(typeof(PlayerBotActionBase), "Update");
            bool baseResult = (bool)baseUpdate.Invoke(__instance, null);
            if (baseResult)
            {
                __result = true;
                return false; // skip original
            }


            return false;
        }

    }
}

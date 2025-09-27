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

            //hurt action is handled somewhere else?  Might need to find it?  Or maybe it's unused now?  Who knows.

            data.allActions.Clear();

            var m_idleAction = new zPlayerBotActionIdle.Descriptor(__instance.m_bot); // Create the action wrapper
            m_idleAction.PrioFreezeForTwitcher = RootPlayerBotAction.m_prioSettings.IdleFreezeForTwitcher;
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

            __instance.m_attackAction =             new PlayerBotActionAttack.Descriptor            (__instance.m_bot);
            __instance.m_reviveAction =             new PlayerBotActionRevive.Descriptor            (__instance.m_bot);
            __instance.m_unlockAction =             new PlayerBotActionUnlock.Descriptor            (__instance.m_bot); 
            __instance.m_highlightAction =          new PlayerBotActionHighlight.Descriptor         (__instance.m_bot);
            __instance.m_useEnemyScannerAction =    new PlayerBotActionUseEnemyScanner.Descriptor   (__instance.m_bot);
            __instance.m_tagEnemiesAction =         new PlayerBotActionUseEnemyScanner.Descriptor   (__instance.m_bot);
            __instance.m_collectItemAction =        new PlayerBotActionCollectItem.Descriptor       (__instance.m_bot);
            __instance.m_shareResourceAction =      new PlayerBotActionShareResourcePack.Descriptor (__instance.m_bot);
            __instance.m_evadeAction =              new PlayerBotActionEvadeProjectile.Descriptor   (__instance.m_bot);
            __instance.RefreshGearAvailability();
            
            
            data.allActions.Add(__instance.m_attackAction);
            data.allActions.Add(__instance.m_reviveAction);
            data.allActions.Add(__instance.m_unlockAction);
            data.allActions.Add(__instance.m_highlightAction);
            data.allActions.Add(__instance.m_useEnemyScannerAction);
            data.allActions.Add(__instance.m_tagEnemiesAction);
            data.allActions.Add(__instance.m_collectItemAction);
            data.allActions.Add(__instance.m_shareResourceAction);
            data.allActions.Add(__instance.m_evadeAction);
            return false;
        }

        [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.Update))]
        [HarmonyPrefix]
        public static bool Update(RootPlayerBotAction __instance, ref bool __result)
        {
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

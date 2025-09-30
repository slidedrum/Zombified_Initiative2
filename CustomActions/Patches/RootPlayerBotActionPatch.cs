using HarmonyLib;
using Player;

namespace ZombieTweak2.zRootBotPlayerAction.Patches
{
    [HarmonyPatch]
    public class RootPlayerBotActionPatch
    {
        [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.Update))]
        [HarmonyPrefix]
        public static bool PreUpdate(RootPlayerBotAction __instance, ref bool __result)
        {
            //We need to reset the best action watcher before we start calling vanilla actions.
            var data = zActions.GetOrCreateData(__instance);
            data.bestAction = null;
            return true;
        }
        [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.Update))]
        [HarmonyPostfix]
        public static void PostUpdate(RootPlayerBotAction __instance, ref bool __result)
        {
            //after vanilla actions eval we need to eval custom actions.
            //Whatever vanilla action is best still gets called no matter what, might want to chagne that?  Might not be a problem?
            var data = zActions.GetOrCreateData(__instance);
            foreach (var act in data.customActions)
            {
                act.compareAction(ref data.bestAction);
            }
            if (data.bestAction != null)
            {
                __instance.m_bot.StartAction(data.bestAction);
            }
        }
    }
}
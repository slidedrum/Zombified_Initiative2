using HarmonyLib;
using Il2CppSystem.Security.Cryptography;
using Player;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        internal static RootPlayerBotActionData GetOrCreateData(PlayerBotActionBase botBase)
        {
            int botId = botBase.m_bot.GetInstanceID();
            if (!RootActionDataStore.TryGetValue(botId, out var data))
            {
                data = new RootPlayerBotActionData();
                RootActionDataStore[botId] = data;
            }
            return data;
        }
        internal static readonly Dictionary<int, RootPlayerBotActionData> RootActionDataStore = new();

        [HarmonyPatch(typeof(RootPlayerBotAction.Descriptor), nameof(RootPlayerBotAction.Descriptor.CreateAction))]
        [HarmonyPrefix]
        public static bool CreateAction(RootPlayerBotAction.Descriptor __instance,ref PlayerBotActionBase __result)
        {
            // Can't hook the constructor directly for some reason, so hook the method that calls the constructor and call my wrapper instead.
            __result = new zRootPlayerBotAction(__instance);
            return false;
        }

        [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.Update))]
        [HarmonyPrefix]
        public static bool Update(RootPlayerBotAction __instance, ref bool __result)
        {
            if (!ZiMain.newRootBotPlayerAction)
                return true;
            var data = GetOrCreateData(__instance);
            return true;
        }

    }
}
//__result = __instance.IsActive();
////if (base.Update())
//var baseUpdate = AccessTools.Method(typeof(PlayerBotActionBase), "Update");
//bool baseResult = (bool)baseUpdate.Invoke(__instance, null);
//if (baseResult)
//{
//    __result = true;
//    return false; // skip original
//}
//return false;
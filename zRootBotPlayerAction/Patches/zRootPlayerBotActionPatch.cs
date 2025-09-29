using BetterBots.Utils;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Player;
using System.Collections.Generic;
using ZombieTweak2.zRootBotPlayerAction.BaseActionWrappers;
using Zombified_Initiative;

namespace ZombieTweak2.zRootBotPlayerAction.Patches
{
    [HarmonyPatch]
    public class RootPlayerBotActionPatch
    {
        [HarmonyPatch(typeof(RootPlayerBotAction.Descriptor), nameof(RootPlayerBotAction.Descriptor.CreateAction))]
        [HarmonyPrefix]
        public static bool CreateAction(RootPlayerBotAction.Descriptor __instance, ref PlayerBotActionBase __result)
        {
            // Can't hook the constructor directly for some reason, so hook the method that calls the constructor and call my wrapper instead.
            // Might flip back to hooking the constructor if I can figure out how.
            __result = new zRootPlayerBotAction(__instance);
            return false;
        }
        public static bool BaseUpdate(RootPlayerBotAction __instance)
        {
            return !__instance.IsActive(); //this is really dumb but this is what the base game does. /shrug
        }
        [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.Update))]
        [HarmonyPrefix]
        public static bool Update(RootPlayerBotAction __instance, ref bool __result)
        {
            if (!ZiMain.newRootBotPlayerAction)
                return true;
            var data = zActions.GetOrCreateData(__instance);

            if (BaseUpdate(__instance))
            {
                __result = true;
                return false;
            }
            __instance.RefreshGearAvailability();

            PlayerBotActionBase.Descriptor bestAction = null;
            foreach (PlayerBotActionBase.Descriptor actionDesc in data.allActions)
            {
                data.comparisonMap[actionDesc.Pointer].Invoke<PlayerBotActionBase.Descriptor>(ref bestAction);
            }
            if (bestAction != null)
            {
                __instance.StartAction(bestAction);
            }
            __result = !__instance.IsActive();
            return false;
        }
        [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.Stop))]
        [HarmonyPrefix]
        public static bool Stop(RootPlayerBotAction __instance)
        {
            var data = zActions.GetOrCreateData(__instance);
            foreach (var actionDesc in data.allActions)
            {
                __instance.SafeStopAction(actionDesc);
            }
            return false;
        }
        //[HarmonyPatch(typeof(PlayerBotActionBase.Descriptor), nameof(PlayerBotActionBase.Descriptor.CreateAction))]
        //[HarmonyPostfix]
        //static void CreateAction(PlayerBotActionBase.Descriptor __instance, ref PlayerBotActionBase __result)
        //{
        //    if (__result is ICustomPlayerBotActionBase)
        //    {
        //        zActions.RegisterStrictTypeInstance(__result);
        //    }
        //}
        [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.RefreshGearAvailability))]
        [HarmonyPrefix]
        private static bool RefreshGearAvailability(RootPlayerBotAction __instance)
        {
            PlayerBackpack backpack = PlayerBackpackManager.GetBackpack(__instance.m_agent.Owner);
            __instance.m_gearAvailability.Reset();
            BackpackItem backpackItem = backpack.Slots[3];
            if (backpackItem != null)
            {
                uint num = backpackItem.Instance.ItemDataBlock.persistentID;
                if (num <= 73U)
                {
                    if (num != 28U)
                    {
                        if (num != 37U)
                        {
                            if (num == 73U)
                            {
                                __instance.m_gearAvailability.Set(RootPlayerBotAction.GearAvailability.GearFlags.GlueGun);
                            }
                        }
                        else
                        {
                            __instance.m_gearAvailability.Set(RootPlayerBotAction.GearAvailability.GearFlags.TripMineDeployer);
                        }
                    }
                    else
                    {
                        __instance.m_gearAvailability.Set(RootPlayerBotAction.GearAvailability.GearFlags.EnemyScanner);
                    }
                }
                else if (num != 97U)
                {
                    if (num != 139U)
                    {
                        if (num == 144U)
                        {
                            __instance.m_gearAvailability.Set(RootPlayerBotAction.GearAvailability.GearFlags.TripMineGlue);
                        }
                    }
                    else
                    {
                        __instance.m_gearAvailability.Set(RootPlayerBotAction.GearAvailability.GearFlags.TripMineExplosive);
                    }
                }
                else
                {
                    __instance.m_gearAvailability.Set(RootPlayerBotAction.GearAvailability.GearFlags.SentryGun);
                }
            }
            backpackItem = backpack.Slots[4];
            if (backpackItem != null)
            {
                uint num = backpackItem.Instance.ItemDataBlock.persistentID;
                if (num <= 102U)
                {
                    if (num != 101U)
                    {
                        if (num == 102U)
                        {
                            __instance.m_gearAvailability.Set(RootPlayerBotAction.GearAvailability.GearFlags.Medipack);
                        }
                    }
                    else
                    {
                        __instance.m_gearAvailability.Set(RootPlayerBotAction.GearAvailability.GearFlags.AmmoPackWeapon);
                    }
                }
                else if (num != 127U)
                {
                    if (num == 132U)
                    {
                        __instance.m_gearAvailability.Set(RootPlayerBotAction.GearAvailability.GearFlags.DisinfectionPack);
                    }
                }
                else
                {
                    __instance.m_gearAvailability.Set(RootPlayerBotAction.GearAvailability.GearFlags.AmmoPackTool);
                }
            }
            backpackItem = backpack.Slots[8];
            if (backpackItem != null)
            {
                __instance.m_gearAvailability.Set(RootPlayerBotAction.GearAvailability.GearFlags.ExpeditionItem);
            }
            return false;
        }

    }
}
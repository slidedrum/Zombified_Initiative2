using HarmonyLib;
using Player;
using System;
using UnityEngine;
using static Player.PlayerBotActionBase;

namespace ZombieTweak2
{
    [HarmonyPatch]
    internal class CustomDescBase
    {
        private readonly PlayerBotActionBase.Descriptor __instance;

        public CustomDescBase(PlayerBotActionBase.Descriptor instance)
        {
            __instance = instance;
        }

        // ==== Non-static wrappers ====
        public void OnQueued() => OnQueuedBase(__instance);

        public bool CheckCollision(PlayerBotActionBase.Descriptor desc)
            => CheckCollisionBase(__instance, desc);

        public bool IsActionAllowed(PlayerBotActionBase.Descriptor desc)
            => IsActionAllowedBase(__instance, desc);

        public AccessLayers GetAccessLayersRuntime()
            => GetAccessLayersRuntimeBase(__instance);

        public void InternalOnTerminated()
            => InternalOnTerminatedBase(__instance);


        // ==== Static reverse-patch stubs ====
        [HarmonyPatch(typeof(PlayerBotActionBase.Descriptor), nameof(PlayerBotActionBase.Descriptor.OnQueued))]
        [HarmonyReversePatch]
        private static void OnQueuedBase(PlayerBotActionBase.Descriptor __instance)
        {
            throw new NotImplementedException();
        }

        [HarmonyPatch(typeof(PlayerBotActionBase.Descriptor), nameof(PlayerBotActionBase.Descriptor.CheckCollision))]
        [HarmonyReversePatch]
        private static bool CheckCollisionBase(PlayerBotActionBase.Descriptor __instance, PlayerBotActionBase.Descriptor desc)
        {
            throw new NotImplementedException();
        }

        [HarmonyPatch(typeof(PlayerBotActionBase.Descriptor), nameof(PlayerBotActionBase.Descriptor.IsActionAllowed))]
        [HarmonyReversePatch]
        private static bool IsActionAllowedBase(PlayerBotActionBase.Descriptor __instance, PlayerBotActionBase.Descriptor desc)
        {
            throw new NotImplementedException();
        }

        [HarmonyPatch(typeof(PlayerBotActionBase.Descriptor), nameof(PlayerBotActionBase.Descriptor.GetAccessLayersRuntime))]
        [HarmonyReversePatch]
        private static AccessLayers GetAccessLayersRuntimeBase(PlayerBotActionBase.Descriptor __instance)
        {
            throw new NotImplementedException();
        }

        [HarmonyPatch(typeof(PlayerBotActionBase.Descriptor), nameof(PlayerBotActionBase.Descriptor.InternalOnTerminated))]
        [HarmonyReversePatch]
        private static void InternalOnTerminatedBase(PlayerBotActionBase.Descriptor __instance)
        {
            throw new NotImplementedException();
        }
    }
    [HarmonyPatch]
    internal class CustomBase
    {
        private readonly PlayerBotActionBase __instance;

        public CustomBase(PlayerBotActionBase instance)
        {
            __instance = instance;
        }

        // ==== Non-static wrappers ====
        public void Stop() => StopBase(__instance);

        public bool Update() => UpdateBase(__instance);

        public void OnWarped(Vector3 position) => OnWarpedBase(__instance, position);

        public bool IsActionAllowed(PlayerBotActionBase.Descriptor desc)
            => IsActionAllowedBase(__instance, desc);

        public bool CheckCollision(PlayerBotActionBase.Descriptor desc)
            => CheckCollisionBase(__instance, desc);

        public AccessLayers GetAccessLayersRuntime()
            => GetAccessLayersRuntimeBase(__instance);


        // ==== Static reverse-patch stubs ====
        [HarmonyPatch(typeof(PlayerBotActionBase), nameof(PlayerBotActionBase.Stop))]
        [HarmonyReversePatch]
        private static void StopBase(PlayerBotActionBase __instance)
        {
            throw new NotImplementedException();
        }

        [HarmonyPatch(typeof(PlayerBotActionBase), nameof(PlayerBotActionBase.Update))]
        [HarmonyReversePatch]
        private static bool UpdateBase(PlayerBotActionBase __instance)
        {
            throw new NotImplementedException();
        }

        [HarmonyPatch(typeof(PlayerBotActionBase), nameof(PlayerBotActionBase.OnWarped))]
        [HarmonyReversePatch]
        private static void OnWarpedBase(PlayerBotActionBase __instance, Vector3 position)
        {
            throw new NotImplementedException();
        }

        [HarmonyPatch(typeof(PlayerBotActionBase), nameof(PlayerBotActionBase.IsActionAllowed))]
        [HarmonyReversePatch]
        private static bool IsActionAllowedBase(PlayerBotActionBase __instance, PlayerBotActionBase.Descriptor desc)
        {
            throw new NotImplementedException();
        }

        [HarmonyPatch(typeof(PlayerBotActionBase), nameof(PlayerBotActionBase.CheckCollision))]
        [HarmonyReversePatch]
        private static bool CheckCollisionBase(PlayerBotActionBase __instance, PlayerBotActionBase.Descriptor desc)
        {
            throw new NotImplementedException();
        }

        [HarmonyPatch(typeof(PlayerBotActionBase), nameof(PlayerBotActionBase.GetAccessLayersRuntime))]
        [HarmonyReversePatch]
        private static AccessLayers GetAccessLayersRuntimeBase(PlayerBotActionBase __instance)
        {
            throw new NotImplementedException();
        }
    }

}

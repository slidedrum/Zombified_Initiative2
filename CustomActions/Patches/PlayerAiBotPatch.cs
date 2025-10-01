using FluffyUnderware.Curvy.ThirdParty.LibTessDotNet;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using Zombified_Initiative;

namespace ZombieTweak2.zRootBotPlayerAction.Patches
{
    
    [HarmonyPatch]
    internal class PlayerAiBotPatch
    {
        private static Dictionary<string,bool> vanillaOverides = new();

        [HarmonyPatch(typeof(PlayerAIBot), nameof(PlayerAIBot.Setup))]
        [HarmonyPrefix]
        public static void Setup(PlayerAIBot __instance)
        {
            // This is the only patch that actually has any new behavior.
            // Everything else is just needed to actually call my derived overides.
            bool overide = true;
            vanillaOverides["StartQueuedActions"]       = overide;
            vanillaOverides["UpdateActions"]            = overide;
            vanillaOverides["IsActionForbidden"]        = overide;
            vanillaOverides["OnWarped"]                 = overide;
            vanillaOverides["RemoveCollidingActions"]   = overide;
            vanillaOverides["SetEnabled"]               = overide;
            vanillaOverides["StartAction"]              = overide;
            vanillaOverides["StopAction"]               = overide;

            var data = zActions.GetOrCreateData(__instance);
            var assembly = Assembly.GetExecutingAssembly();
            var customActionTypes = assembly.GetTypes().Where(t => !t.IsAbstract && typeof(CustomActionBase).IsAssignableFrom(t));
            foreach (var actionType in customActionTypes)
            {
                if (!ClassInjector.IsTypeRegisteredInIl2Cpp(actionType))
                    ClassInjector.RegisterTypeInIl2Cpp(actionType);
                var descriptorType = actionType.GetNestedType("Descriptor", BindingFlags.Public | BindingFlags.NonPublic);
                if (!ClassInjector.IsTypeRegisteredInIl2Cpp(descriptorType))
                    ClassInjector.RegisterTypeInIl2Cpp(descriptorType);
                var descriptorInstance = Activator.CreateInstance(descriptorType, new object[] { __instance });
                data.customActions.Add((CustomActionBase.Descriptor)descriptorInstance);
            }
            ZiMain.log.LogMessage("init playerbot");
        }
        [HarmonyPatch(typeof(PlayerAIBot), nameof(PlayerAIBot.StartQueuedActions))]
        [HarmonyPrefix]
        public static bool StartQueuedActions(PlayerAIBot __instance)
        {
            if (!vanillaOverides["StartQueuedActions"])
                return true;
            if (__instance.m_queuedActions.Count == 0)
            {
                return false;
            }
            var array = new Il2CppReferenceArray<PlayerBotActionBase.Descriptor>(__instance.m_queuedActions.Count);
            __instance.m_queuedActions.CopyTo(array);
            __instance.m_queuedActions.Clear();
            foreach (PlayerBotActionBase.Descriptor descriptor in array)
            {
                if (descriptor.Status == PlayerBotActionBase.Descriptor.StatusType.Queued)
                {
                    descriptor.OnStarted();
                    PlayerBotActionBase playerBotActionBase = descriptor.CreateAction();
                    __instance.RemoveCollidingActions(descriptor);
                    __instance.m_actions.Add(playerBotActionBase);
                }
            }
            return false;
        }
        [HarmonyPatch(typeof(PlayerAIBot), nameof(PlayerAIBot.UpdateActions))]
        [HarmonyPrefix]
        public static bool UpdateActions(PlayerAIBot __instance)
        {
            if (!vanillaOverides["UpdateActions"])
                return true;
            if (__instance.m_actions.Count == 0)
            {
                return false;
            }
            //PlayerBotActionBase[] array = new PlayerBotActionBase[__instance.m_actions.Count];
            var array = new Il2CppReferenceArray<PlayerBotActionBase>(__instance.m_actions.Count);
            __instance.m_actions.CopyTo(array, 0);
            for (int i = 0; i < array.Length; i++)
            {
                PlayerBotActionBase playerBotActionBase = array[i];
                PlayerAIBot.s_updatingAction = playerBotActionBase.DescBase;
                if (!playerBotActionBase.IsActive() || playerBotActionBase.Update())
                { //Has the action completed?
                    int num = i;
                    if (num >= __instance.m_actions.Count || __instance.m_actions[num] != playerBotActionBase)
                    { //Find the __instance to remove it at
                        bool flag = false;
                        for (int j = 0; j < __instance.m_actions.Count; j++)
                        {
                            if (__instance.m_actions[j].Pointer == playerBotActionBase.Pointer) //this is failing as a direct comparison so have to compare pointer
                            {
                                flag = true;
                                num = j;
                                break;
                            }
                        }
                        if (!flag)
                        {
                            continue;
                        }
                    }
                    PlayerAIBot.s_updatingAction = null;
                    __instance.m_actions.RemoveAt(num);
                    playerBotActionBase.DescBase.OnExpired();
                    playerBotActionBase.Stop();
                }
            }
            PlayerAIBot.s_updatingAction = null;
            return false;
        }
        [HarmonyPatch(typeof(PlayerAIBot), nameof(PlayerAIBot.IsActionForbidden))]
        [HarmonyPrefix]
        public static bool IsActionForbidden(PlayerAIBot __instance, PlayerBotActionBase.Descriptor desc, ref bool __result)
        {
            if (!vanillaOverides["IsActionForbidden"])
                return true;
            for (int i = 0; i < __instance.m_queuedActions.Count; i++)
            {
                if (!__instance.m_queuedActions[i].IsActionAllowed(desc))
                {
                    __result = true;
                    return false;
                }
            }
            for (int j = 0; j < __instance.m_actions.Count; j++)
            {
                if (!__instance.m_actions[j].IsActionAllowed(desc))
                {
                    __result = true;
                    return false;
                }
            }
            __result = false;
            return false;
        }
        [HarmonyPatch(typeof(PlayerAIBot), nameof(PlayerAIBot.OnWarped))]
        [HarmonyPrefix]
        public static bool OnWarped(PlayerAIBot __instance, Vector3 position)
        {
            if (!vanillaOverides["OnWarped"])
                return true;
            for (int i = 0; i < __instance.m_actions.Count; i++)
            {
                PlayerBotActionBase playerBotActionBase = __instance.m_actions[i];
                if (playerBotActionBase.IsActive())
                {
                    playerBotActionBase.OnWarped(position);
                }
            }
            __instance.m_syncValues.Position = position;
            __instance.ApplyValues();
            return false;
        }
        [HarmonyPatch(typeof(PlayerAIBot), nameof(PlayerAIBot.RemoveCollidingActions))]
        [HarmonyPrefix]
        public static bool RemoveCollidingActions(PlayerAIBot __instance, PlayerBotActionBase.Descriptor desc)
        {
            if (!vanillaOverides["RemoveCollidingActions"])
                return true;
            bool hasRemoved;
            do
            {
                hasRemoved = false;
                int i = 0;
                while (i < __instance.m_queuedActions.Count)
                {
                    PlayerBotActionBase.Descriptor descriptor = __instance.m_queuedActions[i];
                    if (descriptor.Status == PlayerBotActionBase.Descriptor.StatusType.Queued && descriptor.CheckCollision(desc))
                    {
                        __instance.m_queuedActions.RemoveAt(i);
                        descriptor.OnAborted();
                        hasRemoved = true;
                    }
                    else
                    {
                        i++;
                    }
                }
                int j = 0;
                while (j < __instance.m_actions.Count)
                {
                    PlayerBotActionBase playerBotActionBase = __instance.m_actions[j];
                    if (playerBotActionBase.CheckCollision(desc)) //this might be a problem with pointers?
                    {
                        __instance.m_actions.RemoveAt(j);
                        playerBotActionBase.DescBase.OnInterrupted();
                        playerBotActionBase.Stop();
                        hasRemoved = true;
                    }
                    else
                    {
                        j++;
                    }
                }
            }
            while (hasRemoved && !desc.IsTerminated());
            return false;
        }
        [HarmonyPatch(typeof(PlayerAIBot), nameof(PlayerAIBot.SetEnabled))]
        [HarmonyPrefix]
        public static bool SetEnabled(PlayerAIBot __instance, bool state)
        {
            if (!vanillaOverides["SetEnabled"])
                return true;
            if (state == __instance.enabled)
            {
                return false;
            }
            if (state)
            {
                NavMeshHit navMeshHit;
                if (NavMesh.SamplePosition(__instance.m_playerAgent.Position, out navMeshHit, 3f, -1))
                {
                    __instance.m_syncValues.Position = navMeshHit.position;
                }
                else
                {
                    __instance.m_syncValues.Position = __instance.m_playerAgent.Position;
                }
                __instance.m_syncValues.Forward = __instance.m_playerAgent.Forward;
                __instance.m_syncValues.LookDirection = __instance.m_playerAgent.TargetLookDir;
                __instance.m_syncValues.Ladder = __instance.m_playerAgent.Locomotion.CurrentLadder;
                __instance.InitValues();
                __instance.m_lastSyncedPosition = __instance.m_syncValues.Position;
            }
            else
            {
                bool hasRemoved;
                do
                {
                    hasRemoved = false;
                    for (int i = 0; i < __instance.m_queuedActions.Count; i++)
                    {
                        if (__instance.m_queuedActions[i].Pointer != __instance.m_rootAction.Pointer)
                        {
                            __instance.m_queuedActions[i].OnAborted();
                            __instance.m_queuedActions.RemoveAt(i);
                            hasRemoved = true;
                        }
                    }
                    for (int j = 0; j < __instance.m_actions.Count; j++)
                    {
                        if (__instance.m_actions[j].DescBase.Pointer != __instance.m_rootAction.Pointer)
                        {
                            __instance.m_actions[j].Stop();
                            __instance.m_actions[j].DescBase.OnStopped();
                            __instance.m_actions.RemoveAt(j);
                            hasRemoved = true;
                        }
                    }
                }
                while (hasRemoved);
            }
            __instance.enabled = state;
            return false;
        }
        [HarmonyPatch(typeof(PlayerAIBot), nameof(PlayerAIBot.StartAction))]
        [HarmonyPrefix]
        public static bool StartAction(PlayerAIBot __instance, PlayerBotActionBase.Descriptor desc)
        {
            if (!vanillaOverides["StartAction"])
                return true;
            if (!desc.IsTerminated())
            {
                Debug.LogError("Action was queued while active: " + desc);
                return false;
            }
            for (int i = 0; i < __instance.m_actions.Count; i++)
            {
                if (__instance.m_actions[i].DescBase == desc)
                {
                    __instance.m_actions.RemoveAt(i);
                    break;
                }
            }
            desc.OnQueued();
            __instance.RemoveCollidingActions(desc);
            __instance.m_queuedActions.Add(desc);
            return false;
        }
        [HarmonyPatch(typeof(PlayerAIBot), nameof(PlayerAIBot.StopAction))]
        [HarmonyPrefix]
        public static bool StopAction(PlayerAIBot __instance, PlayerBotActionBase.Descriptor desc)
        {
            if (!vanillaOverides["StopAction"])
                return true;
            if (desc == PlayerAIBot.s_updatingAction)
            {
                Debug.LogError("Action was removed during its update: " + desc);
            }
            if (desc.Status == PlayerBotActionBase.Descriptor.StatusType.Queued)
            {
                desc.OnAborted();
                for (int i = 0; i < __instance.m_queuedActions.Count; i++)
                {
                    if (__instance.m_queuedActions[i] == desc)
                    {
                        __instance.m_queuedActions.RemoveAt(i);
                        return false;
                    }
                }
                return false;
            }
            if (desc.Status == PlayerBotActionBase.Descriptor.StatusType.Active)
            {
                if (desc.ActionBase == null)
                {
                    Debug.LogError("Active descriptor is missing action: " + desc);
                }
                __instance.m_actions.Remove(desc.ActionBase);
                desc.ActionBase.Stop();
                desc.OnStopped();
            }
            return false;
        }
    } // class
}// namespace

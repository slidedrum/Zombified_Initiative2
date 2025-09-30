using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Player;
using UnityEngine;
using ZombieTweak2.zRootBotPlayerAction.CustomActions;
using Zombified_Initiative;

namespace ZombieTweak2.zRootBotPlayerAction.Patches
{
    [HarmonyPatch]
    internal class PlayerAiBotPatch
    {
        [HarmonyPatch(typeof(PlayerAIBot), nameof(PlayerAIBot.StartQueuedActions))]
        [HarmonyPrefix]
        public static bool StartQueuedActions(PlayerAIBot __instance)
        {
            var data = zActions.GetOrCreateData(__instance);
            if (data.m_queuedActions.Count == 0)
            {
                return false;
            }
            var array = new Il2CppReferenceArray<PlayerBotActionBase.Descriptor>(data.m_queuedActions.Count);
            data.m_queuedActions.CopyTo(array);
            data.m_queuedActions.Clear();
            foreach (PlayerBotActionBase.Descriptor descriptor in array)
            {
                if (descriptor.Status == PlayerBotActionBase.Descriptor.StatusType.Queued)
                {
                    descriptor.OnStarted();
                    PlayerBotActionBase playerBotActionBase = descriptor.CreateAction();
                    __instance.RemoveCollidingActions(descriptor);
                    data.m_actions.Add(playerBotActionBase);
                }
            }
            return false;
        }
        [HarmonyPatch(typeof(PlayerAIBot), nameof(PlayerAIBot.UpdateActions))]
        [HarmonyPrefix]
        public static bool UpdateActions(PlayerAIBot __instance)
        {
            var data = zActions.GetOrCreateData(__instance);
            if (data.m_actions.Count == 0)
            {
                return false;
            }
            //PlayerBotActionBase[] array = new PlayerBotActionBase[instance.m_actions.Count];
            var array = new Il2CppReferenceArray<PlayerBotActionBase>(data.m_actions.Count);
            data.m_actions.CopyTo(array, 0);
            for (int i = 0; i < array.Length; i++)
            {
                PlayerBotActionBase playerBotActionBase = array[i];
                PlayerAIBot.s_updatingAction = playerBotActionBase.DescBase;
                if (!playerBotActionBase.IsActive() || playerBotActionBase.Update())
                { //Has the action completed?
                    int num = i;
                    if (num >= data.m_actions.Count || data.m_actions[num] != playerBotActionBase)
                    { //Find the instance to remove it at
                        bool flag = false;
                        for (int j = 0; j < data.m_actions.Count; j++)
                        {
                            if (data.m_actions[j].Pointer == playerBotActionBase.Pointer) //this is failing as a direct comparison so have to compare pointer
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
                    data.m_actions.RemoveAt(num);
                    playerBotActionBase.DescBase.OnExpired();
                    playerBotActionBase.Stop();
                }
            }
            PlayerAIBot.s_updatingAction = null;
            return false;
        }
        [HarmonyPatch(typeof(PlayerAIBot), nameof(PlayerAIBot.Setup))]
        [HarmonyPrefix]
        public static void Setup(PlayerAIBot __instance)
        {
            var data = zActions.GetOrCreateData(__instance);
            __instance.m_actions = data.m_actions;
            __instance.m_queuedActions = data.m_queuedActions;
            //var m_exploreAction = new CustomActions.exploreDescriptor(instance);
            //data.customActions.Add(m_exploreAction);
            ClassInjector.RegisterTypeInIl2Cpp(typeof(CustomActionBase.Descriptor));
            ClassInjector.RegisterTypeInIl2Cpp(typeof(CustomActionBase));
            ClassInjector.RegisterTypeInIl2Cpp(typeof(zPlayerBotActionExplore.Descriptor));
            ClassInjector.RegisterTypeInIl2Cpp(typeof(zPlayerBotActionExplore));
            CustomActionBase.Descriptor m_testAction = new CustomActionBase.Descriptor(__instance);
            data.customActions.Add(m_testAction);
            zPlayerBotActionExplore.Descriptor m_exploreAction = new zPlayerBotActionExplore.Descriptor(__instance);
            data.customActions.Add(m_exploreAction);
            ZiMain.log.LogMessage("init playerbot");
        }
        //[HarmonyPatch(typeof(PlayerAIBot), nameof(PlayerAIBot.Update))]
        //[HarmonyPrefix]
        //public static void Update(PlayerAIBot instance)
        //{

        //}
        //[HarmonyPatch(typeof(PlayerAIBot), nameof(PlayerAIBot.ApplyRestrictionsToRootPosition))]
        //[HarmonyPrefix]
        //public static bool ApplyRestrictionsToRootPosition(PlayerAIBot instance, ref Vector3 testPosition, ref float restrictionPrio, ref bool __result)
        //{
        //    var data = zActions.GetOrCreateData(instance);
        //    float resultPrio = restrictionPrio;
        //    Vector3 resultPos = testPosition;
        //    Vector3 tmpPos;
        //    Func<PlayerBotActionBase.Descriptor, Vector3, bool> ApplyRestriction = delegate (PlayerBotActionBase.Descriptor desc, Vector3 prevPos)
        //    {
        //        tmpPos = prevPos;
        //        if (desc.Prio > resultPrio && desc.ApplyPositionRestriction(ref tmpPos))
        //        {
        //            resultPrio = desc.Prio;
        //            resultPos = tmpPos;
        //        }
        //        return true;
        //    };
        //    for (int i = 0; i < data.m_queuedActions.Count; i++)
        //    {
        //        ApplyRestriction(data.m_queuedActions[i], testPosition);
        //    }
        //    for (int j = 0; j < data.m_actions.Count; j++)
        //    {
        //        ApplyRestriction(data.m_actions[j].DescBase, testPosition);
        //    }
        //    if (resultPrio > restrictionPrio)
        //    {
        //        restrictionPrio = resultPrio;
        //        testPosition = resultPos;
        //        __result = true;
        //        return false;
        //    }
        //    __result = false;
        //    return false;
        //}
        //[HarmonyPatch(typeof(PlayerAIBot), nameof(PlayerAIBot.IsActionForbidden))]
        //[HarmonyPrefix]
        //public static bool IsActionForbidden(PlayerAIBot instance, PlayerBotActionBase.Descriptor desc, ref bool __result)
        //{
        //    var data = zActions.GetOrCreateData(instance);
        //    for (int i = 0; i < data.m_queuedActions.Count; i++)
        //    {
        //        if (!data.m_queuedActions[i].IsActionAllowed(desc))
        //        {
        //            __result = true;
        //            return false;
        //        }
        //    }
        //    for (int j = 0; j < data.m_actions.Count; j++)
        //    {
        //        if (!data.m_actions[j].IsActionAllowed(desc))
        //        {
        //            __result = true;
        //            return false;
        //        }
        //    }
        //    __result = false;
        //    return false;
        //}
        //[HarmonyPatch(typeof(PlayerAIBot), nameof(PlayerAIBot.OnWarped))]
        //[HarmonyPrefix]
        //public static bool OnWarped(PlayerAIBot instance, Vector3 position)
        //{
        //    var data = zActions.GetOrCreateData(instance);
        //    for (int i = 0; i < data.m_actions.Count; i++)
        //    {
        //        PlayerBotActionBase playerBotActionBase = data.m_actions[i];
        //        if (playerBotActionBase.IsActive())
        //        {
        //            playerBotActionBase.OnWarped(position);
        //        }
        //    }
        //    instance.m_syncValues.Position = position;
        //    instance.ApplyValues();
        //    return false;
        //}
        [HarmonyPatch(typeof(PlayerAIBot), nameof(PlayerAIBot.RemoveCollidingActions))]
        [HarmonyPrefix]
        public static bool RemoveCollidingActions(PlayerAIBot __instance, PlayerBotActionBase.Descriptor desc)
        {
            var data = zActions.GetOrCreateData(__instance);
            bool hasRemoved;
            do
            {
                hasRemoved = false;
                int i = 0;
                while (i < data.m_queuedActions.Count)
                {
                    PlayerBotActionBase.Descriptor descriptor = data.m_queuedActions[i];
                    if (descriptor.Status == PlayerBotActionBase.Descriptor.StatusType.Queued && descriptor.CheckCollision(desc))
                    {
                        data.m_queuedActions.RemoveAt(i);
                        descriptor.OnAborted();
                        hasRemoved = true;
                    }
                    else
                    {
                        i++;
                    }
                }
                int j = 0;
                while (j < data.m_actions.Count)
                {
                    PlayerBotActionBase playerBotActionBase = data.m_actions[j];
                    if (playerBotActionBase.CheckCollision(desc)) //this might be a problem with pointers?
                    {
                        data.m_actions.RemoveAt(j);
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
        //[HarmonyPatch(typeof(PlayerAIBot), nameof(PlayerAIBot.SetEnabled))]
        //[HarmonyPrefix]
        //public static bool SetEnabled(PlayerAIBot instance, bool state)
        //{
        //    var data = zActions.GetOrCreateData(instance);
        //    if (state == instance.enabled)
        //    {
        //        return false;
        //    }
        //    if (state)
        //    {
        //        NavMeshHit navMeshHit;
        //        if (NavMesh.SamplePosition(instance.m_playerAgent.Position, out navMeshHit, 3f, -1))
        //        {
        //            instance.m_syncValues.Position = navMeshHit.position;
        //        }
        //        else
        //        {
        //            instance.m_syncValues.Position = instance.m_playerAgent.Position;
        //        }
        //        instance.m_syncValues.Forward = instance.m_playerAgent.Forward;
        //        instance.m_syncValues.LookDirection = instance.m_playerAgent.TargetLookDir;
        //        instance.m_syncValues.Ladder = instance.m_playerAgent.Locomotion.CurrentLadder;
        //        instance.InitValues();
        //        instance.m_lastSyncedPosition = instance.m_syncValues.Position;
        //    }
        //    else
        //    {
        //        bool hasRemoved;
        //        do
        //        {
        //            hasRemoved = false;
        //            for (int i = 0; i < data.m_queuedActions.Count; i++)
        //            {
        //                if (data.m_queuedActions[i].Pointer != instance.m_rootAction.Pointer)
        //                {
        //                    data.m_queuedActions[i].OnAborted();
        //                    data.m_queuedActions.RemoveAt(i);
        //                    hasRemoved = true;
        //                }
        //            }
        //            for (int j = 0; j < data.m_actions.Count; j++)
        //            {
        //                if (data.m_actions[j].DescBase.Pointer != instance.m_rootAction.Pointer)
        //                {
        //                    data.m_actions[j].Stop();
        //                    data.m_actions[j].DescBase.OnStopped();
        //                    data.m_actions.RemoveAt(j);
        //                    hasRemoved = true;
        //                }
        //            }
        //        }
        //        while (hasRemoved);
        //    }
        //    instance.enabled = state;
        //    return false;
        //}
        [HarmonyPatch(typeof(PlayerAIBot), nameof(PlayerAIBot.StartAction))]
        [HarmonyPrefix]
        public static bool StartAction(PlayerAIBot __instance, PlayerBotActionBase.Descriptor desc)
        {
            var data = zActions.GetOrCreateData(__instance);
            if (!desc.IsTerminated())
            {
                Debug.LogError("Action was queued while active: " + desc);
                return false;
            }
            for (int i = 0; i < data.m_actions.Count; i++)
            {
                if (data.m_actions[i].DescBase == desc)
                {
                    data.m_actions.RemoveAt(i);
                    break;
                }
            }
            desc.OnQueued();
            __instance.RemoveCollidingActions(desc);
            data.m_queuedActions.Add(desc);
            return false;
        }
        //[HarmonyPatch(typeof(PlayerAIBot), nameof(PlayerAIBot.StopAction))]
        //[HarmonyPrefix]
        //public static bool StopAction(PlayerAIBot instance, PlayerBotActionBase.Descriptor desc)
        //{
        //    var data = zActions.GetOrCreateData(instance);
        //    if (desc == PlayerAIBot.s_updatingAction)
        //    {
        //        Debug.LogError("Action was removed during its update: " + desc);
        //    }
        //    if (desc.Status == PlayerBotActionBase.Descriptor.StatusType.Queued)
        //    {
        //        desc.OnAborted();
        //        for (int i = 0; i < data.m_queuedActions.Count; i++)
        //        {
        //            if (data.m_queuedActions[i] == desc)
        //            {
        //                data.m_queuedActions.RemoveAt(i);
        //                return false;
        //            }
        //        }
        //        return false;
        //    }
        //    if (desc.Status == PlayerBotActionBase.Descriptor.StatusType.Active)
        //    {
        //        if (desc.ActionBase == null)
        //        {
        //            Debug.LogError("Active descriptor is missing action: " + desc);
        //        }
        //        data.m_actions.Remove(desc.ActionBase);
        //        desc.ActionBase.Stop();
        //        desc.OnStopped();
        //    }
        //    return false;
        //}
    } // class
}// namespace

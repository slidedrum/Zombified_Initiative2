using GTFO.API.Extensions;
using Player;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Zombified_Initiative;

namespace ZombieTweak2.zRootBotPlayerAction
{
    public class dataStore
    {
        public OrderedSet<PlayerBotActionBase.Descriptor> allActions = new();
        public Il2CppSystem.Collections.Generic.List<PlayerBotActionBase> m_actions { get; set; } = new();
        public Il2CppSystem.Collections.Generic.List<PlayerBotActionBase.Descriptor> m_queuedActions { get; set; } = new();
    }
    public static class zActions
    {
        internal static readonly Dictionary<int, dataStore> ActionDataStore = new();
        public static Dictionary<IntPtr, ICustomPlayerBotActionBase.IDescriptor> StrictDescriptorTypeMap = new();
        public static Dictionary<IntPtr, ICustomPlayerBotActionBase> StrictActionTypeMap = new();
        internal static dataStore GetOrCreateData(PlayerBotActionBase.Descriptor desc)
        {
            PlayerAIBot bot = desc.Bot;
            return GetOrCreateData(bot);
        }
        internal static dataStore GetOrCreateData(PlayerBotActionBase botBase)
        {
            PlayerAIBot bot = botBase.m_bot;
            return GetOrCreateData(bot);
        }
        internal static dataStore GetOrCreateData(PlayerAIBot botBase)
        {
            int botId = botBase.GetInstanceID();
            if (!ActionDataStore.TryGetValue(botId, out var data))
            {
                data = new dataStore();
                ActionDataStore[botId] = data;
            }
            return data;
        }
        public static ICustomPlayerBotActionBase.IDescriptor GetStrictTypeInstance(PlayerBotActionBase.Descriptor looseDescType)
        {
            var pointer = looseDescType.Pointer;
            if (StrictDescriptorTypeMap.TryGetValue(pointer, out var strictDescType))
            {
                return strictDescType;
            }
            return null;
        }
        public static PlayerBotActionBase.Descriptor RegisterStrictTypeInstanceDescriptor(PlayerBotActionBase.Descriptor strictDescType)
        {
            if (strictDescType is ICustomPlayerBotActionBase.IDescriptor descriptor)
                StrictDescriptorTypeMap[strictDescType.Pointer] = descriptor;
            else
                ZiMain.log.LogError($"Trying to register non-ICustomPlayerBotActionBase descriptor: {strictDescType}");
            return strictDescType;
        }
        public static ICustomPlayerBotActionBase GetStrictTypeInstance(PlayerBotActionBase looseActType)
        {
            var pointer = looseActType.Pointer;
            if (StrictActionTypeMap.TryGetValue(pointer, out var strictActType))
            {
                return strictActType;
            }
            return null;
        }
        public static PlayerBotActionBase RegisterStrictTypeInstance(PlayerBotActionBase strictActType)
        {
            if (strictActType is ICustomPlayerBotActionBase descriptor)
                StrictActionTypeMap[strictActType.Pointer] = descriptor;
            else
                ZiMain.log.LogError($"Trying to register non-ICustomPlayerBotActionBase descriptor: {strictActType}");
            return strictActType;
        }
    }
}

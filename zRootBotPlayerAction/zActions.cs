using Player;
using System;
using System.Collections.Generic;
using Zombified_Initiative;

namespace ZombieTweak2.zRootBotPlayerAction
{
    public class dataStore
    {
        public OrderedSet<PlayerBotActionBase.Descriptor> allActions = new();
    }
    public static class zActions
    {
        internal static readonly Dictionary<int, dataStore> ActionDataStore = new();
        private static Dictionary<IntPtr, ICustomPlayerBotActionBase.IDescriptor> StrictDescriptorTypeMap = new();
        private static Dictionary<IntPtr, ICustomPlayerBotActionBase> StrictActionTypeMap = new();
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
        public static PlayerBotActionBase.Descriptor RegisterStrictTypeInstance(PlayerBotActionBase.Descriptor strictDescType)
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

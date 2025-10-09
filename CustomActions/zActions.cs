using Player;
using System;
using System.Collections.Generic;
using Zombified_Initiative;

namespace ZombieTweak2.zRootBotPlayerAction
{
    public class dataStore
    {
        public OrderedSet<CustomActionBase.Descriptor> customActions = new();
        public PlayerBotActionBase.Descriptor bestAction = null;
        public bool consideringActions = false;
        public bool consideringCollectItem = false;
        public PlayerAgent actualLeader = null;
        public Il2CppSystem.Collections.Generic.List<PlayerBotActionBase> m_actions { get; set; } = new();
        public Il2CppSystem.Collections.Generic.List<PlayerBotActionBase.Descriptor> m_queuedActions { get; set; } = new();
    }
    public static class zActions
    {
        public static List<PlayerBotActionBase.Descriptor> manualActions = new();
        internal static readonly Dictionary<int, dataStore> ActionDataStore = new();
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
        public static bool isManualAction(PlayerBotActionBase.Descriptor descriptor)
        {
            if (descriptor == null) return false;
            if (manualActions == null) return false;

            foreach (var desc in manualActions)
            {
                if (desc == null) continue; // just in case

                if (desc.Pointer == descriptor.Pointer)
                    return true;
            }

            if (descriptor.ParentActionBase != null)
            {
                return isManualAction(descriptor.ParentActionBase.DescBase);
            }

            return false;
        }
    }
}

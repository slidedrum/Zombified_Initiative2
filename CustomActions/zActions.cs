using Player;
using System.Collections.Generic;

namespace ZombieTweak2.zRootBotPlayerAction
{
    public class dataStore
    {
        public OrderedSet<CustomActionBase.Descriptor> customActions = new();
        public PlayerBotActionBase.Descriptor bestAction = null;
    }
    public static class zActions
    {
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
    }
}

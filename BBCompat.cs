using BepInEx.Unity.IL2CPP;
using BetterBots.Components;
using Player;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ZombieTweak2
{
    public static class BBCompat
    {

        private static Dictionary<int, BotRecorder> botRecorders = new();
        public static BotRecorder GetBotRecorder(PlayerAIBot bot)
        {
            return GetBotRecorder(bot.Agent);
        }
        public static BotRecorder GetBotRecorder(PlayerAgent agent)
        {
            return GetBotRecorder(agent.PlayerSlotIndex);
        }
        public static BotRecorder GetBotRecorder(int index)
        {
            if (!PlayerManager.TryGetPlayerAgent(ref index, out var agent))
                throw new InvalidOperationException($"Could not find bot at index {index}");
            if (!botRecorders.ContainsKey(index) || botRecorders[index] == null)
                botRecorders[index] = agent.gameObject.GetComponent<BotRecorder>();
            return botRecorders[index];
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool CheckDanger(PlayerAgent agent)
        {
            BotRecorder recorder = GetBotRecorder(agent);
            if (recorder != null)
                return recorder.IsInDangerousSituation();
            return false;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static bool CheckReviveAllowed(PlayerAgent m_agent)
        {
            BotRecorder recorder = BBCompat.GetBotRecorder(m_agent);
            if (recorder != null)
                return !recorder.Brain.ReviveRestricted;
            return true;
        }
    }
}

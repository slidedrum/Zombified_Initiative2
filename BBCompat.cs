using BepInEx.Unity.IL2CPP;
using BetterBots.Components;
using HarmonyLib;
using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace BotControl
{
    public static class BBCompat
    {

        private static Dictionary<int, BotRecorder> botRecorders = new();
        public static void OnInit()
        {
            var original = AccessTools.Method(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.UpdateActionCollectItem));
            ZiMain.m_Harmony.Unpatch(original, HarmonyPatchType.Prefix,"com.east.bb");
        }
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


using System;
using System.Collections.Generic;
using System.Linq;
using ZombieTweak2.zMenu;
using Zombified_Initiative;
using static Zombified_Initiative.ZiMain;

namespace ZombieTweak2.zNetworking
{
    public class zNetworking
    { //This class will handle all incoming and outgoing network requests.
        //todo only update values every 100ms.  
        //if not host ask for host's value after change
        public static Dictionary<int, bool> botSelections = new Dictionary<int, bool>();
        public static long EncodeBotSelectionForNetwork(Dictionary<int, bool> botSelection)
        {
            //this method is AI generated, but I fully understand what it's doing.  I just didn't know how to do bitwise ops.
            const int MaxBots = 21; // 64 bits / 3 bits per bot
            long result = 0;
            int bitPos = 0;

            if (botSelection.Keys.Any(k => k < 0 || k >= MaxBots))
                throw new ArgumentOutOfRangeException(nameof(botSelection), $"All bot keys must be between 0 and {MaxBots - 1}.");

            var keys = botSelection.Keys.OrderBy(k => k).ToList();
            int lastKey = keys.Last();

            for (int i = 0; i < MaxBots; i++)
            {
                bool hasData = botSelection.ContainsKey(i);
                bool data = hasData && botSelection[i];
                bool end = i == lastKey;
                if (hasData) result |= (1L << bitPos);
                bitPos++;
                if (data) result |= (1L << bitPos);
                bitPos++;
                if (end) result |= (1L << bitPos);
                bitPos++;
            }

            return result;
        }
        public static Dictionary<int, bool> DecodeBotSelectionFromNetwork(long encoded)
        {
            const int MaxBots = 21;
            var botSelection = new Dictionary<int, bool>();
            int bitPos = 0;

            for (int i = 0; i < MaxBots; i++)
            {
                bool hasData = (encoded & (1L << bitPos)) != 0;
                bitPos++;
                bool data = (encoded & (1L << bitPos)) != 0;
                bitPos++;
                bool end = (encoded & (1L << bitPos)) != 0;
                bitPos++;

                if (hasData)
                {
                    botSelection[i] = data;
                }

                if (end)
                {
                    break;
                }
            }
            return botSelection;
        }
        public static void reciveTogglePickupPermission(ulong sender, pStructs.pBotSelections info)
        {
            ZiMain.log.LogInfo($"Recived toggle perm wiht pbotsends {info.data}");
            botSelections = DecodeBotSelectionFromNetwork(info.data);
            zSlideComputer.TogglePickupPermission(botSelections); //this works, it's downstream.
        }

        public static void reciveSetItemPrioDissable(ulong sender, pStructs.pItemPrioDisable info)
        {
            ZiMain.log.LogInfo($"Recived set item prio dissabled from network!");
            uint id = info.id;
            bool allowed = info.allowed;
            ZiMain.log.LogInfo($"id:{id}, allowed:{allowed}");
            if (!PermissionsMenuClass.prioNodesByID.ContainsKey(id)) 
            {
                ZiMain.log.LogError("Unknown id recived!");
                return;
            }
            zSlideComputer.SetItemPrioDissabled(id, allowed, sender);
            PermissionsMenuClass.updateNodePriorityDisplay(PermissionsMenuClass.prioNodesByID[id], id);
        }

        internal static void reciveSetBotItemPrio(ulong sender, pStructs.pBotItemPrio info)
        {
            ZiMain.log.LogInfo($"Recived set item prio value from network!");
            uint id = info.id;
            float prio = info.prio;
            ZiMain.log.LogInfo($"id:{id}, allowed:{prio}");
            if (!PermissionsMenuClass.prioNodesByID.ContainsKey(id))
            {
                ZiMain.log.LogError("Unknown id recived!");
                return;
            }
            zSlideComputer.SetBotItemPriority(id, prio, sender);
            PermissionsMenuClass.updateNodePriorityDisplay(PermissionsMenuClass.prioNodesByID[id], id);
        }
    }
}

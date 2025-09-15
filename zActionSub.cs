using Player;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Zombified_Initiative
{
    public static class zActionSub
    {
        public static List<Action<PlayerAIBot, PlayerBotActionBase>> onAdded = new();
        public static List<Action<PlayerAIBot, PlayerBotActionBase>> onRemoved = new();
        public static Dictionary<int, List<PlayerBotActionBase>> botActionMap = new();
        public static void update()
        {
            //there's got to be a better way to get all bots.
            List<PlayerAIBot> playerAiBots = ZiMain.GetBotList();
            var comparer = new Il2CppActionComparer();
            foreach (var bot in playerAiBots)
            {
                var oldList = new List<PlayerBotActionBase>();
                botActionMap.TryGetValue(bot.GetInstanceID(), out oldList);
                if (oldList == null)
                {
                    oldList = new List<PlayerBotActionBase>();
                }
                var newList = new List<PlayerBotActionBase>();
                foreach (var action in bot.Actions)
                {
                    newList.Add(action);
                }
                var addedItems = newList.Except(oldList, comparer).ToList();
                var removedItems = oldList.Except(newList, comparer).ToList();
                foreach (var item in addedItems)
                {
                    onAdd(bot, item);
                }
                foreach (var item in removedItems)
                {
                    onRemove(bot, item);
                }
                botActionMap[bot.GetInstanceID()] = newList;
            }
        }
        public static void addOnAdded(Action<PlayerAIBot, PlayerBotActionBase> action)
        {
            onAdded.Add(action);
        }
        public static void addOnRemoved(Action<PlayerAIBot, PlayerBotActionBase> action)
        {
            onRemoved.Add(action);
        }
        public static void onAdd(PlayerAIBot bot, PlayerBotActionBase botAction)
        {
            foreach (var action in onAdded)
            {
                action(bot, botAction);
            }
        }
        public static void onRemove(PlayerAIBot bot, PlayerBotActionBase botAction)
        {
            foreach (var action in onRemoved)
            {
                action(bot, botAction);
            }
        }
    }
    public class Il2CppActionComparer : IEqualityComparer<PlayerBotActionBase>
    {
        public bool Equals(PlayerBotActionBase x, PlayerBotActionBase y)
        {
            if (x == null || y == null) return x == y;
            return x.Pointer == y.Pointer;
        }

        public int GetHashCode(PlayerBotActionBase obj)
        {
            if (obj == null) return 0;
            return obj.Pointer.GetHashCode();
        }
    }
}

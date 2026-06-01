using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BotControl.SmartSelect.PressActions
{
    public abstract class PressAction
    {
        private static Dictionary<string, PressAction> ActionMap;
        public abstract string FriendlyName { get; }
        public virtual string FriendlyNameShort => FriendlyName;
        protected PressAction() {}
        public static void Initialize()
        {
            if (ActionMap != null)
                return;
            ActionMap = new Dictionary<string, PressAction>();
            var baseType = typeof(PressAction);
            var types = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => !t.IsAbstract && baseType.IsAssignableFrom(t));
            foreach (var type in types)
            {
                var instance = (PressAction)Activator.CreateInstance(type, nonPublic: true);
                var key = instance.FriendlyName;
                if (ActionMap.ContainsKey(key))
                    throw new Exception($"Duplicate PressAction key: {key}");
                ActionMap[key] = instance;
            }
        }
        public static PressAction GetAction(string name)
        {
            if (ActionMap == null)
                Initialize();

            if (!ActionMap.TryGetValue(name, out var action))
                ZiMain.log.LogError($"Could not find action {name} in Press Action Map.");
            return action;
        }
        public abstract bool Invoke(Component BestComponenet);
    }
}

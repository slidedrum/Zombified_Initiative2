using System;
using System.Collections.Generic;
using UnityEngine;
using Zombified_Initiative;

namespace ZombieTweak2
{
    public class zUpdater : MonoBehaviour
    {   
        //This class handles stuff that would normally be handled by making things a monobehavior
        //For whatever reason I can't use custom types as args in methods in Il2cpp (╯°□°)╯︵ ┻━┻
        //So i'm doing this instead.  If there's a better way, lmk.  Maybe I'll refactor again.

        public static FlexibleEvent onUpdate = new();
        public static FlexibleEvent onLateUpdate = new();
        public static zUpdater Instance = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                ZiMain.log.LogWarning("Multiple updater instances created");
                return;
            }
            Instance = this;
        }
        public static void CreateInstance()
        {
            if (Instance == null)
            {
                GameObject go = new GameObject("zUpdater");
                Instance = go.AddComponent<zUpdater>();
            }
        }
        private void Update()
        {
            onUpdate?.Invoke();
        }

        private void LateUpdate()
        {
            onLateUpdate?.Invoke();
        }
        public static void InvokeStatic(string methodName, float time)
        {
            if (Instance == null)
                CreateInstance();
            Instance.Invoke(methodName, time);
        }
    }
    public static class Default
    {
        //This was added by an AI, not 100% sure why it's needed. ¯\_(ツ)_/¯
        //but I don't understand it enough to get rid of it (yet).
        public static readonly object Value = new object();
    }
    public class FlexibleMethodDefinition
    {
        // This is a mostly AI generated class.
        // This is a much more easy to use version of action that handles return types, arbitrary argument types and ammounts.

        public Delegate method;
        public object[] args;

        public FlexibleMethodDefinition(Delegate method)
        {
            this.method = method;
            this.args = Array.Empty<object>();
        }
        public FlexibleMethodDefinition(Delegate method, object[] args)
        {
            this.method = method;
            this.args = args;
        }
        public static implicit operator FlexibleMethodDefinition(Delegate d)
        {
            return new FlexibleMethodDefinition(d);
        }
    }
    public class FlexibleEvent
    {
        // This is a mostly AI generated class.
        // This is a much more easy to use version of action that handles return types, arbitrary argument types and ammounts.

        private readonly HashSet<Action> listeners = new();//this used to be a list, but I think hash set is better.
                                                           //if you need to do the same thing twice, do it inside your own method.
                                                           //Don't add the same callback twice.
        public void Listen(Action method)
        {
            if (method == null) return;
            listeners.Add(method);
        }
        public void Listen(FlexibleMethodDefinition method)
        {
            Listen(method.method, method.args);
        }
        public void Listen(Delegate method, params object[] args)
        {
            if (method == null) return;

            var parameters = method.Method.GetParameters();
            object[] finalArgs = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                if (i < args.Length)
                {
                    if (args[i] != Default.Value)
                        finalArgs[i] = args[i];
                    else if (parameters[i].IsOptional)
                        finalArgs[i] = parameters[i].DefaultValue;
                    else
                        throw new ArgumentException($"Missing required argument '{parameters[i].Name}'");
                }
                else
                {
                    if (parameters[i].IsOptional)
                        finalArgs[i] = parameters[i].DefaultValue;
                    else
                        throw new ArgumentException($"Missing required argument '{parameters[i].Name}'");
                }
            }
            void Wrapper() => method.DynamicInvoke(finalArgs);
            listeners.Add(Wrapper);
        }
        public void Unlisten(Action method)
        {
            listeners.Remove(method);
        }
        public void Unlisten(FlexibleMethodDefinition method)
        {
            listeners.Remove((Action)method.method);
        }
        public void ClearListeners()
        {
            listeners.Clear();
        }
        public void Invoke()
        {
            foreach (var listener in listeners)
            {
                listener.Invoke();
            }
        }
    }
}

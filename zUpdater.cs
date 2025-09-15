using LevelGeneration;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zombified_Initiative;
using static Il2CppSystem.Globalization.CultureInfo;

namespace ZombieTweak2
{

    public class zUpdater : MonoBehaviour
    {
        public static FlexibleEvent onUpdate = new();
        public static FlexibleEvent onLateUpdate = new();
        public static zUpdater Instance = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                Zi.log.LogWarning("Multiple updater instances created");
                return;
            }
            Instance = this;
        }
        public static void CreateInstance()
        {
            if (Instance == null)
            {
                // Create a new GameObject and attach zUpdater
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

    /// --- AI GENERATED ---
    /// <summary>
    /// Sentinel to indicate optional/default argument
    /// </summary>
    public static class Default
    {
        public static readonly object Value = new object();
    }

    /// <summary>
    /// Fully managed flexible event system that works in BepInEx IL2CPP environments
    /// </summary>
    public class FlexibleMethodDefinition
    {
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
        // Each listener stores a wrapper delegate (pure C#) and pre-bound arguments
        private readonly List<Action> listeners = new();

        /// <summary>
        /// Subscribe a method with preset arguments
        /// </summary>
        public void Listen(Action method)
        {
            if (method == null) return;
            listeners.Add(method);
        }
        public void Listen(FlexibleMethodDefinition method)
        {
            Listen(method.method, method.args);
        }

        /// <summary>
        /// Subscribe a method with arguments (supports optional parameters via Default.Value)
        /// </summary>
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

            // Wrap the method in a pure Action to avoid IL2CPP delegate issues
            void Wrapper() => method.DynamicInvoke(finalArgs);
            listeners.Add(Wrapper);
        }

        /// <summary>
        /// Unsubscribe a listener (works for parameterless Actions only)
        /// </summary>
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

        /// <summary>
        /// Invoke all listeners
        /// </summary>
        public void Invoke()
        {
            foreach (var listener in listeners)
            {
                listener.Invoke();
            }
        }
    }
    /// --- END AI GENERATED ---
}

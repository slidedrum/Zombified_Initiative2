using Player;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zombified_Initiative;
namespace ZombieTweak2
{
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

        private readonly OrderedSet<Action> listeners = new();//this used to be a list, but I think OrderedSet is better.
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
    public class OrderedSet<T> : IEnumerable<T>, IEnumerable
    {
        //This didn't exist for some reason, so I had an AI make it.  I mostly understand it.  
        //TODO remake it not with AI.

        private readonly List<T> _list = new();
        private readonly Dictionary<T, int> _dict = new();

        public int Count => _list.Count;

        public bool Add(T item)
        {
            if (_dict.ContainsKey(item))
                return false;

            _list.Add(item);
            _dict[item] = _list.Count - 1;
            return true;
        }

        public bool Remove(T item)
        {
            if (!_dict.TryGetValue(item, out int index))
                return false;

            _dict.Remove(item);

            int lastIndex = _list.Count - 1;
            if (index != lastIndex)
            {
                T lastItem = _list[lastIndex];
                _list[index] = lastItem;
                _dict[lastItem] = index;
            }

            _list.RemoveAt(lastIndex);
            return true;
        }

        public void Clear()
        {
            _list.Clear();
            _dict.Clear();
        }
        public bool Contains(T item) => _dict.ContainsKey(item);

        public T this[int index] => _list[index];

        // Generic enumerator
        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();

        // Non-generic enumerator
        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

        public List<T> ToList() => new(_list);
    }
    public static class Default
    {
        //This was added by an AI, not 100% sure why it's needed. ¯\_(ツ)_/¯
        //but I don't understand it enough to get rid of it (yet).
        public static readonly object Value = new object();
    }
}

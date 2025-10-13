using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using UnityEngine;
namespace ZombieTweak2
{
    public static class zHelpers
    {
        public static float Round(float value, int decimalPlaces)
        {
            float multiplier = Mathf.Pow(10f, decimalPlaces);
            return Mathf.Round(value * multiplier) / multiplier;
        }
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
        public object? Invoke()
        {
            return Invoke(args);
        }

        public object? Invoke(params object[] suppliedArgs)
        {
            if (method == null)
                throw new InvalidOperationException("No method assigned to FlexibleMethodDefinition.");

            var finalArgs = suppliedArgs?.Length > 0 ? suppliedArgs : args;

            try
            {
                return method.DynamicInvoke(finalArgs);
            }
            catch (TargetParameterCountException e)
            {
                throw new InvalidOperationException(
                    $"Parameter count mismatch. Expected {method.Method.GetParameters().Length}, got {finalArgs?.Length ?? 0}.", e);
            }
            catch (Exception e)
            {
                throw new TargetInvocationException($"Error invoking method '{method.Method.Name}'.", e);
            }
        }
    }
    public class FlexibleEvent
    {
        private readonly OrderedSet<Action> listeners = new();

        public void Listen(Action method)
        {
            if (method == null) return;
            listeners.Add(method);
        }

        public void Listen(FlexibleMethodDefinition method)
        {
            Listen(method.method, method.args, method);
        }

        public void Listen(Delegate method, object[] args, FlexibleMethodDefinition fMethod = null)
        {
            if (method == null) return;

            var parameters = method.Method.GetParameters();
            object[] finalArgs = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                if (i < args.Length && args[i] != Default.Value)
                    finalArgs[i] = args[i];
                else if (parameters[i].IsOptional)
                    finalArgs[i] = parameters[i].DefaultValue;
                else
                    throw new ArgumentException($"Missing required argument '{parameters[i].Name}'");
            }

            void Wrapper()
            {
                method.DynamicInvoke(finalArgs);

                // Copy back ref/out changes to original args array
                if (fMethod?.args != null)
                {
                    for (int i = 0; i < finalArgs.Length && i < fMethod.args.Length; i++)
                        fMethod.args[i] = finalArgs[i];
                }
            }

            listeners.Add(Wrapper);
        }

        public void Unlisten(Action method)
        {
            listeners.Remove(method);
        }

        public void Unlisten(FlexibleMethodDefinition method)
        {
            // Note: This only works if the same wrapper instance is stored.
            // You may need to track the wrapper separately to remove it correctly.
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
        /// <summary>
        /// Adds an item to the end of the queue if not already present.
        /// </summary>
        public bool Enqueue(T item)
        {
            return Add(item);
        }

        /// <summary>
        /// Removes and returns the item at the front of the queue.
        /// Throws InvalidOperationException if empty.
        /// </summary>
        public T Dequeue()
        {
            if (_list.Count == 0)
                throw new InvalidOperationException("The OrderedSet is empty.");

            T item = _list[0];
            Remove(item);
            return item;
        }

        /// <summary>
        /// Returns (but does not remove) the item at the front of the queue.
        /// </summary>
        public T Peek()
        {
            if (_list.Count == 0)
                throw new InvalidOperationException("The OrderedSet is empty.");

            return _list[0];
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
    #nullable enable
    public class OverrideChain<T>
    {
        private OrderedDictionary Chain = new();
        private Dictionary<string, Func<bool>?> Conditionals = new();
        private OverrideChain<T>? parrentChain = null;
        private OverrideChain<T>? childChain = null;
        private bool AllowSameValueOveride = false;
        public List<string> Keys => Chain.Keys.Cast<string>().ToList();
        public OverrideChain()
        {
        }
        public OverrideChain(OverrideChain<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            this.Chain = new OrderedDictionary();
            foreach (DictionaryEntry entry in other.Chain)
            {
                this.Chain.Add(entry.Key, entry.Value);
            }
            this.Conditionals = new Dictionary<string, Func<bool>?>(other.Conditionals);
            this.AllowSameValueOveride = other.AllowSameValueOveride;
        }
        public OverrideChain(string key, T? variable, Func<bool>? condition = null, bool sameValueOveride = false)
        {
            Chain[key] = variable;
            if (condition == null)
                return;
            Conditionals[key] = condition;
            AllowSameValueOveride = sameValueOveride;
        }
        public void AllowSameValue(bool sameValueOveride)
        {
            AllowSameValueOveride = sameValueOveride;
        }
        public OverrideChain<T>? CreateParentChain()
        {
            if (parrentChain != null)
                Console.WriteLine("Warning: overiding existing parrent chain");
            OverrideChain<T> newChain = new OverrideChain<T>(this);
            SetParrentChain(newChain);
            newChain.SetChildChain(this);
            return newChain;
        }
        public OverrideChain<T>? CreateChildChain()
        {
            OverrideChain<T> newChain = new OverrideChain<T>(this);
            newChain.SetParrentChain(this);
            SetChildChain(newChain);
            return newChain;
        }
        public int Count()
        {
            return Chain.Count;
        }
        public void InsertOverride(string key, int index, T? variable, Func<bool>? condition = null)
        {
            Chain.Insert(index, key, variable);
            if (condition == null)
                return;
            Conditionals[key] = condition;
        }
        public OverrideChain<T>? RemoveChildChain()
        {
            if (childChain == null)
                return null;
            var ret = childChain;
            childChain.RemoveParrentChain();
            childChain = null;
            return ret;
        }
        public OverrideChain<T>? RemoveParrentChain()
        {
            if (parrentChain == null)
                return null;
            var ret = parrentChain;
            parrentChain.RemoveChildChain();
            parrentChain = null;
            return ret;
        }

        public void SetParrentChain(OverrideChain<T> parent)
        {
            if (ReferenceEquals(parent, this))
                throw new InvalidOperationException("A chain cannot be its own parent.");
            parrentChain = parent;
            if (parent.childChain != this)
                parent.childChain = this;
        }
        public void SetChildChain(OverrideChain<T> child)
        {
            if (ReferenceEquals(child, this))
                throw new InvalidOperationException("A chain cannot be its own child.");
            childChain = child;
            if (child.parrentChain != this)
                child.parrentChain = this;
        }
        [Flags]
        public enum PropagationDirection
        {
            None = 0,
            Up = 1,
            Down = 2,
            Both = Up | Down
        }
        public void SetConditional(string key, Func<bool>? condition, PropagationDirection propagate = PropagationDirection.Both)
        {
            Conditionals[key] = condition;
            if (propagate.HasFlag(PropagationDirection.Up))
                parrentChain?.SetConditional(key, condition, PropagationDirection.Up);
            if (propagate.HasFlag(PropagationDirection.Down))
                childChain?.SetConditional(key, condition, PropagationDirection.Down);
        }
        public T? SetValue(string key, T? value)
        {
            Chain[key] = null;
            if (AllowSameValueOveride || !object.Equals(GetValue(key), value))
                Chain[key] = value;
            return value;
        }
        public void RemoveOverride(string key)
        {
            Conditionals.Remove(key);
            Chain.Remove(key);
        }
        public void RemoveOverride(int index)
        {
            Conditionals.Remove(Keys[index]);
            Chain.Remove(index);
        }
        public void AddOverride(string key, T? variable, Func<bool>? condition = null)
        {
            Chain[key] = variable;
            if (condition == null)
                return;
            Conditionals[key] = condition;
        }
        public T? GetValue(string? key = null)
        {
            if (!string.IsNullOrEmpty(key))
            {
                var val = ValueAt(key);
                if (val != null)
                    return val;
            }
            var reversedKeys = Enumerable.Reverse(Keys);
            foreach (var k in reversedKeys)
            {
                var val = ValueAt(k);
                if (val != null)
                    return val;
            }
            return default;
        }
        public T? ValueAt(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));
            if (Chain.Contains(key))
            {
                var obj = Chain[key];
                if (obj is T value && (!Conditionals.TryGetValue(key, out var cond) || cond?.Invoke() != false))
                {
                    return value;
                }
            }
            if (parrentChain != null && parrentChain.Chain.Contains(key))
                return parrentChain.ValueAt(key);
            return default;
        }

    }
}

using GTFO.API;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using ZombieTweak2.zNetworking;
using Zombified_Initiative;
#nullable enable
namespace ZombieTweak2
{
    public static class zHelpers
    {
        public static float Round(float value, int decimalPlaces)
        {
            float multiplier = Mathf.Pow(10f, decimalPlaces);
            return Mathf.Round(value * multiplier) / multiplier;
        }
        public static uint HashString(string str)
        {
            unchecked
            {
                uint hash = 2166136261;

                for (int i = 0; i < str.Length; i++)
                {
                    hash ^= str[i];
                    hash *= 16777619;
                }

                return hash;
            }
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

        public void Listen(Delegate method, object[] args, FlexibleMethodDefinition? fMethod = null)
        {
            if (method == null) return;

            var parameters = method.Method.GetParameters();
            object[] finalArgs = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                if (i < args.Length && args[i] != Default.Value)
                    finalArgs[i] = args[i];
                else if (parameters[i].IsOptional)
#pragma warning disable CS8601 // Possible null reference assignment.
                    finalArgs[i] = parameters[i].DefaultValue;
#pragma warning restore CS8601 // Possible null reference assignment.
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
#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
        private readonly Dictionary<T, int> _dict = new();
#pragma warning restore CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.

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

    public class OverrideTree<T>
    {
        internal static Dictionary<int, OverrideTree<T>> Trees = new();
        private static int NextTreeID { get { return Trees.Keys.Count; } }
        private int ID;
        private int NextNodeID = 1;
        public Dictionary<uint, Node> nodesByID { get; private set; } = new(); //For O(1) lookup by ID, used for network syncing
        public Dictionary<string, Node> nodes { get; private set; } = new(StringComparer.Ordinal); //For O(1) lookup, starting search in the middle of a tree
        public Node rootNode { get; private set; }
        public class Node
        {
            public uint ID { get; private set; }
            public string Key { get; private set; }
            public T? Value { get; set; }
            public Func<bool>? Condition { get; }
            public bool IsRoot => Parent == null;
            public Node? Parent { get; private set; }
            public OverrideTree<T> Tree { get; internal set; }
            public List<Node> Children { get; } = new();
            internal Node(string key, int id, T? value, Func<bool>? condition = null) //If you don't supply a parent, you MUST supply a value
            {
                Key = key;
                Value = value;
                Condition = condition;
            }
            internal Node(string key, int id, Node parent, T? value = default, Func<bool>? condition = null) //If you supply a parent, you can opt to not supply a value
            {
                Key = key;
                Value = value;
                Condition = condition;
                Parent = parent;
            }
            public T? GetValue() //Traverse down the tree to get deepest value
            {
                T? ret = ValueAt();
                foreach (var node in Children)
                {
                    if (node.Condition != null && !node.Condition.Invoke())
                        continue;
                    var childValue = node.GetValue();
                    if (childValue != null)
                    {
                        ret = childValue;
                    }
                }
                return ret;
            }
            public T? ValueAt() //Traverse up the tree to get value at given node
            {
                if (Value != null) return Value;
                if (Parent == null)
                    throw new InvalidOperationException("Root node has null value.");
                return Parent.ValueAt();
            }
            public void SetValue(T? value)
            {
                Value = value;
            }
            public bool HasValue()
            {
                return Value != null;
            }
            public bool IsDefaultValue()
            {
                if (HasValue() == false)
                    return true;
                if (Parent == null)
                    return false;
                return EqualityComparer<T?>.Default.Equals(Parent.ValueAt(), Value);
            }
        }
        public OverrideTree(T rootValue, string rootKey = "Default")
        {
            ID = NextTreeID;
            Trees[ID] = this;
            if (rootValue == null)
                throw new ArgumentNullException(nameof(rootValue), "Initial value can not be null");
            rootNode = new Node(rootKey, 0, rootValue);
            nodes[rootKey] = rootNode;
            nodesByID[0] = rootNode;
        }
        public static OverrideTree<T> GetTreeFromID(int ID)
        {
            return Trees[ID];
        }
        public Node GetNodeFromId(uint id)
        {
            return nodesByID[id];
        }
        public Node AddNode(string key, T? value, string parent, Func<bool>? condition = null)
        {
            if (!nodes.ContainsKey(parent))
                throw new KeyNotFoundException(nameof(parent));
            var parrentNode = nodes[parent];
            return AddNode(key, value, parrentNode, condition);
        }
        public Node AddNode(string key, T? value, Node? parent = null, Func<bool>? condition = null)
        {
            if (nodes.ContainsKey(key))
                throw new InvalidOperationException($"Key '{key}' already in use.");
            if (parent == null)
                parent = rootNode;
            if (!nodes.Values.Contains(parent))
                throw new InvalidOperationException($"Parent '{parent.Key}' not found.");

            var node = new Node(key, NextNodeID++, parent, value, condition);
            parent.Children.Add(node);
            nodes[key] = node;
            nodesByID[node.ID] = node;
            node.Tree = this;
            return node;
        }
        public T? SetValue(uint ID, T? value, ulong netSender = 0)
        { 
            return SetValue(nodesByID[ID].Key, value, netSender);
        }
        public T? SetValue(string key, T? value, ulong netSender = 0)
        {
            if (!nodes.ContainsKey(key))
                throw new KeyNotFoundException(nameof(key));
            Node node = nodes[key];
            node.SetValue(value);
            if (netSender == 0) // We need to sync these values between clients.
            {
                Type type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Boolean:
                        {
                            pStructs.pBoolOverideTreeInfo info = new pStructs.pBoolOverideTreeInfo();
                            info.treeID = ID;
                            info.keyId = node.ID;
                            if (value is null)
                            {
                                info.value = false;
                                info.isNull = true;
                            }
                            else if (value is bool v)
                            {
                                info.value = v;
                                info.isNull = false;
                            }
                            else
                            {
                                throw new InvalidCastException($"Expected bool value for key '{key}', but got {value.GetType().Name}.");
                            }
                            NetworkAPI.InvokeEvent<pStructs.pBoolOverideTreeInfo>("RequestToSetBoolOverideTree", info);
                            break;
                        }
                    case TypeCode.Int32:
                        {
                            pStructs.pIntOverideTreeInfo info = new pStructs.pIntOverideTreeInfo();
                            info.treeID = ID;
                            info.keyId = node.ID;
                            if (value is null)
                            {
                                info.value = 0;
                                info.isNull = true;
                            }
                            else if (value is int v)
                            {
                                info.value = v;
                                info.isNull = false;
                            }
                            else
                            {
                                throw new InvalidCastException($"Expected int value for key '{key}', but got {value.GetType().Name}.");
                            }
                            NetworkAPI.InvokeEvent<pStructs.pIntOverideTreeInfo>("RequestToSetIntOverideTree", info);
                            break;
                        }
                    case TypeCode.Single:
                        {
                            pStructs.pFloatOverideTreeInfo info = new pStructs.pFloatOverideTreeInfo();
                            info.treeID = ID;
                            info.keyId = node.ID;
                            if (value is null)
                            {
                                info.value = 0f;
                                info.isNull = true;
                            }
                            else if (value is float v)
                            {
                                info.value = v;
                                info.isNull = false;
                            }
                            else
                            {
                                throw new InvalidCastException($"Expected float value for key '{key}', but got {value.GetType().Name}.");
                            }
                            NetworkAPI.InvokeEvent<pStructs.pFloatOverideTreeInfo>("RequestToSetFloatOverideTree", info);
                            break;
                        }
                    default:
                        {
                            ZiMain.log.LogWarning($"set unusual type ({value?.GetType().Name ?? "null"}) in override tree.");
                            break;
                        }
                        
                }
            }
            return ValueAt(key);
        }
        public T? GetValue()
        {
            return rootNode.GetValue();
        }
        public T? ValueAt(string key)
        {
            if (!nodes.ContainsKey(key))
                throw new KeyNotFoundException(nameof(key));
            return nodes[key].ValueAt();
        }
    }
}

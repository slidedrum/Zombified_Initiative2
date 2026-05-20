using GTFO.API;
using System;
using System.Collections.Generic;
using System.Linq;
using ZombieTweak2.zNetworking;
using Zombified_Initiative;

namespace ZombieTweak2
{

    //TODO NEXT figure out where the default prio value of "5" is coming from and fix it


    public class OverrideTree<T>
    {
        internal static Dictionary<uint, OverrideTree<T>> Trees = new();
        private uint treeID;
        private string identifier = "DefaultIdent";
        public Dictionary<uint, Node> nodesByID { get; private set; } = new(); //For O(1) lookup by ID, used for network syncing
        public Dictionary<string, Node> nodes { get; private set; } = new(StringComparer.Ordinal); //For O(1) lookup, starting search in the middle of a tree
        public Node rootNode { get; private set; }
        public class Node
        {
            public uint nodeID { get; private set; }
            public string nodeIdentity { get; private set; }
            public T? Value { get; set; }
            private bool hasDefaultValue { get; }
            private T? _DefaultValue { get; }
            public T? DefaultValue { get 
                {  
                    if (hasDefaultValue)
                        return _DefaultValue;
                    if (Parent != null)
                        return Parent.DefaultValue;
                    return default(T);
                } }
            public T? InitalValue { get;}
            public FlexibleEvent onChanged = new();
            public FlexibleEvent onThisNodeChanged = new();
            public Func<bool>? Condition { get; }
            public bool IsRoot => Parent == null;
            public Node? Parent { get; private set; }
            public OverrideTree<T> Tree { get; internal set; }
            public List<Node> Children { get; } = new();
            internal Node(string key, Node parent = null, T? value = default, Func<bool>? condition = null, T? defaultValue = default, bool hasDefaultValue = false) //If you supply a parent, you can opt to not supply a value
            {
                if (defaultValue != null)
                    hasDefaultValue = true;
                this.hasDefaultValue = hasDefaultValue;
                nodeIdentity = key;
                Value = value;
                Condition = condition;
                Parent = parent;
                InitalValue = value;
                _DefaultValue = defaultValue;
                nodeID = zHelpers.HashString(GetNodeTreeString());
                this.hasDefaultValue = hasDefaultValue;
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
            public void SetValue(T? newValue)
            {
                var callList = OnChanged();
                Value = newValue;
                onThisNodeChanged.Invoke();
                foreach (var call in callList)
                    call.Invoke();
            }
            private HashSet<FlexibleEvent> OnChanged(bool fromParrent = false, HashSet<FlexibleEvent> callList = null)
            {
                if (callList == null)
                    callList = new HashSet<FlexibleEvent>();
                if (Value != null && fromParrent)
                    return callList;
                callList.Add(onChanged);
                foreach (var child in Children)
                    child.OnChanged(true, callList);
                return callList;
            }
            public bool HasValue()
            {
                return Value != null;
            }
            public bool IsDefaultValue()
            {
                if (HasValue() == false)
                    return true;
                //if (Parent == null)
                //    return false;
                //return EqualityComparer<T?>.Default.Equals(Parent.ValueAt(), Value);
                return EqualityComparer<T?>.Default.Equals(DefaultValue, Value);
            }
            internal string GetNodeTreeString()
            {
                if (Parent == null)
                    return nodeIdentity;
                return Parent.GetNodeTreeString() + "/" + nodeIdentity;
            }
        }
        public static void ResetTrees()
        {
            Trees.Clear();
        }
        public OverrideTree(T rootValue, string identifier, string rootKey = "Default", FlexibleMethodDefinition OnChanged = null)
        {
            this.identifier = identifier;
            treeID = zHelpers.HashString(identifier);
            Trees[treeID] = this;
            if (rootValue == null)
                throw new ArgumentNullException(nameof(rootValue), "Initial value can not be null");
            rootNode = new Node(rootKey, value: rootValue, defaultValue: rootValue);
            nodes[rootKey] = rootNode;
            nodesByID[0] = rootNode;
            if (OnChanged != null)
                rootNode.onChanged.Listen(OnChanged);
        }
        public static OverrideTree<T> GetTreeFromID(uint ID)
        {
            return Trees[ID];
        }
        public Node GetNodeFromIdent(string ident)
        {
            if (!nodes.ContainsKey(ident))
                throw new KeyNotFoundException(nameof(ident));
            return nodes[ident];
        }
        public Node GetNodeFromId(uint id)
        {
            return nodesByID[id];
        }
        public Node AddNode(string key, T? value, string? parent = null, Func<bool>? condition = null, FlexibleMethodDefinition onChanged = null, T? defaultValue = default, bool hasDefaultValue = false)
        {
            if (defaultValue != null)
                hasDefaultValue = true;
            Node parrentNode = null;
            if (parent != null)
            {
                if (!nodes.ContainsKey(parent))
                    throw new KeyNotFoundException($"Could not find parrent named {parent} when adding node {key}");
                parrentNode = nodes[parent];
            }

            return AddNode(key, value, parrentNode, condition, onChanged, defaultValue,  hasDefaultValue);
        }
        public Node AddNode(string key, T? value, Node? parent = null, Func<bool>? condition = null, FlexibleMethodDefinition onChanged = null, T? defaultValue = default, bool hasDefaultValue = false)
        {
            if (defaultValue != null)
                hasDefaultValue = true;
            if (nodes.ContainsKey(key))
                if (parent == null)
                    throw new InvalidOperationException($"Key '{key}' already in use.");
                else
                    throw new InvalidOperationException($"Key '{key}' already in use. Consider combineing with the parrent key for '{parent.nodeIdentity}/{key}'");
                
            if (parent == null)
                parent = rootNode;
            if (!nodes.Values.Contains(parent))
                throw new InvalidOperationException($"Parent '{parent.nodeIdentity}' not found.");

            var node = new Node(key, parent, value, condition, defaultValue, hasDefaultValue);
            parent.Children.Add(node);
            nodes[key] = node;
            nodesByID[node.nodeID] = node;
            node.Tree = this;
            if (onChanged != null)
                node.onChanged.Listen(onChanged);
            return node;
        }
        public T? SetValue(uint nodeID, T? value, ulong netSender = 0)
        {
            if (!nodesByID.ContainsKey(nodeID))
            {
                ZiMain.log.LogWarning($"Tried to set unknown nodeID '{nodeID}' in '{treeID} ({identifier}) to '{value}' via netSender {netSender} ");
                return default(T);
            }
            //throw new KeyNotFoundException(nameof(nodeID));
            var node = nodesByID[nodeID];
            ZiMain.log.LogDebug($"Setting value of node by ID '{nodeID}' ({node.GetNodeTreeString()}) in tree {treeID} ({identifier}) to '{value}' (netSender: {netSender})");
            return SetValue(nodesByID[nodeID].nodeIdentity, value, netSender);
        }
        public T? ResetToDefault(string key, ulong netSender = 0)
        {
            if (!nodes.ContainsKey(key))
            {
                ZiMain.log.LogWarning($"Tried to reset unknown key '{key}' in '{treeID} ({identifier}) to default via netSender {netSender} ");
                return default(T);
            }
            Node node = nodes[key];
            return SetValue(key, node.DefaultValue, netSender);
        }
        public T? SetValue(string key, T? value, ulong netSender = 0)
        {
            if (!nodes.ContainsKey(key))
            {
                ZiMain.log.LogWarning($"Tried to set unknown key '{key}' in '{treeID} ({identifier}) to '{value}' via netSender {netSender} ");
                return default(T);
            }
            //throw new KeyNotFoundException(nameof(key));
            Node node = nodes[key];
            ZiMain.log.LogDebug($"Setting value of node by key '{node.GetNodeTreeString()}' ({node.nodeID}) in tree {treeID} ({identifier}) to '{value}' (netSender: {netSender})");
            node.SetValue(value);
            if (netSender == 0) // We need to sync these values between clients.
            {
                Type type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Boolean:
                        {
                            pStructs.pBoolOverideTreeInfo info = new pStructs.pBoolOverideTreeInfo();
                            info.treeID = treeID;
                            info.keyId = node.nodeID;
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
                            info.treeID = treeID;
                            info.keyId = node.nodeID;
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
                            info.treeID = treeID;
                            info.keyId = node.nodeID;
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
            //node.menuNode?.UpdateNode();
            return ValueAt(key);
        }
        public T? GetValue()
        {
            return rootNode.GetValue();
        }
        public bool HasKey(string key)
        {
            return nodes.ContainsKey(key);
        }
        public bool IsDefaultValue(string key)
        {
            Node node = NodeAt(key);
            return node.IsDefaultValue();
        }
        private Node NodeAt(string key, bool shouldThrow = true)
        {
            if (!nodes.ContainsKey(key))
                if (shouldThrow)
                    throw new KeyNotFoundException(nameof(key));
                else
                    return default;
            return nodes[key];
        }
        public T? ValueAt(string key, bool shouldThrow = true)
        {
            return NodeAt(key, shouldThrow).ValueAt();
        }
    }
}

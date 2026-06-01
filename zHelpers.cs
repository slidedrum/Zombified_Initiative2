using Il2CppInterop.Runtime;
using Player;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
#nullable enable
namespace BotControl
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
        public static bool IsOfType<T>(Il2CppSystem.Type type)
        {
            Il2CppSystem.Type target = Il2CppType.Of<T>();
            return type == target || type.IsSubclassOf(target);
        }
        public static uint GetAgentBackpackItemId(PlayerAgent agent, InventorySlot slot)
        {
            return GetAgentBackpackItem(agent, slot)?.ItemID ?? 0;
        }
        public static BackpackItem GetAgentBackpackItem(PlayerAgent agent, InventorySlot slot)
        {
            PlayerBackpack backpack = PlayerBackpackManager.GetBackpack(agent.Owner);
            if (backpack.TryGetBackpackItem(slot, out BackpackItem backpackItem))
                return backpackItem;
            return null;
        }
        public static bool PositionIsValidForAgent(PlayerAgent Agent, ref Vector3 Position)
        {
            NavMeshHit navMeshHit;
            if (!NavMesh.SamplePosition(Position, out navMeshHit, 0.2f, -1))
                return false;
            Position = navMeshHit.position;
            NavMeshPath navMeshPath = new NavMeshPath();
            if (!NavMesh.CalculatePath(Agent.GoodPosition, Position, 17, navMeshPath))
                return false;
            if (navMeshPath.status != NavMeshPathStatus.PathComplete)
                return false;
            return true;
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
}

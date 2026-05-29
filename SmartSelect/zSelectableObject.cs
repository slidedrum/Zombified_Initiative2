using System;
using UnityEngine;

namespace BotControl.SmartSelect
{
    public abstract class zSelectableObject
    {
        public GameObject gameObject;
        public bool Selected { get; protected set; }
        public abstract Il2CppSystem.Type Type { get; }
    }
}

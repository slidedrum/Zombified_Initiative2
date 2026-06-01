using System.Collections.Generic;
using UnityEngine;

namespace BotControl.SmartSelect
{
    public abstract class PressType
    {
        public float SelectionAngle = 30;

        public abstract HashSet<Il2CppSystem.Type> SelectableTypes { get; }
        public abstract Component CurrentComponent { get; }
        public abstract PressAction CurrentAction { get; }
        public virtual void Update()
        {
            SeCurrentComponent();
            SetCurrentAction();
        }
        public abstract bool SeCurrentComponent();
        public abstract bool SetCurrentAction();
        public virtual bool Invoke()
        {
            if (CurrentAction != null)
                return CurrentAction.Invoke(CurrentComponent);
            return false;
        }
    }
}

using UnityEngine;

namespace SlideDrum.sInputSystem
{
    public struct sKeyPressDefinition
    {
        public KeyCode? Key;
        public float MaxHoldDurration = float.MaxValue;
        public float MinHoldDurration = 0f;
        public float MaxUnheldDurration = float.MaxValue;
        public float MinUnheldDurration = 0f;

        public sKeyPressDefinition(KeyCode? Key = null, float? MaxHoldDurration = null, float? MinHoldDurration = null, float? MaxUnheldDurration = null, float? MinUnheldDurration = null)
        {
            this.Key = Key;
            this.MaxHoldDurration = MaxHoldDurration ?? this.MaxHoldDurration;
            this.MinHoldDurration = MinHoldDurration ?? this.MinHoldDurration;
            this.MaxUnheldDurration = MaxUnheldDurration ?? this.MaxUnheldDurration;
            this.MinUnheldDurration = MinUnheldDurration ?? this.MinUnheldDurration;
        }
        public override string ToString()
        {
            return $"{Key}[{MinHoldDurration}, {MaxHoldDurration}]/[{MinUnheldDurration},{MaxUnheldDurration}]";
        }
    }
}

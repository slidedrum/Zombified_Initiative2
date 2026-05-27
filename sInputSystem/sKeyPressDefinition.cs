using UnityEngine;

namespace SlideDrum.sInputSystem
{
    public class sKeyPressDefinition
    {
        public KeyCode? Key;
        public float MaxHoldDurration = float.MaxValue;
        public float MinHoldDurration = 0f;
        public float MaxUnheldDurration = float.MaxValue;
        public float MinUnheldDurration = 0f;
        public bool InvertedInput = false;
        public bool InvertedOutput = false;

        public sKeyPressDefinition(KeyCode? Key = null, float? MaxHoldDurration = null, float? MinHoldDurration = null, float? MaxUnheldDurration = null, float? MinUnheldDurration = null, bool? InvertedInput = false, bool? InvertedOutput = false)
        {
            this.Key = Key;
            this.MaxHoldDurration = MaxHoldDurration ?? float.MaxValue;
            this.MinHoldDurration = MinHoldDurration ?? 0f;
            this.MaxUnheldDurration = MaxUnheldDurration ?? float.MaxValue;
            this.MinUnheldDurration = MinUnheldDurration ?? 0f;
            this.InvertedInput = InvertedInput ?? false;
            this.InvertedOutput = InvertedOutput ?? false;
        }
        public sKeyPressDefinition(sKeyPressDefinition other)
        {
            this.Key = other.Key;
            this.MaxHoldDurration = other.MaxHoldDurration;
            this.MinHoldDurration = other.MinHoldDurration;
            this.MaxUnheldDurration = other.MaxUnheldDurration;
            this.MinUnheldDurration = other.MinUnheldDurration;
            this.InvertedInput = other.InvertedInput;
            this.InvertedOutput = other.InvertedOutput;
        }
        public override string ToString()
        {
            return $"{Key}[{MinHoldDurration},{MaxHoldDurration}]/[{MinUnheldDurration},{MaxUnheldDurration}],[{InvertedInput},{InvertedOutput}]";
        }
    }
}

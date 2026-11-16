using System;
using UnityEngine;

namespace CHG._02_Scripts.CoreSystem
{
    [Serializable]
    public class DamageTextStyle
    {
        [field: SerializeField] public Gradient Gradient { get; private set; }
        [field: SerializeField] public float Intensity { get; private set; }
    }
}
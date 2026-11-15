using System;
using UnityEngine;

namespace CHG._02_Scripts.CoreSystem.LetterboxSystem
{
    [Serializable]
    public struct LetterboxSetting
    {
        [field: SerializeField, Range(0f, 50f)] public float HeightPercent { get; private set; }
        [field: SerializeField, Range(0f, 1f)] public float Opacity { get; private set; }
    }
}
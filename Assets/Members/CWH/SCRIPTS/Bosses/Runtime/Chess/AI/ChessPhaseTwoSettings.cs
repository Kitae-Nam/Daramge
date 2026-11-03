using System;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>미확정 밸런스 수치를 한곳에서 조절한다. 패턴의 실행 순서와 분리된 설정 데이터다.</summary>
    [Serializable]
    public sealed class ChessPhaseTwoSettings
    {
        [Min(0.1f)] public float DamageMultiplier = 1.35f;
        [Range(2, 5)] public int MeleeStrikes = 3;
        [Min(0.03f)] public float SwingDuration = 0.13f;
        [Min(0.1f)] public float ComboWarning = 0.6f;
        [Min(0.1f)] public float DashSpeedMultiplier = 1.2f;
        [Min(0.1f)] public float DashHazardLifetime = 5f;
        [Min(0.05f)] public float DashHazardTickInterval = 0.75f;
        [Min(0f)] public float DashHazardDamage = 10f;
        [Range(1, 8)] public int ThrowCount = 3;
        [Min(0f)] public float ThrowSpread = 3f;
        [Min(0.1f)] public float RoarRadius = 8f;
        [Min(0f)] public float RoarPushSpeed = 16f;
        [Min(0.01f)] public float RoarPushDuration = 0.6f;
        [Min(0f)] public float RoarWallDamage = 35f;
        public LayerMask WallLayers = Physics.DefaultRaycastLayers;
    }
}

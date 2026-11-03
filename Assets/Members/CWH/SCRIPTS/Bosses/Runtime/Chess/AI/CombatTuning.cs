using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    // 읽기 전용 값으로 전달하여 각 공격이 다른 공격의 설정이나 전환 조율자를 알 필요가 없게 한다.
    public readonly struct MeleeTuning
    {
        public int Strikes { get; }
        public float SwingDuration { get; }
        public float ComboWarning { get; }
        public float DamageMultiplier { get; }
        public MeleeTuning(int strikes, float swing, float warning, float damageMultiplier)
        { Strikes = strikes; SwingDuration = swing; ComboWarning = warning; DamageMultiplier = damageMultiplier; }
        public static MeleeTuning Normal(float swing, float warning) => new MeleeTuning(2, swing, warning, 1f);
    }

    public readonly struct ScorchSettings
    {
        public float Lifetime { get; }
        public float TickInterval { get; }
        public float Damage { get; }
        public ScorchSettings(float lifetime, float tickInterval, float damage)
        { Lifetime = Mathf.Max(0.1f, lifetime); TickInterval = Mathf.Max(0.05f, tickInterval); Damage = Mathf.Max(0f, damage); }
    }

    public readonly struct DashTuning
    {
        public float SpeedMultiplier { get; }
        public float DamageMultiplier { get; }
        public bool LeavesScorch { get; }
        public ScorchSettings Scorch { get; }
        public DashTuning(float speed, float damage, bool leavesScorch, ScorchSettings scorch)
        { SpeedMultiplier = speed; DamageMultiplier = damage; LeavesScorch = leavesScorch; Scorch = scorch; }
        public static DashTuning Normal => new DashTuning(1f, 1f, false, default);
    }

    public readonly struct ThrowTuning
    {
        public int Count { get; }
        public float Spread { get; }
        public ThrowTuning(int count, float spread) { Count = Mathf.Max(1, count); Spread = Mathf.Max(0f, spread); }
        public static ThrowTuning Normal => new ThrowTuning(1, 0f);
    }

    public readonly struct RoarTuning
    {
        public float Radius { get; }
        public float Speed { get; }
        public float Duration { get; }
        public float WallDamage { get; }
        public LayerMask WallLayers { get; }
        public RoarTuning(float radius, float speed, float duration, float wallDamage, LayerMask walls)
        { Radius = radius; Speed = speed; Duration = duration; WallDamage = wallDamage; WallLayers = walls; }
        public static RoarTuning Normal(float radius, float speed, float duration) => new RoarTuning(radius, speed, duration, 0f, 0);
    }
}

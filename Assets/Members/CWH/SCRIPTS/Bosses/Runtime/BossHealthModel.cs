using System;
using System.Collections.Generic;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>
    /// 체력 계산만 담당하는 일반 C# 객체. Unity 생명주기와 분리해 계산을 독립 검증한다(SRP).
    /// 배율은 소유자별로 등록하므로 소환 보호를 해제해도 그로기 배율은 유지된다.
    /// </summary>
    public sealed class BossHealthModel : IDamageReceiver
    {
        private readonly Dictionary<object, float> _damageMultipliers = new Dictionary<object, float>();

        public float MaxHealth { get; }
        public float CurrentHealth { get; private set; }
        public float HealthRatio => CurrentHealth / MaxHealth;
        public bool IsDead => CurrentHealth <= 0f;

        public event Action<float, float> HealthChanged;
        public event Action Died;

        public BossHealthModel(float maxHealth)
        {
            ValidateNonNegativeFinite(maxHealth, nameof(maxHealth));
            if (maxHealth <= 0f) throw new ArgumentOutOfRangeException(nameof(maxHealth));

            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
        }

        public void SetDamageMultiplier(object source, float multiplier)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            ValidateNonNegativeFinite(multiplier, nameof(multiplier));
            _damageMultipliers[source] = multiplier;
        }

        public void RemoveDamageMultiplier(object source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            _damageMultipliers.Remove(source);
        }

        public float ReceiveDamage(float baseDamage)
        {
            ValidateNonNegativeFinite(baseDamage, nameof(baseDamage));
            if (IsDead || baseDamage == 0f) return 0f;

            // 곱셈 중 float 오버플로를 줄이고, 무적(0배)은 다른 배율과 무관하게 우선한다.
            double damage = baseDamage;
            foreach (float multiplier in _damageMultipliers.Values)
            {
                if (multiplier == 0f) return 0f;
                damage *= multiplier;
            }

            float appliedDamage = (float)Math.Min(CurrentHealth, damage);
            if (appliedDamage <= 0f) return 0f;

            CurrentHealth -= appliedDamage;
            bool died = IsDead;
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
            if (died) Died?.Invoke();
            return appliedDamage;
        }

        private static void ValidateNonNegativeFinite(float value, string parameterName)
        {
            // NaN/Infinity가 체력이나 배율에 들어가면 이후 전투 상태 전체가 망가지므로 입구에서 거절한다.
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                throw new ArgumentOutOfRangeException(parameterName, "유한한 0 이상의 값이어야 합니다.");
        }
    }
}

using System.Collections.Generic;
using GGMLib.ModuleSystems;
using UnityEngine;

namespace NKT.Enemy.Skill
{
    public class EnemySkillModule : MonoBehaviour, IModule
    {
        [Tooltip("직전에 쓴 스킬의 가중치 배율 (같은 스킬 연속 사용 억제)")]
        [SerializeField, Range(0f, 1f)] private float repeatWeightMultiplier = 0.3f;

        public EnemySkill CurrentSkill { get; private set; }
        public bool IsUsingSkill => CurrentSkill != null && CurrentSkill.IsUsing;
        public bool IsArmored => IsUsingSkill && CurrentSkill.Data.armored;

        private AbstractEnemy _enemy;
        private EnemySkill[] _skills;
        private EnemySkill _lastSkill;
        private readonly List<EnemySkill> _candidates = new();

        public void Initialize(ModuleOwner owner)
        {
            _enemy = owner as AbstractEnemy;
            Debug.Assert(_enemy != null, $"EnemySkillModule은 AbstractEnemy에 있어야 합니다. {gameObject.name}");

            _skills = owner.GetComponentsInChildren<EnemySkill>(true);
            foreach (EnemySkill skill in _skills)
                skill.Initialize(_enemy);
        }

        public EnemySkill SelectSkill(Transform target)
        {
            _candidates.Clear();
            float totalWeight = 0f;

            foreach (EnemySkill skill in _skills)
            {
                if (!skill.IsReady || !skill.IsInRange(target)) continue;

                float weight = GetWeight(skill);
                if (weight <= 0f) continue;

                _candidates.Add(skill);
                totalWeight += weight;
            }

            if (_candidates.Count == 0) return null;

            float pick = Random.value * totalWeight;
            foreach (EnemySkill skill in _candidates)
            {
                pick -= GetWeight(skill);
                if (pick <= 0f) return skill;
            }
            return _candidates[^1];
        }

        public void UseSkill(EnemySkill skill, Transform target)
        {
            if (IsUsingSkill) CurrentSkill.Cancel();

            CurrentSkill = skill;
            _lastSkill = skill;
            skill.Use(target);
        }

        public void CancelCurrentSkill()
        {
            if (IsUsingSkill) CurrentSkill.Cancel();
        }

        private float GetWeight(EnemySkill skill)
            => skill == _lastSkill ? skill.Data.weight * repeatWeightMultiplier : skill.Data.weight;
    }
}

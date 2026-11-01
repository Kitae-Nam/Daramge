using System;
using CHG._02_Scripts.Agent;
using CHG._02_Scripts.CombatSystem;
using UnityEngine;

namespace NKT.Enemy.Skill
{
    public abstract class AbstractEnemySkill : MonoBehaviour, ISkill
    {
        public event Action OnSkillEnd;
        [field: SerializeField] public SkillDataSO SkillData { get; private set; }

        protected AbstractEnemy _ownerEnemy;
        protected IRenderer _renderer;
        protected float _lastUseTime = float.NegativeInfinity; //0이면 게임 시작 직후 쿨다운 동안 사용할 수 없다.

        public bool IsUsing { get; private set; }
        public float NormalizedCooldown => Mathf.Approximately(SkillData.cooldown, 0)
            ? 1f
            : Mathf.Clamp01((Time.time - _lastUseTime) / SkillData.cooldown);

        public virtual void InitializeSkill(ISkillModule skillModule)
        {
            _ownerEnemy = skillModule.Owner as AbstractEnemy;
            Debug.Assert(_ownerEnemy != null, $"적 스킬은 반드시 적의 스킬 모듈 밑에 있어야 합니다. {gameObject}");
            _renderer = _ownerEnemy.GetModule<IRenderer>(); //모듈 초기화 중이라 AbstractEnemy의 프로퍼티는 아직 비어있다.
        }

        public abstract bool CanUseSkill(GameObject target = null);

        public virtual void UseSkill(GameObject target = null)
        {
            IsUsing = true;
        }

        public virtual void StopSkill()
        {
            if (!IsUsing) return;

            IsUsing = false;
            _lastUseTime = Time.time;
            OnSkillEnd?.Invoke();
        }

        protected bool IsTargetInRange(GameObject target, float range)
        {
            Vector3 distance = target.transform.position - _ownerEnemy.transform.position;
            distance.y = 0; //쿼터뷰라 높이차이는 무시
            return distance.sqrMagnitude <= range * range;
        }
    }
}

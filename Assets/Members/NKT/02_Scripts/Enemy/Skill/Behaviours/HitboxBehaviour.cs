using System.Collections.Generic;
using Members.CHG._02_Scripts.Agent.StatSystem;
using Members.CHG._02_Scripts.CombatSystem;
using Members.CHG._02_Scripts.CoreSystem.AnimationSystem;
using UnityEngine;

namespace NKT.Enemy.Skill.Behaviours
{
    //hitOnEvent ~ hitOffEvent 구간 동안 매 프레임 판정한다. 한 구간에서 같은 대상은 한 번만 맞는다.
    public class HitboxBehaviour : MonoBehaviour, ISkillBehaviour
    {
        [Tooltip("판정 중심. 비워두면 이 오브젝트 위치를 사용합니다.")]
        [SerializeField] private Transform hitArea;
        [SerializeField] private float radius = 1f;
        [SerializeField] private LayerMask whatIsTarget;
        [SerializeField] private AnimParamSO hitOnEvent;
        [SerializeField] private AnimParamSO hitOffEvent;
        [SerializeField] private int maxHitCount = 8;

        private Collider[] _results;
        private readonly HashSet<IDamageable> _hitTargets = new();
        private EnemySkill _skill;
        private bool _isActive;

        private Transform HitAreaTrm => hitArea != null ? hitArea : transform;

        private void Awake()
        {
            _results = new Collider[maxHitCount];
        }

        public void OnSkillStart(EnemySkill skill)
        {
            _skill = skill;
            _isActive = false;
            _hitTargets.Clear();
        }

        public void OnSkillEvent(AnimParamSO skillEvent)
        {
            if (skillEvent == hitOnEvent)
            {
                _isActive = true;
                _hitTargets.Clear(); //HitOn을 다시 찍으면 다단히트가 된다.
            }
            else if (skillEvent == hitOffEvent)
            {
                _isActive = false;
            }
        }

        public void OnSkillUpdate()
        {
            if (_isActive)
                CheckHit();
        }

        public void OnSkillEnd() => _isActive = false;

        private void CheckHit()
        {
            Vector3 center = HitAreaTrm.position;
            int count = Physics.OverlapSphereNonAlloc(center, radius, _results, whatIsTarget);

            for (int i = 0; i < count; i++)
            {
                Collider target = _results[i];
                IDamageable damageable = target.GetComponentInParent<IDamageable>();
                if (damageable == null || ReferenceEquals(damageable, _skill.Owner)) continue;
                if (!_hitTargets.Add(damageable)) continue;

                damageable.ApplyDamage(CreateDamageData(target, center));
            }
        }

        private DamageData CreateDamageData(Collider target, Vector3 center)
        {
            EnemySkillDataSO data = _skill.Data;
            IStatModule statModule = _skill.Owner.GetModule<IStatModule>();

            DamageData damageData = statModule != null
                ? statModule.CalculateDamage(data)
                : new DamageData(data.baseDamage * data.damageMultiplier, Vector3.zero, Vector3.zero, _skill.Owner, false);

            //레이캐스트의 노멀처럼 맞은 쪽에서 공격자를 향하는 방향
            Vector3 normal = _skill.Owner.transform.position - target.transform.position;
            normal.y = 0;

            damageData.HitPoint = target.bounds.ClosestPoint(center);
            damageData.HitNormal = normal.normalized;
            damageData.Attacker = _skill.Owner;
            return damageData;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = _isActive ? Color.red : new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawWireSphere(HitAreaTrm.position, radius);
        }
    }
}

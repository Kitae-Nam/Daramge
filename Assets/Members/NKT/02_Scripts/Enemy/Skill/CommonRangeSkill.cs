using System;
using CHG._02_Scripts.Agent;
using CHG._02_Scripts.CombatSystem;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using EpicToonFX;
using NKT.Enemy.BT.Actions;
using NKT.Enemy.Module;
using NKT.Enemy.Visual;
using UnityEngine;
using UnityEngine.Serialization;

namespace NKT.Enemy.Skill
{
    public enum Phase
    {
        Aim, Lock, Fire
    }
    public class CommonRangeSkill : AbstractEnemySkill
    {
        [SerializeField] private AimTelegraph telegraph;
        [SerializeField] private AnimParamSO attackAnimParam;
        [SerializeField] private float crossFadeDuration = 0.8f;
        [SerializeField] private float aimDuration = 0.8f;
        [SerializeField] private float lockDuration = 0.2f;
        [SerializeField] private float startAngle = 40f;
        [SerializeField] private float trackSpeed = 360f;
        [SerializeField] private AnimationCurve narrowCurve;

        private AgentTrigger _trigger;
        private AgentSensor _sensor;
        private Phase _phase;
        private float _phaseStartTime;
        private Transform _target;
        private Vector3 _fireDirection;

        public override void InitializeSkill(ISkillModule skillModule)
        {
            base.InitializeSkill(skillModule);
            _trigger = _ownerEnemy.GetModule<AgentTrigger>();
            _sensor = _ownerEnemy.GetModule<AgentSensor>();
        }

        public override bool CanUseSkill(GameObject target = null)
        {
            if (target == null) return false;

            Vector3 distance = target.transform.position - _ownerEnemy.transform.position;
            distance.y = 0;

            return NormalizedCooldown >= 1f && distance.magnitude <= SkillData.skillRange
                                            && _sensor.IsTargetIsInSight(target.transform);
        }

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);
            _target = target?.transform;
            _phase = Phase.Aim;
            _phaseStartTime = Time.time;
            _renderer.PlayClip(attackAnimParam.ParamHash, 0, crossFadeDuration);
            telegraph.Show();
        }

        private void Update()
        {
            if (!IsUsing) return;
            float elapsed = Time.time - _phaseStartTime;
            float range = SkillData.skillRange;

            switch (_phase)
            {
                case Phase.Aim:
                {
                    float t = Mathf.Clamp01(elapsed / aimDuration);
                    RotateToTarget();
                    telegraph.ShapeUpdate(range, Mathf.Lerp(startAngle, 0f, narrowCurve.Evaluate(t)), t);
                    if (t >= 1f) _phase = Phase.Lock;
                    break;
                }
                case Phase.Lock:
                {
                    telegraph.ShapeUpdate(range, 0f, 1f);
                    if (elapsed >= lockDuration) Fire();
                    break;
                }
            }
        }

        private void RotateToTarget()
        {
            transform.rotation = Quaternion.LookRotation(_target.position - transform.position);
        }

        private void Fire()
        {
            _phase = Phase.Fire;
            telegraph.Hide();
            _fireDirection = _target.position - transform.position;
            _trigger.OnDamageCast += HandleDamageCast;
            _trigger.OnAnimationEnd += StopSkill;
            _renderer.PlayClip(attackAnimParam.ParamHash, 0f, 0.05f);
        }

        private void HandleDamageCast()
        {
            //총알 생성
        }

        public override void StopSkill()
        {
            telegraph.Hide();
            _trigger.OnDamageCast -= HandleDamageCast;
            _trigger.OnAnimationEnd -= StopSkill;
            base.StopSkill();
        }
    }
}
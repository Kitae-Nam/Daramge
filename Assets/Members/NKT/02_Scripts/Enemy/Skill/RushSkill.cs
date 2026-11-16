using System.Collections;
using CHG._02_Scripts.Agent;
using CHG._02_Scripts.CombatSystem;
using CHG._02_Scripts.CombatSystem.Indicator;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using UnityEngine;
using UnityEngine.AI;

namespace NKT.Enemy.Skill
{
    public class RushSkill : AbstractEnemySkill
    {
        [Header("경로")]
        [SerializeField] private float windupTime = 0.7f;//경로 보여주는 시간
        [SerializeField] private float pathWidth = 1.5f;//경로 폭
        [SerializeField] private AnimParamSO windupAnimParam;
        
        [Header("스킬")]
        [SerializeField] private float dashDistance = 6f;
        [SerializeField] private float dashSpeed = 18f;
        [SerializeField] private float crossFadeDuration = 0.15f;
        [SerializeField] private AnimParamSO dashAnimParam;
        [SerializeField] private AbstractDamageCaster damageCaster;
        
        private ISkillRangeIndicator _indicator;
        private NavMeshAgent _navMeshAgent;
        private Coroutine _routine;
        
        private Transform _indicatorTransform;
        private Transform _indicatorParent;
        private Vector3 _indicatorLocalPosition;
        private Quaternion _indicatorLocalRotation;
        
        public override void InitializeSkill(ISkillModule skillModule)
        {
            base.InitializeSkill(skillModule);
            Debug.Assert(damageCaster != null, $"데미지 캐스터가 없습니다. {gameObject}");
            _indicator = _ownerEnemy.GetComponentInChildren<ISkillRangeIndicator>(true);
            _navMeshAgent = _ownerEnemy.GetComponent<NavMeshAgent>();
            Debug.Assert(_navMeshAgent != null, $"MeshAgent가 없습니다. {gameObject}");
            
            damageCaster.InitCaster(_ownerEnemy);
            
            _indicatorTransform = ((Component)_indicator).transform;
            _indicatorParent = _indicatorTransform.parent;
            _indicatorLocalPosition = _indicatorTransform.localPosition;
            _indicatorLocalRotation = _indicatorTransform.localRotation;
        }

        public override bool CanUseSkill(GameObject target = null)
        {
            if (target == null) return false;

            return NormalizedCooldown >= 1f && IsTargetInRange(target, SkillData.skillRange);

        }
        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);
            _routine = StartCoroutine(DashCoroutine(target));
        }

        private IEnumerator DashCoroutine(GameObject target)
        {
            Transform enemy = _ownerEnemy.transform;
            
            //방향 고정
            Vector3 start = enemy.position;
            Vector3 dir = target.transform.position - start;
            dir.y = 0;
            dir.Normalize();
            enemy.rotation = Quaternion.LookRotation(dir);
            _navMeshAgent.ResetPath();
            
            //끝지점 정하기
            Vector3 end = start + dir * dashDistance;
            if(NavMesh.Raycast(start, end, out NavMeshHit hit, _navMeshAgent.areaMask))
                end = hit.position;
            float length = Vector3.Distance(start, end);
            
            //경로 보여주기
            _renderer.PlayClip(windupAnimParam.ParamHash, 0, 0.1f);
            yield return Telegraph(_indicator, new Vector2(pathWidth, length), windupTime, 0.15f);
            DetachIndicator();
                
            //위치 이동
            _renderer.PlayClip(dashAnimParam.ParamHash, 0, 0.1f);
            if (damageCaster is OverlapDamageCaster overlapCaster)
                overlapCaster.ResetHitCaster();
            while ((enemy.position - end).sqrMagnitude > 0.0001f)
            {
                Vector3 next = Vector3.MoveTowards(enemy.position, end, dashSpeed * Time.deltaTime);
                enemy.position = next;
                _navMeshAgent.nextPosition = next;
                damageCaster.CastDamage(next, dir, SkillData);
                yield return null;
            }

            _routine = null;
            StopSkill();
        }

        public override void StopSkill()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
            _indicator.Hide();
            AttachIndicator();
            base.StopSkill();
        }
        
        private void DetachIndicator() => _indicatorTransform.SetParent(null, true);
        
        private void AttachIndicator()
        {
            if (_indicatorTransform.parent == _indicatorParent) return;
            _indicatorTransform.SetParent(_indicatorParent, false);
            _indicatorTransform.localPosition = _indicatorLocalPosition;
            _indicatorTransform.localRotation = _indicatorLocalRotation;
        }
    }
}
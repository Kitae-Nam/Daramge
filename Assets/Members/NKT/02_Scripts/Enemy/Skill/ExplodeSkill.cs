using System.Collections;
using CHG._02_Scripts.CombatSystem;
using CHG._02_Scripts.CoreSystem.Events;
using GGMLib.EventSystem;
using GGMLib.ObjectPool.Runtime;
using UnityEngine;

namespace NKT.Enemy.Skill
{
    //자폭. 사거리 안에 들어오면 펑 터지면서 범위 데미지를 주고 자신은 죽는다.
    //애니메이션 없이 스킬 안에서 바로 처리한다.
    public class ExplodeSkill : AbstractEnemySkill
    {
        [SerializeField] private PoolItemSO explosionEffect;
        [SerializeField] private GameEventChannelSO createChannel;
        [Tooltip("터질 때 숨길 모델. 풀에서 다시 나올 때 MimicEnemy.ResetItem에서 다시 켠다.")]
        [SerializeField] private GameObject visualRoot;
        [Tooltip("폭발 범위 판정. whatIsEnemy에 Player 레이어를 넣고, 반경은 skillRange보다 조금 크게.")]
        [SerializeField] private AbstractDamageCaster damageCaster;
        [Tooltip("0이면 바로 터진다. 값을 주면 그 시간 뒤에 터진다(퓨즈).")]
        [SerializeField] private float fuseTime = 0f;

        private HealthModule _health;
        private Coroutine _fuseRoutine;

        public override void InitializeSkill(ISkillModule skillModule)
        {
            base.InitializeSkill(skillModule);
            _health = _ownerEnemy.GetModule<HealthModule>();

            Debug.Assert(_health != null, $"HealthModule이 없어 자폭할 수 없습니다. {gameObject}");
            Debug.Assert(damageCaster != null, $"폭발 데미지 캐스터가 없습니다. {gameObject}");
            damageCaster.InitCaster(_ownerEnemy);
        }

        public override bool CanUseSkill(GameObject target = null)
        {
            if (target == null)
                return false;

            return IsTargetInRange(target, SkillData.skillRange);
        }

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);
            if (damageCaster is OverlapDamageCaster overlapCaster)
                overlapCaster.ResetHitCaster(); //이전에 맞은 대상 기록 초기화

            _fuseRoutine = StartCoroutine(FuseRoutine());
        }

        private IEnumerator FuseRoutine()
        {
            if (fuseTime > 0f)
                yield return new WaitForSeconds(fuseTime);
            else
                yield return null;

            _fuseRoutine = null; //Explode에서 죽으면서 StopSkill이 불리므로 먼저 비워둔다.
            Explode();
        }

        private void Explode()
        {
            Vector3 position = _ownerEnemy.transform.position;

            damageCaster.CastDamage(position, _ownerEnemy.transform.forward, SkillData);

            if (createChannel != null && explosionEffect != null)
                createChannel.RaiseEvent(CreateEvents.ShowPoolingEffect.InitData(explosionEffect, position, Quaternion.identity));

            if (visualRoot != null)
                visualRoot.SetActive(false);

            _health.ApplyDamage(_health.CurrentHealth);
        }

        public override void StopSkill()
        {
            if (_fuseRoutine != null)
            {
                StopCoroutine(_fuseRoutine);
                _fuseRoutine = null;
            }
            base.StopSkill();
        }
    }
}

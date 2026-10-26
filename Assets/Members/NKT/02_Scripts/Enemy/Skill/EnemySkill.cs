using System;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using UnityEngine;

namespace NKT.Enemy.Skill
{
    public class EnemySkill : MonoBehaviour
    {
        [field: SerializeField] public EnemySkillDataSO Data { get; private set; }
        [SerializeField] private AnimParamSO animParam;
        [SerializeField] private float crossFadeDuration = 0.1f;
        [Tooltip("AnimationEndTrigger가 오지 않을 때 강제로 끝내는 시간")]
        [SerializeField] private float maxDuration = 5f;

        [Header("Debug")]
        [SerializeField] private Transform testTarget;

        public event Action OnSkillFinished;

        public AbstractEnemy Owner { get; private set; }
        public Transform Target { get; private set; }
        public bool IsUsing { get; private set; }
        public bool IsReady => Time.time >= _lastUseTime + Data.cooldown;

        private ISkillBehaviour[] _behaviours;
        private float _lastUseTime = float.NegativeInfinity;
        private float _startTime;

        public void Initialize(AbstractEnemy owner)
        {
            Owner = owner;
            _behaviours = GetComponents<ISkillBehaviour>();
            Debug.Assert(Data != null, $"스킬 데이터가 없습니다. {gameObject.name}");
            Debug.Assert(animParam != null, $"스킬 애니메이션이 없습니다. {gameObject.name}");
        }

        public bool IsInRange(Transform target)
        {
            Vector3 direction = target.position - Owner.transform.position;
            direction.y = 0;
            float distance = direction.magnitude;

            if (distance < Data.minRange || distance > Data.maxRange)
                return false;

            return !Data.needLineOfSight || Owner.AgentSensor.IsTargetIsInSight(target);
        }

        public void Use(Transform target)
        {
            if (IsUsing) Cancel();

            Target = target;
            IsUsing = true;
            _lastUseTime = _startTime = Time.time;

            Owner.NavMovement.NavAgent.ResetPath();
            Owner.SkillEventRelay.OnSkillEvent += HandleSkillEvent;
            Owner.AgentTrigger.OnAnimationEnd += HandleAnimationEnd;

            foreach (ISkillBehaviour behaviour in _behaviours)
                behaviour.OnSkillStart(this);

            Owner.Renderer.PlayClip(animParam.ParamHash, 0f, crossFadeDuration);
        }

        public void Finish() => End(true);
        public void Cancel() => End(false);

        private void Update()
        {
            if (!IsUsing) return;

            foreach (ISkillBehaviour behaviour in _behaviours)
                behaviour.OnSkillUpdate();

            if (Time.time - _startTime > maxDuration)
            {
                Debug.LogWarning($"{gameObject.name}: 애니메이션 종료 이벤트가 오지 않아 스킬을 강제 종료합니다.", this);
                Finish();
            }
        }

        private void OnDisable() => Cancel();

        private void HandleSkillEvent(AnimParamSO skillEvent)
        {
            foreach (ISkillBehaviour behaviour in _behaviours)
                behaviour.OnSkillEvent(skillEvent);
        }

        private void HandleAnimationEnd()
        {
            //크로스페이드 중에 이전 클립의 종료 이벤트가 들어오는 경우는 무시
            Animator animator = Owner.Renderer.Animator;
            if (animator.IsInTransition(0)) return;
            if (animator.GetCurrentAnimatorStateInfo(0).shortNameHash != animParam.ParamHash) return;

            Finish();
        }

        private void End(bool isCompleted)
        {
            if (!IsUsing) return;
            IsUsing = false;

            Owner.SkillEventRelay.OnSkillEvent -= HandleSkillEvent;
            Owner.AgentTrigger.OnAnimationEnd -= HandleAnimationEnd;

            foreach (ISkillBehaviour behaviour in _behaviours)
                behaviour.OnSkillEnd();

            if (isCompleted)
                OnSkillFinished?.Invoke();
        }

        [ContextMenu("Test Use")]
        private void TestUse()
        {
            if (!Application.isPlaying || testTarget == null)
            {
                Debug.LogWarning("플레이 중이고 Test Target이 지정되어 있어야 합니다.", this);
                return;
            }
            Owner.SkillModule.UseSkill(this, testTarget);
        }
    }
}

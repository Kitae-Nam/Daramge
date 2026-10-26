using Members.CHG._02_Scripts.CoreSystem.AnimationSystem;
using UnityEngine;

namespace NKT.Enemy.Skill.Behaviours
{
    //예비동작 동안 타겟을 향해 회전하다가 lockEvent가 오면 회전을 고정한다.
    public class TrackTargetBehaviour : MonoBehaviour, ISkillBehaviour
    {
        [Tooltip("초당 회전 각도")]
        [SerializeField] private float rotateSpeed = 540f;
        [Tooltip("비워두면 스킬이 끝날 때까지 계속 추적합니다.")]
        [SerializeField] private AnimParamSO lockEvent;

        private EnemySkill _skill;
        private bool _isLocked;

        public void OnSkillStart(EnemySkill skill)
        {
            _skill = skill;
            _isLocked = false;
        }

        public void OnSkillEvent(AnimParamSO skillEvent)
        {
            if (skillEvent == lockEvent)
                _isLocked = true;
        }

        public void OnSkillUpdate()
        {
            if (_isLocked || _skill.Target == null) return;

            Transform ownerTrm = _skill.Owner.transform;
            Vector3 direction = _skill.Target.position - ownerTrm.position;
            direction.y = 0;
            if (direction.sqrMagnitude < 0.001f) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            ownerTrm.rotation = Quaternion.RotateTowards(
                ownerTrm.rotation, targetRotation, rotateSpeed * Time.deltaTime);
        }

        public void OnSkillEnd() => _isLocked = true;
    }
}

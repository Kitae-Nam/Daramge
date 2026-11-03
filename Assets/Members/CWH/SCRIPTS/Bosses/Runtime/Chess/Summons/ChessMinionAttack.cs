using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Telegraphs;
using UnityEngine;
using UnityEngine.AI;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>
    /// 기물 FSM이 사용하는 공격 계약이다. 새 행동은 이 클래스를 구현하여 연결하며 FSM을 수정하지 않는다.
    /// 준비 실패 시 false, 공격이 끝났을 때 Tick에서 true를 반환한다는 약속을 모든 구현이 지킨다.
    /// </summary>
    public abstract class ChessMinionAttack : MonoBehaviour
    {
        protected NavMeshAgent Agent { get; private set; }
        protected Transform Target { get; private set; }
        protected float BaseRadius { get; private set; }
        protected float Damage { get; private set; }
        protected AreaTelegraphPresenter Warning { get; private set; }
        public abstract float TriggerDistance { get; }
        public void Initialize(NavMeshAgent agent, float radius, float damage, AreaTelegraphPresenter warning)
        { Agent = agent; BaseRadius = radius; Damage = damage; Warning = warning; }
        public bool Prepare(Transform target)
        {
            if (target == null) return false;
            Target = target;
            return TryPrepare();
        }
        protected abstract bool TryPrepare();
        public abstract void ShowWarning();
        public abstract void BeginAttack();
        public abstract bool Tick(float deltaTime);
        public virtual void Cancel() { }
        protected void ApplyDamage(Vector3 origin, Vector3 forward, float radius, float angle)
        {
            if (Target != null)
                BossAttackQuery.ApplySector(transform, origin, forward, radius, angle, 5f, Damage, ~0, targetRoot: Target);
        }
    }
}

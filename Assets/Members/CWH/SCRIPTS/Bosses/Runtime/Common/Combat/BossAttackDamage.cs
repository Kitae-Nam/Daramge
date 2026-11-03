using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat
{
    /// <summary>
    /// 체스 보스 전투의 하트 단위 피해 규칙. 명중 판정과 플레이어 체력 구현은 알지 않는다.
    /// 페이즈가 없는 독립 공격/테스트는 전달받은 기존 피해량을 유지한다.
    /// </summary>
    public static class BossAttackDamage
    {
        public const float HazardDamage = 1f;

        public static float ForPhase(int phase) => phase == 2 ? 2f : 1f;

        public static float Resolve(Transform owner, float fallback)
        {
            BossPhaseController phase = owner != null ? owner.GetComponentInParent<BossPhaseController>() : null;
            return phase != null ? ForPhase(phase.CurrentPhase) : fallback;
        }

        public static float ResolveHazard(Transform owner, float fallback)
        {
            return owner != null && owner.GetComponentInParent<BossPhaseController>() != null
                ? HazardDamage : fallback;
        }
    }
}

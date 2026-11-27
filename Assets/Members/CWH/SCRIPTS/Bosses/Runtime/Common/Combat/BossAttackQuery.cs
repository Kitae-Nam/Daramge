using System.Collections.Generic;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat
{
    /// <summary>수평 부채꼴 판정만 담당한다. 보스/기물 공격이 같은 거리·각도 규칙을 사용한다.</summary>
    public static class BossAttackQuery
    {
        /// <summary>칸별 상자를 검사한다. 큰 콜라이더가 여러 칸을 덮어도 피해는 한 번만 전달한다.</summary>
        public static bool TryApplyConfirmedCells(Transform target, IReadOnlyList<Vector3> centers, float cellSize,
            float damage, Vector3 origin, Vector3 fallback, float pushSpeed, float pushDuration, out Vector3 point)
        {
            point = origin;
            if (target == null) return false;
            var tried = new HashSet<IDamageReceiver>();
            var half = new Vector3(cellSize * .5f, 2.5f, cellSize * .5f);
            foreach (Vector3 center in centers)
            foreach (var collider in Physics.OverlapBox(center + Vector3.up * half.y, half, Quaternion.identity, ~0, QueryTriggerInteraction.Collide))
            {
                if (!collider.transform.IsChildOf(target)) continue;
                var receiver = collider.GetComponentInParent<IDamageReceiver>();
                if (receiver == null || !tried.Add(receiver) || !(receiver.ReceiveDamage(damage) > 0f)) continue;
                point = collider.ClosestPoint(new Vector3(center.x, collider.bounds.center.y, center.z));
                Vector3 away = target.position - origin;
                away.y = 0f;
                if (pushSpeed > 0f) collider.GetComponentInParent<IKnockbackReceiver>()?.ReceiveKnockBack(
                    away.sqrMagnitude > .0001f ? away.normalized : fallback, pushSpeed, pushDuration);
                return true;
            }
            return false;
        }

        public static int ApplySector(Transform owner, Vector3 origin, Vector3 forward, float radius,
            float angle, float height, float damage, LayerMask layers, float pushSpeed = 0f, float pushDuration = 0f,
            Transform targetRoot = null)
            => ApplySector(owner, origin, forward, radius, angle, height, damage, layers, pushSpeed, pushDuration,
                targetRoot, false, out _);

        /// <summary>실제로 피해를 받은 대상만 명중으로 보고, 그 콜라이더의 표면 위치를 연출에 제공한다.</summary>
        public static bool TryApplyConfirmedSector(Transform owner, Vector3 origin, Vector3 forward, float radius,
            float angle, float height, float damage, LayerMask layers, Transform targetRoot, out Vector3 point,
            float pushSpeed = 0f, float pushDuration = 0f)
            => ApplySector(owner, origin, forward, radius, angle, height, damage, layers, pushSpeed, pushDuration,
                targetRoot, true, out point) > 0;

        private static int ApplySector(Transform owner, Vector3 origin, Vector3 forward, float radius,
            float angle, float height, float damage, LayerMask layers, float pushSpeed, float pushDuration,
            Transform targetRoot, bool requireDamage, out Vector3 impactPoint)
        {
            impactPoint = origin;
            int hits = 0;
            var damaged = new HashSet<IDamageReceiver>();
            var pushed = new HashSet<IKnockbackReceiver>();
            // 얇은 지면 판정 대신 높이 있는 박스로 후보를 구해 키가 다른 캐릭터도 검사한다.
            Collider[] candidates = Physics.OverlapBox(origin + Vector3.up * height * 0.5f,
                new Vector3(radius, height * 0.5f, radius), Quaternion.identity, layers, QueryTriggerInteraction.Collide);
            foreach (Collider hit in candidates)
            {
                if (hit.transform.IsChildOf(owner)) continue;
                if (targetRoot != null && !hit.transform.IsChildOf(targetRoot)) continue;
                Vector3 point = hit.ClosestPoint(new Vector3(origin.x, hit.bounds.center.y, origin.z));
                Vector3 offset = point - origin;
                offset.y = 0f;
                if (!Contains(offset, forward, radius, angle)) continue;
                IDamageReceiver receiver = hit.GetComponentInParent<IDamageReceiver>();
                if (receiver != null && damaged.Add(receiver))
                {
                    float applied = receiver.ReceiveDamage(damage);
                    if (requireDamage && !(applied > 0f)) continue;
                    if (hits++ == 0) impactPoint = point;
                }
                else if (requireDamage) continue;
                IKnockbackReceiver push = hit.GetComponentInParent<IKnockbackReceiver>();
                if (push != null && pushSpeed > 0f && pushed.Add(push))
                    push.ReceiveKnockBack(offset.sqrMagnitude > 0.0001f ? offset.normalized : forward, pushSpeed, pushDuration);
            }
            return hits;
        }

        /// <summary>각도 절반을 기준으로 좌우 대칭인 부채꼴 안인지 판단한다.</summary>
        public static bool Contains(Vector3 offset, Vector3 forward, float radius, float angle)
        {
            offset.y = forward.y = 0f;
            if (offset.sqrMagnitude > radius * radius) return false;
            return angle >= 359.9f || offset.sqrMagnitude < 0.0001f ||
                   Vector3.Angle(forward, offset) <= angle * 0.5f;
        }
    }
}

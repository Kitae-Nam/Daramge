using System.Collections.Generic;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat
{
    /// <summary>수평 부채꼴 판정만 담당한다. 보스/기물 공격이 같은 거리·각도 규칙을 사용한다.</summary>
    public static class BossAttackQuery
    {
        public static int ApplySector(Transform owner, Vector3 origin, Vector3 forward, float radius,
            float angle, float height, float damage, LayerMask layers, float pushSpeed = 0f, float pushDuration = 0f,
            Transform targetRoot = null)
        {
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
                if (receiver != null && damaged.Add(receiver)) receiver.ReceiveDamage(damage);
                IKnockbackReceiver push = hit.GetComponentInParent<IKnockbackReceiver>();
                if (push != null && pushSpeed > 0f && pushed.Add(push))
                    push.ReceiveKnockBack(offset.sqrMagnitude > 0.0001f ? offset.normalized : forward, pushSpeed, pushDuration);
            }
            return damaged.Count;
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

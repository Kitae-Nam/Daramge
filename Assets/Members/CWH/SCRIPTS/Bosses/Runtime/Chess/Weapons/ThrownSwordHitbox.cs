using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Health;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Weapons
{
    /// <summary>박힌 검의 피격 가능 여부와 콜라이더 구성만 담당한다. 비행/복귀 로직은 모른다.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(BreakableHealth))]
    public sealed class ThrownSwordHitbox : MonoBehaviour
    {
        private BreakableHealth _health;
        private BoxCollider _collider;
        private void Awake()
        {
            _health = GetComponent<BreakableHealth>();
            if (GetComponent<BossSword>() != null)
            {
                _collider = gameObject.AddComponent<BoxCollider>();
                MeshFilter filter = GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null)
                {
                    _collider.center = filter.sharedMesh.bounds.center;
                    _collider.size = filter.sharedMesh.bounds.size;
                }
            }
            SetDamageable(false);
        }

        public void SetDamageable(bool enabled)
        {
            _health.CanTakeDamage = enabled;
            if (_collider != null) _collider.enabled = enabled;
        }
    }
}

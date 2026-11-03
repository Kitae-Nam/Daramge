using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Weapons
{
    /// <summary>하나의 동작 데이터를 공급하여 판정과 검 회전이 서로 다른 곡선을 사용하지 않게 한다.</summary>
    [DisallowMultipleComponent]
    public sealed class MeleeMotionSource : MonoBehaviour
    {
        [SerializeField] private SwordSwingMotion _motion = new SwordSwingMotion();
        public SwordSwingMotion Motion => _motion;

        // 저장된 씬에는 명시적으로 연결한다. 옛 프리팹에는 같은 인스턴스를 한 번만 보충한다.
        public static SwordSwingMotion Resolve(GameObject owner)
        {
            var source = owner.GetComponent<MeleeMotionSource>();
            if (source == null) source = owner.AddComponent<MeleeMotionSource>();
            return source.Motion;
        }
    }
}

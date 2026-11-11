using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat
{
    /// <summary>
    /// 로어 패턴에서 플레이어에게 넉백에 사용할 인터페이스입니다.
    /// </summary>
    public interface IKnockbackReceiver
    {
        void ReceiveKnockBack(Vector3 dir, float speed, float duration);
    }
}
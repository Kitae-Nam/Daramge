using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>소환수의 공격 상태 머신이 이동 방식과 분리되어 동작하기 위한 선택형 계약이다.</summary>
    public abstract class ChessMinionMovement : MonoBehaviour
    {
        public virtual ChessBoardGrid Board => null;
        public virtual Transform StandaloneTarget => null;
        public abstract void Bind(ChessBoardGrid board, Transform target, Transform summoner);
        public abstract bool Advance(float stoppingDistance, float deltaTime);
        public abstract void Stop();
    }
}

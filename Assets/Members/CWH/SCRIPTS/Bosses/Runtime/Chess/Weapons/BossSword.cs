using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Weapons
{
    /// <summary>
    /// 검 한자루가 가질 수 있는 상태를 보관하는 클래스입니다.
    /// </summary>
    public sealed class BossSword : MonoBehaviour
    {
        public BossSwordState State { get; private set; }
            = BossSwordState.Orbiting;

        /// <summary>
        /// 공전 중인 검만 선택하여 중복 선택을 막습니다.
        /// 이미 손에 들거나 비행/박힘/파괴 상태의 검을 사용하지 못하게 막습니다.
        /// </summary>
        public bool TryHold()
        {
            if (State != BossSwordState.Orbiting)
            {
                return false;
            }

            State = BossSwordState.Held;
            return true;
        }

        /// <summary>
        /// 손에 든 검을 공전 상태로 되돌립니다.
        /// 비행/파괴 된 검을 공전 상태로 잘못 바꾸는걸 막습니다.
        /// </summary>
        public bool TryReturnFromHand()
        {
            if (State != BossSwordState.Held)
            {
                return false;
            }

            State = BossSwordState.Orbiting;
            return true;
        }

        /// <summary>
        /// 공전 중인 검만 투척에 사용합니다.
        /// 손에 들었거나 이미 날라감/파괴된 검을 사용하지 못하게 막습니다.
        /// </summary>
        public bool TryLaunch()
        {
            if (State != BossSwordState.Orbiting)
            {
                return false;
            }

            State = BossSwordState.Flying;
            return true;
        }

        /// <summary>
        /// 비행 중인 검이 도착하면 바닥에 박힌 상태로 바꿉니다.
        /// 아직 투적하지 않는 공전/손에듬/파괴된 상태의 검을 사용하지 못하게 막습니다. 
        /// </summary>
        public bool TryEmbed()
        {
            if (State != BossSwordState.Flying)
            {
                return false;
            }

            State = BossSwordState.Embedded;
            return true;
        }

        /// <summary>
        /// 바닥에 박힌 검을 파괴 상태로 바꿉니다.
        /// 바닥에 박히진 않는 상태의 검은 사용하지 못하게 막습니다.
        /// </summary>
        public bool TryBreak()
        {
            if (State != BossSwordState.Embedded)
            {
                return false;
            }

            State = BossSwordState.Destroyed;
            return true;
        }
#if UNITY_EDITOR
        // 검 손에 들기 테스트 함수
        [ContextMenu("테스트 / 검 손에 들기")]
        public void TestHold()
        {
            if (!Application.isPlaying) return;
            TryHold();
        }
        
        // 손에 든 검 다시 복귀 함수
        [ContextMenu("테스트 / 검 복귀")]
        private void TestReturnFromHand()
        {
            if (!Application.isPlaying) return;
            TryReturnFromHand();
        }
#endif
    }
}
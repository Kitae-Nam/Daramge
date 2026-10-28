using System.Collections.Generic;
using System.Data;
using Codice.Client.BaseCommands;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Weapons
{
    /// <summary>
    /// 보스가 가진 검 목록을 관리합니다.
    /// 공격에 사용할 검의 선택을 공전 계산과 분리합니다.
    /// </summary>
    public sealed class BossSwordInventory : MonoBehaviour
    {
        [SerializeField] private List<BossSword> _swords = new List<BossSword>();

        /// <summary>
        /// 공전 코드가 같은 검 목록을 읽을 수 있도록한다.
        /// 이 프로퍼티를 사용해서 목록의 추가와 삭제를 허용하지 않는다.
        /// </summary>
        public IReadOnlyList<BossSword> Swords => _swords;

        public bool HasOrbitingSword=> HasSwordInState(BossSwordState.Orbiting);
        public bool HasHeldSword => HasSwordInState(BossSwordState.Held);

        /// <summary>
        /// 손에 들수 있는 첫번째 검을 선택합니다.
        /// 상태 변경 가능 여부는 각 검이 판단하도록 맡깁니다.
        /// </summary>
        public bool TryHoldSword(out BossSword selectSword)
        {
            selectSword = null;
            foreach (BossSword sword in _swords)
            {
                if (sword == null) continue;
                if (!sword.TryHold()) continue;

                selectSword = sword;
                return true;
            }

            return false;
        }

        private bool HasSwordInState(BossSwordState state)
        {
            foreach (BossSword sword in _swords)
            {
                if (sword != null && sword.State == state) return true;
            }

            return false;
        }

#if UNITY_EDITOR
        [ContextMenu("테스트 / 검 한 자루 선택")]
        private void TestHoldSword()
        {
            if (!Application.isPlaying) return;

            if (TryHoldSword(out BossSword selectSword))
            {
                Debug.Log($"선택한 검 :  {selectSword.name}", selectSword);
            }
            else
            {
                Debug.Log("손에 들 수 있는 검이 없습니다.", this);
            }
        }
#endif
    }
}
using System.Collections.Generic;
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
        private int _nextMeleeIndex;

        /// <summary>파괴된 참조를 제외한 보유 수. 비행하거나 박힌 검도 보유한 검에 포함한다.</summary>
        public int LivingCount
        {
            get
            {
                int count = 0;
                foreach (BossSword sword in _swords)
                    if (sword != null && sword.State != BossSwordState.Destroyed) count++;
                return count;
            }
        }

        /// <summary>생성 담당이 제공한 검만 등록한다. Inventory는 프리팹 생성 방법을 알지 않는다.</summary>
        public void Register(BossSword sword)
        {
            if (sword != null && !_swords.Contains(sword)) _swords.Add(sword);
        }

        public void RemoveDestroyed()
        {
            // 전환 시 빈 슬롯을 제거해야 보충한 여섯 검이 공전 원 전체에 균등하게 배치된다.
            _swords.RemoveAll(sword => sword == null || sword.State == BossSwordState.Destroyed);
            _nextMeleeIndex = _swords.Count == 0 ? 0 : _nextMeleeIndex % _swords.Count;
        }

        /// <summary>
        /// 공전 코드가 같은 검 목록을 읽을 수 있도록한다.
        /// 이 프로퍼티를 사용해서 목록의 추가와 삭제를 허용하지 않는다.
        /// </summary>
        public IReadOnlyList<BossSword> Swords => _swords;

        public bool HasOrbitingSword => HasSwordInState(BossSwordState.Orbiting);
        public bool HasHeldSword => HasSwordInState(BossSwordState.Held);

        public int OrbitingCount
        {
            get
            {
                int count = 0;
                foreach (BossSword sword in _swords)
                    if (sword != null && sword.isActiveAndEnabled && sword.State == BossSwordState.Orbiting)
                        count++;
                return count;
            }
        }

        /// <summary>투척도 실제 검을 예약하여 근접 공격과 같은 중복 사용 방지 규칙을 쓴다.</summary>
        public bool TryLaunchSword(out BossSword selectedSword)
        {
            selectedSword = null;
            foreach (BossSword sword in _swords)
            {
                if (sword == null || !sword.isActiveAndEnabled || !sword.TryLaunch()) continue;
                selectedSword = sword;
                return true;
            }

            return false;
        }

        public bool TryPrepareThrowSword(out BossSword selectedSword)
        {
            selectedSword = null;
            foreach (BossSword sword in _swords)
            {
                if (sword == null || !sword.isActiveAndEnabled)
                    continue;
                if (!sword.TryThrowPrepare())
                    continue;
                
                selectedSword = sword;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 마지막 선택 다음부터 공전 중인 검을 순환 선택합니다.
        /// 상태 변경 가능 여부는 각 검이 판단하도록 맡깁니다.
        /// </summary>
        public bool TryHoldSword(out BossSword selectSword)
        {
            selectSword = null;
            // 직전에 돌려놓은 첫 검을 반복 선택하지 않고, 마지막 선택 다음부터 순환한다.
            for (int offset = 0; offset < _swords.Count; offset++)
            {
                int index = (_nextMeleeIndex + offset) % _swords.Count;
                BossSword sword = _swords[index];
                if (sword == null || !sword.isActiveAndEnabled) continue;
                if (!sword.TryHold()) continue;

                selectSword = sword;
                _nextMeleeIndex = (index + 1) % _swords.Count;
                return true;
            }

            return false;
        }

        private bool HasSwordInState(BossSwordState state)
        {
            foreach (BossSword sword in _swords)
            {
                if (sword != null && sword.isActiveAndEnabled && sword.State == state) return true;
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

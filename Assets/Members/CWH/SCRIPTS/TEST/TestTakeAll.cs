using System;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Health;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Patterns;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Memebers.CWH.SCRIPTS.TEST
{
    public class TestTakeAll : MonoBehaviour
    {
        [SerializeField] private BossHealth bossHealth;
        [SerializeField] private BossGroggyController _bossGroggyController;
        [SerializeField] private RoarPattern _roarPattern;
        private void Update()
        {
            if (Keyboard.current.rKey.wasPressedThisFrame)
                bossHealth.ReceiveDamage(100f);
            if (Keyboard.current.tKey.wasPressedThisFrame)
                _bossGroggyController.TryEnterGroggy();
            // 에디터 전용 ContextMenu 함수를 참조하지 않아 플레이어 빌드에서도 컴파일된다.
            if(Keyboard.current.yKey.wasPressedThisFrame)
                _roarPattern.GetComponent<BossPatternRunner>().TryRun(_roarPattern);
        }
    }
}

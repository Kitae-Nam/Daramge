using System;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime;
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
            if(Keyboard.current.yKey.wasPressedThisFrame)
                _roarPattern.RoarPatternEditor();
        }
    }
}
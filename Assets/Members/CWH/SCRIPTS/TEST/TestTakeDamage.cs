using System;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Memebers.CWH.SCRIPTS.TEST
{
    public class TestTakeDamage : MonoBehaviour
    {
        [SerializeField] private BossHealth bossHealth;

        private void Update()
        {
            if (Keyboard.current.rKey.wasPressedThisFrame)
                bossHealth.ReceiveDamage(100f);
        }
    }
}
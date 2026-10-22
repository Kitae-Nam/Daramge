using System;
using System.Collections;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Patterns
{
    public class RoarPattern : BossPattern
    {
        private void Awake()
        {
            
        }

        public override IEnumerator Execute()
        {
            Debug.Log("포효 패턴");
            yield return null;
        }
    }
}
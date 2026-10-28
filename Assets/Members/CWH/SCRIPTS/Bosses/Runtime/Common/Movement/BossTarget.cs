using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Movement
{
    /// <summary>
    ///  보스가 타겟을 정하는 스크립트 입니다.
    /// SetTarget으로 보스가 현재 공격할 타겟을 정할 수 있습니다.
    /// SetTarget에 null을 넣어 타겟 지정을 해제할 수 있습니다.
    /// </summary>
    public sealed class BossTarget : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        
        public Transform Target => _target;

        public bool HasTarget => _target != null;
        
        
        public void SetTarget(Transform trm)
        {
           // null 체크를 하지 않는 이유는 다른 스크립트에서 
           // bossTarget.SetTarget(null);로 타겟 지정을 해제할 수 있기 때문
            _target = trm;
            
        }
    }
}
using UnityEngine;

namespace Members.CWH.SCRIPTS
{
    /// <summary>
    /// 타임 라인에서 활성화 신호를 보낼 때 사용하는 스크립트입니다. 
    /// </summary>
    public class ActiveSignal : MonoBehaviour
    {
        [SerializeField] GameObject obj;
        [SerializeField] bool isActive;
        
        public void Activate()
        {
            obj.SetActive(isActive);
        }
    }
}
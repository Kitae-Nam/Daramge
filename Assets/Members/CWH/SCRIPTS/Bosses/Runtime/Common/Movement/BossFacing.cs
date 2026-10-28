using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Movement
{
    /// <summary>
    /// 보스가 타겟을 바라보는 스크립트입니다.
    /// </summary>
    [RequireComponent(typeof(BossTarget))]
    public class BossFacing : MonoBehaviour
    {
        [SerializeField] private float _rotateSpeed = 180f;
        
        private BossTarget _target;
        private bool _isFacing = true;

        private void Awake()
        {
           _target =  GetComponent<BossTarget>();
        }

        private void Update()
        {
            if (!_isFacing) return;
            if (_target == null ||!_target.HasTarget) return;

            Vector3 dir = _target.Target.position - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude < 0.00001f) return;
            Quaternion targetRotation = Quaternion.LookRotation(dir);
            
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _rotateSpeed * Time.deltaTime);
        }

        public void SetFacingEnabled(bool isFacing)
        {
            _isFacing = isFacing;
        }
    }
}
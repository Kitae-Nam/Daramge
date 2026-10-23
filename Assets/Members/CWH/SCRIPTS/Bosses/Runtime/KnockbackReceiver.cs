using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>
    /// 넉백을 적용하는 스크립트입니다.
    /// </summary>
    public class KnockbackReceiver : MonoBehaviour, IKnockbackReceiver
    {
        private Vector3 _knockBackVelocity;
        private float _remainingTime;
        private float _duration;

        public bool IsKnockedBack => _remainingTime > 0f;

        public Vector3 KnockBackVelocity
        {
            get
            {
                if(!IsKnockedBack) return Vector3.zero;
                
                // 남은 시간의 비율만큼 속도를 줄여 서서히 멈추게 한다.
                float strength = Mathf.Clamp01(_remainingTime / _duration);
                return _knockBackVelocity * strength;
            }
        }
        private void Update()
        {
            if (!IsKnockedBack) return;
            _remainingTime = Mathf.Max(0f, _remainingTime -Time.deltaTime );
        }


        public void ReceiveKnockBack(Vector3 dir, float speed, float duration)
        {
            if (speed <= 0f || duration <= 0) return;
            dir.y = 0f;
            if(dir.magnitude <= 0.000001f) return;
            
            // 전달된 방향의 길이와 관계없이 지정한 속도로 밀려나도록 정규화한다.
            _knockBackVelocity = dir.normalized * speed;
            
            _remainingTime = duration;
            _duration = duration;
        }
    }
}
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Weapons
{
    public class SwordThrowPreparationMotion : MonoBehaviour
    {
        private Vector3 _startPosition;
        private Vector3 _readyPosition;
        
        private Quaternion _startRotation;
        private Quaternion _readyRotation;


        public void Begin(Vector3 orbitCenter, float extraDistance)
        {
            _startPosition = transform.position;
            _startRotation = transform.rotation;
            
            Vector3 outward = _startPosition - orbitCenter;

            outward.y = 0f;
            if (outward.sqrMagnitude < 0.0001f)
                outward = Vector3.forward;
            
            _readyPosition = _startPosition + outward.normalized * extraDistance;
            _readyRotation = Quaternion.FromToRotation(transform.up, Vector3.up) * _startRotation;
            }

        public void SetProgress(float progress)
        {
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress));
            
            transform.SetPositionAndRotation(Vector3.Lerp(_startPosition, _readyPosition, t),
                Quaternion.Slerp(_startRotation, _readyRotation, t));
            
            
        }
        
    }
}
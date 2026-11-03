using System.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Weapons
{
    public class SwordRecallMotion : MonoBehaviour
    {
        [Header("땅에서 뽑기")]
        [SerializeField, Min(0f)]  private float _extractDistance = 2f;
        [SerializeField, Min(.01f)]  private float _extractDuration = .3f;
        
        [Header("검 세우기")]
        [SerializeField, Min(.01f)]  private float _uprightDuration = .2f;
        
        [Header("공전으로 복귀")]
        [SerializeField, Min(0.01f)] private float _returnDuration = 0.6f;

        public bool ReachedOrbit { get; private set; }

        public IEnumerator ExtractAndStand(Transform blade)
        {
            if(blade == null) yield break;
            
            
            Vector3 startPosition = transform.position;
            Vector3 endPosition = startPosition
                - blade.up * _extractDistance;
            
            float elapsed = 0f;
            while (elapsed < _extractDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _extractDuration);
                
                transform.position
                    = Vector3.Lerp(startPosition, endPosition, t);
                yield return null;
            }
            
            transform.position = endPosition;
            
            Quaternion startRotation = blade.rotation;
            Quaternion endRotation = 
                Quaternion.FromToRotation(Vector3.up, Vector3.down);
            
            elapsed = 0f;
            while (elapsed < _uprightDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _uprightDuration);
                blade.rotation 
                    = Quaternion.Slerp(startRotation, endRotation, t);
                yield return null;
            }
            
            blade.rotation = endRotation;
        }

        public IEnumerator ReturnToOrbit(BossSword sword, MonoBehaviour orbitSource)
        {
            ReachedOrbit = false;

            ISwordOrbitPoseProvider provider =
                orbitSource as ISwordOrbitPoseProvider;

            if (sword == null || provider == null) yield break;

            Vector3 startPosition = transform.position;
            Quaternion startRotation = transform.rotation;
            float elapsed = 0f;

            while (true)
            {
                if (sword == null || orbitSource == null ||
                    !orbitSource.isActiveAndEnabled)
                    yield break;

                if (!provider.TryGetOrbitPose(
                        sword, out Vector3 targetPosition,
                        out Quaternion targetRotation))
                    yield break;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(
                    elapsed / Mathf.Max(0.01f, _returnDuration));

                transform.SetPositionAndRotation(
                    Vector3.Lerp(startPosition, targetPosition, t),
                    Quaternion.Slerp(startRotation, targetRotation, t));

                if (t >= 1f)
                {
                    ReachedOrbit = true;
                    yield break;
                }

                yield return null;
            }
        }
    }
}
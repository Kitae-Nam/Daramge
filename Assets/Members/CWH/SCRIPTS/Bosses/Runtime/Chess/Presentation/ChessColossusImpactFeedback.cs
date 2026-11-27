using UnityEngine;
using UnityEngine.Events;
using Move = Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Summons.ChessColossusAttack.Move;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>바닥 충돌과 플레이어 명중을 서로 다른 효과로 표현한다.</summary>
    [DisallowMultipleComponent]
    public sealed class ChessColossusImpactFeedback : MonoBehaviour
    {
        [SerializeField] private GameObject _groundEffect;
        [SerializeField] private GameObject _landingRing;
        [SerializeField] private GameObject _sweepSparks;
        [SerializeField] private GameObject _hitEffect;
        [SerializeField] private AudioClip _groundSound;
        [SerializeField] private AudioClip _hitSound;
        [SerializeField, Tooltip("44프레임 주먹 접점의 좌우 간격 절반과 전방 거리")]
        private Vector2 _slamContactOffset = new Vector2(.48f, 1.52f);
        [SerializeField, Range(0f, .3f)] private float _slamShake = .13f;
        [SerializeField, Range(0f, .3f)] private float _leapShake = .22f;
        [SerializeField, Range(0f, .3f)] private float _sweepShake = .09f;
        [SerializeField, Range(0f, .2f)] private float _hitShakeBoost = .1f;
        [SerializeField] private UnityEvent<float> _cameraImpact = new UnityEvent<float>();
        private AudioSource _audio;

        private void Awake()
        {
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            // 내려다보는 카메라 거리 때문에 실제 명중음이 묻히지 않게 공간음을 약하게 섞는다.
            _audio.spatialBlend = .25f;
        }

        public void PlayPrimary(Move move, Vector3 groundPoint, Vector3 facing, float cellSize, bool hit, Vector3 hitPoint)
        {
            if (move == Move.Slam)
            {
                // 판정 영역 중앙과 실제 주먹 접점은 다르므로 바닥 연출만 클립의 접점에 맞춘다.
                float surfaceY = groundPoint.y;
                groundPoint = transform.TransformPoint(0f, 0f, _slamContactOffset.y);
                groundPoint.y = surfaceY;
            }
            float groundShake = PlayGroundEffect(move, groundPoint, facing, cellSize);
            if (hit)
                PlayHit(hitPoint, move == Move.Leap ? 1f : .8f);
            else if (_groundSound != null)
                AudioSource.PlayClipAtPoint(_groundSound, groundPoint, .62f);

            // 바닥과 플레이어를 동시에 때려도 카메라에는 한 번만 합산해서 보낸다.
            _cameraImpact.Invoke(groundShake + (hit ? _hitShakeBoost : 0f));
        }

        public void PlayFollowUpHit(Vector3 hitPoint)
        {
            PlayHit(hitPoint, .65f);
            _cameraImpact.Invoke(_hitShakeBoost);
        }

        public void PlayLeapTakeoff(float surfaceHeight)
        {
            // 점프 클립 14프레임의 손 위치를 체스판 윗면에 투영한다.
            Vector3 leftHand = transform.TransformPoint(-1.55f, 0f, .42f);
            Vector3 rightHand = transform.TransformPoint(1.55f, 0f, .42f);
            leftHand.y = rightHand.y = surfaceHeight + .025f;
            Spawn(_groundEffect, leftHand, .46f);
            Spawn(_groundEffect, rightHand, .46f);
        }

        private float PlayGroundEffect(Move move, Vector3 point, Vector3 facing, float cellSize)
        {
            Vector3 side = Vector3.Cross(Vector3.up, facing);
            switch (move)
            {
                case Move.Slam:
                    Vector3 fistOffset = side * (_slamContactOffset.x * transform.lossyScale.x);
                    Spawn(_groundEffect, point - fistOffset, .9f);
                    Spawn(_groundEffect, point + fistOffset, .9f);
                    return _slamShake;
                case Move.Leap:
                    Spawn(_groundEffect, point, 1.5f);
                    Spawn(_landingRing, point, 1.3f);
                    return _leapShake;
                default:
                    Vector3 sparkOffset = side * (cellSize * .55f);
                    Spawn(_sweepSparks, point - sparkOffset, .65f);
                    Spawn(_sweepSparks, point + sparkOffset, .8f);
                    return _sweepShake;
            }
        }

        private void PlayHit(Vector3 hitPoint, float strength)
        {
            Spawn(_hitEffect, hitPoint, Mathf.Lerp(.75f, 1.1f, strength));
            if (_hitSound != null)
            {
                _audio.pitch = Mathf.Lerp(1.06f, .91f, strength);
                _audio.PlayOneShot(_hitSound, .88f);
            }
        }

        private static void Spawn(GameObject prefab, Vector3 point, float scale)
        {
            if (prefab == null) return;
            // 파티클이 바라보는 축은 프리팹에 저장된 회전을 따른다.
            var effect = Instantiate(prefab, point, prefab.transform.rotation);
            effect.transform.localScale *= scale;
            Destroy(effect, 2f);
        }
    }
}

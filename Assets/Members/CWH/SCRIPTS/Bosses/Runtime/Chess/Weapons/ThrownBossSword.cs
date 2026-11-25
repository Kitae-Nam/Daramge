using System;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Health;
using System.Collections;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Weapons
{
    /// <summary>
    /// 비행→착지→종료의 순서만 관리한다. 피격 구성과 검 소유권/복귀는 별도 담당에게 맡긴다.
    /// 파괴 보상은 Finished 구독자가 결정하므로 보스 체력을 직접 수정하지 않는다.
    /// </summary>
    [RequireComponent(typeof(BreakableHealth))]
    public sealed class ThrownBossSword : MonoBehaviour
    {
        [SerializeField] private Transform _visual;
        [SerializeField, Min(0.03f)] private float _flightDuration = 0.18f;
        [SerializeField, Min(0.1f)] private float _embeddedDuration = 7f;
        [SerializeField, Min(0f)] private float _impactDamage = 1f;
        [SerializeField, Min(0f)] private float _embedBackOffset = 0.7f;


        private BreakableHealth _health;
        private ThrownSwordHitbox _hitbox;
        private IThrownSwordOwnership _ownership;
        private SwordRecallMotion _recallMotion;
        private SwordThrowFlightMotion _flightMotion;

        private Transform _owner;
        private Transform _target;
        private Vector3 _start;
        private Vector3 _destination;
        private Vector3 _landingPosition;
        private Coroutine _recallRoutine;
        private MonoBehaviour _orbitSource;
        private bool _showEmbeddedFeedback;
        private EmbeddedSwordFeedback _embeddedFeedback;

        private float _radius;
        private float _launchDamage;
        private float _timer;
        private bool _launched;
        private bool _finished;
        public event Action<ThrownBossSword> Embedded;
        public bool IsFlying { get; private set; }
        public bool IsEmbedded => _launched && !IsFlying && !IsRecalling && !_finished;
        public event Action<ThrownBossSword, bool> Finished;
        public bool IsRecalling { get; private set; }

        private void Awake()
        {
            _health = GetComponent<BreakableHealth>();
            _hitbox = GetComponent<ThrownSwordHitbox>();
            _recallMotion = GetComponent<SwordRecallMotion>();
            _flightMotion = GetComponent<SwordThrowFlightMotion>();
            BossSword physical = GetComponent<BossSword>();

            if (_hitbox == null) _hitbox = gameObject.AddComponent<ThrownSwordHitbox>();
            if (_recallMotion == null) _recallMotion = gameObject.AddComponent<SwordRecallMotion>();
            // 소유 방식 선택은 조립 시 한 번만 수행한다. 비행 코드에는 실물/일회용 분기가 없다.
            _ownership = physical != null
                ? (IThrownSwordOwnership)new InventorySwordOwnership(physical)
                : new TransientSwordOwnership(gameObject, _visual);
            if (_flightMotion == null)
                _flightMotion = gameObject.AddComponent<SwordThrowFlightMotion>();
        }

        private void OnEnable() => _health.Broken += HandleBroken;

        public void Launch(Transform owner, Transform target, Vector3 start, Vector3 destination, float radius,
            bool showEmbeddedFeedback = false)
        {
            _finished = false;
            _showEmbeddedFeedback = showEmbeddedFeedback;
            _hitbox.SetDamageable(false);
            _visual = _ownership.Prepare();
            _owner = owner;
            _target = target;
            _start = start;
            _destination = destination;
            _radius = radius;
            // 착지 전에 페이즈가 바뀌더라도 던진 공격의 피해량을 유지한다.
            _launchDamage = BossAttackDamage.Resolve(owner, _impactDamage);

            transform.position = start;
            // 칼끝(+Y)을 목표 방향에 맞춘 뒤 직선 비행 동안 방향을 유지한다.
            _landingPosition = destination + Vector3.up * _embedBackOffset;
            _flightMotion.Begin(start, _landingPosition, _visual, _flightDuration);


            _timer = 0f;
            _launched = IsFlying = true;

            _orbitSource = null;
            if (owner != null)
            {
                foreach (MonoBehaviour component in
                         owner.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (component is ISwordOrbitPoseProvider)
                    {
                        _orbitSource = component;
                        break;
                    }
                }
            }
        }

        private void Update()
        {
            if (!_launched || _finished || IsRecalling) return;
            _timer += Time.deltaTime;
            if (!IsFlying)
            {
                if (_timer >= _embeddedDuration) BeginAnimatedRecall();
                else _embeddedFeedback?.Update(_embeddedDuration - _timer);
                return;
            }

            if (!_flightMotion.Tick(_timer)) return;
            
            IsFlying = false;
            _timer = 0f;
            _ownership.Embed();
            _hitbox.SetDamageable(true);
            if (_showEmbeddedFeedback)
            {
                _embeddedFeedback ??= new EmbeddedSwordFeedback(_visual);
                _embeddedFeedback.Show(_embeddedDuration);
            }
            Embedded?.Invoke(this);
            if (_target != null && _owner != null)
                BossAttackQuery.ApplySector(_owner, _destination, Vector3.forward, _radius, 360f, 5f,
                    _launchDamage, ~0, targetRoot: _target);
        }

        private void HandleBroken()
        {
            if (IsEmbedded) Finish(true);
        }

        public void Recall(bool deferParenting = false) => Finish(false, deferParenting);

        private void Finish(bool broken, bool deferParenting = false)
        {
            if (_finished) return;
            _finished = true;
            _embeddedFeedback?.Hide();
            if (_recallRoutine != null)
            {
                StopCoroutine(_recallRoutine);
                _recallRoutine = null;
            }

            IsRecalling = false;
            IsFlying = false;
            _hitbox.SetDamageable(false);
            _ownership.Complete(broken, deferParenting);
            // Destroy는 프레임 끝에 처리되므로 콜백에서 보상과 목록 정리를 마칠 수 있다.
            Finished?.Invoke(this, broken);
        }

        internal void BeginAnimatedRecall()
        {
            // 수명 만료와 그로기 진입은 같은 연출을 사용한다. 이미 회수 중인 검은 계속 돌아온다.
            if (!IsEmbedded) return;

            IsRecalling = true;
            _embeddedFeedback?.Hide();
            _hitbox.SetDamageable(false);
            _recallRoutine = StartCoroutine(RecallSequence());
        }

        private IEnumerator RecallSequence()
        {
            yield return _recallMotion.ExtractAndStand(_visual);

            BossSword sword = GetComponent<BossSword>();

            if (sword != null && _orbitSource != null)
            {
                yield return _recallMotion.ReturnToOrbit(sword, _orbitSource);

                if (_recallMotion.ReachedOrbit)
                    sword.PoseBinding.KeepCurrentWorldPoseOnReturn();
            }

            _recallRoutine = null;
            Finish(false);
        }

        private void OnDisable()
        {
            _health.Broken -= HandleBroken;
            if (_launched && !_finished) Finish(false, true);
        }
    }

    /// <summary>박힌 검의 색과 떨림만 제어하며, 검의 루트와 피격 콜라이더는 움직이지 않는다.</summary>
    internal sealed class EmbeddedSwordFeedback
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private readonly Transform _visual;
        private readonly MaterialPropertyBlock _properties = new MaterialPropertyBlock();
        private MeshRenderer _source;
        private MeshRenderer _display;
        private bool _sourceWasHidden;

        public EmbeddedSwordFeedback(Transform visual) => _visual = visual;

        public void Show(float remaining)
        {
            if (_display == null)
            {
                _source = _visual != null ? _visual.GetComponentInChildren<MeshRenderer>() : null;
                if (_source == null || !_source.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null)
                    return;

                // 같은 메시를 렌더링하는 자식만 흔들어 검 파괴 판정이 움직이지 않게 한다.
                var child = new GameObject("Embedded Sword Feedback");
                child.layer = _source.gameObject.layer;
                child.transform.SetParent(_source.transform, false);
                child.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                _display = child.AddComponent<MeshRenderer>();
                _display.sharedMaterials = _source.sharedMaterials;
                _display.shadowCastingMode = _source.shadowCastingMode;
                _display.receiveShadows = _source.receiveShadows;
                _display.renderingLayerMask = _source.renderingLayerMask;
            }

            _source.GetPropertyBlock(_properties);
            _sourceWasHidden = _source.forceRenderingOff;
            _source.forceRenderingOff = true;
            _display.gameObject.SetActive(true);
            Update(remaining);
        }

        public void Update(float remaining)
        {
            if (_display == null || !_display.gameObject.activeSelf) return;
            const float recallWarning = 1.2f;
            if (remaining > recallWarning)
            {
                _properties.SetColor(BaseColor, new Color(1.7f, 1.05f, 0.32f));
                _display.transform.localRotation = Quaternion.identity;
            }
            else
            {
                float tremor = Mathf.Sin((recallWarning - remaining) * 46f);
                _display.transform.localRotation = Quaternion.Euler(0f, 0f, tremor * 2f);
                _properties.SetColor(BaseColor, Color.Lerp(new Color(1.7f, 0.35f, 0.13f),
                    new Color(2.4f, 1.4f, 0.4f), (tremor + 1f) * 0.5f));
            }
            _display.SetPropertyBlock(_properties);
        }

        public void Hide()
        {
            if (_display == null || !_display.gameObject.activeSelf) return;
            _display.gameObject.SetActive(false);
            if (_source != null) _source.forceRenderingOff = _sourceWasHidden;
        }
    }
}

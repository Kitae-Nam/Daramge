using GGMLib.ModuleSystems;
using UnityEngine;
using UnityEngine.AI;

namespace NKT.Enemy.Module
{
    //비행 적의 높이를 관리한다.
    //NavMeshAgent는 매 프레임 transform.y를 (NavMesh 높이 + baseOffset)으로 덮어쓰므로, 높이는 baseOffset으로만 바꾼다.
    //높이가 meleeReachHeight보다 낮아지면 레이어를 지상 적 레이어로 바꿔서 근접 공격에 맞게 한다.
    public class FlyHeightModule : MonoBehaviour, IModule, IAfterInitModule
    {
        [Header("높이")]
        [SerializeField] private float hoverHeight = 2f;
        [SerializeField] private float heightChangeSpeed = 4f;

        [Header("둥실거림")]
        [SerializeField] private float bobAmplitude = 0.15f;
        [SerializeField] private float bobSpeed = 2f;

        [Header("근접 피격")]
        [Tooltip("이 높이보다 낮으면 근접 공격에 맞는다. 0이면 항상 원거리 전용")]
        [SerializeField] private float meleeReachHeight = 0.8f;
        [SerializeField] private string flyingLayerName = "FlyingEnemy";
        [SerializeField] private string groundLayerName = "Enemy";

        [Header("그림자")]
        [SerializeField] private Transform shadow;
        [SerializeField] private float shadowGroundOffset = 0.02f;
        [SerializeField] private float shadowMinScale = 0.6f;

        public float HoverHeight => hoverHeight;
        public float CurrentHeight { get; private set; }
        public bool IsMeleeVulnerable => CurrentHeight < meleeReachHeight;

        private ModuleOwner _owner;
        private NavMeshAgent _navAgent;
        private int _flyingLayer;
        private int _groundLayer;
        private float _targetHeight;
        private float _changeSpeed;
        private bool _useBob = true;
        private Vector3 _shadowBaseScale = Vector3.one;

        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;
            _flyingLayer = LayerMask.NameToLayer(flyingLayerName);
            _groundLayer = LayerMask.NameToLayer(groundLayerName);
            Debug.Assert(_flyingLayer >= 0, $"{flyingLayerName} 레이어가 없습니다. {gameObject.name}");
            Debug.Assert(_groundLayer >= 0, $"{groundLayerName} 레이어가 없습니다. {gameObject.name}");

            if (shadow != null)
                _shadowBaseScale = shadow.localScale;
        }

        public void AfterInit()
        {
            INavMovement navMovement = _owner.GetModule<INavMovement>();
            _navAgent = navMovement != null ? navMovement.NavAgent : _owner.GetComponent<NavMeshAgent>();
            Debug.Assert(_navAgent != null, $"FlyHeightModule은 NavMeshAgent가 필요합니다. {gameObject.name}");

            CurrentHeight = _targetHeight = hoverHeight;
            _changeSpeed = heightChangeSpeed;
            ApplyHeight();
        }

        //스킬 등에서 높이를 바꿀 때 사용. 끝나면 ResetHeight로 원래 높이로 돌려놓는다.
        public void SetHeight(float height, float speed, bool useBob = false)
        {
            _targetHeight = Mathf.Max(0f, height);
            _changeSpeed = Mathf.Max(0.01f, speed);
            _useBob = useBob;
        }

        public void ResetHeight() => SetHeight(hoverHeight, heightChangeSpeed, true);

        public void DropToGround(float speed) => SetHeight(0f, speed);

        private void Update()
        {
            CurrentHeight = Mathf.MoveTowards(CurrentHeight, _targetHeight, _changeSpeed * Time.deltaTime);
            ApplyHeight();
        }

        private void ApplyHeight()
        {
            if (_navAgent == null) return;

            float bob = _useBob ? Mathf.Sin(Time.time * bobSpeed) * bobAmplitude : 0f;
            float offset = Mathf.Max(0f, CurrentHeight + bob);

            if (_navAgent.enabled)
                _navAgent.baseOffset = offset;

            UpdateLayer();
            UpdateShadow(offset);
        }

        private void UpdateLayer()
        {
            if (_flyingLayer < 0 || _groundLayer < 0) return;

            int layer = IsMeleeVulnerable ? _groundLayer : _flyingLayer;
            if (_owner.gameObject.layer != layer)
                _owner.gameObject.layer = layer;
        }

        //그림자는 항상 지면에 붙이고, 높이 올라갈수록 작게 보이게 한다.
        private void UpdateShadow(float offset)
        {
            if (shadow == null) return;

            shadow.localPosition = new Vector3(0f, -offset + shadowGroundOffset, 0f);
            float t = hoverHeight > 0f ? Mathf.Clamp01(offset / hoverHeight) : 0f;
            shadow.localScale = _shadowBaseScale * Mathf.Lerp(1f, shadowMinScale, t);
        }
    }
}

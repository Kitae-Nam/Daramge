using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime;
using UnityEngine;
using UnityEngine.UIElements;

namespace Daramge.BossUI
{
    /// <summary>보스 이벤트를 View와 잔상 모델에 전달한다. 체력 계산과 페이즈 전환 규칙은 보스가 소유한다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PanelRenderer))]
    public sealed class BossHealthBarController : MonoBehaviour
    {
        [Header("Boss")]
        [SerializeField] private BossHealth _boss;
        [SerializeField] private string _bossName = "THE KING";
        [SerializeField] private Sprite _portrait;
        [SerializeField, Min(1)] private int _phaseCount = 3;
        [SerializeField, Min(1)] private int _currentPhase = 1;
        [Header("Damage trail")]
        [Tooltip("마지막 피해 이후 잔상이 움직이기 전 대기 시간(초)")]
        [SerializeField, Min(0f)] private float _lagDelay = 0.25f;
        [Tooltip("잔상이 전체 게이지 100%를 이동하는 데 걸리는 시간(초)")]
        [SerializeField, Min(0.01f)] private float _catchUpDuration = 0.8f;

        private readonly HealthBarLag _lag = new HealthBarLag();
        private PanelRenderer _panelRenderer;
        private VisualElement _boundRoot;
        private int _uiVersion;
        private BossHealthBarView _view;
        private BossHealth _subscribedBoss;

        private void OnEnable()
        {
            _panelRenderer = GetComponent<PanelRenderer>();
            Subscribe();
            SynchronizeHealth();
            // Unity 6.6은 PanelRenderer의 콜백으로 루트를 전달한다.
            // 이미 패널에 연결된 경우 등록 즉시 호출되며, 이후 UXML 재생성 시에도 다시 연결한다.
            _panelRenderer.RegisterUIReloadCallback(OnUIReloaded);
        }

        private void OnUIReloaded(PanelRenderer panelRenderer, VisualElement root, int version)
        {
            // 버전이 포함된 콜백을 사용해 동일 트리의 중복 알림에서는 View 생성을 생략한다.
            if (root == _boundRoot && version == _uiVersion) return;
            _boundRoot = root;
            _uiVersion = version;
            _view = root == null ? null : new BossHealthBarView(root);
            RefreshPresentation();
        }

        private void Update()
        {
            if (_view == null) return;

            // Unity에서 파괴된 보스도 숨겨 다음 보스를 Bind할 때까지 오래된 HUD가 남지 않게 한다.
            _view.SetVisible(_boss != null);
            _lag.Tick(Time.unscaledDeltaTime, _catchUpDuration);
            _view.SetHealth(_lag.Current, _lag.Trail);
        }

        private void OnDisable()
        {
            // 비활성 컨트롤러가 리로드 콜백으로 다시 연결되지 않도록 구독을 함께 해제한다.
            if (_panelRenderer != null)
                _panelRenderer.UnregisterUIReloadCallback(OnUIReloaded);
            Unsubscribe();
            _view?.SetVisible(false);
            _view = null;
            _boundRoot = null;
            _uiVersion = 0;
        }

        /// <summary>다른 보스로 전환할 때 구독, 표시 정보, 잔상을 함께 초기화한다.</summary>
        public void Bind(BossHealth boss, string bossName, Sprite portrait, int phaseCount, int currentPhase = 1)
        {
            Unsubscribe();
            _boss = boss;
            _bossName = bossName;
            _portrait = portrait;
            SetPhaseState(phaseCount, currentPhase);
            if (isActiveAndEnabled) Subscribe();
            SynchronizeHealth();
            RefreshPresentation();
        }

        /// <summary>보스별 총 페이즈 수와 현재 페이즈(1부터)를 전투 코드에서 전달한다.</summary>
        public void SetPhaseState(int phaseCount, int currentPhase)
        {
            _phaseCount = Mathf.Max(1, phaseCount);
            _currentPhase = Mathf.Clamp(currentPhase, 1, _phaseCount);
            _view?.SetPhases(_phaseCount, _currentPhase);
        }

        private void Subscribe()
        {
            if (_boss == null) return;
            _subscribedBoss = _boss;
            _subscribedBoss.HealthChanged += OnHealthChanged;
        }

        private void Unsubscribe()
        {
            // Unity 객체가 이미 파괴돼도 관리 객체의 이벤트 구독은 정리한다.
            if (ReferenceEquals(_subscribedBoss, null)) return;
            _subscribedBoss.HealthChanged -= OnHealthChanged;
            _subscribedBoss = null;
        }

        private void OnHealthChanged(float current, float maximum)
        {
            _lag.SetTarget(maximum > 0f ? current / maximum : 0f, _lagDelay);
            // 실제 체력은 이벤트 시 즉시 표시하고 잔상만 Update에서 지연시킨다.
            _view?.SetHealth(_lag.Current, _lag.Trail);
        }

        private void SynchronizeHealth()
        {
            _lag.SetTarget(_boss == null ? 0f : _boss.HealthRatio, 0f, true);
            _view?.SetHealth(_lag.Current, _lag.Trail);
        }

        private void RefreshPresentation()
        {
            if (_view == null) return;
            _view.SetIdentity(_bossName, _portrait);
            _view.SetPhases(_phaseCount, _currentPhase);
            _view.SetVisible(_boss != null);
            _view.SetHealth(_lag.Current, _lag.Trail);
        }

        private void OnValidate()
        {
            _phaseCount = Mathf.Max(1, _phaseCount);
            _currentPhase = Mathf.Clamp(_currentPhase, 1, _phaseCount);
            _lagDelay = Mathf.Max(0f, _lagDelay);
            _catchUpDuration = Mathf.Max(0.01f, _catchUpDuration);
            // 플레이 중 Inspector에서 표시 설정을 조절할 수 있게 한다.
            RefreshPresentation();
        }
    }
}

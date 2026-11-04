using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Cinematics;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Health;
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
        [Header("등장 컷신")]
        [SerializeField] private BossCutsceneGate _introCutscene;
        [Header("체력바 등장 연출")]
        [SerializeField, Min(0.01f)] private float _introFadeDuration = 0.3f;
        [SerializeField, Min(0.01f)] private float _introCharacterInterval = 0.06f;
        [SerializeField, Min(0.01f)] private float _introFillDuration = 0.8f;


        private bool ShouldShow
        {
            get
            {
#if UNITY_EDITOR
                if (_testIntroPlaying && _boss != null) return true;
#endif
                return _boss != null && (_introCutscene == null || _introCutscene.HasFinishedFirstCutscene);
            }
        }

#if UNITY_EDITOR
        private bool _testIntroPlaying;
#endif

        private BossHealthBarIntro _intro;
        private PanelRenderer _panelRenderer;
        private VisualElement _boundRoot;
        private int _uiVersion;
        private BossHealthBarView _view;
        private BossHealth _subscribedBoss;
        private readonly HealthBarLag _lag = new HealthBarLag();

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

            if (!ShouldShow)
            {
                RenderHealthBar();
                return;
            }

            if (_intro == null)
            {
                _intro = new BossHealthBarIntro(
                    (_bossName ?? string.Empty).Length,
                    _introFadeDuration,
                    _introCharacterInterval,
                    _introFillDuration);
            }

            if (!_intro.IsComplete)
            {
                _intro.Tick(Time.unscaledDeltaTime);

                if (_intro.IsComplete)
                    _lag.SetTarget(_boss.HealthRatio, 0f, true);
            }
            else
            {
                _lag.Tick(Time.unscaledDeltaTime, _catchUpDuration);
            }

            RenderHealthBar();

#if UNITY_EDITOR
            // 미리보기만 종료하고, 실제 컷신 뒤의 등장 연출은 다시 시작할 수 있게 남긴다.
            if (_testIntroPlaying && _intro.IsComplete)
            {
                _testIntroPlaying = false;
                if (_introCutscene != null && !_introCutscene.HasFinishedFirstCutscene)
                    _intro = null;
            }
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            if (_testIntroPlaying) _intro = null;
            _testIntroPlaying = false;
#endif
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
            _intro = null;
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
            RenderHealthBar();
        }

        private void SynchronizeHealth()
        {
            _lag.SetTarget(_boss == null ? 0f : _boss.HealthRatio, 0f, true);
            RenderHealthBar();
        }

        private void RefreshPresentation()
        {
            if (_view == null) return;

            _view.SetIdentity(_bossName, _portrait);
            _view.SetPhases(_phaseCount, _currentPhase);
            RenderHealthBar();
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
        
        private void RenderHealthBar()
        {
            if (_view == null) return;

            _view.SetVisible(ShouldShow);
            if (!ShouldShow) return;

            if (_intro == null)
            {
                _view.PrepareIntro();
                return;
            }

            string name = _bossName ?? string.Empty;

            if (_intro.IsComplete)
            {
                _view.CompleteIntro(name, _lag.Current);
                _view.SetHealth(_lag.Current, _lag.Trail);
                return;
            }

            int characters = Mathf.Min(_intro.VisibleCharacters, name.Length);
            float displayedHealth = _lag.Current * _intro.FillProgress;

            _view.SetOpacity(_intro.Opacity);
            _view.SetNameText(name.Substring(0, characters));
            _view.SetHealth(displayedHealth, displayedHealth);
            _view.SetPercentVisible(_intro.IsFilling);
        }

#if UNITY_EDITOR
        [ContextMenu("테스트/체력바 등장 연출")]
        private void TestIntro()
        {
            if (!Application.isPlaying || !isActiveAndEnabled || _boss == null || _view == null)
            {
                Debug.LogWarning("플레이 중 활성화된 체력바에서 보스와 UI 연결을 확인한 뒤 실행하세요.", this);
                return;
            }

            // 컷신/전투 상태를 변경하지 않고 UI의 연출 진행도만 초기화한다.
            _testIntroPlaying = true;
            _intro = null;
            _lag.SetTarget(_boss.HealthRatio, 0f, true);
            RefreshPresentation();
        }
#endif
    }
}

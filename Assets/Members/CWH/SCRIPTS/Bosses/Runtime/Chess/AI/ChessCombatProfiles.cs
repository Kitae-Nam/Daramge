using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.AI
{
    /// <summary>
    /// 현재 페이즈에 맞는 공격별 수치만 제공한다. 전환 시간, 외형, 프리팹, 이펙트 생성은 담당하지 않는다.
    /// 새 페이즈의 설정 선택은 이곳에서 확장하며 개별 패턴의 실행 순서는 유지한다.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(BossPhaseController))]
    public sealed class ChessCombatProfiles : MonoBehaviour, IMeleeTuningSource, IDashTuningSource, IThrowTuningSource, IRoarTuningSource
    {
        [SerializeField] private ChessPhaseTwoSettings _phaseTwo = new ChessPhaseTwoSettings();
        private BossPhaseController _phase;
        public ChessPhaseTwoSettings PhaseTwo => _phaseTwo;
        private bool Enhanced => _phase != null && _phase.CurrentPhase == 2;
        private void Awake() => _phase = GetComponent<BossPhaseController>();

        public MeleeTuning GetMelee(float normalSwing, float normalWarning) => Enhanced
            ? new MeleeTuning(_phaseTwo.MeleeStrikes, _phaseTwo.SwingDuration, _phaseTwo.ComboWarning, _phaseTwo.DamageMultiplier)
            : MeleeTuning.Normal(normalSwing, normalWarning);

        // 페이즈별 돌진 횟수를 여기서 정해 패턴은 페이즈 판정 없이 같은 이동을 반복한다.
        public DashTuning GetDash() => Enhanced
            ? new DashTuning(3, _phaseTwo.DashSpeedMultiplier, _phaseTwo.DamageMultiplier)
            : DashTuning.Normal;

        public ThrowTuning GetThrow() => Enhanced ? new ThrowTuning(_phaseTwo.ThrowCount, _phaseTwo.ThrowSpread) : ThrowTuning.Normal;

        public RoarTuning GetRoar(float normalRadius, float normalSpeed, float normalDuration) => Enhanced
            ? new RoarTuning(_phaseTwo.RoarRadius, _phaseTwo.RoarPushSpeed, _phaseTwo.RoarPushDuration,
                _phaseTwo.RoarWallDamage, _phaseTwo.WallLayers)
            : RoarTuning.Normal(normalRadius, normalSpeed, normalDuration);
    }
}

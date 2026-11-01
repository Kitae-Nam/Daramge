namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    // 각 패턴이 사용하는 조회만 계약으로 둔다. 하나의 큰 인터페이스를 모든 패턴에 강요하지 않는다.
    public interface IMeleeTuningSource { MeleeTuning GetMelee(float normalSwing, float normalWarning); }
    public interface IDashTuningSource { DashTuning GetDash(); }
    public interface IThrowTuningSource { ThrowTuning GetThrow(); }
    public interface IRoarTuningSource { RoarTuning GetRoar(float normalRadius, float normalSpeed, float normalDuration); }
}

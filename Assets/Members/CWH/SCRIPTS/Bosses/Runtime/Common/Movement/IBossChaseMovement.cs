namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Movement
{
    /// <summary>상태 제어기는 이동 방식 대신 추적 허용/차단이라는 작은 계약에 의존한다.</summary>
    public interface IBossChaseMovement
    {
        void SetChaseEnabled(bool enabled);
    }
}

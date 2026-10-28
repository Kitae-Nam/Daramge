namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Weapons
{
    
    /// <summary>
    /// 검의 상태 enum입니다.
    /// 검 한 자루가 가질 수 있는 상태를 정의합니다.
    /// </summary>
    public enum BossSwordState
    {
        Orbiting,   // 공전 중
        Held,       // 손에 들어 사용 중
        Flying,     // 투척 되어 날아가는 중
        Embedded,   // 바닥에 박힘
        Destroyed,  // Destroy 파괴 된 상태
    }
}
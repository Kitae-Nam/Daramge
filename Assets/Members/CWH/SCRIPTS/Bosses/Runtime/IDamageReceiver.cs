namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>
    /// 공격자는 보스 종류나 체력 구현을 몰라도 이 계약으로 피해를 전달한다(DIP).
    /// 반환값은 배율과 남은 체력을 반영해 실제로 감소한 체력이다.
    /// </summary>
    public interface IDamageReceiver
    {
        float ReceiveDamage(float baseDamage);
    }
}

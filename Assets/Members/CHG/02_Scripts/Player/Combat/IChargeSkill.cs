namespace CHG._02_Scripts.Player.Combat
{
    public interface IChargeSkill : IReleasable
    {
        ChargeLogic ChargeLogic { get; }
    }
}
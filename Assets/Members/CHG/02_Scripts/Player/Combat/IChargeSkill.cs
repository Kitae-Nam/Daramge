namespace CHG._02_Scripts.Player.Combat
{
    public interface IChargeSkill
    {
        ChargeLogic ChargeLogic { get; }
        void ReleaseCharge();
    }
}
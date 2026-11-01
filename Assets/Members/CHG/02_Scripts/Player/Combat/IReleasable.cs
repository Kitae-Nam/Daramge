namespace CHG._02_Scripts.Player.Combat
{
    public interface IReleasable
    {
        public bool IsHolding { get; }
        public void Release();
    }
}
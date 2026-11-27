namespace CHG._02_Scripts.Player.Respawn
{
    public interface IRespawnable
    {
        void EnterArea(RespawnArea area);
        void ExitArea(RespawnArea area);
        void RespawnFromFall();
    }
}
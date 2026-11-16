using System;

namespace CHG._02_Scripts.Agent.StatSystem
{
    public interface IBuffModule
    {
        event Action<BuffSO> OnBuffEnded;
        void ApplyBuff(BuffSO buff);
    }
}
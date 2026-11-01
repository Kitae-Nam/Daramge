using System;

namespace CHG._02_Scripts.Agent
{
    [Flags]
    public enum AgentStatus
    {
        None = 0,
        Invincible = 1 << 0,
        Bleeding = 1 << 1,
        Shocked = 1 << 2,
    }
}
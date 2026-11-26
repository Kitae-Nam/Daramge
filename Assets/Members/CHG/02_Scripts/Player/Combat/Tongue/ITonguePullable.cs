using UnityEngine;

namespace CHG._02_Scripts.Player.Combat.Tongue
{
    public interface ITonguePullable
    {
        Transform Root { get; }
        void BeginPull();
        void EndPull();
    }
}
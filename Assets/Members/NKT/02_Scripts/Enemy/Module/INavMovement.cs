using UnityEngine;
using UnityEngine.AI;

namespace NKT.Enemy.Module
{
    public interface INavMovement
    {
        NavMeshAgent NavAgent { get; }
        Vector3 Velocity { get; set; }
        float Speed { get; set; }
        bool IsStopped { get; set; }
        bool IsArrived { get; } //get만 준다
        
        void SetDestination(Vector3 destination);
    }
}
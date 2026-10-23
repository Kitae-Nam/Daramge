using Unity.Behavior;

namespace NKT.Enemy.BT
{
    [BlackboardEnum]
    public enum EnemyState
    {
        IDLE, MOVE, COMBAT, HIT, DEATH
    }
}
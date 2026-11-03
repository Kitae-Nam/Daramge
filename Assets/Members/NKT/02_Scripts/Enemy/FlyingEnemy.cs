using NKT.Enemy.Module;
using UnityEngine;

namespace NKT.Enemy
{
    public class FlyingEnemy : AbstractEnemy
    {
        [SerializeField] private float deathFallSpeed = 6f;

        public FlyHeightModule FlyHeight { get; private set; }

        protected override void InitializeModules()
        {
            base.InitializeModules();
            FlyHeight = GetModule<FlyHeightModule>();
            Debug.Assert(FlyHeight != null, $"비행 적에는 FlyHeightModule이 필요합니다. {gameObject.name}");
        }

        protected override void HandleDeath()
        {
            base.HandleDeath();
            if (FlyHeight != null)
                FlyHeight.DropToGround(deathFallSpeed); //죽으면 바닥으로 떨어진다.
        }
    }
}

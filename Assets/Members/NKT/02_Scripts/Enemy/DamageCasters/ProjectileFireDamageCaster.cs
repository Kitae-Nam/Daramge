using CHG._02_Scripts.CombatSystem;
using GGMLib.ModuleSystems;
using GGMLib.ObjectPool.Runtime;
using NKT.Enemy.Projectile;
using UnityEngine;

namespace NKT.Enemy.DamageCasters
{
    //근접 캐스터처럼 스킬이 CastDamage를 부르면, 판정 대신 유도탄을 꺼내서 쏜다.
    //whatIsEnemy(부모 필드)는 "맞힐 대상" 레이어이므로 적이 쓸 때는 Player를 넣는다.
    public class ProjectileFireDamageCaster : AbstractDamageCaster
    {
        [SerializeField] private PoolManagerSO poolManager;
        [SerializeField] private PoolItemSO projectile;

        //유도할 대상. 스킬이 발사 전에 넣어준다.
        public Transform Target { get; set; }

        public override void InitCaster(ModuleOwner owner)
        {
            base.InitCaster(owner);
            Debug.Assert(poolManager != null, $"PoolManager가 없습니다. {gameObject.name}");
            Debug.Assert(projectile != null, $"발사할 투사체 PoolItem이 없습니다. {gameObject.name}");
        }

        public override bool CastDamage(Vector3 position, Vector3 direction, SkillDataSO skillData)
        {
            HomingMissile missile = poolManager.Pop<HomingMissile>(projectile);
            if (missile == null)
            {
                Debug.LogError($"{projectile.name}을 꺼내지 못했습니다. PoolManager itemList와 프리팹의 HomingMissile을 확인하세요.", this);
                return false;
            }

            missile.Fire(poolManager, position, direction, Target, CreateDamageData(skillData), whatIsEnemy);
            return true;
        }

        //데미지는 쏘는 쪽(적의 스탯)에서 계산하고, 총알은 맞았을 때 적용만 한다.
        private DamageData CreateDamageData(SkillDataSO skillData)
        {
            DamageData damageData = StatModule != null
                ? StatModule.CalculateDamage(skillData)
                : new DamageData(skillData.baseDamage * skillData.damageMultiplier, Vector3.zero, Vector3.zero, CasterOwner, false);

            damageData.Attacker = CasterOwner; //피격 방향 계산과 자기 자신 충돌 무시용
            return damageData;
        }
    }
}

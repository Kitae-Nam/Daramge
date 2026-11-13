using System.Collections;
using CHG._02_Scripts.Agent;
using CHG._02_Scripts.Agent.StatSystem;
using CHG._02_Scripts.CombatSystem;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using GGMLib.ObjectPool.Runtime;
using UnityEngine;

namespace CHG._02_Scripts.Player.Combat
{
    public class PlayerRangeAttack : AbstractPlayerSkill, IReleasable
    {
        public bool IsHolding => _isHolding;
        
        [SerializeField] private StatSO moveSpeedStat;
        [SerializeField] private AnimParamSO shotParam;
        [SerializeField] private AnimParamSO reloadParam;
        [SerializeField] private PoolManagerSO poolManager;
        [SerializeField] private PoolItemSO projectileItem;
        [SerializeField] private Transform muzzleTrm;
        [SerializeField] private float moveSpeedSlow = 0.7f;
        [SerializeField] private float fireInterval = 0.1f;
        
        [SerializeField] private float reloadTime; //나중에 스탯으로?
        [SerializeField] private int maxAmmo = 30;
        
        private IStatModule _statModule;
        private AgentTrigger _trigger;
        private bool _isHolding = false;
        private bool _isReloading = false;
        private int _currentAmmo;
        private  float _nextShotTime;
        private Coroutine _shotCoroutine;
        private IDamageCalculator _damageCalculator;
        
        public override void InitializeSkill(ISkillModule skillModule)
        {
            base.InitializeSkill(skillModule);
            _trigger = _player.GetModule<AgentTrigger>();
            Debug.Assert(_trigger != null, $"trigger is null : {gameObject.name}");
            _statModule = _player.GetModule<IStatModule>();
            Debug.Assert(_statModule != null, $"statModule is null : {gameObject.name}");
            _damageCalculator = _player.GetModule<IDamageCalculator>(); 
            Debug.Assert(_damageCalculator != null, $"damageCalculator is null : {gameObject.name}"); 
            
            _currentAmmo = maxAmmo;
            
        }

        public override bool CanUseSkill(GameObject target = null)
        {
            return NormalizedCooldown >= 1f && IsUsing == false && !_isReloading;
        }

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);
            _isHolding = true;
            _movement.CanManualMove = true;
            _movement.CanAutoRotate = false;
            ApplyShotSlow();
            _shotCoroutine = StartCoroutine(ShotCoroutine());
        }

        private IEnumerator ShotCoroutine()
        {
            while (IsHolding)
            {
                AimToMouse();
                if (Time.time >= _nextShotTime)
                {
                    if (_currentAmmo <= 0)
                    {
                        Reload();
                        break;
                    }

                    Shot();
                    float interval = fireInterval / _skillModule.AttackSpeed;
                    _nextShotTime = Mathf.Max(_nextShotTime, Time.time - Time.deltaTime) + interval;  
                }
                yield return null;
            }

            _shotCoroutine = null;
            StopSkill();
        }

        private void AimToMouse()
        {
            Vector3 playerPos = _player.transform.position;
            Vector3 dir = _player.PlayerInput.GetMousePositionOnPlane(playerPos.y) - playerPos;            
            _movement.RotateTo(dir);
        }

        private void Shot()
        {
            _currentAmmo--;
            _renderer.PlayClip(shotParam.ParamHash, 0, 0.01f);
            Projectile projectile = poolManager.Pop<Projectile>(projectileItem);
            Debug.Assert(projectile != null, $"projectile is null : {gameObject.name}");

            DamageData data = _damageCalculator.CalculateDamage(SkillData);
            projectile.Fire(poolManager, muzzleTrm.position, _player.transform.forward, data);
        }

        private void Reload()
        {
            StartCoroutine(ReLoadCoroutine());
        }

        private IEnumerator ReLoadCoroutine()
        {
            _isReloading = true;
            _renderer.PlayClip(reloadParam.ParamHash, 0, 0.01f);
            yield return new WaitForSeconds(reloadTime);
            _isReloading = false;
            _currentAmmo = maxAmmo;
        }

        public void Release()
        {
            _isHolding = false;
        }
        
        private void ApplyShotSlow() 
            => _statModule.AddModifier(moveSpeedStat, this, -moveSpeedSlow, StatModifierType.PercentAdd); 

        private void RemoveShotSlow() 
            => _statModule.RemoveModifier(moveSpeedStat, this); 

        public override void StopSkill()
        {
            if (_shotCoroutine != null)
            {
                StopCoroutine(_shotCoroutine);
                _shotCoroutine = null;
            }
            _isHolding = false;
            _movement.CanAutoRotate = true;
            RemoveShotSlow();
            base.StopSkill();
        }
    }
}
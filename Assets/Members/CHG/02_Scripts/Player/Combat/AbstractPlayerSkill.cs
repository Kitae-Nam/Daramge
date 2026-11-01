using System;
using CHG._02_Scripts.Agent;
using CHG._02_Scripts.CombatSystem;
using UnityEngine;

namespace CHG._02_Scripts.Player.Combat
{
    public abstract class AbstractPlayerSkill : MonoBehaviour, ISkill
    {
        public event Action OnSkillEnd;
        [field: SerializeField] public SkillDataSO SkillData { get; private set; }

        protected PlayerController _player;
        protected PlayerSkillModule _skillModule;
        protected IRenderer _renderer;
        protected IControlMovement _movement;
        protected float _lastUseTime;
        
        public bool IsUsing { get; private set; }
        public float NormalizedCooldown => Mathf.Approximately(SkillData.cooldown, 0) 
            ? 1f : Mathf.Clamp01((Time.time - _lastUseTime) / SkillData.cooldown);
        
        public virtual void InitializeSkill(ISkillModule skillModule)
        {
            _skillModule = skillModule as PlayerSkillModule;
            Debug.Assert(_skillModule != null, $"parentScript is not a PlayerSkillModule : {gameObject.name}");
            
            _player = _skillModule.Player;
            _renderer = _player.GetModule<IRenderer>();
            Debug.Assert(_renderer != null, $"renderer is null : {gameObject.name}");
            _movement = _player.GetModule<IControlMovement>();
            Debug.Assert(_movement != null, $"movement is null : {gameObject.name}");
            IsUsing = false;
        }

        public abstract bool CanUseSkill(GameObject target = null);

        public virtual void UseSkill(GameObject target = null)
        {
            IsUsing = true;
        }

        public virtual void StopSkill()
        {
            IsUsing = false;
            _lastUseTime = Time.time;
            OnSkillEnd?.Invoke();
        }
    }
}
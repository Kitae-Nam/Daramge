using System;
using Members.CHG._02_Scripts.Agent;
using Members.CHG._02_Scripts.CombatSystem;
using UnityEngine;

namespace Members.CHG._02_Scripts.Player.Combat
{
    public abstract class AbstractPlayerSkill : MonoBehaviour, ISkill
    {
        public event Action OnSkillEnd;
        [field: SerializeField] public SkillDataSO SkillData { get; private set; }

        protected PlayerController _player;
        protected PlayerSkillModule _skillModule;
        protected AgentRenderer _renderer;
        protected IControlMovement _movement;
        protected float _lastUseTime;
        
        public bool IsUsing { get; private set; }
        public float NormalizedCooldown => Mathf.Approximately(SkillData.cooldown, 0) 
            ? 1f : Mathf.Clamp01((Time.time - _lastUseTime) / SkillData.cooldown);
        
        public virtual void InitializeSkill(ISkillModule skillModule)
        {
            _skillModule = skillModule as PlayerSkillModule;
            Debug.Assert(_skillModule != null, "플레이어 스킬은 반드시 플레이어 스킬 모듈의 자식이어야 함");
            
            _player = _skillModule.Player;
            _renderer = _player.GetModule<AgentRenderer>();
            Debug.Assert(_renderer != null, "플레이어에 렌더러 모듈이 없습니다.");
            _movement = _player.GetModule<IControlMovement>();
            Debug.Assert(_movement != null, "플레이어의 이동모듈이 없습니다.");
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
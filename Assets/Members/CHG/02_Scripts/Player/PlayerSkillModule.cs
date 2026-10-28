using System;
using System.Collections.Generic;
using CHG._02_Scripts.Agent;
using CHG._02_Scripts.Agent.StatSystem;
using CHG._02_Scripts.CombatSystem;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using CHG._02_Scripts.Player.Combat;
using CHG._02_Scripts.Player.FSM;
using GGMLib.EventSystem;
using GGMLib.ModuleSystems;
using UnityEngine;

namespace CHG._02_Scripts.Player
{
    public class PlayerSkillModule : MonoBehaviour, IModule, ISkillModule, IAfterInitModule
    {
        
        public float AttackSpeed { get; private set; } = 1f;
        public ModuleOwner Owner { get; private set; }
        public PlayerController Player { get; private set; }
        public event Action OnCurrentSkillEnd;
        [field: SerializeField] public GameEventChannelSO CreateChannel { get; private set; }
        
        [SerializeField] private AnimParamSO attackSpeedParam;
        [SerializeField] private StatSO attackSpeedStat;

        private IStatModule _statModule;
        private IRenderer _renderer;
        private Dictionary<int, ISkill> _skillDict;
        private ISkill _currentSkill;
        
        public void Initialize(ModuleOwner owner)
        {
            Owner = owner;
            Player = owner as PlayerController;
            Debug.Assert(Player != null, $"this Object not Player child : {gameObject.name}");

            _skillDict = new Dictionary<int, ISkill>();
            foreach (ISkill skill in GetComponentsInChildren<ISkill>())
            {
                Debug.Assert(skill.SkillData != null, $"skill data is null : {gameObject.name}");
                if (!_skillDict.TryAdd(skill.SkillData.skillIndex, skill))
                {
                    Debug.LogWarning($"skill is duplicated in child : {skill.SkillData.skillIndex}");
                    continue;
                }
                skill.InitializeSkill(this);
            }
            
            _statModule = Owner.GetModule<IStatModule>();
            _renderer = Owner.GetModule<IRenderer>();
            Debug.Assert(_statModule != null, $"statModule is null : {gameObject.name}");
            Debug.Assert(_renderer != null, $"renderer is null : {gameObject.name}");
        }

        public void AfterInit()
        {
            Player.PlayerInput.OnSkillKeyPressed += HandleSkillKeyPressed;
            Player.PlayerInput.OnSkillKeyReleased += HandleSkillKeyReleased;

            AttackSpeed = _statModule.SubscribeStat(attackSpeedStat.AssetIndex, HandleAttackSpeedChange,
                attackSpeedStat.BaseValue);
            _renderer.Animator.SetFloat(attackSpeedParam.ParamHash, AttackSpeed);
        }

        

        private void OnDestroy()
        {
            if (Player != null && Player.PlayerInput != null)
            {
                Player.PlayerInput.OnSkillKeyPressed -= HandleSkillKeyPressed;
                Player.PlayerInput.OnSkillKeyReleased -= HandleSkillKeyReleased;
            }

            _statModule?.UnSubscribeStat(attackSpeedStat.AssetIndex, HandleAttackSpeedChange);
        }
        
        private void HandleAttackSpeedChange(StatSO stat, float currentValue, float prevValue)
        {
            AttackSpeed = currentValue;
            _renderer.Animator.SetFloat(attackSpeedParam.ParamHash, AttackSpeed);
        }

        private void HandleSkillKeyPressed(PlayerSkillSlot slot)
        {
            if (CanUseSkill((int)slot))
            {
                Player.ChangeState(PlayerState.SKILL, 0); 
                UseSkill((int)slot);
            }
        }

        private void HandleSkillKeyReleased(PlayerSkillSlot slot)
        {
            if (_currentSkill is IReleasable releasable 
                && _currentSkill.SkillData.skillIndex == (int)slot
                && releasable.IsHolding)
                releasable.Release();
        }
        

        public bool CanUseSkill(int skillIndex, GameObject target = null)
        {
            
            if (_currentSkill is { IsUsing: true })
                return false; 
            if (_skillDict.TryGetValue(skillIndex, out ISkill skill))
            {
                return skill.CanUseSkill(target);
            }

            return false;
        }

        public void UseSkill(int skillIndex, GameObject target = null)
        {
            if (_skillDict.TryGetValue(skillIndex, out ISkill skill))
            {
                if (_currentSkill != null)
                    _currentSkill.OnSkillEnd -= HandleCurrentSkillEnd;
                _currentSkill = skill;
                _currentSkill.OnSkillEnd += HandleCurrentSkillEnd;
                _currentSkill.UseSkill(target);
            }
        }

        private void HandleCurrentSkillEnd()
        {
            _currentSkill.OnSkillEnd -= HandleCurrentSkillEnd;
            InvokeSkillEnd();
            _currentSkill = null;
        }

        public void InvokeSkillEnd() => OnCurrentSkillEnd?.Invoke();
        public void StopSkillIfNotFinished()
        {
            if (_currentSkill != null)
            {
                _currentSkill.StopSkill();
                _currentSkill = null;
            }
        }
    }
}
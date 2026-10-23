using System;
using System.Collections.Generic;
using System.Linq;
using GGMLib.EventSystem;
using GGMLib.ModuleSystems;
using Members.CHG._02_Scripts.Agent;
using Members.CHG._02_Scripts.Agent.StatSystem;
using Members.CHG._02_Scripts.CombatSystem;
using Members.CHG._02_Scripts.CoreSystem.AnimationSystem;
using Members.CHG._02_Scripts.Player.Combat;
using Members.CHG._02_Scripts.Player.FSM;
using UnityEngine;

namespace Members.CHG._02_Scripts.Player
{
    public class PlayerSkillModule : MonoBehaviour, IModule, ISkillModule, IAfterInitModule
    {
        [field: SerializeField] public GameEventChannelSO CreateChannel { get; private set; }
        [SerializeField] private AnimParamSO attackSpeedParam;
        [SerializeField] private StatSO attackSpeedStat;
        
        public ModuleOwner Owner { get; private set; }
        public PlayerController Player { get; private set; }
        public event Action OnCurrentSkillEnd;

        private IStatModule _statModule;
        private IRenderer _renderer;
        
        private Dictionary<int, ISkill> _skillDict;
        private ISkill _currentSkill;
        private float _atkSpeed = 1f;
        
        public void Initialize(ModuleOwner owner)
        {
            Owner = owner;
            Player = owner as PlayerController;
            Debug.Assert(Player != null, "플레이어 스킬 모듈은 플레이어의 자식으로 존재해야 합니다.");

            _skillDict = GetComponentsInChildren<ISkill>()
                .ToDictionary(skill => skill.SkillData.skillIndex);
            
            foreach(ISkill skill in _skillDict.Values)
                skill.InitializeSkill(this);
            
            _statModule = Owner.GetModule<IStatModule>();
            _renderer = Owner.GetModule<IRenderer>();
            Debug.Assert(_statModule != null && _renderer != null, "스킬 구동을 위한 필수 모듈 누락");
        }

        public void AfterInit()
        {
            Player.PlayerInput.OnAttackKeyPressed += HandleAttackKeyPressed;
            Player.PlayerInput.OnDashKeyPressed += HandleDashKeyPressed;
            Player.PlayerInput.OnFirstSkillKeyPressed += HandleFirstSkillKeyPressed;
            Player.PlayerInput.OnScendSkillKeyPressed += HandleScendSkillKeyPressed;
            Player.PlayerInput.OnFirstSkillKeyReleased += HandleChargeKeyReleased;
            Player.PlayerInput.OnScendSkillKeyReleased += HandleChargeKeyReleased;

            _atkSpeed = _statModule.SubscribeStat(attackSpeedStat.AssetIndex, HandleAttackSpeedChange,
                attackSpeedStat.BaseValue);
            _renderer.Animator.SetFloat(attackSpeedParam.ParamHash, _atkSpeed);
        }
        
        private void OnDestroy()
        {
            if (Player != null && Player.PlayerInput != null)
            {
                Player.PlayerInput.OnAttackKeyPressed -= HandleAttackKeyPressed;
                Player.PlayerInput.OnDashKeyPressed -= HandleDashKeyPressed;
                Player.PlayerInput.OnFirstSkillKeyPressed -= HandleFirstSkillKeyPressed;
                Player.PlayerInput.OnScendSkillKeyPressed -= HandleScendSkillKeyPressed;
                Player.PlayerInput.OnFirstSkillKeyReleased -= HandleChargeKeyReleased;
                Player.PlayerInput.OnScendSkillKeyReleased -= HandleChargeKeyReleased;
                
                
            }

            _statModule?.UnSubscribeStat(attackSpeedStat.AssetIndex, HandleAttackSpeedChange);
        }




        private void HandleAttackSpeedChange(StatSO stat, float currentValue, float prevValue)
        {
            _atkSpeed = currentValue;
            _renderer.Animator.SetFloat(attackSpeedParam.ParamHash, _atkSpeed);
        }


        private void HandleAttackKeyPressed()
        {
            if (CanUseSkill(0))
            {
                Player.ChangeState(PlayerState.SKILL, 0); 
                UseSkill(0);
            }
        }
        
        private void HandleDashKeyPressed()
        {
            if (CanUseSkill(1))
            {
                Player.ChangeState(PlayerState.SKILL, 0);
                UseSkill(1);
            }
        }
        
        private void HandleFirstSkillKeyPressed()
        {
            if (CanUseSkill(2))
            {
                Player.ChangeState(PlayerState.SKILL, 0);
                UseSkill(2);
            }
        }
        
        private void HandleScendSkillKeyPressed()
        {
            if (CanUseSkill(3))
            {
                Player.ChangeState(PlayerState.SKILL, 0);
                UseSkill(3);
            } 
        }
        
        private void HandleChargeKeyReleased()
        {
            if (_currentSkill is IChargeSkill chargeSkill && chargeSkill.ChargeLogic.IsCharging)
                chargeSkill.ReleaseCharge();
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
using System;
using System.Collections.Generic;
using CHG._02_Scripts.CombatSystem;
using GGMLib.ModuleSystems;
using UnityEngine;

namespace NKT.Enemy.Skill
{
    public class EnemySkillModule : MonoBehaviour, IModule, ISkillModule
    {
        public ModuleOwner Owner { get; private set; }
        public AbstractEnemy Enemy { get; private set; }
        public event Action OnCurrentSkillEnd;

        //armored 스킬을 쓰는 중이면 맞아도 경직되지 않는다.
        public bool IsArmored => _currentSkill is { IsUsing: true }
                                 && _currentSkill.SkillData is EnemySkillDataSO { armored: true };

        private readonly Dictionary<int, ISkill> _skillDict = new();
        private ISkill _currentSkill;

        public void Initialize(ModuleOwner owner)
        {
            Owner = owner;
            Enemy = owner as AbstractEnemy;
            Debug.Assert(Enemy != null, "적의 스킬 모듈은 AbstractEnemy의 자식이어야 합니다.");

            foreach (ISkill skill in GetComponentsInChildren<ISkill>(true))
            {
                if (!_skillDict.TryAdd(skill.SkillData.skillIndex, skill))
                {
                    Debug.LogError($"스킬 번호 {skill.SkillData.skillIndex}가 중복됩니다. {gameObject.name}");
                    continue;
                }
                skill.InitializeSkill(this);
            }
        }

        public bool CanUseSkill(int skillIndex, GameObject target = null)
        {
            if (_currentSkill is { IsUsing: true })
                return false;

            return _skillDict.TryGetValue(skillIndex, out ISkill skill) && skill.CanUseSkill(target);
        }

        public void UseSkill(int skillIndex, GameObject target = null)
        {
            if (!_skillDict.TryGetValue(skillIndex, out ISkill skill))
            {
                Debug.LogError($"스킬 번호 {skillIndex}가 없습니다. {gameObject.name}");
                return;
            }

            if (_currentSkill != null)
                _currentSkill.OnSkillEnd -= InvokeSkillEnd;

            _currentSkill = skill;
            _currentSkill.OnSkillEnd += InvokeSkillEnd;
            skill.UseSkill(target);
        }

        public void InvokeSkillEnd()
        {
            if (_currentSkill != null)
                _currentSkill.OnSkillEnd -= InvokeSkillEnd;
            _currentSkill = null;
            OnCurrentSkillEnd?.Invoke();
        }

        public void StopSkillIfNotFinished()
        {
            if (_currentSkill is { IsUsing: true })
                _currentSkill.StopSkill();
        }
    }
}

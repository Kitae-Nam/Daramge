using System;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using GGMLib.ModuleSystems;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NKT.Enemy.Module
{
    public class SkillEventRelay : MonoBehaviour, IModule
    {
        public event Action<AnimParamSO> OnSkillEvent;
        public void Initialize(ModuleOwner owner) { }

        private void SkillEvent(Object eventObj)
        {
            if (eventObj is AnimParamSO evt) OnSkillEvent?.Invoke(evt);
            else Debug.LogWarning($"{name}: SkillEvent에 이벤트 에셋이 비어 있습니다.", this);
        }
    }
}
using GGMLib.ModuleSystems;
using UnityEngine;

namespace Members.CHG._02_Scripts.CombatSystem
{
    public class ActionDataModule : MonoBehaviour, IModule
    {
        public Vector3 HitPoint { get; set; }
        public Vector3 HitNormal { get; set; }
        public ModuleOwner Attacker { get; set; }

        private ModuleOwner _owner;
        
        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;
        }
    }
}
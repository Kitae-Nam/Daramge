using CHG._02_Scripts.CombatSystem;
using GGMLib.ModuleSystems;
using UnityEngine;

namespace CHG._02_Scripts.Player
{
    public class PlayerCombatStateModule : MonoBehaviour, IModule, ICombatState
    {
        [SerializeField] private bool isCombat;
        public bool IsCombat => isCombat;
        public void Initialize(ModuleOwner owner) { }

        public void SetCombat(bool isCombat) => this.isCombat = isCombat;
    }
}
using System;
using System.Collections.Generic;
using CHG._02_Scripts.CombatSystem;
using CHG._02_Scripts.Player;
using CHG._02_Scripts.Player.Combat;
using CHG._02_Scripts.UI.Presenter;
using UnityEngine.UIElements;

namespace CHG._02_Scripts.UI
{
    public class SkillBarPresenter : IDisposable
    {
        private const string SlotClass = "skill-slot";
        private const string IconClass = "skill-slot__icon";
        private const string CooldownClass = "skill-slot__cooldown";
        private const string KeyClass = "skill-slot__key";

        private readonly VisualElement _container;
        private readonly List<(ISkill skill, VisualElement root, CooldownOverlay cooldown)> _slots = new();

        public SkillBarPresenter(VisualElement container, ISkillProvider provider, PlayerInputSO input,
            IReadOnlyList<PlayerSkillSlot> displaySlots)
        {
            _container = container;
            foreach (PlayerSkillSlot slot in displaySlots)
            {
                if (provider.TryGetSkill((int)slot, out ISkill skill))
                    AddSlot(skill, input.GetSkillKeyName(slot));
            }
        }

        public void Tick()
        {
            foreach ((ISkill skill, _, CooldownOverlay cooldown) in _slots)
                cooldown.Value = 1f - skill.NormalizedCooldown;
        }
        
        public void Dispose()
        {
            foreach ((_, VisualElement root, _) in _slots)
                root.RemoveFromHierarchy();
            _slots.Clear();
        }

        private void AddSlot(ISkill skill, string keyName)
        {
            VisualElement root = new() { pickingMode = PickingMode.Ignore };
            root.AddToClassList(SlotClass);
            
            VisualElement icon = new() {pickingMode = PickingMode.Ignore};
            icon.AddToClassList(IconClass);
            EquipmentUIUtility.SetIcon(icon, skill.SkillData.Icon);

            CooldownOverlay cooldown = new();
            cooldown.AddToClassList(CooldownClass);
            
            Label key = new(keyName) { pickingMode = PickingMode.Ignore};
            
            key.AddToClassList(KeyClass);

            root.Add(icon);
            root.Add(cooldown);
            root.Add(key);
            _container.Add(root);
            _slots.Add((skill, root, cooldown));
        }
    }
}
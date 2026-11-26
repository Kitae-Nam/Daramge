using System.Collections.Generic;
using CHG._02_Scripts.CombatSystem.DotSystem;
using GGMLib.ModuleSystems;
using GGMLib.ObjectPool.Runtime;
using UnityEngine;

namespace CHG._02_Scripts.CoreSystem.EffectSystem
{
    public class DotVfxView : MonoBehaviour, IModule
    {
        private struct ActiveVfx
        {
            public PoolableVfx Vfx;
            public Transform PoolParent;
        }

        [SerializeField] private PoolManagerSO poolManager;

        private readonly Dictionary<DotSO, ActiveVfx> _activeVfx = new();
        private IDotModule _dotModule;

        public void Initialize(ModuleOwner owner)
        {
            _dotModule = owner.GetModule<IDotModule>();
            Debug.Assert(_dotModule != null, $"dotModule is null : {gameObject.name}");
            Debug.Assert(poolManager != null, $"poolManager is null : {gameObject.name}");

            _dotModule.OnDotStarted += HandleDotStarted;
            _dotModule.OnDotEnded += HandleDotEnded;
        }

        private void OnDestroy()
        {
            if (_dotModule != null)
            {
                _dotModule.OnDotStarted -= HandleDotStarted;
                _dotModule.OnDotEnded -= HandleDotEnded;
            }

            foreach (ActiveVfx active in _activeVfx.Values)
                Release(active);
            _activeVfx.Clear();
        }

        private void HandleDotStarted(DotSO dot)
        {
            if (dot.VfxItem == null || _activeVfx.ContainsKey(dot)) return;

            PoolableVfx vfx = poolManager.Pop<PoolableVfx>(dot.VfxItem);
            if (vfx == null)
            {
                Debug.LogWarning($"pool item is not registered : {dot.VfxItem.name}");
                return;
            }

            Transform poolParent = vfx.transform.parent;
            vfx.transform.SetParent(transform, false);
            vfx.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            vfx.PlayableVfx.PlayVfx();
            _activeVfx.Add(dot, new ActiveVfx { Vfx = vfx, PoolParent = poolParent });
        }

        private void HandleDotEnded(DotSO dot)
        {
            if (_activeVfx.Remove(dot, out ActiveVfx active))
                Release(active);
        }

        private void Release(ActiveVfx active)
        {
            if (active.Vfx == null) return;
            active.Vfx.transform.SetParent(active.PoolParent, false);
            poolManager.Push(active.Vfx);
        }
    }
}
using System;
using System.Collections;
using CHG._02_Scripts.CoreSystem.EventBusSystem;
using GGMLib.ModuleSystems;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CHG._02_Scripts.Player.Hold
{
    public class HoldModule : MonoBehaviour, IModule, IHolder
    {
        private const int FORCE_DROP_STEPS = 5;
        private const float SETTLE_THRESHOLD = 0.08f;
        
        public event Action<IHoldable> OnReleased;

        [SerializeField] private float holdDistance = 1f;
        [SerializeField] private float holdHeight = 0.5f;
        [SerializeField] private float settleSmoothTime = 0.15f;
        [SerializeField] private LayerMask placeBlockLayer;
        [SerializeField] private bool dropOnHit = true;

        private PlayerController _player;
        private Transform _ownerTrm;
        private Quaternion _rotationOffest;
        private Vector3 _settleVelocity;
        private bool _isSettling;
        private bool _isReleasing;
        
        public IHoldable Current { get; private set; }
        public bool IsHolding => Current != null;
        public bool IsBusy => _isSettling || _isReleasing;
        public Vector3 HoldPosition => _ownerTrm.position + _ownerTrm.forward
            * holdDistance + Vector3.up * holdHeight;
        
        public void Initialize(ModuleOwner owner)
        {
            _player = owner as PlayerController;
            Debug.Assert(_player != null, $"not Player child : {gameObject.name}");
            
            _ownerTrm = _player.transform;
            _player.OnHit.AddListener(HandlePlayerHit);
            
            EventBus<CharacterChangeStartEvent>.Subscribe(HandleCharacterChangeStart);
        }

        private void OnDestroy()
        {
            if (_player != null)
                _player.OnHit.RemoveListener(HandlePlayerHit);
            
            EventBus<CharacterChangeStartEvent>.Unsubscribe(HandleCharacterChangeStart);
        }

        private void LateUpdate()
        {
            if (Current == null || _isReleasing) return;

            Transform root = Current.Root;
            root.rotation = _ownerTrm.rotation * _rotationOffest;

            if (!_isSettling)
            {
                root.position = HoldPosition;
                return;
            }
            
            root.position = Vector3.SmoothDamp(root.position, HoldPosition, 
                ref _settleVelocity, settleSmoothTime);
            if (Vector3.Distance(root.position, HoldPosition) < SETTLE_THRESHOLD)
                _isSettling = false;
        }

        public bool Hold(IHoldable holdable)
        {
            if (IsHolding || holdable is not Object obj || !obj || !holdable.CanHold) return false;
            Current = holdable;
            holdable.BeginHold();
            _rotationOffest = Quaternion.Inverse(_ownerTrm.rotation) * holdable.Root.rotation;
            _settleVelocity = Vector3.zero;
            _isSettling = true;
            return true;
        }

        public bool CanPlace(Vector3 rootPosition) => IsHolding && Current.CanPlace(rootPosition, placeBlockLayer);

        public void Release(Vector3 rootPosition, float duration)
        {
            if (!IsHolding || _isReleasing) return;
            StartCoroutine(ReleaseCoroutine(rootPosition, duration));
        }

        private IEnumerator ReleaseCoroutine(Vector3 target, float duration)
        {
            _isReleasing = true;
            Transform root = Current.Root;
            Vector3 start = root.position;

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                root.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, elapsed / duration));
                yield return null;
            }

            root.position = target;
            FinishRelease();
        }

        private void FinishRelease()
        {
            IHoldable released = Current;
            Current = null;
            _isSettling = false;
            _isReleasing = false;

            released.EndHold();
            OnReleased?.Invoke(released);
        }

        private void ForceDrop()
        {
            if (!IsHolding) return;

            StopAllCoroutines();
            _isReleasing = false;

            Vector3 holdPos = HoldPosition;
            Vector3 center = _ownerTrm.position + Vector3.up * holdHeight;
            Vector3 target = holdPos;
            for (int i = 1; i <= FORCE_DROP_STEPS && !CanPlace(target); i++)
                target = Vector3.Lerp(holdPos, center, (float)i / FORCE_DROP_STEPS);

            Current.Root.position = target;
            FinishRelease();
        }

        private void HandlePlayerHit()
        {
            if (dropOnHit)
                ForceDrop();
        }

        private void HandleCharacterChangeStart(CharacterChangeStartEvent evt)
        {
            if (evt.Prev == _player)
                ForceDrop();
        }
    }
}
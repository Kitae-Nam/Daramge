using System;
using System.Collections;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using CHG._02_Scripts.Player;
using GGMLib.ModuleSystems;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CHG._02_Scripts.Agent
{
    public class InteractionModule : MonoBehaviour, IModule
    {
        [Header("Carry Settings")] 
        [SerializeField] private float pickupRange = 1.8f; 
        [SerializeField] private float carryDistance = 1.0f; 
        [SerializeField] private float carryHeight = 0.8f; 
        [SerializeField] private float carrySmoothTime = 0.15f; 
        [SerializeField] private float dropDuration = 0.25f; 

        [Header("Ground Detection")] 
        [SerializeField] private LayerMask groundLayer; 
        [SerializeField] private float groundRayHeight = 3f; 
        [SerializeField] private float groundRayDistance = 8f; 

        private PlayerController _player; 
        private Transform _ownerTrm; 
        private PushableCrate _heldCrate; 
        private Vector3 _carryVelocity; 
        private bool _isDropping; 
        [Header("Animation")] 
        [SerializeField] private AnimParamSO pickupParam; 
        [SerializeField] private AnimParamSO carryParam; 
        [SerializeField] private AnimParamSO dropParam; 
        [SerializeField] private AnimParamSO emptyParam; 
        [SerializeField] private AnimTriggerSO pickupTrigger; 
        [SerializeField] private AnimTriggerSO dropTrigger; 
        [SerializeField] private int animLayerIndex = 1; 
        [SerializeField] private float crossFadeDuration = 0.1f; 

        private IRenderer _renderer; 
        private AgentTrigger _trigger; 
        private PushableCrate _pendingCrate; 
        private bool _isWaitingTrigger; 
        
        public void Initialize(ModuleOwner owner)
        {
            _player = owner as PlayerController;
            Debug.Assert(_player != null, $"this Object not Player child : {gameObject.name}");

            _ownerTrm = _player.transform;
            _renderer = owner.GetModule<IRenderer>(); 
            _trigger = owner.GetModule<AgentTrigger>(); 
            Debug.Assert(_renderer != null, $"renderer is null : {gameObject.name}"); 
            Debug.Assert(_trigger != null, $"AgentTrigger is null : {gameObject.name}"); 

            _player.PlayerInput.OnInteractKeyPressed += HandleInteractKeyPressed;
            _trigger.Subscribe(pickupTrigger, HandlePickupTrigger); 
            _trigger.Subscribe(dropTrigger, HandleDropTrigger); 
        }

        private void LateUpdate() => UpdateHeldCrate(); 

        private void OnDestroy()
        {
            if (_player != null && _player.PlayerInput != null)
                _player.PlayerInput.OnInteractKeyPressed -= HandleInteractKeyPressed;

            if (_trigger == null) return; 
            _trigger.UnSubscribe(pickupTrigger, HandlePickupTrigger); 
            _trigger.UnSubscribe(dropTrigger, HandleDropTrigger); 
        }
        
        private void HandleInteractKeyPressed()
        {
            if (_isDropping || _isWaitingTrigger) return; 

            if (_heldCrate != null)
                TryDropCrate();
            else
                TryPickUpCrate();
        }

        private void TryPickUpCrate()
        {
            Collider[] nearbyColliders = Physics.OverlapSphere(_ownerTrm.position, pickupRange, ~0, QueryTriggerInteraction.Ignore);

            PushableCrate closestCrate = null;
            float closestDistance = float.MaxValue;

            foreach (Collider col in nearbyColliders)
            {
                PushableCrate crate = col.GetComponentInParent<PushableCrate>();
                if (crate == null) continue;
                if (crate.IsHeld || crate.IsTransitioning) continue;

                Vector3 toCrate = crate.transform.position - _ownerTrm.position;
                toCrate.y = 0f;

                // 플레이어 앞쪽에 있는 상자만 선택
                if (Vector3.Dot(_ownerTrm.forward, toCrate) <= 0f) continue;

                float distance = toCrate.sqrMagnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestCrate = crate;
                }
            }

            if (closestCrate == null) return;

            _pendingCrate = closestCrate; 
            _isWaitingTrigger = true; 
            PlayAnimation(pickupParam); 
        }

        private void TryDropCrate()
        {
            if (_heldCrate == null) return;
            if (_heldCrate.IsTransitioning) return;
            if (!TryGetDropPosition(out _)) return; 

            _isWaitingTrigger = true; 
            PlayAnimation(dropParam); 
        }

        private IEnumerator DropCrateRoutine(Vector3 targetPosition) 
        {
            _isDropping = true;

            PushableCrate crate = _heldCrate;
            crate.BeginDrop();

            Vector3 startPosition = crate.transform.position;
            float elapsed = 0f;

            while (elapsed < dropDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(dropDuration, 0.01f));

                // SmoothStep 형태의 부드러운 이동
                t = t * t * (3f - 2f * t);

                crate.SetCarryPosition(Vector3.Lerp(startPosition, targetPosition, t));
                yield return null;
            }

            crate.FinishDrop(targetPosition);

            _heldCrate = null;
            _carryVelocity = Vector3.zero;
            _isDropping = false;

            Debug.Log("상자를 내려놓았습니다.");
        }

        private void OnDrawGizmosSelected() 
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, pickupRange);
        }
        
                private void HandlePickupTrigger() 
        {
            if (!_isWaitingTrigger || _heldCrate != null) return;

            _isWaitingTrigger = false;
            PushableCrate crate = _pendingCrate;
            _pendingCrate = null;

            // 애니메이션 도중 상자가 사라졌거나 다른 곳에서 잡혔으면 취소
            if (crate == null || crate.IsHeld || crate.IsTransitioning)
            {
                PlayAnimation(emptyParam);
                return;
            }

            _heldCrate = crate;
            _carryVelocity = Vector3.zero;
            _heldCrate.PickUp();

            Debug.Log("상자를 집었습니다.");
        }

        private void HandleDropTrigger() 
        {
            if (!_isWaitingTrigger || _heldCrate == null) return;

            _isWaitingTrigger = false;

            // 애니메이션 동안 이동했을 수 있으므로 놓는 시점에 다시 검사
            if (!TryGetDropPosition(out Vector3 dropPosition))
            {
                PlayAnimation(carryParam);
                return;
            }

            StartCoroutine(DropCrateRoutine(dropPosition));
        }

        private bool TryGetDropPosition(out Vector3 dropPosition) 
        {
            dropPosition = Vector3.zero; 

            Vector3 dropXZ = _ownerTrm.position + _ownerTrm.forward * carryDistance;
            Vector3 rayOrigin = dropXZ + Vector3.up * groundRayHeight;

            if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundRayDistance, groundLayer, QueryTriggerInteraction.Ignore))
            {
                Debug.Log("내려놓을 바닥이 없습니다.");
                return false; 
            }

            Vector3 halfExtents = _heldCrate.HalfExtents;
            dropPosition = new Vector3(dropXZ.x, hit.point.y + halfExtents.y + 0.02f, dropXZ.z); 

            Physics.SyncTransforms();

            Collider[] overlaps = Physics.OverlapBox(dropPosition, halfExtents * 0.9f, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);

            foreach (Collider col in overlaps)
            {
                if ((groundLayer.value & (1 << col.gameObject.layer)) != 0) continue;
                if (col.transform == _ownerTrm || col.transform.IsChildOf(_ownerTrm)) continue;
                if (col.GetComponentInParent<PushableCrate>() == _heldCrate) continue;

                Debug.Log("해당 위치에는 상자를 놓을 수 없습니다.");
                return false; 
            }

            return true; 
        }
        // 캐릭터의 정면 방향을 기준으로 상자를 배 앞쪽에 위치시킨다.
        private Vector3 GetCarryPosition() 
            => _ownerTrm.position + _ownerTrm.forward * carryDistance + Vector3.up * carryHeight;

        private void UpdateHeldCrate() 
        {
            if (_heldCrate == null || _isDropping) return;

            Vector3 targetPosition = GetCarryPosition();

            // 상자를 처음 들어 올릴 때만 부드럽게 보간
            if (_heldCrate.IsTransitioning)
            {
                Vector3 smoothPosition = Vector3.SmoothDamp(_heldCrate.transform.position, targetPosition, ref _carryVelocity, carrySmoothTime);
                _heldCrate.SetCarryPosition(smoothPosition);

                if (Vector3.Distance(_heldCrate.transform.position, targetPosition) < 0.08f)
                {
                    _heldCrate.SetCarryPosition(targetPosition);
                    _heldCrate.FinishPickUp();
                    _carryVelocity = Vector3.zero;
                }
            }
            else
            {
                // 운반 중에는 지연 없이 정면 위치 유지
                _heldCrate.SetCarryPosition(targetPosition);
            }
        }
        private void PlayAnimation(AnimParamSO param) 
            => _renderer.PlayClip(param.ParamHash, 0f, crossFadeDuration, animLayerIndex);
    }
}
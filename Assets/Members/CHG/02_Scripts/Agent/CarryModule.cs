using System;
using System.Collections;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using CHG._02_Scripts.Player;
using CHG._02_Scripts.Player.FSM;
using CHG._02_Scripts.Player.Hold;
using GGMLib.ModuleSystems;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CHG._02_Scripts.Agent
{
    public class CarryModule : MonoBehaviour, IModule, ICarrier
    {
        [Header("Carry Settings")] 
        [SerializeField] private float dropGap = 0.05f;
        [SerializeField] private float dropDuration = 0.25f; 

        [Header("Ground Detection")] 
        [SerializeField] private LayerMask groundLayer; 
        [SerializeField] private float groundRayHeight = 3f; 
        [SerializeField] private float groundRayDistance = 8f; 


        [Header("Animation")] 
        [SerializeField] private AnimParamSO pickupParam; 
        [SerializeField] private AnimParamSO carryParam; 
        [SerializeField] private AnimParamSO dropParam; 
        [SerializeField] private AnimParamSO emptyParam; 
        [SerializeField] private AnimTriggerSO pickupTrigger; 
        [SerializeField] private AnimTriggerSO dropTrigger;
        [SerializeField] private AnimTriggerSO actionEndTrigger;
        [SerializeField] private int animLayerIndex = 1; 
        [SerializeField] private float crossFadeDuration = 0.1f; 

        private PlayerController _player;
        private CharacterController _characterController;  
        private IHolder _holder;
        private IHoldable _pending;
        private Transform _ownerTrm; 
        private IRenderer _renderer; 
        private AgentTrigger _trigger; 
        private bool _isWaitingTrigger;
        private bool _isCarrying;
        
        public bool IsActing { get; private set; }
        public bool IsCarrying  => _isCarrying;
        
        public void Initialize(ModuleOwner owner)
        {
            _player = owner as PlayerController;
            Debug.Assert(_player != null, $"this Object not Player child : {gameObject.name}");

            _ownerTrm = _player.transform;
            _renderer = owner.GetModule<IRenderer>();
            _trigger = owner.GetModule<AgentTrigger>();
            _holder = owner.GetModule<IHolder>();
            _characterController = owner.GetComponent<CharacterController>();  
            Debug.Assert(_renderer != null, $"renderer is null : {gameObject.name}");
            Debug.Assert(_trigger != null, $"AgentTrigger is null : {gameObject.name}");
            Debug.Assert(_holder != null, $"holder is null : {gameObject.name}");
            Debug.Assert(_characterController != null, $"CharacterController is null : {gameObject.name}");  

            _trigger.Subscribe(pickupTrigger, HandlePickupTrigger);
            _trigger.Subscribe(dropTrigger, HandleDropTrigger);
            _trigger.Subscribe(actionEndTrigger, HandleActionEnd);
            _holder.OnReleased += HandleReleased;
        }

        private void OnDestroy()
        {
            if (_holder != null)
                _holder.OnReleased -= HandleReleased;
            
            if (_trigger == null) return; 
            _trigger.UnSubscribe(pickupTrigger, HandlePickupTrigger); 
            _trigger.UnSubscribe(dropTrigger, HandleDropTrigger); 
            _trigger.UnSubscribe(actionEndTrigger, HandleActionEnd);
        }

        private void HandleReleased(IHoldable released)
        {
            if (!_isCarrying) return;

            _isCarrying = false;
            PlayAnimation(emptyParam);
            ActionEnd();
        }


        private void HandlePickupTrigger() 
        {
            if (!_isWaitingTrigger || _isCarrying) return;

            _isWaitingTrigger = false;
            IHoldable holdable = _pending;
            _pending = null;

            if (!_holder.Hold(holdable))
            {
                PlayAnimation(emptyParam);
                ActionEnd();
                return;
            }

            _isCarrying = true;
        }

        private void HandleDropTrigger()
        {
            if (!_isWaitingTrigger || !_isCarrying) return;

            _isWaitingTrigger = false;

            if (!TryGetDropPosition(out Vector3 dropPosition))
            {
                PlayAnimation(carryParam);
                ActionEnd(); 
                return;
            }

            _isCarrying = false;
            _holder.Release(dropPosition, dropDuration);
        }

        private void HandleActionEnd() => ActionEnd();

        private bool TryGetDropPosition(out Vector3 dropPosition)
        {
            dropPosition = Vector3.zero;

            Vector3 forward = _ownerTrm.forward;                                                        
            float bodyRadius = _characterController.radius + _characterController.skinWidth;            
            float dropDistance = bodyRadius + _holder.Current.GetReach(-forward) + dropGap;             
            Vector3 dropXZ = _ownerTrm.position + forward * dropDistance;                               
            Vector3 rayOrigin = dropXZ + Vector3.up * groundRayHeight;

            if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundRayDistance, groundLayer, QueryTriggerInteraction.Ignore))
            {
                Debug.Log("내려놓을 바닥이 없습니다.");
                return false;
            }

            dropPosition = new Vector3(dropXZ.x, hit.point.y + _holder.Current.BottomOffset + 0.02f, dropXZ.z);
            Physics.SyncTransforms();

            if (_holder.CanPlace(dropPosition)) return true;

            Debug.Log("해당 위치에는 놓을 수 없습니다.");
            return false;
        }

        public bool TryHandleInteract()
        {
            if (IsActing) return true;
            if (!_isCarrying) return false;
            
            TryDrop();
            return true;
        }
        
        private void PlayAnimation(AnimParamSO param) 
            => _renderer.PlayClip(param.ParamHash, 0f, crossFadeDuration, animLayerIndex);
        
        public void RequestPickUp(IHoldable holdable) 
        {
            if (holdable == null || !holdable.CanHold) return; 
            if (_holder.IsHolding || IsActing) return;          

            _pending = holdable;
            _isWaitingTrigger = true;
            BeginAction(pickupParam); 
        }

        private void TryDrop() 
        {
            if (_holder.IsBusy) return;
            if (!TryGetDropPosition(out _)) return;

            _isWaitingTrigger = true;
            BeginAction(dropParam); 
        }

        private void BeginAction(AnimParamSO param)
        {
            IsActing = true;
            _player.ChangeState(PlayerState.INTERACT, 0.1f);
            PlayAnimation(param);
        }

        private void ActionEnd()
        {
            IsActing = false;
            _isWaitingTrigger = false;
            _pending = null;
        }
    }
}
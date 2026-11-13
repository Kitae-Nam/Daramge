using System;
using CHG._02_Scripts.CoreSystem.EventBusSystem;
using GGMLib.ModuleSystems;
using UnityEngine;

namespace CHG._02_Scripts.Player
{
    public class PlayerLockOnModule : MonoBehaviour, IModule, IAfterInitModule, ILockOnModule
    {
        [SerializeField] private LayerMask whatIsEnemy;
        [SerializeField] private float searchRadius = 10f;
        [SerializeField] private float releaseRadius = 13f;
        [SerializeField] private int maxSearchCount = 16;

        private PlayerController _player;
        private Collider[] _results;
        private Agent.Agent _target;
        private bool _isHolding;
        
        public bool HasTarget => _target != null;
        public Vector3 DirectionToTarget => HasTarget ?
            GetFlatOffset(_target.transform.position).normalized : _player.transform.forward;
        
        public void Initialize(ModuleOwner owner)
        {
            _player = owner as PlayerController;
            Debug.Assert(_player != null, $"this Object not Player child : {gameObject.name}");
            Debug.Assert(releaseRadius >= searchRadius, $"releaseRadius is smaller than searchRadius : {gameObject.name}");
            _results = new Collider[maxSearchCount];
        }

        public void AfterInit()
        {
            _player.PlayerInput.OnLockOnKeyPressed += HandleLockOnKeyPressed;
            _player.PlayerInput.OnLockOnKeyReleased += HandleLockOnKeyReleased;
            // EventBus<CharacterChangeStartEvent>.Subscribe(HandleCharacterChangeStarted);
        }

        private void Update()
        {
            if (!_isHolding) return;
            
            if(ReferenceEquals(_target, null))
                SetTarget(FindNearestEnemy());
            else if (!IsTargetValid())
                SetTarget(null);
        }

        private void OnDestroy()
        {
            if (_player != null && _player.PlayerInput != null)
            {
                _player.PlayerInput.OnLockOnKeyPressed -= HandleLockOnKeyPressed;
                _player.PlayerInput.OnLockOnKeyReleased -= HandleLockOnKeyReleased;
            }
            // EventBus<CharacterChangeStartEvent>.Unsubscribe(HandleCharacterChangeStarted);
        }

        private void HandleLockOnKeyPressed() => _isHolding = true;

        private void HandleLockOnKeyReleased()
        {
            _isHolding = false;
            SetTarget(null);
        }

        private void HandleCharacterChangeStarted(CharacterChangeStartEvent evt)
        {
            if (evt.Prev == _player)
                HandleLockOnKeyReleased();
        }

        private void SetTarget(Agent.Agent target)
        {
            if (ReferenceEquals(_target, target)) return;
            
            _target = target;
            Transform targetTrm = target != null ? target.transform : null;
            EventBus<LockOnTargetChangedEvent>.Raise(new LockOnTargetChangedEvent(_player, targetTrm));
        }

        private Agent.Agent FindNearestEnemy()
        {
            int count = Physics.OverlapSphereNonAlloc(_player.transform.position, searchRadius, _results, whatIsEnemy);
            Agent.Agent nearest = null;
            float nearestSqrDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Agent.Agent agent = _results[i].GetComponentInParent<Agent.Agent>();
                if (agent == null || agent.IsDead) continue;    
                
                float sqrDistance = GetFlatOffset(agent.transform.position).sqrMagnitude;
                if (sqrDistance < nearestSqrDistance)
                {
                    nearestSqrDistance = sqrDistance;
                    nearest = agent;
                }
            }

            return nearest;
        }
        
        private bool IsTargetValid() => _target != null && !_target.IsDead 
            && _target.gameObject.activeInHierarchy
            && GetFlatOffset(_target.transform.position).sqrMagnitude <= releaseRadius * releaseRadius;
        
        private Vector3 GetFlatOffset(Vector3 position)
        {
            Vector3 offset = position - _player.transform.position;
            offset.y = 0f;
            return offset;
        }
    }
}
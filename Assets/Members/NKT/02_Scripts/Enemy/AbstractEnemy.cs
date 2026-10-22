using System;
using System.Collections.Generic;
using Members.CHG._02_Scripts.Agent;
using NKT.Enemy.BT;
using NKT.Enemy.BT.Events;
using NKT.Enemy.Module;
using Unity.Behavior;
using Unity.Behavior.GraphFramework;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

namespace NKT.Enemy
{
    [RequireComponent(typeof(BehaviorGraphAgent), typeof(NavMeshAgent))]
    public abstract class AbstractEnemy : Agent
    {
        [field:SerializeField] public EnemyDataSO EnemyData {get; private set;}
        
        public INavMovement NavMovement { get; private set; }
        public IRenderer Renderer { get; private set; }
        public AgentSensor AgentSensor { get; private set; }
        public AgentTrigger AgentTrigger { get; private set; }
        public BehaviorGraphAgent BTAgent { get; private set; }
        public StateChannel  StateChannel { get; private set; }
        public Vector3 HomePoint => _homePoint;

        private IReadOnlyDictionary<string, SerializableGUID> _guids;
        private Vector3 _homePoint;
        
        protected override void InitializeModules()
        {
            base.InitializeModules();
            NavMovement = GetModule<INavMovement>();
            Renderer = GetModule<IRenderer>();
            AgentSensor = GetModule<AgentSensor>();
            AgentTrigger = GetModule<AgentTrigger>();
            BTAgent = GetComponent<BehaviorGraphAgent>();
        }

        public virtual void Start()
        {
            SetBTVariable(BTVar.Enemy, this);
            if (BTAgent.GetVariable(BTVar.StateChannel, out BlackboardVariable<StateChannel> channel))
                StateChannel = channel.Value;
            else
                Debug.LogError($"{name}: 블랙보드에 StateChannel이 없습니다.");

            _homePoint = transform.position;
        }

        protected override void HandleHitEvent()
        {
            if (IsDead) return;
            StateChannel?.SendEventMessage(EnemyState.HIT);
        }

        protected override void HandleDeath()
        {
            base.HandleDeath();
            foreach (Collider c in GetComponents<Collider>())
                c.enabled = false;
            StateChannel?.SendEventMessage(EnemyState.DEATH);
        }
        #region BT Helper

        protected bool SetBTVariable<T>(string varName, T value)
        {
            if(_guids != null && _guids.TryGetValue(varName, out var guid))
                return BTAgent.SetVariableValue(guid, value);
            
            Debug.LogError($"BT변수 미해결 : {varName}");
            return false;
        }

        protected bool GetBTVariable<T>(string varName, out BlackboardVariable<T> variable)
        {
            if(_guids != null && _guids.TryGetValue(varName, out var guid))
                return BTAgent.GetVariable(guid, out variable);

            Debug.LogError($"BT변수 미해결 : {varName}");
            variable = null;
            return false;
        }

        #endregion
    }
}
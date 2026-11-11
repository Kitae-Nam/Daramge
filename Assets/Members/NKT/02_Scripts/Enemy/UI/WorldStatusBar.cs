using System;
using CHG._02_Scripts.Agent;
using CHG._02_Scripts.CombatSystem;
using GGMLib.ModuleSystems;
using UnityEngine;
using UnityEngine.UIElements;

namespace NKT.Enemy.UI
{
    [RequireComponent(typeof(PanelRenderer))]
    public class WorldStatusBar : MonoBehaviour, IModule ,IAfterInitModule
    {
        [SerializeField] private PanelRenderer barDocument;

        [Header("Viewmodel Template")] 
        [SerializeField] private HealthViewModelSO healthTemplate;
        
        [SerializeField] private float lagSpeed = 10f; //추격바가 따라오는 속도(health/sec)

        #region UI Link section
        
        [Header("Elements")] 
        [SerializeField] private VisualElementReference<VisualElement> rootRef;
        [SerializeField] private VisualElementReference<VisualElement> hpBarRef;

        private VisualElement _rootElement;
        private VisualElement _hpBarElement;
        
        private void OnEnable()
        {
            rootRef.RegisterReferenceResolvedCallback(OnRootLabelResolved);
            hpBarRef.RegisterReferenceResolvedCallback(OnHpBarResolved);
        }


        private void OnDisable()
        {
            rootRef.UnregisterReferenceResolvedCallback(OnRootLabelResolved);
            hpBarRef.UnregisterReferenceResolvedCallback(OnHpBarResolved);
            _rootElement = null;
            _hpBarElement = null;
        }

        private void OnHpBarResolved(VisualElement obj)
        {
            _hpBarElement = obj;
            if(_realHealthVM != null)
                _hpBarElement.dataSource = _realHealthVM;
        }

        private void OnRootLabelResolved(VisualElement obj)
        {
            _rootElement = obj;
            if (_health != null)
                ApplyVisibility(_health.CurrentHealth, _health.MaxHealth);
        }

        #endregion

        private AbstractEnemy _enemyOwner;
        private HealthViewModelSO _realHealthVM; //클론한 실제 뷰 모델
        private Agent _target;
        private HealthModule _health;
        private float _heightOffset;
        private float _displayLag;
        private bool _isBound;

        private void Awake()
        {
            if (barDocument == null)
            {
                barDocument = GetComponent<PanelRenderer>();
            }
        }
        
        public void Initialize(ModuleOwner owner)
        {
            _enemyOwner = owner as AbstractEnemy;
            _realHealthVM = Instantiate(healthTemplate);
            if(_hpBarElement != null)
                _hpBarElement.dataSource = _realHealthVM;
        }
        
        public void AfterInit()
        {
            _health = _enemyOwner.GetModule<HealthModule>();
            Bind(_enemyOwner, _enemyOwner!.HeightOffset);
        }

        //타겟 적에데가 바를 붙이는 Bind 매서드
        public void Bind(Agent target, float heightOffset)
        {
            if (_isBound)
                UnBind();
            
            _target = target;
            _heightOffset = heightOffset;

            if (_health != null)
            {
                _health.OnHealthChanged += HandleHealthChanged;
                _health.OnDeath += HandleTargetDead;
                _realHealthVM.Set(_health.CurrentHealth, _health.MaxHealth);
                _displayLag = _health.CurrentHealth;
                _realHealthVM.SetLag(_displayLag);
            }

            FollowTarget(); //타겟 오브젝트를 따라다니도록 셋팅하고
            
            ApplyVisibility(_health.CurrentHealth, _health.MaxHealth);
            
            _isBound = true; //바운드 되었음을 알림.(변수명 변경했어 _bound -> _isBound)
        }

        private void ApplyVisibility(float current, float max)
        {
            if (_rootElement == null) return; 
            _rootElement.style.display = current < max - 0.0001f ?  DisplayStyle.Flex :  DisplayStyle.None;
        }
        
        private void FollowTarget()
            => transform.position = _target.transform.position + Vector3.up * _heightOffset;

        private void LateUpdate()
        {
            if (!_isBound) return;
            if (_target == null)
            {
                UnBind();
                return;
            }
            
            FollowTarget();

            float current = _health.CurrentHealth;
            float newLag = current > _displayLag 
                ? current 
                : Mathf.MoveTowards(_displayLag, current, lagSpeed * Time.deltaTime);

            if (!Mathf.Approximately(newLag, _displayLag))
            {
                _displayLag = newLag;
                _realHealthVM.SetLag(_displayLag);
            }
        }

        private void UnBind()
        {
            if (_health != null)
            {
                _health.OnHealthChanged -= HandleHealthChanged;
                _health.OnDeath -= HandleTargetDead;
            }

            _target = null;
            _health = null;
            _isBound = false;
        }

        private void HandleHealthChanged(float current, float max)
        {
            _realHealthVM.Set(current, max);
            ApplyVisibility(current, max);
        }

        private void HandleTargetDead()
        {
            _rootElement.style.display = DisplayStyle.None;
        }

        private void OnDestroy()
        {
            if(_realHealthVM != null)
                Destroy(_realHealthVM);
        }
    }
}
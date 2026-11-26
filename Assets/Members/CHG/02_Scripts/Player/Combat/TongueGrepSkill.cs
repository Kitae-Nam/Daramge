using System;
using System.Collections;
using CHG._02_Scripts.CombatSystem;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using CHG._02_Scripts.CoreSystem.EventBusSystem;
using CHG._02_Scripts.Player.Combat.Tongue;
using CHG._02_Scripts.Player.Hold;
using UnityEngine;

namespace CHG._02_Scripts.Player.Combat
{
    public class TongueGrepSkill : AbstractPlayerSkill, IReleasable, IExclusiveSkill
    {
        
        [SerializeField] private AnimParamSO grepParam;
        [SerializeField] private AnimParamSO aimingParam;
        [SerializeField] private AnimParamSO dragParam;
        [Space(10)]
        [SerializeField] private LayerMask hitLayer;
        [SerializeField] private LineRenderer tongueLine;
        [SerializeField] private Transform startTrm;
        [SerializeField] private float maxRange = 10f;
        [SerializeField] private float aimSpeed = 30f;
        [SerializeField] private float shootSpeed = 10f;
        [SerializeField] private float pullSpeed = 10f;
        [SerializeField] private float tipRadius = 0.3f;
        [SerializeField] private float endPointOffset = 0.5f;
        [SerializeField] private float maxPullTime = 2f;
        [SerializeField] private float objectPullSpeed = 15f;
        [Space(10)]
        [SerializeField] private OverlapDamageCaster damageCaster;
        [SerializeField] private Gradient aimColor;
        [SerializeField] private Gradient tongueColor;

        private ILockOnModule _lockOn;
        
        public bool IsHolding { get; private set; }
        private IHolder _holder;
        private bool _isTongueHolding;
        public bool IsExclusive => _isTongueHolding;
        

        public override void InitializeSkill(ISkillModule skillModule)
        {
            base.InitializeSkill(skillModule);
            Debug.Assert(damageCaster != null, $"DamageCaster is null : {gameObject.name}");
            damageCaster.InitCaster(player);
            _lockOn = player.GetModule<ILockOnModule>();
            _holder = player.GetModule<IHolder>();
            Debug.Assert(_lockOn != null, $"LockOnModule is null : {gameObject.name}");
            Debug.Assert(_holder != null, $"Holder is null : {gameObject.name}");
            _holder.OnReleased += HandleReleased;
        }

        private void LateUpdate()
        {
            if (!_isTongueHolding) return;   
            UpdateLine(_holder.Current.Root.position, tongueColor);
        }

        private void OnDestroy()
        {
            if (_holder != null)
                _holder.OnReleased -= HandleReleased;
        }

        public override bool CanUseSkill(GameObject target = null)
        {
            return _isTongueHolding || (NormalizedCooldown >= 1f && !IsUsing);
            
        }

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);

            if (_isTongueHolding)
            {
                Vector3 rootPosition = _holder.Current.Root.position;
                if (_holder.CanPlace(rootPosition))
                    _holder.Release(rootPosition, 0f);
                StopSkill();
                return;
            }

            movement.CanManualMove = false;
            movement.SetMovementVelocity(Vector3.zero);
            AimingTongue();
        }

        private void AimingTongue()
        {
            IsHolding = true;
            tongueLine.enabled = true;
            renderer.PlayClip(aimingParam.ParamHash, 0f, 0.01f);
            StartCoroutine(AimingCoroutine());
        }

        private IEnumerator AimingCoroutine()
        {
            float aimLength = 0f; 
            while (IsHolding)
            {
                aimLength = Mathf.Min(aimLength + aimSpeed * Time.deltaTime, maxRange); 
                Vector3 dir = GetAimDirection();
                Vector3 startPos = startTrm.position;
                movement.RotateTo(dir); 
                bool isHit = Physics.SphereCast(startPos, tipRadius, dir, out RaycastHit hit, aimLength, hitLayer); 
                UpdateLine(startPos + dir * (isHit ? hit.distance : aimLength), aimColor); 

                yield return null;
            }
        }

        public void Release()
        {
            IsHolding = false;
            StartCoroutine(GrepCoroutine());
        }

        private IEnumerator GrepCoroutine() 
        {
            Vector3 dir = GetAimDirection();
            movement.RotateTo(dir);
            Vector3 tip = startTrm.position;
            Vector3 startPos = startTrm.position;
            float moveDistance = 0f;
            Transform anchor = null;
            Vector3 localHit = Vector3.zero;
            ITonguePullable pullable = null;
            IHoldable holdable = null;
            bool isTongueEventHit = false;

            renderer.PlayClip(grepParam.ParamHash, 0f, 0.01f);
            while (moveDistance < maxRange)  //발사해서 닿거나 최대 사거리까지
            {
                float step = shootSpeed * Time.deltaTime;
                if (Physics.SphereCast(tip, tipRadius, dir, out RaycastHit hit, step, hitLayer))
                {
                    anchor = hit.transform;
                    localHit = anchor.InverseTransformPoint(hit.point);
                    damageCaster.ResetHitCaster();
                    damageCaster.CastDamage(hit.point, SkillData, 1f);
                    if (hit.collider.TryGetComponent<ITongueHitHandler>(out var handler))
                    {
                        handler?.HandleTongueHit(hit.point);
                        isTongueEventHit = true;
                    }
                    holdable = hit.collider.GetComponentInParent<IHoldable>();
                    if (holdable == null)
                        pullable = hit.collider.GetComponentInParent<ITonguePullable>(); 
                    break;
                }

                tip += dir * step;
                moveDistance += step;
                UpdateLine(tip, tongueColor);
                yield return null;
            }

            if (holdable != null && _holder.Hold(holdable)) 
            {
                _isTongueHolding = true; 
                _lockOn.SetBlocked(true);
            }
            else if (pullable != null) 
            {
                yield return PullObjectCoroutine(pullable);
                pullable.EndPull();
            }
            else if (anchor != null && !isTongueEventHit)
            {
                //맞았을 때 이동
                renderer.PlayClip(dragParam.ParamHash, 0f, 0.01f);
                movement.CanUseGravity = false;
                float t = 0f;
                while (anchor != null && t < maxPullTime)
                {
                    tip = anchor.TransformPoint(localHit);
                    Vector3 toTip = tip - player.transform.position;
                    if (toTip.magnitude <= endPointOffset) break;

                    movement.SetMovementVelocity(toTip.normalized * pullSpeed);
                    UpdateLine(tip, tongueColor);
                    t += Time.deltaTime;

                    yield return null;
                }
            }
            else //못 맞추면 회수
            {
                while (Vector3.Distance(tip, startPos) > 0.1f)
                {
                    tip = Vector3.MoveTowards(tip, startPos, shootSpeed * Time.deltaTime);
                    UpdateLine(tip, tongueColor);
                    yield return null;
                }
            }

            StopSkill();
        }

        public override void StopSkill()
        {
            StopAllCoroutines();
            movement.SetMovementVelocity(Vector3.zero);
            tongueLine.enabled = _isTongueHolding;
            IsHolding = false;
            movement.CanManualMove = true;
            movement.CanUseGravity = true;
            base.StopSkill();
        }
        
        private void UpdateLine(Vector3 tip, Gradient color)
        {
            tongueLine.colorGradient = color;
            tongueLine.SetPosition(0, startTrm.position);            
            tongueLine.SetPosition(1, tip);
        }

        private Vector3 GetAimDirection()
        {
            Vector3 playerPos = player.transform.position;
            Vector3 dir = player.PlayerInput.GetMousePositionOnPlane(playerPos.y) - playerPos;
            dir.y = 0f;
            return dir.normalized;
        }

        private IEnumerator PullObjectCoroutine(ITonguePullable pullable) 
        {
            pullable.BeginPull();
            Transform target = pullable.Root;
            while (Vector3.Distance(target.position, _holder.HoldPosition) > 0.05f) 
            {
                target.position = Vector3.MoveTowards(target.position,
                    _holder.HoldPosition, objectPullSpeed * Time.deltaTime); 
                UpdateLine(target.position, tongueColor);
                yield return null;
            }
        }

        private void HandleReleased(IHoldable released)
        {
            if (!_isTongueHolding) return;

            _isTongueHolding = false;
            tongueLine.enabled = false;
            _lockOn.SetBlocked(false);
        }
    }
}
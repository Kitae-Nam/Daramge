using System;
using System.Collections;
using CHG._02_Scripts.CombatSystem;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using CHG._02_Scripts.CoreSystem.EventBusSystem;
using CHG._02_Scripts.Player.Combat.Tongue;
using UnityEngine;

namespace CHG._02_Scripts.Player.Combat
{
    public class TongueGrepSkill : AbstractPlayerSkill, IReleasable, IExclusiveSkill
    {
        private const int FORCE_DROP_STEPS = 5;
        
        [SerializeField] private AnimParamSO grepParam;
        [SerializeField] private AnimParamSO aimingParam;
        [SerializeField] private AnimParamSO dragParam;
        [Space(10)]
        [SerializeField] private LayerMask hitLayer;
        [SerializeField] private LayerMask dropBlockLayer;
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
        [SerializeField] private float holdDistance = 1f;
        [SerializeField] private float holdHeight = 0.5f;
        [Space(10)]
        [SerializeField] private OverlapDamageCaster damageCaster;
        [SerializeField] private Gradient aimColor;
        [SerializeField] private Gradient tongueColor;

        private ITonguePullable _carried;
        private ILockOnModule _lockOn;
        
        public bool IsHolding { get; private set; }
        public bool IsExclusive => _carried != null;
        

        public override void InitializeSkill(ISkillModule skillModule)
        {
            base.InitializeSkill(skillModule);
            Debug.Assert(damageCaster != null, $"DamageCaster is null : {gameObject.name}");
            damageCaster.InitCaster(_player);
            _lockOn = _player.GetModule<ILockOnModule>();
            Debug.Assert(_lockOn != null, $"LockOnModule is null : {gameObject.name}");
            _player.OnHit.AddListener(HandlePlayerHit);
            EventBus<CharacterChangeStartEvent>.Subscribe(HandleCharacterChangeStart);
        }

        private void LateUpdate()
        {
            if (_carried == null) return;
            Vector3 holdPos = GetHoldPosition();
            _carried.Root.position = holdPos;
            UpdateLine(holdPos, tongueColor);
        }

        private void OnDestroy()
        {
            if (_player != null)
                _player.OnHit.RemoveListener(HandlePlayerHit);
            EventBus<CharacterChangeStartEvent>.Unsubscribe(HandleCharacterChangeStart);
        }

        public override bool CanUseSkill(GameObject target = null)
        {
            return _carried != null || (NormalizedCooldown >= 1f && !IsUsing);
        }

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);

            if (_carried != null)
            {
                if (_carried.CanPlace(dropBlockLayer))
                    Drop();
                StopSkill();
                return;
            }
            
            _movement.CanManualMove = false;
            _movement.SetMovementVelocity(Vector3.zero);
            AimingTongue();
        }

        private void AimingTongue()
        {
            IsHolding = true;
            tongueLine.enabled = true;
            _renderer.PlayClip(aimingParam.ParamHash, 0f, 0.01f);
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
                _movement.RotateTo(dir); 
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
            _movement.RotateTo(dir);
            Vector3 tip = startTrm.position;
            Vector3 startPos = startTrm.position;
            float moveDistance = 0f;
            Transform anchor = null;
            Vector3 localHit = Vector3.zero;
            ITonguePullable pullable = null;
            
            _renderer.PlayClip(grepParam.ParamHash, 0f, 0.01f);
            while (moveDistance < maxRange)  //발사해서 닿거나 최대 사거리까지
            {
                float step = shootSpeed * Time.deltaTime;
                if (Physics.SphereCast(tip, tipRadius, dir, out RaycastHit hit, step, hitLayer))
                {
                    anchor = hit.transform;
                    localHit = anchor.InverseTransformPoint(hit.point);
                    damageCaster.ResetHitCaster();
                    damageCaster.CastDamage(hit.point, SkillData, 1f);
                    hit.collider.GetComponentInParent<ITongueHitHandler>()?.HandleTongueHit(hit.point);
                    pullable = hit.collider.GetComponentInParent<ITonguePullable>();
                    break;
                }

                tip += dir * step;
                moveDistance += step;
                UpdateLine(tip, tongueColor);
                yield return null;
            }

            if (pullable != null)
            {
                yield return PullObjectCoroutine(pullable);
                if (pullable is ITongueHoldable)
                {
                    _carried = pullable;
                    _lockOn.SetBlocked(true);
                }
                else 
                    pullable.EndPull();
            }
            else if (anchor != null)
            {
                //맞았을 때 이동
                _renderer.PlayClip(dragParam.ParamHash, 0f, 0.01f);
                _movement.CanUseGravity = false;
                float t = 0f;
                while (anchor != null && t < maxPullTime)
                {
                    tip = anchor.TransformPoint(localHit);
                    Vector3 toTip = tip - _player.transform.position;
                    if (toTip.magnitude <= endPointOffset) break;
            
                    _movement.SetMovementVelocity(toTip.normalized * pullSpeed);
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
            _movement.SetMovementVelocity(Vector3.zero);
            tongueLine.enabled = _carried != null;
            IsHolding = false;
            _movement.CanManualMove = true;
            _movement.CanUseGravity = true;
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
            Vector3 playerPos = _player.transform.position;
            Vector3 dir = _player.PlayerInput.GetMousePositionOnPlane(playerPos.y) - playerPos;
            dir.y = 0f;
            return dir.normalized;
        }

        private IEnumerator PullObjectCoroutine(ITonguePullable pullable)
        {
            pullable.BeginPull();
            Transform target = pullable.Root;
            while (Vector3.Distance(target.position, GetHoldPosition()) > 0.05f)
            {
                target.position = Vector3.MoveTowards(target.position, 
                    GetHoldPosition(), objectPullSpeed * Time.deltaTime);
                UpdateLine(target.position, tongueColor);
                yield return null;
            }
        }

        private void Drop()
        {
            _carried.EndPull();
            _carried = null;
            tongueLine.enabled = false;
            _lockOn.SetBlocked(false);
        }

        private void HandlePlayerHit()
        {
            
            if (_carried != null)
                ForceDrop();
        }

        private void HandleCharacterChangeStart(CharacterChangeStartEvent evt)
        {
            if (evt.Prev == _player && _carried != null)
                ForceDrop();
        }

        private Vector3 GetHoldPosition() => _player.transform.position + _player.transform.forward * holdDistance +
                                             Vector3.up * holdHeight;

        private void ForceDrop()
        {
            Vector3 holdPos = GetHoldPosition();
            Vector3 playerCenter = _player.transform.position + Vector3.up * holdHeight;
            for (int i = 1; i <= FORCE_DROP_STEPS && !_carried.CanPlace(dropBlockLayer); i++)
                _carried.Root.position = Vector3.Lerp(holdPos, playerCenter, (float)i / FORCE_DROP_STEPS);
            Drop();
        }
    }
}
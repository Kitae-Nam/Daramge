using System.Collections;
using CHG._02_Scripts.CombatSystem;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using UnityEngine;

namespace CHG._02_Scripts.Player.Combat
{
    public class TongueGrepSkill : AbstractPlayerSkill, IReleasable
    {
        public bool IsHolding { get; private set; }
        
        [SerializeField] private AnimParamSO grepParam;
        [SerializeField] private AnimParamSO aimingParam;
        [SerializeField] private AnimParamSO dragParam;
        [Space(10)]
        [SerializeField] private LineRenderer tongueLine;
        [SerializeField] private Transform startTrm;
        [SerializeField] private LayerMask hitLayer;
        [SerializeField] private float maxRange = 10f;
        [SerializeField] private float shootSpeed = 10f;
        [SerializeField] private float pullSpeed = 10f;
        [SerializeField] private float tipRadius = 0.3f;
        [SerializeField] private float endPointOffset = 0.5f;
        [SerializeField] private float maxPullTime = 2f;
        [Space(10)]
        [SerializeField] private OverlapDamageCaster damageCaster;
        [SerializeField] private Gradient aimColor;
        [SerializeField] private Gradient tongueColor;
        

        public override void InitializeSkill(ISkillModule skillModule)
        {
            base.InitializeSkill(skillModule);
            Debug.Assert(damageCaster != null, $"DamageCaster is null : {gameObject.name}");
            damageCaster.InitCaster(_player);
        }

        public override bool CanUseSkill(GameObject target = null)
        {
            return NormalizedCooldown >= 1f && !IsUsing;
        }

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);
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
            while (IsHolding)
            {
                Vector3 dir = GetAimDirection();
                Vector3 startPos = startTrm.position;
                _movement.RotateTo(dir); 
                bool isHit = Physics.SphereCast(startPos, tipRadius, dir, out RaycastHit hit, maxRange, hitLayer); 
                UpdateLine(startPos + dir * (isHit ? hit.distance : maxRange), aimColor);

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
                    
                    /*ChessPullableBlock block =
                        hit.collider.GetComponentInParent<ChessPullableBlock>();

                    if (block != null)
                    {
                        block.ReceiveTongueHit(transform.position);
                    }
                    break;*/ //나중에 수정

                    break;
                }

                tip += dir * step;
                moveDistance += step;
                UpdateLine(tip, tongueColor);
                yield return null;
            }
            
            
            if (anchor != null)
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
            tongueLine.enabled = false;
            IsHolding = false;
            _movement.CanManualMove = true;
            _movement.CanUseGravity = true;
            base.StopSkill();
        }
#if UNITY_EDITOR
        
#endif
        private void UpdateLine(Vector3 tip, Gradient color)
        {
            tongueLine.colorGradient = color;
            tongueLine.SetPosition(0, startTrm.position);            
            tongueLine.SetPosition(1, tip);
        }

        private Vector3 GetAimDirection()
        {
            Vector3 dir = _player.PlayerInput.GetWorldMousePosition() - _player.transform.position;
            dir.y = 0f;
            return dir.normalized;
        }
    }
}
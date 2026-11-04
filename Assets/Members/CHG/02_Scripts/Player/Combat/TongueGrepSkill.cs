using System.Collections;
using CHG._02_Scripts.CombatSystem;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using UnityEngine;

namespace CHG._02_Scripts.Player.Combat
{
    public class TongueGrepSkill : AbstractPlayerSkill
    {
        [SerializeField] private AnimParamSO grepParam;
        [SerializeField] private AnimParamSO dragParam;
        [SerializeField] private LineRenderer tongueLine;
        [SerializeField] private Transform startTrm;
        [SerializeField] private LayerMask hitLayer;
        [SerializeField] private float maxRange = 10f;
        [SerializeField] private float shootSpeed = 10f;
        [SerializeField] private float pullSpeed = 10f;
        [SerializeField] private float tipRadius = 0.3f;
        [SerializeField] private float endPointOffset = 0.5f;
        [SerializeField] private float maxPullTime = 2f;
        [SerializeField] private OverlapDamageCaster damageCaster;
        
        private ICombatState _combatState;
        private bool _isWaitingPull;
        private bool _isPullConfirmed;

        public override void InitializeSkill(ISkillModule skillModule)
        {
            base.InitializeSkill(skillModule);
            _combatState = _player.GetModule<ICombatState>();
            Debug.Assert(_combatState != null, $"CombatState is null : {gameObject.name}");
            Debug.Assert(damageCaster != null, $"DamageCaster is null : {gameObject.name}");
            damageCaster.InitCaster(_player);
        }

        private void OnDestroy()
        {
            UnsubscribeWaitInput();
        }

        public override bool CanUseSkill(GameObject target = null)
        {
            return NormalizedCooldown >= 1f && !IsUsing;
        }

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);
            StartCoroutine(GrepCoroutine());
        }

        private IEnumerator GrepCoroutine()
        {
            _movement.CanManualMove = false;
            _movement.CanUseGravity = false;
            _movement.SetMovementVelocity(Vector3.zero);
            tongueLine.enabled = true;

            Vector3 worldMousePos = _player.PlayerInput.GetWorldMousePosition();
            
            Vector3 dir = (worldMousePos - _player.transform.position).normalized;
            _movement.RotateTo(dir);
            dir.y = 0f;
            Vector3 tip = startTrm.position;
            Vector3 startPos = startTrm.position;
            float moveDistance = 0f;
            Transform anchor = null;
            Vector3 localHit = Vector3.zero;
            
            _renderer.PlayClip(grepParam.ParamHash, 0f, 0.01f);
            while (moveDistance < maxRange)
            {
                float step = shootSpeed * Time.deltaTime;
                if (Physics.SphereCast(tip, tipRadius, dir, out RaycastHit hit, step, hitLayer))
                {
                    anchor = hit.transform;
                    localHit = anchor.InverseTransformPoint(hit.point);
                    damageCaster.ResetHitCaster();
                    damageCaster.CastDamage(hit.point, SkillData, 1f);
                    
                    ChessPullableBlock block =
                        hit.collider.GetComponentInParent<ChessPullableBlock>();

                    if (block != null)
                    {
                        block.ReceiveTongueHit(transform.position);
                    }
                    break;
                }

                tip += dir * step;
                moveDistance += step;
                UpdateLine(tip);
                yield return null;
            }

            if (anchor != null && !_combatState.IsCombat)
            {
                _isPullConfirmed = false;
                _isWaitingPull = true;
                SubscribeWaitInput();
                while (_isWaitingPull && anchor != null)
                {
                    tip = anchor.TransformPoint(localHit);
                    UpdateLine(tip);
                    yield return null;
                }
                UnsubscribeWaitInput();
                if (!_isPullConfirmed) anchor = null;
            }
            
            if (anchor != null)
            {
                _renderer.PlayClip(dragParam.ParamHash, 0f, 0.01f);
                float t = 0f;
                while (anchor != null && t < maxPullTime)
                {
                    tip = anchor.TransformPoint(localHit);
                    Vector3 toTip = tip - _player.transform.position;
                    if (toTip.magnitude <= endPointOffset) break;
            
                    _movement.SetMovementVelocity(toTip.normalized * pullSpeed);
                    UpdateLine(tip);
                    t += Time.deltaTime;
            
                    yield return null;
                }
            }
            else
            {
                while (Vector3.Distance(tip, startPos) > 0.1f)
                {
                    tip = Vector3.MoveTowards(tip, startPos, shootSpeed * Time.deltaTime);
                    UpdateLine(tip);
                    yield return null;
                }
            }
            
            StopSkill();
        }

        public override void StopSkill()
        {
            StopAllCoroutines();
            UnsubscribeWaitInput();
            _movement.SetMovementVelocity(Vector3.zero);
            tongueLine.enabled = false;
            _movement.CanManualMove = true;
            _movement.CanUseGravity = true;
            base.StopSkill();
        }

        private void UpdateLine(Vector3 tip)
        {
            tongueLine.SetPosition(0, startTrm.position);            
            tongueLine.SetPosition(1, tip);
        }

        private void SubscribeWaitInput()
        {
            _player.PlayerInput.OnSkillKeyPressed += HandleSkillKeyPressed;
            _player.PlayerInput.OnMovementChange += HandleMovementKeyPressed;
            _player.PlayerInput.OnInteractKeyPressed += HandleInteractKeyPressed;
        }
        
        private void UnsubscribeWaitInput()
        {
            if (_player == null || _player.PlayerInput == null) return;
            _player.PlayerInput.OnSkillKeyPressed -= HandleSkillKeyPressed;
            _player.PlayerInput.OnMovementChange -= HandleMovementKeyPressed;
            _player.PlayerInput.OnInteractKeyPressed -= HandleInteractKeyPressed;
        }

        private void HandleSkillKeyPressed(PlayerSkillSlot slot)
            => EndWaitPull((int)slot == SkillData.skillIndex);
            
        private void HandleMovementKeyPressed(Vector2 movement)
        {
            if (movement.sqrMagnitude > 0f)
                EndWaitPull(false);
        }

        private void HandleInteractKeyPressed()
        {
            EndWaitPull(false);
        }

        private void EndWaitPull(bool isConfirmed)
        {
            if (!_isWaitingPull) return;
            _isWaitingPull = false;
            _isPullConfirmed = isConfirmed;
        }
    }
}
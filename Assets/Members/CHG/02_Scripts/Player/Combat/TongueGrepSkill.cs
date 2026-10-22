using System.Collections;
using Members.CHG._02_Scripts.CoreSystem.AnimationSystem;
using UnityEngine;

namespace Members.CHG._02_Scripts.Player.Combat
{
    public class TongueGrepSkill : AbstractPlayerSkill
    {
        [SerializeField] private AnimParamSO grepParam;
        [SerializeField] private AnimParamSO dragParam;
        [SerializeField] private AnimParamSO dragEndParam;
        [SerializeField] private LineRenderer tongueLine;
        [SerializeField] private Transform startTrm;
        [SerializeField] private LayerMask hitLayer;
        [SerializeField] private float maxRange = 10f;
        [SerializeField] private float shootSpeed = 10f;
        [SerializeField] private float pullSpeed = 10f;
        [SerializeField] private float tipRadius = 0.3f;
        [SerializeField] private float endPointOffset = 0.5f;
        [SerializeField] private float maxPullTime = 2f;
        
        
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
                    break;
                }

                tip += dir * step;
                moveDistance += step;
                UpdateLine(tip);
                yield return null;
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
            
            _movement.SetMovementVelocity(Vector3.zero);
            tongueLine.enabled = false;
            _movement.CanManualMove = true;
            _movement.CanUseGravity = true;
            StopSkill();
        }

        private void UpdateLine(Vector3 tip)
        {
            tongueLine.SetPosition(0, startTrm.position);            
            tongueLine.SetPosition(1, tip);
        }
    }
}
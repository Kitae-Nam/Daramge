using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Movement;
using UnityEngine;

namespace CWH.BossIntegration
{
    /// <summary>
    /// 수신자가 보관한 넉백을 CharacterController의 추가 수평 이동으로 적용한다.
    /// CHG의 입력, 중력, 점프 상태를 덮어쓰지 않고 충돌은 기존 Controller에 맡긴다.
    /// </summary>
    [DefaultExecutionOrder(200), DisallowMultipleComponent, RequireComponent(typeof(KnockbackReceiver))]
    public sealed class PlayerKnockbackMovement : MonoBehaviour
    {
        private CharacterController _controller;
        private KnockbackReceiver _receiver;

        public void Bind(CharacterController controller)
        {
            _controller = controller;
            _receiver = GetComponent<KnockbackReceiver>();
        }

        private void FixedUpdate()
        {
            if (_controller == null || !_controller.enabled || !_controller.gameObject.activeInHierarchy ||
                _receiver == null || !_receiver.isActiveAndEnabled || !_receiver.IsKnockedBack) return;
            _controller.Move(_receiver.KnockBackVelocity * Time.fixedDeltaTime);
        }
    }
}

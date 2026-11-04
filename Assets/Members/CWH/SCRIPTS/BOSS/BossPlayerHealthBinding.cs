using CHG._02_Scripts.Player;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Movement;
using GGMLib.ModuleSystems;
using UnityEngine;

namespace CWH.BossIntegration
{
    /// <summary>
    /// 보스의 현재 타깃에 피해 어댑터를 조립한다. 다른 멤버의 플레이어 프리팹은 수정하지 않는다.
    /// 체력 모듈은 자식에 있어도 어댑터는 캐릭터 루트에 두어 여러 콜라이더가 한 수신자를 공유한다.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(BossTarget))]
    public sealed class BossPlayerHealthBinding : MonoBehaviour
    {
        private BossTarget _target;
        private Transform _boundTarget;
        private PlayerHealthDamageReceiver _receiver;

        private void Awake() => _target = GetComponent<BossTarget>();
        private void Start() => TryBindTarget();

        private void Update()
        {
            if (_boundTarget != _target.Target || _receiver == null || !_receiver.HasHealth)
                TryBindTarget();
        }

        public bool TryBindTarget()
        {
            Transform target = _target.Target;
            if (target == null) return false;

            ModuleOwner owner = target.GetComponentInParent<ModuleOwner>();
            Transform root = owner != null ? owner.transform : target;
            PlayerHealthModule health = root.GetComponentInChildren<PlayerHealthModule>(true);
            if (health == null) health = target.GetComponentInParent<PlayerHealthModule>();
            if (health == null) return false;

            _receiver = root.GetComponent<PlayerHealthDamageReceiver>();
            if (_receiver == null) _receiver = root.gameObject.AddComponent<PlayerHealthDamageReceiver>();
            _receiver.Bind(health);
            _boundTarget = target;
            return true;
        }
    }
}

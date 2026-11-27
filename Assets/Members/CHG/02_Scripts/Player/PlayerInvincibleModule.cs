using System; 
using System.Collections; 
using CHG._02_Scripts.Agent; 
using GGMLib.ModuleSystems; 
using UnityEngine; 

namespace CHG._02_Scripts.Player 
{
    public class PlayerInvincibleModule : MonoBehaviour, IModule 
    {
        [SerializeField] private float invincibleDuration = 1f;

        private PlayerController _player;
        private Coroutine _invincibleCoroutine;

        public event Action OnInvincibleStarted;
        public event Action OnInvincibleEnded;

        public void Initialize(ModuleOwner owner)
        {
            _player = owner as PlayerController;
            Debug.Assert(_player != null, $"owner is not PlayerController : {gameObject.name}");
            _player.OnHit.AddListener(HandleHit);
        }

        private void OnDestroy()
        {
            if (_player != null)
                _player.OnHit.RemoveListener(HandleHit);
        }

        private void OnDisable() => StopInvincible();

        private void HandleHit()
        {
            if (_player.IsDead || _invincibleCoroutine != null) return;
            _invincibleCoroutine = StartCoroutine(InvincibleCoroutine());
        }

        private IEnumerator InvincibleCoroutine()
        {
            _player.AddStatus(AgentStatus.Invincible);
            OnInvincibleStarted?.Invoke();
            yield return new WaitForSeconds(invincibleDuration);
            _invincibleCoroutine = null;
            EndInvincible();
        }

        private void StopInvincible()
        {
            if (_invincibleCoroutine == null) return;
            StopCoroutine(_invincibleCoroutine);
            _invincibleCoroutine = null;
            EndInvincible();
        }

        private void EndInvincible()
        {
            _player.RemoveStatus(AgentStatus.Invincible);
            OnInvincibleEnded?.Invoke();
        }
    }
}
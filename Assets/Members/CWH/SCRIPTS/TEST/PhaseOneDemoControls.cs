using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Memebers.CWH.SCRIPTS.TEST
{
    /// <summary>검증 씬 전용 입력/HUD. 실제 플레이어 공격이 생기면 이 컴포넌트만 교체한다.</summary>
    public sealed class PhaseOneDemoControls : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private BossHealth _boss;
        [SerializeField] private PhaseOneBrain _brain;
        [SerializeField] private float _testDamage = 40f;
        private TestDamageReceiver _playerHealth;
        private float _nextAttack;
        private bool _deathHandled;
        private void Awake() => _playerHealth = GetComponent<TestDamageReceiver>();
        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                // 에디터에서 Build Settings에 등록하지 않고 연 씬도 다시 시작할 수 있게 한다.
#if UNITY_EDITOR
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(SceneManager.GetActiveScene().path,
                    new LoadSceneParameters(LoadSceneMode.Single));
#else
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
#endif
                return;
            }
            if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame && _brain != null)
                _brain.enabled = !_brain.enabled;
            if (_playerHealth != null && _playerHealth.IsDead)
            {
                if (!_deathHandled)
                {
                    _deathHandled = true;
                    _brain.enabled = false;
                    _boss.GetComponent<BossPatternRunner>().CancelCurrentPattern();
                    _boss.GetComponent<BossMovementStateController>().SetMovementAllowed(false);
                    _boss.GetComponent<BossSummonRoster>().Clear();
                    _boss.GetComponent<BossSwordController>().Clear();
                    GetComponent<TestCameraController>().enabled = false;
                    GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
                }
                return;
            }
            if (Time.time < _nextAttack) return;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && _camera != null)
            {
                Ray ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit, 200f, ~0, QueryTriggerInteraction.Collide))
                {
                    IDamageReceiver receiver = hit.collider.GetComponentInParent<IDamageReceiver>();
                    if (receiver != null && !hit.transform.IsChildOf(transform)) receiver.ReceiveDamage(_testDamage);
                }
                _nextAttack = Time.time + 0.35f;
            }
            else if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                BossAttackQuery.ApplySector(transform, transform.position - Vector3.up, transform.forward,
                    6f, 360f, 7f, _testDamage, ~0);
                _nextAttack = Time.time + 0.35f;
            }
        }
        private void OnGUI()
        {
            if (_boss == null) return;
            var style = new GUIStyle(GUI.skin.box) { fontSize = 18, alignment = TextAnchor.UpperLeft };
            int phase = _boss.GetComponent<BossPhaseController>().CurrentPhase;
            string state = _deathHandled ? "PLAYER DEFEATED - press R" : phase == 2 ? "PHASE 1 CLEAR (phase 2 pending)" : _boss.GetComponent<BossPatternRunner>().State.ToString();
            GUI.Box(new Rect(15, 15, 590, 150),
                $"BOSS {_boss.CurrentHealth:0}/{_boss.MaxHealth:0}  |  PLAYER {_playerHealth.CurrentHealth:0}\n" +
                $"{state}   Minions: {_boss.GetComponent<BossSummonRoster>().LivingCount}\n" +
                "WASD: move | F: nearby test attack | Click: ranged test damage\n" +
                "P: toggle boss AI | R: restart\nBreak minions to remove protection. Break the embedded sword!", style);
        }
    }
}

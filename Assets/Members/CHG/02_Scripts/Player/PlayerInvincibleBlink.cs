using System.Collections; 
using GGMLib.ModuleSystems; 
using UnityEngine; 

namespace CHG._02_Scripts.Player 
{
    public class PlayerInvincibleBlink : MonoBehaviour, IModule 
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Renderer[] targetRenderers;
        [SerializeField, ColorUsage(false, true)] private Color glowColor = new(0.8f, 0.8f, 0.8f);
        [SerializeField] private float blinkInterval = 0.15f;

        private PlayerInvincibleModule _invincible;
        private MaterialPropertyBlock _propertyBlock;
        private Coroutine _blinkCoroutine;

        public void Initialize(ModuleOwner owner)
        {
            _invincible = owner.GetModule<PlayerInvincibleModule>();
            Debug.Assert(_invincible != null, $"PlayerInvincibleModule is null : {gameObject.name}");
            _propertyBlock = new MaterialPropertyBlock();
            _invincible.OnInvincibleStarted += HandleInvincibleStarted;
            _invincible.OnInvincibleEnded += HandleInvincibleEnded;
        }

        private void OnDestroy()
        {
            if (_invincible == null) return;
            _invincible.OnInvincibleStarted -= HandleInvincibleStarted;
            _invincible.OnInvincibleEnded -= HandleInvincibleEnded;
        }

        private void HandleInvincibleStarted()
        {
            StopBlink();
            _blinkCoroutine = StartCoroutine(BlinkCoroutine());
        }

        private void HandleInvincibleEnded() => StopBlink();

        private IEnumerator BlinkCoroutine()
        {
            float startTime = Time.time;
            while (true)
            {
                float t = Mathf.PingPong((Time.time - startTime) / blinkInterval, 1f);
                SetEmission(glowColor * t);
                yield return null;
            }
        }

        private void StopBlink()
        {
            if (_blinkCoroutine != null)
            {
                StopCoroutine(_blinkCoroutine);
                _blinkCoroutine = null;
            }
            ClearEmission();
        }

        private void SetEmission(Color color)
        {
            foreach (Renderer targetRenderer in targetRenderers)
            {
                targetRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(EmissionColorId, color);
                targetRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        private void ClearEmission()
        {
            _propertyBlock.Clear();
            foreach (Renderer targetRenderer in targetRenderers)
                targetRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
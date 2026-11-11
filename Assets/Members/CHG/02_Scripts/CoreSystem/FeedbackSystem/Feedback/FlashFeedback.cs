using System.Collections;
using UnityEngine;

namespace CHG._02_Scripts.CoreSystem.FeedbackSystem.Feedback
{
    public class FlashFeedback : MonoBehaviour, IFeedback
    {
        private const string EMISSION = "_EMISSION";
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Renderer[] targetRenderers;
        [SerializeField, ColorUsage(false, true)] private Color flashColor;
        [SerializeField] private float flashDuration = 0.08f;

        private MaterialPropertyBlock _propertyBlock;
        private Coroutine _flashCoroutine;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            WarnIfEmissionDisabled();
        }

        private void OnDisable() => StopFlash();

        public void PlayFeedback(FeedbackContext context)
        {
            StopFlash();
            _flashCoroutine = StartCoroutine(FlashCoroutine());
        }

        private IEnumerator FlashCoroutine()
        {
            SetFlashColor();
            yield return new WaitForSecondsRealtime(flashDuration);
            ClearFlashColor();
            _flashCoroutine = null;
        }

        private void StopFlash()
        {
            if (_flashCoroutine == null) return;
            StopCoroutine(_flashCoroutine);
            _flashCoroutine = null;
            ClearFlashColor();
        }
        
        private void SetFlashColor()
        {
            foreach (Renderer targetRenderer in targetRenderers)
            {
                targetRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(EmissionColorId, flashColor);
                targetRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        private void ClearFlashColor()
        {
            _propertyBlock.Clear();
            foreach (Renderer targetRenderer in targetRenderers)
                targetRenderer.SetPropertyBlock(_propertyBlock);
        }
        
        private void WarnIfEmissionDisabled()
        {
            foreach (Renderer targetRenderer in targetRenderers)
            {
                foreach (Material material in targetRenderer.sharedMaterials)
                {
                    if (material != null &&
                        !material.IsKeywordEnabled(EMISSION)) Debug.LogWarning($"Emission is disabled :{gameObject.name}, {material.name} ");
                }
            }
        }
    }
}
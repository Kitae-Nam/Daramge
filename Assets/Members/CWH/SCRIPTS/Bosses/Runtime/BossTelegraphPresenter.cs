using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>
    /// 텔레그래프의 진행도와 투명도를 조절하는 스크립트입니다.
    /// </summary>
    public class BossTelegraphPresenter : MonoBehaviour
    {
        [SerializeField] private Renderer _targetRenderer;
        
        private MaterialPropertyBlock propertyBlock;

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
            if (_targetRenderer == null) _targetRenderer = GetComponent<Renderer>();
            SetOpacity(0f);
        }

        /// <summary>소환체처럼 런타임에 생성한 표시도 같은 셰이더 제어 코드를 사용한다.</summary>
        public void ConfigureRenderer(Renderer targetRenderer)
        {
            _targetRenderer = targetRenderer;
            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
            SetOpacity(0f);
        }

        public void SetProgress(float progress)
        {
            if (_targetRenderer == null || propertyBlock == null) return;
            
            // 이미 적용된 다른 프로퍼티 값을 유지한다.
            _targetRenderer.GetPropertyBlock(propertyBlock);
            // 진행도를 0~1 범위로 제한해 기록한다.
            propertyBlock.SetFloat("_Progress", Mathf.Clamp01(progress));
            // 기록된 값을 실제 렌더러에 적용한다.
            _targetRenderer.SetPropertyBlock(propertyBlock);
        }

        public void SetOpacity(float opacity)
        {
            if (_targetRenderer == null || propertyBlock == null) return;
            
            _targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat("_Opacity", Mathf.Clamp01(opacity));
            _targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}

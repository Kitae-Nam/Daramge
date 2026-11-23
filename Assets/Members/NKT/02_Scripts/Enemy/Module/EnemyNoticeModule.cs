using System;
using System.Collections;
using GGMLib.ModuleSystems;
using UnityEngine;

namespace NKT.Enemy.Module
{
    public class EnemyNoticeModule : MonoBehaviour, IModule
    {
        [SerializeField] private Transform mark;
        [SerializeField] private float popTime = 0.5f;
        [SerializeField] private float holdTime = 0.5f;
        [SerializeField] private float shrinkTime = 0.5f;
        [SerializeField] private AnimationCurve popCurve;

        private Vector3 _baseScale;
        private Coroutine _routine;
        
        public void Initialize(ModuleOwner owner)
        {
            _baseScale = mark.localScale;
            HideMark();
        }

        public void Noticed()
        {
            if (_routine != null) return;
            _routine = StartCoroutine(NoticeRoutine());
        }

        private IEnumerator NoticeRoutine()
        {
            mark.gameObject.SetActive(true);
            yield return ScaleRoutine(popTime, t => popCurve.Evaluate(t));
            yield return new WaitForSeconds(holdTime);
            yield return ScaleRoutine(shrinkTime, t => 1f - t);
            HideMark();
        }

        private IEnumerator ScaleRoutine(float duration, Func<float, float> scaleAt)
        {
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                mark.localScale = _baseScale * scaleAt(t / duration);
                yield return null;
            }
            mark.localScale = _baseScale * scaleAt(1f);
        }

        private void HideMark()
        {
            _routine = null;
            mark.localScale = _baseScale;
            mark.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if(mark != null)
                mark.gameObject.SetActive(false);
        }
    }
}
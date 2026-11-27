using System.Collections.Generic;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>쓸기 진행에 맞춰 바닥 자국과 긁는 소리를 재생한다.</summary>
    [DisallowMultipleComponent]
    public sealed class ChessSweepPresentation : MonoBehaviour
    {
        [SerializeField] private Material _scratchMaterial;
        [SerializeField] private Color _scratchColor = new Color(.16f, .085f, .055f, .92f);
        [SerializeField, Min(.1f)] private float _scratchLifetime = 2.4f;
        [SerializeField] private AudioClip _scrapeSound;
        private ChessSweepGroundMark _mark;
        private bool _running;

        public void BeginGrid(IReadOnlyList<Vector3> cells, float cellSize, Vector3 forward, float surfaceHeight)
        {
            End();
            if (cells.Count == 0) return;
            _running = true;
            Vector3 center = Vector3.zero;
            foreach (Vector3 cell in cells) center += cell;
            center /= cells.Count;
            // 자국과 긁는 소리의 기준점은 NavMesh가 아닌 보드 모델의 윗면이다.
            center.y = surfaceHeight;
            if (_scratchMaterial != null)
            {
                var root = new GameObject("Colossus sweep ground mark");
                root.transform.SetPositionAndRotation(center + Vector3.up * .025f, Quaternion.LookRotation(forward));
                _mark = root.AddComponent<ChessSweepGroundMark>();
                _mark.Initialize(_scratchMaterial, cellSize * cells.Count, cellSize, _scratchColor, _scratchLifetime);
            }
            if (_scrapeSound != null) AudioSource.PlayClipAtPoint(_scrapeSound, center, .62f);
        }

        public void Advance(float progress)
        {
            if (_mark != null) _mark.Reveal(progress);
        }

        public void End()
        {
            if (!_running) return;
            _running = false;
            if (_mark != null) _mark.Release();
            _mark = null;
        }

        private void OnDisable() => End();
    }
}

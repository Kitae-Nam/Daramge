using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>한 번의 팔 동작과 실제 이동 거리를 같은 당김 구간에 맞춘다.</summary>
    public static class ChessColossusDragGait
    {
        public const float PlantPhase = 0.44f;
        public const float ReleasePhase = 0.9f;
        public const float MaxStride = 1.15f;
        public const float MinCycleDuration = 0.76f;

        public static float PullProgress(float phase) =>
            Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(PlantPhase, ReleasePhase, phase));

        public static float LegProgress(float elapsedFraction, int pulls, out float phase)
        {
            float cycle = Mathf.Clamp01(elapsedFraction) * pulls;
            if (cycle >= pulls)
            {
                phase = 1f;
                return 1f;
            }
            int completed = Mathf.FloorToInt(cycle);
            phase = cycle - completed;
            return (completed + PullProgress(phase)) / pulls;
        }
    }
}

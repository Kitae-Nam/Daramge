using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Weapons
{
    public interface ISwordOrbitPoseProvider
    {
        bool TryGetOrbitPose(
            BossSword sword,
            out Vector3 position,
            out Quaternion rotation);
    }
}
using System.Collections.Generic;

namespace CHG._05.Graphic.Asset.DynamicMeshCutter.Scripts.Utility
{
    public static class MeshTargetShephard
    {
        public static List<MeshTarget> Targets = new List<MeshTarget>();
        public static void RegisterMeshTarget(MeshTarget target)
        {
            if (!Targets.Contains(target))
                Targets.Add(target);
        }

        public static void UnRegisterMeshTarget(MeshTarget target)
        {
            if (Targets.Contains(target))
                Targets.Remove(target);
        }
    }
}

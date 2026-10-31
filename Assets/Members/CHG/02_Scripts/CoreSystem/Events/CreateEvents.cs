using GGMLib.EventSystem;
using GGMLib.ObjectPool.Runtime;
using UnityEngine;

namespace CHG._02_Scripts.CoreSystem.Events
{
    public static class CreateEvents
    {
        public static readonly ShowPoolingEffect ShowPoolingEffect = new();
        public static readonly ShowDisintegrateEffect ShowDisintegrate = new();
    }

    public class ShowPoolingEffect : GameEvent
    {
        public PoolItemSO ItemData;
        public Vector3 Position;
        public Quaternion Rotation;

        public ShowPoolingEffect InitData(PoolItemSO itemData, Vector3 position, Quaternion rotation)
        {
            ItemData = itemData;
            Position = position;
            Rotation = rotation;
            return this;
        }
    }

    public class ShowDisintegrateEffect : GameEvent
    {
        public PoolItemSO ItemData;
        public Vector3 Position;
        public Quaternion Rotation;
        public SkinnedMeshRenderer MeshRenderer;
        public Transform VisualTrm;

        public ShowDisintegrateEffect InitData(PoolItemSO itemData, Vector3 position, Quaternion rotation,
            SkinnedMeshRenderer meshRenderer, Transform visualTrm)
        {
            ItemData = itemData;
            Position = position;
            Rotation = rotation;
            MeshRenderer = meshRenderer;
            VisualTrm = visualTrm;
            return this;
        }
    }
}
using UnityEngine;

namespace GGMLib.DatabaseSystem.Runtime
{
    public abstract class IndexedAsset : ScriptableObject
    {
        [field: SerializeField] public int AssetIndex { get; set; }
        [field: SerializeField] public string AssetName { get; set; }
    }
}
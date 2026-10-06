using System.Linq;
using UnityEngine;

namespace GGMLib.DatabaseSystem.Runtime
{
    [CreateAssetMenu(fileName = "Data table", menuName = "Lib/DB/Table", order = 0)]
    public class DataTableSO : ScriptableObject
    {
        public string tableName;
        public IndexedAsset[] assets;
        
        public bool HasAsset(int index) 
            => assets.Any(asset => asset.AssetIndex == index);
        
        public IndexedAsset GetAsset(int index) 
            => assets.FirstOrDefault(asset => asset.AssetIndex == index);
    }
}
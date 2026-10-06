using UnityEngine;

namespace GGMLib.ObjectPool.Runtime
{
    public interface IPoolable
    {
        PoolItemSO PoolItem { get; set; }
        GameObject GameObject { get;}
        void ResetItem();
    }
}
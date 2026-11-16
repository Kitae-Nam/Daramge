using UnityEngine;

namespace NKT.Enemy
{
    public class MimicEnemy : AbstractEnemy
    {
        [SerializeField] private GameObject visualRoot;
        
        public override void ResetItem()
        {
            visualRoot.SetActive(true);
            base.ResetItem();
        }
    }
}
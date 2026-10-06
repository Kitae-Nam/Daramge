using UnityEngine;

namespace GGMLib.CoreLibrary
{
    public abstract class AbstractFeedback : MonoBehaviour
    {
        public abstract void CreateFeedback();
        public virtual void StopFeedback() { }
    }
}
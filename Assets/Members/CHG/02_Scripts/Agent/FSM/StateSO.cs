using Members.CHG._02_Scripts.CoreSystem.AnimationSystem;
using UnityEngine;

namespace Members.CHG._02_Scripts.FSM
{
    [CreateAssetMenu(fileName = "State data", menuName = "Agent/StateData", order = 10)]
    public class StateSO : ScriptableObject
    {
        public string stateName;
        public string className;
        public int assetIndex;
        public AnimParamSO stateParam;
    }
}
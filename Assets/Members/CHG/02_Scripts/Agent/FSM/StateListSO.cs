using UnityEngine;

namespace CHG._02_Scripts.Agent.FSM
{
    [CreateAssetMenu(fileName = "State list data", menuName = "Agent/State list", order = 21)]
    public class StateListSO : ScriptableObject
    {
        [HideInInspector] public string generatePath;
        public string enumName;
        public StateSO[] states;
    }
}
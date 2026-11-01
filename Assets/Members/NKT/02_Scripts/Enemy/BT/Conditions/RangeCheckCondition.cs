using System;
using Unity.Behavior;
using UnityEngine;

namespace NKT.Enemy.BT.Conditions
{
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [Condition(name: "RangeCheck", story: "[Enemy] to [Target] distance [Operator] [Field]", category: "Conditions", id: "2395ee168070a01b4491812826f650d2")]
    public partial class RangeCheckCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [SerializeReference] public BlackboardVariable<GameObject> Target;
        [Comparison(comparisonType: ComparisonType.All)]
        [SerializeReference] public BlackboardVariable<ConditionOperator> Operator;

        [SerializeReference] public BlackboardVariable<RangeField> Field;

        public override bool IsTrue()
        {
            if (Enemy.Value is not IHasRangeData rangeEnemy)
                return false;

            if (Target.Value == null) return false;
        
            Vector3 diff = Target.Value.transform.position - Enemy.Value.transform.position;
            diff.y = 0; //다른 거리 조건들과 같은 기준(높이 무시). 비행 적은 높이만큼 멀게 계산되는 것을 막는다.
            float distance = diff.magnitude;
            float threshold = rangeEnemy.RangeData.GetFieldValue(Field.Value);
        
            return Operator.Value switch
            {
                ConditionOperator.Equal => Mathf.Approximately(distance, threshold),
                ConditionOperator.NotEqual => !Mathf.Approximately(distance, threshold),
                ConditionOperator.Greater => distance > threshold,
                ConditionOperator.Lower => distance < threshold,
                ConditionOperator.GreaterOrEqual => distance >= threshold,
                ConditionOperator.LowerOrEqual => distance <= threshold,
                _ => false
            };
        }
    }
}

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
        
            float distance = Vector3.Distance(Enemy.Value.transform.position, Target.Value.transform.position);
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

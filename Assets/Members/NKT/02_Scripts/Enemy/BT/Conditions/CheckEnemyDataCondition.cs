using System;
using Unity.Behavior;
using UnityEngine;

namespace NKT.Enemy.BT.Conditions
{
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [Condition(name: "CheckEnemyData", story: "[Enemy] to [Target] distance [Operator] [Field]", category: "Conditions", id: "9b4f2c6e1d8a4735a0c6e2b7f913d58c")]
    public partial class CheckEnemyDataCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [SerializeReference] public BlackboardVariable<GameObject> Target;
        [Comparison(comparisonType: ComparisonType.All)]
        [SerializeReference] public BlackboardVariable<ConditionOperator> Operator;
        [SerializeReference] public BlackboardVariable<EnemyDataField> Field;

        public override bool IsTrue()
        {
            if (Enemy.Value == null || Target.Value == null)
                return false;

            Vector3 diff = Target.Value.transform.position - Enemy.Value.transform.position;
            diff.y = 0; //ChaseToTarget과 같은 기준(높이 무시)으로 비교
            float distance = diff.magnitude;
            float threshold = Enemy.Value.EnemyData.GetFieldValue(Field.Value);

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

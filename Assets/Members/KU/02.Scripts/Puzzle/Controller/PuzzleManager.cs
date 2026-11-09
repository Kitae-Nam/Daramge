
using System;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleManager : MonoBehaviour
{
    // 문과 발판을 연결하는 데이터
    [Serializable]
    public class DoorRule
    {
        [Header("Target Door")]
        public PuzzleDoorView targetDoor;

        [Header("Required Pressure Plates")]
        public List<PressurePlateController> requiredPlates
            = new List<PressurePlateController>();

        [Header("Activation Condition")]
        public ActivationType activationType =
            ActivationType.All;
    }

    // 문이 열리는 조건
    public enum ActivationType
    {
        All, // 모든 발판이 활성화
        Any  // 하나 이상의 발판이 활성화
    }

    [Header("Puzzle Door Rules")]
    [SerializeField]
    private List<DoorRule> doorRules =
        new List<DoorRule>();

    private readonly HashSet<PressurePlateController>
        subscribedPlates =
            new HashSet<PressurePlateController>();

    private void Start()
    {
        // 모든 발판 상태 확인
        UpdateAllDoors();
    }

    private void OnEnable()
    {
        SubscribePlateEvents();
    }

    private void OnDisable()
    {
        UnsubscribePlateEvents();
    }

    // --------------------------------
    // Event Subscription
    // --------------------------------

    private void SubscribePlateEvents()
    {
        foreach (DoorRule rule in doorRules)
        {
            if (rule == null)
                continue;

            foreach (PressurePlateController plate
                in rule.requiredPlates)
            {
                if (plate == null)
                    continue;

                // 같은 발판을 여러 문에 연결해도
                // 이벤트는 한 번만 등록한다.
                if (subscribedPlates.Add(plate))
                {
                    plate.OnPlateStateChanged +=
                        OnPlateChanged;
                }
            }
        }
    }

    private void UnsubscribePlateEvents()
    {
        foreach (PressurePlateController plate
            in subscribedPlates)
        {
            if (plate != null)
            {
                plate.OnPlateStateChanged -=
                    OnPlateChanged;
            }
        }

        subscribedPlates.Clear();
    }

    // --------------------------------
    // Plate Event
    // --------------------------------

    private void OnPlateChanged(bool activated)
    {
        UpdateAllDoors();
    }

    // --------------------------------
    // Door Management
    // --------------------------------

    private void UpdateAllDoors()
    {
        foreach (DoorRule rule in doorRules)
        {
            if (rule == null || rule.targetDoor == null)
                continue;

            bool shouldOpen =
                CheckDoorCondition(rule);

            rule.targetDoor.SetOpen(shouldOpen);
        }
    }

    // --------------------------------
    // Condition Check
    // --------------------------------

    private bool CheckDoorCondition(DoorRule rule)
    {
        if (rule.requiredPlates == null ||
            rule.requiredPlates.Count == 0)
        {
            return false;
        }

        switch (rule.activationType)
        {
            case ActivationType.All:
                return CheckAllPlates(rule);

            case ActivationType.Any:
                return CheckAnyPlate(rule);
        }

        return false;
    }

    // 모든 발판이 활성화되어야 함
    private bool CheckAllPlates(DoorRule rule)
    {
        foreach (PressurePlateController plate
            in rule.requiredPlates)
        {
            if (plate == null || !plate.IsActivated)
                return false;
        }

        return true;
    }

    // 하나 이상의 발판만 활성화되면 됨
    private bool CheckAnyPlate(DoorRule rule)
    {
        foreach (PressurePlateController plate
            in rule.requiredPlates)
        {
            if (plate != null && plate.IsActivated)
                return true;
        }

        return false;
    }
}

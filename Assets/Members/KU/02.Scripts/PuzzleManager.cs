
using UnityEngine;

public class PuzzleManager : MonoBehaviour
{
    [Header("Pressure Plates")]
    [SerializeField]
    private PressurePlateController redPlate;

    [SerializeField]
    private PressurePlateController bluePlate;

    [Header("Doors")]
    [SerializeField]
    private PuzzleDoorView middleDoor;

    [SerializeField]
    private PuzzleDoorView finalDoor;

    private void OnEnable()
    {
        if (redPlate != null)
            redPlate.OnPlateStateChanged += OnPlateChanged;

        if (bluePlate != null)
            bluePlate.OnPlateStateChanged += OnPlateChanged;

        UpdateDoors();
    }

    private void Start()
    {
        UpdateDoors();
    }

    private void OnDisable()
    {
        if (redPlate != null)
            redPlate.OnPlateStateChanged -= OnPlateChanged;

        if (bluePlate != null)
            bluePlate.OnPlateStateChanged -= OnPlateChanged;
    }

    private void OnPlateChanged(bool activated)
    {
        UpdateDoors();
    }

    private void UpdateDoors()
    {
        bool redActive =
            redPlate != null && redPlate.IsActivated;

        bool blueActive =
            bluePlate != null && bluePlate.IsActivated;

        if (middleDoor != null)
            middleDoor.SetOpen(redActive);

        if (finalDoor != null)
            finalDoor.SetOpen(redActive && blueActive);
    }
}

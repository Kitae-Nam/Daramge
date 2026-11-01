
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class PuzzleCameraZone : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField]
    private PuzzleCameraController cameraController;

    [SerializeField]
    private Transform cameraPoint;

    private readonly HashSet<Collider> playerColliders =
        new HashSet<Collider>();

    public Transform CameraPoint => cameraPoint;

    private void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (cameraController == null)
            return;

        if (!cameraController.IsPlayerCollider(other))
            return;

        playerColliders.Add(other);

        // 플레이어가 처음 진입했을 때만 처리
        if (playerColliders.Count == 1)
        {
            cameraController.EnterPuzzleZone(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (cameraController == null)
            return;

        if (!playerColliders.Remove(other))
            return;

        // 플레이어의 모든 Collider가 나갔을 때 처리
        if (playerColliders.Count == 0)
        {
            cameraController.ExitPuzzleZone(this);
        }
    }

    private void OnDisable()
    {
        playerColliders.Clear();

        if (cameraController != null)
        {
            cameraController.ExitPuzzleZone(this);
        }
    }
}

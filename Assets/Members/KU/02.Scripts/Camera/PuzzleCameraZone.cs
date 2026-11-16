
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(BoxCollider))]
public class PuzzleCameraZone : MonoBehaviour
{
    private const float DefaultOrthographicSize = 7f;

    [Header("References")]
    [SerializeField]
    private PuzzleCameraController cameraController;

    [SerializeField]
    private Transform cameraPoint;

    [Header("Camera Lens")]
    [FormerlySerializedAs("orthographicSizeOverride")]
    [SerializeField, Min(0.01f)]
    private float orthographicSize = DefaultOrthographicSize;

    [SerializeField]
    private float nearClipPlane;

    private readonly HashSet<Collider> playerColliders = new();

    public Transform CameraPoint => cameraPoint;
    public float OrthographicSize => orthographicSize > 0f ? orthographicSize : DefaultOrthographicSize;
    public float NearClipPlane => nearClipPlane;

    private void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        CheckPlayer(other);
    }

    private void OnTriggerStay(Collider other)
    {
        CheckPlayer(other);
    }

    private void CheckPlayer(Collider other)
    {
        if (cameraController == null)
            return;

        if (!cameraController.IsPlayerCollider(other))
            return;

        playerColliders.Add(other);
        cameraController.EnterPuzzleZone(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!playerColliders.Remove(other))
            return;

        if (playerColliders.Count == 0 &&
            cameraController != null)
        {
            cameraController.ExitPuzzleZone(this);
        }
    }

    // 캐릭터 전환 시 이전 플레이어 감지 정보 제거
    public void ForgetTrackedPlayer()
    {
        playerColliders.Clear();
    }

    private void OnDisable()
    {
        playerColliders.Clear();

        if (cameraController != null)
            cameraController.ExitPuzzleZone(this);
    }
}

    using Unity.Cinemachine;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineCamera))]
[DefaultExecutionOrder(-100)]
public sealed class InteractionCameraView : MonoBehaviour
{
    [SerializeField, Tooltip("Optional world pose. If empty, use this object's Transform.")]
    private Transform cameraPoint;

    [SerializeField]
    private int activePriority = 200;

    [SerializeField, Tooltip("Optional target whose world-space height changes the view follows.")]
    private Transform verticalFollowTarget;

    [SerializeField, Min(0.01f)]
    private float verticalFollowSmoothTime = 0.35f;

    private CinemachineCamera viewCamera;
    private float startCameraY;
    private float startTargetY;
    private float verticalOffset;
    private float verticalVelocity;

    private void Awake()
    {
        viewCamera = GetComponent<CinemachineCamera>();
        HideView();
    }

    public void ShowView()
    {
        if (!isActiveAndEnabled)
            return;

        if (cameraPoint != null)
            transform.SetPositionAndRotation(cameraPoint.position, cameraPoint.rotation);
        else if (verticalOffset != 0f)
            transform.position -= Vector3.up * verticalOffset;

        verticalOffset = 0f;
        verticalVelocity = 0f;
        if (verticalFollowTarget != null)
        {
            startCameraY = transform.position.y;
            startTargetY = verticalFollowTarget.position.y;
        }

        ResumeView();
    }

    public void ResumeView()
    {
        if (!isActiveAndEnabled)
            return;

        viewCamera.Priority = activePriority;
        viewCamera.enabled = true;
    }

    private void LateUpdate()
    {
        if (!viewCamera.isActiveAndEnabled || verticalFollowTarget == null)
            return;

        float targetOffset = verticalFollowTarget.position.y - startTargetY;
        verticalOffset = Mathf.SmoothDamp(
            verticalOffset, targetOffset, ref verticalVelocity, verticalFollowSmoothTime);

        Vector3 position = transform.position;
        position.y = startCameraY + verticalOffset;
        transform.position = position;
    }

    public void HideView()
    {
        if (viewCamera != null)
            viewCamera.enabled = false;
    }

    private void OnDisable()
    {
        HideView();
    }
}

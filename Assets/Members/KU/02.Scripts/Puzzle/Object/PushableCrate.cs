
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public class PushableCrate : MonoBehaviour
{
    [Header("Puzzle")]
    [SerializeField] private PuzzleColor crateColor;

    private Rigidbody rb;
    private BoxCollider boxCollider;

    private Collider[] allColliders;
    private bool[] originalColliderStates;

    private bool isHeld;
    private bool isTransitioning;

    public PuzzleColor CrateColor => crateColor;
    public bool IsHeld => isHeld;
    public bool IsTransitioning => isTransitioning;

    public Vector3 HalfExtents
    {
        get
        {
            Vector3 size = Vector3.Scale(
                boxCollider.size,
                transform.lossyScale
            );

            return new Vector3(
                Mathf.Abs(size.x),
                Mathf.Abs(size.y),
                Mathf.Abs(size.z)
            ) * 0.5f;
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        boxCollider = GetComponent<BoxCollider>();

        allColliders = GetComponentsInChildren<Collider>(true);
        originalColliderStates = new bool[allColliders.Length];

        for (int i = 0; i < allColliders.Length; i++)
        {
            originalColliderStates[i] = allColliders[i].enabled;
        }

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.detectCollisions = true;
    }

    public void PickUp()
    {
        if (isHeld || isTransitioning)
            return;

        isHeld = true;
        isTransitioning = true;

        rb.isKinematic = true;
        rb.useGravity = false;

        // 운반 중 Rigidbody의 물리 충돌 비활성화
        rb.detectCollisions = false;

        // 자식 오브젝트를 포함한 모든 Collider 비활성화
        foreach (Collider col in allColliders)
        {
            if (col != null)
                col.enabled = false;
        }
    }

    public void FinishPickUp()
    {
        isTransitioning = false;
    }

    public void BeginDrop()
    {
        if (!isHeld || isTransitioning)
            return;

        isTransitioning = true;
    }

    public void FinishDrop(Vector3 dropPosition)
    {
        transform.position = dropPosition;

        isHeld = false;
        isTransitioning = false;

        // 원래 활성화되어 있던 Collider만 복구
        for (int i = 0; i < allColliders.Length; i++)
        {
            if (allColliders[i] != null)
            {
                allColliders[i].enabled =
                    originalColliderStates[i];
            }
        }

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.detectCollisions = true;

        Physics.SyncTransforms();
    }

    public void SetCarryPosition(Vector3 position)
    {
        if (!isHeld)
            return;

        transform.position = position;
    }
}

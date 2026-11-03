
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public class FrogTongueInteractor : MonoBehaviour
{
    [Header("Test Input")]
    [SerializeField]
    private bool useTestInput = true;

    [SerializeField]
    private PlayerInput playerInput;

    [SerializeField]
    private string tongueActionName = "FirstSkill";

    [Header("References")]
    [SerializeField]
    private Camera aimCamera;

    [SerializeField]
    private Transform tongueOrigin;

    [Header("Tongue Settings")]
    [SerializeField, Min(0.1f)]
    private float tongueRange = 15f;

    [SerializeField]
    private LayerMask tongueHitLayers = ~0;

    [SerializeField, Min(0.01f)]
    private float tongueVisualDuration = 0.15f;

    [SerializeField, Min(0.001f)]
    private float tongueWidth = 0.06f;

    private InputAction tongueAction;
    private LineRenderer lineRenderer;
    private Coroutine visualCoroutine;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();

        if (aimCamera == null)
            aimCamera = Camera.main;

        if (tongueOrigin == null)
            tongueOrigin = transform;

        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = tongueWidth;
        lineRenderer.endWidth = tongueWidth;
        lineRenderer.enabled = false;
    }

    private void Start()
    {
        if (!useTestInput)
            return;

        if (playerInput == null ||
            playerInput.actions == null)
        {
            Debug.LogWarning(
                "테스트 입력을 사용하려면 PlayerInput이 필요합니다.",
                this
            );
            return;
        }

        tongueAction = playerInput.actions.FindAction(
            tongueActionName,
            false
        );

        if (tongueAction == null)
        {
            Debug.LogWarning(
                $"Input Action '{tongueActionName}'을 찾지 못했습니다.",
                this
            );
        }
    }

    private void Update()
    {
        if (!useTestInput || tongueAction == null)
            return;

        if (tongueAction.WasPressedThisFrame())
            FireTongue();
    }

    // --------------------------------
    // Test Tongue Fire
    // --------------------------------

    public void FireTongue()
    {
        if (aimCamera == null || Mouse.current == null)
            return;

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        Ray aimRay = aimCamera.ScreenPointToRay(
            mousePosition
        );

        Vector3 origin = tongueOrigin.position;

        Vector3 visualEnd =
            origin + transform.forward * tongueRange;

        // 카메라에서 조준한 오브젝트 찾기
        if (Physics.Raycast(
            aimRay,
            out RaycastHit aimHit,
            1000f,
            tongueHitLayers,
            QueryTriggerInteraction.Ignore
        ))
        {
            Vector3 hitPoint = aimHit.point;

            Vector3 toHit = hitPoint - origin;
            float distance = toHit.magnitude;

            if (distance > 0.001f)
            {
                visualEnd = origin +
                    toHit.normalized *
                    Mathf.Min(distance, tongueRange);
            }

            // 혀의 실제 사거리 검사
            if (distance <= tongueRange &&
                distance > 0.001f)
            {
                // 혀 시작점과 목표 사이의 장애물 검사
                if (HasClearPath(
                    origin,
                    hitPoint,
                    aimHit.collider
                ))
                {
                    HandleTongueHit(aimHit.collider);
                }
            }
        }

        ShowTongue(origin, visualEnd);
    }

    // --------------------------------
    // Tongue Hit
    // --------------------------------

    private void HandleTongueHit(Collider hitCollider)
    {
        if (hitCollider == null)
            return;

        // 1. 체스판 흰 블록
        ChessPullableBlock block =
            hitCollider.GetComponentInParent<ChessPullableBlock>();

        if (block != null)
        {
            TryPullBlock(block, transform.position);
            return;
        }

        // 2. 협동 퍼즐 의자
        CoopChairPuzzle chair =
            hitCollider.GetComponentInParent<CoopChairPuzzle>();

        if (chair != null)
        {
            TryPullChair(chair);
            return;
        }
    }

    // --------------------------------
    // Chess Puzzle Integration
    // --------------------------------

    public bool TryPullBlock(
        ChessPullableBlock block,
        Vector3 playerWorldPosition)
    {
        if (block == null)
            return false;

        ChessPuzzleBoard board =
            block.GetComponentInParent<ChessPuzzleBoard>();

        if (board == null)
        {
            Debug.LogWarning(
                "ChessPuzzleBoard를 찾을 수 없습니다.",
                block
            );

            return false;
        }

        return board.TryPullBlock(
            block,
            playerWorldPosition
        );
    }

    // --------------------------------
    // Chair Puzzle Integration
    // --------------------------------

    // 팀원의 혀 적중 코드에서 직접 호출 가능
    public bool TryPullChair(CoopChairPuzzle chair)
    {
        if (chair == null)
            return false;

        return chair.TryPullChair();
    }

    // --------------------------------
    // Tongue Path Detection
    // --------------------------------

    private bool HasClearPath(
        Vector3 origin,
        Vector3 targetPoint,
        Collider targetCollider)
    {
        Vector3 direction = targetPoint - origin;
        float distance = direction.magnitude;

        if (distance <= 0.001f)
            return false;

        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            direction.normalized,
            distance + 0.05f,
            tongueHitLayers,
            QueryTriggerInteraction.Ignore
        );

        System.Array.Sort(
            hits,
            (a, b) => a.distance.CompareTo(b.distance)
        );

        foreach (RaycastHit hit in hits)
        {
            // 개구리 자신의 Collider 무시
            if (hit.collider.transform == transform ||
                hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            // 먼저 맞은 Collider가
            // 조준한 오브젝트인지 확인
            if (hit.collider == targetCollider)
                return true;

            // 같은 상호작용 오브젝트에 속한
            // 다른 Collider를 먼저 맞은 경우도 허용
            ChessPullableBlock targetBlock =
                targetCollider.GetComponentInParent<ChessPullableBlock>();

            if (targetBlock != null &&
                hit.collider.GetComponentInParent<ChessPullableBlock>()
                    == targetBlock)
            {
                return true;
            }

            CoopChairPuzzle targetChair =
                targetCollider.GetComponentInParent<CoopChairPuzzle>();

            if (targetChair != null &&
                hit.collider.GetComponentInParent<CoopChairPuzzle>()
                    == targetChair)
            {
                return true;
            }

            // 다른 물체가 앞에 있으면 실패
            return false;
        }

        return false;
    }

    // --------------------------------
    // Line Renderer
    // --------------------------------

    private void ShowTongue(Vector3 start, Vector3 end)
    {
        if (visualCoroutine != null)
            StopCoroutine(visualCoroutine);

        visualCoroutine = StartCoroutine(
            TongueVisualRoutine(start, end)
        );
    }

    private IEnumerator TongueVisualRoutine(
        Vector3 start,
        Vector3 end)
    {
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);

        lineRenderer.enabled = true;

        yield return new WaitForSeconds(
            tongueVisualDuration
        );

        lineRenderer.enabled = false;
        visualCoroutine = null;
    }

    private void OnDisable()
    {
        if (visualCoroutine != null)
        {
            StopCoroutine(visualCoroutine);
            visualCoroutine = null;
        }

        if (lineRenderer != null)
            lineRenderer.enabled = false;
    }
}

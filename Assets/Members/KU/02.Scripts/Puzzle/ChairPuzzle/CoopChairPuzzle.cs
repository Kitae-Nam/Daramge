
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class CoopChairPuzzle : MonoBehaviour
{
    [Header("Required Breakable Blocks")]
    [SerializeField]
    private GameObject[] requiredBlocks;

    [Header("Chair Movement")]
    [SerializeField]
    private Vector3 moveOffset = new Vector3(0f, 0f, 4f);

    [SerializeField, Min(0.01f)]
    private float moveDuration = 2f;

    [SerializeField]
    private bool smoothMovement = true;

    [Header("Puzzle Events")]
    [SerializeField]
    private UnityEvent onUnlocked;

    [SerializeField]
    private UnityEvent onChairMoved;

    private Vector3 startPosition;
    private Vector3 targetPosition;

    private bool isUnlocked;
    private bool isMoving;
    private bool isCompleted;

    private Coroutine moveCoroutine;

    public bool IsUnlocked => isUnlocked;
    public bool IsMoving => isMoving;
    public bool IsCompleted => isCompleted;

    private void Awake()
    {
        // 현재 의자 위치를 시작 위치로 저장
        startPosition = transform.localPosition;

        // Inspector의 Offset을 더해 목표 위치 계산
        targetPosition = startPosition + moveOffset;
    }

    private void Start()
    {
        CheckUnlockCondition();
    }

    // --------------------------------
    // Block Check
    // --------------------------------

    private bool AreAllBlocksDestroyed()
    {
        // 필요한 블록을 등록하지 않았다면
        // 잠금 해제하지 않는다.
        if (requiredBlocks == null ||
            requiredBlocks.Length == 0)
        {
            return false;
        }

        foreach (GameObject block in requiredBlocks)
        {
            // Inspector에 비어 있는 칸이 있으면
            // 설정이 잘못된 것이므로 잠금 유지
            if (block == null)
            {
                // Destroy된 오브젝트와
                // 미할당 슬롯을 런타임에 완벽하게
                // 구별하기 어려우므로
                // 아래 설계에서는 비활성화 방식 권장.
                continue;
            }

            // 살아 있고 활성화된 블록이 하나라도
            // 있으면 아직 잠겨 있다.
            if (block.activeInHierarchy)
                return false;
        }

        return true;
    }

    public void CheckUnlockCondition()
    {
        if (isUnlocked || isCompleted)
            return;

        if (!AreAllBlocksDestroyed())
            return;

        isUnlocked = true;

        Debug.Log(
            "의자 잠금 해제! 개구리 혀로 당길 수 있습니다.",
            this
        );

        onUnlocked?.Invoke();
    }

    // --------------------------------
    // Tongue Interaction
    // --------------------------------

    public void PullChair()
    {
        TryPullChair();
    }
    
    public bool TryPullChair()
    {
        if (isMoving || isCompleted)
            return false;

        // 혀가 맞을 때마다 최신 블록 상태 확인
        CheckUnlockCondition();

        if (!isUnlocked)
        {
            Debug.Log(
                "아직 의자를 움직일 수 없습니다. " +
                "의자 다리의 블록을 모두 파괴하세요.",
                this
            );

            return false;
        }

        moveCoroutine = StartCoroutine(
            MoveChairRoutine()
        );

        return true;
    }

    // --------------------------------
    // Chair Movement
    // --------------------------------

    private IEnumerator MoveChairRoutine()
    {
        isMoving = true;

        Vector3 from = transform.localPosition;

        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / Mathf.Max(moveDuration, 0.01f)
            );

            if (smoothMovement)
            {
                // Ease In / Ease Out
                t = t * t * (3f - 2f * t);
            }

            transform.localPosition = Vector3.Lerp(
                from,
                targetPosition,
                t
            );

            yield return null;
        }

        // 최종 좌표 정확하게 맞추기
        transform.localPosition = targetPosition;

        Physics.SyncTransforms();

        isMoving = false;
        isCompleted = true;
        moveCoroutine = null;

        Debug.Log("협동 의자 퍼즐 클리어!", this);

        // 나중에 보스 등장 연결
        onChairMoved?.Invoke();
    }

    // --------------------------------
    // Debug
    // --------------------------------

    private void OnDrawGizmosSelected()
    {
        Vector3 startWorld = transform.position;

        Vector3 endWorld = transform.parent != null
            ? startWorld +
              transform.parent.TransformVector(moveOffset)
            : startWorld + moveOffset;

        if (Application.isPlaying)
        {
            startWorld = transform.parent != null
                ? transform.parent.TransformPoint(startPosition)
                : startPosition;

            endWorld = transform.parent != null
                ? transform.parent.TransformPoint(targetPosition)
                : targetPosition;
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(startWorld, 0.2f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(endWorld, 0.2f);

        Gizmos.DrawLine(startWorld, endWorld);
    }
    
    #if UNITY_EDITOR
    [ContextMenu("테스트")]
    public void TestCom()
    {
        onChairMoved?.Invoke();
        
    }
    #endif 
}

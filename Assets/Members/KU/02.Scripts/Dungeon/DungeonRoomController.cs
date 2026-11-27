
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class DungeonRoomController : MonoBehaviour
{
    public enum DungeonState
    {
        Waiting,
        Opening,
        EntranceOpen,
        Combat,
        Cleared
    }

    [Header("Dungeon Doors")]
    [SerializeField]
    private PuzzleDoorView entranceDoor;

    [SerializeField]
    private PuzzleDoorView exitDoor;

    [Header("Combat Settings")]
    [SerializeField, Min(0f)]
    private float enemySpawnDelay = 0.5f;

    [Header("Dungeon Events")]
    [SerializeField]
    private UnityEvent onSpawnEnemies;

    [SerializeField]
    private UnityEvent onDungeonCleared;

    [SerializeField]
    private UnityEvent onDungeonReset;

    private DungeonState currentState =
        DungeonState.EntranceOpen;

    private Coroutine combatCoroutine;

    public DungeonState CurrentState => currentState;

    public bool IsCleared =>
        currentState == DungeonState.Cleared;

    // --------------------------------
    // Initialize
    // --------------------------------

    private void Start()
    {
        currentState = DungeonState.EntranceOpen;

        if (entranceDoor != null)
            entranceDoor.SetOpenImmediately(true);

        if (exitDoor != null)
            exitDoor.SetOpenImmediately(false);
    }

    // --------------------------------
    // Legacy Compatibility
    // --------------------------------

    // 기존 씬 이벤트 참조 유지
    public bool TryEnterDungeon()
    {
        return false;
    }

    // --------------------------------
    // Player Entry
    // --------------------------------

    public void NotifyPlayerEntered()
    {
        if (currentState != DungeonState.EntranceOpen)
            return;

        currentState = DungeonState.Combat;

        Debug.Log(
            "플레이어 입장 확인 - 입구 문 닫기",
            this
        );

        if (entranceDoor != null)
            entranceDoor.SetOpen(false);

        StopCombatCoroutine();

        combatCoroutine = StartCoroutine(
            BeginCombatRoutine()
        );
    }

    private IEnumerator BeginCombatRoutine()
    {
        if (enemySpawnDelay > 0f)
        {
            yield return new WaitForSeconds(
                enemySpawnDelay
            );
        }

        combatCoroutine = null;

        // 리셋 등으로 상태가 바뀌었다면
        // 적을 생성하지 않음
        if (currentState != DungeonState.Combat)
            yield break;

        Debug.Log(
            "던전 적 생성 시작",
            this
        );

        onSpawnEnemies?.Invoke();
    }

    // --------------------------------
    // Enemy Clear Callback
    // --------------------------------

    public void NotifyAllEnemiesDefeated()
    {
        if (currentState != DungeonState.Combat)
            return;

        StopCombatCoroutine();

        currentState = DungeonState.Cleared;

        Debug.Log(
            "던전 클리어!",
            this
        );

        if (exitDoor != null)
            exitDoor.SetOpen(true);

        onDungeonCleared?.Invoke();
    }

    // --------------------------------
    // Reset Dungeon
    // --------------------------------

    // 플레이어 사망 또는 재도전 시 호출
    public void ResetDungeon()
    {
        // 이미 클리어한 던전은 초기화하지 않음
        if (currentState == DungeonState.Cleared)
            return;

        Debug.Log(
            "던전 초기화 시작",
            this
        );

        // 적 생성 대기 코루틴 중지
        StopCombatCoroutine();

        // 적 제거 및 전투 오브젝트 초기화
        onDungeonReset?.Invoke();

        // 재입장 가능 상태로 복구
        currentState = DungeonState.EntranceOpen;

        // 입구 다시 열기
        if (entranceDoor != null)
            entranceDoor.SetOpen(true);

        // 출구 닫기
        if (exitDoor != null)
            exitDoor.SetOpen(false);

        Debug.Log(
            "던전 초기화 완료 - 재입장 가능",
            this
        );
    }

    // --------------------------------
    // Coroutine Management
    // --------------------------------

    private void StopCombatCoroutine()
    {
        if (combatCoroutine == null)
            return;

        StopCoroutine(combatCoroutine);
        combatCoroutine = null;
    }

    // --------------------------------
    // Editor Test
    // --------------------------------

    [ContextMenu("Test Enter Dungeon")]
    private void TestEnterDungeon()
    {
        if (Application.isPlaying)
            NotifyPlayerEntered();
    }

    [ContextMenu("Test Clear Dungeon")]
    private void TestClearDungeon()
    {
        if (Application.isPlaying)
            NotifyAllEnemiesDefeated();
    }

    [ContextMenu("Test Reset Dungeon")]
    private void TestResetDungeon()
    {
        if (Application.isPlaying)
            ResetDungeon();
    }

    // --------------------------------
    // Disable
    // --------------------------------

    private void OnDisable()
    {
        StopCombatCoroutine();
    }
}

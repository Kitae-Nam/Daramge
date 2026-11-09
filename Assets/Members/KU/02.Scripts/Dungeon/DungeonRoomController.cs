
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

    [Header("Entrance Settings")]
    [SerializeField, Min(0f)]
    private float entranceOpenDelay = 2f;

    [Header("Combat Settings")]
    [SerializeField, Min(0f)]
    private float enemySpawnDelay = 0.5f;

    [Header("Dungeon Events")]
    [SerializeField]
    private UnityEvent onSpawnEnemies;

    [SerializeField]
    private UnityEvent onDungeonCleared;

    private DungeonState currentState =
        DungeonState.Waiting;

    private Coroutine entranceCoroutine;
    private Coroutine combatCoroutine;

    public DungeonState CurrentState => currentState;

    public bool IsCleared =>
        currentState == DungeonState.Cleared;

    private void Start()
    {
        currentState = DungeonState.Waiting;

        if (entranceDoor != null)
            entranceDoor.SetOpen(false);

        if (exitDoor != null)
            exitDoor.SetOpen(false);
    }

    // --------------------------------
    // Entrance Interaction
    // --------------------------------

    public bool TryEnterDungeon()
    {
        if (currentState != DungeonState.Waiting)
            return false;

        currentState = DungeonState.Opening;

        Debug.Log("던전 입구 문 열기", this);

        entranceCoroutine = StartCoroutine(
            OpenEntranceRoutine()
        );

        return true;
    }

    private IEnumerator OpenEntranceRoutine()
    {
        // 입구 문 열기
        if (entranceDoor != null)
            entranceDoor.SetOpen(true);

        // 입구 문이 열리는 동안 대기
        if (entranceOpenDelay > 0f)
        {
            yield return new WaitForSeconds(
                entranceOpenDelay
            );
        }

        currentState = DungeonState.EntranceOpen;

        entranceCoroutine = null;

        Debug.Log("던전 입장 가능", this);
    }

    // --------------------------------
    // Player Entry
    // --------------------------------

    // DungeonEntryTrigger에서 호출
    public void NotifyPlayerEntered()
    {
        if (currentState != DungeonState.EntranceOpen)
            return;

        currentState = DungeonState.Combat;

        Debug.Log(
            "플레이어 입장 확인 - 입구 문 닫기",
            this
        );

        // 입구 문 닫기
        if (entranceDoor != null)
            entranceDoor.SetOpen(false);

        combatCoroutine = StartCoroutine(
            BeginCombatRoutine()
        );
    }

    private IEnumerator BeginCombatRoutine()
    {
        // 문이 닫히는 시간을 고려하여 적 생성
        if (enemySpawnDelay > 0f)
        {
            yield return new WaitForSeconds(
                enemySpawnDelay
            );
        }

        Debug.Log("던전 적 생성 시작", this);

        onSpawnEnemies?.Invoke();

        combatCoroutine = null;
    }

    // --------------------------------
    // Enemy Clear Callback
    // --------------------------------

    public void NotifyAllEnemiesDefeated()
    {
        if (currentState != DungeonState.Combat)
            return;

        currentState = DungeonState.Cleared;

        Debug.Log("던전 클리어!", this);

        if (exitDoor != null)
            exitDoor.SetOpen(true);

        onDungeonCleared?.Invoke();
    }
}

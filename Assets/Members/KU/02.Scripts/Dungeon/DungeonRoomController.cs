
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class DungeonRoomController : MonoBehaviour
{
    public enum DungeonState
    {
        Waiting,
        Opening,
        Combat,
        Cleared
    }

    [Header("Dungeon Doors")]
    [SerializeField] private PuzzleDoorView entranceDoor;
    [SerializeField] private PuzzleDoorView exitDoor;

    [Header("Entrance Settings")]
    [SerializeField, Min(0f)]
    private float entranceOpenDelay = 2f;

    [Header("Dungeon Events")]
    [SerializeField]
    private UnityEvent onSpawnEnemies;

    [SerializeField]
    private UnityEvent onDungeonCleared;

    private DungeonState currentState = DungeonState.Waiting;

    public DungeonState CurrentState => currentState;

    public bool IsCleared =>
        currentState == DungeonState.Cleared;

    private void Start()
    {
        currentState = DungeonState.Waiting;

        // 시작할 때 두 문 모두 닫힌 상태
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
        // 최초 상호작용만 허용
        if (currentState != DungeonState.Waiting)
            return false;

        currentState = DungeonState.Opening;

        Debug.Log("던전 입장 시작!", this);

        StartCoroutine(OpenDungeonRoutine());

        return true;
    }

    private IEnumerator OpenDungeonRoutine()
    {
        // 입구 문 열기
        if (entranceDoor != null)
            entranceDoor.SetOpen(true);

        // 문이 열리는 애니메이션 대기
        if (entranceOpenDelay > 0f)
        {
            yield return new WaitForSeconds(
                entranceOpenDelay
            );
        }

        // 적 생성 전에 전투 상태로 변경
        currentState = DungeonState.Combat;

        Debug.Log("던전 전투 시작!", this);

        // 팀원의 적 생성 메서드 호출
        onSpawnEnemies?.Invoke();
    }

    // --------------------------------
    // Enemy Clear Callback
    // --------------------------------

    // 팀원 스크립트에서
    // 모든 적이 죽었을 때 호출하면 된다.
    public void NotifyAllEnemiesDefeated()
    {
        // 전투 중일 때만 클리어 가능
        if (currentState != DungeonState.Combat)
            return;

        currentState = DungeonState.Cleared;

        Debug.Log("던전 클리어!", this);

        // 출구 문 열기
        if (exitDoor != null)
            exitDoor.SetOpen(true);

        // 클리어 후 추가 이벤트
        onDungeonCleared?.Invoke();
    }
}

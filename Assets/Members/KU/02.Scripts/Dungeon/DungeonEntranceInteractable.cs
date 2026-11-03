
using UnityEngine;

public class DungeonEntranceInteractable : MonoBehaviour
{
    [Header("Dungeon Reference")]
    [SerializeField]
    private DungeonRoomController dungeonRoom;

    public bool Interact()
    {
        if (dungeonRoom == null)
        {
            Debug.LogWarning(
                "DungeonRoomController가 연결되지 않았습니다.",
                this
            );

            return false;
        }

        return dungeonRoom.TryEnterDungeon();
    }
}

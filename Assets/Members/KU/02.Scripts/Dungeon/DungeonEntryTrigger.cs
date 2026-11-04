
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class DungeonEntryTrigger : MonoBehaviour
{
    [Header("Dungeon Reference")]
    [SerializeField]
    private DungeonRoomController dungeonRoom;

    [Header("Player Detection")]
    [SerializeField]
    private LayerMask playerLayer;

    private void Reset()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        box.isTrigger = true;
    }

    private void Awake()
    {
        BoxCollider box = GetComponent<BoxCollider>();

        box.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어 레이어만 감지
        if ((playerLayer.value &
            (1 << other.gameObject.layer)) == 0)
        {
            return;
        }

        if (dungeonRoom == null)
            return;

        dungeonRoom.NotifyPlayerEntered();
    }

    private void OnDrawGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>();

        if (box == null)
            return;

        Gizmos.color = Color.cyan;

        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.DrawWireCube(
            box.center,
            box.size
        );
    }
}

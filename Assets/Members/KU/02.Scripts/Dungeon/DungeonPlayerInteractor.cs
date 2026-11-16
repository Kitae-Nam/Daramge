
using CHG._02_Scripts.Player;
using UnityEngine;
using UnityEngine.InputSystem;

public class DungeonPlayerInteractor : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField]
    private bool useKeyboardInput = true;

    [SerializeField]
    private Key interactKey = Key.E;

    [Header("Interaction Settings")]
    [SerializeField, Min(0.1f)]
    private float interactionRange = 2.5f;

    [SerializeField]
    private LayerMask interactionLayers = ~0;

    [Header("References")]
    [SerializeField]
    private Transform interactionOrigin;

    [SerializeField]
    private CharacterChange characterChange;

    // --------------------------------
    // Input
    // --------------------------------

    private void Update()
    {
        if (!useKeyboardInput)
            return;

        if (Keyboard.current == null)
            return;

        // PlayerInput 없이 E키 직접 감지
        if (Keyboard.current[interactKey].wasPressedThisFrame)
        {
            TryInteract();
        }
    }

    // --------------------------------
    // Interaction
    // --------------------------------

    // 팀원의 상호작용 시스템에서 직접 호출 가능
    public bool TryInteract()
    {
        Transform originTransform = GetInteractionOrigin();
        if (originTransform == null)
            return false;

        Vector3 origin = originTransform.position;

        Collider[] colliders = Physics.OverlapSphere(
            origin,
            interactionRange,
            interactionLayers,
            QueryTriggerInteraction.Collide
        );

        DungeonEntranceInteractable nearest = null;

        float nearestDistance = float.MaxValue;

        foreach (Collider col in colliders)
        {
            DungeonEntranceInteractable entrance =
                col.GetComponentInParent<
                    DungeonEntranceInteractable>();

            if (entrance == null || !entrance.isActiveAndEnabled)
                continue;

            Vector3 closestPoint =
                col.ClosestPoint(origin);

            float distance = Vector3.Distance(
                origin,
                closestPoint
            );

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = entrance;
            }
        }

        // 상호작용 가능한 던전 입구가 없음
        if (nearest == null)
            return false;

        // 기존 DungeonEntranceInteractable의
        // Interact() 호출
        return nearest.Interact();
    }

    private Transform GetInteractionOrigin()
    {
        if (characterChange == null)
            return interactionOrigin != null ? interactionOrigin : transform;

        int index = characterChange.CurrentIndex;
        if (characterChange.IsChanging || index < 0 || index >= characterChange.Count)
            return null;

        var player = characterChange.GetPlayer(index);
        return player != null && player.isActiveAndEnabled ? player.transform : null;
    }

    // --------------------------------
    // Debug
    // --------------------------------

    private void OnDrawGizmosSelected()
    {
        Transform origin = GetInteractionOrigin();
        if (origin == null)
            return;

        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            origin.position,
            interactionRange
        );
    }
}


using UnityEngine;
using UnityEngine.InputSystem;

public class DungeonPlayerInteractor : MonoBehaviour
{
    [Header("Input")]
    [SerializeField]
    private PlayerInput playerInput;

    [SerializeField]
    private string interactActionName = "Interact";

    [Header("Interaction Settings")]
    [SerializeField, Min(0.1f)]
    private float interactionRange = 2.5f;

    [SerializeField]
    private LayerMask interactionLayers = ~0;

    private InputAction interactAction;

    private void Awake()
    {
        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();
    }

    private void Start()
    {
        if (playerInput == null ||
            playerInput.actions == null)
        {
            Debug.LogWarning(
                "PlayerInput을 연결해주세요.",
                this
            );

            return;
        }

        interactAction = playerInput.actions.FindAction(
            interactActionName,
            false
        );

        if (interactAction == null)
        {
            Debug.LogWarning(
                $"'{interactActionName}' 액션을 찾지 못했습니다.",
                this
            );
        }
    }

    private void Update()
    {
        if (interactAction == null)
            return;

        if (interactAction.WasPressedThisFrame())
            TryInteract();
    }

    public void TryInteract()
    {
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
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

            if (entrance == null)
                continue;

            Vector3 closestPoint =
                col.ClosestPoint(transform.position);

            float distance = Vector3.Distance(
                transform.position,
                closestPoint
            );

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = entrance;
            }
        }

        if (nearest != null)
        {
            nearest.Interact();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            interactionRange
        );
    }
}

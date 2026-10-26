
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PuzzleDoorView : MonoBehaviour
{
    [SerializeField]
    private Renderer doorRenderer;

    private Collider doorCollider;

    private bool isOpen;

    private void Awake()
    {
        doorCollider = GetComponent<Collider>();

        if (doorRenderer == null)
            doorRenderer = GetComponent<Renderer>();

        SetOpen(false);
    }

    public void SetOpen(bool open)
    {
        if (isOpen == open)
            return;

        isOpen = open;

        if (doorRenderer != null)
            doorRenderer.enabled = !isOpen;

        if (doorCollider != null)
            doorCollider.enabled = !isOpen;
    }
}

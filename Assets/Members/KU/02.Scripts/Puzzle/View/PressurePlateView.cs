
using UnityEngine;

public class PressurePlateView : MonoBehaviour
{
    [SerializeField]
    private Renderer targetRenderer;

    [SerializeField]
    private Color inactiveColor = Color.gray;

    [SerializeField]
    private Color activeColor = Color.green;

    private Material plateMaterial;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        if (targetRenderer != null)
            plateMaterial = targetRenderer.material;
    }

    public void UpdateView(bool isActivated)
    {
        if (plateMaterial == null)
            return;

        plateMaterial.color =
            isActivated ? activeColor : inactiveColor;
    }
}


using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BreakableWall : MonoBehaviour
{
    private bool isBroken;

    public void Break()
    {
        if (isBroken)
            return;

        isBroken = true;

        gameObject.SetActive(false);
    }
}

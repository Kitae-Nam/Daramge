
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public class PushableCrate : MonoBehaviour
{
    [SerializeField]
    private PuzzleColor crateColor;

    public PuzzleColor CrateColor => crateColor;
}

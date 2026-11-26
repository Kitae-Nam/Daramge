
using CHG._02_Scripts.Player.Hold;
using UnityEngine;

public class PushableCrate : Holdable
{
    [Header("Puzzle")]
    [SerializeField] private PuzzleColor crateColor;

    public PuzzleColor CrateColor => crateColor;

    protected override void Awake() 
    {
        base.Awake();
        GetComponent<Rigidbody>().isKinematic = true;
    }
}

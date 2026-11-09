
using UnityEngine;
using UnityEngine.Events;

public class EnergySource : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private BuzzEnergyStorage buzzEnergy;

    [SerializeField]
    private Transform buzzPlayer;

    [Header("Absorption Settings")]
    [SerializeField, Min(0.1f)]
    private float absorptionRange = 4f;

    [Header("Events")]
    [SerializeField]
    private UnityEvent onEnergyAbsorbed;

    [SerializeField]
    private UnityEvent onEnergyRestored;

    private bool hasEnergy = true;

    public bool HasEnergy => hasEnergy;

    // 버즈 전자기장 차징 완료 시 호출
    // UnityEvent 연결 가능
    public void ReceiveChargeCompleted()
    {
        if (!hasEnergy)
            return;

        if (buzzEnergy == null || buzzPlayer == null)
            return;

        // 버즈와 장치 사이의 거리 확인
        float distance = Vector3.Distance(
            buzzPlayer.position,
            transform.position
        );

        if (distance > absorptionRange)
            return;

        // 버즈가 이미 전기를 가지고 있다면 흡수 불가
        if (!buzzEnergy.TryStoreEnergy())
            return;

        hasEnergy = false;

        Debug.Log(
            $"{name}의 전기가 흡수되었습니다.",
            this
        );

        onEnergyAbsorbed?.Invoke();
    }

    // 초기화
    public void ResetSource()
    {
        hasEnergy = true;
        onEnergyRestored?.Invoke();
    }

    [ContextMenu("Test Absorb Energy")]
    private void TestAbsorbEnergy()
    {
        if (Application.isPlaying)
            ReceiveChargeCompleted();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(
            transform.position,
            absorptionRange
        );
    }
}

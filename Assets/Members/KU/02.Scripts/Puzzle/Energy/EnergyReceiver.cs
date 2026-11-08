
using UnityEngine;
using UnityEngine.Events;

public class EnergyReceiver : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private BuzzEnergyStorage buzzEnergy;

    [Header("Events")]
    [SerializeField]
    private UnityEvent onPowered;

    [SerializeField]
    private UnityEvent onPowerReset;

    private bool isPowered;

    public bool IsPowered => isPowered;

    private void Awake()
    {
        isPowered = false;
    }

    // 버즈의 일반 공격이 적중했을 때 호출
    // UnityEvent 연결 가능
    public void ReceiveNormalAttackHit()
    {
        if (isPowered)
            return;

        if (buzzEnergy == null)
            return;

        // 버즈가 전기를 가지고 있지 않으면 무반응
        if (!buzzEnergy.TryUseEnergy())
            return;

        isPowered = true;

        Debug.Log(
            $"{name}에 전력이 공급되었습니다!",
            this
        );

        onPowered?.Invoke();
    }

    // 퍼즐 초기화
    public void ResetReceiver()
    {
        isPowered = false;
        onPowerReset?.Invoke();
    }

    [ContextMenu("Test Normal Attack Hit")]
    private void TestNormalAttackHit()
    {
        if (Application.isPlaying)
            ReceiveNormalAttackHit();
    }
}

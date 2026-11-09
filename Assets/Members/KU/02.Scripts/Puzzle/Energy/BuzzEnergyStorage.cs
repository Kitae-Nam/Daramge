
using UnityEngine;
using UnityEngine.Events;

public class BuzzEnergyStorage : MonoBehaviour
{
    [Header("Events")]
    [SerializeField]
    private UnityEvent onEnergyGained;

    [SerializeField]
    private UnityEvent onEnergyUsed;

    private bool hasEnergy;

    public bool HasEnergy => hasEnergy;

    private void Awake()
    {
        hasEnergy = false;
    }

    // 전기 흡수
    public bool TryStoreEnergy()
    {
        if (hasEnergy)
            return false;

        hasEnergy = true;

        Debug.Log("버즈가 전기를 흡수했습니다.", this);

        onEnergyGained?.Invoke();

        return true;
    }

    // 전기 방출
    public bool TryUseEnergy()
    {
        if (!hasEnergy)
            return false;

        hasEnergy = false;

        Debug.Log("버즈가 전기를 방출했습니다.", this);

        onEnergyUsed?.Invoke();

        return true;
    }

    // 퍼즐 테스트 및 초기화
    public void ResetEnergy()
    {
        hasEnergy = false;
    }
}

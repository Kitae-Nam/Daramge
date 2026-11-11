using System;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace NKT.Enemy.UI
{
    [CreateAssetMenu(fileName = "Health view model", menuName = "System/UI/Health view model", order = 0)]
    public class HealthViewModelSO : ScriptableObject, INotifyBindablePropertyChanged
    {
        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;
        //EventHandler는 닷넷 표준 C# 델리게이트이고
        //이벤트 Sender와 Event를 전달한다. Event는 BindablePropertyChangedEventArgs 이거다.

        [SerializeField] private float current = 80f;
        [SerializeField] private float max = 100f;
        [SerializeField] private float lag = 80f;
        //이 값을 데이터 모델을 만들데 Tween시켜도 되고
        //트랜지션으로 처리해도 돼.

        [CreateProperty]
        public float Current
        {
            get => current;
            set
            {
                float clamped = Mathf.Clamp(value, 0, max);
                if (Mathf.Approximately(current, clamped)) return;
                current = clamped;
                NotifyValueChanged();
            }
        }

        [CreateProperty]
        public float Max
        {
            get => max;
            set
            {
                float clamped = Mathf.Max(1f, value);
                if (Mathf.Approximately(max, clamped)) return;
                max = clamped;
                current = Mathf.Clamp(current, 0, max);
                NotifyValueChanged();
            }
        }
        
        [CreateProperty]
        public string HPLabel => $"{Mathf.RoundToInt(current)}/{Mathf.RoundToInt(max)}";

        //체력이 80%일때 20%가 나오게.
        [CreateProperty] public float TranslateY => (1 - current / max) * 100f;
        
        
        [CreateProperty]
        public float Ratio => max <= 0f ? 0f : Mathf.Clamp01(current / max);

        [CreateProperty] 
        public Scale FillScale => new Scale(new Vector2(Ratio, 1f));
        [CreateProperty]
        public Scale LagScale => new Scale(new Vector2(max <= 0 ? 0 : Mathf.Clamp01(lag/max), 1f));

        public void Set(float currentValue, float maxValue)
        {
            Max = maxValue;
            Current = currentValue;
            NotifyValueChanged();
        }

        public void SetLag(float value)
        {
            lag = Mathf.Clamp(value, 0, max);
            Notify(nameof(LagScale)); //랙이 변할 때 굳이 다 변할 필요 없다.
        }
        
        [ContextMenu("Notify")]
        private void NotifyValueChanged()
        {
            Notify(nameof(Current));
            Notify(nameof(Max));
            Notify(nameof(TranslateY));
            Notify(nameof(HPLabel));
            Notify(nameof(Ratio));
            Notify(nameof(FillScale));
            Notify(nameof(LagScale));
        }
        
        private void Notify(string propertyName)
        => propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(propertyName));
    }
}
using System;
using CHG._02_Scripts.Player;

namespace CHG._02_Scripts.UI.Presenter
{
    public class StaminaPresenter : IDisposable
    {
        private const string HiddenClass = "stamina-gauge--hidden";
        private const string LowClass= "stamina-gauge--low";
        private const float LowThreshold = 0.3f;

        private readonly RadialGauge _gauge;
        private readonly StaminaModule _model;

        public StaminaPresenter(RadialGauge gauge, StaminaModule model)
        {
            _gauge = gauge;
            _model = model;
            _model.OnStaminaChanged += HandleStaminaChanged;
            HandleStaminaChanged(_model.CurrentStamina, _model.MaxStamina);
        }

        private void HandleStaminaChanged(float current, float max)
        {
            float ratio = max > 0f ? current / max : 0f;
            _gauge.Value = ratio;
            _gauge.EnableInClassList(HiddenClass, ratio >= 1f);
            _gauge.EnableInClassList(LowClass, ratio <= LowThreshold);
        }

        public void Dispose()
            => _model.OnStaminaChanged -= HandleStaminaChanged;
    }
}
using System;
using CHG._02_Scripts.Player;

namespace CHG._02_Scripts.UI.Presenter
{
    public class StaminaPresenter : IDisposable
    {
        private const string HiddenClass = "stamina-gauge--hidden";

        private readonly RadialGauge _gauge;
        private readonly StaminaModule _model;

        public StaminaPresenter(RadialGauge gauge, StaminaModule model)
        {
            _gauge = gauge;
            _model = model;
            _model.OnStaminaChanged += HandleStaminaChanged;
        }

        private void HandleStaminaChanged(float current, float max)
        {
            float ratio = max > 0f ? current / max : 0f;
            _gauge.Value = ratio;
            _gauge.EnableInClassList(HiddenClass, ratio >= 1f);
        }

        public void Dispose()
            => _model.OnStaminaChanged -= HandleStaminaChanged;
    }
}
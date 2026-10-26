using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace CHG._02_Scripts.CoreSystem.EffectSystem
{
    [VFXBinder("Transform/My Transform Binder")]
    public class MyTransformBinder : VFXBinderBase
    {
        [VFXPropertyBinding("UnityEditor.VFX.Transform")]
        public ExposedProperty Property;
        public Transform Target;

        private ExposedProperty _position;
        private ExposedProperty _angles;
        private ExposedProperty _scale;

        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateSubProperties();
        }

        private void OnValidate() => UpdateSubProperties();

        private void UpdateSubProperties()
        {
            _position = Property + "_position";
            _angles = Property + "_angles";
            _scale = Property + "_scale";
        }

        public override bool IsValid(VisualEffect component)
        {
            return Target != null 
                && component.HasVector3((int)_position)
                && component.HasVector3((int)_angles)
                && component.HasVector3((int)_scale);;
        }

        public override void UpdateBinding(VisualEffect component)
        {
            Transform vfxTrm = component.transform;
            
            component.SetVector3((int)_position, vfxTrm.InverseTransformPoint(Target.position));
            component.SetVector3((int)_angles, 
                (Quaternion.Inverse(vfxTrm.rotation) * Target.rotation).eulerAngles);
            component.SetVector3((int)_scale, new Vector3(
                Target.lossyScale.x / vfxTrm.lossyScale.x,
                Target.lossyScale.y / vfxTrm.lossyScale.y,
                Target.lossyScale.z / vfxTrm.lossyScale.z));
        }
    }
}
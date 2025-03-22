using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Features.Filter.TemporalDenoiser
{
    public sealed class TemporalDenoiserSetting:VolumeComponent, IPostProcessComponent
    {

        public VarianceClippingParameter varianceClipping = new VarianceClippingParameter(VarianceClipping._4Tap);

        // [Tooltip("Sampling Distance")]
        // public ClampedFloatParameter spread = new ClampedFloatParameter(1.0f, 0f, 1f);
        
        [Tooltip("Feedback")]
        public ClampedFloatParameter feedback = new ClampedFloatParameter(0.0f, 0f, 1f);

        public bool IsActive() => feedback.value > 0.0f && feedback.overrideState == true;

        public bool IsTileCompatible() => false;
    }

    public enum VarianceClipping
    {
        Disabled,
        _4Tap,
        _8Tap
    }
    [Serializable]
    public sealed class VarianceClippingParameter : VolumeParameter<VarianceClipping>
    {
        public VarianceClippingParameter(VarianceClipping value, bool overrideState = false)
            : base(value, overrideState) { }
    }

}

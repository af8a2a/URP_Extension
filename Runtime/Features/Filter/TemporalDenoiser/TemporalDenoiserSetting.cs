using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Features.Filter.TemporalDenoiser
{
    public sealed class TemporalDenoiserSetting:VolumeComponent, IPostProcessComponent
    {
        [Tooltip("The quality of AntiAliasing")]
        public MotionBlurQualityParameter quality = new MotionBlurQualityParameter(MotionBlurQuality.Low);

        [Tooltip("Sampling Distance")]
        public ClampedFloatParameter spread = new ClampedFloatParameter(1.0f, 0f, 1f);
        
        [Tooltip("Feedback")]
        public ClampedFloatParameter feedback = new ClampedFloatParameter(0.0f, 0f, 1f);

        public bool IsActive() => feedback.value > 0.0f && feedback.overrideState == true;

        public bool IsTileCompatible() => false;
    }

    
}

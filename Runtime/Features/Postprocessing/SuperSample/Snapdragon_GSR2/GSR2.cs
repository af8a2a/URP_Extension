using UnityEngine.Rendering;

namespace Features.Postprocessing.SuperSample.Snapdragon_GSR2
{
    public sealed class GSR2 : VolumeComponent, IPostProcessComponent
    {
        public ClampedFloatParameter upscaledRatio = new ClampedFloatParameter(1f, 0.1f, 2f);
        public ClampedFloatParameter minLerpContribution = new ClampedFloatParameter(1f, 0f, 1f);

        public BoolParameter enabled = new BoolParameter(false);

        public bool IsActive() => enabled.value == true;
    }
}
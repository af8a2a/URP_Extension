using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Features.VolumetricLight
{
    public class VolumetricLightVolume : VolumeComponent, IPostProcessComponent
    {
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);
        public ClampedIntParameter sampleCount = new ClampedIntParameter(1, 1, 64);
        public bool IsActive()
        {
            return intensity.value > 0;
        }

    }
}
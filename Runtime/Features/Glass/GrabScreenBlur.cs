using UnityEngine.Rendering;

namespace URP_Extension.Features.Glass
{
    public class GrabScreenBlur : VolumeComponent, IPostProcessComponent
    {
        public ClampedIntParameter blurAmount = new ClampedIntParameter(0, 0, 4);
        public ClampedFloatParameter blurIntensity = new ClampedFloatParameter(0f, 0f, 1f);
        public BoolParameter enabled = new BoolParameter(false);

        public bool IsActive() => enabled.value;
    }
}
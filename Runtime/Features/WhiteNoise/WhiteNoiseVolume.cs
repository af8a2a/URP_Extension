using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;


    public class WhiteNoiseVolume : VolumeComponent, IPostProcessComponent
    {
        public ClampedFloatParameter Intensity = new ClampedFloatParameter(0f, 0f, 1f);

        public bool IsActive()
        {
            return Intensity.value > 0f;
        }

        public bool IsTileCompatible() => false;
    }

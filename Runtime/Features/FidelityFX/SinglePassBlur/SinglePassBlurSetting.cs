using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace URP_Extension.Features.SinglePassBlur
{
    public class SinglePassBlurSetting : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter enabled = new BoolParameter(false);
        public BoolParameter wavefrontOperation = new BoolParameter(false);
        public BoolParameter fp16Packed = new BoolParameter(false);

        public bool IsActive() => enabled.value;
    }
}
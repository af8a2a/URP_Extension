using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Features.CoreFeature
{
    public class ForwardGBufferManager
    {
        int NeedGbufferPasses = 0;

        static Lazy<ForwardGBufferManager> _instance = new Lazy<ForwardGBufferManager>(() => new ForwardGBufferManager());

        public static ForwardGBufferManager instance => _instance.Value;

        public void UseGBufferPasses()
        {
            NeedGbufferPasses++;
        }

        public void ReleaseGBufferPasses()
        {
            NeedGbufferPasses--;
        }

        public bool EnableGBufferPasses()
        {
            return NeedGbufferPasses > 0;
        }
    }
}
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Features.Filter.TemporalDenoiser
{
    public class TemporalDenoiser : IDisposable
    {
        private RTHandle input;
        
        //RGBA32_Float
        //RGB channel store previous color
        //A channel store previous depth
        private RTHandle[] historyHandle = new RTHandle[2];

        private RTHandle targetRT;
        Material TemporalDenoiserMaterial;


        private static readonly int accumFactor = Shader.PropertyToID("_AccumulationFactor");
        private int frameCount = 0;

        public TemporalDenoiser()
        {
        }

        public void Setup(CommandBuffer cmd, RenderingData renderingData, RTHandle targetRT)
        {
            this.targetRT = targetRT;
            RenderTextureDescriptor desc = targetRT.rt.descriptor;
            desc.colorFormat = RenderTextureFormat.ARGBFloat;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;
            desc.useMipMap = false;
            RenderingUtils.ReAllocateIfNeeded(ref historyHandle[0], desc, FilterMode.Point, TextureWrapMode.Clamp,
                name: "_HistoryTexture_0");
            // cmd.SetGlobalTexture("_SourceTexture", sourceHandle);
            RenderingUtils.ReAllocateIfNeeded(ref historyHandle[1], desc, FilterMode.Point, TextureWrapMode.Clamp,
                name: "_HistoryTexture_1");

            TemporalDenoiserMaterial = new Material(Shader.Find("PostProcessing/TemporalFilter"));
        }

        public void Dispose()
        {
            historyHandle?.ToList().ForEach(rt => rt?.Release());
        }

        //
        public void Execute(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var setting = VolumeManager.instance.stack.GetComponent<TemporalDenoiserSetting>();
            
            if (setting is null || !setting.IsActive())
            {
                return;
            }

            var readIndex = frameCount % 2;
            frameCount += 1;
            var writeIndex = frameCount % 2;
        
            
            foreach (var enabledKeyword in TemporalDenoiserMaterial.enabledKeywords)
            {
                TemporalDenoiserMaterial.DisableKeyword(enabledKeyword);
            }
            switch (setting.varianceClipping.value)
            {
                case VarianceClipping.Disabled:
                    break;
                case VarianceClipping._4Tap:
                    CoreUtils.SetKeyword(cmd,"VARIANCE_CLIPPING_4TAP",true);
                    break;
                case VarianceClipping._8Tap:
                    CoreUtils.SetKeyword(cmd,"VARIANCE_CLIPPING_8TAP",true);

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            cmd.SetGlobalTexture("_HistoryColorTexture", historyHandle[readIndex]);
            TemporalDenoiserMaterial.SetFloat(accumFactor, setting.feedback.value);
            Blitter.BlitCameraTexture(cmd, targetRT, historyHandle[writeIndex],
                TemporalDenoiserMaterial, pass: 0);
            Blitter.BlitCameraTexture(cmd, historyHandle[writeIndex], targetRT);
        }
    }
}
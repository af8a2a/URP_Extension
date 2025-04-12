using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
using RenderGraphUtils = UnityEngine.Rendering.RenderGraphModule.Util.RenderGraphUtils;

public class WhiteNoiseRenderPassFeature : ScriptableRendererFeature
{
    private const string ShaderName = "WhiteNoise";


    class WhiteNoiseRenderPass : ScriptableRenderPass
    {
        private Material whiteNoiseMaterial;

        public Material WhiteNoiseMaterial
        {
            get
            {
                if (whiteNoiseMaterial == null)
                {
                    whiteNoiseMaterial = new Material(Shader.Find(ShaderName));
                }

                return whiteNoiseMaterial;
            }
        }

        public WhiteNoiseRenderPass()
        {
            requiresIntermediateTexture = true;
        }

        static class ShaderConstants
        {
            public static readonly int _Intensity = Shader.PropertyToID("_Intensity");
        }

        class PassData
        {
            Material material;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var volume = VolumeManager.instance.stack.GetComponent<WhiteNoiseVolume>();

            if (volume == null || !volume.IsActive())
            {
                return;
            }

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            WhiteNoiseMaterial.SetFloat(ShaderConstants._Intensity, volume.Intensity.value);

            TextureHandle source = resourceData.activeColorTexture;
            var destinationDesc = renderGraph.GetTextureDesc(source);
            destinationDesc.name = $"CameraColor-{nameof(WhiteNoiseRenderPass)}";
            destinationDesc.clearBuffer = false;

            TextureHandle destination = renderGraph.CreateTexture(destinationDesc);

            RenderGraphUtils.BlitMaterialParameters para = new(source, destination, whiteNoiseMaterial,
                0);
            renderGraph.AddBlitPass(para, passName: nameof(WhiteNoiseRenderPass));
            resourceData.cameraColor = destination;
        }
    }

    WhiteNoiseRenderPass whiteNoiseRenderPass;

    /// <inheritdoc/>
    public override void Create()
    {
        whiteNoiseRenderPass = new WhiteNoiseRenderPass();

        // Configures where the render pass should be injected.
        whiteNoiseRenderPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(whiteNoiseRenderPass);
    }
}
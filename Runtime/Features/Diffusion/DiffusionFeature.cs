using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Features.Diffusion
{
    public class DiffusionFeature : ScriptableRendererFeature
    {
        private DiffusionPass _diffusionPass;
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        public override void Create()
        {
            _diffusionPass = new DiffusionPass();
            _diffusionPass.renderPassEvent = renderPassEvent;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(_diffusionPass);
        }
    }

    public class DiffusionPass : ScriptableRenderPass
    {
        private Material DiffusionMaterial;

        private static int SourceTexture = Shader.PropertyToID("_SourceTexture");
        private static int IntensityID = Shader.PropertyToID("_Intensity");
        private static int BlurIntensityID = Shader.PropertyToID("_BlurIntensity");
        private RenderTextureDescriptor TextureDescriptor;

        public DiffusionPass()
        {
            profilingSampler = new ProfilingSampler("Diffusion");

            DiffusionMaterial = new Material(Shader.Find("Diffusion"));
            TextureDescriptor = new RenderTextureDescriptor(Screen.width, Screen.height,
                RenderTextureFormat.RGB111110Float, 0);
        }

        class PassData
        {
            internal Material material;

            internal TextureHandle cameraColor;

            // internal TextureHandle sourceTexture;
            internal TextureHandle blurHTexture;
            internal TextureHandle blurVTexture;
        }


        private void UpdateBlurSettings(DiffusionSetting setting)
        {
            if (DiffusionMaterial == null) return;

            // Use the Volume settings or the default settings if no Volume is set.
            float blurIntensity = setting.blurIntensity.GetValue<float>();
            float intensity = setting.intensity.GetValue<float>();
            DiffusionMaterial.SetFloat(IntensityID, intensity);
            DiffusionMaterial.SetFloat(BlurIntensityID, blurIntensity);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            if (resourceData.isActiveTargetBackBuffer)
                return;


            TextureDescriptor.width = cameraData.cameraTargetDescriptor.width;
            TextureDescriptor.height = cameraData.cameraTargetDescriptor.height;
            TextureDescriptor.depthBufferBits = 0;

            TextureHandle cameraColor = resourceData.activeColorTexture;
            TextureHandle blurTextureH =
                UniversalRenderer.CreateRenderGraphTexture(renderGraph, TextureDescriptor, "_DiffusionHorizon", false);
            TextureHandle blurTextureV =
                UniversalRenderer.CreateRenderGraphTexture(renderGraph, TextureDescriptor, "_DiffusionVertical", false);

            // Update the blur settings in the material

            // This check is to avoid an error from the material preview in the scene
            if (!cameraColor.IsValid() || !blurTextureH.IsValid() || !blurTextureV.IsValid())
                return;
            var volumeComponent =
                VolumeManager.instance.stack.GetComponent<DiffusionSetting>();
            if (volumeComponent == null || !volumeComponent.IsActive())
            {
                return;
            }

            UpdateBlurSettings(volumeComponent);

            using (var builder =
                   renderGraph.AddUnsafePass<PassData>("Diffusion", out var passData, profilingSampler))
            {
                builder.AllowGlobalStateModification(true);
                builder.AllowPassCulling(false);
                // passData.sourceTexture = cameraColor;
                passData.blurHTexture = blurTextureH;
                passData.blurVTexture = blurTextureV;
                passData.material = DiffusionMaterial;
                passData.cameraColor = resourceData.cameraColor;

                builder.UseTexture(passData.cameraColor, AccessFlags.ReadWrite);
                builder.UseTexture(passData.blurHTexture, AccessFlags.ReadWrite);
                builder.UseTexture(passData.blurVTexture, AccessFlags.ReadWrite);


                builder.SetRenderFunc((PassData data, UnsafeGraphContext rgContext) =>
                {
                    var cmd = CommandBufferHelpers.GetNativeCommandBuffer(rgContext.cmd);
                    Blitter.BlitCameraTexture(cmd, data.cameraColor, data.blurHTexture, data.material, 0);
                    Blitter.BlitCameraTexture(cmd, data.blurHTexture, data.blurVTexture, data.material, 1);
                    Blitter.BlitCameraTexture(cmd, data.cameraColor, data.blurHTexture);
                    data.material.SetTexture(SourceTexture, data.blurHTexture);

                    Blitter.BlitCameraTexture(cmd, data.blurVTexture, data.cameraColor, data.material,
                        2);
                });
            }
        }
    }
}
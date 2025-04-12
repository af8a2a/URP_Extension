using System;
using Features.VolumetricLight;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;
using RenderGraphUtils = UnityEngine.Rendering.RenderGraphModule.Util.RenderGraphUtils;


public class VolumetricLightFeature : ScriptableRendererFeature
{
    class VolumetricLightPass : ScriptableRenderPass
    {
        private Material _material = new(Shader.Find("Volumetric Light"));

        private RenderTextureDescriptor TextureDescriptor = new(Screen.width, Screen.height,
            RenderTextureFormat.RGB111110Float, 0);

        public VolumetricLightPass()
        {
            profilingSampler = new ProfilingSampler(nameof(VolumetricLightPass));
        }

        class PassData
        {
            internal Material material;

            internal TextureHandle cameraColor;

            internal TextureHandle volumeTexture;
            internal TextureHandle blurHTexture;
            internal TextureHandle blurVTexture;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_material == null)
            {
                return;
            }

            var cameraData = frameData.Get<UniversalCameraData>();
            var resourceData = frameData.Get<UniversalResourceData>();
            var setting = VolumeManager.instance.stack.GetComponent<VolumetricLightVolume>();
            if (setting == null || !setting.IsActive())
            {
                return;
            }

            TextureDescriptor.width = cameraData.cameraTargetDescriptor.width;
            TextureDescriptor.height = cameraData.cameraTargetDescriptor.height;
            TextureDescriptor.depthBufferBits = 0;

            TextureHandle cameraColor = resourceData.activeColorTexture;
            TextureHandle blurTextureH =
                UniversalRenderer.CreateRenderGraphTexture(renderGraph, TextureDescriptor, "_VolumetricLightHorizon",
                    false);
            TextureHandle blurTextureV =
                UniversalRenderer.CreateRenderGraphTexture(renderGraph, TextureDescriptor, "_VolumetricLightVertical",
                    false);


            _material.SetFloat("_StepTime", setting.sampleCount.value);
            _material.SetFloat("_Intensity", setting.intensity.value);

            using (var builder =
                   renderGraph.AddUnsafePass<PassData>("VolumetricLight", out var passData, profilingSampler))
            {
                builder.AllowGlobalStateModification(true);
                builder.AllowPassCulling(false);
                // passData.sourceTexture = cameraColor;
                passData.blurHTexture = blurTextureH;
                passData.blurVTexture = blurTextureV;
                passData.material = _material;
                passData.cameraColor = resourceData.cameraColor;

                builder.UseTexture(passData.cameraColor, AccessFlags.ReadWrite);
                builder.UseTexture(passData.blurHTexture, AccessFlags.ReadWrite);
                builder.UseTexture(passData.blurVTexture, AccessFlags.ReadWrite);


                builder.SetRenderFunc((PassData data, UnsafeGraphContext rgContext) =>
                {
                    var cmd = CommandBufferHelpers.GetNativeCommandBuffer(rgContext.cmd);
                    Blitter.BlitCameraTexture(cmd, data.cameraColor, data.blurHTexture, data.material, 0);
                    Blitter.BlitCameraTexture(cmd, data.blurHTexture, data.blurVTexture, data.material, 1);
                    Blitter.BlitCameraTexture(cmd, data.blurVTexture, data.blurHTexture, data.material, 2);
                    Blitter.BlitCameraTexture(cmd, data.blurHTexture, data.cameraColor, data.material, 3);
                });
            }
        }

        //     // Here you can implement the rendering logic.
        //     // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        //     // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        //     // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        //     public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        //     {
        //         if (_material == null)
        //         {
        //             return;
        //         }
        //
        //         var camera = renderingData.cameraData.camera;
        //
        //         if (camera.cameraType == CameraType.Preview)
        //         {
        //             return;
        //         }
        //
        //         var setting = VolumeManager.instance.stack.GetComponent<VolumetricLightVolume>();
        //         if (setting == null)
        //         {
        //             return;
        //         }
        //
        //         var cmd = CommandBufferPool.Get("VolumetricLight");
        //
        //         var desc = new RenderTextureDescriptor(renderingData.cameraData.cameraTargetDescriptor.width,
        //             renderingData.cameraData.cameraTargetDescriptor.height, RenderTextureFormat.RGB111110Float);
        //
        //         _material.SetFloat("_StepTime", setting.sampleCount.value);
        //         _material.SetFloat("_Intensity", setting.intensity.value);
        //
        //         RenderingUtils.ReAllocateIfNeeded(ref RT0, desc, name: "VolumetricLightBlurHorizonRT");
        //         RenderingUtils.ReAllocateIfNeeded(ref RT1, desc, name: "VolumetricLightBlurVerticalRT");
        //
        //
        //         Blitter.BlitTexture(cmd, renderingData.cameraData.renderer.cameraColorTargetHandle, RT1, _material, 0);
        //         Blit(cmd, RT1, RT0, _material, 1);
        //         Blit(cmd, RT0, RT1, _material, 2);
        //         Blit(cmd, RT1, renderingData.cameraData.renderer.cameraColorTargetHandle, _material, 3);
        //
        //         context.ExecuteCommandBuffer(cmd);
        //         CommandBufferPool.Release(cmd);
        //     }
    }

    public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

    VolumetricLightPass m_ScriptablePass;


    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new VolumetricLightPass();

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = renderPassEvent;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }
}
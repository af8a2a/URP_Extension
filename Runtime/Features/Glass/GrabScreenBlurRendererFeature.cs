using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;
using URP_Extension.Features.Glass;
using RenderGraphUtils = UnityEngine.Rendering.RenderGraphModule.Util.RenderGraphUtils;

public class GrabScreenBlurRendererFeature : ScriptableRendererFeature
{
    private GrabScreenBlurPass grabScreenBlurPass;

    public override void Create()
    {
        grabScreenBlurPass = new GrabScreenBlurPass();
        grabScreenBlurPass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
    }


    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(grabScreenBlurPass);
    }

    // render pass
    class GrabScreenBlurPass : ScriptableRenderPass
    {
        private Material blurMat;
        private int blurAmount;

        private Material BlurMat
        {
            get
            {
                if (blurMat == null)
                {
                    blurMat = CoreUtils.CreateEngineMaterial("FullScreenBlur");
                }

                return blurMat;
            }
        }

        private RenderTexture _texture;

        public GrabScreenBlurPass()
        {
            blurMat = new Material(Shader.Find("FullScreenBlur"));
            profilingSampler = new ProfilingSampler(nameof(GrabScreenBlurPass));
        }

        private class PassData
        {
            internal TextureHandle cameraTexture;
            internal TextureHandle blurHorizonRT;
            internal TextureHandle blurVerticalRT;
            internal Material blurMat;
            internal int blurAmount;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var volume = VolumeManager.instance.stack.GetComponent<GrabScreenBlur>();
            if (volume == null || !volume.IsActive())
            {
                return;
            }

            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();

            var source = resourceData.activeColorTexture;
            var destinationDesc = renderGraph.GetTextureDesc(source);
            // destinationDesc.name = $"CameraColor-{passName}";
            // destinationDesc.clearBuffer = false;

            TextureHandle blurHorizonRT = renderGraph.CreateTexture(destinationDesc);
            TextureHandle blurVerticalRT = renderGraph.CreateTexture(destinationDesc);
            blurMat.SetFloat("_BlurIntensity", volume.blurIntensity.value);

            // var blurHorizonRT = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "blurHorizonRT", false);
            // var blurVerticalRT = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "BlurVerticalRT", false);

            // RenderGraphUtils.BlitMaterialParameters blitH =
            //     new RenderGraphUtils.BlitMaterialParameters(blurHorizonRT, blurVerticalRT, BlurMat, 0);
            // RenderGraphUtils.BlitMaterialParameters blitV =
            //     new RenderGraphUtils.BlitMaterialParameters(blurVerticalRT, blurHorizonRT, BlurMat, 1);
            // for (int i = 0; i < volume.blurAmount.value; i++)
            // {
            //     renderGraph.AddBlitPass(blitH);
            //     renderGraph.AddBlitPass(blitV);
            // }

            using (var builder = renderGraph.AddUnsafePass<PassData>("GrabBlur", out var passData))
            {
                builder.AllowPassCulling(false);
                passData.blurHorizonRT = blurHorizonRT;
                passData.blurVerticalRT = blurVerticalRT;
                passData.cameraTexture = resourceData.activeColorTexture;
                passData.blurMat = blurMat;
                passData.blurAmount = volume.blurAmount.value;

                builder.UseTexture(passData.cameraTexture);
                builder.UseTexture(passData.blurHorizonRT, AccessFlags.ReadWrite);
                builder.UseTexture(passData.blurVerticalRT, AccessFlags.ReadWrite);
                builder.SetGlobalTextureAfterPass(blurHorizonRT,
                    Shader.PropertyToID("_BlurTexture"));

                builder.SetRenderFunc((PassData data, UnsafeGraphContext context) =>
                {
                    var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
                    Blitter.BlitCameraTexture(cmd, data.cameraTexture, data.blurVerticalRT, blurMat, 0);

                    Blitter.BlitCameraTexture(cmd, data.blurVerticalRT, data.blurHorizonRT, blurMat, 1);

                    for (int i = 0; i < data.blurAmount - 1; i++)
                    {
                        Blitter.BlitCameraTexture(cmd, data.blurHorizonRT, data.blurVerticalRT, blurMat, 0);
                        Blitter.BlitCameraTexture(cmd, data.blurVerticalRT, data.blurHorizonRT, blurMat, 1);
                    }
                });
            }
        }
    }
}
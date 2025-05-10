using UnityEngine;
using UnityEngine.Rendering.Universal;
using URP_Extension.Features.SinglePassBlur;

namespace Features.FidelityFX.SinglePassBlur
{
    [DisallowMultipleRendererFeature]
    public class SinglePassBlurFeature : ScriptableRendererFeature
    {
        [SerializeField] ComputeShader computeShader;
        SinglePassBlurPass singlePassBlurPass;

        public override void Create()
        {
            singlePassBlurPass = new SinglePassBlurPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            singlePassBlurPass.Setup(computeShader);

            renderer.EnqueuePass(singlePassBlurPass);
        }
    }
}
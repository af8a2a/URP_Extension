using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace URP_Extension.Features.MipGenerator
{
    //copy and modifed from HDRP
    public class MipGenerator
    {
        RTHandle m_TempColorTargets;
        RTHandle m_TempDownsamplePyramid;
        MaterialPropertyBlock m_PropertyBlock;

        ComputeShader m_ColorPyramidCS;

        int m_ColorDownsampleKernel;
        int m_ColorGaussianKernel;
        int m_HizDownsampleKernel;
        int m_PassThroughtKernel;

        RenderTextureDescriptor m_ColorPyramidDescriptor;
        RenderTextureDescriptor m_DepthPyramidDescriptor;

        public MipGenerator()
        {
            m_ColorPyramidCS = Resources.Load<ComputeShader>("ColorPyramid");
            m_ColorDownsampleKernel = m_ColorPyramidCS.FindKernel("KColorDownsample");
            m_ColorGaussianKernel = m_ColorPyramidCS.FindKernel("KColorGaussian");
            m_HizDownsampleKernel = m_ColorPyramidCS.FindKernel("KHizDownsample");
            m_PassThroughtKernel = m_ColorPyramidCS.FindKernel("KPassthrought");
            m_PropertyBlock = new MaterialPropertyBlock();
            m_ColorPyramidDescriptor = new RenderTextureDescriptor();
            m_DepthPyramidDescriptor = new RenderTextureDescriptor();
        }

        private static Lazy<MipGenerator> s_Instance = new Lazy<MipGenerator>(() => new MipGenerator());

        public static MipGenerator Instance => s_Instance.Value;


        public int RenderDepthPyramid(CommandBuffer cmd, Vector2Int size, Texture source,
            RenderTexture destination)
        {
            int srcMipLevel = 0;
            int srcMipWidth = size.x;
            int srcMipHeight = size.y;

            m_DepthPyramidDescriptor = destination.descriptor;
            m_DepthPyramidDescriptor.useDynamicScale = true;
            m_DepthPyramidDescriptor.useMipMap = false;

            // Check if format has changed since last time we generated mips
            RenderingUtils.ReAllocateHandleIfNeeded(ref m_TempDownsamplePyramid,
                Vector2.one * 0.5f,
                m_DepthPyramidDescriptor,
                name: "Temporary Downsampled Pyramid");

            // m_TempDownsamplePyramid = RTHandles.Alloc(
            //     Vector2.one * 0.5f,
            //     // dimension: source.dimension,
            //     // filterMode: FilterMode.Bilinear,
            //     colorFormat: destination.graphicsFormat,
            //     enableRandomWrite: true,
            //     useMipMap: false,
            //     useDynamicScale: true,
            //     name: "Temporary Downsampled Pyramid"
            // );

            cmd.SetRenderTarget(m_TempDownsamplePyramid);
            cmd.ClearRenderTarget(false, true, Color.black);


            bool isHardwareDrsOn = DynamicResolutionHandler.instance.HardwareDynamicResIsEnabled();
            var hardwareTextureSize = new Vector2Int(source.width, source.height);
            if (isHardwareDrsOn)
                hardwareTextureSize = DynamicResolutionHandler.instance.ApplyScalesOnSize(hardwareTextureSize);

            float sourceScaleX = (float)size.x / (float)hardwareTextureSize.x;
            float sourceScaleY = (float)size.y / (float)hardwareTextureSize.y;

            // // Copies src mip0 to dst mip0
            // // Note that we still use a fragment shader to do the first copy because fragment are faster at copying
            // // data types like R11G11B10 (default) and pretty similar in term of speed with R16G16B16A16.
            // m_PropertyBlock.SetTexture(HDShaderIDs._BlitTexture, source);
            // m_PropertyBlock.SetVector(HDShaderIDs._BlitScaleBias, new Vector4(sourceScaleX, sourceScaleY, 0f, 0f));
            // m_PropertyBlock.SetFloat(HDShaderIDs._BlitMipLevel, 0f);
            // cmd.SetRenderTarget(destination, 0, CubemapFace.Unknown, -1);
            // cmd.SetViewport(new Rect(0, 0, srcMipWidth, srcMipHeight));
            // cmd.DrawProcedural(Matrix4x4.identity, HDUtils.GetBlitMaterial(source.dimension), 0, MeshTopology.Triangles,
            //     3, 1, m_PropertyBlock);
            m_PropertyBlock.SetTexture("_BlitTexture", source);
            m_PropertyBlock.SetVector("_BlitScaleBias", new Vector4(sourceScaleX, sourceScaleY, 0f, 0f));
            m_PropertyBlock.SetFloat("_BlitMipLevel", 0f);
            cmd.SetRenderTarget(destination, 0, CubemapFace.Unknown, -1);
            cmd.SetViewport(new Rect(0, 0, srcMipWidth, srcMipHeight));

            cmd.DrawProcedural(Matrix4x4.identity, Blitter.GetBlitMaterial(source.dimension), 0, MeshTopology.Triangles,
                3, 1, m_PropertyBlock);
            var finalTargetSize = new Vector2Int(destination.width, destination.height);
            if (destination.useDynamicScale && isHardwareDrsOn)
                finalTargetSize = DynamicResolutionHandler.instance.ApplyScalesOnSize(finalTargetSize);

            // Note: smaller mips are excluded as we don't need them and the gaussian compute works
            // on 8x8 blocks
            while (srcMipWidth >= 8 || srcMipHeight >= 8)
            {
                int dstMipWidth = Mathf.Max(1, srcMipWidth >> 1);
                int dstMipHeight = Mathf.Max(1, srcMipHeight >> 1);

                // Scale for downsample
                float scaleX = ((float)srcMipWidth / finalTargetSize.x);
                float scaleY = ((float)srcMipHeight / finalTargetSize.y);

                cmd.SetComputeVectorParam(m_ColorPyramidCS, "_Size",
                    new Vector4(srcMipWidth, srcMipHeight, 0f, 0f));

                {
                    // Downsample.
                    cmd.SetComputeTextureParam(m_ColorPyramidCS, m_HizDownsampleKernel, "_Source",
                        destination, srcMipLevel);
                    cmd.SetComputeTextureParam(m_ColorPyramidCS, m_HizDownsampleKernel, "_Destination",
                        m_TempDownsamplePyramid);
                    cmd.DispatchCompute(m_ColorPyramidCS, m_HizDownsampleKernel, (dstMipWidth + 7) / 8,
                        (dstMipHeight + 7) / 8, 1);


                    // Single pass blur
                    cmd.SetComputeVectorParam(m_ColorPyramidCS, "_Size",
                        new Vector4(dstMipWidth, dstMipHeight, 0f, 0f));
                    cmd.SetComputeTextureParam(m_ColorPyramidCS, m_PassThroughtKernel, "_Source",
                        m_TempDownsamplePyramid);
                    cmd.SetComputeTextureParam(m_ColorPyramidCS, m_PassThroughtKernel, "_Destination",
                        destination, srcMipLevel + 1);
                    cmd.DispatchCompute(m_ColorPyramidCS, m_PassThroughtKernel, (dstMipWidth + 7) / 8,
                        (dstMipHeight + 7) / 8, 1);
                }

                srcMipLevel++;
                srcMipWidth >>= 1;
                srcMipHeight >>= 1;

                finalTargetSize.x >>= 1;
                finalTargetSize.y >>= 1;
            }

            return srcMipLevel + 1;
        }


        // Generates the gaussian pyramid of source into destination
        // We can't do it in place as the color pyramid has to be read while writing to the color
        // buffer in some cases (e.g. refraction, distortion)
        // Returns the number of mips
        public int RenderColorGaussianPyramid(CommandBuffer cmd, Vector2Int size, Texture source,
            RenderTexture destination)
        {
            int srcMipLevel = 0;
            int srcMipWidth = size.x;
            int srcMipHeight = size.y;

            
            
            m_ColorPyramidDescriptor = destination.descriptor;
            m_ColorPyramidDescriptor.useDynamicScale = true;
            m_ColorPyramidDescriptor.useMipMap = false;

            // Check if format has changed since last time we generated mips
            RenderingUtils.ReAllocateHandleIfNeeded(ref m_TempDownsamplePyramid,
                Vector2.one * 0.5f,
                m_ColorPyramidDescriptor,
                name: "Temporary Downsampled Pyramid");

            // // Check if format has changed since last time we generated mips
            // m_TempDownsamplePyramid = RTHandles.Alloc(
            //     Vector2.one * 0.5f,
            //     dimension: source.dimension,
            //     filterMode: FilterMode.Bilinear,
            //     colorFormat: destination.graphicsFormat,
            //     enableRandomWrite: true,
            //     useMipMap: false,
            //     useDynamicScale: true,
            //     name: "Temporary Downsampled Pyramid"
            // );

            cmd.SetRenderTarget(m_TempDownsamplePyramid);
            cmd.ClearRenderTarget(false, true, Color.black);


            bool isHardwareDrsOn = DynamicResolutionHandler.instance.HardwareDynamicResIsEnabled();
            var hardwareTextureSize = new Vector2Int(source.width, source.height);
            if (isHardwareDrsOn)
                hardwareTextureSize = DynamicResolutionHandler.instance.ApplyScalesOnSize(hardwareTextureSize);

            float sourceScaleX = (float)size.x / (float)hardwareTextureSize.x;
            float sourceScaleY = (float)size.y / (float)hardwareTextureSize.y;

            // // Copies src mip0 to dst mip0
            // // Note that we still use a fragment shader to do the first copy because fragment are faster at copying
            // // data types like R11G11B10 (default) and pretty similar in term of speed with R16G16B16A16.
            // m_PropertyBlock.SetTexture(HDShaderIDs._BlitTexture, source);
            // m_PropertyBlock.SetVector(HDShaderIDs._BlitScaleBias, new Vector4(sourceScaleX, sourceScaleY, 0f, 0f));
            // m_PropertyBlock.SetFloat(HDShaderIDs._BlitMipLevel, 0f);
            // cmd.SetRenderTarget(destination, 0, CubemapFace.Unknown, -1);
            // cmd.SetViewport(new Rect(0, 0, srcMipWidth, srcMipHeight));
            // cmd.DrawProcedural(Matrix4x4.identity, HDUtils.GetBlitMaterial(source.dimension), 0, MeshTopology.Triangles,
            //     3, 1, m_PropertyBlock);
            m_PropertyBlock.SetTexture("_BlitTexture", source);
            m_PropertyBlock.SetVector("_BlitScaleBias", new Vector4(sourceScaleX, sourceScaleY, 0f, 0f));
            m_PropertyBlock.SetFloat("_BlitMipLevel", 0f);
            cmd.SetRenderTarget(destination, 0, CubemapFace.Unknown, -1);
            cmd.SetViewport(new Rect(0, 0, srcMipWidth, srcMipHeight));

            cmd.DrawProcedural(Matrix4x4.identity, Blitter.GetBlitMaterial(source.dimension), 0, MeshTopology.Triangles,
                3, 1, m_PropertyBlock);
            var finalTargetSize = new Vector2Int(destination.width, destination.height);
            if (destination.useDynamicScale && isHardwareDrsOn)
                finalTargetSize = DynamicResolutionHandler.instance.ApplyScalesOnSize(finalTargetSize);

            // Note: smaller mips are excluded as we don't need them and the gaussian compute works
            // on 8x8 blocks
            while (srcMipWidth >= 8 || srcMipHeight >= 8)
            {
                int dstMipWidth = Mathf.Max(1, srcMipWidth >> 1);
                int dstMipHeight = Mathf.Max(1, srcMipHeight >> 1);

                // Scale for downsample
                float scaleX = ((float)srcMipWidth / finalTargetSize.x);
                float scaleY = ((float)srcMipHeight / finalTargetSize.y);

                cmd.SetComputeVectorParam(m_ColorPyramidCS, "_Size",
                    new Vector4(srcMipWidth, srcMipHeight, 0f, 0f));

                {
                    // Downsample.
                    cmd.SetComputeTextureParam(m_ColorPyramidCS, m_ColorDownsampleKernel, "_Source",
                        destination, srcMipLevel);
                    cmd.SetComputeTextureParam(m_ColorPyramidCS, m_ColorDownsampleKernel, "_Destination",
                        m_TempDownsamplePyramid);
                    cmd.DispatchCompute(m_ColorPyramidCS, m_ColorDownsampleKernel, (dstMipWidth + 7) / 8,
                        (dstMipHeight + 7) / 8, 1);

                    // Single pass blur
                    cmd.SetComputeVectorParam(m_ColorPyramidCS, "_Size",
                        new Vector4(dstMipWidth, dstMipHeight, 0f, 0f));
                    cmd.SetComputeTextureParam(m_ColorPyramidCS, m_ColorGaussianKernel, "_Source",
                        m_TempDownsamplePyramid);
                    cmd.SetComputeTextureParam(m_ColorPyramidCS, m_ColorGaussianKernel, "_Destination",
                        destination, srcMipLevel + 1);
                    cmd.DispatchCompute(m_ColorPyramidCS, m_ColorGaussianKernel, (dstMipWidth + 7) / 8,
                        (dstMipHeight + 7) / 8, 1);
                }

                srcMipLevel++;
                srcMipWidth >>= 1;
                srcMipHeight >>= 1;

                finalTargetSize.x >>= 1;
                finalTargetSize.y >>= 1;
            }

            return srcMipLevel + 1;
        }
    }
}
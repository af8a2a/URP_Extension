using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    public sealed class AmbientOcclusionHistory : CameraHistoryItem
    {
        private int[] m_Ids = new int[2];

        private static readonly string[] m_Names = new[]
        {
            "AmbientOcclusionColorHistory0",
            "AmbientOcclusionColorHistory1"
        };

        private Hash128 m_DescKey;
        private RenderTextureDescriptor m_Descriptor;

        /// <summary>
        /// Get the current history texture.
        /// Current history might not be valid yet. It is valid only after executing the producing render pass.
        /// </summary>
        /// <param name="eyeIndex">Eye index, typically XRPass.multipassId.</param>
        /// <returns>The texture.</returns>
        public RTHandle GetCurrentTexture(int eyeIndex = 0)
        {
            if ((uint)eyeIndex >= m_Ids.Length)
                return null;

            return GetCurrentFrameRT(m_Ids[eyeIndex]);
        }

        /// <summary>
        /// Get the previous history texture.
        /// Previous history might not be valid yet. It is valid only after executing the producing render pass.
        /// </summary>
        /// <param name="eyeIndex">Eye index, typically XRPass.multipassId.</param>
        /// <returns>The texture.</returns>
        public RTHandle GetPreviousTexture(int eyeIndex = 0)
        {
            if ((uint)eyeIndex >= m_Ids.Length)
                return null;

            return GetPreviousFrameRT(m_Ids[eyeIndex]);
        }


        private bool IsAllocated()
        {
            return GetCurrentTexture() != null;
        }

        // True if the desc changed, graphicsFormat etc.
        private bool IsDirty(ref RenderTextureDescriptor desc)
        {
            return m_DescKey != Hash128.Compute(ref desc);
        }

        private void Alloc(ref RenderTextureDescriptor desc, bool xrMultipassEnabled)
        {
            // In generic case, the current texture might not have been written yet. We need double buffering.
            AllocHistoryFrameRT(m_Ids[0], 2, ref desc, m_Names[0]);

            if (xrMultipassEnabled)
                AllocHistoryFrameRT(m_Ids[1], 2, ref desc, m_Names[1]);

            m_Descriptor = desc;
            m_DescKey = Hash128.Compute(ref desc);
        }


        internal RenderTextureDescriptor GetHistoryDescriptor(in RenderTextureDescriptor cameraDesc)
        {
            var aoDesc = cameraDesc;
            aoDesc.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
            aoDesc.mipCount = 0;
            aoDesc.enableRandomWrite = true;
            aoDesc.msaaSamples = 1;

            return aoDesc;
        }

        // Return true if the RTHandles were reallocated.
        internal bool Update(in RenderTextureDescriptor cameraDesc)
        {
            if (cameraDesc.width > 0 && cameraDesc.height > 0 && cameraDesc.graphicsFormat != GraphicsFormat.None)
            {
                var historyDesc = GetHistoryDescriptor(in cameraDesc);

                if (IsDirty(ref historyDesc))
                    Reset();

                if (!IsAllocated())
                {
                    Alloc(ref historyDesc, false);
                    return true;
                }
            }

            return false;
        }


        public override void Reset()
        {
            for (int i = 0; i < m_Ids.Length; i++)
                ReleaseHistoryFrameRT(m_Ids[i]);
        }
    }
}
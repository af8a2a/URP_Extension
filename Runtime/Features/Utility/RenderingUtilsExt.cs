using UnityEngine;
using UnityEngine.Rendering;

namespace URP_Extension.Features.Utility
{
    public static class RenderingUtilsExt
    {
        /// <summary>
        /// Divides one value by another and rounds up to the next integer.
        /// This is often used to calculate dispatch dimensions for compute shaders.
        /// </summary>
        /// <param name="value">The value to divide.</param>
        /// <param name="divisor">The value to divide by.</param>
        /// <returns>The value divided by the divisor rounded up to the next integer.</returns>
        public static int DivRoundUp(int value, int divisor)
        {
            return (value + (divisor - 1)) / divisor;
        }


        public static float ComputeViewportScale(int viewportSize, int bufferSize)
        {
            float rcpBufferSize = 1.0f / bufferSize;

            // Scale by (vp_dim / buf_dim).
            return viewportSize * rcpBufferSize;
        }

        public static float ComputeViewportLimit(int viewportSize, int bufferSize)
        {
            float rcpBufferSize = 1.0f / bufferSize;

            // Clamp to (vp_dim - 0.5) / buf_dim.
            return (viewportSize - 0.5f) * rcpBufferSize;
        }

        public static Vector4 ComputeViewportScaleAndLimit(Vector2Int viewportSize, Vector2Int bufferSize)
        {
            return new Vector4(ComputeViewportScale(viewportSize.x, bufferSize.x), // Scale(x)
                ComputeViewportScale(viewportSize.y, bufferSize.y), // Scale(y)
                ComputeViewportLimit(viewportSize.x, bufferSize.x), // Limit(x)
                ComputeViewportLimit(viewportSize.y, bufferSize.y)); // Limit(y)
        }

        public static int CalcMipCount(Vector2Int textureSize)
        {
            int maxLength = Mathf.Max(textureSize.x, textureSize.y);
            return (int)Mathf.Log(maxLength, 2);
        }
    }
}
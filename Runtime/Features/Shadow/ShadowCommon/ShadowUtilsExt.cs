using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Features.Shadow
{
    public static class ShadowUtilsExt
    {
        internal static void RenderShadowSliceNoOffset(RasterCommandBuffer cmd,
            ref ShadowSliceData shadowSliceData, ref RendererList shadowRendererList,
            Matrix4x4 proj, Matrix4x4 view)
        {
            cmd.SetGlobalDepthBias(1.0f,
                2.5f); // these values match HDRP defaults (see https://github.com/Unity-Technologies/Graphics/blob/9544b8ed2f98c62803d285096c91b44e9d8cbc47/com.unity.render-pipelines.high-definition/Runtime/Lighting/Shadow/HDShadowAtlas.cs#L197 )

            cmd.SetViewProjectionMatrices(view, proj);
            if (shadowRendererList.isValid)
                cmd.DrawRendererList(shadowRendererList);

            cmd.DisableScissorRect();
            cmd.SetGlobalDepthBias(0.0f, 0.0f); // Restore previous depth bias values
        }


        static Vector3 s_camCenter = Vector3.zero;
        static Vector3 s_TR_Dir = Vector3.zero;

        public static bool ComputeDirectionalShadowMatricesAndCullingSphere
        (ref UniversalCameraData cameraData, ref UniversalShadowData shadowData, int cascadeIndex, Light light, int shadowResolution
            , out Vector4 cullingSphere, out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out float ZDistance)
        {
            var camNear = 0f;
            var s_BL_Dir = Vector3.zero;
            if (cascadeIndex == 0)
            {
                camNear = cameraData.camera.nearClipPlane;
                Matrix4x4 VP = cameraData.GetProjectionMatrix() * cameraData.GetViewMatrix();
                Matrix4x4 I_VP = VP.inverse;

                Vector3 nearBL = I_VP.MultiplyPoint(new Vector3(-1, -1, -1));
                Vector3 nearTR = I_VP.MultiplyPoint(new Vector3(1, 1, -1));

                s_camCenter = cameraData.camera.transform.position;
                s_BL_Dir = nearBL - s_camCenter;
                s_TR_Dir = nearTR - s_camCenter;
            }



            float cascadeFar = camNear + cameraData.maxShadowDistance * shadowData.mainLightShadowCascadesSplitArray[cascadeIndex];
            float cascadeNear = camNear;
            if (cascadeIndex > 0)
            {
                cascadeNear = camNear + cameraData.maxShadowDistance * shadowData.mainLightShadowCascadesSplitArray[cascadeIndex - 1];
            }

            Vector3 cascadeNearBL = s_camCenter + s_BL_Dir / camNear * cascadeNear;
            Vector3 cascadeNearTR = s_camCenter + s_TR_Dir / camNear * cascadeNear;

            Vector3 cascadeFarBL = s_camCenter + s_BL_Dir / camNear * cascadeFar;
            Vector3 cascadeFarTR = s_camCenter + s_TR_Dir / camNear * cascadeFar;

            //sphere bounding box
            float a = Vector3.Distance(cascadeNearBL, cascadeNearTR);
            float b = Vector3.Distance(cascadeFarBL, cascadeFarTR);
            float l = cascadeFar - cascadeNear;

            float x = (b * b - a * a) / (8 * l) + l / 2;

            Vector3 sphereCenter = cameraData.camera.transform.position + cameraData.camera.transform.forward * (cascadeNear + x);
            float sphereR = Mathf.Sqrt(x * x + a * a / 4);

            //Anti-Shimmering
            if (cascadeIndex < 4)
            {
                float squrePixelWidth = 2 * sphereR / shadowResolution;
                Vector3 sphereCenterLS = light.transform.worldToLocalMatrix.MultiplyPoint(sphereCenter);
                sphereCenterLS.x /= squrePixelWidth;
                sphereCenterLS.x = Mathf.Floor(sphereCenterLS.x);
                sphereCenterLS.x *= squrePixelWidth;
                sphereCenterLS.y /= squrePixelWidth;
                sphereCenterLS.y = Mathf.Floor(sphereCenterLS.y);
                sphereCenterLS.y *= squrePixelWidth;
                sphereCenter = light.transform.localToWorldMatrix.MultiplyPoint(sphereCenterLS);
            }


            cullingSphere.x = sphereCenter.x;
            cullingSphere.y = sphereCenter.y;
            cullingSphere.z = sphereCenter.z;
            cullingSphere.w = sphereR;

            float backDistance = sphereR * light.shadowNearPlane * 10;
            Vector3 shadowMapEye = sphereCenter - light.transform.forward * backDistance;
            Vector3 shadowMapAt = sphereCenter;

            Matrix4x4 lookMatrix = Matrix4x4.LookAt(shadowMapEye, shadowMapAt, light.transform.up);
            // Matrix that mirrors along Z axis, to match the camera space convention.
            Matrix4x4 scaleMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, -1));
            // Final view matrix is inverse of the LookAt matrix, and then mirrored along Z.
            viewMatrix = scaleMatrix * lookMatrix.inverse;

            projMatrix = Matrix4x4.Ortho(-sphereR, sphereR, -sphereR, sphereR, 0.0f, 2.0f * backDistance);
            ZDistance = 2.0f * backDistance;

            return true;
        }
    }
}
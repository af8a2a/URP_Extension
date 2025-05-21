using Unity.Mathematics;
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


        public static bool CalculateDirectionalLightShadowSliceData(
            UniversalLightData lightData,
            UniversalCameraData cameraData,
            UniversalShadowData shadowData,
            int cascadeIndex, int shadowResolution, int shadowLightIndex, out ShadowSliceData shadowSliceData)
        {
            shadowSliceData = default;
            // 阴影切片的尺寸
            float delta = 0;
            Light mainLight = lightData.visibleLights[shadowLightIndex].light;
            Camera mainCamera = cameraData.camera;
            if (mainCamera == null)
            {
                Debug.LogWarning("没有主相机");
                return false;
            }

            // 初始化球形包围体参数
            Vector4 cullingSphere = Vector4.zero;
            Vector3 sphereCenter = Vector3.zero;
            float sphereRadius = 0;
            float nearPlane = mainCamera.nearClipPlane;
            float farPlane = mainCamera.farClipPlane;
            float shadowMaxDistance = Mathf.Min(farPlane, UniversalRenderPipeline.asset.shadowDistance);
            farPlane = shadowMaxDistance;

            // 根据级联索引设置近裁剪面和远裁剪面
            switch (cascadeIndex)
            {
                case 0:
                    break;
                case 1:
                    nearPlane = shadowData.mainLightShadowCascadesSplit[0] * shadowMaxDistance;
                    break;
                case 2:
                    nearPlane = shadowData.mainLightShadowCascadesSplit[1] * shadowMaxDistance;
                    break;
                case 3:
                    nearPlane = shadowData.mainLightShadowCascadesSplit[2] * shadowMaxDistance;
                    break;
                case 4:
                    nearPlane = shadowData.mainLightShadowCascadesSplit2[0] * shadowMaxDistance;
                    break;
                case 5:
                    nearPlane = shadowData.mainLightShadowCascadesSplit2[1] * shadowMaxDistance;
                    break;
                case 6:
                    nearPlane = shadowData.mainLightShadowCascadesSplit2[2] * shadowMaxDistance;
                    break;
                case 7:
                    nearPlane = shadowData.mainLightShadowCascadesSplit2[3] * shadowMaxDistance;
                    break;
            }

            if (cascadeIndex < shadowData.mainLightShadowCascadesCount - 1)
            {
                if (cascadeIndex < 3)
                {
                    farPlane = shadowData.mainLightShadowCascadesSplit[cascadeIndex] * shadowMaxDistance;
                }
                else if (cascadeIndex < 7)
                {
                    int index = cascadeIndex - 3;
                    farPlane = shadowData.mainLightShadowCascadesSplit2[index] * shadowMaxDistance;
                }
            }

            // 计算相机视锥体的宽高比和对角线长度
            float cameraWidth = Mathf.Tan(Mathf.Deg2Rad * mainCamera.fieldOfView / 2) * nearPlane * 2;
            float cameraHeight = cameraWidth * Screen.width / Screen.height;
            float k = Mathf.Sqrt(1 + (cameraHeight * cameraHeight) / (cameraWidth * cameraWidth)) * Mathf.Tan(mainCamera.fieldOfView * Mathf.Deg2Rad / 2);
            float k2 = k * k;

            // 根据视锥体参数计算球形包围体的中心点和半径
            if (k2 >= (farPlane - nearPlane) / (farPlane + nearPlane))
            {
                sphereCenter = farPlane * mainCamera.transform.forward + mainCamera.transform.position;
                sphereRadius = farPlane * k;
            }
            else
            {
                sphereCenter = 0.5f * (farPlane + nearPlane) * (1 + k2) * mainCamera.transform.forward + mainCamera.transform.position;
                sphereRadius = 0.5f * Mathf.Sqrt((farPlane - nearPlane) * (farPlane - nearPlane) + 2 * (farPlane * farPlane + nearPlane * nearPlane) * k2 +
                                                 (farPlane + nearPlane) * (farPlane + nearPlane) * k2 * k2);
            }

            // 减少阴影贴图中的抖动
            delta = 2.0f * sphereRadius / shadowResolution;
            Vector3 sphereCenterSnappedOS = mainLight.transform.worldToLocalMatrix.MultiplyVector(sphereCenter);
            sphereCenterSnappedOS.x /= delta;
            sphereCenterSnappedOS.x = Mathf.Floor(sphereCenterSnappedOS.x);
            sphereCenterSnappedOS.x *= delta;
            sphereCenterSnappedOS.y /= delta;
            sphereCenterSnappedOS.y = Mathf.Floor(sphereCenterSnappedOS.y);
            sphereCenterSnappedOS.y *= delta;
            sphereCenter = mainLight.transform.localToWorldMatrix.MultiplyVector(sphereCenterSnappedOS);

            // 构建观察矩阵
            Matrix4x4 viewMatrix = Matrix4x4.identity;
            viewMatrix.m00 = mainLight.transform.right.x;
            viewMatrix.m01 = mainLight.transform.right.y;
            viewMatrix.m02 = mainLight.transform.right.z;
            viewMatrix.m10 = mainLight.transform.up.x;
            viewMatrix.m11 = mainLight.transform.up.y;
            viewMatrix.m12 = mainLight.transform.up.z;
            viewMatrix.m20 = -mainLight.transform.forward.x;
            viewMatrix.m21 = -mainLight.transform.forward.y;
            viewMatrix.m22 = -mainLight.transform.forward.z;
            viewMatrix.m03 = -Vector3.Dot(mainLight.transform.right, sphereCenter);
            viewMatrix.m13 = -Vector3.Dot(mainLight.transform.up, sphereCenter);
            viewMatrix.m23 = Vector3.Dot(mainLight.transform.forward, sphereCenter);

            // 构建投影矩阵
            Matrix4x4 projectionMatrix = Matrix4x4.identity;
            projectionMatrix.m00 = 1.0f / sphereRadius;
            projectionMatrix.m11 = 1.0f / sphereRadius;
            projectionMatrix.m22 = -2.0f / (sphereRadius - (-sphereRadius));
            projectionMatrix.m23 = -(sphereRadius + (-sphereRadius)) / (sphereRadius - (-sphereRadius));
            projectionMatrix.m33 = 1;

            // 设置球形包围体数据
            cullingSphere = new Vector4(sphereCenter.x, sphereCenter.y, sphereCenter.z, sphereRadius);

            // 更新阴影切片数据
            shadowSliceData.viewMatrix = viewMatrix;
            shadowSliceData.projectionMatrix = projectionMatrix;
            shadowSliceData.splitData.cullingSphere = cullingSphere;

            return true;
        }
    }
}
// /**********************************************************************
// Copyright (c) 2021 Advanced Micro Devices, Inc. All rights reserved.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// ********************************************************************/
//
//
// [[vk::binding(0, 1)]] Texture2D<float4> g_lit_scene                                         : register(t0);
// [[vk::binding(1, 1)]] Texture2D<float> g_depth_buffer_hierarchy                             : register(t1);
// [[vk::binding(2, 1)]] Texture2D<float4> g_normal                                            : register(t2);
// [[vk::binding(3, 1)]] Texture2D<float> g_roughness                                          : register(t3);
// [[vk::binding(4, 1)]] TextureCube g_environment_map                                         : register(t4);
// [[vk::binding(5, 1)]] Texture2D<float2> g_blue_noise_texture                                : register(t5);
// [[vk::binding(6, 1)]] Buffer<uint> g_ray_list                                               : register(t6);
//
// [[vk::binding(7, 1)]] SamplerState g_environment_map_sampler                                : register(s0);
//
// [[vk::binding(8, 1)]] RWTexture2D<float4> g_intersection_output                             : register(u0);
// [[vk::binding(9, 1)]] RWBuffer<uint> g_ray_counter                                          : register(u1);
//
// #define M_PI                               3.14159265358979f
//
//
// // Mat must be able to transform origin from texture space to a linear space.
// float3 InvProjectPosition(float3 coord, float4x4 mat)
// {
//     coord.y = (1 - coord.y);
//     coord.xy = 2 * coord.xy - 1;
//     float4 projected = mul(mat, float4(coord, 1));
//     projected.xyz /= projected.w;
//     return projected.xyz;
// }
// float3 ScreenSpaceToViewSpace(float3 screen_space_position) {
//     return InvProjectPosition(screen_space_position, UNITY_MATRIX_I_P);
// }
//
// float3 FFX_SSSR_LoadWorldSpaceNormal(int2 pixel_coordinate) {
//     return normalize(2 * g_normal.Load(int3(pixel_coordinate, 0)).xyz - 1);
// }
//
// float FFX_SSSR_LoadDepth(int2 pixel_coordinate, int mip) {
//     return g_depth_buffer_hierarchy.Load(int3(pixel_coordinate, mip));
// }
//
//
// float3 ScreenSpaceToWorldSpace(float3 screen_space_position) {
//     return InvProjectPosition(screen_space_position, g_inv_view_proj);
// }
//
// // http://jcgt.org/published/0007/04/01/paper.pdf by Eric Heitz
// // Input Ve: view direction
// // Input alpha_x, alpha_y: roughness parameters
// // Input U1, U2: uniform random numbers
// // Output Ne: normal sampled with PDF D_Ve(Ne) = G1(Ve) * max(0, dot(Ve, Ne)) * D(Ne) / Ve.z
// float3 SampleGGXVNDF(float3 Ve, float alpha_x, float alpha_y, float U1, float U2) {
//     // Section 3.2: transforming the view direction to the hemisphere configuration
//     float3 Vh = normalize(float3(alpha_x * Ve.x, alpha_y * Ve.y, Ve.z));
//     // Section 4.1: orthonormal basis (with special case if cross product is zero)
//     float lensq = Vh.x * Vh.x + Vh.y * Vh.y;
//     float3 T1 = lensq > 0 ? float3(-Vh.y, Vh.x, 0) * rsqrt(lensq) : float3(1, 0, 0);
//     float3 T2 = cross(Vh, T1);
//     // Section 4.2: parameterization of the projected area
//     float r = sqrt(U1);
//     float phi = 2.0 * M_PI * U2;
//     float t1 = r * cos(phi);
//     float t2 = r * sin(phi);
//     float s = 0.5 * (1.0 + Vh.z);
//     t2 = (1.0 - s) * sqrt(1.0 - t1 * t1) + s * t2;
//     // Section 4.3: reprojection onto hemisphere
//     float3 Nh = t1 * T1 + t2 * T2 + sqrt(max(0.0, 1.0 - t1 * t1 - t2 * t2)) * Vh;
//     // Section 3.4: transforming the normal back to the ellipsoid configuration
//     float3 Ne = normalize(float3(alpha_x * Nh.x, alpha_y * Nh.y, max(0.0, Nh.z)));
//     return Ne;
// }
//
// float3 Sample_GGX_VNDF_Ellipsoid(float3 Ve, float alpha_x, float alpha_y, float U1, float U2) {
//     return SampleGGXVNDF(Ve, alpha_x, alpha_y, U1, U2);
// }
//
// float3 Sample_GGX_VNDF_Hemisphere(float3 Ve, float alpha, float U1, float U2) {
//     return Sample_GGX_VNDF_Ellipsoid(Ve, alpha, alpha, U1, U2);
// }
//
// float3x3 CreateTBN(float3 N) {
//     float3 U;
//     if (abs(N.z) > 0.0) {
//         float k = sqrt(N.y * N.y + N.z * N.z);
//         U.x = 0.0; U.y = -N.z / k; U.z = N.y / k;
//     }
//     else {
//         float k = sqrt(N.x * N.x + N.y * N.y);
//         U.x = N.y / k; U.y = -N.x / k; U.z = 0.0;
//     }
//
//     float3x3 TBN;
//     TBN[0] = U;
//     TBN[1] = cross(N, U);
//     TBN[2] = N;
//     return transpose(TBN);
// }
//
// float2 SampleRandomVector2D(uint2 pixel) {
//     return g_blue_noise_texture.Load(int3(pixel.xy % 128, 0));
// }
//
// float3 SampleReflectionVector(float3 view_direction, float3 normal, float roughness, int2 dispatch_thread_id) {
//     float3x3 tbn_transform = CreateTBN(normal);
//     float3 view_direction_tbn = mul(-view_direction, tbn_transform);
//
//     float2 u = SampleRandomVector2D(dispatch_thread_id);
//     
//     float3 sampled_normal_tbn = Sample_GGX_VNDF_Hemisphere(view_direction_tbn, roughness, u.x, u.y);
//     #ifdef PERFECT_REFLECTIONS
//         sampled_normal_tbn = float3(0, 0, 1); // Overwrite normal sample to produce perfect reflection.
//     #endif
//     
//     float3 reflected_direction_tbn = reflect(-view_direction_tbn, sampled_normal_tbn);
//
//     // Transform reflected_direction back to the initial space.
//     float3x3 inv_tbn_transform = transpose(tbn_transform);
//     return mul(reflected_direction_tbn, inv_tbn_transform);
// }
//
// // float3 SampleEnvironmentMap(float3 direction) {
// //     return g_environment_map.SampleLevel(g_environment_map_sampler, direction, 0).xyz;
// // }
//
// bool IsMirrorReflection(float roughness) {
//     return roughness < 0.0001;
// }
//

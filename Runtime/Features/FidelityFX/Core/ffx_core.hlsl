﻿// This file is part of the FidelityFX SDK.
//
// Copyright (C) 2024 Advanced Micro Devices, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and /or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

/// @defgroup FfxGPU GPU
/// The FidelityFX SDK GPU References
/// 
/// @ingroup ffxSDK

/// @defgroup FfxHLSL HLSL References
/// FidelityFX SDK HLSL GPU References
/// 
/// @ingroup FfxGPU

/// @defgroup FfxGLSL GLSL References
/// FidelityFX SDK GLSL GPU References
/// 
/// @ingroup FfxGPU

/// @defgroup FfxGPUEffects FidelityFX GPU References
/// FidelityFX Effect GPU Reference Documentation
/// 
/// @ingroup FfxGPU

/// @defgroup GPUCore GPU Core
/// GPU defines and functions
/// 
/// @ingroup FfxGPU

#if !defined(FFX_CORE_H)
#define FFX_CORE_H


#include "ffx_common_types.hlsl"



#if defined(FFX_HLSL) && defined(FFX_GPU)
    #include "ffx_core_hlsl.hlsl"
#endif // #if defined(FFX_HLSL) && defined(FFX_GPU)

#if defined(FFX_GPU)
    #include "ffx_core_gpu_common.hlsl"
    #include "ffx_core_gpu_common_half.hlsl"
    #include "ffx_core_portability.hlsl"
#endif // #if defined(FFX_GPU)


#endif // #if !defined(FFX_CORE_H)

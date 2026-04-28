#ifndef LUMINCHEN_SHADER_COMMON_INCLUDED
#define LUMINCHEN_SHADER_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#define LC_SETUP_INSTANCE_ID(input) UNITY_SETUP_INSTANCE_ID(input)
#define LC_TRANSFER_INSTANCE_ID(input, output) UNITY_TRANSFER_INSTANCE_ID(input, output)
#define LC_INIT_VERTEX_OUTPUT_STEREO(output) UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output)
#define LC_SETUP_STEREO_EYE_INDEX(input) UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)

inline VertexPositionInputs LC_GetVertexPositionInputs(float3 positionOS)
{
    return GetVertexPositionInputs(positionOS);
}

inline VertexNormalInputs LC_GetVertexNormalInputs(float3 normalOS, float4 tangentOS)
{
    return GetVertexNormalInputs(normalOS, tangentOS);
}

inline half3 LC_GetWorldSpaceViewDir(float3 positionWS)
{
    return GetWorldSpaceNormalizeViewDir(positionWS);
}

inline half3 LC_NormalFromMap(TEXTURE2D_PARAM(normalMap, normalSampler), float2 uv, half normalScale, half3 tangentWS, half3 bitangentWS, half3 baseNormalWS)
{
    half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(normalMap, normalSampler, uv), normalScale);
    half3x3 tangentToWorld = half3x3(tangentWS, bitangentWS, baseNormalWS);
    half3 normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
    return NormalizeNormalPerPixel(normalWS);
}

// 基础随机 hash
float Hash(float x)
{
    return frac(sin(x) * 43758.5453f);
}

float Hash2(float2 uv)
{
    return frac(sin(dot(uv, float2(12.9898f, 4.233f))) * 43758.5453f);
}

// 【核心API】随时间变化的全局随机数 0~1
float TimeRandom(float time)
{
    return Hash(time * 0.15f);
}

// 带偏移种子 + 时间随机（适合多波浪分层）
float TimeRandomSeed(float time, float seed)
{
    return Hash(time * 0.12f + seed * 123.456f);
}

// 传入世界坐标 + 时间，区域差异化随机（水面最常用）
float TimeRandomPos(float3 worldPos, float time)
{
    float2 uv = worldPos.xz * 0.01f;
    return Hash2(uv + float2(time * 0.2f, time * 0.1f));
}

#endif

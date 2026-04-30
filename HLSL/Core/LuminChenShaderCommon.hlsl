#ifndef LUMINCHEN_SHADER_COMMON_INCLUDED
#define LUMINCHEN_SHADER_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#define LC_SETUP_INSTANCE_ID(input) UNITY_SETUP_INSTANCE_ID(input)
#define LC_TRANSFER_INSTANCE_ID(input, output) UNITY_TRANSFER_INSTANCE_ID(input, output)
#define LC_INIT_VERTEX_OUTPUT_STEREO(output) UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output)
#define LC_SETUP_STEREO_EYE_INDEX(input) UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)

static const float LC_PI = 3.14159265359f;
static const float LC_TWO_PI = 6.28318530718f;

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

inline float3 LC_TransformObjectToWorld(float3 positionOS)
{
    return TransformObjectToWorld(positionOS);
}

inline float3 LC_TransformWorldToObject(float3 positionWS)
{
    return TransformWorldToObject(positionWS);
}

inline float3 LC_TransformObjectToWorldDir(float3 dirOS)
{
    return TransformObjectToWorldDir(dirOS);
}

inline float3 LC_TransformWorldToObjectDir(float3 dirWS)
{
    return TransformWorldToObjectDir(dirWS);
}

inline float3 LC_TransformObjectToWorldNormal(float3 normalOS)
{
    return TransformObjectToWorldNormal(normalOS);
}

inline float4 LC_TransformWorldToHClip(float3 positionWS)
{
    return TransformWorldToHClip(positionWS);
}

inline half3 LC_NormalFromMap(TEXTURE2D_PARAM(normalMap, normalSampler), float2 uv, half normalScale, half3 tangentWS, half3 bitangentWS, half3 baseNormalWS)
{
    half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(normalMap, normalSampler, uv), normalScale);
    half3x3 tangentToWorld = half3x3(tangentWS, bitangentWS, baseNormalWS);
    half3 normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
    return NormalizeNormalPerPixel(normalWS);
}

inline half3 LC_NormalFromMapRG(TEXTURE2D_PARAM(normalMap, normalSampler), float2 uv, half normalScale, half3 tangentWS, half3 bitangentWS, half3 baseNormalWS)
{
    half4 packedNormal = SAMPLE_TEXTURE2D(normalMap, normalSampler, uv);
    half3 normalTS = UnpackNormalScale(half4(packedNormal.rg, 1.0h, packedNormal.a), normalScale);
    half3x3 tangentToWorld = half3x3(tangentWS, bitangentWS, baseNormalWS);
    half3 normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
    return NormalizeNormalPerPixel(normalWS);
}

inline half3 LC_BlendNormalRNM(half3 baseNormalWS, half3 detailNormalWS)
{
    half3 t = baseNormalWS + half3(0.0h, 0.0h, 1.0h);
    half3 u = detailNormalWS * half3(-1.0h, -1.0h, 1.0h);
    return normalize((t / max(t.z, 1e-5h)) * dot(t, u) - u);
}

inline float LC_Remap(float value, float2 inMinMax, float2 outMinMax)
{
    float t = (value - inMinMax.x) / max(inMinMax.y - inMinMax.x, 1e-5f);
    return lerp(outMinMax.x, outMinMax.y, t);
}

inline float LC_RemapClamped(float value, float2 inMinMax, float2 outMinMax)
{
    float t = saturate((value - inMinMax.x) / max(inMinMax.y - inMinMax.x, 1e-5f));
    return lerp(outMinMax.x, outMinMax.y, t);
}

inline half LC_Luminance(half3 color)
{
    return dot(color, half3(0.2126h, 0.7152h, 0.0722h));
}

inline half3 LC_ApplyExposureAndTint(half3 color, half3 tint, half exposure)
{
    return color * tint * exposure;
}

inline half LC_FresnelSchlick(half ndotv, half power)
{
    return pow(saturate(1.0h - ndotv), power);
}

inline half LC_FresnelTerm(half3 normalWS, half3 viewDirWS, half power)
{
    return LC_FresnelSchlick(saturate(dot(normalWS, viewDirWS)), power);
}

inline float2 LC_GetNormalizedScreenUV(float4 positionHCS)
{
    float2 uv = positionHCS.xy / max(positionHCS.w, 1e-5f);
    return uv * 0.5f + 0.5f;
}

inline float2 LC_RotateUV(float2 uv, float radiansValue, float2 center)
{
    float s = sin(radiansValue);
    float c = cos(radiansValue);
    float2 offset = uv - center;
    return float2(offset.x * c - offset.y * s, offset.x * s + offset.y * c) + center;
}

inline float3 LC_RotateDirectionY(float3 direction, float degrees)
{
    float radiansValue = radians(degrees);
    float s = sin(radiansValue);
    float c = cos(radiansValue);
    return float3(
        c * direction.x - s * direction.z,
        direction.y,
        s * direction.x + c * direction.z);
}

inline float LC_LinearEyeDepthFromRaw(float rawDepth)
{
    return LinearEyeDepth(rawDepth, _ZBufferParams);
}

inline float LC_Linear01DepthFromRaw(float rawDepth)
{
    return Linear01Depth(rawDepth, _ZBufferParams);
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

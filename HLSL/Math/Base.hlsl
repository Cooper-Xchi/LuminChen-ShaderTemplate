#ifndef LUMINCHEN_MATH_BASE_INCLUDED
#define LUMINCHEN_MATH_BASE_INCLUDED

inline float LC_LineFunctionMap(float x, float k, float n)
{
    return saturate(k * (x - n));
}

inline float2 LC_BuildScrollingUV(float2 uv, float2 tiling, float2 speed, float time)
{
    return uv * tiling + speed * time;
}

inline float LC_SampleScrollingNoise(TEXTURE2D_PARAM(noiseMap, noiseSampler), float2 uv, float2 tiling, float2 speed, float time)
{
    float2 noiseUV = LC_BuildScrollingUV(uv, tiling, speed, time);
    return SAMPLE_TEXTURE2D_LOD(noiseMap, noiseSampler, noiseUV, 0).r;
}

inline float3 LC_ApplyFogNoiseVertexOffset(
    float3 positionOS,
    float3 normalOS,
    TEXTURE2D_PARAM(noiseMap, noiseSampler),
    float2 uv,
    float2 tiling,
    float2 speed,
    float time,
    float amplitude,
    float verticalLift,
    out float noiseValue)
{
    noiseValue = LC_SampleScrollingNoise(TEXTURE2D_ARGS(noiseMap, noiseSampler), uv, tiling, speed, time);
    float centeredNoise = noiseValue * 2.0f - 1.0f;
    float3 offset = normalOS * centeredNoise * amplitude;
    offset.y += noiseValue * verticalLift;
    return positionOS + offset;
}

#endif

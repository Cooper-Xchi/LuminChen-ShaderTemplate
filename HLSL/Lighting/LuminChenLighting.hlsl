#ifndef LUMINCHEN_LIGHTING_INCLUDED
#define LUMINCHEN_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

struct LC_LightingInput
{
    float3 positionWS;
    half3 normalWS;
    half3 viewDirWS;
    half3 albedo;
    half3 specular;
    half smoothness;
    half metallic;
    half occlusion;
    half3 emission;
    half alpha;
};

inline half LC_Lambert(half3 normalWS, half3 lightDirWS)
{
    return saturate(dot(normalWS, lightDirWS));
}

inline half LC_BlinnPhongSpecular(half3 normalWS, half3 lightDirWS, half3 viewDirWS, half smoothness)
{
    half3 halfDir = SafeNormalize(lightDirWS + viewDirWS);
    half ndoth = saturate(dot(normalWS, halfDir));
    return pow(ndoth, max(1.0h, smoothness * 128.0h));
}

inline half3 LC_GetAmbientLighting(half3 normalWS)
{
    return SampleSH(normalWS);
}

inline float4 LC_GetShadowCoord(float3 positionWS)
{
    return TransformWorldToShadowCoord(positionWS);
}

inline Light LC_GetMainLight(float3 positionWS)
{
    return GetMainLight(LC_GetShadowCoord(positionWS));
}

inline half3 LC_GetMainLightDirection(float3 positionWS)
{
    return LC_GetMainLight(positionWS).direction;
}

inline half3 LC_GetMainLightColor(float3 positionWS)
{
    Light mainLight = LC_GetMainLight(positionWS);
    return mainLight.color * (mainLight.distanceAttenuation * mainLight.shadowAttenuation);
}

inline half3 LC_GetMainLightDiffuse(float3 positionWS, half3 normalWS)
{
    Light mainLight = LC_GetMainLight(positionWS);
    half ndotl = LC_Lambert(normalWS, mainLight.direction);
    return mainLight.color * (mainLight.distanceAttenuation * mainLight.shadowAttenuation * ndotl);
}

inline half3 LC_GetMainLightSpecular(float3 positionWS, half3 normalWS, half3 viewDirWS, half smoothness, half3 specularColor)
{
    Light mainLight = LC_GetMainLight(positionWS);
    half spec = LC_BlinnPhongSpecular(normalWS, mainLight.direction, viewDirWS, smoothness);
    return specularColor * mainLight.color * spec * mainLight.distanceAttenuation * mainLight.shadowAttenuation;
}

inline half3 LC_GetAdditionalLightsDiffuse(float3 positionWS, half3 normalWS)
{
    half3 lighting = 0.0h;

    #if defined(_ADDITIONAL_LIGHTS)
    uint lightCount = GetAdditionalLightsCount();
    for (uint lightIndex = 0u; lightIndex < lightCount; lightIndex++)
    {
        Light light = GetAdditionalLight(lightIndex, positionWS);
        half ndotl = LC_Lambert(normalWS, light.direction);
        lighting += light.color * (light.distanceAttenuation * light.shadowAttenuation * ndotl);
    }
    #endif

    return lighting;
}

inline half3 LC_GetAdditionalLightsSpecular(float3 positionWS, half3 normalWS, half3 viewDirWS, half smoothness, half3 specularColor)
{
    half3 lighting = 0.0h;

    #if defined(_ADDITIONAL_LIGHTS)
    uint lightCount = GetAdditionalLightsCount();
    for (uint lightIndex = 0u; lightIndex < lightCount; lightIndex++)
    {
        Light light = GetAdditionalLight(lightIndex, positionWS);
        half spec = LC_BlinnPhongSpecular(normalWS, light.direction, viewDirWS, smoothness);
        lighting += specularColor * light.color * spec * light.distanceAttenuation * light.shadowAttenuation;
    }
    #endif

    return lighting;
}

inline half3 LC_GetVertexLighting(float3 positionWS, half3 normalWS)
{
    return VertexLighting(positionWS, normalWS);
}

inline half3 LC_GetBakedGI(float3 normalWS)
{
    return SampleSH(normalWS);
}

inline half3 LC_ApplyFog(float3 positionWS, half3 color)
{
    float fogFactor = ComputeFogFactor(TransformWorldToHClip(positionWS).z);
    return MixFog(color, fogFactor);
}

inline half4 LC_ApplyFog(float3 positionWS, half4 color)
{
    float fogFactor = ComputeFogFactor(TransformWorldToHClip(positionWS).z);
    color.rgb = MixFog(color.rgb, fogFactor);
    return color;
}

inline half4 LC_EvaluateBasicLighting(LC_LightingInput input)
{
    half3 ambient = LC_GetAmbientLighting(input.normalWS);
    half3 mainDiffuse = LC_GetMainLightDiffuse(input.positionWS, input.normalWS);
    half3 addDiffuse = LC_GetAdditionalLightsDiffuse(input.positionWS, input.normalWS);
    half3 mainSpecular = LC_GetMainLightSpecular(input.positionWS, input.normalWS, input.viewDirWS, input.smoothness, input.specular);
    half3 addSpecular = LC_GetAdditionalLightsSpecular(input.positionWS, input.normalWS, input.viewDirWS, input.smoothness, input.specular);

    half3 diffuseLighting = ambient + mainDiffuse + addDiffuse;
    half3 color = input.albedo * diffuseLighting;
    color += mainSpecular + addSpecular;
    color *= input.occlusion;
    color += input.emission;

    return half4(color, input.alpha);
}

#endif

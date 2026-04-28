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

inline half3 LC_GetAmbientLighting(half3 normalWS)
{
    return SampleSH(normalWS);
}

inline half3 LC_GetMainLightDiffuse(float3 positionWS, half3 normalWS)
{
    Light mainLight = GetMainLight();
    half ndotl = saturate(dot(normalWS, mainLight.direction));
    return mainLight.color * (mainLight.distanceAttenuation * mainLight.shadowAttenuation * ndotl);
}

inline half3 LC_GetMainLightSpecular(float3 positionWS, half3 normalWS, half3 viewDirWS, half smoothness, half3 specularColor)
{
    Light mainLight = GetMainLight();
    half3 halfDir = SafeNormalize(mainLight.direction + viewDirWS);
    half ndoth = saturate(dot(normalWS, halfDir));
    half spec = pow(ndoth, max(1.0h, smoothness * 128.0h));
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
        half ndotl = saturate(dot(normalWS, light.direction));
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
        half3 halfDir = SafeNormalize(light.direction + viewDirWS);
        half ndoth = saturate(dot(normalWS, halfDir));
        half spec = pow(ndoth, max(1.0h, smoothness * 128.0h));
        lighting += specularColor * light.color * spec * light.distanceAttenuation * light.shadowAttenuation;
    }
    #endif

    return lighting;
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

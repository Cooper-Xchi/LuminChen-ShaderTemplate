using System.Text;

public partial class ShaderEditor : UnityEditor.EditorWindow
{
    private string BuildShaderSource()
    {
        if (templateType == ShaderTemplateType.ComputeShader)
        {
            return BuildComputeShaderSource();
        }

        return Build3DTemplateShaderSource();
    }

    private string Build3DTemplateShaderSource()
    {
        string shaderPath = BuildShaderPath();
        string surfaceTag = surfaceType == SurfaceType.Transparent ? "Transparent" : "Opaque";
        string queueTag = surfaceType == SurfaceType.Transparent ? "Transparent" : "Geometry";
        string blendLine = BuildBlendCommand();
        string zWriteLine = "            ZWrite " + (enableDepthWrite ? "On" : "Off");
        string zTestLine = "            ZTest " + ToShaderValue(depthTestMode);
        string cullLine = "            Cull " + ToShaderValue(cullMode);

        StringBuilder properties = new StringBuilder();
        StringBuilder varyings = new StringBuilder();
        StringBuilder vertexBody = new StringBuilder();
        StringBuilder fragmentBody = new StringBuilder();

        if (useMainTexture)
        {
            properties.AppendLine("        _BaseMap(\"Base Map\", 2D) = \"white\" {}");
        }

        if (useTintColor)
        {
            properties.AppendLine("        _BaseColor(\"Base Color\", Color) = (1,1,1,1)");
        }

        if (useNormalMap)
        {
            properties.AppendLine("        _NormalMap(\"Normal Map\", 2D) = \"bump\" {}");
            properties.AppendLine("        _NormalScale(\"Normal Scale\", Range(0, 2)) = 1");
        }

        if (useMaskMap)
        {
            properties.AppendLine("        _MaskMap(\"Mask Map\", 2D) = \"white\" {}");
        }

        if (surfaceWorkflow == SurfaceWorkflow.Metallic)
        {
            properties.AppendLine("        _Metallic(\"Metallic\", Range(0, 1)) = 0");
            if (useMetallicMap)
            {
                properties.AppendLine("        _MetallicMap(\"Metallic Map\", 2D) = \"black\" {}");
            }
        }

        if (surfaceWorkflow == SurfaceWorkflow.Specular)
        {
            properties.AppendLine("        _SpecColor(\"Specular Color\", Color) = (0.2,0.2,0.2,1)");
            if (useSpecularMap)
            {
                properties.AppendLine("        _SpecGlossMap(\"Specular Map\", 2D) = \"white\" {}");
            }
        }

        if (useRoughnessMap)
        {
            properties.AppendLine("        _RoughnessMap(\"Roughness Map\", 2D) = \"gray\" {}");
            properties.AppendLine("        _Roughness(\"Roughness\", Range(0, 1)) = 0.5");
        }

        if (useOcclusionMap)
        {
            properties.AppendLine("        _OcclusionMap(\"Occlusion Map\", 2D) = \"white\" {}");
            properties.AppendLine("        _OcclusionStrength(\"Occlusion Strength\", Range(0, 1)) = 1");
        }

        if (useEmissionMap)
        {
            properties.AppendLine("        [HDR]_EmissionColor(\"Emission Color\", Color) = (0,0,0,0)");
            properties.AppendLine("        _EmissionMap(\"Emission Map\", 2D) = \"black\" {}");
            properties.AppendLine("        _EmissionIntensity(\"Emission Intensity\", Range(0, 8)) = 1");
        }

        if (useHeightMap)
        {
            properties.AppendLine("        _HeightMap(\"Height Map\", 2D) = \"black\" {}");
            properties.AppendLine("        _HeightStrength(\"Height Strength\", Range(0, 0.1)) = 0.02");
        }

        if (enableAlphaClip)
        {
            properties.AppendLine($"        _Cutoff(\"Alpha Cutoff\", Range(0, 1)) = {alphaCutoff:0.###}");
        }

        varyings.AppendLine("                float4 positionHCS : SV_POSITION;");
        varyings.AppendLine("                float2 uv : TEXCOORD0;");
        varyings.AppendLine("                float3 positionWS : TEXCOORD1;");
        varyings.AppendLine("                half3 normalWS : TEXCOORD2;");
        varyings.AppendLine("                half4 tangentWS : TEXCOORD3;");
        varyings.AppendLine("                half3 bitangentWS : TEXCOORD4;");
        varyings.AppendLine("                half3 viewDirWS : TEXCOORD5;");
        varyings.AppendLine("                half4 color : COLOR;");
        varyings.AppendLine("                UNITY_VERTEX_INPUT_INSTANCE_ID");
        varyings.AppendLine("                UNITY_VERTEX_OUTPUT_STEREO");

        if (useMainTexture)
        {
            vertexBody.AppendLine("                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);");
        }
        else
        {
            vertexBody.AppendLine("                output.uv = input.uv;");
        }

        vertexBody.AppendLine("                output.positionWS = positionInputs.positionWS;");
        vertexBody.AppendLine("                output.normalWS = normalInputs.normalWS;");
        vertexBody.AppendLine("                output.tangentWS = half4(normalInputs.tangentWS, input.tangentOS.w);");
        vertexBody.AppendLine("                output.bitangentWS = normalInputs.bitangentWS;");
        vertexBody.AppendLine("                output.viewDirWS = LC_GetWorldSpaceViewDir(positionInputs.positionWS);");
        vertexBody.AppendLine("                output.color = input.color;");

        if (useMainTexture)
        {
            fragmentBody.AppendLine("                half4 albedoSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);");
        }
        else
        {
            fragmentBody.AppendLine("                half4 albedoSample = half4(1, 1, 1, 1);");
        }

        if (useNormalMap)
        {
            fragmentBody.AppendLine("                half3 normalWS = LC_NormalFromMap(TEXTURE2D_ARGS(_NormalMap, sampler_NormalMap), input.uv, _NormalScale, input.tangentWS.xyz, input.bitangentWS, input.normalWS);");
        }
        else
        {
            fragmentBody.AppendLine("                half3 normalWS = NormalizeNormalPerPixel(input.normalWS);");
        }

        fragmentBody.AppendLine("                half metallic = 0.0h;");
        fragmentBody.AppendLine("                half3 specular = half3(0.2h, 0.2h, 0.2h);");
        fragmentBody.AppendLine("                half roughness = 0.5h;");
        fragmentBody.AppendLine("                half smoothness = 0.5h;");
        fragmentBody.AppendLine("                half occlusion = 1.0h;");
        fragmentBody.AppendLine("                half height = 0.0h;");
        fragmentBody.AppendLine("                half3 emission = half3(0.0h, 0.0h, 0.0h);");

        if (useMaskMap)
        {
            fragmentBody.AppendLine("                half4 maskSample = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, input.uv);");
            fragmentBody.AppendLine("                metallic = maskSample.r;");
            fragmentBody.AppendLine("                occlusion = maskSample.g;");
            fragmentBody.AppendLine("                smoothness = maskSample.a;");
            fragmentBody.AppendLine("                roughness = 1.0h - smoothness;");
        }

        if (surfaceWorkflow == SurfaceWorkflow.Metallic)
        {
            if (useMetallicMap)
            {
                fragmentBody.AppendLine("                metallic = SAMPLE_TEXTURE2D(_MetallicMap, sampler_MetallicMap, input.uv).r * _Metallic;");
            }
            else
            {
                fragmentBody.AppendLine("                metallic = _Metallic;");
            }
        }

        if (surfaceWorkflow == SurfaceWorkflow.Specular)
        {
            fragmentBody.AppendLine("                specular = _SpecColor.rgb;");
            if (useSpecularMap)
            {
                fragmentBody.AppendLine("                half4 specGlossSample = SAMPLE_TEXTURE2D(_SpecGlossMap, sampler_SpecGlossMap, input.uv);");
                fragmentBody.AppendLine("                specular *= specGlossSample.rgb;");
                fragmentBody.AppendLine("                smoothness = specGlossSample.a;");
                fragmentBody.AppendLine("                roughness = 1.0h - smoothness;");
            }
        }

        if (useRoughnessMap)
        {
            fragmentBody.AppendLine("                roughness = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, input.uv).r * _Roughness;");
            fragmentBody.AppendLine("                smoothness = 1.0h - roughness;");
        }

        if (useOcclusionMap)
        {
            fragmentBody.AppendLine("                occlusion = lerp(1.0h, SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, input.uv).g, _OcclusionStrength);");
        }

        if (useEmissionMap)
        {
            fragmentBody.AppendLine("                emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb * _EmissionColor.rgb * _EmissionIntensity;");
        }

        if (useHeightMap)
        {
            fragmentBody.AppendLine("                height = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, input.uv).r * _HeightStrength;");
        }

        fragmentBody.AppendLine("                half4 color = albedoSample;");
        fragmentBody.AppendLine("                color *= input.color;");

        if (useTintColor)
        {
            fragmentBody.AppendLine("                color *= _BaseColor;");
        }

        if (enableAlphaClip)
        {
            fragmentBody.AppendLine("                clip(color.a - _Cutoff);");
        }

        if (surfaceType == SurfaceType.Opaque)
        {
            fragmentBody.AppendLine("                color.a = 1;");
        }

        fragmentBody.AppendLine("                LC_LightingInput lightingInput;");
        fragmentBody.AppendLine("                lightingInput.positionWS = input.positionWS;");
        fragmentBody.AppendLine("                lightingInput.normalWS = normalWS;");
        fragmentBody.AppendLine("                lightingInput.viewDirWS = input.viewDirWS;");
        fragmentBody.AppendLine("                lightingInput.albedo = color.rgb;");
        fragmentBody.AppendLine("                lightingInput.specular = specular;");
        fragmentBody.AppendLine("                lightingInput.smoothness = smoothness;");
        fragmentBody.AppendLine("                lightingInput.metallic = metallic;");
        fragmentBody.AppendLine("                lightingInput.occlusion = occlusion;");
        fragmentBody.AppendLine("                lightingInput.emission = emission;");
        fragmentBody.AppendLine("                lightingInput.alpha = color.a;");
        fragmentBody.AppendLine("                return LC_EvaluateBasicLighting(lightingInput);");

        return
$@"Shader ""{shaderPath}""
{{
    Properties
    {{
{properties}    }}
    SubShader
    {{
        Tags {{ ""RenderType""=""{surfaceTag}"" ""Queue""=""{queueTag}"" ""RenderPipeline""=""UniversalPipeline"" }}
        Pass
        {{
{blendLine}{zWriteLine}
{zTestLine}
{cullLine}
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _ADDITIONAL_LIGHTS

{BuildSharedHlslIncludeBlock()}

            struct Attributes
            {{
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            }};

            struct Varyings
            {{
{varyings}            }};

{BuildTextureDeclarations()}
{BuildMaterialDeclarations()}

            Varyings vert(Attributes input)
            {{
                Varyings output;
                LC_SETUP_INSTANCE_ID(input);
                LC_TRANSFER_INSTANCE_ID(input, output);
                LC_INIT_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs positionInputs = LC_GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = LC_GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.positionHCS = positionInputs.positionCS;
{vertexBody}                return output;
            }}

            half4 frag(Varyings input) : SV_Target
            {{
                LC_SETUP_INSTANCE_ID(input);
                LC_SETUP_STEREO_EYE_INDEX(input);
{fragmentBody}
            }}
            ENDHLSL
        }}
    }}
}}";
    }

    private string BuildComputeShaderSource()
    {
        return
@"#pragma kernel CSMain

RWTexture2D<float4> Result;

[numthreads(8, 8, 1)]
void CSMain(uint3 id : SV_DispatchThreadID)
{
    Result[id.xy] = float4(1, 1, 1, 1);
}";
    }

    private string BuildTextureDeclarations()
    {
        StringBuilder builder = new StringBuilder();

        if (useMainTexture)
        {
            builder.AppendLine("            TEXTURE2D(_BaseMap);");
            builder.AppendLine("            SAMPLER(sampler_BaseMap);");
        }

        if (useNormalMap)
        {
            builder.AppendLine("            TEXTURE2D(_NormalMap);");
            builder.AppendLine("            SAMPLER(sampler_NormalMap);");
        }

        if (useMaskMap)
        {
            builder.AppendLine("            TEXTURE2D(_MaskMap);");
            builder.AppendLine("            SAMPLER(sampler_MaskMap);");
        }

        if (surfaceWorkflow == SurfaceWorkflow.Metallic && useMetallicMap)
        {
            builder.AppendLine("            TEXTURE2D(_MetallicMap);");
            builder.AppendLine("            SAMPLER(sampler_MetallicMap);");
        }

        if (surfaceWorkflow == SurfaceWorkflow.Specular && useSpecularMap)
        {
            builder.AppendLine("            TEXTURE2D(_SpecGlossMap);");
            builder.AppendLine("            SAMPLER(sampler_SpecGlossMap);");
        }

        if (useRoughnessMap)
        {
            builder.AppendLine("            TEXTURE2D(_RoughnessMap);");
            builder.AppendLine("            SAMPLER(sampler_RoughnessMap);");
        }

        if (useOcclusionMap)
        {
            builder.AppendLine("            TEXTURE2D(_OcclusionMap);");
            builder.AppendLine("            SAMPLER(sampler_OcclusionMap);");
        }

        if (useEmissionMap)
        {
            builder.AppendLine("            TEXTURE2D(_EmissionMap);");
            builder.AppendLine("            SAMPLER(sampler_EmissionMap);");
        }

        if (useHeightMap)
        {
            builder.AppendLine("            TEXTURE2D(_HeightMap);");
            builder.AppendLine("            SAMPLER(sampler_HeightMap);");
        }

        return builder.ToString();
    }

    private string BuildMaterialDeclarations()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("            CBUFFER_START(UnityPerMaterial)");

        if (useMainTexture)
        {
            builder.AppendLine("                float4 _BaseMap_ST;");
        }

        if (useTintColor)
        {
            builder.AppendLine("                half4 _BaseColor;");
        }

        if (useNormalMap)
        {
            builder.AppendLine("                half _NormalScale;");
        }

        if (surfaceWorkflow == SurfaceWorkflow.Metallic)
        {
            builder.AppendLine("                half _Metallic;");
        }

        if (surfaceWorkflow == SurfaceWorkflow.Specular)
        {
            builder.AppendLine("                half4 _SpecColor;");
        }

        if (useRoughnessMap)
        {
            builder.AppendLine("                half _Roughness;");
        }

        if (useOcclusionMap)
        {
            builder.AppendLine("                half _OcclusionStrength;");
        }

        if (useEmissionMap)
        {
            builder.AppendLine("                half4 _EmissionColor;");
            builder.AppendLine("                half _EmissionIntensity;");
        }

        if (useHeightMap)
        {
            builder.AppendLine("                half _HeightStrength;");
        }

        if (enableAlphaClip)
        {
            builder.AppendLine("                half _Cutoff;");
        }

        builder.AppendLine("            CBUFFER_END");
        return builder.ToString();
    }

    private string BuildBlendCommand()
    {
        if (surfaceType != SurfaceType.Transparent)
        {
            return string.Empty;
        }

        switch (blendMode)
        {
            case BlendMode.Additive:
                return "            Blend One One\n";
            case BlendMode.Premultiply:
                return "            Blend One OneMinusSrcAlpha\n";
            case BlendMode.Multiply:
                return "            Blend DstColor Zero\n";
            default:
                return "            Blend SrcAlpha OneMinusSrcAlpha\n";
        }
    }

    private string ToShaderValue(DepthTestMode value)
    {
        switch (value)
        {
            case DepthTestMode.Less:
                return "Less";
            case DepthTestMode.Greater:
                return "Greater";
            case DepthTestMode.GreaterEqual:
                return "GEqual";
            case DepthTestMode.Equal:
                return "Equal";
            case DepthTestMode.NotEqual:
                return "NotEqual";
            case DepthTestMode.Always:
                return "Always";
            default:
                return "LEqual";
        }
    }

    private string ToShaderValue(CullMode value)
    {
        switch (value)
        {
            case CullMode.Front:
                return "Front";
            case CullMode.Off:
                return "Off";
            default:
                return "Back";
        }
    }
}

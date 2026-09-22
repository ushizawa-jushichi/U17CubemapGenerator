Shader "Uchuhikoshi/U17CubemapGenerator/PreviewURP"
{
    Properties
    {
        [MainTexture][NoScaleOffset] _MainTex("MainTex", Cube) = "white" {}
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
    }

    HLSLINCLUDE
    #pragma target 2.0
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Background"
        }
        LOD 100

        Blend One Zero
        BlendOp Add
        ZWrite On
        ZTest LEqual
        Cull [_Cull]

        Pass
        {
            Name "PreviewCubeUnlit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma shader_feature _ SKYBOX_ON
            #pragma shader_feature _ ROTATE_ON

            TEXTURECUBE (_MainTex);
            SAMPLER (sampler_MainTex);
            uniform float4x4 _PreviewRotationMatrix;

            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionOS = input.positionOS.xyz;
                output.positionCS = TransformWorldToHClip(output.positionWS);

                float3 normalOS = normalize(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(normalOS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                #if defined(SKYBOX_ON)
                float3 reflectDir = normalize(input.positionOS);
                #else
                float3 viewDir = normalize(input.positionWS - _WorldSpaceCameraPos);
                float3 reflectDir = reflect(viewDir, input.normalWS);
                #endif

                #if defined(ROTATE_ON)
                reflectDir = mul(reflectDir, (float3x3)_PreviewRotationMatrix);
                #endif

                float4 color = SAMPLE_TEXTURECUBE(_MainTex, sampler_MainTex, reflectDir);
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);

                return output;
            }

            half frag(Varyings input) : SV_Target
            {
                return input.positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);

                float3 normalOS = normalize(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(normalOS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 normalWS = NormalizeNormalPerPixel(input.normalWS);
                return half4(normalWS, 0.0);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

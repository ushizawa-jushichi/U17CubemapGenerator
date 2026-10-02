Shader "Uchuhikoshi/U17CubemapGenerator/PreviewHDRP"
{
    Properties
    {
        [MainTexture][NoScaleOffset] _MainTex("MainTex", Cube) = "white" {}
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
    }

    HLSLINCLUDE
    #pragma target 3.0
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

    CBUFFER_START(UnityPerDraw)
        float4x4 unity_ObjectToWorld;
        float4x4 unity_WorldToObject;
        float4x4 unity_MatrixPreviousM;
        float4x4 unity_MatrixPreviousMI;
        float4 unity_LODFade;
        real4 unity_WorldTransformParams;
    CBUFFER_END

    float4x4 unity_MatrixVP;
    float4x4 unity_MatrixV;
    float4x4 unity_MatrixInvV;
    float4x4 glstate_matrix_projection;
    float4x4 unity_MatrixInvP;
    float4x4 unity_MatrixInvVP;
    float3 _WorldSpaceCameraPos;

    #define UNITY_MATRIX_M unity_ObjectToWorld
    #define UNITY_MATRIX_I_M unity_WorldToObject
    #define UNITY_PREV_MATRIX_M unity_MatrixPreviousM
    #define UNITY_PREV_MATRIX_I_M unity_MatrixPreviousMI
    #define UNITY_MATRIX_V unity_MatrixV
    #define UNITY_MATRIX_I_V unity_MatrixInvV
    #define UNITY_MATRIX_P glstate_matrix_projection
    #define UNITY_MATRIX_I_P unity_MatrixInvP
    #define UNITY_MATRIX_VP unity_MatrixVP
    #define UNITY_MATRIX_I_VP unity_MatrixInvVP

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "HDRenderPipeline"
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
            Name "PreviewCubeUnlitHD"
            Tags
            {
                "LightMode" = "HDUnlitShader"
            }

            HLSLPROGRAM
            #pragma shader_feature _ SKYBOX_ON
            #pragma shader_feature _ ROTATE_ON

            TEXTURECUBE(_MainTex);
            SAMPLER(sampler_MainTex);
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
            Name "PreviewCubeUnlitFallback"
            Tags
            {
                "LightMode" = "SRPDefaultUnlit"
            }

            HLSLPROGRAM
            #pragma shader_feature _ SKYBOX_ON
            #pragma shader_feature _ ROTATE_ON

            TEXTURECUBE(_MainTex);
            SAMPLER(sampler_MainTex);
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
    }
    FallBack Off
}

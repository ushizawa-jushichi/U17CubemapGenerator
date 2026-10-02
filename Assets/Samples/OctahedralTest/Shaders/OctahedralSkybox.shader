Shader "Sandbox/OctahedralSkybox"
{
    Properties
    {
        [MainTexture] _MainTex ("Octahedral Texture 2D", 2D) = "white" {}
        [Toggle] _YUp ("Y-Up Pole Alignment", Float) = 0
        _Exposure ("Exposure", Range(0, 8)) = 1.0
        _Rotation ("Rotation", Range(0, 360)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "PreviewType" = "Skybox"
        }

        Cull Off
        ZWrite Off

        // Pass 0: Universal / Default (URP, Built-in, Inspector Preview)
        Pass
        {
            Name "OctahedralSkyboxMain"
            Tags { "LightMode" = "Always" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            Texture2D _MainTex;
            SamplerState sampler_MainTex;

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _YUp;
                float _Exposure;
                float _Rotation;
            CBUFFER_END

            float2 DirectionToOctahedralUV(float3 dir, float yUp)
            {
                float3 n = normalize(dir);
                if (yUp > 0.5)
                {
                    n = float3(n.x, n.z, n.y);
                }

                n /= (abs(n.x) + abs(n.y) + abs(n.z));
                float2 uv = n.xy;
                if (n.z < 0.0)
                {
                    float2 s = float2(n.x >= 0.0 ? 1.0 : -1.0, n.y >= 0.0 ? 1.0 : -1.0);
                    uv = (1.0 - abs(n.yx)) * s;
                }
                return uv * 0.5 + 0.5;
            }

            float3 RotateY(float3 dir, float deg)
            {
                float rad = deg * (3.14159265359 / 180.0);
                float s = sin(rad);
                float c = cos(rad);
                return float3(c * dir.x + s * dir.z, dir.y, -s * dir.x + c * dir.z);
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 dirOS : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.dirOS = input.positionOS.xyz;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 viewDir = normalize(input.dirOS);
                viewDir = RotateY(viewDir, _Rotation);

                float2 uv = DirectionToOctahedralUV(viewDir, _YUp);
                half4 col = _MainTex.Sample(sampler_MainTex, uv);
                col.rgb *= _Exposure;
                return col;
            }
            ENDHLSL
        }
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "RenderPipeline" = "HDRenderPipeline"
            "PreviewType" = "Skybox"
        }

        Cull Off
        ZWrite Off

        Pass
        {
            Name "OctahedralSkyboxHDRP"
            Tags { "LightMode" = "HDUnlitShader" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

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

            Texture2D _MainTex;
            SamplerState sampler_MainTex;

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _YUp;
                float _Exposure;
                float _Rotation;
            CBUFFER_END

            float2 DirectionToOctahedralUV(float3 dir, float yUp)
            {
                float3 n = normalize(dir);
                if (yUp > 0.5)
                {
                    n = float3(n.x, n.z, n.y);
                }

                n /= (abs(n.x) + abs(n.y) + abs(n.z));
                float2 uv = n.xy;
                if (n.z < 0.0)
                {
                    float2 s = float2(n.x >= 0.0 ? 1.0 : -1.0, n.y >= 0.0 ? 1.0 : -1.0);
                    uv = (1.0 - abs(n.yx)) * s;
                }
                return uv * 0.5 + 0.5;
            }

            float3 RotateY(float3 dir, float deg)
            {
                float rad = deg * (3.14159265359 / 180.0);
                float s = sin(rad);
                float c = cos(rad);
                return float3(c * dir.x + s * dir.z, dir.y, -s * dir.x + c * dir.z);
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 dirOS : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.dirOS = input.positionOS.xyz;
                output.positionCS = TransformWorldToHClip(TransformObjectToWorld(input.positionOS.xyz));
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 viewDir = normalize(input.dirOS);
                viewDir = RotateY(viewDir, _Rotation);

                float2 uv = DirectionToOctahedralUV(viewDir, _YUp);
                half4 col = _MainTex.Sample(sampler_MainTex, uv);
                col.rgb *= _Exposure;
                return col;
            }
            ENDHLSL
        }
    }

    Fallback "Skybox/Cubemap"
}

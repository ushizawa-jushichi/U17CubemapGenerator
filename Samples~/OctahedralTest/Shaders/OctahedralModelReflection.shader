Shader "Sandbox/OctahedralModelReflection"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        [MainTexture] _OctahedralTex ("Octahedral Environment Map (2D)", 2D) = "white" {}
        [Toggle] _YUp ("Y-Up Pole Alignment", Float) = 0
        _Metallic ("Metallic / Reflectivity", Range(0, 1)) = 1.0
        _Exposure ("Environment Exposure", Range(0, 8)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "OctahedralModelReflectionURP"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            Texture2D _OctahedralTex;
            SamplerState sampler_OctahedralTex;

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _OctahedralTex_ST;
                float _YUp;
                float _Metallic;
                float _Exposure;
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

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 viewDirWS = normalize(input.positionWS - _WorldSpaceCameraPos);
                float3 normalWS = normalize(input.normalWS);
                float3 reflectWS = reflect(viewDirWS, normalWS);

                float2 octUV = DirectionToOctahedralUV(reflectWS, _YUp);
                half4 envColor = _OctahedralTex.Sample(sampler_OctahedralTex, octUV);
                envColor.rgb *= _Exposure;

                half3 finalColor = lerp(_BaseColor.rgb, envColor.rgb * _BaseColor.rgb, _Metallic);
                return half4(finalColor, _BaseColor.a);
            }
            ENDHLSL
        }
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "HDRenderPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "OctahedralModelReflectionHDRP"
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

            Texture2D _OctahedralTex;
            SamplerState sampler_OctahedralTex;

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _OctahedralTex_ST;
                float _YUp;
                float _Metallic;
                float _Exposure;
            CBUFFER_END

            float3 _WorldSpaceCameraPos;

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

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 viewDirWS = normalize(input.positionWS - _WorldSpaceCameraPos);
                float3 normalWS = normalize(input.normalWS);
                float3 reflectWS = reflect(viewDirWS, normalWS);

                float2 octUV = DirectionToOctahedralUV(reflectWS, _YUp);
                half4 envColor = _OctahedralTex.Sample(sampler_OctahedralTex, octUV);
                envColor.rgb *= _Exposure;

                half3 finalColor = lerp(_BaseColor.rgb, envColor.rgb * _BaseColor.rgb, _Metallic);
                return half4(finalColor, _BaseColor.a);
            }
            ENDHLSL
        }
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "OctahedralModelReflectionBuiltIn"

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _BaseColor;
            sampler2D _OctahedralTex;
            float _YUp;
            float _Metallic;
            float _Exposure;

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

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 viewDirWS = normalize(i.worldPos - _WorldSpaceCameraPos);
                float3 normalWS = normalize(i.worldNormal);
                float3 reflectWS = reflect(viewDirWS, normalWS);

                float2 octUV = DirectionToOctahedralUV(reflectWS, _YUp);
                fixed4 envColor = tex2D(_OctahedralTex, octUV);
                envColor.rgb *= _Exposure;

                fixed3 finalColor = lerp(_BaseColor.rgb, envColor.rgb * _BaseColor.rgb, _Metallic);
                return fixed4(finalColor, _BaseColor.a);
            }
            ENDCG
        }
    }
    Fallback "Diffused"
}

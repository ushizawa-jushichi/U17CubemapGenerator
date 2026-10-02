Shader "Uchuhikoshi/U17CubemapGenerator/BlitterURP"
{
    Properties
    {
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
        [HideInInspector] _MainTex_ST_("_MainTex_ST_", Vector) = (0,0,1,1)
        _CubeTex("CubeTex", Cube) = "white" {}
        _RotationY("RotationY", Float) = 1
        _FaceIndex("FaceIndex", Int) = 0
        _Scale("Scale", Vector) = (1,1,0,0)
        _Offset("Offset", Vector) = (0,0,0,0)
        _YUp("YUp", Float) = 0
    }

    HLSLINCLUDE
    #pragma target 2.0
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"
        }
        Cull Off
        ZWrite Off
        ZTest Always

        // 0: MainTex
        Pass
        {
            HLSLPROGRAM
            TEXTURE2D (_MainTex);
            SAMPLER (sampler_MainTex);
            float4 _MainTex_ST_;

            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);

                output.uv = input.uv * _MainTex_ST_.xy + _MainTex_ST_.zw;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv.xy);
            }
            ENDHLSL
        }

        // 1: Equirectangular panorama
        Pass
        {
            HLSLPROGRAM
            float _RotationY;
            TEXTURECUBE (_CubeTex);
            SAMPLER (sampler_CubeTex);

            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = (input.uv - 0.5) * float2(PI * 2.0, PI) + float2(_RotationY, 0.0);

                return output;
            }

            half4 frag(Varyings input) : SV_TARGET
            {
                float cy = cos(input.uv.y);
                float4 color = SAMPLE_TEXTURECUBE(_CubeTex, sampler_CubeTex,
                                                  float3(sin(input.uv.x) * cy, sin(input.uv.y),
                                                         cos(input.uv.x) * cy));
                return color;
            }
            ENDHLSL
        }

        // 2: Matcap
        Pass
        {
            HLSLPROGRAM
            TEXTURECUBE (_CubeTex);
            SAMPLER (sampler_CubeTex);
            float _FillOutside;

            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;

                return output;
            }

            half4 frag(Varyings input) : SV_TARGET
            {
                float2 coord = input.uv * 2.0 - 1.0;
                float r2 = dot(coord, coord);

                if (r2 <= 1.0)
                {
                    float z = sqrt(max(0.0, 1.0 - r2));
                    float3 dir = float3(2.0 * coord.x * z, 2.0 * coord.y * z, 2.0 * z * z - 1.0);
                    return SAMPLE_TEXTURECUBE(_CubeTex, sampler_CubeTex, dir);
                }
                else if (_FillOutside > 0.5)
                {
                    float2 dir2D = normalize(coord);
                    float edgeZ = 0.05;
                    float edgeR = sqrt(1.0 - edgeZ * edgeZ);
                    float3 dir = float3(2.0 * dir2D.x * edgeR * edgeZ, 2.0 * dir2D.y * edgeR * edgeZ,
                                        2.0 * edgeZ * edgeZ - 1.0);
                    return half4(SAMPLE_TEXTURECUBE(_CubeTex, sampler_CubeTex, dir).rgb, 1.0);
                }

                return half4(0.0, 0.0, 0.0, 0.0);
            }
            ENDHLSL
        }

        // 3: Cubemap Face to 2D RenderTexture (Supports Crunch / Compressed formats)
        Pass
        {
            HLSLPROGRAM
            TEXTURECUBE (_CubeTex);
            SAMPLER (sampler_CubeTex);
            int _FaceIndex;
            float2 _Scale;
            float2 _Offset;

            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_TARGET
            {
                float2 uv = (input.uv * _Scale.xy + _Offset.xy) * 2.0 - 1.0;
                uv.y = -uv.y;
                float3 dir = float3(0, 0, 0);
                if (_FaceIndex == 0) dir = float3(1.0, uv.y, -uv.x); // +X
                else if (_FaceIndex == 1) dir = float3(-1.0, uv.y, uv.x); // -X
                else if (_FaceIndex == 2) dir = float3(uv.x, 1.0, -uv.y); // +Y
                else if (_FaceIndex == 3) dir = float3(uv.x, -1.0, uv.y); // -Y
                else if (_FaceIndex == 4) dir = float3(uv.x, uv.y, 1.0); // +Z
                else if (_FaceIndex == 5) dir = float3(-uv.x, uv.y, -1.0); // -Z
                return SAMPLE_TEXTURECUBE(_CubeTex, sampler_CubeTex, normalize(dir));
            }
            ENDHLSL
        }

        // 4: Octahedral panorama / map
        Pass
        {
            HLSLPROGRAM
            float _RotationY;
            float _YUp;
            TEXTURECUBE (_CubeTex);
            SAMPLER (sampler_CubeTex);

            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;

                return output;
            }

            half4 frag(Varyings input) : SV_TARGET
            {
                float2 p = input.uv * 2.0 - 1.0;
                float3 dir = float3(p.x, p.y, 1.0 - abs(p.x) - abs(p.y));
                if (dir.z < 0.0)
                {
                    dir.xy = (1.0 - abs(dir.yx)) * (p >= 0.0 ? 1.0 : -1.0);
                }
                dir = normalize(dir);

                if (_YUp > 0.5)
                {
                    dir = float3(dir.x, dir.z, dir.y);
                }

                float s = sin(_RotationY);
                float c = cos(_RotationY);
                dir = float3(c * dir.x + s * dir.z, dir.y, -s * dir.x + c * dir.z);

                return SAMPLE_TEXTURECUBE(_CubeTex, sampler_CubeTex, dir);
            }
            ENDHLSL
        }
    }
    Fallback Off
}

Shader "Ushino17/U17CubemapGenerator/BlitterHDRP"
{
	Properties
	{
		[HideInInspector] _MainTex ("Texture", 2D) = "white" {}
		[HideInInspector] _XMainTex ("XTexture", 2D) = "white" {}
		[HideInInspector] _MainTex_ST_("_MainTex_ST_", Vector) = (0,0,1,1)
		_CubeTex("CubeTex", Cube) = "white" {}
		_RotationY("RotationY", Float) = 1
	}

	HLSLINCLUDE
	#pragma target 4.5
	#pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
	#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

	ENDHLSL

	SubShader
	{
		Tags{ "RenderPipeline" = "HDRenderPipeline" }

		// 0: MainTex
		Pass
		{
			HLSLPROGRAM

			TEXTURE2D(_MainTex);	SAMPLER(sampler_MainTex);
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

		// 1: Equirectanglar panorama
		Pass
		{
			HLSLPROGRAM

			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

			float _RotationY;
			TEXTURECUBE(_CubeTex);        SAMPLER(sampler_CubeTex);

			#pragma vertex vert
			#pragma fragment frag

			Varyings vert(Attributes input)
			{
				Varyings output;
				output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
				float2 uv  = GetFullScreenTriangleTexCoord(input.vertexID);
				uv = (uv - 0.5) * float2(PI * 2, PI/2 * 2) + float2(_RotationY, 0);
				output.texcoord = uv;
				return output;
			}

			half4 frag(Varyings input) : SV_TARGET
			{
				float cy = cos(input.texcoord.y);
				float4 color = SAMPLE_TEXTURECUBE(_CubeTex, sampler_CubeTex, float3(sin(input.texcoord.x) * cy, sin(input.texcoord.y), cos(input.texcoord.x) * cy));
				return color;
			}
			ENDHLSL
		}

	}
	Fallback Off
}
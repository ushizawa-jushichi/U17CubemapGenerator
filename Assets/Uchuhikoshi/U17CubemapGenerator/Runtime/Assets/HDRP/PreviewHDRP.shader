Shader "Uchuhikoshi/U17CubemapGenerator/PreviewHDRP"
{
	Properties
	{
		[MainTexture] _MainTex("MainTex", Cube) = "white" {}
	}

	HLSLINCLUDE
	#pragma target 4.5
	#pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
	#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
	ENDHLSL

	SubShader
	{
		Tags
		{
			"RenderPipeline" = "HDRenderPipeline"
			"RenderType" = "HDUnlitShader"
		}
		LOD 100

		Blend One Zero
		BlendOp Add
		ZWrite On
		ZTest LEqual
		Cull Back

		Pass
		{
			Name "PreviewCubeUnlit"
			Tags
			{
				"LightMode" = "ForwardOnly"
			}

			HLSLPROGRAM

			#pragma shader_feature _ SKYBOX_ON

			TEXTURECUBE(_MainTex);	SAMPLER(sampler_MainTex);
			uniform float4x4 _PreviewRotationMatrix;

			#pragma vertex vert
			#pragma fragment frag

			struct Attributes
			{
				float4 positionOS	: POSITION;
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

			float3 RotateY(float3 vec, float angle)
            {
                float sinY = sin(angle);
                float cosY = cos(angle);
                float3x3 rotY = float3x3(
                    float3(cosY, 0, -sinY),
                    float3(0, 1, 0),
                    float3(sinY, 0, cosY));
				return mul(vec, rotY);
            }

			half4 frag(Varyings input) : SV_Target
			{
#if defined(SKYBOX_ON)
				float3 vec = normalize(input.positionOS);
				float4 color = SAMPLE_TEXTURECUBE( _MainTex, sampler_MainTex, vec);
				return color;
#else
				float3 viewDir = normalize(_WorldSpaceCameraPos - input.positionWS);
				float3 reflectDir = reflect(-viewDir, input.normalWS);
				reflectDir = mul(reflectDir, (float3x3)_PreviewRotationMatrix);

				reflectDir = RotateY(reflectDir, PI); // rotate 180deg
                reflectDir.x *= -1;

				float4 color = SAMPLE_TEXTURECUBE( _MainTex, sampler_MainTex, reflectDir);
				return color;
#endif
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
				float4 positionOS	: POSITION;
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
				float4 positionOS	: POSITION;
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
	            half3 normalWS = SafeNormalize(input.normalWS);
				return half4(normalWS, 0.0);
			}

			ENDHLSL
		}
	}
	FallBack "Hidden/HDRP/FallbackError"
}
Shader "Ushino17/U17CubemapGenerator/PreviewBuiltin"
{
	Properties
	{
		[MainTexture] _MainTex("MainTex", Cube) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 positionOS : POSITION;
			};

			struct v2f
			{
				float4 positionCS : SV_POSITION;
				float3 positionOS : TEXCOORD0;
				float3 positionWS : TEXCOORD1;
				float3 normalWS : TEXCOORD2;
			};

			UNITY_DECLARE_TEXCUBE(_MainTex);
			uniform float4x4 _PreviewRotationMatrix;
			#pragma shader_feature _ SKYBOX_ON

			v2f vert (appdata input)
			{
				v2f output;
				output.positionCS = UnityObjectToClipPos(input.positionOS);
				output.positionOS = input.positionOS.xyz;
				output.positionWS = mul(unity_ObjectToWorld, input.positionOS.xyz).xyz;

				float3 normalOS = normalize(input.positionOS.xyz);
				output.normalWS = mul(normalOS, (float3x3)unity_ObjectToWorld);

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

			fixed4 frag (v2f input) : SV_Target
			{
#if defined(SKYBOX_ON)
				float3 vec = normalize(input.positionOS);
				fixed4 color = UNITY_SAMPLE_TEXCUBE(_MainTex, vec);
				return color;
#else
				float3 viewDir = normalize(_WorldSpaceCameraPos - input.positionWS);
				float3 reflectDir = reflect(-viewDir, input.normalWS);
				reflectDir = mul(reflectDir, (float3x3)_PreviewRotationMatrix);

				reflectDir = RotateY(reflectDir, UNITY_PI); // rotate 180deg
                reflectDir.x *= -1;

				fixed4 color = UNITY_SAMPLE_TEXCUBE(_MainTex, reflectDir);
				return color;
#endif
			}
			ENDCG
		}
	}
}

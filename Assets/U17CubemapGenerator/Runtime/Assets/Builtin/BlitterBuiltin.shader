Shader "Uchuhikoshi/U17CubemapGenerator/BlitterBuiltin"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
		_CubeTex("CubeTex", Cube) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

		// 0: MainTex
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST_;

            v2f vert (appdata input)
            {
                v2f output;
                output.positionCS = UnityObjectToClipPos(input.positionOS);
				output.uv = input.uv * _MainTex_ST_.xy + _MainTex_ST_.zw;
                return output;
            }

            fixed4 frag (v2f input) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, input.uv);
                return col;
            }
            ENDCG
        }

		// 1: Equirectanglar panorama
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 texcoord : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

			float _RotationY;
			UNITY_DECLARE_TEXCUBE(_CubeTex);

            v2f vert (appdata input)
            {
                v2f output;
                output.positionCS = UnityObjectToClipPos(input.positionOS);
				float2 uv  = input.uv;
				uv = (uv - 0.5) * float2(UNITY_PI * 2, UNITY_PI/2 * 2) + float2(_RotationY, 0);
				output.texcoord = uv;

				return output;
            }

            fixed4 frag (v2f input) : SV_Target
            {
				float cy = cos(input.texcoord.y);
				float4 color = UNITY_SAMPLE_TEXCUBE(_CubeTex, float3(sin(input.texcoord.x) * cy, sin(input.texcoord.y), cos(input.texcoord.x) * cy));
				return color;
            }
            ENDCG
        }
    }
}

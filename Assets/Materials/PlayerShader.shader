// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/PlayerShader"
{
	Properties
	{
		_Color ("Color", Color) = (1,1,1,1)
	}
	SubShader
	{

		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			Name "BASE"

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				half3 colMod : TEXCOORD0;
				float4 vertex : SV_POSITION;

			};

			float4 _Color;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				
				float3 norm = UnityObjectToWorldDir(normalize(v.normal));
				float3 viewDir = UNITY_MATRIX_IT_MV[2].xyz;

				o.colMod = float3(1.0f - dot(viewDir,norm), 0,0);

				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = lerp(fixed4(1,1,1,1), _Color, i.colMod.x * 0.8);
				return col;
			}
			ENDCG
		}
	}
	
}

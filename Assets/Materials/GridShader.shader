Shader "Unlit/GridShader"
{
	Properties
	{
	}
	SubShader
	{
		Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
		LOD 100

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float4 col : COLOR;
			};

			struct v2f
			{
				fixed4 col : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.col = v.col;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				clip(i.col);
				return i.col;
			}
			ENDCG
		}
	}
}

Shader "BlackShamrock/Runequest/GravityFieldShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		
		_PointCount("Point Count", int) = 0
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Transparent+1"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}

		Blend SrcAlpha OneMinusSrcAlpha
		Cull Off
		Lighting Off
		ZWrite On
		ZTest LEqual
		Fog { Mode Off }

		Pass
		{
			CGPROGRAM

			#pragma target 4.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_particles
			#pragma multi_compile_fog

			#include "UnityCG.cginc"

			struct appdata_t
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				fixed4 color : COLOR;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				UNITY_FOG_COORDS(1)
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			uniform float4 _Points[20];

			uniform int _PointCount;

			v2f vert(appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = v.color;
				UNITY_TRANSFER_FOG(o, o.vertex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float4 col = tex2D(_MainTex, i.uv);

				UNITY_APPLY_FOG_COLOR(i.fogCoord, col, (fixed4)1);

				float2 texPos = float2(i.uv.x, i.uv.y);

				// G = 667.4f;

				for (int j = 0; j < 2; j++)//
				{
					float4 data = _Points[j];

					float2 attractorPos = float2(data.x, data.y);
					float attractorMass = data.z;

					float2 difference = attractorPos - texPos;
					
					float forceMagnitude = (667.4 * attractorMass) / dot(difference, difference);

					float2 result = normalize(difference) * forceMagnitude;

					col += float4(result.x, result.y, 0, 1);
				}

				return col;
			}

			ENDCG
		}
	}
}
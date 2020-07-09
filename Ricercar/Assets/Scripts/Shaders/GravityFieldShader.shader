Shader "Ricercar/GravityFieldShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_GravityFieldOutputTexture("Texture", 2D) = "white" {}

		_EffectScalar("Effect Scalar", float) = 1
		_GridScale("Grid Scale", float) = 0.2
		[Toggle(IS_DISTORTION_MAP)] _IsDistortionMap("Is Distortion Map", float) = 0

		_GravityAuraSize("Gravity Aura Size", float) = 1
		_PositiveGravityColour("Positive Gravity Colour", Color) = (1, 1, 1, 1)
		_NegativeGravityColour("Negative Gravity Colour", Color) = (1, 1, 1, 1)
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

			#pragma shader_feature IS_DISTORTION_MAP

			#include "UnityCG.cginc"
			
			sampler2D _MainTex;
			float4 _MainTex_ST;

			sampler2D _GravityFieldOutputTexture;

			uniform float _EffectScalar;	// how much to exaggerate the effect being shown
			uniform float _GridScale; // a scale for the texture being sampled

			float4 _PositiveGravityColour;
			float4 _NegativeGravityColour;

			float _GravityAuraSize;

			float fwidthSmooth(float value)
			{
				float ddxRes = ddx(value);
				float ddyRes = ddy(value);

				return sqrt(ddxRes * ddxRes + ddyRes * ddyRes);
			}

			float4 fwidthSmooth(float4 value)
			{
				return float4(fwidthSmooth(value.x), fwidthSmooth(value.y), fwidthSmooth(value.z), fwidthSmooth(value.w));
			}
			
			float2 fwidthSmooth(float2 value)
			{
				return float2(fwidthSmooth(value.x), fwidthSmooth(value.y));
			}

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
				float3 worldPos : TEXCOORD1;
				UNITY_FOG_COORDS(1)
			};

			v2f vert(appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.worldPos = mul (unity_ObjectToWorld, v.vertex);
				o.color = v.color;
				UNITY_TRANSFER_FOG(o, o.vertex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float4 gravityData = tex2D(_GravityFieldOutputTexture, i.uv);

				float ddxIn = ddx(gravityData.x);
				float ddyIn = ddy(gravityData.y);
				float div = sign(ddxIn + ddyIn);
				float sum = ddxIn + ddyIn;

				//float4 something = div * float4(ddxIn, ddyIn, 0, 1);

				float4 col = float4(1, 1, 1, 1);

				float gravityAuraColourLerp = saturate(abs(sum * _GravityAuraSize));

				if (sum > 0)
					col = lerp(col, _PositiveGravityColour, gravityAuraColourLerp);
				else 
					col = lerp(col, _NegativeGravityColour, gravityAuraColourLerp);
					
				float2 gravity = float2(gravityData.x, gravityData.y) * _EffectScalar;

			#ifdef IS_DISTORTION_MAP

				float2 distortedUVs = (float2(i.worldPos.x, i.worldPos.y) - gravity) / _GridScale;

				float4 sampleCol = tex2D(_MainTex, distortedUVs);

				return col * sampleCol * i.color;

			#else
				return float4(gravity, 0, 1) * i.color;
			#endif
			}

			ENDCG
		}
	}
}
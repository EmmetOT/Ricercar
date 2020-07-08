Shader "Ricercar/GravityFieldShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_GravityFieldOutputTexture("Texture", 2D) = "white" {}
		
		_FieldSize("Field Size", int) = 0
		_EffectScalar("Effect Scalar", float) = 1
		_GridScale("Grid Scale", float) = 0.2
		[Toggle(IS_DISTORTION_MAP)] _IsDistortionMap("Is Distortion Map", float) = 0
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

			uniform StructuredBuffer<float3> _Points;

			sampler2D _GravityFieldOutputTexture;
			float4 _GravityFieldOutputTexture_TexelSize;

			uniform int _FieldSize;
			uniform float _EffectScalar;
			uniform float _GridScale;

			inline float invLerp(float from, float to, float value) 
			{
				return (value - from) / (to - from);
			}
			
			inline float4 invLerp(float4 from, float4 to, float value)
			{
				return float4(invLerp(from.x, to.x, value), invLerp(from.y, to.y, value), invLerp(from.z, to.z, value), invLerp(from.w, to.w, value));
			}

			inline float4 invLerp(float3 from, float3 to, float value)
			{
				return float4(invLerp(from.x, to.x, value), invLerp(from.y, to.y, value), invLerp(from.z, to.z, value), 1);
			}

			float fwidth(float value){
			  return abs(ddx(value)) + abs(ddy(value));
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
				float2 texelSize = _GravityFieldOutputTexture_TexelSize * 3;

				float4 gravityData = tex2D(_GravityFieldOutputTexture, i.uv);

				float ddxIn = ddx(gravityData.x);
				float ddyIn = ddy(gravityData.y);
				float sum = ddxIn + ddyIn;

				float4 col = float4(1, 1, 1, 1) * 0.1;

				//if (abs(sum) > 0.1)
				//{
					if (sum > 0)
						col = lerp(col, float4(0, 0, 1, 1), min(1, abs(sum)));
					else 
						col = lerp(col, float4(1, 0, 0, 1), min(1, abs(sum)));
				//}

				float2 gravity = float2(gravityData.x, gravityData.y) * _EffectScalar;

			#ifdef IS_DISTORTION_MAP

				float2 distortedUVs = (float2(i.worldPos.x, i.worldPos.y) - gravity) / _GridScale;

				float4 sampleCol = tex2D(_MainTex, distortedUVs);

				//float4 tint = lerp(float4(1, 1, 1, 1), float4(0, 0, 1, 1), positiveTowardsiness);
				//tint = lerp(tint, float4(1, 0, 0, 1), negativeTowardsiness);

				return col * sampleCol * i.color;

			#else
				return float4(gravity, 0, 1) * i.color;
			#endif
			}

			ENDCG
		}
	}
}
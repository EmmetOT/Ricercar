Shader "Ricercar/GravityFieldShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		
		_FieldSize("Field Size", int) = 0
		_ColourScale("Colour Scale", float) = 1
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

			uniform StructuredBuffer<float2> _Points;

			uniform int _FieldSize;
			uniform float _ColourScale;

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
                // sample the texture
                fixed4 texCol = tex2D(_MainTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, texCol);

				//return texCol;

				int fieldSizeMinus1 = _FieldSize - 1;

				int x0 = floor(i.uv.x * fieldSizeMinus1);
				int y0 = floor(i.uv.y * fieldSizeMinus1);

				int x1 = min(fieldSizeMinus1, x0 + 1);
				int y1 = min(fieldSizeMinus1, y0 + 1);
				
				// binomial interpolation

				float x_t = invLerp(x0, x1, i.uv.x * fieldSizeMinus1);
				float y_t = invLerp(y0, y1, i.uv.y * fieldSizeMinus1);
				
				float2 gravityBottomLeft = _Points[y0 * _FieldSize + x0];
				float2 gravityBottomRight = _Points[y0 * _FieldSize + x1];
				float2 gravityTopLeft = _Points[y1 * _FieldSize + x0];
				float2 gravityTopRight = _Points[y1 * _FieldSize + x1];

				float2 lerp_bottom = lerp(gravityBottomLeft, gravityBottomRight, x_t);
				float2 lerp_top = lerp(gravityTopLeft, gravityTopRight, x_t);
				
				float4 gravity = float4(lerp(lerp_bottom, lerp_top, y_t), 0, 1);

				float2 distortedUVs = float2(-gravity.x * 0.006 + i.uv.x, -gravity.y * 0.006 + i.uv.y);

				int xInt = floor(distortedUVs.x * 150);
				int yInt = floor(distortedUVs.y * 150);

				float4 finalCol = float4(0, 0, 0, 1);

				finalCol = lerp(finalCol, float4(1, 1, 1, 1), xInt % 10 == 0);
				finalCol = lerp(finalCol, float4(1, 1, 1, 1), yInt % 10 == 0);

				return finalCol;


			#ifdef IS_DISTORTION_MAP
				gravity *= _ColourScale;
				gravity.a = 1;
				
				return gravity;
			#else

				float left;
				float right;

				if (gravity.x < 0)
				{
					left = -gravity.x;
					right = 0;
				}
				else
				{
					left = 0;
					right = gravity.x;
				}

				float up;
				float down;

				if (gravity.y < 0)
				{
					down = -gravity.y;
					up = 0;
				}
				else
				{
					down = 0;
					up = gravity.y;
				}

				float4 result = float4(0, 0, 0, 1);

				left *= _ColourScale;
				up *= _ColourScale;
				down *= _ColourScale;
				right *= _ColourScale;

				result = lerp(result, float4(0, 0, 1, 1), left);
				result = lerp(result, float4(0, 1, 0, 1), up);
				result = lerp(result, float4(1, 0, 0, 1), down);
				result = lerp(result, float4(1, 1, 0, 1), right);

				return result;
			#endif
			}

			ENDCG
		}
	}
}
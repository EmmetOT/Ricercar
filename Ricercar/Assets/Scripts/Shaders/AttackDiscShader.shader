Shader "BlackShamrock/Runequest/AttackDiscShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_OutOfBoundsTex("Out of Bounds Texture", 2D) = "white" {}

		_ArcCount("Arc Count", int) = 0
		_CurrentArc("Current Arc", int) = 0
		_OutOfBoundsTextureScale("Out of Bounds Texture Scale", float) = 0

		_Colour("Background Colour", Color) = (0, 0, 0, 0)
		_OutOfBoundsColour("Out of Bounds Colour", Color) = (0, 0, 0, 0)

		_FilledAlpha("Filled Alpha", float) = 0.9
		_UnfilledAlpha("Unfilled Alpha", float) = 0.2

		_Fill("Fill", Range(0, 1)) = 0
		[MaterialToggle] _DrawChargeRings("Draw Charge Rings", float) = 0
		_ChargeRingCount("Charge Ring Count", float) = 0
		_ChargeRingWidth("Charge Ring Width", float) = 0.0075

		_MaskStartAngle("Mask Start Angle", float) = 0
		_MaskEndAngle("Mask End Angle", float) = 0
		_MaskEndPoint("Mask End Point", Vector) = (0, 0, 0, 0)
		_MaskRange("Mask Range", float) = 0.3

		_OuterMaskStartAngle("Outer Mask Start Angle", float) = 0
		_OuterMaskEndAngle("Outer Mask End Angle", float) = 0
		_OuterMaskStartPoint("Mask Start Point", Vector) = (0, 0, 0, 0)
		_OuterMaskEndPoint("Mask End Point", Vector) = (0, 0, 0, 0)
		_OuterMaskRangeScalar("Outer Mask Range Scalar", Range(0, 1)) = 0.8

		_OuterGradientSize("Outer Gradient Size", Range(0, 0.2)) = 0.05

		_BorderSize("Border Size", float) = 0.0075
		_InnerRingDist("Inner Ring Dist", Range(0, 1)) = 0.15
		_MaskStartPoint("Mask Start Point", Vector) = (0, 0, 0, 0)

		_DrawOrder("Draw Order", int) = 0

		_BoxMaskThickness("Box Mask Thickness", float) = 0.2
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

		// Pass 0 allows for drawing of the entire set of arcs. Angles passed in must be clamped to bearings.
		// Data is in the form:
		//
		// arc count vectors have data in the structure:
		// i + 0: (startAngle, endAngle, radius)
		// i + 1: position of far corner of start point
		// i + 2: position of far corner of end point
		//
		// colours are just... the colours of each arc.
		Pass
		{
			CGPROGRAM

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

			sampler2D _OutOfBoundsTex;
			uniform float _OutOfBoundsTextureScale;
			uniform fixed4 _OutOfBoundsColour;

			float4 _MainTex_ST;
			uniform float _BorderSize;

			uniform float4 _Arcs[60];
			uniform fixed4 _Colours[20];

			uniform float _FilledAlpha;
			uniform float _UnfilledAlpha;

			uniform int _ArcCount;
			uniform int _CurrentArc;

			uniform float _Fill;
			uniform float _InnerRingDist;
			uniform float _MaskRange;

			uniform float _ChargeRings[10];
			uniform int _ChargeRingCount;
			uniform float _ChargeRingWidth;

			uniform float _MaskStartAngle;
			uniform float _MaskEndAngle;

			v2f vert(appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = v.color;
				UNITY_TRANSFER_FOG(o, o.vertex);
				return o;
			}

			inline int Intersect(float one, float two, float three)
			{
				return (two > three && (one >= two || one <= three)) || (one >= two && one <= three);
			}

			inline bool IsIn(float4 pos, float start, float end)
			{
				return Intersect(atan2(pos.y - 0.5, pos.x - 0.5) * 57.295779, start, end);
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float4 col = tex2D(_MainTex, i.uv);

				UNITY_APPLY_FOG_COLOR(i.fogCoord, col, (fixed4)1);

				float x = i.uv.x;
				float y = i.uv.y;
				float dist = sqrt(pow((0.5 - x), 2) + pow((0.5 - y), 2)); // calculate the euclidean distance of this point from the centre

				float4 flippedPos = float4(y, x, 0, 0);

				float isInCone = IsIn(flippedPos, _MaskStartAngle, _MaskEndAngle);

				// this value tells us whether the current texel is in the 'current arc',
				// ie the arc being aimed into right now
				float4 currentArc = _Arcs[3 * _CurrentArc];
				float isInCurrentArcRange = ceil(currentArc.z - dist);
				float isCurrentArc = lerp(0, IsIn(flippedPos, currentArc.x, currentArc.y), isInCurrentArcRange);
				isCurrentArc = lerp(0, isCurrentArc, isInCone);

				// initial colours

				fixed4 fullDiscCol = fixed4(0, 0, 0, 0);

				// arc count vectors have data in the structure:
				// i + 0: (startAngle, endAngle, radius)
				// i + 1: position of far corner of start point
				// i + 2: position of far corner of end point

				float isInAnyArc = 0;

				for (int k = 0; k < _ArcCount; k++)
				{
					float4 arcData = _Arcs[3 * k];
					float startAngle = arcData.x;
					float endAngle = arcData.y;
					float radius = arcData.z;

					float2 startCorner = _Arcs[3 * k + 1];
					float2 endCorner = _Arcs[3 * k + 2];

					// this number will just tell you whether it's inside the range or not
					float isInArc = ceil(radius - dist);
					isInArc = lerp(0, isInArc, IsIn(flippedPos, startAngle, endAngle));

					fixed4 col = _Colours[k];
					col.a = lerp(col.a, _UnfilledAlpha, isInCone);

					isInAnyArc = max(isInAnyArc, isInArc);

					// now combine that with a check for whether we're between the start and end angle
					fullDiscCol += lerp(fixed4(0, 0, 0, 0), col, isInArc);
				}

				// now determine whether or not this texel is part of a 'charge ring'

				float halfChargeRingWidth = _ChargeRingWidth * 0.5;
				float isChargeRing = 0;

				for (int j = 0; j < _ChargeRingCount; j++)
				{
					float chargeRingDist = lerp(_InnerRingDist, _MaskRange, _ChargeRings[j]) * 0.5;
					float innerDist = chargeRingDist - halfChargeRingWidth;
					float outerDist = chargeRingDist + halfChargeRingWidth;

					// this whole line basically produces a positive value if the current texel is between the inner
					// and outer dist, or a 0 or negative otherwise. i'm then using max to combine it with the previous results
					isChargeRing = max(isChargeRing, ceil((outerDist - dist) * (dist - innerDist)));
				}

				isChargeRing = lerp(0, isChargeRing, isInCone);

				// next step is the fill, which again will only be on the current arc

				// modify fill slightly so that 0 is the distance of the inner ring, so it appears immediately,
				// as opposed to the centre of the circle.
				// halve it because we're drawing on a 1x1 texture (radius 0.5), but we want to be drawing with a circle of radius 1
				float fill = lerp(_InnerRingDist, _MaskRange, _Fill) * 0.5;

				float isFilled = ceil(fill - dist);
				isFilled = lerp(0, isFilled, isInCone);

				fixed4 initialColour = _Colours[_CurrentArc];

				fixed4 filled = initialColour;
				fixed4 unfilled = initialColour;

				filled.a = _FilledAlpha;
				unfilled.a = _UnfilledAlpha;

				filled = lerp(unfilled, filled, isFilled);

				// combine fill with charge ring

				fixed4 filledWithChargeRing = lerp(filled, initialColour, isChargeRing);

				fixed4 combinedFullDiscWithChargeRings = lerp(fullDiscCol, filledWithChargeRing, isCurrentArc);

				// finally, if not in any arc, sample the out of bounds texture

				float4 outOfBounds = lerp(fixed4(0, 0, 0, 0), _OutOfBoundsColour, tex2D(_OutOfBoundsTex, i.uv * _OutOfBoundsTextureScale).a);

				return lerp(outOfBounds, combinedFullDiscWithChargeRings, isInAnyArc);
			}

			ENDCG
		}

		// Pass 1 just draws a solid colour, a fill circle, and optionally some rings for charge levels.
		Pass
		{
			CGPROGRAM

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
			uniform fixed4 _Colour;
			uniform float _FilledAlpha;
			uniform float _UnfilledAlpha;

			uniform float _Fill;
			uniform float _InnerRingDist;
			uniform float _MaskRange;

			uniform float _ChargeRings[10];
			uniform int _ChargeRingCount;
			uniform float _ChargeRingWidth;

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
				float halfChargeRingWidth = _ChargeRingWidth * 0.5;

				float x = i.uv.x;
				float y = i.uv.y;
				float dist = sqrt(pow((0.5 - x), 2) + pow((0.5 - y), 2)); // calculate the euclidean distance of this point from the centre

				float isChargeRing = 0;

				for (int i = 0; i < _ChargeRingCount; i++)
				{
					float chargeRingDist = lerp(_InnerRingDist, _MaskRange, _ChargeRings[i]) * 0.5;
					float innerDist = chargeRingDist - halfChargeRingWidth;
					float outerDist = chargeRingDist + halfChargeRingWidth;

					// this whole line basically produces a positive value if the current texel is between the inner
					// and outer dist, or a 0 or negative otherwise. i'm then using max to combine it with the previous results
					isChargeRing = max(isChargeRing, ceil((outerDist - dist) * (dist - innerDist)));
				}

				// modify fill slightly so that 0 is the distance of the inner ring, so it appears immediately,
				// as opposed to the centre of the circle.
				// halve it because we're drawing on a 1x1 texture (radius 0.5), but we want to be drawing with a circle of radius 1
				float fill = lerp(_InnerRingDist, _MaskRange, _Fill) * 0.5;

				float isFilled = ceil(fill - dist);

				fixed4 filled = _Colour;
				filled.a = _FilledAlpha;

				fixed4 unfilled = _Colour;
				unfilled.a = _UnfilledAlpha;

				filled = lerp(unfilled, filled, isFilled);

				return lerp(filled, _Colour, isChargeRing);
			}

			ENDCG
		}

		// Pass 2 masks out most of the texture in a cone shape and draws a nice border
		Pass
		{
			CGPROGRAM

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
			uniform float _MaskRange;
			uniform float _InnerRingDist;
			uniform float _BorderSize;
			uniform fixed4 _Colour;

			uniform float _OuterMaskRangeScalar;
			uniform float _OuterGradientSize;

			uniform float _MaskStartAngle;
			uniform float _MaskEndAngle;

			uniform fixed2 _MaskStartPoint;
			uniform fixed2 _MaskEndPoint;

			uniform float _OuterMaskStartAngle;
			uniform float _OuterMaskEndAngle;

			uniform fixed2 _OuterMaskStartPoint;
			uniform fixed2 _OuterMaskEndPoint;

			v2f vert(appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = v.color;
				UNITY_TRANSFER_FOG(o, o.vertex);
				return o;
			}

			inline int Intersect(float one, float two, float three)
			{
				return (two > three && (one >= two || one <= three)) || (one >= two && one <= three);
			}

			inline bool IsIn(float4 pos, float start, float end)
			{
				return Intersect(atan2(pos.y - 0.5, pos.x - 0.5) * 57.295779, start, end);
			}

			inline float SegmentDistance(float2 p, float2 a, float2 b)
			{
				float2 ab = b - a, ap = p - a;
				return length(ap - ab * clamp(dot(ab, ap) / dot(ab, ab), 0, 1));
			}

			inline float invLerp(float from, float to, float value) 
			{
				return (value - from) / (to - from);
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float4 col = tex2D(_MainTex, i.uv);

				float x = i.uv.x;
				float y = i.uv.y;
				float dist = sqrt(pow((0.5 - x), 2) + pow((0.5 - y), 2)); // calculate the euclidean distance of this point from the centre

				float range = 0.5f * _MaskRange;

				// now check if we're near the edges of the arc
				float distToStartEdge = SegmentDistance(float2(x, y), float2(0.5, 0.5), _MaskStartPoint);
				float distToEndEdge = SegmentDistance(float2(x, y), float2(0.5, 0.5), _MaskEndPoint);
				float distToFarEdge = range - (0.25 * _BorderSize) - dist;

				float distToEdge = min(distToStartEdge, distToEndEdge);
				distToEdge = min(distToEdge, distToFarEdge);

				float isBorder = ceil(_BorderSize - distToEdge);

				fixed4 maskBorder = lerp(col, _Colour, isBorder);

				float4 flippedPos = float4(y, x, 0, 0);

				float isInCone = IsIn(flippedPos, _MaskStartAngle, _MaskEndAngle);
				isInCone = lerp(0, isInCone, ceil(range - dist));

				// mask out everything that's not in the mask arc
				fixed4 maskCol = lerp(fixed4(0, 0, 0, 0), maskBorder, isInCone);

				// now do the gradient previewing stuff outside the mask

				float isInOuterCone = IsIn(flippedPos, _OuterMaskStartAngle, _OuterMaskEndAngle);

				float maskRange = lerp(range, range * _OuterMaskRangeScalar, isInOuterCone - isInCone);

				float isOnlyInOuterCone = lerp(0, isInOuterCone, ceil(maskRange - dist)) - isInCone;

				float innerGradientVal = 0.01;

				//float inverseLerpedStartToBlurEdge = invLerp(innerGradientVal, 0, distToStartEdge);
				//float inverseLerpedEndToBlurEdge = invLerp(innerGradientVal, 0, distToEndEdge);
				//float combinedInnerInvLerps = lerp(0, max(inverseLerpedStartToBlurEdge, inverseLerpedEndToBlurEdge), isOnlyInOuterCone);

				float inverseLerpedDistToStartEdge = invLerp(_OuterGradientSize, 0, distToStartEdge);
				float inverseLerpedDistToEndEdge = invLerp(_OuterGradientSize, 0, distToEndEdge);
				float combinedOuterInvLerps = lerp(0, max(inverseLerpedDistToStartEdge, inverseLerpedDistToEndEdge), isOnlyInOuterCone);

				//return (1, 1, 1, 1) * combinedOuterInvLerps;

				//float combinedInvLerps = combinedOuterInvLerps;// lerp(0, combinedOuterInvLerps, combinedInnerInvLerps);

				fixed4 outerMaskCol = col;
				outerMaskCol.a = lerp(0, outerMaskCol.a, combinedOuterInvLerps);

				float isInInnerRing = 1 - ceil(dist - _InnerRingDist * 0.5);
				return lerp(lerp(maskCol, outerMaskCol, isOnlyInOuterCone), fixed4(0, 0, 0, 0), isInInnerRing);
			}

			ENDCG
		}


		// Pass 3 masks out most of the texture in a box shape and draws a nice border
		Pass
		{
			CGPROGRAM

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
			uniform fixed2 _MaskStartPoint;
			uniform fixed2 _MaskEndPoint;
			uniform float _MaskRange;
			uniform float _BoxMaskThickness;
			uniform float _InnerRingDist;
			uniform float _BorderSize;
			uniform fixed4 _Colour;

			v2f vert(appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = v.color;
				UNITY_TRANSFER_FOG(o, o.vertex);
				return o;
			}

			inline int Intersect(float one, float two, float three)
			{
				return (two > three && (one >= two || one <= three)) || (one >= two && one <= three);
			}

			inline bool IsIn(float4 pos, float start, float end)
			{
				return Intersect(atan2(pos.y - 0.5, pos.x - 0.5) * 57.295779, start, end);
			}

			inline float SegmentDistance(float2 p, float2 a, float2 b)
			{
				float2 ab = b - a, ap = p - a;
				return length(ap - ab * clamp(dot(ab, ap) / dot(ab, ab), 0, 1));
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float4 col = tex2D(_MainTex, i.uv);

				float x = i.uv.x;
				float y = i.uv.y;
				float dist = sqrt(pow((0.5 - x), 2) + pow((0.5 - y), 2)); // calculate the euclidean distance of this point from the centre

				float range = 0.5f * _MaskRange;

				// now check if we're near the edges of the arc
				float distToLine = SegmentDistance(float2(x, y), _MaskStartPoint, _MaskEndPoint);
				float distToFarEdge = range - (0.25 * _BorderSize) - dist;

				float halfBoxThickness = 0.5 * _BoxMaskThickness;

				float isBorder = ceil(_BorderSize - distToFarEdge) + ceil(distToLine - halfBoxThickness);

				fixed4 maskBorder = lerp(col, _Colour, isBorder);

				float isInsideMask = ceil((halfBoxThickness + _BorderSize) - distToLine);

				// mask out everything that's not in the mask arc
				fixed4 maskCol = lerp(fixed4(0, 0, 0, 0), maskBorder, isInsideMask);

				// mask out everything beyond the mask range
				float isInsideMaskRange = ceil(range - dist);
				maskCol = lerp(float4(0, 0, 0, 0), maskCol, isInsideMaskRange);

				// mask out the inner part of the disc
				float isInsideInnerRing = ceil(dist - _InnerRingDist * 0.5);
				maskCol = lerp(fixed4(0, 0, 0, 0), maskCol, isInsideInnerRing);

				return maskCol;
			}

			ENDCG
		}
	}
}
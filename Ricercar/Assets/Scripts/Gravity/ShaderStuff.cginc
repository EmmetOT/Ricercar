int when_eq(float x, float y) 
{
    return 1 - abs(sign(x - y));
}

int when_neq(float x, float y) 
{
    return abs(sign(x - y));
}

int when_gt(float x, float y) 
{
  return max(sign(x - y), 0);
}

int when_lt(float x, float y) 
{
    return max(sign(y - x), 0);
}

int when_ge(float x, float y) 
{
  return 1 - when_lt(x, y);
}

int when_le(float x, float y) 
{
  return 1 - when_gt(x, y);
}

int and(int a, int b) 
{
  return a * b;
}

int or(int a, int b) 
{
  return min(a + b, 1);
}

int xor(int a, int b) 
{
  return (a + b) % 2;
}

int not(int a) 
{
  return 1 - a;
}

float2 ProjectPointOnLineSegment(float2 a, float2 b, float2 p)
{
    float2 diff = b - a;
    float diffMagnitude = length(diff);
    float2 lineDirection = diff / diffMagnitude;

    //get vector from point on line to point in space
    float2 linePointToLineDirection = p - a;

    // how far along the point is from a to b
    float t = dot(linePointToLineDirection, lineDirection);

    float2 projectedPoint = a + lineDirection * t;

    float2 pointVec = projectedPoint - a;

    float dotProduct = dot(pointVec, diff);

    float dotGreaterThanZero = when_gt(dotProduct, 0);
    float pointVecShorterThanDiff = when_le(length(pointVec), diffMagnitude);

    return lerp(a, lerp(b, projectedPoint, pointVecShorterThanDiff), dotGreaterThanZero);
}

float invLerp(float from, float to, float value) 
{
	return (value - from) / (to - from);
}

float2 transform(float2 uv, float2 pivot, float rotation, float scale)
{
    float cosRot = cos(rotation);
    float sinRot = sin(rotation);
    float2x2 rotationMatrix = { cosRot, -sinRot, sinRot, cosRot };

    uv -= pivot;
    uv = mul(rotationMatrix, uv);
    uv /= scale;
    uv += pivot;
    return uv;
}
		
float4 SampleBilinear(Texture2DArray<float4> t, int texIndex, float2 uv)
{
    float4 tl = t[float3(uv, texIndex)];
    float4 tr = t[float3(uv + float2(1, 0), texIndex)];
    float4 bl = t[float3(uv + float2(0, 1), texIndex)];
    float4 br = t[float3(uv + float2(1, 1), texIndex)];
    float2 f = frac(uv);
    float4 tA = lerp(tl, tr, f.x);
    float4 tB = lerp(bl, br, f.x);
    return lerp(tA, tB, f.y);
}
	
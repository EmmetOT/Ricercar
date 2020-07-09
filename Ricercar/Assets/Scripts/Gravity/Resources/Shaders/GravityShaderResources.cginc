#define G 667.4
#define DEGREES_TO_RADIANS 0.0174533
#define DEFAULT_BAKED_SIZE 256
#define EPSILON 0.00000001

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

struct AttractorData
{
    float x;
    float y;
    int ignore;
    float mass;
    float radius;
    float surfaceGravityForce;

    int isLine;
    float lineStartX;
    float lineStartY;
    float lineEndX;
    float lineEndY;
};

struct BakedAttractorData
{
    float2 position;
    int ignore;
    float mass;
    float2 centreOfGravity;
    float rotation;
    float size;
    float scale;
    int textureIndex;
};

float2x2 GetRotationMatrix(float rotation)
{
    float cosRot = cos(rotation);
    float sinRot = sin(rotation);
    float2x2 rotationMatrix = { cosRot, -sinRot, sinRot, cosRot };

    return rotationMatrix;
}

float2 Rotate(float2 vec, float rotation)
{
    float cosRot = cos(rotation);
    float sinRot = sin(rotation);
    float2x2 rotationMatrix = { cosRot, -sinRot, sinRot, cosRot };

    return mul(rotationMatrix, vec);
}

float2 Transform(float2 uv, float2 pivot, float rotation, float scale)
{
    uv -= pivot;
    uv = Rotate(uv, rotation);
    uv /= scale;
    uv += pivot;

    return uv;
}
		
float2 Transform(float2 uv, float2 pivot, float2x2 rotation, float2 scale)
{
    uv -= pivot;
    uv = float2(uv.x / scale.x, uv.y / scale.y);
    uv = mul(rotation, uv);
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
	
float2 CalculateGravity(float2 to, float2 from, float massOne, float massTwo)
{
    // adding a tiny number prevents NaN
    float2 difference = (to - from) + EPSILON;
    float2 direction = normalize(difference);
    float sqrMagnitude = dot(difference, difference);

    float forceMagnitude = (G * massOne * massTwo) / sqrMagnitude;
    float2 gravityForce = (direction * forceMagnitude);

    return gravityForce;
}

float2 CalculateGravity(Texture2DArray<float4> textures, BakedAttractorData data, float2 uvPosition, float2 globalScalar, float2 scaledWorldPos, float2 pointPosition, float pointMass)
{
    int doNotCount = data.ignore;
    float scale = data.scale;
    float2 scaleSampler = globalScalar * scale;
    float2 origin = float2(data.size, data.size) * 0.5;
    float2x2 rotation = GetRotationMatrix(-data.rotation * DEGREES_TO_RADIANS);

    float2 coord = Transform(uvPosition, origin - scaledWorldPos, rotation, scaleSampler) + scaledWorldPos;

    // first, we transform the texture and sample it to get the force "baked in"
    float4 texData = SampleBilinear(textures, data.textureIndex, coord);
        
    // mass has to be divided by square of scale to stay constant
    // - doubling the size of an object will quarter its density (in 2 dimensions)
    float scaledMass = data.mass / (scale * scale);

    // values have to be scaled back to their true forms!
    float4 gravityForce = texData * scaledMass;

    // rotate force vectors by rotation of attractor
    float2 bakedForce = mul(float2(gravityForce.x, gravityForce.y), rotation);
    
    // next calculate the normal point gravity towards the centre of gravity
    float2 pointGravity = CalculateGravity(data.centreOfGravity, pointPosition, data.mass, 1);

    // this determines whether we're inside or outside the baked texture
    float isOutsideBounds = or(or(when_le(coord.x, 1), when_le(coord.y, 1)), or(when_ge(coord.x, data.size - 1), when_ge(coord.y, data.size - 1)));

    return (1 - doNotCount) * lerp(bakedForce, pointGravity, isOutsideBounds);
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

float InvLerp(float from, float to, float value) 
{
	return (value - from) / (to - from);
}


#define RM 39482.17593
#define RD1 7.8671
#define RD2 3.3419
#define RD3 5.8912
#define RP1 2.1759
#define RP2 4.7921

float Random11(float seed)
{
    return frac(sin(dot(float2(RD1, seed), float2(seed, RD2))) * RM);
}
float2 Random12(float seed)
{
    return float2(
        frac(sin(dot(float2(RD1, seed), float2(seed, RD2))) * RM),
        frac(sin(dot(float2(seed, RD2), float2(RD3, seed))) * RM)
    );
}
float3 Random13(float seed)
{
    return float3(
        frac(sin(dot(float2(seed, RD1), float2(RD2, seed))) * RM),
        frac(sin(dot(float2(seed, RD2), float2(RD3, seed))) * RM),
        frac(sin(dot(float2(seed, RD3), float2(RD1, seed))) * RM)
    );
}

float RandomRange11(float seed, float min, float max)
{
    return lerp(min, max, Random11(seed)); 
}
float2 RandomRange12(float seed, float2 min, float2 max)
{
    float2 vec;
    vec.x = RandomRange11(seed,       min.x, max.x);
    vec.y = RandomRange11(seed + RP1, min.y, max.y);
    return vec;
}
float3 RandomRange13(float seed, float3 min, float3 max)
{
    float3 vec;
    vec.x = RandomRange11(seed,       min.x, max.x);
    vec.y = RandomRange11(seed + RP1, min.y, max.y);
    vec.z = RandomRange11(seed + RP2, min.z, max.z);
    return vec;
}

float Random21(float2 seed)
{
    return frac(sin(dot(seed, float2(RD1, RD2))) * RM);
}
float2 Random22(float2 seed)
{
    return float2(
        frac(sin(dot(seed,                    float2(RD1, RD2))) * RM),
        frac(sin(dot(seed + float2(RP1, RP2), float2(RD2, RD3))) * RM)
    );
}
float3 Random23(float2 seed)
{
    return float3(
        frac(sin(dot(seed,                    float2(RD1, RD2))) * RM),
        frac(sin(dot(seed + float2(RP1, RP2), float2(RD2, RD3))) * RM),
        frac(sin(dot(seed + float2(RP2, RP1), float2(RD3, RD1))) * RM)
    );
}

float RandomRange21(float2 seed, float min, float max)
{
    return lerp(min, max, Random21(seed)); 
}
float2 RandomRange22(float2 seed, float2 min, float2 max)
{
    float2 vec;
    vec.x = RandomRange21(seed,                    min.x, max.x);
    vec.y = RandomRange21(seed + float2(RP1, RP2), min.y, max.y);
    return vec;
}
float3 RandomRange23(float2 seed, float3 min, float3 max)
{
    float3 vec;
    vec.x = RandomRange21(seed,                    min.x, max.x);
    vec.y = RandomRange21(seed + float2(RP1, RP2), min.y, max.y);
    vec.z = RandomRange21(seed + float2(RP2, RP1), min.z, max.z);
    return vec;
}
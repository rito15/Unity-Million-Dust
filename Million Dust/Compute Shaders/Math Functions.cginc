
float SqrMagnitude(float3 vec)
{
    return (vec.x * vec.x) + (vec.y * vec.y) + (vec.z * vec.z); // dot(vec,vec)
}

float Square(float value)
{
    return value * value;
}

bool InRange2(float2 vec, float2 min, float2 max)
{
    return min.x <= vec.x && vec.x <= max.x &&
           min.y <= vec.y && vec.y <= max.y;
}

bool InRange3(float3 vec, float3 min, float3 max)
{
    return min.x <= vec.x && vec.x <= max.x &&
           min.y <= vec.y && vec.y <= max.y &&
           min.z <= vec.z && vec.z <= max.z;
}

bool ExRange3(float3 vec, float3 min, float3 max)
{
    return min.x < vec.x && vec.x < max.x &&
           min.y < vec.y && vec.y < max.y &&
           min.z < vec.z && vec.z < max.z;
}

// 벡터의 X, Y, Z 성분 중 가장 큰 성분 구하기
float MaxElement(float3 vec)
{
    return max(vec.x, max(vec.y, vec.z));
}

float3 ReverseX(float3 vec)
{
    return float3(-vec.x, vec.y, vec.z);
}

float3 ReverseY(float3 vec)
{
    return float3(vec.x, -vec.y, vec.z);
}

float3 ReverseZ(float3 vec)
{
    return float3(vec.x, vec.y, -vec.z);
}

// flag
// 0 : X
// 1 : Y
// 2 : Z
float3 Reverse(float3 vec, uint flag)
{
    switch(flag)
    {
        case 0: return float3(-vec.x, vec.y, vec.z);
        case 1: return float3(vec.x, -vec.y, vec.z);
        case 2: return float3(vec.x, vec.y, -vec.z);
    }
    return 0;
}
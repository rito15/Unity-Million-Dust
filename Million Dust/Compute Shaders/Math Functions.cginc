
float SqrMagnitude(float3 vec)
{
    return (vec.x * vec.x) + (vec.y * vec.y) + (vec.z * vec.z); // dot(vec,vec)
}

float Square(float value)
{
    return value * value;
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
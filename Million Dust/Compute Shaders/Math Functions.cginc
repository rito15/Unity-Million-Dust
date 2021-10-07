
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
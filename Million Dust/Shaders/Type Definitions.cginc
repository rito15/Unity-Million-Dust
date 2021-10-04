
struct Dust
{
    float3 position;
    int isAlive;
};

// 육면체 영역
struct Bounds
{
    float3 min;
    float3 max;
};

// 평면
struct Plane
{
    float3 position; // 평면 위의 한 점
    float3 normal;   // 평면의 법선
};
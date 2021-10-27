
struct Dust
{
    float3 position;
    int isAlive;
};

// ����ü ����
struct Bounds
{
    float3 min;
    float3 max;
};

// ���
struct Plane
{
    float3 position; // ��� ���� �� ��
    float3 normal;   // ����� ����
};

struct ColliderTransform
{
    float4x4 localToWorld;
    float4x4 worldToLocal;
    float3 scale;
};
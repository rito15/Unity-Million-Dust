
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
#ifndef GERSTNER_WAVE_INCLUDED
#define GERSTNER_WAVE_INCLUDED
#define PI 3.14159267

// 一波 Gerstner 波浪计算（输出 偏移 + 法线 信息）
// 输出：偏移后的世界位置 + 世界空间法线
void GerstnerWave(
    float3 worldPos,       // 原始世界坐标
    float waveHeight,      // 浪高
    float waveLength,      // 波长
    float waveSpeed,       // 速度
    float2 waveDir,        // 方向 (归一化)
    float steepness,       // 陡峭度 (0~1)
    float time,            // 时间
    inout float3 outPos,   // 输出叠加后的位置
    inout float3 outNormal // 输出叠加后的法线
)
{
    float k = 2 * PI / waveLength;         // 波数
    float c = waveSpeed * k;               // 波速
    float2 d = normalize(waveDir);         // 方向
    float f = k * dot(d, worldPos.xz) - c * time; // 波函数
    float a = waveHeight;                  // 振幅

    // 顶点偏移
    float dx = steepness * a * d.x * cos(f);
    float dz = steepness * a * d.y * cos(f);
    float dy = a * sin(f);

    outPos.x += dx;
    outPos.z += dz;
    outPos.y += dy;

    // 法线计算
    float3 norm = float3(
        -dx * k,
        1 - dy * k,
        -dz * k
    );
    outNormal += norm;
}

// 快速随机函数
float FastRand(float n)
{
    return frac(sin(n * 0.123f) * 2687.321f);
}

// ==============================
// 修复版：层数越多不会变尖！
// ==============================
void MultiGerstnerWave(
    float3 worldPos,
    float waveHeight,
    float waveLength,
    float waveSpeed,
    float2 waveDir,
    float steepness,
    float time,
    int waveCount,        // 叠加次数
    inout float3 outPos,
    inout float3 outNormal
)
{
    // 核心修复：总高度平分给所有波浪
    // 不管多少层，总高度永远等于你设置的 waveHeight
    float heightPerWave = waveHeight / max(waveCount, 1);

    for (int i = 0; i < waveCount; i++)
    {
        // 随机方向（自然杂乱）
        float r = FastRand(i * 12.5f);
        float angle = r * PI * 2.0f;
        float cosA = cos(angle);
        float sinA = sin(angle);
        float2 dir = mul(float2x2(cosA, -sinA, sinA, cosA), waveDir);

        // 随机长短波浪（更自然）
        float randLen = lerp(0.7f, 1.5f, FastRand(i + 2.0f));
        float randSpd = lerp(0.8f, 1.2f, FastRand(i + 7.0f));

        float len = waveLength * randLen;
        float spd = waveSpeed * randSpd;

        // 每一层只用【平分后的高度】，不会叠加爆炸
        GerstnerWave(worldPos, heightPerWave, len, spd, dir, steepness, time, outPos, outNormal);
    }

    outNormal = normalize(outNormal);
}

#endif
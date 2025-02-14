#ifndef Z_RANDOM_H
#define Z_RANDOM_H

/*
 * 生成随机向量，依赖于 frameCounter 帧计数器
 * 代码来源：https://blog.demofox.org/2020/05/25/casual-shadertoy-path-tracing-1-basic-camera-diffuse-emissive/
*/
// screen uv, screen resolution (width, height)
uint ZRP_RandomSeed(float2 uv, float2 screenWH)
{
    return uint(
        uint(uv.x * screenWH.x)  * uint(1973) + 
        uint(uv.y * screenWH.y) * uint(9277) + 
        uint(114514) * uint(26699)) | uint(1);
}

uint ZRP_Hash(inout uint seed) {
    seed = uint(seed ^ uint(61)) ^ uint(seed >> uint(16));
    seed *= uint(9);
    seed = seed ^ (seed >> 4);
    seed *= uint(0x27d4eb2d);
    seed = seed ^ (seed >> 15);
    return seed;
}
 
float ZRP_Rand(inout uint seed) {
    return float(ZRP_Hash(seed)) / 4294967296.0;
}

#endif
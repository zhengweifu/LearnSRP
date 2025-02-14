#ifndef __GAMMA_CORRECTION_COMMON_H__
#define __GAMMA_CORRECTION_COMMON_H__

#define select(q, a, b) (q ? a : b)

half ZRP_LinearToSrgbBranchingChannel(half value)
{
    return select(value < 0.00313067, value * 12.92, pow(value, (1.0/2.4) * 1.055 - 0.055)); 
}

half3 ZRP_LinearToSrgbBranching(half3 color)
{
    return half3(
        ZRP_LinearToSrgbBranchingChannel(color.r),
        ZRP_LinearToSrgbBranchingChannel(color.g),
        ZRP_LinearToSrgbBranchingChannel(color.b));
}

half3 ZRP_LinearToSrgb(half3 color)
{
    return ZRP_LinearToSrgbBranching(color);
}

half3 ZRP_SrgbToLinear(half3 color)
{
    color = max(6.10352e-5, color);
    return select(color > 0.04045, pow(color * (1.0 / 1.055) + 0.0521327, 2.4), color * (1.0 / 12.92));
}

#endif 
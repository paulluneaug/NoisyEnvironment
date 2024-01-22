using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NoiseUtils
{
    public static uint GetCellSeed2D(int ix, int iy, int seed)
    {
        int w = 8 * 4;
        int s = w / 2;
        uint a = (uint)ix;
        uint b = (uint)iy;
        a *= 1284157443 + ((uint)seed * 821521017);
        b ^= a << s | a >> w - s;
        b *= 1911520717;
        a ^= b << s | b >> w - s;
        a *= 2048419325;
        return a;
    }    
    
    // Hash function from H. Schechter & R. Bridson, goo.gl/RXiKaH
    public static uint Hash(uint s)
    {
        s ^= 2747636419u;
        s *= 2654435769u;
        s ^= s >> 16;
        s *= 2654435769u;
        s ^= s >> 16;
        s *= 2654435769u;
        return s;
    }

    public static float RandomFloat(ref uint seed)
    {
        return RandomFloat01(ref seed) * 2 - 1; // [-1;1]
    }

    public static float RandomFloat01(ref uint seed)
    {
        seed = Hash(seed);
        return seed / 4294967295.0f; // 2^32-1
    }

    public static float Smootherstep(float w)
    {
        w = Mathf.Clamp(w, 0.0f, 1.0f);
        return ((w * (w * 6.0f - 15.0f) + 10.0f) * w * w * w);
    }
}

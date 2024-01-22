using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Windows;

using static NoiseUtils;

public static class PerlinNoiseGenerator
{
    public class PerlinNoiseGenerationParameters
    {
        public Vector2Int ZoneToGenerate;
        public PerlinNoiseLayer Layer;

        public PerlinNoiseGenerationParameters(Vector2Int zoneToGenerate, PerlinNoiseLayer layer)
        {
            ZoneToGenerate = zoneToGenerate;
            Layer = layer;
        }
    }

    public struct PerlinNoiseLayer
    {
        public int GradientOffset;
        public int NoiseScale;

        public float Pow;

        public bool UseSmootherStep;
        public bool Inverse;

        public PerlinNoiseLayer(int gradientOffset, int scale, bool useSmootherStep, bool inverse, float pow)
        {
            GradientOffset = gradientOffset;
            NoiseScale = scale;
            UseSmootherStep = useSmootherStep;
            Inverse = inverse;
            Pow = pow;
        }
    }

    public static float[,] GenerateZone(PerlinNoiseGenerationParameters parameters)
    {
        Vector2Int zoneToGenerate = parameters.ZoneToGenerate;
        float[,] result = new float[zoneToGenerate.x, zoneToGenerate.y];

        Parallel.For(0, zoneToGenerate.x * zoneToGenerate.y, i => GetNoiseValue(i, zoneToGenerate.x, parameters, ref result));

        return result;
    }

    public static void GetNoiseValue(int index, int xSize, PerlinNoiseGenerationParameters parameters, ref float[,] result)
    {
        int ix = index % xSize;
        int iy = index / xSize;

        float x = (float)ix / parameters.Layer.NoiseScale;
        float y = (float)iy / parameters.Layer.NoiseScale;

        // Determine grid cell coordinates
        int x0 = Mathf.FloorToInt(x);
        int y0 = Mathf.FloorToInt(y);
        int x1 = x0 + 1;
        int y1 = y0 + 1;

        // Determine interpolation weights
        // Could also use higher order polynomial/s-curve here
        float sx = x - x0;
        float sy = y - y0;

        // Interpolate between grid point gradients
        int gradientOffset = parameters.Layer.GradientOffset;
        bool useSmootherStep = parameters.Layer.UseSmootherStep;

        float n0 = DotGridGradient2D(x0, y0, x, y, gradientOffset);
        float n1 = DotGridGradient2D(x1, y0, x, y, gradientOffset);
        float ix0 = Interpolate(n0, n1, sx, useSmootherStep);
        n0 = DotGridGradient2D(x0, y1, x, y, gradientOffset);
        n1 = DotGridGradient2D(x1, y1, x, y, gradientOffset);
        float ix1 = Interpolate(n0, n1, sx, useSmootherStep);
        float layerValue = Interpolate(ix0, ix1, sy, useSmootherStep);

        layerValue = layerValue / 2 + 0.5f;

        if (parameters.Layer.Inverse)
        {
            layerValue = 1.0f - layerValue;
        }
        result[ix, iy] = Mathf.Pow(layerValue, parameters.Layer.Pow);
    }

    public static float Smoothstep(float w)
    {
        w = Mathf.Clamp(w, 0.0f, 1.0f);
        return w * w * (3.0f - 2.0f * w);
    }

    public static float Interpolate(float a0, float a1, float w, bool smootherStep)
    {
        float smoothW = 0;
        if (smootherStep)
        {
            smoothW = Smootherstep(w);
        }
        else
        {
            smoothW = Smoothstep(w);
        }

        return a0 + (a1 - a0) * smoothW;
    }



    public static float3 RandomFloat3InsideUnitSphere(ref uint seed)
    {
        float3 attempt;
        do
        {
            attempt = new float3(RandomFloat(ref seed), RandomFloat(ref seed), RandomFloat(ref seed));
        } while (attempt.x * attempt.x + attempt.y * attempt.y + attempt.z * attempt.z > 1);
        return attempt;
    }

    // Noise generation inspired by the Perlin Noise Wikipedia article (https://en.wikipedia.org/wiki/Perlin_noise)
    public static float3 RandomGradient(int ix, int iy, int iz, int gradientOffset)
    {
        // No precomputed gradients mean this works for any number of grid coordinates
        int w = 8 * 4;
        int s = w / 2;
        uint a = (uint)ix;
        uint b = (uint)iy;
        uint c = (uint)iz;
        a *= 1284157443;
        b ^= a << s | a >> w - s;
        b *= 1911520717 - (uint)Mathf.Abs(gradientOffset);
        c ^= b << s | b >> w - s;
        c *= 1529716214;
        a ^= c << s | c >> w - s;
        a *= 2048419325;

        return RandomFloat3InsideUnitSphere(ref a);
    }

    // Noise generation inspired by the Perlin Noise Wikipedia article (https://en.wikipedia.org/wiki/Perlin_noise)
    public static float2 RandomGradient2D(int ix, int iy, int seed)
    {
        // No precomputed gradients mean this works for any number of grid coordinates
        uint a = GetCellSeed2D(ix, iy, seed);

        return new float2(Mathf.Cos(a), Mathf.Sin(a));
    }

    public static float DotGridGradient(int ix, int iy, int iz, float x, float y, float z, int seed)
    {
        float3 randomVec = RandomGradient(ix, iy, iz, seed);

        // Compute the distance vector
        float dx = x - ix;
        float dy = y - iy;
        float dz = z - iz;

        // Compute the dot-product
        return (dx * randomVec.x + dy * randomVec.y + dz * randomVec.z);
    }

    public static float DotGridGradient2D(int ix, int iy, float x, float y, int gradientOffset)
    {
        float2 randomVec = RandomGradient2D(ix, iy, gradientOffset);

        // Compute the distance vector
        float dx = x - (float)ix;
        float dy = y - (float)iy;

        // Compute the dot-product
        return (dx * randomVec.x + dy * randomVec.y);
    }
}

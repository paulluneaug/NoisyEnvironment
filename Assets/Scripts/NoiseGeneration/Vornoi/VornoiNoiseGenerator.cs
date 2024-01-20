using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Diagnostics;
using static NoiseUtils;

public static class VornoiNoiseGenerator
{
    public class VornoiNoiseGenerationParameters
    {
        public Vector2Int ZoneToGenerate;
        public VornoiNoiseLayer Layer;

        public VornoiNoiseGenerationParameters(Vector2Int zoneToGenerate, VornoiNoiseLayer layer)
        {
            ZoneToGenerate = zoneToGenerate;
            Layer = layer;
        }
    }

    public struct VornoiNoiseLayer
    {
        public int GradientOffset;
        public int NoiseScale;

        public bool UseSmootherStep;
        public bool Inverse;

        public VornoiNoiseLayer(int gradientOffset, int scale, bool useSmootherStep, bool inverse)
        {
            GradientOffset = gradientOffset;
            NoiseScale = scale;
            UseSmootherStep = useSmootherStep;
            Inverse = inverse;
        }
    }

    public static float[,] GenerateZone(VornoiNoiseGenerationParameters parameters)
    {
        Vector2Int zoneToGenerate = parameters.ZoneToGenerate;
        float[,] result = new float[zoneToGenerate.x, zoneToGenerate.y];

        Parallel.For(0, zoneToGenerate.x * zoneToGenerate.y, i => GetNoiseValue(i, zoneToGenerate.x, parameters, ref result));

        return result;
    }

    public static void GetNoiseValue(int index, int xSize, VornoiNoiseGenerationParameters parameters, ref float[,] result)
    {
        int ix = index % xSize;
        int iy = index / xSize;

        float x = (float)ix / parameters.Layer.NoiseScale;
        float y = (float)iy / parameters.Layer.NoiseScale;

        float2 position = new float2(x, y);

        // Determine grid cell coordinates
        int x0 = Mathf.FloorToInt(x);
        int y0 = Mathf.FloorToInt(y);

        float minSqrDist = 2.0f;

        for(int jx = -1;  jx <= 1; ++jx) 
        {
            for (int jy = -1; jy <= 1; ++jy)
            {
                minSqrDist = Mathf.Min(minSqrDist, SqrMagnitude(GetCellPointCoordinates(x0 + jx, y0 + jy, parameters.Layer.GradientOffset) - position));
            }
        }


        float layerValue = minSqrDist;// Mathf.Sqrt(minSqrDist);

        layerValue = layerValue / 2 + 0.5f;

        if (parameters.Layer.Inverse)
        {
            layerValue = 1.0f - layerValue;
        }

        result[ix, iy] = layerValue;
    }

    private static float SqrMagnitude(float2 v)
    {
        return v.x * v.x + v.y * v.y;
    }

    private static float2 GetCellPointCoordinates(int cellX, int cellY, int seed)
    {
        uint cellSeed = GetCellSeed2D(cellX, cellY, seed);

        return new float2(cellX + RandomFloat(ref cellSeed) / 2 + 0.5f , cellY + RandomFloat(ref cellSeed) / 2 + 0.5f);
    }
}

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
        public VornoiNoiseLayer[] Layers;

        public int LayerCount;
        public float LayerWeightMultiplier;

        public VornoiNoiseGenerationParameters(Vector2Int zoneToGenerate, VornoiNoiseLayer[] layers)
        {
            ZoneToGenerate = zoneToGenerate;
            Layers = layers;
            LayerCount = layers.Length;
            LayerWeightMultiplier = 1.0f / layers.Sum(l => l.LayerWeigth);
        }
    }

    public struct VornoiNoiseLayer
    {
        public float LayerWeigth;

        public int GradientOffset;
        public int NoiseScale;

        public bool UseSmootherStep;
        public bool Inverse;

        public VornoiNoiseLayer(float layerWeigth, int gradientOffset, int scale, bool useSmootherStep, bool inverse)
        {
            LayerWeigth = layerWeigth;
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
        float value = 0;

        int ix = index % xSize;
        int iy = index / xSize;

        for (int i = 0; i < parameters.LayerCount; i++)
        {
            VornoiNoiseLayer currentLayer = parameters.Layers[i];

            float x = (float)ix / currentLayer.NoiseScale;
            float y = (float)iy / currentLayer.NoiseScale;

            float2 position = new float2(x, y);

            // Determine grid cell coordinates
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);

            float minSqrDist = 2.0f;

            for(int jx = -1;  jx <= 1; ++jx) 
            {
                for (int jy = -1; jy <= 1; ++jy)
                {
                    minSqrDist = Mathf.Min(minSqrDist, SqrMagnitude(GetCellPointCoordinates(x0 + jx, y0 + jy, currentLayer.GradientOffset) - position));
                }
            }


            float layerValue = minSqrDist;// Mathf.Sqrt(minSqrDist);

            layerValue = layerValue / 2 + 0.5f;

            if (currentLayer.Inverse)
            {
                layerValue = 1.0f - layerValue;
            }

            value += layerValue * currentLayer.LayerWeigth * parameters.LayerWeightMultiplier;
        }

        result[ix, iy] = value;
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
